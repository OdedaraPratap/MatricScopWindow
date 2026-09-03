using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Matric_scope
{
    public class MeasurePearShape1
    {
        /*public string MeasurePearWithOpenCvSharp1(Mat src)
        {
            if (src == null || src.Empty()) return "No Shape Found";

            // Standard local unmanaged matrices
            using (Mat gray = new Mat())
            using (Mat blur = new Mat())
            using (Mat thresh = new Mat())
            {
                // ==========================================
                // 1. GRAYSCALE & BLUR
                // ==========================================
                Cv2.CvtColor(src, gray, ColorConversionCodes.BGR2GRAY);
                Cv2.GaussianBlur(gray, blur, new Size(5, 5), 0);

                // ==========================================
                // 2. THE CHIEF FIX: BINARY THRESHOLD
                // Separates the dark diamond silhouette mass from dark gray backgrounds
                // ==========================================
                Cv2.Threshold(blur, thresh, 40, 255, ThresholdTypes.BinaryInv);

                // ==========================================
                // 3. MORPH CLOSE (Cleans edges without swelling dimensions)
                // ==========================================
                using (Mat kernel = Cv2.GetStructuringElement(MorphShapes.Ellipse, new Size(5, 5)))
                {
                    Cv2.MorphologyEx(thresh, thresh, MorphTypes.Close, kernel);
                    Cv2.MorphologyEx(thresh, thresh, MorphTypes.Open, kernel); // Disappears background dust specks
                }

                // ==========================================
                // 4. FIND CONTOURS
                // ==========================================
                Point[][] contours;
                HierarchyIndex[] hierarchy;
                Cv2.FindContours(thresh, out contours, out hierarchy, RetrievalModes.External, ContourApproximationModes.ApproxSimple);

                if (contours.Length == 0)
                {
                    return "No Shape Found";
                }

                // Isolate largest diamond outline
                var largestContour = contours.OrderByDescending(c => Cv2.ContourArea(c)).First();

                // ==========================================
                // 5. NOISE RESTRICTION SHIELD
                // ==========================================
                double contourArea = Cv2.ContourArea(largestContour);
                Rect boundingBox = Cv2.BoundingRect(largestContour);
                double aspect = (double)boundingBox.Width / boundingBox.Height;

                if (contourArea < 800 || aspect < 0.22 || aspect > 4.5)
                {
                    return "No Object Detected (Noise Ignored)";
                }

                // ==========================================
                // 6. CONVEX HULL & SMOOTHING
                // ==========================================
                Point[] hull = Cv2.ConvexHull(largestContour);
                double epsilon = 0.01 * Cv2.ArcLength(hull, true);
                Point[] smoothContour = Cv2.ApproxPolyDP(hull, epsilon, true);

                // Draw green edge profile lines
                Cv2.Polylines(src, new[] { smoothContour }, true, Scalar.Lime, 2);

                // ==========================================
                // 7. FIT ROTATED RECTANGLE (MIN AREA RECT)
                // ==========================================
                RotatedRect rr = Cv2.MinAreaRect(hull);

                Point2f[] box = rr.Points();
                Point[] boxPoints = box.Select(p => new Point((int)p.X, (int)p.Y)).ToArray();

                // Draw the red bounding rectangle
                Cv2.Polylines(src, new[] { boxPoints }, true, Scalar.Red, 2);

                // ==========================================
                // 8. MEASUREMENTS & REGISTRY SCALING
                // ==========================================
                double ppm = Convert.ToDouble(new ModifyRegistry().Read("ppm"));
                if (ppm <= 0) ppm = 1.0; // Avoid potential crash divide-by-zero traps

                double widthPx = Math.Min(rr.Size.Width, rr.Size.Height);
                double lengthPx = Math.Max(rr.Size.Width, rr.Size.Height);

                double widthMM = widthPx / ppm;
                double lengthMM = lengthPx / ppm;
                double ratio = lengthMM / widthMM;

                // ==========================================
                // 9. DRAW CENTER ANCHOR DOT
                // ==========================================
                Point center = new Point((int)rr.Center.X, (int)rr.Center.Y);
                Cv2.Circle(src, center, 5, Scalar.Yellow, -1);

                // Return perfectly consistent metric string outputs
                return $"Length : {lengthMM:F2} mm\n" +
                       $"Width  : {widthMM:F2} mm\n" +
                       $"L/W Ratio : {ratio:F2}";
            } // Automatic using block scope disposal handles unmanaged memory perfectly
        }
        */

        /*public string MeasurePearWithOpenCvSharp1(Mat src)
        {
            if (src == null || src.Empty()) return "No Image Data";

            // ==========================================
            // 1. LOAD CALIBRATION SCALE
            // ==========================================
            double ppm = 1.0;
            try
            {
                ppm = Convert.ToDouble(new ModifyRegistry().Read("ppm"));
            }
            catch
            {
                return "Calibration Error";
            }
            if (ppm <= 0) ppm = 1.0;

            // Standard local unmanaged matrices inside using scopes to prevent memory leaks
            using (Mat gray = new Mat())
            using (Mat blur = new Mat())
            using (Mat thresh = new Mat())
            {
                // ==========================================
                // 2. PREPROCESSING & SEGMENTATION
                // ==========================================
                Cv2.CvtColor(src, gray, ColorConversionCodes.BGR2GRAY);
                Cv2.GaussianBlur(gray, blur, new Size(5, 5), 0);

                // Isolates the stone footprint cleanly from dark backgrounds
                Cv2.Threshold(blur, thresh, 40, 255, ThresholdTypes.BinaryInv);

                using (Mat kernel = Cv2.GetStructuringElement(MorphShapes.Ellipse, new Size(5, 5)))
                {
                    Cv2.MorphologyEx(thresh, thresh, MorphTypes.Close, kernel);
                    Cv2.MorphologyEx(thresh, thresh, MorphTypes.Open, kernel); // Dissolves background noise
                }

                // ==========================================
                // 3. FIND CONTOURS
                // ==========================================
                Point[][] contours;
                HierarchyIndex[] hierarchy;
                Cv2.FindContours(thresh, out contours, out hierarchy, RetrievalModes.External, ContourApproximationModes.ApproxSimple);

                if (contours.Length == 0) return "No Shape Found";

                // Isolate largest object silhouette mass
                var largestContour = contours.OrderByDescending(c => Cv2.ContourArea(c)).First();

                double contourArea = Cv2.ContourArea(largestContour);
                if (contourArea < 800) return "No Object Detected (Noise Ignored)";

                // ==========================================
                // 4. CONVEX HULL & POLYGON APPROXIMATION
                // ==========================================
                Point[] hull = Cv2.ConvexHull(largestContour);
                double epsilon = 0.01 * Cv2.ArcLength(hull, true);
                Point[] smoothContour = Cv2.ApproxPolyDP(hull, epsilon, true);

                // Draw green edge profile vectors
                Cv2.Polylines(src, new[] { smoothContour }, true, Scalar.Lime, 2);

                // ==========================================
                // METRIC TYPE A: MINIMUM BOUNDING BOX PARAMETERS
                // ==========================================
                RotatedRect minRect = Cv2.MinAreaRect(hull);

                double boxLengthPx = Math.Max(minRect.Size.Width, minRect.Size.Height);
                double boxWidthPx = Math.Min(minRect.Size.Width, minRect.Size.Height);

                double boxLengthMM = boxLengthPx / ppm;
                double boxWidthMM = boxWidthPx / ppm;

                // Draw the Red Bounding Box
                Point2f[] box = minRect.Points();
                Point[] boxPoints = box.Select(p => new Point((int)Math.Round(p.X), (int)Math.Round(p.Y))).ToArray();
                Cv2.Polylines(src, new[] { boxPoints }, true, Scalar.Red, 1);

                // ==========================================
                // METRIC TYPE B: VERTEX/AXIS-BASED PARAMETERS
                // ==========================================
                // Step 1: Find the absolute maximum distance between any two corners (Major Axis / Length)
                Point tip1 = hull[0];
                Point tip2 = hull[0];
                double vertexLengthPx = 0;

                for (int i = 0; i < hull.Length; i++)
                {
                    for (int j = i + 1; j < hull.Length; j++)
                    {
                        double d = Math.Sqrt(Math.Pow(hull[i].X - hull[j].X, 2) + Math.Pow(hull[i].Y - hull[j].Y, 2));
                        if (d > vertexLengthPx)
                        {
                            vertexLengthPx = d;
                            tip1 = hull[i];
                            tip2 = hull[j];
                        }
                    }
                }
                double vertexLengthMM = vertexLengthPx / ppm;

                // Draw Orange Major Axis Line
                Cv2.Line(src, tip1, tip2, Scalar.Orange, 2, LineTypes.AntiAlias);

                // Step 2: Calculate maximum perpendicular crosshair width gap relative to the line above
                double vertexWidthPx = 0;
                Point widthPoint1 = new Point(0, 0);
                Point widthPoint2 = new Point(0, 0);

                double axisX = tip2.X - tip1.X;
                double axisY = tip2.Y - tip1.Y;
                double axisLength = Math.Sqrt(axisX * axisX + axisY * axisY);

                for (int i = 0; i < hull.Length; i++)
                {
                    for (int j = i + 1; j < hull.Length; j++)
                    {
                        double sX = hull[j].X - hull[i].X;
                        double sY = hull[j].Y - hull[i].Y;

                        // Project test point vectors onto the perpendicular axis normal line
                        double crossProduct = Math.Abs(sX * axisY - sY * axisX) / (axisLength == 0 ? 1 : axisLength);

                        if (crossProduct > vertexWidthPx)
                        {
                            vertexWidthPx = crossProduct;
                            widthPoint1 = hull[i];
                            widthPoint2 = hull[j];
                        }
                    }
                }
                double vertexWidthMM = vertexWidthPx / ppm;

                // Draw Yellow Crosshair Width Line
                Cv2.Line(src, widthPoint1, widthPoint2, Scalar.Yellow, 2, LineTypes.AntiAlias);

                // ==========================================
                // 5. DRAW TEXT LABELS ON THE IMAGE
                // ==========================================
                // Center Anchor Point Marker
                Point center = new Point((int)minRect.Center.X, (int)minRect.Center.Y);
                Cv2.Circle(src, center, 4, Scalar.White, -1);

                // Text Position Coordinations (Offset locations safely away from direct intersections)
                Point pBoxText = new Point(15, 35);
                Point pVertexText = new Point(15, 85);

                // Overlay Text Blocks onto frame
                Cv2.PutText(src, $"Box L: {boxLengthMM:F2}mm | W: {boxWidthMM:F2}mm (Red Box)", pBoxText, HersheyFonts.HersheySimplex, 0.55, Scalar.Red, 1, LineTypes.AntiAlias);
                Cv2.PutText(src, $"Axis L: {vertexLengthMM:F2}mm | W: {vertexWidthMM:F2}mm (Cross)", pVertexText, HersheyFonts.HersheySimplex, 0.55, Scalar.Yellow, 1, LineTypes.AntiAlias);

                // Save to file system
                src.ImWrite("Combined_Shape_Result.png");

                // ==========================================
                // 6. BUILD CONSOLE METRIC STRING OUTPUT
                // ==========================================
                StringBuilder sb = new StringBuilder();
                sb.Append($"Box Length : {boxLengthMM:F2} mm");
                sb.Append($"Box Width  : {boxWidthMM:F2} mm");
                sb.Append($"Axis Length: {vertexLengthMM:F2} mm");
                sb.Append($"Axis Width : {vertexWidthMM:F2} mm");

                return sb.ToString();
            }
        }
        */

        public string MeasurePearWithOpenCvSharp1(Mat src)
        {
            if (src == null || src.Empty()) return "No Image Data";

            // ==========================================
            // 1. LOAD CALIBRATION SCALE
            // ==========================================
            double ppm = 1.0;
            try
            {
                ppm = Convert.ToDouble(new ModifyRegistry().Read("ppm"));
            }
            catch
            {
                return "Calibration Error";
            }
            if (ppm <= 0) ppm = 1.0;

            // Standard local unmanaged matrices inside using scopes to prevent memory leaks
            using (Mat gray = new Mat())
            using (Mat blur = new Mat())
            using (Mat thresh = new Mat())
            {
                // ==========================================
                // 2. PREPROCESSING & SEGMENTATION
                // ==========================================
                Cv2.CvtColor(src, gray, ColorConversionCodes.BGR2GRAY);
                Cv2.GaussianBlur(gray, blur, new Size(5, 5), 0);
                Cv2.Threshold(blur, thresh, 40, 255, ThresholdTypes.BinaryInv);

                using (Mat kernel = Cv2.GetStructuringElement(MorphShapes.Ellipse, new Size(5, 5)))
                {
                    Cv2.MorphologyEx(thresh, thresh, MorphTypes.Close, kernel);
                    Cv2.MorphologyEx(thresh, thresh, MorphTypes.Open, kernel);
                }

                // ==========================================
                // 3. FIND CONTOURS & FILTER NOISE
                // ==========================================
                Point[][] contours;
                HierarchyIndex[] hierarchy;
                Cv2.FindContours(thresh, out contours, out hierarchy, RetrievalModes.External, ContourApproximationModes.ApproxSimple);

                if (contours.Length == 0) return "No Shape Found";

                var largestContour = contours.OrderByDescending(c => Cv2.ContourArea(c)).First();

                double contourArea = Cv2.ContourArea(largestContour);
                if (contourArea < 800) return "No Object Detected (Noise Ignored)";

                Point[] hull = Cv2.ConvexHull(largestContour);
                double epsilon = 0.01 * Cv2.ArcLength(hull, true);
                Point[] smoothContour = Cv2.ApproxPolyDP(hull, epsilon, true);

                // Draw green edge profile vectors
                Cv2.Polylines(src, new[] { smoothContour }, true, Scalar.Lime, 2);

                // ==========================================
                // 4. MINIMUM BOUNDING BOX CALCULATIONS
                // ==========================================
                RotatedRect minRect = Cv2.MinAreaRect(hull);

                double boxLengthPx = Math.Max(minRect.Size.Width, minRect.Size.Height);
                double boxWidthPx = Math.Min(minRect.Size.Width, minRect.Size.Height);

                double boxLengthMM = boxLengthPx / ppm;
                double boxWidthMM = boxWidthPx / ppm;
                double ratio = boxLengthMM / boxWidthMM;

                // Draw the Red Bounding Box
                Point2f[] box = minRect.Points();
                Point[] boxPoints = box.Select(p => new Point((int)Math.Round(p.X), (int)Math.Round(p.Y))).ToArray();
                Cv2.Polylines(src, new[] { boxPoints }, true, Scalar.Red, 1);

                // Center Anchor Point Marker
                Point center = new Point((int)minRect.Center.X, (int)minRect.Center.Y);
                Cv2.Circle(src, center, 4, Scalar.White, -1);

                // Overlay Text Label on Frame
                Point pBoxText = new Point(15, 35);
                Cv2.PutText(src, $"Box L: {boxLengthMM:F2}mm | W: {boxWidthMM:F2}mm (Red Box)", pBoxText, HersheyFonts.HersheySimplex, 0.55, Scalar.Red, 1, LineTypes.AntiAlias);

                src.ImWrite("Box_Shape_Result.png");

                // Return form-formatted metric layout
                return $"Length : {boxLengthMM:F2} mm\n" +
                       $"Width  : {boxWidthMM:F2} mm\n" +
                       $"L/W Ratio  : {ratio:F2}";
            }
        }

        public string MeasureMarkWithOpenCvSharp(Mat src)
        {
            //Mat src = inputImage.Clone();

            // ==========================================
            // 1. GRAYSCALE
            // ==========================================

            Mat gray = new Mat();

            Cv2.CvtColor(src, gray, ColorConversionCodes.BGR2GRAY);

            // ==========================================
            // 2. BLUR
            // ==========================================

            Mat blur = new Mat();

            Cv2.GaussianBlur(gray, blur, new Size(7, 7), 0);

            // ==========================================
            // 3. CANNY EDGE DETECTION
            // ==========================================

            Mat edges = new Mat();

            Cv2.Canny(blur, edges, 30, 100);

            // ==========================================
            // 4. MORPH CLOSE
            // ==========================================

            Mat kernel = Cv2.GetStructuringElement(MorphShapes.Ellipse, new Size(3, 3));
            Cv2.MorphologyEx(edges, edges, MorphTypes.Close, kernel);

            // ==========================================
            // 5. DILATE
            // Connect broken outline
            // ==========================================

            Cv2.Dilate(edges, edges, kernel, iterations: 2);

            // ==========================================
            // 6. FIND CONTOURS
            // ==========================================

            Point[][] contours;

            HierarchyIndex[] hierarchy;

            Cv2.FindContours(edges, out contours, out hierarchy, RetrievalModes.External, ContourApproximationModes.ApproxSimple);

            if (contours.Length == 0)
            {
                return "No Shape Found";
            }

            // ==========================================
            // 7. LARGEST CONTOUR
            // ==========================================

            double maxArea = 0;

            int largestIndex = 0;

            for (int i = 0; i < contours.Length; i++)
            {
                double area = Cv2.ContourArea(contours[i]);

                if (area > maxArea)
                {
                    maxArea = area;
                    largestIndex = i;
                }
            }

            Point[] contour = contours[largestIndex];

            // ==========================================
            // 8. CONVEX HULL
            // Removes internal dents
            // ==========================================

            Point[] hull = Cv2.ConvexHull(contour);

            // ==========================================
            // 9. SMOOTH CONTOUR
            // ==========================================

            double epsilon =
                0.01 * Cv2.ArcLength(hull, true);

            Point[] smoothContour = Cv2.ApproxPolyDP(hull, epsilon, true);

            // ==========================================
            // 10. DRAW OUTER SHAPE ONLY
            // ==========================================

            Cv2.Polylines(src, new[] { smoothContour }, true, Scalar.Lime, 1);

            // Replace section 11 and 12 in your code with this exact vertex-tracking math:

            // ==========================================
            // 11. EXTRACTION OF EXTREME MARQUISE VERTICES
            // ==========================================
            // Find the absolute maximum distance between any two points in the hull to locate the true tips
            /*Point tip1 = new Point(0, 0);
            Point tip2 = new Point(0, 0);
            double maxTipDist = 0;

            for (int i = 0; i < hull.Length; i++)
            {
                for (int j = i + 1; j < hull.Length; j++)
                {
                    double d = Math.Sqrt(Math.Pow(hull[i].X - hull[j].X, 2) + Math.Pow(hull[i].Y - hull[j].Y, 2));
                    if (d > maxTipDist)
                    {
                        maxTipDist = d;
                        tip1 = hull[i];
                        tip2 = hull[j];
                    }
                }
            }

            // Draw the true length axis line directly tip-to-tip
            Cv2.Line(src, tip1, tip2, Scalar.Red, 2);

            // Find the maximum perpendicular width relative to this tip-to-tip center axis line
            double maxWidthDist = 0;
            Point widthPoint1 = new Point(0, 0);
            Point widthPoint2 = new Point(0, 0);

            // Vector line logic calculation parameters
            double axisX = tip2.X - tip1.X;
            double axisY = tip2.Y - tip1.Y;
            double axisLength = Math.Sqrt(axisX * axisX + axisY * axisY);

            for (int i = 0; i < hull.Length; i++)
            {
                for (int j = i + 1; j < hull.Length; j++)
                {
                    // Vector between two test side points
                    double sX = hull[j].X - hull[i].X;
                    double sY = hull[j].Y - hull[i].Y;
                    double currentWidth = Math.Sqrt(sX * sX + sY * sY);

                    // Project onto the perpendicular axis vector to isolate true structural width gaps
                    double crossProduct = Math.Abs(sX * axisY - sY * axisX) / axisLength;

                    if (crossProduct > maxWidthDist)
                    {
                        maxWidthDist = crossProduct;
                        widthPoint1 = hull[i];
                        widthPoint2 = hull[j];
                    }
                }
            }

            // Draw the true width axis crosshair line
            Cv2.Line(src, widthPoint1, widthPoint2, Scalar.Yellow, 2);
            */


            // ==========================================
            // 11. EXTRACTION OF EXTREME MARQUISE VERTICES (FIXED: Using smoothContour)
            // ==========================================
            // Find the absolute maximum distance between any two points in the SMOOTHED contour
            Point tip1 = new Point(0, 0);
            Point tip2 = new Point(0, 0);
            double maxTipDist = 0;

            // CHANGED: Loop over smoothContour instead of hull to ignore pixel artifacts
            for (int i = 0; i < smoothContour.Length; i++)
            {
                for (int j = i + 1; j < smoothContour.Length; j++)
                {
                    double d = Math.Sqrt(Math.Pow(smoothContour[i].X - smoothContour[j].X, 2) + Math.Pow(smoothContour[i].Y - smoothContour[j].Y, 2));
                    if (d > maxTipDist)
                    {
                        maxTipDist = d;
                        tip1 = smoothContour[i];
                        tip2 = smoothContour[j];
                    }
                }
            }

            // Draw the true length axis line directly tip-to-tip
            Cv2.Line(src, tip1, tip2, Scalar.Red, 2);

            // Find the maximum perpendicular width relative to this tip-to-tip center axis line
            double maxWidthDist = 0;
            Point widthPoint1 = new Point(0, 0);
            Point widthPoint2 = new Point(0, 0);

            // Vector line logic calculation parameters
            double axisX = tip2.X - tip1.X;
            double axisY = tip2.Y - tip1.Y;
            double axisLength = Math.Sqrt(axisX * axisX + axisY * axisY);

            // CHANGED: Loop over smoothContour here as well for width symmetry consistency
            for (int i = 0; i < smoothContour.Length; i++)
            {
                for (int j = i + 1; j < smoothContour.Length; j++)
                {
                    // Vector between two test side points
                    double sX = smoothContour[j].X - smoothContour[i].X;
                    double sY = smoothContour[j].Y - smoothContour[i].Y;

                    // Project onto the perpendicular axis vector to isolate true structural width gaps
                    double crossProduct = Math.Abs(sX * axisY - sY * axisX) / (axisLength == 0 ? 1 : axisLength);

                    if (crossProduct > maxWidthDist)
                    {
                        maxWidthDist = crossProduct;
                        widthPoint1 = smoothContour[i];
                        widthPoint2 = smoothContour[j];
                    }
                }
            }

            // Draw the true width axis crosshair line
            Cv2.Line(src, widthPoint1, widthPoint2, Scalar.Yellow, 2);


            // Render measurement values directly on screen text boxes next to the lines


            // ==========================================
            // 12. CALIBRATED CONVERSION MEASUREMENTS
            // ==========================================
            double ppm = Convert.ToDouble(new ModifyRegistry().Read("ppm"));

            double lengthMM = maxTipDist / ppm;
            double widthMM = maxWidthDist / ppm;
            double ratio = lengthMM / widthMM;

            Point lengthMidPoint = new Point((tip1.X + tip2.X) / 2 + 20, (tip1.Y + tip2.Y) / 2 - 15);
            Point widthMidPoint = new Point((widthPoint1.X + widthPoint2.X) / 2 - 80, (widthPoint1.Y + widthPoint2.Y) / 2 + 25);

            Cv2.PutText(src, $"L: {lengthMM:F2}mm", lengthMidPoint, HersheyFonts.HersheySimplex, 0.55, Scalar.Cyan, 2);
            Cv2.PutText(src, $"W: {widthMM:F2}mm", widthMidPoint, HersheyFonts.HersheySimplex, 0.55, Scalar.Yellow, 2);

            src.ImWrite("Marquise_Result.png");

            
            // ==========================================
            // 15. SHOW RESULT
            // ==========================================
            return $"Length : {lengthMM:F2} mm\n" +
                    $"Width  : {widthMM:F2} mm\n" +
                    $"L/W Ratio : {ratio:F2}";
        }

        public string MeasureHeartWithOpenCvSharp(Mat src)
        {
            if (src == null || src.Empty()) return "No Shape Found";

            using (Mat gray = new Mat())
            using (Mat blur = new Mat())
            using (Mat edges = new Mat())
            {
                Cv2.CvtColor(src, gray, ColorConversionCodes.BGR2GRAY);
                Cv2.GaussianBlur(gray, blur, new Size(7, 7), 0);
                Cv2.Canny(blur, edges, 30, 100);

                using (Mat kernel = Cv2.GetStructuringElement(MorphShapes.Ellipse, new Size(3, 3)))
                {
                    Cv2.MorphologyEx(edges, edges, MorphTypes.Close, kernel);
                    Cv2.Dilate(edges, edges, kernel, iterations: 2);
                }

                Point[][] contours;
                HierarchyIndex[] hierarchy;
                Cv2.FindContours(edges, out contours, out hierarchy, RetrievalModes.External, ContourApproximationModes.ApproxSimple);

                if (contours.Length == 0) return "No Shape Found";

                // Isolate the largest diamond contour
                var largestContour = contours.OrderByDescending(c => Cv2.ContourArea(c)).First();

                // Smooth out the boundary but KEEP the raw contour lines (No Convex Hull!)
                double epsilon = 0.008 * Cv2.ArcLength(largestContour, true);
                Point[] smoothContour = Cv2.ApproxPolyDP(largestContour, epsilon, true);

                // Draw the outer green boundary contour
                Cv2.Polylines(src, new[] { smoothContour }, true, Scalar.Lime, 2);

                // ==========================================================
                // VERTEX TRACKING MATH FOR HEART PROFILE
                // ==========================================================
                Point topLobePoint = smoothContour[0];
                Point bottomApexPoint = smoothContour[0];
                Point widthLeft = smoothContour[0];
                Point widthRight = smoothContour[0];

                foreach (Point p in smoothContour)
                {
                    // Vertical limits (Y-axis)
                    if (p.Y < topLobePoint.Y) topLobePoint = p;       // Highest lobe point (Min Y)
                    if (p.Y > bottomApexPoint.Y) bottomApexPoint = p;   // Sharp bottom point (Max Y)

                    // Horizontal limits (X-axis)
                    if (p.X < widthLeft.X) widthLeft = p;               // Furthest left boundary
                    if (p.X > widthRight.X) widthRight = p;             // Furthest right boundary
                }

                // Calculate Pixel distances
                double lengthPx = Math.Abs(bottomApexPoint.Y - topLobePoint.Y);
                double widthPx = Math.Abs(widthRight.X - widthLeft.X);

                // Convert to millimeters using calibrated PPM scale
                double ppm = Convert.ToDouble(new ModifyRegistry().Read("ppm"));
                double lengthMM = lengthPx / ppm;
                double widthMM = widthPx / ppm;
                double ratio = lengthMM / widthMM;

                // ==========================================================
                // DRAW GRAPHICS ON THE LIVE FRAME
                // ==========================================================
                // Draw the Length Axis (Red line)
                Cv2.Line(src, new Point(bottomApexPoint.X, topLobePoint.Y), bottomApexPoint, Scalar.Red, 2);

                // Draw the Width Axis (Yellow line across widest part)
                int middleY = topLobePoint.Y + (int)(lengthPx * 0.35); // Estimated widest section line placement
                Cv2.Line(src, new Point(widthLeft.X, middleY), new Point(widthRight.X, middleY), Scalar.Yellow, 2);

                // Text rendering right on the canvas frame
                Cv2.PutText(src, $"L: {lengthMM:F2}mm", new Point(bottomApexPoint.X + 15, topLobePoint.Y + 40), HersheyFonts.HersheySimplex, 0.55, Scalar.Cyan, 2);
                Cv2.PutText(src, $"W: {widthMM:F2}mm", new Point(widthLeft.X + 20, middleY - 15), HersheyFonts.HersheySimplex, 0.55, Scalar.Yellow, 2);

                return $"Length : {lengthMM:F2} mm\n" +
                       $"Width  : {widthMM:F2} mm\n" +
                       $"L/W Ratio : {ratio:F2}";
            }
        }
        
        public string MeasurePearWithOpenCvSharp(Mat src)
        {
            if (src == null || src.Empty()) return "No Shape Found";

            using (Mat gray = new Mat())
            using (Mat blur = new Mat())
            using (Mat edges = new Mat())
            {
                Cv2.CvtColor(src, gray, ColorConversionCodes.BGR2GRAY);
                Cv2.GaussianBlur(gray, blur, new Size(7, 7), 0);
                Cv2.Canny(blur, edges, 30, 100);

                using (Mat kernel = Cv2.GetStructuringElement(MorphShapes.Ellipse, new Size(3, 3)))
                {
                    Cv2.MorphologyEx(edges, edges, MorphTypes.Close, kernel);
                    Cv2.Dilate(edges, edges, kernel, iterations: 2);
                }

                Point[][] contours;
                HierarchyIndex[] hierarchy;
                Cv2.FindContours(edges, out contours, out hierarchy, RetrievalModes.External, ContourApproximationModes.ApproxSimple);

                if (contours.Length == 0) return "No Shape Found";

                var largestContour = contours.OrderByDescending(c => Cv2.ContourArea(c)).First();

                double epsilon = 0.005 * Cv2.ArcLength(largestContour, true);
                Point[] smoothContour = Cv2.ApproxPolyDP(largestContour, epsilon, true);

                Cv2.Polylines(src, new[] { smoothContour }, true, Scalar.Lime, 2);

                // Get the center of mass (Centroid) using Image Moments
                Moments mu = Cv2.Moments(smoothContour);
                Point centroid = new Point((int)(mu.M10 / mu.M00), (int)(mu.M01 / mu.M00));

                // ==========================================================
                // VECTOR PROJECTION MATH FOR THE PEAR SHAPE
                // ==========================================================
                // 1. Locate the absolute sharpest tip (the point furthest away from the center of mass)
                Point pearTip = smoothContour[0];
                double maxDistToCentroid = 0;
                foreach (Point p in smoothContour)
                {
                    double dist = Math.Sqrt(Math.Pow(p.X - centroid.X, 2) + Math.Pow(p.Y - centroid.Y, 2));
                    if (dist > maxDistToCentroid)
                    {
                        maxDistToCentroid = dist;
                        pearTip = p;
                    }
                }

                // 2. Extrapolate the length line running from the tip directly through the centroid to the base
                double dirX = centroid.X - pearTip.X;
                double dirY = centroid.Y - pearTip.Y;
                double lenVector = Math.Sqrt(dirX * dirX + dirY * dirY);
                dirX /= lenVector; // Normalize direction vectors
                dirY /= lenVector;

                // Find the base point on the opposite side of the contour along this centerline axis
                Point pearBase = centroid;
                double maxBaseProjection = 0;
                foreach (Point p in smoothContour)
                {
                    double vX = p.X - pearTip.X;
                    double vY = p.Y - pearTip.Y;
                    double projection = vX * dirX + vY * dirY;
                    if (projection > maxBaseProjection)
                    {
                        maxBaseProjection = projection;
                        pearBase = new Point(pearTip.X + (int)(dirX * projection), pearTip.Y + (int)(dirY * projection));
                    }
                }

                // 3. Find the maximum perpendicular width gap relative to this length centerline
                double maxWidthDist = 0;
                Point widthL = new Point(0, 0);
                Point widthR = new Point(0, 0);

                for (int i = 0; i < smoothContour.Length; i++)
                {
                    for (int j = i + 1; j < smoothContour.Length; j++)
                    {
                        double sX = smoothContour[j].X - smoothContour[i].X;
                        double sY = smoothContour[j].Y - smoothContour[i].Y;

                        // Cross product projection determines the clean perpendicular distance spacing
                        double perpDistance = Math.Abs(sX * dirY - sY * dirX);
                        if (perpDistance > maxWidthDist)
                        {
                            maxWidthDist = perpDistance;
                            widthL = smoothContour[i];
                            widthR = smoothContour[j];
                        }
                    }
                }

                // ==========================================================
                // CONVERSION & RENDERING
                // ==========================================================
                double ppm = Convert.ToDouble(new ModifyRegistry().Read("ppm"));
                double lengthMM = maxBaseProjection / ppm;
                double widthMM = maxWidthDist / ppm;
                double ratio = lengthMM / widthMM;

                // Draw the primary center axis line (Red)
                Cv2.Line(src, pearTip, pearBase, Scalar.Red, 2);
                // Draw the maximum cross section width line (Yellow)
                Cv2.Line(src, widthL, widthR, Scalar.Yellow, 2);
                // Draw the center anchor point dot
                Cv2.Circle(src, centroid, 5, Scalar.Orange, -1);

                // Draw measurement text next to the axis lines
                Cv2.PutText(src, $"L: {lengthMM:F2}mm", new Point(centroid.X + 25, centroid.Y - 20), HersheyFonts.HersheySimplex, 0.55, Scalar.Cyan, 2);
                Cv2.PutText(src, $"W: {widthMM:F2}mm", new Point(widthL.X + 15, widthL.Y + 25), HersheyFonts.HersheySimplex, 0.55, Scalar.Yellow, 2);

                return $"Length : {lengthMM:F2} mm\n" +
                       $"Width  : {widthMM:F2} mm\n" +
                       $"L/W Ratio : {ratio:F2}";
            }
        }

        public string MeasureShapeWithVertexAxis(Mat src)
        {
            if (src == null || src.Empty()) return "No Image Data";

            // ==========================================
            // 1. LOAD CALIBRATION SCALE
            // ==========================================
            double ppm = 1.0;
            try
            {
                ppm = Convert.ToDouble(new ModifyRegistry().Read("ppm"));
            }
            catch
            {
                return "Calibration Error";
            }
            if (ppm <= 0) ppm = 1.0;

            using (Mat gray = new Mat())
            using (Mat blur = new Mat())
            using (Mat thresh = new Mat())
            {
                // ==========================================
                // 2. PREPROCESSING (Kept for 0.01mm accuracy)
                // ==========================================
                Cv2.CvtColor(src, gray, ColorConversionCodes.BGR2GRAY);
                Cv2.GaussianBlur(gray, blur, new Size(5, 5), 0);

                Cv2.Threshold(blur, thresh, 40, 255, ThresholdTypes.BinaryInv);

                using (Mat kernel = Cv2.GetStructuringElement(MorphShapes.Ellipse, new Size(5, 5)))
                {
                    Cv2.MorphologyEx(thresh, thresh, MorphTypes.Close, kernel);
                    Cv2.MorphologyEx(thresh, thresh, MorphTypes.Open, kernel);
                }

                // ==========================================
                // 3. FIND CONTOURS & HULL
                // ==========================================
                Point[][] contours;
                HierarchyIndex[] hierarchy;
                Cv2.FindContours(thresh, out contours, out hierarchy, RetrievalModes.External, ContourApproximationModes.ApproxSimple);

                if (contours.Length == 0) return "No Shape Found";

                var largestContour = contours.OrderByDescending(c => Cv2.ContourArea(c)).First();

                // The hull wraps the outermost pixels tightly
                Point[] hull = Cv2.ConvexHull(largestContour);

                // ==========================================
                // 4. ROTATING CALIPERS MATH (MinAreaRect)
                // ==========================================
                // This single line perfectly simulates the flat jaws of a screw gauge
                RotatedRect caliperBox = Cv2.MinAreaRect(hull);

                // Extract the absolute length and width from the locked jaws
                double lengthPx = Math.Max(caliperBox.Size.Width, caliperBox.Size.Height);
                double widthPx = Math.Min(caliperBox.Size.Width, caliperBox.Size.Height);

                // Map out the 4 corners of the caliper box to draw them
                Point2f[] boxF = caliperBox.Points();
                Point[] boxPoints = new Point[4];
                for (int i = 0; i < 4; i++)
                {
                    boxPoints[i] = new Point((int)Math.Round(boxF[i].X), (int)Math.Round(boxF[i].Y));
                }

                // ==========================================
                // 5. CALIBRATED CONVERSION MEASUREMENTS
                // ==========================================
                double lengthMM = lengthPx / ppm;
                double widthMM = widthPx / ppm;
                double ratio = widthMM == 0 ? 0 : lengthMM / widthMM;

                // ==========================================
                // 6. RENDER THE VISUALIZATION
                // ==========================================
                // 1. Draw the Caliper Jaws (Yellow Box) clamped around the stone
                Cv2.Polylines(src, new[] { boxPoints }, true, Scalar.Yellow, 2, LineTypes.AntiAlias);

                // 2. Draw the Green Shape Outline inside the jaws
                double epsilon = 0.01 * Cv2.ArcLength(hull, true);
                Point[] polygon = Cv2.ApproxPolyDP(hull, epsilon, true);
                Cv2.Polylines(src, new[] { polygon }, true, Scalar.Lime, 2, LineTypes.AntiAlias);

                // 3. Overlay the Text above the box
                Point textLocation = new Point(boxPoints[1].X - 20, boxPoints[1].Y - 15);
                Cv2.PutText(src, $"L: {lengthMM:F2}mm | W: {widthMM:F2}mm", textLocation, HersheyFonts.HersheySimplex, 0.55, Scalar.Cyan, 2, LineTypes.AntiAlias);

                src.ImWrite("Marquise_Result.png");

                return $"Length : {lengthMM:F2} mm\n" +
                       $"Width  : {widthMM:F2} mm\n" +
                       $"L/W Ratio : {ratio:F2}";
            }
        }

        public string MeasureShapeWithVertexAxisoo(Mat src)
        {
            if (src == null || src.Empty()) return "No Image Data";

            // ==========================================
            // 1. LOAD CALIBRATION SCALE
            // ==========================================
            double ppm = 1.0;
            try
            {
                ppm = Convert.ToDouble(new ModifyRegistry().Read("ppm"));
            }
            catch
            {
                return "Calibration Error";
            }
            if (ppm <= 0) ppm = 1.0;

            // Standard local unmanaged matrices inside using scopes to prevent memory leaks
            using (Mat gray = new Mat())
            using (Mat blur = new Mat())
            using (Mat edges = new Mat())
            {
                // ==========================================
                // 2. PREPROCESSING (Restored to your highly accurate original method)
                // ==========================================
                Cv2.CvtColor(src, gray, ColorConversionCodes.BGR2GRAY);
                Cv2.GaussianBlur(gray, blur, new Size(7, 7), 0);

                // Canny successfully catches the faint, semi-transparent tips of the stones
                Cv2.Canny(blur, edges, 30, 100);

                using (Mat kernel = Cv2.GetStructuringElement(MorphShapes.Ellipse, new Size(3, 3)))
                {
                    Cv2.MorphologyEx(edges, edges, MorphTypes.Close, kernel);

                    // Dilation restores the edge thickness to match your calibration
                    Cv2.Dilate(edges, edges, kernel, iterations: 2);
                }

                // ==========================================
                // 3. FIND CONTOURS & POLYGON APPROXIMATION
                // ==========================================
                Point[][] contours;
                HierarchyIndex[] hierarchy;
                Cv2.FindContours(edges, out contours, out hierarchy, RetrievalModes.External, ContourApproximationModes.ApproxSimple);

                if (contours.Length == 0) return "No Shape Found";

                var largestContour = contours.OrderByDescending(c => Cv2.ContourArea(c)).First();
                Point[] hull = Cv2.ConvexHull(largestContour);

                // Using your exact 0.01 epsilon multiplier
                double epsilon = 0.01 * Cv2.ArcLength(hull, true);
                Point[] polygon = Cv2.ApproxPolyDP(hull, epsilon, true);

                if (polygon.Length < 3) return "Invalid Polygon Corners";

                // Draw green edge profile vectors
                Cv2.Polylines(src, new[] { polygon }, true, Scalar.Lime, 2);

                // ==========================================
                // 4. EXACT VERTEX MATH (Orthogonal Vector Projection)
                // ==========================================
                // Step A: Find the absolute maximum distance between any two corners (Length)
                Point tip1 = new Point(0, 0);
                Point tip2 = new Point(0, 0);
                double maxTipDist = 0;

                for (int i = 0; i < polygon.Length; i++)
                {
                    for (int j = i + 1; j < polygon.Length; j++)
                    {
                        double d = Math.Sqrt(Math.Pow(polygon[i].X - polygon[j].X, 2) + Math.Pow(polygon[i].Y - polygon[j].Y, 2));
                        if (d > maxTipDist)
                        {
                            maxTipDist = d;
                            tip1 = polygon[i];
                            tip2 = polygon[j];
                        }
                    }
                }

                // Draw Orange Major Axis Line
                Cv2.Line(src, tip1, tip2, Scalar.Red, 2, LineTypes.AntiAlias);

                // Step B: Find the maximum perpendicular width strictly bounded by the polygon edges
                double maxWidthDist = 0;
                Point widthPoint1 = new Point(0, 0);
                Point widthPoint2 = new Point(0, 0);

                // Vector line logic calculation parameters
                double axisX = tip2.X - tip1.X;
                double axisY = tip2.Y - tip1.Y;
                double axisLength = Math.Sqrt(axisX * axisX + axisY * axisY);

                for (int i = 0; i < polygon.Length; i++)
                {
                    for (int j = i + 1; j < polygon.Length; j++)
                    {
                        // Vector between two test side points
                        double sX = polygon[j].X - polygon[i].X;
                        double sY = polygon[j].Y - polygon[i].Y;

                        // Project onto the perpendicular axis vector to isolate true structural width gaps
                        double crossProduct = Math.Abs(sX * axisY - sY * axisX) / (axisLength == 0 ? 1 : axisLength);

                        if (crossProduct > maxWidthDist)
                        {
                            maxWidthDist = crossProduct;
                            widthPoint1 = polygon[i];
                            widthPoint2 = polygon[j];
                        }
                    }
                }

                // Draw the true width axis crosshair line perfectly connecting the corners
                Cv2.Line(src, widthPoint1, widthPoint2, Scalar.Yellow, 2, LineTypes.AntiAlias);

                // ==========================================
                // 5. CALIBRATED CONVERSION MEASUREMENTS
                // ==========================================
                double lengthMM = maxTipDist / ppm;
                double widthMM = maxWidthDist / ppm;
                double ratio = widthMM == 0 ? 0 : lengthMM / widthMM;

                Point lengthMidPoint = new Point((tip1.X + tip2.X) / 2 + 20, (tip1.Y + tip2.Y) / 2 - 15);
                Point widthMidPoint = new Point((widthPoint1.X + widthPoint2.X) / 2 - 80, (widthPoint1.Y + widthPoint2.Y) / 2 + 25);

                Cv2.PutText(src, $"L: {lengthMM:F2}mm", lengthMidPoint, HersheyFonts.HersheySimplex, 0.55, Scalar.Cyan, 2, LineTypes.AntiAlias);
                Cv2.PutText(src, $"W: {widthMM:F2}mm", widthMidPoint, HersheyFonts.HersheySimplex, 0.55, Scalar.Yellow, 2, LineTypes.AntiAlias);

                src.ImWrite("Marquise_Result.png");

                // ==========================================
                // 6. SHOW RESULT
                // ==========================================
                return $"Length : {lengthMM:F2} mm\n" +
                       $"Width  : {widthMM:F2} mm\n" +
                       $"L/W Ratio : {ratio:F2}";
            }
        }

        public string MeasureShapeWithVertexAxis1(Mat src)
        {
            if (src == null || src.Empty()) return "No Image Data";

            // ==========================================
            // 1. LOAD CALIBRATION SCALE
            // ==========================================
            double ppm = 1.0;
            try
            {
                ppm = Convert.ToDouble(new ModifyRegistry().Read("ppm"));
            }
            catch
            {
                return "Calibration Error";
            }
            if (ppm <= 0) ppm = 1.0;

            // Standard local unmanaged matrices inside using scopes to prevent memory leaks
            using (Mat gray = new Mat())
            using (Mat blur = new Mat())
            using (Mat thresh = new Mat())
            {
                // ==========================================
                // 2. PREPROCESSING & SEGMENTATION
                // ==========================================
                Cv2.CvtColor(src, gray, ColorConversionCodes.BGR2GRAY);
                Cv2.GaussianBlur(gray, blur, new Size(5, 5), 0);
                Cv2.Threshold(blur, thresh, 40, 255, ThresholdTypes.BinaryInv);

                using (Mat kernel = Cv2.GetStructuringElement(MorphShapes.Ellipse, new Size(5, 5)))
                {
                    Cv2.MorphologyEx(thresh, thresh, MorphTypes.Close, kernel);
                    Cv2.MorphologyEx(thresh, thresh, MorphTypes.Open, kernel);
                }

                // ==========================================
                // 3. FIND CONTOURS & FILTER NOISE
                // ==========================================
                Point[][] contours;
                HierarchyIndex[] hierarchy;
                Cv2.FindContours(thresh, out contours, out hierarchy, RetrievalModes.External, ContourApproximationModes.ApproxSimple);

                if (contours.Length == 0) return "No Shape Found";

                var largestContour = contours.OrderByDescending(c => Cv2.ContourArea(c)).First();

                double contourArea = Cv2.ContourArea(largestContour);
                if (contourArea < 800) return "No Object Detected (Noise Ignored)";

                Point[] hull = Cv2.ConvexHull(largestContour);
                double epsilon = 0.01 * Cv2.ArcLength(hull, true);
                Point[] smoothContour = Cv2.ApproxPolyDP(hull, epsilon, true);

                // Draw green edge profile vectors
                Cv2.Polylines(src, new[] { smoothContour }, true, Scalar.Lime, 2);
                
                // ==========================================
                // 4. VERTEX TRACKING MATH (AXIS ENGAGEMENT)
                // ==========================================
                // Step A: Find the absolute maximum distance between any two corners (Major Axis / Length)
                Point tip1 = hull[0];
                Point tip2 = hull[0];
                double vertexLengthPx = 0;

                for (int i = 0; i < hull.Length; i++)
                {
                    for (int j = i + 1; j < hull.Length; j++)
                    {
                        double d = Math.Sqrt(Math.Pow(hull[i].X - hull[j].X, 2) + Math.Pow(hull[i].Y - hull[j].Y, 2));
                        if (d > vertexLengthPx)
                        {
                            vertexLengthPx = d;
                            tip1 = hull[i];
                            tip2 = hull[j];
                        }
                    }
                }
                double vertexLengthMM = vertexLengthPx / ppm;

                // Draw Orange Major Axis Line
                Cv2.Line(src, tip1, tip2, Scalar.Orange, 2, LineTypes.AntiAlias);

                // Step B: Calculate maximum perpendicular crosshair width gap relative to the axis line
                double vertexWidthPx = 0;
                Point widthPoint1 = new Point(0, 0);
                Point widthPoint2 = new Point(0, 0);

                double axisX = tip2.X - tip1.X;
                double axisY = tip2.Y - tip1.Y;
                double axisLength = Math.Sqrt(axisX * axisX + axisY * axisY);

                for (int i = 0; i < hull.Length; i++)
                {
                    for (int j = i + 1; j < hull.Length; j++)
                    {
                        double sX = hull[j].X - hull[i].X;
                        double sY = hull[j].Y - hull[i].Y;

                        // Project test point vectors onto the perpendicular axis normal line
                        double crossProduct = Math.Abs(sX * axisY - sY * axisX) / (axisLength == 0 ? 1 : axisLength);

                        if (crossProduct > vertexWidthPx)
                        {
                            vertexWidthPx = crossProduct;
                            widthPoint1 = hull[i];
                            widthPoint2 = hull[j];
                        }
                    }
                }
                double vertexWidthMM = vertexWidthPx / ppm;
                double ratio = vertexLengthMM / vertexWidthMM;

                // Draw Yellow Crosshair Width Line
                Cv2.Line(src, widthPoint1, widthPoint2, Scalar.Yellow, 2, LineTypes.AntiAlias);

                // Overlay Text Label on Frame
                Point pVertexText = new Point(15, 35);
                Cv2.PutText(src, $"Axis L: {vertexLengthMM:F2}mm | W: {vertexWidthMM:F2}mm (Cross)", pVertexText, HersheyFonts.HersheySimplex, 0.55, Scalar.Yellow, 1, LineTypes.AntiAlias);

                src.ImWrite("Axis_Shape_Result.png");

                return $"Length : {vertexLengthMM:F2} mm\n" +
                       $"Width  : {vertexWidthMM:F2} mm\n" +
                       $"L/W Ratio   : {ratio:F2}";
                
                /*// ==========================================
                // 4. VERTEX TRACKING MATH (SHIELD / KITE GEOMETRY)
                // ==========================================

                // Convert the approximated polygon points to a list for spatial querying
                var points = smoothContour.ToList();

                // Step A: Find the Top tip and the Bottom edge midpoint (Length Axis)
                // 1. Top tip is the point with the minimum Y value
                Point tip1 = points.OrderBy(p => p.Y).First();

                // 2. Bottom edge consists of the two points with the maximum Y values
                var bottomPoints = points.OrderByDescending(p => p.Y).Take(2).ToList();

                // 3. Find the exact center of that flat bottom edge
                Point tip2 = new Point(
                    (bottomPoints[0].X + bottomPoints[1].X) / 2,
                    (bottomPoints[0].Y + bottomPoints[1].Y) / 2
                );

                // Calculate Length physical distance
                double vertexLengthPx = Math.Sqrt(Math.Pow(tip2.X - tip1.X, 2) + Math.Pow(tip2.Y - tip1.Y, 2));
                double vertexLengthMM = vertexLengthPx / ppm;

                // Draw Orange Major Axis Line (BGR Format: 0, 165, 255)
                Cv2.Line(src, tip1, tip2, new Scalar(0, 165, 255), 2, LineTypes.AntiAlias);

                // Step B: Find Left and Right extremes for the Width Axis
                Point widthPoint1 = points.OrderBy(p => p.X).First();
                Point widthPoint2 = points.OrderByDescending(p => p.X).First();

                // Calculate Width physical distance
                double vertexWidthPx = Math.Sqrt(Math.Pow(widthPoint2.X - widthPoint1.X, 2) + Math.Pow(widthPoint2.Y - widthPoint1.Y, 2));
                double vertexWidthMM = vertexWidthPx / ppm;
                double ratio = vertexWidthMM > 0 ? vertexLengthMM / vertexWidthMM : 0;

                // Draw Yellow Crosshair Width Line (BGR Format: 0, 255, 255)
                Cv2.Line(src, widthPoint1, widthPoint2, new Scalar(0, 255, 255), 2, LineTypes.AntiAlias);

                // Overlay Text Label on Frame
                Point pVertexText = new Point(15, 35);
                Cv2.PutText(src, $"Axis L: {vertexLengthMM:F2}mm | W: {vertexWidthMM:F2}mm", pVertexText, HersheyFonts.HersheySimplex, 0.55, new Scalar(0, 255, 255), 1, LineTypes.AntiAlias);

                // Save the result using OpenCvSharp's standard ImWrite
                Cv2.ImWrite("Axis_Shape_Result.png", src);

                return $"Length : {vertexLengthMM:F2} mm\n" +
                       $"Width  : {vertexWidthMM:F2} mm\n" +
                       $"L/W Ratio   : {ratio:F2}";*/
            }
        }

        public string MeasureShapeWithVertexAxisnew(Mat src)
        {
            if (src == null || src.Empty()) return "No Image Data";

            // ==========================================
            // 1. LOAD CALIBRATION SCALE
            // ==========================================
            double ppm = 1.0;
            try
            {
                ppm = Convert.ToDouble(new ModifyRegistry().Read("ppm"));
            }
            catch
            {
                return "Calibration Error";
            }
            if (ppm <= 0) ppm = 1.0;

            // Standard local unmanaged matrices inside using scopes to prevent memory leaks
            using (Mat gray = new Mat())
            using (Mat blur = new Mat())
            using (Mat thresh = new Mat())
            {
                // ==========================================
                // 2. PREPROCESSING & SEGMENTATION
                // ==========================================
                Cv2.CvtColor(src, gray, ColorConversionCodes.BGR2GRAY);
                Cv2.GaussianBlur(gray, blur, new Size(5, 5), 0);
                Cv2.Threshold(blur, thresh, 40, 255, ThresholdTypes.BinaryInv);

                using (Mat kernel = Cv2.GetStructuringElement(MorphShapes.Ellipse, new Size(5, 5)))
                {
                    Cv2.MorphologyEx(thresh, thresh, MorphTypes.Close, kernel);
                    Cv2.MorphologyEx(thresh, thresh, MorphTypes.Open, kernel);
                }

                // ==========================================
                // 3. FIND CONTOURS & FILTER NOISE
                // ==========================================
                Point[][] contours;
                HierarchyIndex[] hierarchy;
                Cv2.FindContours(thresh, out contours, out hierarchy, RetrievalModes.External, ContourApproximationModes.ApproxSimple);

                if (contours.Length == 0) return "No Shape Found";

                var largestContour = contours.OrderByDescending(c => Cv2.ContourArea(c)).First();

                double contourArea = Cv2.ContourArea(largestContour);
                if (contourArea < 800) return "No Object Detected (Noise Ignored)";

                Point[] hull = Cv2.ConvexHull(largestContour);
                double epsilon = 0.01 * Cv2.ArcLength(hull, true);
                Point[] polygon = Cv2.ApproxPolyDP(hull, epsilon, true);

                if (polygon.Length < 3) return "Invalid Polygon Corners";

                // Draw green edge profile vectors
                Cv2.Polylines(src, new[] { polygon }, true, Scalar.Lime, 2);

                // ==========================================
                // 4. UNIFIED VERNIER CALIPER MATH (FERET)
                // ==========================================
                // Step A: Find the maximum absolute distance between any two corners (Length Jaws)
                Point lengthTip1 = polygon[0];
                Point lengthTip2 = polygon[0];
                double maxDistSq = 0;

                for (int i = 0; i < polygon.Length; i++)
                {
                    for (int j = i + 1; j < polygon.Length; j++)
                    {
                        double dSq = Math.Pow(polygon[i].X - polygon[j].X, 2) + Math.Pow(polygon[i].Y - polygon[j].Y, 2);
                        if (dSq > maxDistSq)
                        {
                            maxDistSq = dSq;
                            lengthTip1 = polygon[i];
                            lengthTip2 = polygon[j];
                        }
                    }
                }

                double lengthPx = Math.Sqrt(maxDistSq);
                if (lengthPx == 0) return "Invalid Shape Profile";
                double lengthMM = lengthPx / ppm;

                // Step B: Calculate the orthogonal span perpendicular to the length (Width Jaws)
                double ux = (lengthTip2.X - lengthTip1.X) / lengthPx;
                double uy = (lengthTip2.Y - lengthTip1.Y) / lengthPx;

                // 90-Degree Normal Vector
                double nx = -uy;
                double ny = ux;

                double maxProj = double.MinValue;
                double minProj = double.MaxValue;

                Point widthTip1 = polygon[0];
                Point widthTip2 = polygon[0];

                // Sweep the shape to find exactly which two corners stop the caliper jaws
                foreach (Point p in polygon)
                {
                    double proj = p.X * nx + p.Y * ny;

                    if (proj > maxProj)
                    {
                        maxProj = proj;
                        widthTip1 = p; // Corner touching the top jaw
                    }
                    if (proj < minProj)
                    {
                        minProj = proj;
                        widthTip2 = p; // Corner touching the bottom jaw
                    }
                }

                // True geometric screw gauge width
                double widthPx = maxProj - minProj;
                double widthMM = widthPx / ppm;
                double ratio = lengthMM / widthMM;

                // ==========================================
                // 5. DRAW CROSSHAIR & GAUGE VISUALS
                // ==========================================
                // Draw Orange Length Axis Line
                Cv2.Line(src, lengthTip1, lengthTip2, Scalar.Orange, 2, LineTypes.AntiAlias);

                // Draw Yellow Width Axis Line directly connecting the two extreme width corners
                Cv2.Line(src, widthTip1, widthTip2, Scalar.Yellow, 2, LineTypes.AntiAlias);

                // ==========================================
                // 6. DRAW SIDES AND ANGLES OVERLAYS
                // ==========================================
                // Measure Sides & Draw Text
                for (int i = 0; i < polygon.Length; i++)
                {
                    Point p1 = polygon[i];
                    Point p2 = polygon[(i + 1) % polygon.Length];

                    double pixelDistance = Math.Sqrt(Math.Pow(p2.X - p1.X, 2) + Math.Pow(p2.Y - p1.Y, 2));
                    double mmDistance = pixelDistance / ppm;

                    Point mid = new Point((p1.X + p2.X) / 2, (p1.Y + p2.Y) / 2);
                    Cv2.PutText(src, $"{mmDistance:F1}mm", mid, HersheyFonts.HersheySimplex, 0.55, Scalar.Yellow, 1, LineTypes.AntiAlias);
                }

                // Measure Angles & Draw Text
                for (int i = 0; i < polygon.Length; i++)
                {
                    Point prev = polygon[(i - 1 + polygon.Length) % polygon.Length];
                    Point current = polygon[i];
                    Point next = polygon[(i + 1) % polygon.Length];

                    double ax = prev.X - current.X;
                    double ay = prev.Y - current.Y;
                    double bx = next.X - current.X;
                    double by = next.Y - current.Y;

                    double dot = (ax * bx) + (ay * by);
                    double magA = Math.Sqrt(ax * ax + ay * ay);
                    double magB = Math.Sqrt(bx * bx + by * by);
                    double angle = 0;

                    if (magA != 0 && magB != 0)
                    {
                        double cosTheta = Math.Max(-1.0, Math.Min(1.0, dot / (magA * magB)));
                        angle = Math.Acos(cosTheta) * 180.0 / Math.PI;
                    }

                    Cv2.Circle(src, current, 4, Scalar.Red, -1);
                    Cv2.PutText(src, $"{angle:F1}°", new Point(current.X + 10, current.Y), HersheyFonts.HersheySimplex, 0.55, Scalar.Cyan, 1, LineTypes.AntiAlias);
                }

                // Overlay Main Output Text Label on Frame
                Point pVertexText = new Point(15, 35);
                Cv2.PutText(src, $"L: {lengthMM:F2}mm | W: {widthMM:F2}mm (Gauge)", pVertexText, HersheyFonts.HersheySimplex, 0.55, Scalar.Yellow, 1, LineTypes.AntiAlias);

                src.ImWrite("Axis_Shape_Result.png");

                return $"Length : {lengthMM:F2} mm\n" +
                       $"Width  : {widthMM:F2} mm\n" +
                       $"L/W Ratio   : {ratio:F2}";
            }
        }

    }
}