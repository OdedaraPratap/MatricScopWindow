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
                Cv2.Circle(frame, new OpenCvSharp.Point((int)liveCenter.X, (int)liveCenter.Y),
                    5, Scalar.White, 1, LineTypes.AntiAlias);

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

                // Custom-shape measurements are already calibrated from the
                // measured pixel distance. Do not apply the generic LenVar/WidVar
                // offsets here; those offsets changed the values even when the
                // projected lines matched the trained axes.

                Cv2.Line(frame, (OpenCvSharp.Point)calcW1, (OpenCvSharp.Point)calcW2, Scalar.Red, 2);
                Cv2.Line(frame, (OpenCvSharp.Point)calcL1, (OpenCvSharp.Point)calcL2, Scalar.Blue, 2);
                Cv2.Circle(frame, new OpenCvSharp.Point((int)liveCenter.X, (int)liveCenter.Y),
                    5, Scalar.White, 1, LineTypes.AntiAlias);

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

        public static void GetInvariantTransform(OpenCvSharp.Point[] hull, out Point2f centroid, out double angle, out float span1, out float span2)
        {
            if (hull == null || hull.Length < 2)
            {
                centroid = new Point2f();
                angle = 0.0;
                span1 = 1f;
                span2 = 1f;
                return;
            }

            Moments mu = Cv2.Moments(hull);
            if (Math.Abs(mu.M00) > double.Epsilon)
            {
                centroid = new Point2f((float)(mu.M10 / mu.M00), (float)(mu.M01 / mu.M00));
            }
            else
            {
                centroid = new Point2f((float)hull.Average(point => point.X),
                    (float)hull.Average(point => point.Y));
            }

            // Use the two most distant contour points as the primary direction.
            // For pointed custom shapes these are the two centered end points
            // selected by the operator. Moment/covariance orientation can lean
            // toward one curved side and make the projected red axis hit off-centre
            // edges, as in the supplied measured image.
            long greatestDistanceSquared = -1;
            OpenCvSharp.Point firstEnd = hull[0];
            OpenCvSharp.Point secondEnd = hull[1];
            for (int first = 0; first < hull.Length - 1; first++)
            {
                for (int second = first + 1; second < hull.Length; second++)
                {
                    long deltaX = hull[second].X - hull[first].X;
                    long deltaY = hull[second].Y - hull[first].Y;
                    long distanceSquared = deltaX * deltaX + deltaY * deltaY;
                    if (distanceSquared > greatestDistanceSquared)
                    {
                        greatestDistanceSquared = distanceSquared;
                        firstEnd = hull[first];
                        secondEnd = hull[second];
                    }
                }
            }

            double theta = Math.Atan2(secondEnd.Y - firstEnd.Y, secondEnd.X - firstEnd.X);
            // A line has no preferred forward direction. Keeping it in [0, PI)
            // prevents the saved local coordinates from flipping by 180 degrees.
            while (theta < 0) theta += Math.PI;
            while (theta >= Math.PI) theta -= Math.PI;

            // Preserve the original orientation algorithm and round only its
            // final angle to the requested quarter-degree resolution. Both the
            // trainer and live measurement use this same transform.
            const double quarterDegreeRadians = Math.PI / 720.0;
            angle = Math.Round(theta / quarterDegreeRadians) * quarterDegreeRadians;
            if (angle >= Math.PI) angle = 0.0;

            // Extract the independent physical spans in the endpoint-based frame.
            double dx = Math.Cos(angle), dy = Math.Sin(angle);
            double nx = -dy, ny = dx;
            double maxP1 = 0, minP1 = 0, maxP2 = 0, minP2 = 0;

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
