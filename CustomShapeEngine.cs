using System;
using System.Linq;
using OpenCvSharp;

namespace Matric_scope
{
    public class CustomShapeEngine
    {
        public double PixelToMmRatio { get; set; } = 1.0;

        /*public string MeasureCustomShape(Mat frame, ShapeData activeShape, Mat backgroundGray = null, int thresholdValue = 100)
        {
            if (activeShape == null) return "Error: No Active Shape Selected";

            try
            {
                object regVal = new ModifyRegistry().Read("ppm");
                if (regVal != null && double.TryParse(regVal.ToString(), out double ppm) && ppm > 0)
                    PixelToMmRatio = 1.0 / ppm;
            }
            catch { return "Error: Please do calibration"; }

            using (Mat gray = new Mat())
            using (Mat edges = new Mat())
            {
                Cv2.CvtColor(frame, gray, ColorConversionCodes.BGR2GRAY);
                Cv2.MedianBlur(gray, gray, 5);
                Cv2.Canny(gray, edges, 40, 120);

                using (Mat element = Cv2.GetStructuringElement(MorphShapes.Rect, new OpenCvSharp.Size(9, 9)))
                {
                    Cv2.MorphologyEx(edges, edges, MorphTypes.Close, element);
                }

                Cv2.FindContours(edges, out OpenCvSharp.Point[][] contours, out _, RetrievalModes.External, ContourApproximationModes.ApproxSimple);
                var largestContour = contours?.Where(c => Cv2.ContourArea(c) > 1200).OrderByDescending(c => Cv2.ContourArea(c)).FirstOrDefault();

                if (largestContour == null) return "Object not present";

                var liveHull = Cv2.ConvexHull(largestContour);

                // 1. Extract Bi-Axial independent dimensions from the live stone
                GetInvariantTransform(liveHull, out Point2f liveCenter, out double liveAngle, out float liveSpan1, out float liveSpan2);

                // 2. Map normalized clicks to screen using independent scaling to accommodate fat/skinny stones
                Point2f calcW1 = ProjectToScreen(activeShape.WidthPt1, liveCenter, liveAngle, liveSpan1, liveSpan2);
                Point2f calcW2 = ProjectToScreen(activeShape.WidthPt2, liveCenter, liveAngle, liveSpan1, liveSpan2);
                Point2f calcL1 = ProjectToScreen(activeShape.LengthPt1, liveCenter, liveAngle, liveSpan1, liveSpan2);
                Point2f calcL2 = ProjectToScreen(activeShape.LengthPt2, liveCenter, liveAngle, liveSpan1, liveSpan2);

                // 3. Straight Ray-Cast Snapping
                if (activeShape.SnapToEdge)
                {
                    calcW1 = SnapToEdgeStraight(liveCenter, calcW1, liveHull);
                    calcW2 = SnapToEdgeStraight(liveCenter, calcW2, liveHull);
                    calcL1 = SnapToEdgeStraight(liveCenter, calcL1, liveHull);
                    calcL2 = SnapToEdgeStraight(liveCenter, calcL2, liveHull);
                }

                // 4. Output Render
                double lengthVal = calcL1.DistanceTo(calcL2) * PixelToMmRatio;
                double widthVal = calcW1.DistanceTo(calcW2) * PixelToMmRatio;

                Cv2.Line(frame, (OpenCvSharp.Point)calcW1, (OpenCvSharp.Point)calcW2, Scalar.Red, 2);
                Cv2.Line(frame, (OpenCvSharp.Point)calcL1, (OpenCvSharp.Point)calcL2, Scalar.Blue, 2);
                Cv2.Circle(frame, new OpenCvSharp.Point((int)liveCenter.X, (int)liveCenter.Y), 4, Scalar.Green, -1);

                frame.ImWrite("CUSTOMS.png");
                return $"Length: {lengthVal:F2} \nWidth: {widthVal:F2}";
            }
        }
        */

        public string MeasureCustomShape(Mat frame, ShapeData activeShape, Mat backgroundGray = null, int thresholdValue = 100)
        {
            if (activeShape == null) return "Error: No Active Shape Selected";

            try
            {
                object regVal = new ModifyRegistry().Read("ppm");
                if (regVal != null && double.TryParse(regVal.ToString(), out double ppm) && ppm > 0)
                    PixelToMmRatio = 1.0 / ppm;
            }
            catch { return "Error: Please do calibration"; }

            using (Mat gray = new Mat())
            using (Mat edges = new Mat())
            {
                Cv2.CvtColor(frame, gray, ColorConversionCodes.BGR2GRAY);
                Cv2.MedianBlur(gray, gray, 5);
                Cv2.Canny(gray, edges, 40, 120);

                using (Mat element = Cv2.GetStructuringElement(MorphShapes.Rect, new OpenCvSharp.Size(9, 9)))
                {
                    Cv2.MorphologyEx(edges, edges, MorphTypes.Close, element);
                }

                Cv2.FindContours(edges, out OpenCvSharp.Point[][] contours, out _, RetrievalModes.External, ContourApproximationModes.ApproxSimple);
                var largestContour = contours?.Where(c => Cv2.ContourArea(c) > 1200).OrderByDescending(c => Cv2.ContourArea(c)).FirstOrDefault();

                if (largestContour == null) return "Object not present";

                var liveHull = Cv2.ConvexHull(largestContour);

                // 1. Extract Bi-Axial independent dimensions from the live stone
                GetInvariantTransform(liveHull, out Point2f liveCenter, out double liveAngle, out float liveSpan1, out float liveSpan2);

                // 2. Map normalized clicks to screen using independent scaling to accommodate fat/skinny stones
                Point2f calcW1 = ProjectToScreen(activeShape.WidthPt1, liveCenter, liveAngle, liveSpan1, liveSpan2);
                Point2f calcW2 = ProjectToScreen(activeShape.WidthPt2, liveCenter, liveAngle, liveSpan1, liveSpan2);
                Point2f calcL1 = ProjectToScreen(activeShape.LengthPt1, liveCenter, liveAngle, liveSpan1, liveSpan2);
                Point2f calcL2 = ProjectToScreen(activeShape.LengthPt2, liveCenter, liveAngle, liveSpan1, liveSpan2);

                // 3. Straight Ray-Cast Snapping
                if (activeShape.SnapToEdge)
                {
                    calcW1 = SnapToEdgeStraight(liveCenter, calcW1, liveHull);
                    calcW2 = SnapToEdgeStraight(liveCenter, calcW2, liveHull);
                    calcL1 = SnapToEdgeStraight(liveCenter, calcL1, liveHull);
                    calcL2 = SnapToEdgeStraight(liveCenter, calcL2, liveHull);
                }

                // 4. Output Render
                double lengthVal = calcL1.DistanceTo(calcL2) * PixelToMmRatio;
                double widthVal = calcW1.DistanceTo(calcW2) * PixelToMmRatio;

                try
                {
                    lengthVal = ApplyVariation(lengthVal, true);
                    widthVal = ApplyVariation(widthVal, false);
                }
                catch
                {
                }

                Cv2.Line(frame, (OpenCvSharp.Point)calcW1, (OpenCvSharp.Point)calcW2, Scalar.Red, 2);
                Cv2.Line(frame, (OpenCvSharp.Point)calcL1, (OpenCvSharp.Point)calcL2, Scalar.Blue, 2);
                Cv2.Circle(frame, new OpenCvSharp.Point((int)liveCenter.X, (int)liveCenter.Y), 4, Scalar.Green, -1);

                // ==========================================================
                // 5. ADD TEXT MEASUREMENTS TO THE IMAGE
                // ==========================================================

                // Calculate midpoints of the lines to place the text nicely
                OpenCvSharp.Point wMid = new OpenCvSharp.Point((calcW1.X + calcW2.X) / 2, (calcW1.Y + calcW2.Y) / 2);
                OpenCvSharp.Point lMid = new OpenCvSharp.Point((calcL1.X + calcL2.X) / 2, (calcL1.Y + calcL2.Y) / 2);

                // Draw Width Text (Red) slightly offset from the line
                Cv2.PutText(frame, $"{widthVal:F2} mm", new OpenCvSharp.Point(wMid.X + 10, wMid.Y - 10),
                    HersheyFonts.HersheySimplex, 0.7, Scalar.Red, 2, LineTypes.AntiAlias);

                // Draw Length Text (Blue) slightly offset from the line
                Cv2.PutText(frame, $"{lengthVal:F2} mm", new OpenCvSharp.Point(lMid.X + 10, lMid.Y + 20),
                    HersheyFonts.HersheySimplex, 0.7, Scalar.Blue, 2, LineTypes.AntiAlias);

                frame.ImWrite("CUSTOMS.png");
                return $"Length: {lengthVal:F2} \nWidth: {widthVal:F2}";
            }
        }

        private static double ApplyVariation(double rawMeasurementMM, bool isLength)
        {
            // Find which range the measurement falls into (e.g., 4.2mm falls into index 4 (4 to 5 mm))
            int rangeIndex = (int)Math.Floor(rawMeasurementMM);

            // Cap it at 24 so anything 24mm or higher uses the last box
            if (rangeIndex > 24) rangeIndex = 24;
            if (rangeIndex < 0) rangeIndex = 0;

            double variation = 0.0;
            ModifyRegistry mr = new ModifyRegistry();

            try
            {
                string regKey = isLength ? $"LenVar_{rangeIndex}" : $"WidVar_{rangeIndex}";
                string val = mr.Read(regKey);

                if (!string.IsNullOrEmpty(val))
                {
                    variation = Convert.ToDouble(val);
                }
            }
            catch { }

            // Add the variation to the original measurement
            return rawMeasurementMM + variation;
        }

        public static void GetInvariantTransform(OpenCvSharp.Point[] hull, out Point2f centroid, out double angle, out float span1, out float span2)
        {
            Moments mu = Cv2.Moments(hull);
            centroid = new Point2f((float)(mu.M10 / mu.M00), (float)(mu.M01 / mu.M00));

            double theta = 0.0;
            if (Math.Abs(mu.Mu20 - mu.Mu02) < 1e-2 && Math.Abs(mu.Mu11) < 1e-2)
            {
                double maxR = 0;
                foreach (var pt in hull)
                {
                    double r2 = Math.Pow(pt.X - centroid.X, 2) + Math.Pow(pt.Y - centroid.Y, 2);
                    if (r2 > maxR) { maxR = r2; theta = Math.Atan2(pt.Y - centroid.Y, pt.X - centroid.X); }
                }
            }
            else
            {
                theta = 0.5 * Math.Atan2(2 * mu.Mu11, mu.Mu20 - mu.Mu02);
            }

            double dx = Math.Cos(theta), dy = Math.Sin(theta);
            double nx = -dy, ny = dx;

            double maxP1 = 0, minP1 = 0, maxP2 = 0, minP2 = 0;
            foreach (var pt in hull)
            {
                double p1 = (pt.X - centroid.X) * dx + (pt.Y - centroid.Y) * dy;
                double p2 = (pt.X - centroid.X) * nx + (pt.Y - centroid.Y) * ny;
                if (p1 > maxP1) maxP1 = p1; if (p1 < minP1) minP1 = p1;
                if (p2 > maxP2) maxP2 = p2; if (p2 < minP2) minP2 = p2;
            }

            double spanP1 = maxP1 - minP1;
            double spanP2 = maxP2 - minP2;
            double asym1 = Math.Abs(Math.Abs(maxP1) - Math.Abs(minP1));
            double asym2 = Math.Abs(Math.Abs(maxP2) - Math.Abs(minP2));

            // Lock orientation robustly
            if (Math.Max(asym1, asym2) > 0.05 * Math.Sqrt(mu.M00))
            {
                if (asym2 > asym1) { theta += Math.PI / 2.0; }

                // Recalculate temp bounds for new theta to verify 180 flip
                dx = Math.Cos(theta); dy = Math.Sin(theta);
                maxP1 = 0; minP1 = 0;
                foreach (var pt in hull)
                {
                    double p1 = (pt.X - centroid.X) * dx + (pt.Y - centroid.Y) * dy;
                    if (p1 > maxP1) maxP1 = p1; if (p1 < minP1) minP1 = p1;
                }
                if (Math.Abs(minP1) > Math.Abs(maxP1)) { theta += Math.PI; }
            }
            else
            {
                if (spanP2 > spanP1) { theta += Math.PI / 2.0; }
                double finalDx = Math.Cos(theta), finalDy = Math.Sin(theta);
                if (finalDy > 0.001 || (Math.Abs(finalDy) <= 0.001 && finalDx < 0)) { theta += Math.PI; }
            }

            while (theta < 0) theta += 2 * Math.PI;
            while (theta >= 2 * Math.PI) theta -= 2 * Math.PI;
            angle = theta;

            // Finally, accurately extract the independent X and Y physical spans based on locked rotation
            dx = Math.Cos(angle); dy = Math.Sin(angle);
            nx = -dy; ny = dx;
            maxP1 = 0; minP1 = 0; maxP2 = 0; minP2 = 0;

            foreach (var pt in hull)
            {
                double p1 = (pt.X - centroid.X) * dx + (pt.Y - centroid.Y) * dy;
                double p2 = (pt.X - centroid.X) * nx + (pt.Y - centroid.Y) * ny;
                if (p1 > maxP1) maxP1 = p1; if (p1 < minP1) minP1 = p1;
                if (p2 > maxP2) maxP2 = p2; if (p2 < minP2) minP2 = p2;
            }

            span1 = (float)(maxP1 - minP1);
            span2 = (float)(maxP2 - minP2);
        }

        public static Point2f ProjectToLocal(Point2f pt, Point2f centroid, double angle, float span1, float span2)
        {
            double dx = pt.X - centroid.X;
            double dy = pt.Y - centroid.Y;
            double cosA = Math.Cos(angle);
            double sinA = Math.Sin(angle);

            double p1 = dx * cosA + dy * sinA;
            double p2 = -dx * sinA + dy * cosA;

            float u = (float)(p1 / (span1 > 0 ? span1 : 1f));
            float v = (float)(p2 / (span2 > 0 ? span2 : 1f));
            return new Point2f(u, v);
        }

        public static Point2f ProjectToScreen(Point2f localPt, Point2f centroid, double angle, float span1, float span2)
        {
            double p1 = localPt.X * span1;
            double p2 = localPt.Y * span2;

            double cosA = Math.Cos(angle);
            double sinA = Math.Sin(angle);

            float x = (float)(centroid.X + p1 * cosA - p2 * sinA);
            float y = (float)(centroid.Y + p1 * sinA + p2 * cosA);
            return new Point2f(x, y);
        }

        private Point2f SnapToEdgeStraight(Point2f center, Point2f targetPt, OpenCvSharp.Point[] contour)
        {
            float dx = targetPt.X - center.X;
            float dy = targetPt.Y - center.Y;

            float length = (float)Math.Sqrt(dx * dx + dy * dy);
            if (length == 0) return targetPt;

            float ndx = dx / length;
            float ndy = dy / length;

            Point2f lastInsidePt = center;

            for (float d = 0; d < 3000; d += 0.5f)
            {
                Point2f testPt = new Point2f(center.X + ndx * d, center.Y + ndy * d);
                double status = Cv2.PointPolygonTest(contour, testPt, false);
                if (status < 0) return lastInsidePt;
                lastInsidePt = testPt;
            }

            return targetPt;
        }
    }
}