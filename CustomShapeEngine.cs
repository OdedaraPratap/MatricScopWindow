using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using OpenCvSharp;

namespace Matric_scope
{
    public enum CustomSegmentationMode
    {
        Auto = 0,
        OtsuDarkObject = 1,
        BackgroundDifference = 2,
        CannyClosedContour = 3
    }

    public enum CustomPoseSolver
    {
        AutoHybrid = 0,
        ContourCorrespondence = 1,
        PcaAssisted = 2,
        MinAreaRectAssisted = 3
    }

    public sealed class CustomShapeTrainingOptions
    {
        public CustomSegmentationMode SegmentationMode { get; set; } = CustomSegmentationMode.Auto;
        public CustomPoseSolver PoseSolver { get; set; } = CustomPoseSolver.AutoHybrid;
        public bool RejectAmbiguousPose { get; set; } = true;
        public bool AllowSymmetricEquivalentMeasurements { get; set; } = false;
    }

    public class CustomShapeEngine
    {
        private const string FingerprintPrefix = "CSF6";
        private const string LegacyFingerprintPrefix5 = "CSF5";
        private const string LegacyFingerprintPrefix4 = "CSF4";
        private const string LegacyFingerprintPrefix3 = "CSF3";
        private const int FingerprintPointCount = 256;

        public double PixelToMmRatio { get; set; } = 1.0;
        public double MinimumMatchScore { get; set; } = 0.70;
        public bool VerifyShapeBeforeMeasure { get; set; } = true;
        public double LastMatchScore { get; private set; } = 1.0;

        // Robust registration settings used by ALL trained modes.
        // Numeric mode values keep this file compatible with projects whose enum
        // currently only declares the original three values:
        // 0 = Centroid, 1 = Free Offset, 2 = Relative Landmark,
        // 3 = Template Registration, 4 = Smart Auto, 5 = Rotation-Invariant Caliper.
        public bool RejectAmbiguousPose { get; set; } = true;
        public bool UseTemporalPoseContinuity { get; set; } = false;
        public double PoseAmbiguityScoreGap { get; set; } = 0.015;
        public double TemporalCandidateScoreWindow { get; set; } = 0.020;

        public double LastRegistrationAngleDegrees { get; private set; } = 0.0;
        public double LastRegistrationSecondScore { get; private set; } = 0.0;
        public bool LastRegistrationAmbiguous { get; private set; } = false;
        public bool LastPoseResolvedByHistory { get; private set; } = false;
        public bool LastUsedAutoCaliperFallback { get; private set; } = false;

        private readonly Dictionary<string, double> _lastPoseAngleByShape =
            new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);

        private sealed class ShapeFingerprint
        {
            // Points are normalized independently by span1/span2. They are used to
            // rebuild the operator's trained measurement locations.
            public Point2f[] Points;

            // IsoPoints preserve the training silhouette aspect ratio and are used
            // only for pose registration. This prevents a near-square object from
            // becoming artificially identical at 0/90 degrees because of X/Y normalization.
            public Point2f[] IsoPoints;

            public double ReferenceAspectRatio = 1.0;
            public CustomSegmentationMode SegmentationMode = CustomSegmentationMode.Auto;
            public CustomPoseSolver PoseSolver = CustomPoseSolver.AutoHybrid;
            public bool RejectAmbiguousPose = true;
            public bool AllowSymmetricEquivalentMeasurements = false;

            // CSF6 embeds the measurement definition itself so runtime no longer
            // depends on database enum serialization or separate ShapeData endpoint fields.
            public bool HasMeasurementDefinition = false;
            public int MeasurementMode = 0;
            public bool SnapToEdge = true;
            public Point2f WidthPt1;
            public Point2f WidthPt2;
            public Point2f LengthPt1;
            public Point2f LengthPt2;
        }

        private sealed class PoseResult
        {
            public Point2f Center;
            public double Angle;
            public float Span1;
            public float Span2;
            public double Score;
            public double Error;
            public double SecondBestScore;
            public bool IsAmbiguous;
            public bool ResolvedByHistory;
            public int Shift;
            public bool Reversed;
            public double ScaleX;
            public double ScaleY;
            public PoseResult Alternative;
            public List<PoseResult> Alternatives;
        }

        private struct LocalBounds
        {
            public double Min1, Max1, Min2, Max2;
        }

        public string MeasureCustomShape(Mat frame, ShapeData activeShape, Mat backgroundGray = null, int thresholdValue = 100)
        {
            if (frame == null || frame.Empty()) return "Error: No Image Data";
            if (activeShape == null) return "Error: No Active Shape Selected";

            try
            {
                object regVal = new ModifyRegistry().Read("ppm");
                if (regVal != null && double.TryParse(regVal.ToString(), out double ppm) && ppm > 0)
                    PixelToMmRatio = 1.0 / ppm;
                else
                    return "Error: Please do calibration";
            }
            catch
            {
                return "Error: Please do calibration";
            }

            string shapeKey = string.IsNullOrWhiteSpace(activeShape.Name) ? "__default__" : activeShape.Name;

            ShapeFingerprint fingerprint = null;
            bool hasFingerprint = TryParseFingerprint(activeShape.ContourData, out fingerprint);
            CustomSegmentationMode segmentationMode = hasFingerprint
                ? fingerprint.SegmentationMode
                : CustomSegmentationMode.Auto;

            // CSF6 is authoritative.  This is deliberately resolved BEFORE any mode
            // decision so a database that serializes an unknown enum value as 0 cannot
            // silently turn Smart/Template/Free/Relative into Centroid mode.
            int effectiveMode = GetEffectiveModeValue(activeShape, fingerprint);
            int databaseMode = SafeModeValue(activeShape.TransformMode);

            if (!TryExtractStoneContourAdvanced(frame, backgroundGray, segmentationMode, thresholdValue,
                out OpenCvSharp.Point[] liveContour, out Mat detectedMask))
            {
                _lastPoseAngleByShape.Remove(shapeKey);
                return "Object not present";
            }
            detectedMask?.Dispose();

            LastUsedAutoCaliperFallback = false;
            LastPoseResolvedByHistory = false;
            LastRegistrationAmbiguous = false;
            LastRegistrationSecondScore = 0.0;

            // Mode 5 is deliberately orientation-independent.  Use the CSF6 mode,
            // not only activeShape.TransformMode.
            if (effectiveMode == 5)
                return MeasureAutoCaliper(frame, liveContour, "AUTO CALIPER");

            Point2f liveCenter;
            double liveAngle;
            float liveSpan1;
            float liveSpan2;
            PoseResult bestPose = null;

            if (hasFingerprint)
            {
                bestPose = FindRegisteredPoseV4(liveContour, fingerprint, shapeKey);
                LastMatchScore = bestPose.Score;
                LastRegistrationAngleDegrees = bestPose.Angle * 180.0 / Math.PI;
                LastRegistrationSecondScore = bestPose.SecondBestScore;
                LastPoseResolvedByHistory = bestPose.ResolvedByHistory;

                if (VerifyShapeBeforeMeasure && bestPose.Score < MinimumMatchScore)
                {
                    DrawingTextSettings.PutText(frame, $"Shape mismatch: {bestPose.Score:F2}",
                        new OpenCvSharp.Point(20, 35),
                        HersheyFonts.HersheySimplex, 0.8, Scalar.Red, 2, LineTypes.AntiAlias);
                    return $"Shape mismatch (score {bestPose.Score:F2})";
                }

                bool ambiguityChangesMeasurement =
                    PoseAlternativesChangeMeasurementV4(activeShape, fingerprint, liveContour, bestPose);

                LastRegistrationAmbiguous =
                    bestPose.IsAmbiguous && ambiguityChangesMeasurement && !bestPose.ResolvedByHistory;

                bool rejectAmbiguous = fingerprint.RejectAmbiguousPose && RejectAmbiguousPose;

                if (LastRegistrationAmbiguous)
                {
                    // Smart Auto (4) preserves the exact trained CSF6 lines whenever the
                    // pose is unique.  It falls back to calipers only for true ambiguity.
                    if (effectiveMode == 4)
                    {
                        LastUsedAutoCaliperFallback = true;
                        return MeasureAutoCaliper(frame, liveContour,
                            $"AUTO FALLBACK match {bestPose.Score:F2}");
                    }

                    if (rejectAmbiguous)
                    {
                        DrawingTextSettings.PutText(frame,
                            $"Orientation ambiguous {bestPose.Score:F2}/{bestPose.SecondBestScore:F2}",
                            new OpenCvSharp.Point(20, 35),
                            HersheyFonts.HersheySimplex, 0.68, Scalar.Red, 2, LineTypes.AntiAlias);

                        return $"Orientation ambiguous (best {bestPose.Score:F2}, second {bestPose.SecondBestScore:F2})";
                    }
                }

                liveCenter = bestPose.Center;
                liveAngle = bestPose.Angle;
                liveSpan1 = bestPose.Span1;
                liveSpan2 = bestPose.Span2;
            }
            else
            {
                // Compatibility path for CSF3/CSF4/records without usable registration.
                LastMatchScore = 1.0;
                GetInvariantTransform(liveContour, (ShapeTransformMode)effectiveMode,
                    out liveCenter, out liveAngle, out liveSpan1, out liveSpan2);
            }

            BuildMeasurementLinesForPose(activeShape, fingerprint, liveContour, bestPose,
                liveCenter, liveAngle, liveSpan1, liveSpan2,
                out Point2f calcW1, out Point2f calcW2,
                out Point2f calcL1, out Point2f calcL2);

            double lengthVal = calcL1.DistanceTo(calcL2) * PixelToMmRatio;
            double widthVal = calcW1.DistanceTo(calcW2) * PixelToMmRatio;

            Cv2.Line(frame, (OpenCvSharp.Point)calcW1, (OpenCvSharp.Point)calcW2,
                Scalar.Red, 2, LineTypes.AntiAlias);
            Cv2.Line(frame, (OpenCvSharp.Point)calcL1, (OpenCvSharp.Point)calcL2,
                Scalar.Blue, 2, LineTypes.AntiAlias);

            // White = live contour centroid. Yellow = final registered width midpoint.
            // If an off-centre trained line ever moves to the centroid, these two markers
            // make the error immediately visible in a saved result image.
            Cv2.Circle(frame,
                new OpenCvSharp.Point((int)Math.Round(liveCenter.X), (int)Math.Round(liveCenter.Y)),
                5, Scalar.White, 1, LineTypes.AntiAlias);

            Point2f registeredWidthMid = new Point2f(
                (calcW1.X + calcW2.X) * 0.5f,
                (calcW1.Y + calcW2.Y) * 0.5f);
            Cv2.Circle(frame, (OpenCvSharp.Point)registeredWidthMid,
                4, new Scalar(0, 255, 255), 1, LineTypes.AntiAlias);

            OpenCvSharp.Point wMid = new OpenCvSharp.Point(
                (int)Math.Round(registeredWidthMid.X),
                (int)Math.Round(registeredWidthMid.Y));
            OpenCvSharp.Point lMid = new OpenCvSharp.Point(
                (int)Math.Round((calcL1.X + calcL2.X) / 2.0),
                (int)Math.Round((calcL1.Y + calcL2.Y) / 2.0));

            DrawingTextSettings.PutText(frame, $"{widthVal:F2} mm",
                new OpenCvSharp.Point(wMid.X + 10, wMid.Y - 10),
                HersheyFonts.HersheySimplex, 0.7, Scalar.Red, 2, LineTypes.AntiAlias);
            DrawingTextSettings.PutText(frame, $"{lengthVal:F2} mm",
                new OpenCvSharp.Point(lMid.X + 10, lMid.Y + 20),
                HersheyFonts.HersheySimplex, 0.7, Scalar.Blue, 2, LineTypes.AntiAlias);

            if (hasFingerprint)
            {
                DrawingTextSettings.PutText(frame, $"Match {LastMatchScore:F2}",
                    new OpenCvSharp.Point(20, 35),
                    HersheyFonts.HersheySimplex, 0.7, Scalar.LimeGreen, 2, LineTypes.AntiAlias);

                double gap = Math.Max(0.0, LastMatchScore - LastRegistrationSecondScore);
                string poseText = $"Reg {LastRegistrationAngleDegrees:F1} deg gap {gap:F3}";
                if (LastPoseResolvedByHistory) poseText += " TRACK";

                DrawingTextSettings.PutText(frame, poseText,
                    new OpenCvSharp.Point(20, 65),
                    HersheyFonts.HersheySimplex, 0.55, Scalar.LimeGreen, 2, LineTypes.AntiAlias);

                string modeText = $"Mode {ModeName(effectiveMode)}({effectiveMode}) DB({databaseMode})";
                if (fingerprint.HasMeasurementDefinition) modeText += " CSF6";
                DrawingTextSettings.PutText(frame, modeText,
                    new OpenCvSharp.Point(20, 92),
                    HersheyFonts.HersheySimplex, 0.48, Scalar.LimeGreen, 1, LineTypes.AntiAlias);
            }

            frame.ImWrite("result.png");
            return $"Length: {lengthVal:F2} \nWidth: {widthVal:F2}";
        }

        private void BuildMeasurementLinesForPose(ShapeData activeShape, ShapeFingerprint fingerprint,
            OpenCvSharp.Point[] liveContour, PoseResult pose,
            Point2f liveCenter, double liveAngle, float liveSpan1, float liveSpan2,
            out Point2f calcW1, out Point2f calcW2, out Point2f calcL1, out Point2f calcL2)
        {
            int modeValue = GetEffectiveModeValue(activeShape, fingerprint);
            bool snapToEdge = GetEffectiveSnapToEdge(activeShape, fingerprint);

            GetStoredMeasurementLines(activeShape, fingerprint,
                out Point2f trainW1, out Point2f trainW2,
                out Point2f trainL1, out Point2f trainL2);

            if (modeValue == 2 && fingerprint != null)
            {
                // RELATIVE LANDMARK: keep the trained percentage position within the
                // silhouette, while using the robust registered orientation.
                BuildRelativeLandmarkLine(trainW1, trainW2,
                    fingerprint.Points, fingerprint.ReferenceAspectRatio,
                    liveContour, liveCenter, liveAngle, liveSpan1, liveSpan2,
                    out calcW1, out calcW2);

                BuildRelativeLandmarkLine(trainL1, trainL2,
                    fingerprint.Points, fingerprint.ReferenceAspectRatio,
                    liveContour, liveCenter, liveAngle, liveSpan1, liveSpan2,
                    out calcL1, out calcL2);
            }
            else if ((modeValue == 1 || modeValue == 3 || modeValue == 4) &&
                     fingerprint != null && pose != null)
            {
                // FREE OFFSET, TEMPLATE REGISTRATION and SMART AUTO (when pose is unique):
                // transform the EXACT trained CSF6 endpoints by the same Procrustes transform
                // that matched the contour.  This is the crucial V5/CSF6 change; no centroid
                // anchoring and no reconstruction from live bounding spans.
                calcW1 = TransformStoredNormalizedPointToLive(trainW1, fingerprint, pose);
                calcW2 = TransformStoredNormalizedPointToLive(trainW2, fingerprint, pose);
                calcL1 = TransformStoredNormalizedPointToLive(trainL1, fingerprint, pose);
                calcL2 = TransformStoredNormalizedPointToLive(trainL2, fingerprint, pose);
            }
            else
            {
                // Centroid mode and legacy fallback. For CSF6 centroid mode we still use
                // registration to recover the TRAINED directions, but the final lines are
                // intentionally anchored through the live centroid below.
                if (fingerprint != null && pose != null)
                {
                    calcW1 = TransformStoredNormalizedPointToLive(trainW1, fingerprint, pose);
                    calcW2 = TransformStoredNormalizedPointToLive(trainW2, fingerprint, pose);
                    calcL1 = TransformStoredNormalizedPointToLive(trainL1, fingerprint, pose);
                    calcL2 = TransformStoredNormalizedPointToLive(trainL2, fingerprint, pose);
                }
                else
                {
                    calcW1 = ProjectToScreen(trainW1, liveCenter, liveAngle, liveSpan1, liveSpan2);
                    calcW2 = ProjectToScreen(trainW2, liveCenter, liveAngle, liveSpan1, liveSpan2);
                    calcL1 = ProjectToScreen(trainL1, liveCenter, liveAngle, liveSpan1, liveSpan2);
                    calcL2 = ProjectToScreen(trainL2, liveCenter, liveAngle, liveSpan1, liveSpan2);
                }
            }

            if (snapToEdge)
            {
                if (modeValue == 0)
                {
                    // Only explicit CENTROID mode is ever allowed to re-anchor a line
                    // through the centroid.
                    SnapLineThroughAnchor(ref calcW1, ref calcW2, liveCenter, liveContour);
                    SnapLineThroughAnchor(ref calcL1, ref calcL2, liveCenter, liveContour);
                }
                else
                {
                    // Every off-centre mode preserves its transformed midpoint and only
                    // extends that infinite line until it intersects the real contour.
                    SnapLineToEdges(ref calcW1, ref calcW2, liveContour);
                    SnapLineToEdges(ref calcL1, ref calcL2, liveContour);
                }
            }
        }

        private static Point2f TransformStoredNormalizedPointToLive(
            Point2f normalizedPoint, ShapeFingerprint fingerprint, PoseResult pose)
        {
            if (fingerprint == null || pose == null)
                return normalizedPoint;

            // Training endpoints are normalized by training span1/span2. Registration,
            // however, was solved in isotropic coordinates. Convert the stored endpoint
            // into that same isotropic template frame before applying ScaleX/ScaleY+rotation.
            double aspect = Math.Max(0.0001, fingerprint.ReferenceAspectRatio);
            double sqrtAspect = Math.Sqrt(aspect);

            double isoX = normalizedPoint.X * sqrtAspect;
            double isoY = normalizedPoint.Y / sqrtAspect;

            // FindRegisteredPoseV4 centres the template contour by its sampled-point mean
            // before solving Procrustes. An arbitrary trained measurement point must be
            // expressed in that SAME centred frame or every off-centre line gets a fixed
            // translation error. This correction is essential for exact line placement.
            Point2f templateMean = MeanPoint(fingerprint.IsoPoints);
            isoX -= templateMean.X;
            isoY -= templateMean.Y;

            double lx = pose.ScaleX * isoX;
            double ly = pose.ScaleY * isoY;

            double ca = Math.Cos(pose.Angle);
            double sa = Math.Sin(pose.Angle);

            return new Point2f(
                (float)(pose.Center.X + lx * ca - ly * sa),
                (float)(pose.Center.Y + lx * sa + ly * ca));
        }

        private static void GetStoredMeasurementLines(ShapeData shape, ShapeFingerprint fingerprint,
            out Point2f w1, out Point2f w2, out Point2f l1, out Point2f l2)
        {
            if (fingerprint != null && fingerprint.HasMeasurementDefinition)
            {
                w1 = fingerprint.WidthPt1;
                w2 = fingerprint.WidthPt2;
                l1 = fingerprint.LengthPt1;
                l2 = fingerprint.LengthPt2;
                return;
            }

            w1 = shape.WidthPt1;
            w2 = shape.WidthPt2;
            l1 = shape.LengthPt1;
            l2 = shape.LengthPt2;
        }

        private static int GetEffectiveModeValue(ShapeData shape, ShapeFingerprint fingerprint)
        {
            if (fingerprint != null && fingerprint.HasMeasurementDefinition)
                return ClampModeValue(fingerprint.MeasurementMode);

            return ClampModeValue(SafeModeValue(shape != null ? shape.TransformMode : (ShapeTransformMode)0));
        }

        private static bool GetEffectiveSnapToEdge(ShapeData shape, ShapeFingerprint fingerprint)
        {
            if (fingerprint != null && fingerprint.HasMeasurementDefinition)
                return fingerprint.SnapToEdge;
            return shape != null && shape.SnapToEdge;
        }

        private static int SafeModeValue(ShapeTransformMode mode)
        {
            try { return Convert.ToInt32(mode); }
            catch { return 0; }
        }

        private static int ClampModeValue(int mode)
        {
            return mode < 0 || mode > 5 ? 0 : mode;
        }

        private static string ModeName(int mode)
        {
            switch (mode)
            {
                case 0: return "CENTROID";
                case 1: return "FREE";
                case 2: return "RELATIVE";
                case 3: return "TEMPLATE";
                case 4: return "SMART";
                case 5: return "CALIPER";
                default: return "UNKNOWN";
            }
        }

        // ================================================================
        // ROBUST CONTOUR EXTRACTION
        // ================================================================
        public static bool TryExtractStoneContour(Mat frame, out OpenCvSharp.Point[] contour)
        {
            bool ok = TryExtractStoneContourAdvanced(frame, null, CustomSegmentationMode.Auto, 0,
                out contour, out Mat mask);
            mask?.Dispose();
            return ok;
        }

        public static bool TryExtractStoneContour(Mat frame, Mat backgroundGray,
            CustomSegmentationMode mode, out OpenCvSharp.Point[] contour, out Mat selectedMask)
        {
            return TryExtractStoneContourAdvanced(frame, backgroundGray, mode, 0,
                out contour, out selectedMask);
        }

        private static bool TryExtractStoneContourAdvanced(Mat frame, Mat backgroundGray,
            CustomSegmentationMode mode, int thresholdValue,
            out OpenCvSharp.Point[] contour, out Mat selectedMask)
        {
            contour = null;
            selectedMask = null;
            if (frame == null || frame.Empty()) return false;

            using (Mat gray = new Mat())
            using (Mat blurred = new Mat())
            {
                if (frame.Channels() == 1) frame.CopyTo(gray);
                else Cv2.CvtColor(frame, gray, ColorConversionCodes.BGR2GRAY);

                Cv2.MedianBlur(gray, blurred, 5);
                var masks = new List<Mat>();

                try
                {
                    bool wantsBg = mode == CustomSegmentationMode.Auto ||
                                   mode == CustomSegmentationMode.BackgroundDifference;
                    bool wantsOtsu = mode == CustomSegmentationMode.Auto ||
                                     mode == CustomSegmentationMode.OtsuDarkObject;
                    bool wantsCanny = mode == CustomSegmentationMode.Auto ||
                                      mode == CustomSegmentationMode.CannyClosedContour;

                    if (wantsBg && backgroundGray != null && !backgroundGray.IsDisposed &&
                        !backgroundGray.Empty() && backgroundGray.Width == frame.Width &&
                        backgroundGray.Height == frame.Height)
                    {
                        Mat bgMask = BuildBackgroundDifferenceMask(blurred, backgroundGray, thresholdValue);
                        if (bgMask != null) masks.Add(bgMask);
                    }

                    if (wantsOtsu)
                    {
                        Mat inv = new Mat();
                        Cv2.Threshold(blurred, inv, 0, 255,
                            ThresholdTypes.BinaryInv | ThresholdTypes.Otsu);
                        CleanBinaryMask(inv);
                        masks.Add(inv);
                    }

                    if (wantsCanny)
                    {
                        Mat cannyFilled = BuildCannyFilledMask(blurred);
                        if (cannyFilled != null) masks.Add(cannyFilled);
                    }

                    double bestScore = double.MinValue;
                    OpenCvSharp.Point[] bestContour = null;
                    Mat bestMask = null;

                    foreach (Mat mask in masks)
                    {
                        if (!TryChooseBestContour(mask, frame.Width, frame.Height,
                            out OpenCvSharp.Point[] candidate, out double candidateScore))
                            continue;

                        if (candidateScore <= bestScore) continue;

                        bestScore = candidateScore;
                        bestContour = candidate;

                        bestMask?.Dispose();
                        bestMask = new Mat(frame.Height, frame.Width, MatType.CV_8UC1, Scalar.Black);
                        Cv2.DrawContours(bestMask, new[] { candidate }, -1, Scalar.White, -1, LineTypes.Link8);
                    }

                    if (bestContour == null)
                    {
                        bestMask?.Dispose();
                        return false;
                    }

                    // Re-read the contour from the clean filled component. This removes
                    // double Canny edges and small holes before pose/measurement.
                    Cv2.FindContours(bestMask, out OpenCvSharp.Point[][] cleaned, out _,
                        RetrievalModes.External, ContourApproximationModes.ApproxNone);

                    contour = cleaned
                        .Where(c => c != null && c.Length >= 20)
                        .OrderByDescending(c => Math.Abs(Cv2.ContourArea(c)))
                        .FirstOrDefault();

                    if (contour == null)
                    {
                        bestMask.Dispose();
                        return false;
                    }

                    selectedMask = bestMask;
                    return true;
                }
                finally
                {
                    foreach (Mat m in masks) m.Dispose();
                }
            }
        }

        private static Mat BuildBackgroundDifferenceMask(Mat blurredGray, Mat backgroundFrame, int thresholdValue)
        {
            Mat bg = new Mat();
            Mat bgBlur = new Mat();
            Mat diff = new Mat();
            Mat mask = new Mat();
            try
            {
                if (backgroundFrame.Channels() == 1) backgroundFrame.CopyTo(bg);
                else Cv2.CvtColor(backgroundFrame, bg, ColorConversionCodes.BGR2GRAY);

                Cv2.MedianBlur(bg, bgBlur, 5);
                Cv2.Absdiff(blurredGray, bgBlur, diff);

                if (thresholdValue > 0 && thresholdValue < 255)
                    Cv2.Threshold(diff, mask, thresholdValue, 255, ThresholdTypes.Binary);
                else
                    Cv2.Threshold(diff, mask, 0, 255, ThresholdTypes.Binary | ThresholdTypes.Otsu);

                CleanBinaryMask(mask);
                return mask;
            }
            catch
            {
                mask.Dispose();
                return null;
            }
            finally
            {
                bg.Dispose();
                bgBlur.Dispose();
                diff.Dispose();
            }
        }

        private static Mat BuildCannyFilledMask(Mat blurredGray)
        {
            Mat edges = new Mat();
            try
            {
                Cv2.Canny(blurredGray, edges, 35, 110);
                using (Mat element = Cv2.GetStructuringElement(
                    MorphShapes.Ellipse, new OpenCvSharp.Size(7, 7)))
                {
                    Cv2.MorphologyEx(edges, edges, MorphTypes.Close, element);
                }

                Cv2.FindContours(edges, out OpenCvSharp.Point[][] contours, out _,
                    RetrievalModes.External, ContourApproximationModes.ApproxNone);
                if (contours == null || contours.Length == 0) return null;

                OpenCvSharp.Point[] best = contours
                    .Where(c => c != null && c.Length >= 20)
                    .OrderByDescending(c => Math.Abs(Cv2.ContourArea(c)))
                    .FirstOrDefault();
                if (best == null) return null;

                Mat filled = new Mat(blurredGray.Height, blurredGray.Width, MatType.CV_8UC1, Scalar.Black);
                Cv2.DrawContours(filled, new[] { best }, -1, Scalar.White, -1, LineTypes.Link8);
                CleanBinaryMask(filled);
                return filled;
            }
            finally
            {
                edges.Dispose();
            }
        }

        private static bool TryChooseBestContour(Mat mask, int width, int height,
            out OpenCvSharp.Point[] best, out double bestScore)
        {
            best = null;
            bestScore = double.MinValue;

            Cv2.FindContours(mask, out OpenCvSharp.Point[][] contours, out _,
                RetrievalModes.External, ContourApproximationModes.ApproxNone);
            if (contours == null) return false;

            double frameArea = width * (double)height;

            foreach (OpenCvSharp.Point[] c in contours)
            {
                if (c == null || c.Length < 20) continue;
                double area = Math.Abs(Cv2.ContourArea(c));
                if (area < Math.Max(500.0, frameArea * 0.0008)) continue;
                if (area > frameArea * 0.80) continue;

                Rect r = Cv2.BoundingRect(c);
                bool touchesBorder = r.X <= 2 || r.Y <= 2 ||
                    r.Right >= width - 2 || r.Bottom >= height - 2;
                if (touchesBorder) continue;

                Point[] hull = Cv2.ConvexHull(c);
                double hullArea = hull != null && hull.Length >= 3
                    ? Math.Abs(Cv2.ContourArea(hull)) : area;
                double solidity = hullArea > 1e-6 ? area / hullArea : 0.0;
                solidity = Clamp01(solidity);

                double perimeter = Math.Max(1.0, Cv2.ArcLength(c, true));
                double compactness = Clamp01(4.0 * Math.PI * area / (perimeter * perimeter));

                // Area dominates because the stone is normally the largest isolated
                // object; solidity/compactness suppress dirt and broken edge fragments.
                double score = Math.Log(1.0 + area) + 0.70 * solidity + 0.20 * compactness;
                if (score > bestScore)
                {
                    bestScore = score;
                    best = c;
                }
            }

            return best != null;
        }

        private static void CleanBinaryMask(Mat mask)
        {
            using (Mat closeKernel = Cv2.GetStructuringElement(
                MorphShapes.Ellipse, new OpenCvSharp.Size(5, 5)))
            using (Mat openKernel = Cv2.GetStructuringElement(
                MorphShapes.Ellipse, new OpenCvSharp.Size(3, 3)))
            {
                Cv2.MorphologyEx(mask, mask, MorphTypes.Close, closeKernel);
                Cv2.MorphologyEx(mask, mask, MorphTypes.Open, openKernel);
            }
        }

        // ================================================================
        // FINGERPRINT / PER-SHAPE SETTINGS
        // ================================================================
        public static string BuildFingerprint(OpenCvSharp.Point[] contour, ShapeTransformMode transformMode,
            Point2f center, double angle, float span1, float span2)
        {
            return BuildFingerprint(contour, transformMode, center, angle, span1, span2,
                new CustomShapeTrainingOptions(),
                null, null, null, null, true);
        }

        public static string BuildFingerprint(OpenCvSharp.Point[] contour, ShapeTransformMode transformMode,
            Point2f center, double angle, float span1, float span2,
            CustomShapeTrainingOptions options)
        {
            return BuildFingerprint(contour, transformMode, center, angle, span1, span2,
                options, null, null, null, null, true);
        }

        // CSF6 overload used by the robust trainer. The four endpoints passed here are
        // ALREADY normalized with ProjectToLocal(). They are stored inside ContourData
        // together with the mode and SnapToEdge setting. This makes ContourData a complete,
        // self-contained measurement definition and avoids database enum/default-value bugs.
        public static string BuildFingerprint(OpenCvSharp.Point[] contour, ShapeTransformMode transformMode,
            Point2f center, double angle, float span1, float span2,
            CustomShapeTrainingOptions options,
            Point2f? normalizedWidthPt1, Point2f? normalizedWidthPt2,
            Point2f? normalizedLengthPt1, Point2f? normalizedLengthPt2,
            bool snapToEdge)
        {
            if (contour == null || contour.Length < 3) return "";
            if (options == null) options = new CustomShapeTrainingOptions();

            Point2f[] normalized = NormalizeAndResample(
                contour, center, angle, span1, span2, FingerprintPointCount);

            double uniformScale = Math.Sqrt(Math.Max(1.0, span1) * Math.Max(1.0, span2));
            Point2f[] iso = NormalizeAndResampleIsotropic(
                contour, center, angle, (float)uniformScale, FingerprintPointCount);

            double referenceAspectRatio = span2 > 1e-6f ? span1 / span2 : 1.0;
            bool hasMeasurement = normalizedWidthPt1.HasValue && normalizedWidthPt2.HasValue &&
                                  normalizedLengthPt1.HasValue && normalizedLengthPt2.HasValue;

            int modeValue = ClampModeValue(SafeModeValue(transformMode));

            return string.Join("|", new[]
            {
                FingerprintPrefix,                                                    // 0
                normalized.Length.ToString(CultureInfo.InvariantCulture),             // 1
                referenceAspectRatio.ToString("R", CultureInfo.InvariantCulture),     // 2
                ((int)options.SegmentationMode).ToString(CultureInfo.InvariantCulture),// 3
                ((int)options.PoseSolver).ToString(CultureInfo.InvariantCulture),      // 4
                options.RejectAmbiguousPose ? "1" : "0",                            // 5
                options.AllowSymmetricEquivalentMeasurements ? "1" : "0",           // 6
                modeValue.ToString(CultureInfo.InvariantCulture),                      // 7
                snapToEdge ? "1" : "0",                                              // 8
                hasMeasurement ? SerializePoint(normalizedWidthPt1.Value) : "-",      // 9
                hasMeasurement ? SerializePoint(normalizedWidthPt2.Value) : "-",      // 10
                hasMeasurement ? SerializePoint(normalizedLengthPt1.Value) : "-",     // 11
                hasMeasurement ? SerializePoint(normalizedLengthPt2.Value) : "-",     // 12
                SerializePoints(normalized),                                           // 13
                SerializePoints(iso)                                                   // 14
            });
        }

        private static string SerializePoint(Point2f point)
        {
            return point.X.ToString("R", CultureInfo.InvariantCulture) + "," +
                   point.Y.ToString("R", CultureInfo.InvariantCulture);
        }

        private static bool TryParsePoint(string text, out Point2f point)
        {
            point = new Point2f();
            if (string.IsNullOrWhiteSpace(text) || text == "-") return false;
            string[] xy = text.Split(',');
            if (xy.Length != 2) return false;
            if (!float.TryParse(xy[0], NumberStyles.Float, CultureInfo.InvariantCulture, out float x)) return false;
            if (!float.TryParse(xy[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float y)) return false;
            point = new Point2f(x, y);
            return true;
        }

        private static string SerializePoints(Point2f[] points)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < points.Length; i++)
            {
                if (i > 0) sb.Append(';');
                sb.Append(points[i].X.ToString("R", CultureInfo.InvariantCulture));
                sb.Append(',');
                sb.Append(points[i].Y.ToString("R", CultureInfo.InvariantCulture));
            }
            return sb.ToString();
        }

        private static Point2f[] ParsePoints(string block)
        {
            if (string.IsNullOrWhiteSpace(block)) return new Point2f[0];
            string[] pairs = block.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            var points = new List<Point2f>(pairs.Length);
            foreach (string pair in pairs)
            {
                string[] xy = pair.Split(',');
                if (xy.Length != 2) continue;
                if (float.TryParse(xy[0], NumberStyles.Float, CultureInfo.InvariantCulture, out float x) &&
                    float.TryParse(xy[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float y))
                    points.Add(new Point2f(x, y));
            }
            return points.ToArray();
        }

        private static bool TryParseFingerprint(string data, out ShapeFingerprint fingerprint)
        {
            fingerprint = null;
            if (string.IsNullOrWhiteSpace(data)) return false;

            string[] parts = data.Split('|');
            if (parts.Length < 3) return false;

            bool is6 = parts[0] == FingerprintPrefix;
            bool is5 = parts[0] == LegacyFingerprintPrefix5;
            bool is4 = parts[0] == LegacyFingerprintPrefix4;
            bool is3 = parts[0] == LegacyFingerprintPrefix3;
            if (!is6 && !is5 && !is4 && !is3) return false;

            double aspect = 1.0;
            Point2f[] points;
            Point2f[] iso;
            CustomSegmentationMode segmentation = CustomSegmentationMode.Auto;
            CustomPoseSolver poseSolver = CustomPoseSolver.AutoHybrid;
            bool rejectAmbiguous = true;
            bool allowEquivalent = false;

            bool hasMeasurementDefinition = false;
            int measurementMode = 0;
            bool snapToEdge = true;
            Point2f widthPt1 = new Point2f();
            Point2f widthPt2 = new Point2f();
            Point2f lengthPt1 = new Point2f();
            Point2f lengthPt2 = new Point2f();

            if (is6)
            {
                if (parts.Length < 15) return false;
                if (!double.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out aspect) ||
                    aspect <= 0.0001) aspect = 1.0;

                if (int.TryParse(parts[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out int seg))
                    segmentation = (CustomSegmentationMode)seg;
                if (int.TryParse(parts[4], NumberStyles.Integer, CultureInfo.InvariantCulture, out int solver))
                    poseSolver = (CustomPoseSolver)solver;

                rejectAmbiguous = parts[5] != "0";
                allowEquivalent = parts[6] == "1";

                if (int.TryParse(parts[7], NumberStyles.Integer, CultureInfo.InvariantCulture, out int savedMode))
                    measurementMode = ClampModeValue(savedMode);
                snapToEdge = parts[8] != "0";

                hasMeasurementDefinition =
                    TryParsePoint(parts[9], out widthPt1) &&
                    TryParsePoint(parts[10], out widthPt2) &&
                    TryParsePoint(parts[11], out lengthPt1) &&
                    TryParsePoint(parts[12], out lengthPt2);

                points = ParsePoints(parts[13]);
                iso = ParsePoints(parts[14]);
            }
            else if (is5)
            {
                if (parts.Length < 9) return false;
                if (!double.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out aspect) ||
                    aspect <= 0.0001) aspect = 1.0;

                if (int.TryParse(parts[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out int seg))
                    segmentation = (CustomSegmentationMode)seg;
                if (int.TryParse(parts[4], NumberStyles.Integer, CultureInfo.InvariantCulture, out int solver))
                    poseSolver = (CustomPoseSolver)solver;

                rejectAmbiguous = parts[5] != "0";
                allowEquivalent = parts[6] == "1";
                points = ParsePoints(parts[7]);
                iso = ParsePoints(parts[8]);
            }
            else if (is4)
            {
                if (parts.Length < 4) return false;
                if (!double.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out aspect) ||
                    aspect <= 0.0001) aspect = 1.0;
                points = ParsePoints(parts[3]);
                iso = BuildLegacyIsoPoints(points, aspect);
            }
            else
            {
                points = ParsePoints(parts[2]);
                iso = BuildLegacyIsoPoints(points, 1.0);
            }

            if (points == null || points.Length < 16) return false;
            if (iso == null || iso.Length != points.Length)
                iso = BuildLegacyIsoPoints(points, aspect);

            fingerprint = new ShapeFingerprint
            {
                Points = points,
                IsoPoints = iso,
                ReferenceAspectRatio = aspect,
                SegmentationMode = segmentation,
                PoseSolver = poseSolver,
                RejectAmbiguousPose = rejectAmbiguous,
                AllowSymmetricEquivalentMeasurements = allowEquivalent,
                HasMeasurementDefinition = hasMeasurementDefinition,
                MeasurementMode = measurementMode,
                SnapToEdge = snapToEdge,
                WidthPt1 = widthPt1,
                WidthPt2 = widthPt2,
                LengthPt1 = lengthPt1,
                LengthPt2 = lengthPt2
            };
            return true;
        }

        private static Point2f[] BuildLegacyIsoPoints(Point2f[] points, double aspect)
        {
            if (points == null) return new Point2f[0];
            double safeAspect = Math.Max(0.05, Math.Min(20.0, aspect));
            double sx = Math.Sqrt(safeAspect);
            double sy = 1.0 / Math.Sqrt(safeAspect);
            Point2f[] iso = new Point2f[points.Length];
            for (int i = 0; i < points.Length; i++)
                iso[i] = new Point2f((float)(points[i].X * sx), (float)(points[i].Y * sy));
            return iso;
        }

        private static Point2f[] NormalizeAndResample(OpenCvSharp.Point[] contour, Point2f center,
            double angle, float span1, float span2, int count)
        {
            Point2f[] local = contour.Select(p => ProjectToLocal(new Point2f(p.X, p.Y), center, angle, span1, span2)).ToArray();
            return ResampleClosed(local, count);
        }

        // Isotropic normalization keeps the stone's true aspect ratio.
        // Unlike NormalizeAndResample(), both local axes use the SAME scale.
        // This is important for robust template registration because a near-square
        // shape must not be artificially stretched into another orientation.
        private static Point2f[] NormalizeAndResampleIsotropic(OpenCvSharp.Point[] contour,
            Point2f center, double angle, float uniformScale, int count)
        {
            if (contour == null || contour.Length == 0)
                return new Point2f[0];

            double scale = Math.Max(1e-6, uniformScale);
            double cosA = Math.Cos(angle);
            double sinA = Math.Sin(angle);

            Point2f[] local = new Point2f[contour.Length];

            for (int i = 0; i < contour.Length; i++)
            {
                double dx = contour[i].X - center.X;
                double dy = contour[i].Y - center.Y;

                // Rotate screen point into the trained local coordinate frame.
                double u = dx * cosA + dy * sinA;
                double v = -dx * sinA + dy * cosA;

                local[i] = new Point2f(
                    (float)(u / scale),
                    (float)(v / scale));
            }

            return ResampleClosed(local, count);
        }

        private static Point2f[] ResampleClosed(Point2f[] input, int count)
        {
            if (input == null || input.Length == 0) return new Point2f[0];
            if (input.Length == 1) return Enumerable.Repeat(input[0], count).ToArray();

            double[] cumulative = new double[input.Length + 1];
            cumulative[0] = 0;
            for (int i = 0; i < input.Length; i++)
            {
                Point2f a = input[i];
                Point2f b = input[(i + 1) % input.Length];
                cumulative[i + 1] = cumulative[i] + a.DistanceTo(b);
            }

            double total = cumulative[input.Length];
            if (total < 1e-9) return Enumerable.Repeat(input[0], count).ToArray();

            var output = new Point2f[count];
            int seg = 0;
            for (int k = 0; k < count; k++)
            {
                double target = total * k / count;
                while (seg < input.Length - 1 && cumulative[seg + 1] < target) seg++;

                double segStart = cumulative[seg];
                double segEnd = cumulative[seg + 1];
                double t = segEnd > segStart ? (target - segStart) / (segEnd - segStart) : 0;

                Point2f a = input[seg];
                Point2f b = input[(seg + 1) % input.Length];
                output[k] = new Point2f((float)(a.X + (b.X - a.X) * t), (float)(a.Y + (b.Y - a.Y) * t));
            }
            return output;
        }

        // ================================================================
        // ROBUST POSE + MATCHING V4
        // Closed-contour cyclic correspondence + anisotropic Procrustes fit.
        // PCA and MinAreaRect are used only as optional reliable priors; they no
        // longer decide the orientation by themselves for near-symmetric shapes.
        // ================================================================
        private PoseResult FindRegisteredPoseV4(OpenCvSharp.Point[] liveContour,
            ShapeFingerprint fingerprint, string shapeKey)
        {
            Point2f[] template = fingerprint.IsoPoints != null && fingerprint.IsoPoints.Length >= 16
                ? fingerprint.IsoPoints
                : BuildLegacyIsoPoints(fingerprint.Points, fingerprint.ReferenceAspectRatio);

            int n = template.Length;
            Point2f[] live = ResampleClosed(
                liveContour.Select(p => new Point2f(p.X, p.Y)).ToArray(), n);

            Point2f templateMean = MeanPoint(template);
            Point2f liveMean = MeanPoint(live);

            Point2f[] tp = template.Select(p => new Point2f(p.X - templateMean.X, p.Y - templateMean.Y)).ToArray();
            Point2f[] lq = live.Select(p => new Point2f(p.X - liveMean.X, p.Y - liveMean.Y)).ToArray();

            double liveRadius = Math.Sqrt(Math.Max(1e-9,
                lq.Average(p => p.X * p.X + p.Y * p.Y)));

            var raw = new List<PoseResult>(n * 2);
            for (int reverse = 0; reverse <= 1; reverse++)
            {
                bool reversed = reverse == 1;
                for (int shift = 0; shift < n; shift++)
                {
                    PoseResult candidate = EvaluateProcrustesCandidate(
                        tp, lq, liveMean, liveContour, shift, reversed, liveRadius);
                    if (candidate != null) raw.Add(candidate);
                }
            }

            if (raw.Count == 0)
            {
                GetCentroidTransform(liveContour, out Point2f c, out double a, out float s1, out float s2);
                return new PoseResult { Center = c, Angle = a, Span1 = s1, Span2 = s2, Score = 0.0, Error = 1.0 };
            }

            // Collapse many neighboring cyclic shifts that describe the same physical angle.
            var candidates = new List<PoseResult>();
            foreach (PoseResult p in raw.OrderBy(x => x.Error))
            {
                if (candidates.Any(x => CircularAngleDistance(x.Angle, p.Angle) < 3.0 * Math.PI / 180.0))
                    continue;
                candidates.Add(p);
                if (candidates.Count >= 24) break;
            }

            PoseResult scoreBest = candidates[0];
            PoseResult chosen = scoreBest;

            GetPcaAngleReliability(liveContour, out double pcaAngle, out double pcaReliability);
            GetMinRectAngleReliability(liveContour, out double rectAngle, out double rectReliability);

            // Only candidates that already fit almost as well as the best contour fit
            // are allowed to be influenced by PCA/MinAreaRect. This prevents either
            // helper from rotating a near-square cushion to a wrong axis.
            List<PoseResult> nearBest = candidates
                .Where(p => p.Error <= scoreBest.Error * 1.10 + 0.0015)
                .ToList();

            if (nearBest.Count > 1)
            {
                if (fingerprint.PoseSolver == CustomPoseSolver.PcaAssisted && pcaReliability >= 0.10)
                    chosen = nearBest.OrderBy(p => UndirectedAngleDistance(p.Angle, pcaAngle)).First();
                else if (fingerprint.PoseSolver == CustomPoseSolver.MinAreaRectAssisted && rectReliability >= 0.10)
                    chosen = nearBest.OrderBy(p => UndirectedAngleDistance(p.Angle, rectAngle)).First();
                else if (fingerprint.PoseSolver == CustomPoseSolver.AutoHybrid)
                {
                    chosen = nearBest.OrderBy(p =>
                    {
                        double cost = p.Error;
                        if (pcaReliability >= 0.16)
                            cost += 0.010 * pcaReliability *
                                (UndirectedAngleDistance(p.Angle, pcaAngle) / (Math.PI / 2.0));
                        if (rectReliability >= 0.16)
                            cost += 0.007 * rectReliability *
                                (UndirectedAngleDistance(p.Angle, rectAngle) / (Math.PI / 2.0));
                        return cost;
                    }).First();
                }
            }

            bool resolvedByHistory = false;
            if (UseTemporalPoseContinuity &&
                _lastPoseAngleByShape.TryGetValue(shapeKey, out double previousAngle))
            {
                List<PoseResult> continuitySet = candidates
                    .Where(p => chosen.Score - p.Score <= TemporalCandidateScoreWindow)
                    .ToList();

                if (continuitySet.Count > 1)
                {
                    PoseResult continuityBest = continuitySet
                        .OrderBy(p => CircularAngleDistance(p.Angle, previousAngle))
                        .First();
                    chosen = continuityBest;
                    resolvedByHistory = true;
                }
            }

            List<PoseResult> alternatives = candidates
                .Where(p => !ReferenceEquals(p, chosen) &&
                            CircularAngleDistance(p.Angle, chosen.Angle) >= 10.0 * Math.PI / 180.0)
                .OrderBy(p => p.Error)
                .ToList();

            PoseResult second = alternatives.FirstOrDefault();
            chosen.SecondBestScore = second != null ? second.Score : 0.0;
            chosen.Alternative = second;
            chosen.Alternatives = alternatives;
            chosen.ResolvedByHistory = resolvedByHistory;

            chosen.IsAmbiguous = alternatives.Any(p =>
                p.Score >= MinimumMatchScore &&
                Math.Abs(chosen.Score - p.Score) < PoseAmbiguityScoreGap &&
                p.Error <= chosen.Error * 1.12 + 0.0020);

            _lastPoseAngleByShape[shapeKey] = chosen.Angle;
            return chosen;
        }

        private static PoseResult EvaluateProcrustesCandidate(
            Point2f[] templateCentered, Point2f[] liveCentered, Point2f liveMean,
            OpenCvSharp.Point[] liveContour, int shift, bool reversed, double liveRadius)
        {
            int n = Math.Min(templateCentered.Length, liveCentered.Length);
            if (n < 8) return null;

            double a = 0.0, b = 0.0;
            for (int i = 0; i < n; i++)
            {
                Point2f p = templateCentered[i];
                Point2f q = GetShiftedPoint(liveCentered, i, shift, reversed);
                a += p.X * q.X + p.Y * q.Y;
                b += p.X * q.Y - p.Y * q.X;
            }

            double angle = Math.Atan2(b, a);
            double ca = Math.Cos(angle), sa = Math.Sin(angle);

            double sumPx2 = 0.0, sumPy2 = 0.0;
            double sumX = 0.0, sumY = 0.0;

            for (int i = 0; i < n; i++)
            {
                Point2f p = templateCentered[i];
                Point2f q = GetShiftedPoint(liveCentered, i, shift, reversed);

                // Rotate the live point back into the template local frame.
                double qx = q.X * ca + q.Y * sa;
                double qy = -q.X * sa + q.Y * ca;

                sumPx2 += p.X * p.X;
                sumPy2 += p.Y * p.Y;
                sumX += p.X * qx;
                sumY += p.Y * qy;
            }

            double sx = sumPx2 > 1e-12 ? sumX / sumPx2 : 0.0;
            double sy = sumPy2 > 1e-12 ? sumY / sumPy2 : 0.0;

            if (sx < 0.0 && sy < 0.0)
            {
                sx = -sx; sy = -sy; angle += Math.PI;
                ca = Math.Cos(angle); sa = Math.Sin(angle);
            }

            if (sx <= 1e-6 || sy <= 1e-6) return null;

            double meanScale = 0.5 * (sx + sy);
            double ratioX = sx / Math.Max(1e-9, meanScale);
            double ratioY = sy / Math.Max(1e-9, meanScale);
            if (ratioX < 0.45 || ratioX > 1.80 || ratioY < 0.45 || ratioY > 1.80)
                return null;

            double sumErr2 = 0.0;
            for (int i = 0; i < n; i++)
            {
                Point2f p = templateCentered[i];
                Point2f q = GetShiftedPoint(liveCentered, i, shift, reversed);

                double lx = sx * p.X;
                double ly = sy * p.Y;
                double px = lx * ca - ly * sa;
                double py = lx * sa + ly * ca;

                double dx = px - q.X;
                double dy = py - q.Y;
                sumErr2 += dx * dx + dy * dy;
            }

            double rmsPixels = Math.Sqrt(sumErr2 / n);
            double error = rmsPixels / Math.Max(2.0, liveRadius);

            // Very large anisotropic deformation is possible for a different member
            // of the same shape family, but it should cost a little confidence.
            double deformation = Math.Abs(Math.Log(Math.Max(1e-9, sx / sy)));
            double combinedError = error + 0.012 * deformation;
            double score = Clamp01(Math.Exp(-7.0 * combinedError));

            angle = NormalizeAngle(angle);
            Point2f center = liveMean;
            ComputeSpans(liveContour, center, angle, out float span1, out float span2);

            return new PoseResult
            {
                Center = center,
                Angle = angle,
                Span1 = span1,
                Span2 = span2,
                Score = score,
                Error = combinedError,
                Shift = shift,
                Reversed = reversed,
                ScaleX = sx,
                ScaleY = sy
            };
        }

        private static Point2f GetShiftedPoint(Point2f[] points, int i, int shift, bool reversed)
        {
            int n = points.Length;
            int index = reversed ? shift - i : shift + i;
            index %= n;
            if (index < 0) index += n;
            return points[index];
        }

        private static Point2f MeanPoint(Point2f[] points)
        {
            if (points == null || points.Length == 0) return new Point2f();
            double x = 0.0, y = 0.0;
            foreach (Point2f p in points) { x += p.X; y += p.Y; }
            return new Point2f((float)(x / points.Length), (float)(y / points.Length));
        }

        private static void GetPcaAngleReliability(OpenCvSharp.Point[] contour,
            out double angle, out double reliability)
        {
            angle = 0.0; reliability = 0.0;
            if (contour == null || contour.Length < 3) return;

            Moments m = Cv2.Moments(contour);
            angle = 0.5 * Math.Atan2(2.0 * m.Mu11, m.Mu20 - m.Mu02);

            double trace = m.Mu20 + m.Mu02;
            double disc = Math.Sqrt(Math.Max(0.0,
                (m.Mu20 - m.Mu02) * (m.Mu20 - m.Mu02) + 4.0 * m.Mu11 * m.Mu11));
            double l1 = 0.5 * (trace + disc);
            double l2 = 0.5 * (trace - disc);
            reliability = (l1 + l2) > 1e-9 ? Math.Abs(l1 - l2) / (l1 + l2) : 0.0;
            angle = NormalizeAngle(angle);
        }

        private static void GetMinRectAngleReliability(OpenCvSharp.Point[] contour,
            out double angle, out double reliability)
        {
            angle = 0.0; reliability = 0.0;
            if (contour == null || contour.Length < 3) return;

            RotatedRect rr = Cv2.MinAreaRect(contour);
            Point2f[] b = rr.Points();
            Point2f e1 = new Point2f(b[1].X - b[0].X, b[1].Y - b[0].Y);
            Point2f e2 = new Point2f(b[2].X - b[1].X, b[2].Y - b[1].Y);
            double d1 = Math.Sqrt(e1.X * e1.X + e1.Y * e1.Y);
            double d2 = Math.Sqrt(e2.X * e2.X + e2.Y * e2.Y);
            Point2f e = d1 >= d2 ? e1 : e2;
            double longSide = Math.Max(d1, d2);
            double shortSide = Math.Max(1e-9, Math.Min(d1, d2));
            angle = NormalizeAngle(Math.Atan2(e.Y, e.X));
            reliability = longSide > 1e-9 ? (longSide - shortSide) / longSide : 0.0;
        }

        private static double UndirectedAngleDistance(double a, double b)
        {
            double d = CircularAngleDistance(a, b);
            return Math.Min(d, Math.Abs(Math.PI - d));
        }

        private static Point2f GetContourCentroid(OpenCvSharp.Point[] contour)
        {
            if (contour == null || contour.Length == 0)
                return new Point2f();

            Moments mu = Cv2.Moments(contour);
            if (Math.Abs(mu.M00) > double.Epsilon)
                return new Point2f(
                    (float)(mu.M10 / mu.M00),
                    (float)(mu.M01 / mu.M00));

            return new Point2f(
                (float)contour.Average(p => p.X),
                (float)contour.Average(p => p.Y));
        }

        private static double CircularAngleDistance(double a, double b)
        {
            double d = Math.Abs(NormalizeAngle(a) - NormalizeAngle(b));
            if (d > Math.PI) d = 2.0 * Math.PI - d;
            return d;
        }

        private static double BestCyclicRms(Point2f[] a, Point2f[] b)
        {
            int n = Math.Min(a.Length, b.Length);
            if (n == 0) return double.MaxValue;

            double best = double.MaxValue;
            int step = Math.Max(1, n / 64);

            for (int shift = 0; shift < n; shift += step)
            {
                double sum = 0.0;
                double sumRev = 0.0;

                for (int i = 0; i < n; i++)
                {
                    Point2f p = a[i];
                    Point2f q = b[(i + shift) % n];

                    double dx = p.X - q.X;
                    double dy = p.Y - q.Y;
                    sum += dx * dx + dy * dy;

                    int ri = shift - i;
                    while (ri < 0) ri += n;

                    Point2f qr = b[ri % n];
                    double rdx = p.X - qr.X;
                    double rdy = p.Y - qr.Y;
                    sumRev += rdx * rdx + rdy * rdy;
                }

                double rms = Math.Sqrt(Math.Min(sum, sumRev) / n);
                if (rms < best) best = rms;
            }

            return best;
        }

        private static double BestCyclicRmsExact(Point2f[] a, Point2f[] b)
        {
            int n = Math.Min(a.Length, b.Length);
            if (n == 0) return double.MaxValue;

            double best = double.MaxValue;

            for (int shift = 0; shift < n; shift++)
            {
                double sum = 0.0;
                double sumRev = 0.0;

                for (int i = 0; i < n; i++)
                {
                    Point2f p = a[i];
                    Point2f q = b[(i + shift) % n];

                    double dx = p.X - q.X;
                    double dy = p.Y - q.Y;
                    sum += dx * dx + dy * dy;

                    int ri = shift - i;
                    while (ri < 0) ri += n;

                    Point2f qr = b[ri % n];
                    double rdx = p.X - qr.X;
                    double rdy = p.Y - qr.Y;
                    sumRev += rdx * rdx + rdy * rdy;
                }

                double rms = Math.Sqrt(Math.Min(sum, sumRev) / n);
                if (rms < best) best = rms;
            }

            return best;
        }

        private bool PoseAlternativesChangeMeasurementV4(
            ShapeData shape, ShapeFingerprint fingerprint,
            OpenCvSharp.Point[] liveContour, PoseResult chosen)
        {
            if (shape == null || chosen == null ||
                chosen.Alternatives == null ||
                chosen.Alternatives.Count == 0)
                return false;

            BuildMeasurementLinesForPose(shape, fingerprint, liveContour, chosen,
                chosen.Center, chosen.Angle, chosen.Span1, chosen.Span2,
                out Point2f cw1, out Point2f cw2,
                out Point2f cl1, out Point2f cl2);

            double tolerancePixels =
                Math.Max(3.0, 0.008 * Math.Max(chosen.Span1, chosen.Span2));

            foreach (PoseResult alt in chosen.Alternatives)
            {
                if (Math.Abs(chosen.Score - alt.Score) >= PoseAmbiguityScoreGap)
                    continue;

                BuildMeasurementLinesForPose(shape, fingerprint, liveContour, alt,
                    alt.Center, alt.Angle, alt.Span1, alt.Span2,
                    out Point2f aw1, out Point2f aw2,
                    out Point2f al1, out Point2f al2);

                bool sameWidth =
                    UndirectedSegmentDifference(cw1, cw2, aw1, aw2) <= tolerancePixels;
                bool sameLength =
                    UndirectedSegmentDifference(cl1, cl2, al1, al2) <= tolerancePixels;

                if (!sameWidth || !sameLength)
                {
                    if (fingerprint != null && fingerprint.AllowSymmetricEquivalentMeasurements)
                    {
                        double cw = cw1.DistanceTo(cw2);
                        double cl = cl1.DistanceTo(cl2);
                        double aw = aw1.DistanceTo(aw2);
                        double al = al1.DistanceTo(al2);
                        double tol = Math.Max(1.5, 0.004 * Math.Max(chosen.Span1, chosen.Span2));
                        bool sameValues = Math.Abs(cw - aw) <= tol && Math.Abs(cl - al) <= tol;
                        if (sameValues) continue;
                    }
                    return true;
                }
            }

            return false;
        }

        private string MeasureAutoCaliper(Mat frame,
            OpenCvSharp.Point[] contour, string statusText)
        {
            if (contour == null || contour.Length < 3)
                return "Object not present";

            RotatedRect rr = Cv2.MinAreaRect(contour);
            Point2f[] box = rr.Points();

            Point2f e01 = new Point2f(
                box[1].X - box[0].X,
                box[1].Y - box[0].Y);
            Point2f e12 = new Point2f(
                box[2].X - box[1].X,
                box[2].Y - box[1].Y);

            double len01 = Math.Sqrt(e01.X * e01.X + e01.Y * e01.Y);
            double len12 = Math.Sqrt(e12.X * e12.X + e12.Y * e12.Y);

            Point2f dirA = len01 >= len12 ? e01 : e12;
            double dirALen = Math.Sqrt(dirA.X * dirA.X + dirA.Y * dirA.Y);
            if (dirALen < 1e-6) return "Unable to measure object";

            dirA = new Point2f(
                (float)(dirA.X / dirALen),
                (float)(dirA.Y / dirALen));

            Point2f dirB = new Point2f(-dirA.Y, dirA.X);

            Point2f anchor = GetContourCentroid(contour);
            if (Cv2.PointPolygonTest(contour, anchor, false) < 0)
                anchor = rr.Center;

            Point2f a1 = new Point2f(anchor.X - dirA.X * 2f, anchor.Y - dirA.Y * 2f);
            Point2f a2 = new Point2f(anchor.X + dirA.X * 2f, anchor.Y + dirA.Y * 2f);
            Point2f b1 = new Point2f(anchor.X - dirB.X * 2f, anchor.Y - dirB.Y * 2f);
            Point2f b2 = new Point2f(anchor.X + dirB.X * 2f, anchor.Y + dirB.Y * 2f);

            SnapLineToEdges(ref a1, ref a2, contour);
            SnapLineToEdges(ref b1, ref b2, contour);

            double dA = a1.DistanceTo(a2);
            double dB = b1.DistanceTo(b2);

            Point2f l1, l2, w1, w2;
            double lengthPixels, widthPixels;

            if (dA >= dB)
            {
                l1 = a1; l2 = a2; lengthPixels = dA;
                w1 = b1; w2 = b2; widthPixels = dB;
            }
            else
            {
                l1 = b1; l2 = b2; lengthPixels = dB;
                w1 = a1; w2 = a2; widthPixels = dA;
            }

            double lengthVal = lengthPixels * PixelToMmRatio;
            double widthVal = widthPixels * PixelToMmRatio;

            Cv2.Line(frame, (OpenCvSharp.Point)w1, (OpenCvSharp.Point)w2,
                Scalar.Red, 2, LineTypes.AntiAlias);
            Cv2.Line(frame, (OpenCvSharp.Point)l1, (OpenCvSharp.Point)l2,
                Scalar.Blue, 2, LineTypes.AntiAlias);
            Cv2.Circle(frame,
                new OpenCvSharp.Point((int)Math.Round(anchor.X), (int)Math.Round(anchor.Y)),
                5, Scalar.White, 1, LineTypes.AntiAlias);

            OpenCvSharp.Point wMid = new OpenCvSharp.Point(
                (int)Math.Round((w1.X + w2.X) / 2.0),
                (int)Math.Round((w1.Y + w2.Y) / 2.0));
            OpenCvSharp.Point lMid = new OpenCvSharp.Point(
                (int)Math.Round((l1.X + l2.X) / 2.0),
                (int)Math.Round((l1.Y + l2.Y) / 2.0));

            DrawingTextSettings.PutText(frame, $"{widthVal:F2} mm",
                new OpenCvSharp.Point(wMid.X + 10, wMid.Y - 10),
                HersheyFonts.HersheySimplex, 0.7, Scalar.Red, 2, LineTypes.AntiAlias);
            DrawingTextSettings.PutText(frame, $"{lengthVal:F2} mm",
                new OpenCvSharp.Point(lMid.X + 10, lMid.Y + 20),
                HersheyFonts.HersheySimplex, 0.7, Scalar.Blue, 2, LineTypes.AntiAlias);
            DrawingTextSettings.PutText(frame, statusText,
                new OpenCvSharp.Point(20, 35),
                HersheyFonts.HersheySimplex, 0.58, new Scalar(0, 255, 255), 2, LineTypes.AntiAlias);

            frame.ImWrite("result.png");
            return $"Length: {lengthVal:F2} \nWidth: {widthVal:F2}";
        }

        private static double UndirectedSegmentDifference(
            Point2f a1, Point2f a2, Point2f b1, Point2f b2)
        {
            double same = (a1.DistanceTo(b1) + a2.DistanceTo(b2)) * 0.5;
            double reversed = (a1.DistanceTo(b2) + a2.DistanceTo(b1)) * 0.5;
            return Math.Min(same, reversed);
        }

        // ================================================================
        // 3 TRANSFORM / MEASUREMENT BEHAVIOURS
        // ================================================================
        public static void GetInvariantTransform(OpenCvSharp.Point[] contour, ShapeTransformMode transformMode,
            out Point2f centroid, out double angle, out float span1, out float span2)
        {
            GetBaseTransform(contour, transformMode, out centroid, out angle, out span1, out span2);
        }

        private static void GetBaseTransform(OpenCvSharp.Point[] contour, ShapeTransformMode mode,
            out Point2f centroid, out double angle, out float span1, out float span2)
        {
            if (mode == ShapeTransformMode.TaperedLongestEdge)
            {
                GetStableFreeOffsetTransform(contour, out centroid, out angle, out span1, out span2);
                return;
            }

            // Centroid, RelativeLandmark and TemplateRegistration use the same training
            // coordinate frame. TemplateRegistration does NOT trust this PCA angle at runtime;
            // it searches the full contour to recover the live orientation.
            GetCentroidTransform(contour, out centroid, out angle, out span1, out span2);
        }

        private static void GetCentroidTransform(OpenCvSharp.Point[] contour,
            out Point2f centroid, out double angle, out float span1, out float span2)
        {
            if (contour == null || contour.Length < 3)
            {
                centroid = new Point2f(); angle = 0; span1 = span2 = 1; return;
            }

            Moments mu = Cv2.Moments(contour);
            centroid = Math.Abs(mu.M00) > double.Epsilon
                ? new Point2f((float)(mu.M10 / mu.M00), (float)(mu.M01 / mu.M00))
                : new Point2f((float)contour.Average(p => p.X), (float)contour.Average(p => p.Y));

            double theta = 0.5 * Math.Atan2(2.0 * mu.Mu11, mu.Mu20 - mu.Mu02);
            double dx = Math.Cos(theta), dy = Math.Sin(theta);
            if (dy > 0.001 || (Math.Abs(dy) <= 0.001 && dx < 0)) theta += Math.PI;

            angle = QuantizeQuarterDegree(NormalizeAngle(theta));
            ComputeSpans(contour, centroid, angle, out span1, out span2);
        }

        private static void GetStableFreeOffsetTransform(OpenCvSharp.Point[] contour,
            out Point2f centroid, out double angle, out float span1, out float span2)
        {
            if (contour == null || contour.Length < 3)
            {
                centroid = new Point2f();
                angle = 0;
                span1 = span2 = 1;
                return;
            }

            Moments mu = Cv2.Moments(contour);
            centroid = Math.Abs(mu.M00) > double.Epsilon
                ? new Point2f((float)(mu.M10 / mu.M00), (float)(mu.M01 / mu.M00))
                : new Point2f((float)contour.Average(p => p.X), (float)contour.Average(p => p.Y));

            // Use the whole silhouette, not one ApproxPolyDP edge.
            double theta = 0.5 * Math.Atan2(2.0 * mu.Mu11, mu.Mu20 - mu.Mu02);

            while (theta < 0.0) theta += Math.PI;
            while (theta >= Math.PI) theta -= Math.PI;

            ResolveNarrowEndDirection(contour, centroid, ref theta);

            angle = QuantizeQuarterDegree(NormalizeAngle(theta));
            ComputeSpans(contour, centroid, angle, out span1, out span2);
        }

        private static void ResolveNarrowEndDirection(OpenCvSharp.Point[] contour,
            Point2f center, ref double theta)
        {
            double ax = Math.Cos(theta), ay = Math.Sin(theta);
            double nx = -ay, ny = ax;

            double minP = double.MaxValue, maxP = double.MinValue;
            var projected = new List<Tuple<double, double>>(contour.Length);

            foreach (OpenCvSharp.Point p in contour)
            {
                double rx = p.X - center.X;
                double ry = p.Y - center.Y;
                double along = rx * ax + ry * ay;
                double across = rx * nx + ry * ny;
                projected.Add(Tuple.Create(along, across));
                if (along < minP) minP = along;
                if (along > maxP) maxP = along;
            }

            double range = maxP - minP;
            if (range < 1e-6) return;

            double band = range * 0.22;
            double negMin = double.MaxValue, negMax = double.MinValue;
            double posMin = double.MaxValue, posMax = double.MinValue;
            int negCount = 0, posCount = 0;

            foreach (var item in projected)
            {
                double along = item.Item1;
                double across = item.Item2;

                if (along <= minP + band)
                {
                    negMin = Math.Min(negMin, across);
                    negMax = Math.Max(negMax, across);
                    negCount++;
                }

                if (along >= maxP - band)
                {
                    posMin = Math.Min(posMin, across);
                    posMax = Math.Max(posMax, across);
                    posCount++;
                }
            }

            double negWidth = negCount >= 2 ? negMax - negMin : 0.0;
            double posWidth = posCount >= 2 ? posMax - posMin : 0.0;
            double maxWidth = Math.Max(negWidth, posWidth);

            bool clearlyDifferent = maxWidth > 1e-6 &&
                Math.Abs(posWidth - negWidth) > maxWidth * 0.06;

            if (clearlyDifferent)
            {
                // Convention: +axis points toward the narrower end.
                if (posWidth > negWidth)
                    theta += Math.PI;
            }
            else
            {
                double negDistance = Math.Abs(minP);
                double posDistance = Math.Abs(maxP);

                if (negDistance > posDistance * 1.03)
                {
                    theta += Math.PI;
                }
                else if (Math.Abs(negDistance - posDistance) <=
                         Math.Max(negDistance, posDistance) * 0.03)
                {
                    // Truly symmetric shapes have no geometry-only front/back.
                    double dx = Math.Cos(theta), dy = Math.Sin(theta);
                    if (dy > 0.001 || (Math.Abs(dy) <= 0.001 && dx < 0))
                        theta += Math.PI;
                }
            }

            theta = NormalizeAngle(theta);
        }

        private static void ComputeSpans(OpenCvSharp.Point[] contour, Point2f center, double angle,
            out float span1, out float span2)
        {
            LocalBounds b = GetLocalBoundsPixels(contour, center, angle);
            span1 = (float)Math.Max(1.0, b.Max1 - b.Min1);
            span2 = (float)Math.Max(1.0, b.Max2 - b.Min2);
        }

        private static LocalBounds GetLocalBoundsPixels(OpenCvSharp.Point[] contour, Point2f center, double angle)
        {
            double ax = Math.Cos(angle), ay = Math.Sin(angle);
            double nx = -ay, ny = ax;
            LocalBounds b = new LocalBounds
            {
                Min1 = double.MaxValue, Max1 = double.MinValue,
                Min2 = double.MaxValue, Max2 = double.MinValue
            };

            foreach (OpenCvSharp.Point p in contour)
            {
                double rx = p.X - center.X, ry = p.Y - center.Y;
                double p1 = rx * ax + ry * ay;
                double p2 = rx * nx + ry * ny;
                if (p1 < b.Min1) b.Min1 = p1;
                if (p1 > b.Max1) b.Max1 = p1;
                if (p2 < b.Min2) b.Min2 = p2;
                if (p2 > b.Max2) b.Max2 = p2;
            }
            return b;
        }

        // Free Offset mode scales POSITION with the live stone but preserves
        // the DIRECTION that the operator trained.
        private static void BuildFreeOffsetLine(Point2f trainA, Point2f trainB,
            double referenceAspectRatio,
            Point2f liveCenter, double liveAngle, float liveSpan1, float liveSpan2,
            out Point2f liveA, out Point2f liveB)
        {
            Point2f localMid = new Point2f(
                (trainA.X + trainB.X) * 0.5f,
                (trainA.Y + trainB.Y) * 0.5f);

            Point2f liveMid = ProjectToScreen(localMid, liveCenter, liveAngle, liveSpan1, liveSpan2);

            double du = trainB.X - trainA.X;
            double dv = trainB.Y - trainA.Y;

            double localDx = du * Math.Max(0.0001, referenceAspectRatio);
            double localDy = dv;
            double localLen = Math.Sqrt(localDx * localDx + localDy * localDy);

            if (localLen < 1e-9)
            {
                localDx = 1.0;
                localDy = 0.0;
                localLen = 1.0;
            }

            localDx /= localLen;
            localDy /= localLen;

            double ca = Math.Cos(liveAngle), sa = Math.Sin(liveAngle);
            double screenDx = localDx * ca - localDy * sa;
            double screenDy = localDx * sa + localDy * ca;

            Point2f naiveA = ProjectToScreen(trainA, liveCenter, liveAngle, liveSpan1, liveSpan2);
            Point2f naiveB = ProjectToScreen(trainB, liveCenter, liveAngle, liveSpan1, liveSpan2);
            double targetLength = Math.Max(4.0, naiveA.DistanceTo(naiveB));
            double half = targetLength * 0.5;

            liveA = new Point2f(
                (float)(liveMid.X - screenDx * half),
                (float)(liveMid.Y - screenDy * half));
            liveB = new Point2f(
                (float)(liveMid.X + screenDx * half),
                (float)(liveMid.Y + screenDy * half));
        }

        // RelativeLandmark stores ordinary normalized endpoints, but at runtime uses their
        // midpoint as a percentage inside the TRAINED contour bounds. This means an axis
        // trained near a tip/base/edge remains at the same relative level even when the live
        // stone becomes much fatter or thinner.
        private static void BuildRelativeLandmarkLine(Point2f trainA, Point2f trainB,
            Point2f[] trainingFingerprint, double referenceAspectRatio,
            OpenCvSharp.Point[] liveContour,
            Point2f liveCenter, double liveAngle, float liveSpan1, float liveSpan2,
            out Point2f liveA, out Point2f liveB)
        {
            Point2f mid = new Point2f((trainA.X + trainB.X) * 0.5f, (trainA.Y + trainB.Y) * 0.5f);
            Point2f dir = new Point2f(trainB.X - trainA.X, trainB.Y - trainA.Y);

            float minU = trainingFingerprint.Min(p => p.X);
            float maxU = trainingFingerprint.Max(p => p.X);
            float minV = trainingFingerprint.Min(p => p.Y);
            float maxV = trainingFingerprint.Max(p => p.Y);

            double fu = maxU - minU > 1e-6 ? (mid.X - minU) / (maxU - minU) : 0.5;
            double fv = maxV - minV > 1e-6 ? (mid.Y - minV) / (maxV - minV) : 0.5;
            fu = Clamp01(fu); fv = Clamp01(fv);

            LocalBounds lb = GetLocalBoundsPixels(liveContour, liveCenter, liveAngle);
            double p1 = lb.Min1 + fu * (lb.Max1 - lb.Min1);
            double p2 = lb.Min2 + fv * (lb.Max2 - lb.Min2);

            double ca = Math.Cos(liveAngle), sa = Math.Sin(liveAngle);
            Point2f liveMid = new Point2f(
                (float)(liveCenter.X + p1 * ca - p2 * sa),
                (float)(liveCenter.Y + p1 * sa + p2 * ca));

            double localDx = dir.X * Math.Max(0.0001, referenceAspectRatio);
            double localDy = dir.Y;
            double localLen = Math.Sqrt(localDx * localDx + localDy * localDy);
            if (localLen < 1e-6)
            {
                localDx = 1.0;
                localDy = 0.0;
                localLen = 1.0;
            }
            localDx /= localLen;
            localDy /= localLen;

            double vx = localDx * ca - localDy * sa;
            double vy = localDx * sa + localDy * ca;

            // Small seed line. SnapLineToEdges will extend it to the actual live contour.
            liveA = new Point2f((float)(liveMid.X - vx * 2.0), (float)(liveMid.Y - vy * 2.0));
            liveB = new Point2f((float)(liveMid.X + vx * 2.0), (float)(liveMid.Y + vy * 2.0));
        }

        // ================================================================
        // LOCAL MAPPING
        // ================================================================
        public static Point2f ProjectToLocal(Point2f pt, Point2f centroid, double angle, float span1, float span2)
        {
            double dx = pt.X - centroid.X;
            double dy = pt.Y - centroid.Y;
            double cosA = Math.Cos(angle), sinA = Math.Sin(angle);
            double p1 = dx * cosA + dy * sinA;
            double p2 = -dx * sinA + dy * cosA;
            return new Point2f((float)(p1 / Math.Max(1f, span1)), (float)(p2 / Math.Max(1f, span2)));
        }

        public static Point2f ProjectToScreen(Point2f localPt, Point2f centroid, double angle, float span1, float span2)
        {
            double p1 = localPt.X * span1;
            double p2 = localPt.Y * span2;
            double cosA = Math.Cos(angle), sinA = Math.Sin(angle);
            return new Point2f(
                (float)(centroid.X + p1 * cosA - p2 * sinA),
                (float)(centroid.Y + p1 * sinA + p2 * cosA));
        }

        // ================================================================
        // SNAP TO REAL CONTOUR - SUBPIXEL LINE/SEGMENT INTERSECTIONS
        // ================================================================
        private static void SnapLineToEdges(ref Point2f first, ref Point2f second,
            OpenCvSharp.Point[] contour)
        {
            Point2f anchor = new Point2f(
                (first.X + second.X) * 0.5f,
                (first.Y + second.Y) * 0.5f);

            Point2f direction = new Point2f(
                second.X - first.X,
                second.Y - first.Y);

            if (TryIntersectInfiniteLineWithContour(
                anchor, direction, contour,
                out Point2f p1, out Point2f p2))
            {
                first = p1;
                second = p2;
            }
        }

        private static void SnapLineThroughAnchor(ref Point2f first, ref Point2f second,
            Point2f anchor, OpenCvSharp.Point[] contour)
        {
            Point2f direction = new Point2f(
                second.X - first.X,
                second.Y - first.Y);

            if (Math.Sqrt(direction.X * direction.X + direction.Y * direction.Y) < 0.001)
            {
                direction = new Point2f(
                    second.X - anchor.X,
                    second.Y - anchor.Y);
            }

            if (TryIntersectInfiniteLineWithContour(
                anchor, direction, contour,
                out Point2f p1, out Point2f p2))
            {
                first = p1;
                second = p2;
            }
        }

        private static bool TryIntersectInfiniteLineWithContour(
            Point2f anchor, Point2f direction,
            OpenCvSharp.Point[] contour,
            out Point2f first, out Point2f second)
        {
            first = second = anchor;
            if (contour == null || contour.Length < 2) return false;

            double dx = direction.X;
            double dy = direction.Y;
            double dLen = Math.Sqrt(dx * dx + dy * dy);
            if (dLen < 1e-9) return false;

            dx /= dLen;
            dy /= dLen;

            var hits = new List<Tuple<double, Point2f>>();

            for (int i = 0; i < contour.Length; i++)
            {
                Point2f q = new Point2f(contour[i].X, contour[i].Y);
                Point2f r = new Point2f(
                    contour[(i + 1) % contour.Length].X,
                    contour[(i + 1) % contour.Length].Y);

                double sx = r.X - q.X;
                double sy = r.Y - q.Y;

                double denom = Cross(dx, dy, sx, sy);
                if (Math.Abs(denom) < 1e-10) continue;

                double qpx = q.X - anchor.X;
                double qpy = q.Y - anchor.Y;

                double t = Cross(qpx, qpy, sx, sy) / denom;
                double u = Cross(qpx, qpy, dx, dy) / denom;

                if (u < -1e-7 || u > 1.0000001) continue;

                Point2f hit = new Point2f(
                    (float)(anchor.X + t * dx),
                    (float)(anchor.Y + t * dy));

                bool duplicate = hits.Any(h =>
                    Math.Abs(h.Item1 - t) < 0.20);

                if (!duplicate)
                    hits.Add(Tuple.Create(t, hit));
            }

            if (hits.Count < 2) return false;
            hits = hits.OrderBy(h => h.Item1).ToList();

            bool anchorInside =
                Cv2.PointPolygonTest(contour, anchor, false) >= 0;

            if (anchorInside)
            {
                Tuple<double, Point2f> left = hits
                    .Where(h => h.Item1 <= 0.0)
                    .OrderByDescending(h => h.Item1)
                    .FirstOrDefault();

                Tuple<double, Point2f> right = hits
                    .Where(h => h.Item1 >= 0.0)
                    .OrderBy(h => h.Item1)
                    .FirstOrDefault();

                if (left != null && right != null)
                {
                    first = left.Item2;
                    second = right.Item2;
                    return true;
                }
            }

            double bestDistance = double.MaxValue;
            Tuple<double, Point2f> bestA = null;
            Tuple<double, Point2f> bestB = null;

            for (int i = 0; i < hits.Count - 1; i++)
            {
                double ta = hits[i].Item1;
                double tb = hits[i + 1].Item1;
                double tm = (ta + tb) * 0.5;

                Point2f mid = new Point2f(
                    (float)(anchor.X + tm * dx),
                    (float)(anchor.Y + tm * dy));

                if (Cv2.PointPolygonTest(contour, mid, false) < 0)
                    continue;

                double dist = Math.Abs(tm);
                if (dist < bestDistance)
                {
                    bestDistance = dist;
                    bestA = hits[i];
                    bestB = hits[i + 1];
                }
            }

            if (bestA != null && bestB != null)
            {
                first = bestA.Item2;
                second = bestB.Item2;
                return true;
            }

            first = hits.First().Item2;
            second = hits.Last().Item2;
            return true;
        }

        private static double Cross(double ax, double ay, double bx, double by)
        {
            return ax * by - ay * bx;
        }

        private static void SnapMeasurementAxes(ShapeTransformMode mode,
            Point2f centroid, OpenCvSharp.Point[] contour,
            ref Point2f w1, ref Point2f w2,
            ref Point2f l1, ref Point2f l2)
        {
            int modeValue = Convert.ToInt32(mode);

            if (modeValue == 0)
            {
                SnapLineThroughAnchor(ref w1, ref w2, centroid, contour);
                SnapLineThroughAnchor(ref l1, ref l2, centroid, contour);
            }
            else
            {
                SnapLineToEdges(ref w1, ref w2, contour);
                SnapLineToEdges(ref l1, ref l2, contour);
            }
        }

        private static bool IsTemplateRegistrationMode(ShapeTransformMode mode)
        {
            return Convert.ToInt32(mode) == 3;
        }

        private static bool IsSmartAutoMode(ShapeTransformMode mode)
        {
            return Convert.ToInt32(mode) == 4;
        }

        private static bool IsAutoCaliperMode(ShapeTransformMode mode)
        {
            return Convert.ToInt32(mode) == 5;
        }

        public void ResetPoseHistory()
        {
            _lastPoseAngleByShape.Clear();
        }

        private static double QuantizeQuarterDegree(double angle)
        {
            const double q = Math.PI / 720.0;
            return NormalizeAngle(Math.Round(angle / q) * q);
        }

        private static double NormalizeAngle(double angle)
        {
            while (angle < 0) angle += 2.0 * Math.PI;
            while (angle >= 2.0 * Math.PI) angle -= 2.0 * Math.PI;
            return angle;
        }

        private static double Clamp01(double v) => v < 0 ? 0 : (v > 1 ? 1 : v);
    }
}
