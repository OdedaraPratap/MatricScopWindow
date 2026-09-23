using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using OpenCvSharp;

namespace Matric_scope
{
    public class CustomShapeEngine
    {
        private const string FingerprintPrefix = "CSF4";
        private const string LegacyFingerprintPrefix = "CSF3";
        private const int FingerprintPointCount = 256;

        public double PixelToMmRatio { get; set; } = 1.0;
        public double MinimumMatchScore { get; set; } = 0.70;
        public bool VerifyShapeBeforeMeasure { get; set; } = true;
        public double LastMatchScore { get; private set; } = 1.0;

        private sealed class ShapeFingerprint
        {
            public Point2f[] Points;
            public double ReferenceAspectRatio = 1.0;
        }

        private sealed class PoseResult
        {
            public Point2f Center;
            public double Angle;
            public float Span1;
            public float Span2;
            public double Score;
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

            if (!TryExtractStoneContour(frame, out OpenCvSharp.Point[] liveContour))
                return "Object not present";

            Point2f liveCenter;
            double liveAngle;
            float liveSpan1;
            float liveSpan2;
            ShapeFingerprint fingerprint = null;

            if (TryParseFingerprint(activeShape.ContourData, out fingerprint))
            {
                PoseResult bestPose = FindBestPose(liveContour, activeShape.TransformMode, fingerprint);
                LastMatchScore = bestPose.Score;

                if (VerifyShapeBeforeMeasure && bestPose.Score < MinimumMatchScore)
                {
                    DrawingTextSettings.PutText(frame, $"Shape mismatch: {bestPose.Score:F2}", new OpenCvSharp.Point(20, 35),
                        HersheyFonts.HersheySimplex, 0.8, Scalar.Red, 2, LineTypes.AntiAlias);
                    return $"Shape mismatch (score {bestPose.Score:F2})";
                }

                liveCenter = bestPose.Center;
                liveAngle = bestPose.Angle;
                liveSpan1 = bestPose.Span1;
                liveSpan2 = bestPose.Span2;
            }
            else
            {
                LastMatchScore = 1.0;
                GetInvariantTransform(liveContour, activeShape.TransformMode,
                    out liveCenter, out liveAngle, out liveSpan1, out liveSpan2);
            }

            Point2f calcW1, calcW2, calcL1, calcL2;

            if (activeShape.TransformMode == ShapeTransformMode.RelativeLandmark && fingerprint != null)
            {
                BuildRelativeLandmarkLine(activeShape.WidthPt1, activeShape.WidthPt2,
                    fingerprint.Points, fingerprint.ReferenceAspectRatio,
                    liveContour, liveCenter, liveAngle, liveSpan1, liveSpan2,
                    out calcW1, out calcW2);

                BuildRelativeLandmarkLine(activeShape.LengthPt1, activeShape.LengthPt2,
                    fingerprint.Points, fingerprint.ReferenceAspectRatio,
                    liveContour, liveCenter, liveAngle, liveSpan1, liveSpan2,
                    out calcL1, out calcL2);
            }
            else if (activeShape.TransformMode == ShapeTransformMode.TaperedLongestEdge && fingerprint != null)
            {
                BuildFreeOffsetLine(activeShape.WidthPt1, activeShape.WidthPt2,
                    fingerprint.ReferenceAspectRatio,
                    liveCenter, liveAngle, liveSpan1, liveSpan2,
                    out calcW1, out calcW2);

                BuildFreeOffsetLine(activeShape.LengthPt1, activeShape.LengthPt2,
                    fingerprint.ReferenceAspectRatio,
                    liveCenter, liveAngle, liveSpan1, liveSpan2,
                    out calcL1, out calcL2);
            }
            else
            {
                calcW1 = ProjectToScreen(activeShape.WidthPt1, liveCenter, liveAngle, liveSpan1, liveSpan2);
                calcW2 = ProjectToScreen(activeShape.WidthPt2, liveCenter, liveAngle, liveSpan1, liveSpan2);
                calcL1 = ProjectToScreen(activeShape.LengthPt1, liveCenter, liveAngle, liveSpan1, liveSpan2);
                calcL2 = ProjectToScreen(activeShape.LengthPt2, liveCenter, liveAngle, liveSpan1, liveSpan2);
            }

            if (activeShape.SnapToEdge)
            {
                SnapMeasurementAxes(activeShape.TransformMode, liveCenter, liveContour,
                    ref calcW1, ref calcW2, ref calcL1, ref calcL2);
            }

            double lengthVal = calcL1.DistanceTo(calcL2) * PixelToMmRatio;
            double widthVal = calcW1.DistanceTo(calcW2) * PixelToMmRatio;

            Cv2.Line(frame, (OpenCvSharp.Point)calcW1, (OpenCvSharp.Point)calcW2, Scalar.Red, 2, LineTypes.AntiAlias);
            Cv2.Line(frame, (OpenCvSharp.Point)calcL1, (OpenCvSharp.Point)calcL2, Scalar.Blue, 2, LineTypes.AntiAlias);
            Cv2.Circle(frame, new OpenCvSharp.Point((int)Math.Round(liveCenter.X), (int)Math.Round(liveCenter.Y)),
                5, Scalar.White, 1, LineTypes.AntiAlias);

            OpenCvSharp.Point wMid = new OpenCvSharp.Point(
                (int)Math.Round((calcW1.X + calcW2.X) / 2.0),
                (int)Math.Round((calcW1.Y + calcW2.Y) / 2.0));
            OpenCvSharp.Point lMid = new OpenCvSharp.Point(
                (int)Math.Round((calcL1.X + calcL2.X) / 2.0),
                (int)Math.Round((calcL1.Y + calcL2.Y) / 2.0));

            DrawingTextSettings.PutText(frame, $"{widthVal:F2} mm", new OpenCvSharp.Point(wMid.X + 10, wMid.Y - 10),
                HersheyFonts.HersheySimplex, 0.7, Scalar.Red, 2, LineTypes.AntiAlias);
            DrawingTextSettings.PutText(frame, $"{lengthVal:F2} mm", new OpenCvSharp.Point(lMid.X + 10, lMid.Y + 20),
                HersheyFonts.HersheySimplex, 0.7, Scalar.Blue, 2, LineTypes.AntiAlias);

            if (fingerprint != null)
            {
                DrawingTextSettings.PutText(frame, $"Match {LastMatchScore:F2}", new OpenCvSharp.Point(20, 35),
                    HersheyFonts.HersheySimplex, 0.7, Scalar.LimeGreen, 2, LineTypes.AntiAlias);
            }

            frame.ImWrite("CUSTOMS.png");
            return $"Length: {lengthVal:F2} \nWidth: {widthVal:F2}";
        }

        // ================================================================
        // CONTOUR EXTRACTION
        // ================================================================
        public static bool TryExtractStoneContour(Mat frame, out OpenCvSharp.Point[] contour)
        {
            contour = null;
            if (frame == null || frame.Empty()) return false;

            using (Mat gray = new Mat())
            using (Mat blurred = new Mat())
            using (Mat edges = new Mat())
            {
                if (frame.Channels() == 1) frame.CopyTo(gray);
                else Cv2.CvtColor(frame, gray, ColorConversionCodes.BGR2GRAY);

                Cv2.MedianBlur(gray, blurred, 5);
                Cv2.Canny(blurred, edges, 40, 120);

                using (Mat element = Cv2.GetStructuringElement(MorphShapes.Ellipse, new OpenCvSharp.Size(5, 5)))
                    Cv2.MorphologyEx(edges, edges, MorphTypes.Close, element);

                Cv2.FindContours(edges, out OpenCvSharp.Point[][] contours, out _,
                    RetrievalModes.External, ContourApproximationModes.ApproxNone);

                if (contours == null || contours.Length == 0) return false;

                OpenCvSharp.Point[] best = contours
                    .Where(c => c != null && c.Length >= 20 && Math.Abs(Cv2.ContourArea(c)) > 1200)
                    .OrderByDescending(c => Math.Abs(Cv2.ContourArea(c)))
                    .FirstOrDefault();

                if (best == null) return false;

                double perimeter = Cv2.ArcLength(best, true);
                OpenCvSharp.Point[] simplified = Cv2.ApproxPolyDP(best, Math.Max(0.5, perimeter * 0.001), true);
                contour = simplified != null && simplified.Length >= 8 ? simplified : best;
                return true;
            }
        }

        // ================================================================
        // FINGERPRINT
        // ================================================================
        public static string BuildFingerprint(OpenCvSharp.Point[] contour, ShapeTransformMode transformMode,
            Point2f center, double angle, float span1, float span2)
        {
            if (contour == null || contour.Length < 3) return "";

            Point2f[] normalized = NormalizeAndResample(contour, center, angle, span1, span2, FingerprintPointCount);
            var sb = new StringBuilder();
            double referenceAspectRatio = span2 > 1e-6f ? span1 / span2 : 1.0;

            sb.Append(FingerprintPrefix).Append('|')
              .Append(normalized.Length.ToString(CultureInfo.InvariantCulture)).Append('|')
              .Append(referenceAspectRatio.ToString("R", CultureInfo.InvariantCulture)).Append('|');

            for (int i = 0; i < normalized.Length; i++)
            {
                if (i > 0) sb.Append(';');
                sb.Append(normalized[i].X.ToString("R", CultureInfo.InvariantCulture));
                sb.Append(',');
                sb.Append(normalized[i].Y.ToString("R", CultureInfo.InvariantCulture));
            }
            return sb.ToString();
        }

        private static bool TryParseFingerprint(string data, out ShapeFingerprint fingerprint)
        {
            fingerprint = null;
            if (string.IsNullOrWhiteSpace(data)) return false;

            string[] parts = data.Split('|');
            if (parts.Length < 3) return false;

            bool isCsf4 = parts[0] == FingerprintPrefix;
            bool isCsf3 = parts[0] == LegacyFingerprintPrefix;
            if (!isCsf4 && !isCsf3) return false;

            double referenceAspectRatio = 1.0;
            string pointBlock;

            if (isCsf4)
            {
                if (parts.Length < 4) return false;
                if (!double.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture,
                    out referenceAspectRatio) || referenceAspectRatio <= 0.0001)
                    referenceAspectRatio = 1.0;
                pointBlock = parts[3];
            }
            else
            {
                pointBlock = parts[2];
            }

            string[] pairs = pointBlock.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            var points = new List<Point2f>(pairs.Length);

            foreach (string pair in pairs)
            {
                string[] xy = pair.Split(',');
                if (xy.Length != 2) continue;
                if (float.TryParse(xy[0], NumberStyles.Float, CultureInfo.InvariantCulture, out float x) &&
                    float.TryParse(xy[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float y))
                    points.Add(new Point2f(x, y));
            }

            if (points.Count < 16) return false;
            fingerprint = new ShapeFingerprint
            {
                Points = points.ToArray(),
                ReferenceAspectRatio = referenceAspectRatio
            };
            return true;
        }

        private static Point2f[] NormalizeAndResample(OpenCvSharp.Point[] contour, Point2f center,
            double angle, float span1, float span2, int count)
        {
            Point2f[] local = contour.Select(p => ProjectToLocal(new Point2f(p.X, p.Y), center, angle, span1, span2)).ToArray();
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
        // POSE + MATCHING
        // ================================================================
        private static PoseResult FindBestPose(OpenCvSharp.Point[] liveContour, ShapeTransformMode mode, ShapeFingerprint fingerprint)
        {
            GetBaseTransform(liveContour, mode, out Point2f center, out double baseAngle, out _, out _);

            // Keep the major/minor axes assigned consistently.  Testing +/- 90°
            // can swap span1/span2 and make trained axes jump to a different frame.
            double[] offsets = { 0.0, Math.PI };
            PoseResult best = null;

            foreach (double offset in offsets)
            {
                double angle = NormalizeAngle(baseAngle + offset);
                ComputeSpans(liveContour, center, angle, out float span1, out float span2);
                Point2f[] liveNorm = NormalizeAndResample(liveContour, center, angle, span1, span2, fingerprint.Points.Length);
                double rms = BestCyclicRms(fingerprint.Points, liveNorm);
                double score = Math.Exp(-4.0 * rms);

                if (best == null || score > best.Score)
                {
                    best = new PoseResult
                    {
                        Center = center,
                        Angle = angle,
                        Span1 = span1,
                        Span2 = span2,
                        Score = Clamp01(score)
                    };
                }
            }

            return best;
        }

        private static double BestCyclicRms(Point2f[] a, Point2f[] b)
        {
            int n = Math.Min(a.Length, b.Length);
            if (n == 0) return double.MaxValue;

            double best = double.MaxValue;
            int step = Math.Max(1, n / 64); // 64 start positions is enough for 256-point contour.

            for (int shift = 0; shift < n; shift += step)
            {
                double sum = 0;
                double sumRev = 0;
                for (int i = 0; i < n; i++)
                {
                    Point2f p = a[i];
                    Point2f q = b[(i + shift) % n];
                    double dx = p.X - q.X, dy = p.Y - q.Y;
                    sum += dx * dx + dy * dy;

                    int ri = shift - i;
                    while (ri < 0) ri += n;
                    Point2f qr = b[ri % n];
                    double rdx = p.X - qr.X, rdy = p.Y - qr.Y;
                    sumRev += rdx * rdx + rdy * rdy;
                }

                best = Math.Min(best, Math.Sqrt(Math.Min(sum, sumRev) / n));
            }
            return best;
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

            // Centroid and RelativeLandmark use a moment/PCA-style stable major axis.
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
        // SNAP TO REAL CONTOUR
        // ================================================================
        private static Point2f SnapToEdgeStraight(Point2f origin, Point2f targetPt, OpenCvSharp.Point[] contour)
        {
            float dx = targetPt.X - origin.X, dy = targetPt.Y - origin.Y;
            float len = (float)Math.Sqrt(dx * dx + dy * dy);
            if (len < 0.0001f) return targetPt;
            if (Cv2.PointPolygonTest(contour, origin, false) < 0) return targetPt;

            float ux = dx / len, uy = dy / len;
            float inside = 0f, outside = -1f;

            for (float d = 1f; d <= 5000f; d += 1f)
            {
                Point2f p = new Point2f(origin.X + ux * d, origin.Y + uy * d);
                if (Cv2.PointPolygonTest(contour, p, false) < 0) { outside = d; break; }
                inside = d;
            }

            if (outside < 0) return targetPt;

            for (int i = 0; i < 12; i++)
            {
                float mid = (inside + outside) * 0.5f;
                Point2f p = new Point2f(origin.X + ux * mid, origin.Y + uy * mid);
                if (Cv2.PointPolygonTest(contour, p, false) >= 0) inside = mid;
                else outside = mid;
            }

            return new Point2f(origin.X + ux * inside, origin.Y + uy * inside);
        }

        private static void SnapLineToEdges(ref Point2f first, ref Point2f second, OpenCvSharp.Point[] contour)
        {
            Point2f midpoint = new Point2f((first.X + second.X) * 0.5f, (first.Y + second.Y) * 0.5f);
            float dx = second.X - first.X, dy = second.Y - first.Y;
            float len = (float)Math.Sqrt(dx * dx + dy * dy);
            if (len < 0.001f || Cv2.PointPolygonTest(contour, midpoint, false) < 0) return;

            Point2f forward = new Point2f(midpoint.X + dx / len, midpoint.Y + dy / len);
            Point2f backward = new Point2f(midpoint.X - dx / len, midpoint.Y - dy / len);
            second = SnapToEdgeStraight(midpoint, forward, contour);
            first = SnapToEdgeStraight(midpoint, backward, contour);
        }

        private static void SnapMeasurementAxes(ShapeTransformMode mode, Point2f centroid,
            OpenCvSharp.Point[] contour, ref Point2f w1, ref Point2f w2, ref Point2f l1, ref Point2f l2)
        {
            if (mode == ShapeTransformMode.Centroid)
            {
                w1 = SnapToEdgeStraight(centroid, w1, contour);
                w2 = SnapToEdgeStraight(centroid, w2, contour);
                l1 = SnapToEdgeStraight(centroid, l1, contour);
                l2 = SnapToEdgeStraight(centroid, l2, contour);
            }
            else
            {
                SnapLineToEdges(ref w1, ref w2, contour);
                SnapLineToEdges(ref l1, ref l2, contour);
            }
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
