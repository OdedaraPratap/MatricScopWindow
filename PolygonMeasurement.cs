using OpenCvSharp;
using System;
using System.Linq;
using System.Text;

namespace Matric_scope
{
    public class PolygonMeasurement
    {
        /*public static string DetectAndMeasurePolygon(Mat src)
        {
            // ==========================================
            // 1. LOAD CALIBRATION
            // ==========================================
            double ppm = 0;
            try
            {
                ppm = Convert.ToDouble(new ModifyRegistry().Read("ppm"));
            }
            catch
            {
                return "Calibration Error";
            }

            if (ppm <= 0)
            {
                return "Please Calibrate First";
            }

            // ==========================================
            // 2. IMAGE PREPROCESSING
            // ==========================================
            //Mat src = inputImage.Clone();
            Mat gray = new Mat();
            Cv2.CvtColor(src, gray, ColorConversionCodes.BGR2GRAY);
            Cv2.GaussianBlur(gray, gray, new Size(3, 3), 0);

            Mat thresh = new Mat();
            Cv2.InRange(gray, new Scalar(0), new Scalar(95), thresh);

            Mat kernel = Cv2.GetStructuringElement(MorphShapes.Rect, new Size(5, 5));
            Cv2.MorphologyEx(thresh, thresh, MorphTypes.Close, kernel);

            Mat expandKernel = Cv2.GetStructuringElement(MorphShapes.Rect, new Size(3, 3));
            Cv2.Dilate(thresh, thresh, expandKernel, iterations: 1);

            // ==========================================
            // 3. FIND CONTOURS
            // ==========================================
            Point[][] contours;
            HierarchyIndex[] hierarchy;
            Cv2.FindContours(thresh, out contours, out hierarchy, RetrievalModes.External, ContourApproximationModes.ApproxSimple);

            if (contours.Length == 0)
            {
                return "No Polygon Found";
            }

            // ==========================================
            // 4. FIND LARGEST CONTOUR
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
            Point[] largestContour = contours[largestIndex];

            // ==========================================
            // 5. CONVEX HULL
            // ==========================================
            Point[] hull = Cv2.ConvexHull(largestContour);

            // ==========================================
            // 6. POLYGON APPROXIMATION
            // ==========================================
            double perimeter = Cv2.ArcLength(hull, true);
            double epsilon = 0.04 * perimeter;
            Point[] polygon = Cv2.ApproxPolyDP(hull, epsilon, true);

            // ==========================================
            // 7. DYNAMIC SHAPE NAMING (REPLACED "FORCE 4 SIDES")
            // ==========================================
            string shapeName;
            switch (polygon.Length)
            {
                case 3: shapeName = "Triangle"; break;
                case 4: shapeName = "Quadrilateral"; break;
                case 5: shapeName = "Pentagon"; break;
                case 6: shapeName = "Hexagon"; break;
                default: shapeName = $"{polygon.Length}-sided Polygon"; break;
            }

            // ==========================================
            // 8. SORT POINTS CLOCKWISE
            // ==========================================
            polygon = SortCornersClockwise(polygon);

            // ==========================================
            // 9. DRAW POLYGON
            // ==========================================
            for (int i = 0; i < polygon.Length; i++)
            {
                Point p1 = polygon[i];
                Point p2 = polygon[(i + 1) % polygon.Length];
                Cv2.Line(src, p1, p2, Scalar.Lime, 1);
            }

            // ==========================================
            // 10. MEASURE SIDES
            // ==========================================
            StringBuilder sb = new StringBuilder();
            sb.Append($"Detected Shape: {shapeName}");
            sb.Append("");
            sb.Append("Side Lengths ");

            for (int i = 0; i < polygon.Length; i++)
            {
                Point p1 = polygon[i];
                Point p2 = polygon[(i + 1) % polygon.Length];

                double pixelDistance = Distance(p1, p2);
                double mmDistance = pixelDistance / ppm;

                sb.Append($"Side {i + 1}: {mmDistance:F2} mm ");

                Point mid = new Point((p1.X + p2.X) / 2, (p1.Y + p2.Y) / 2);
                Cv2.PutText(src, $"{mmDistance:F1}mm", mid, HersheyFonts.HersheySimplex, 0.6, Scalar.Yellow, 1);
            }

            // ==========================================
            // 11. MEASURE ANGLES
            // ==========================================
            sb.Append("Interior Angles ");
            for (int i = 0; i < polygon.Length; i++)
            {
                Point prev = polygon[(i - 1 + polygon.Length) % polygon.Length];
                Point current = polygon[i];
                Point next = polygon[(i + 1) % polygon.Length];

                double angle = CalculateAngle(prev, current, next);
                sb.Append($"Angle {i + 1}: {angle:F1}°");

                Cv2.Circle(src, current, 5, Scalar.Red, -1);
                Cv2.PutText(src, $"{angle:F2}°", new Point(current.X + 10, current.Y), HersheyFonts.HersheySimplex, 0.7, Scalar.Cyan, 1);
            }

            // ==========================================
            // 13. SAVE RESULT
            // ==========================================
            //src.ImWrite("poly.png");

            return sb.ToString();
        }
        */

        /*public static string DetectAndMeasurePolygon(Mat src)
        {
            if (src == null || src.Empty()) return "No Image Data";

            // ==========================================
            // 1. LOAD CALIBRATION
            // ==========================================
            double ppm = 0;
            try
            {
                ppm = Convert.ToDouble(new ModifyRegistry().Read("ppm"));
            }
            catch
            {
                return "Calibration Error";
            }

            if (ppm <= 0) return "Please Calibrate First";

            // Standard local unmanaged matrices inside using scopes to prevent memory leaks
            using (Mat gray = new Mat())
            using (Mat blur = new Mat())
            using (Mat thresh = new Mat())
            {
                // ==========================================
                // 2. FIXED PREPROCESSING: BINARY THRESHOLD
                // ==========================================
                Cv2.CvtColor(src, gray, ColorConversionCodes.BGR2GRAY);
                Cv2.GaussianBlur(gray, blur, new Size(5, 5), 0);

                // Value 40 safely isolates the stone footprint from dark-gray backgrounds
                Cv2.Threshold(blur, thresh, 40, 255, ThresholdTypes.BinaryInv);

                using (Mat kernel = Cv2.GetStructuringElement(MorphShapes.Ellipse, new Size(5, 5)))
                {
                    Cv2.MorphologyEx(thresh, thresh, MorphTypes.Close, kernel);
                    Cv2.MorphologyEx(thresh, thresh, MorphTypes.Open, kernel); // Dissolves background dust
                }

                // ==========================================
                // 3. FIND CONTOURS
                // ==========================================
                Point[][] contours;
                HierarchyIndex[] hierarchy;
                Cv2.FindContours(thresh, out contours, out hierarchy, RetrievalModes.External, ContourApproximationModes.ApproxSimple);

                if (contours.Length == 0) return "No Polygon Found";

                // Isolate largest object silhouette mass
                var largestContour = contours.OrderByDescending(c => Cv2.ContourArea(c)).First();

                // Dust Shield Exclusion Filter
                double contourArea = Cv2.ContourArea(largestContour);
                if (contourArea < 800) return "No Object Detected (Noise Ignored)";

                // ==========================================
                // 4. CONVEX HULL & POLYGON APPROXIMATION
                // ==========================================
                Point[] hull = Cv2.ConvexHull(largestContour);

                // Epsilon factor safely smooths minor pixel artifacts into clean straight side vectors
                double perimeter = Cv2.ArcLength(hull, true);
                double epsilon = 0.02 * perimeter;
                Point[] polygon = Cv2.ApproxPolyDP(hull, epsilon, true);

                if (polygon.Length < 3) return "Invalid Polygon Corners";

                // Dynamic Shape Naming
                string shapeName;
                switch (polygon.Length)
                {
                    case 3: shapeName = "Triangle"; break;
                    case 4: shapeName = "Quadrilateral"; break;
                    case 5: shapeName = "Pentagon"; break;
                    case 6: shapeName = "Hexagon"; break;
                    default: shapeName = $"{polygon.Length}-sided Polygon"; break;
                }

                // Sort Corners Clockwise
                polygon = SortCornersClockwise(polygon);

                // ==========================================
                // 5. EXTRACT TIP-TO-TIP LENGTH & PERPENDICULAR WIDTH (FIXED AXIS)
                // ==========================================
                // Step A: Find the absolute two corners furthest apart (The Major Axis / Length)
                double maxDistSq = 0;
                Point tip1 = polygon[0];
                Point tip2 = polygon[0];

                for (int i = 0; i < polygon.Length; i++)
                {
                    for (int j = i + 1; j < polygon.Length; j++)
                    {
                        double dx = polygon[i].X - polygon[j].X;
                        double dy = polygon[i].Y - polygon[j].Y;
                        double distSq = (dx * dx) + (dy * dy);
                        if (distSq > maxDistSq)
                        {
                            maxDistSq = distSq;
                            tip1 = polygon[i];
                            tip2 = polygon[j];
                        }
                    }
                }

                double lengthPx = Math.Sqrt(maxDistSq);

                // Step B: Calculate the exact heading angle of this primary centerline axis
                double angleRad = Math.Atan2(tip2.Y - tip1.Y, tip2.X - tip1.X);
                double angleDeg = angleRad * 180.0 / Math.PI;

                // Step C: Project all remaining points perpendicular to this axis to extract true max width gap
                // Line equation components from tip1 to tip2: Ax + By + C = 0
                double A = tip2.Y - tip1.Y;
                double B = tip1.X - tip2.X;
                double C = (tip2.X * tip1.Y) - (tip1.X * tip2.Y);
                double denominator = Math.Sqrt(A * A + B * B);

                double maxLeftDist = 0;
                double maxRightDist = 0;

                foreach (Point p in polygon)
                {
                    // Signed distance determines which side of the centerline axis the point sits on
                    double signedDist = (A * p.X + B * p.Y + C) / denominator;
                    if (signedDist > 0)
                    {
                        if (signedDist > maxLeftDist) maxLeftDist = signedDist;
                    }
                    else
                    {
                        if (Math.Abs(signedDist) > maxRightDist) maxRightDist = Math.Abs(signedDist);
                    }
                }

                double widthPx = maxLeftDist + maxRightDist;

                // Step D: Calculate final metric millimeter sizes
                double generalLengthMM = lengthPx / ppm;
                double generalWidthMM = widthPx / ppm;

                // Step E: Build a custom RotatedRect aligned straight tip-to-tip
                Point2f rectCenter = new Point2f((tip1.X + tip2.X) / 2f, (tip1.Y + tip2.Y) / 2f);
                Size2f rectSize = new Size2f((float)lengthPx, (float)widthPx);

                // This creates a bounding box rotated exactly to match the main diagonal axis
                RotatedRect tipToTipRect = new RotatedRect(rectCenter, rectSize, (float)angleDeg);

                // Draw the perfectly aligned red tip-to-tip box onto image frame
                Point2f[] box = tipToTipRect.Points();
                Point[] boxPoints = box.Select(p => new Point((int)p.X, (int)p.Y)).ToArray();
                Cv2.Polylines(src, new[] { boxPoints }, true, Scalar.Red, 1);

                // Draw a subtle baseline path directly connecting both primary tips
                Cv2.Line(src, tip1, tip2, Scalar.Orange, 1, LineTypes.AntiAlias);

                // ==========================================
                // 6. BUILD STRINGS & DRAW VISUAL OVERLAYS
                // ==========================================
                StringBuilder sb = new StringBuilder();
                
                //sb.AppendLine($"Detected Shape: {shapeName}");
                
                sb.AppendLine($"Length: {generalLengthMM:F2} mm");
                sb.AppendLine($"Width : {generalWidthMM:F2} mm");
                
                //sb.Append("Side Lengths ");

                // Draw Polygon Vectors
                for (int i = 0; i < polygon.Length; i++)
                {
                    Point p1 = polygon[i];
                    Point p2 = polygon[(i + 1) % polygon.Length];
                    Cv2.Line(src, p1, p2, Scalar.Lime, 2);
                }

                // Measure Sides
                for (int i = 0; i < polygon.Length; i++)
                {
                    Point p1 = polygon[i];
                    Point p2 = polygon[(i + 1) % polygon.Length];

                    double pixelDistance = Distance(p1, p2);
                    double mmDistance = pixelDistance / ppm;

                    //sb.Append($"Side {i + 1}: {mmDistance:F2} mm ");

                    Point mid = new Point((p1.X + p2.X) / 2, (p1.Y + p2.Y) / 2);
                    Cv2.PutText(src, $"{mmDistance:F1}mm", mid, HersheyFonts.HersheySimplex, 0.55, Scalar.Yellow, 1);
                }

                // Measure Angles
                //sb.Append("Interior Angles ");
                for (int i = 0; i < polygon.Length; i++)
                {
                    Point prev = polygon[(i - 1 + polygon.Length) % polygon.Length];
                    Point current = polygon[i];
                    Point next = polygon[(i + 1) % polygon.Length];

                    double angle = CalculateAngle(prev, current, next);
                    //sb.Append($"Angle {i + 1}: {angle:F1}°");

                    Cv2.Circle(src, current, 4, Scalar.Red, -1);
                    Cv2.PutText(src, $"{angle:F1}°", new Point(current.X + 10, current.Y), HersheyFonts.HersheySimplex, 0.55, Scalar.Cyan, 1);
                }

                src.ImWrite("Polygon_Result.png");
                return sb.ToString();
            }
        }
        */

        /*public static string DetectAndMeasurePolygon(Mat src)
        {
            if (src == null || src.Empty()) return "No Image Data";

            // ==========================================
            // 1. LOAD CALIBRATION
            // ==========================================
            double ppm = 0;
            try
            {
                ppm = Convert.ToDouble(new ModifyRegistry().Read("ppm"));
            }
            catch
            {
                return "Calibration Error";
            }

            if (ppm <= 0) return "Please Calibrate First";

            using (Mat gray = new Mat())
            using (Mat blur = new Mat())
            using (Mat thresh = new Mat())
            {
                // ==========================================
                // 2. FIXED PREPROCESSING: BINARY THRESHOLD
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
                // 3. FIND CONTOURS
                // ==========================================
                Point[][] contours;
                HierarchyIndex[] hierarchy;
                Cv2.FindContours(thresh, out contours, out hierarchy, RetrievalModes.External, ContourApproximationModes.ApproxSimple);

                if (contours.Length == 0) return "No Polygon Found";

                var largestContour = contours.OrderByDescending(c => Cv2.ContourArea(c)).First();

                double contourArea = Cv2.ContourArea(largestContour);
                if (contourArea < 800) return "No Object Detected (Noise Ignored)";

                // ==========================================
                // 4. CONVEX HULL & POLYGON APPROXIMATION
                // ==========================================
                Point[] hull = Cv2.ConvexHull(largestContour);

                double perimeter = Cv2.ArcLength(hull, true);
                double epsilon = 0.02 * perimeter;
                Point[] polygon = Cv2.ApproxPolyDP(hull, epsilon, true);

                if (polygon.Length < 3) return "Invalid Polygon Corners";

                // ==========================================
                // 5. TRUE TIP-TO-TIP VECTOR BOUNDING BOX
                // ==========================================
                double maxDistSq = 0;
                Point tip1 = polygon[0];
                Point tip2 = polygon[0];

                for (int i = 0; i < polygon.Length; i++)
                {
                    for (int j = i + 1; j < polygon.Length; j++)
                    {
                        double dSq = Math.Pow(polygon[i].X - polygon[j].X, 2) + Math.Pow(polygon[i].Y - polygon[j].Y, 2);
                        if (dSq > maxDistSq)
                        {
                            maxDistSq = dSq;
                            tip1 = polygon[i];
                            tip2 = polygon[j];
                        }
                    }
                }

                double lengthPx = Math.Sqrt(maxDistSq);
                if (lengthPx == 0) return "Invalid Shape Profile";

                double ux = (tip2.X - tip1.X) / lengthPx;
                double uy = (tip2.Y - tip1.Y) / lengthPx;

                double nx = uy;
                double ny = -ux;

                double maxLeftDist = 0;
                double maxRightDist = 0;

                foreach (Point p in polygon)
                {
                    double px = p.X - tip1.X;
                    double py = p.Y - tip1.Y;

                    double projection = px * nx + py * ny;

                    if (projection > maxLeftDist) maxLeftDist = projection;
                    if (projection < maxRightDist) maxRightDist = projection;
                }

                double widthPx = maxLeftDist - maxRightDist;

                double midX = (tip1.X + tip2.X) / 2.0;
                double midY = (tip1.Y + tip2.Y) / 2.0;

                double shift = (maxLeftDist + maxRightDist) / 2.0;
                double centerX = midX + shift * nx;
                double centerY = midY + shift * ny;

                double halfL = lengthPx / 2.0;
                double halfW = widthPx / 2.0;

                Point[] boxPoints = new Point[4];
                boxPoints[0] = new Point((int)Math.Round(centerX + halfL * ux + halfW * nx), (int)Math.Round(centerY + halfL * uy + halfW * ny));
                boxPoints[1] = new Point((int)Math.Round(centerX - halfL * ux + halfW * nx), (int)Math.Round(centerY - halfL * uy + halfW * ny));
                boxPoints[2] = new Point((int)Math.Round(centerX - halfL * ux - halfW * nx), (int)Math.Round(centerY - halfL * uy - halfW * ny));
                boxPoints[3] = new Point((int)Math.Round(centerX + halfL * ux - halfW * nx), (int)Math.Round(centerY + halfL * uy - halfW * ny));

                Cv2.Polylines(src, new[] { boxPoints }, true, Scalar.Red, 1);
                Cv2.Line(src, tip1, tip2, Scalar.Orange, 1, LineTypes.AntiAlias);

                // ==========================================
                // 6. BUILD STRINGS & DRAW VISUAL OVERLAYS
                // ==========================================
                double generalLengthMM = lengthPx / ppm;
                double generalWidthMM = widthPx / ppm;

                StringBuilder sb = new StringBuilder();
                sb.AppendLine($"Length: {generalLengthMM:F2} mm");
                sb.AppendLine($"Width : {generalWidthMM:F2} mm");

                // Draw Polygon Vectors
                for (int i = 0; i < polygon.Length; i++)
                {
                    Point p1 = polygon[i];
                    Point p2 = polygon[(i + 1) % polygon.Length];
                    Cv2.Line(src, p1, p2, Scalar.Lime, 2);
                }

                // Measure Sides & Draw Text Overlay
                for (int i = 0; i < polygon.Length; i++)
                {
                    Point p1 = polygon[i];
                    Point p2 = polygon[(i + 1) % polygon.Length];

                    double pixelDistance = Distance(p1, p2);
                    double mmDistance = pixelDistance / ppm;

                    Point mid = new Point((p1.X + p2.X) / 2, (p1.Y + p2.Y) / 2);
                    Cv2.PutText(src, $"{mmDistance:F1}mm", mid, HersheyFonts.HersheySimplex, 0.55, Scalar.Yellow, 1);
                }

                // Measure Angles & Draw Text Overlay
                for (int i = 0; i < polygon.Length; i++)
                {
                    Point prev = polygon[(i - 1 + polygon.Length) % polygon.Length];
                    Point current = polygon[i];
                    Point next = polygon[(i + 1) % polygon.Length];

                    double angle = CalculateAngle(prev, current, next);

                    Cv2.Circle(src, current, 4, Scalar.Red, -1);
                    Cv2.PutText(src, $"{angle:F1}°", new Point(current.X + 10, current.Y), HersheyFonts.HersheySimplex, 0.55, Scalar.Cyan, 1);
                }

                src.ImWrite("Polygon_Result.png");
                return sb.ToString();
            }
        }
        */

        public static string DetectAndMeasurePolygon(Mat src)
        {
            if (src == null || src.Empty()) return "No Image Data";

            // ==========================================
            // 1. LOAD CALIBRATION
            // ==========================================
            double ppm = 0;
            try
            {
                ppm = Convert.ToDouble(new ModifyRegistry().Read("ppm"));
            }
            catch
            {
                return "Calibration Error";
            }

            if (ppm <= 0) return "Please Calibrate First";

            using (Mat gray = new Mat())
            using (Mat blur = new Mat())
            using (Mat thresh = new Mat())
            {
                // ==========================================
                // 2. PREPROCESSING: OTSU AUTOMATIC THRESHOLDING
                // ==========================================
                Cv2.CvtColor(src, gray, ColorConversionCodes.BGR2GRAY);
                Cv2.GaussianBlur(gray, blur, new Size(5, 5), 0);

                // Universal Otsu Thresholding for Black Object on White Background
                Cv2.Threshold(blur, thresh, 0, 255, ThresholdTypes.BinaryInv | ThresholdTypes.Otsu);

                using (Mat kernel = Cv2.GetStructuringElement(MorphShapes.Ellipse, new Size(5, 5)))
                {
                    Cv2.MorphologyEx(thresh, thresh, MorphTypes.Close, kernel);
                    Cv2.MorphologyEx(thresh, thresh, MorphTypes.Open, kernel);
                }

                // ==========================================
                // 3. FIND CONTOURS
                // ==========================================
                Point[][] contours;
                HierarchyIndex[] hierarchy;
                Cv2.FindContours(thresh, out contours, out hierarchy, RetrievalModes.External, ContourApproximationModes.ApproxSimple);

                if (contours.Length == 0) return "No Polygon Found";

                var largestContour = contours.OrderByDescending(c => Cv2.ContourArea(c)).First();

                double contourArea = Cv2.ContourArea(largestContour);
                if (contourArea < 800) return "No Object Detected (Noise Ignored)";

                // ==========================================
                // 4. CONVEX HULL & POLYGON APPROXIMATION
                // ==========================================
                Point[] hull = Cv2.ConvexHull(largestContour);

                double perimeter = Cv2.ArcLength(hull, true);
                double epsilon = 0.02 * perimeter;
                Point[] polygon = Cv2.ApproxPolyDP(hull, epsilon, true);

                if (polygon.Length < 3) return "Invalid Polygon Corners";

                // ==========================================
                // 5. TRUE TIP-TO-TIP VECTOR BOUNDING BOX
                // ==========================================
                double maxDistSq = 0;
                Point tip1 = polygon[0];
                Point tip2 = polygon[0];

                for (int i = 0; i < polygon.Length; i++)
                {
                    for (int j = i + 1; j < polygon.Length; j++)
                    {
                        double dSq = Math.Pow(polygon[i].X - polygon[j].X, 2) + Math.Pow(polygon[i].Y - polygon[j].Y, 2);
                        if (dSq > maxDistSq)
                        {
                            maxDistSq = dSq;
                            tip1 = polygon[i];
                            tip2 = polygon[j];
                        }
                    }
                }

                double lengthPx = Math.Sqrt(maxDistSq);
                if (lengthPx == 0) return "Invalid Shape Profile";

                double ux = (tip2.X - tip1.X) / lengthPx;
                double uy = (tip2.Y - tip1.Y) / lengthPx;

                double nx = uy;
                double ny = -ux;

                double maxLeftDist = 0;
                double maxRightDist = 0;

                foreach (Point p in polygon)
                {
                    double px = p.X - tip1.X;
                    double py = p.Y - tip1.Y;

                    double projection = px * nx + py * ny;

                    if (projection > maxLeftDist) maxLeftDist = projection;
                    if (projection < maxRightDist) maxRightDist = projection;
                }

                double widthPx = maxLeftDist - maxRightDist;

                double midX = (tip1.X + tip2.X) / 2.0;
                double midY = (tip1.Y + tip2.Y) / 2.0;

                double shift = (maxLeftDist + maxRightDist) / 2.0;
                double centerX = midX + shift * nx;
                double centerY = midY + shift * ny;

                double halfL = lengthPx / 2.0;
                double halfW = widthPx / 2.0;

                Point[] boxPoints = new Point[4];
                boxPoints[0] = new Point((int)Math.Round(centerX + halfL * ux + halfW * nx), (int)Math.Round(centerY + halfL * uy + halfW * ny));
                boxPoints[1] = new Point((int)Math.Round(centerX - halfL * ux + halfW * nx), (int)Math.Round(centerY - halfL * uy + halfW * ny));
                boxPoints[2] = new Point((int)Math.Round(centerX - halfL * ux - halfW * nx), (int)Math.Round(centerY - halfL * uy - halfW * ny));
                boxPoints[3] = new Point((int)Math.Round(centerX + halfL * ux - halfW * nx), (int)Math.Round(centerY + halfL * uy - halfW * ny));

                double generalLengthMM = lengthPx / ppm;
                double generalWidthMM = widthPx / ppm;

                generalLengthMM = ApplyVariation(generalLengthMM, true);
                generalWidthMM = ApplyVariation(generalWidthMM, false);
                StringBuilder sb = new StringBuilder();
                sb.AppendLine($"Length: {generalLengthMM:F2} mm");
                sb.AppendLine($"Width : {generalWidthMM:F2} mm");

                // ==========================================
                // 6. RENDER ORDER FIX (Draw lines first, contours last)
                // ==========================================
                // Draw measurement box and axis lines first with thickness 1
                Cv2.Polylines(src, new[] { boxPoints }, true, Scalar.Red, 1, LineTypes.AntiAlias);
                Cv2.Line(src, tip1, tip2, Scalar.Orange, 1, LineTypes.AntiAlias);

                // Draw Polygon Vectors (Green Border) LAST with thickness 2 to mask line bleeding
                for (int i = 0; i < polygon.Length; i++)
                {
                    Point p1 = polygon[i];
                    Point p2 = polygon[(i + 1) % polygon.Length];
                    Cv2.Line(src, p1, p2, Scalar.Lime, 2, LineTypes.AntiAlias);
                }

                // Measure Sides & Draw Text Overlay
                for (int i = 0; i < polygon.Length; i++)
                {
                    Point p1 = polygon[i];
                    Point p2 = polygon[(i + 1) % polygon.Length];

                    double pixelDistance = Distance(p1, p2);
                    double mmDistance = pixelDistance / ppm;

                    Point mid = new Point((p1.X + p2.X) / 2, (p1.Y + p2.Y) / 2);
                    Cv2.PutText(src, $"{mmDistance:F2}mm", mid, HersheyFonts.HersheySimplex, 0.55, Scalar.Yellow, 1, LineTypes.AntiAlias);
                }

                // Measure Angles & Draw Text Overlay
                for (int i = 0; i < polygon.Length; i++)
                {
                    Point prev = polygon[(i - 1 + polygon.Length) % polygon.Length];
                    Point current = polygon[i];
                    Point next = polygon[(i + 1) % polygon.Length];

                    double angle = CalculateAngle(prev, current, next);

                    Cv2.Circle(src, current, 3, Scalar.Red, -1, LineTypes.AntiAlias);
                    DrawAngleWithDegreeSymbol(src, angle, new Point(current.X + 10, current.Y));
                }

                // ==========================================================
                // 7. CREATE ISOLATED SHAPE IMAGE FOR LABEL PRINTING
                // ==========================================================
                using (Mat printCanvas = new Mat(src.Size(), MatType.CV_8UC3, Scalar.White))
                {
                    // Draw ONLY the polygon shape outline in thick black
                    Cv2.Polylines(printCanvas, new[] { polygon }, true, Scalar.Black, 3, LineTypes.AntiAlias);

                    // Find the bounding box to crop away empty white space
                    OpenCvSharp.Rect cropRect = Cv2.BoundingRect(polygon);

                    // Add a 15-pixel margin around the shape
                    cropRect.Inflate(15, 15);

                    // Safety check: Ensure the crop box doesn't go outside the image boundaries
                    cropRect.Intersect(new OpenCvSharp.Rect(0, 0, printCanvas.Width, printCanvas.Height));

                    // Crop the canvas and save it specifically for the label printer
                    using (Mat croppedForPrint = new Mat(printCanvas, cropRect))
                    {
                        croppedForPrint.ImWrite("ShapeForLabel.png");
                    }
                }

                src.ImWrite("Polygon_Result.png");
                return sb.ToString();
            }
        }
        static double Distance(Point p1, Point p2)
        {
            return Math.Sqrt(Math.Pow(p2.X - p1.X, 2) + Math.Pow(p2.Y - p1.Y, 2));
        }

        static double CalculateAngle(Point prev, Point current, Point next)
        {
            double ax = prev.X - current.X;
            double ay = prev.Y - current.Y;
            double bx = next.X - current.X;
            double by = next.Y - current.Y;

            double dot = (ax * bx) + (ay * by);
            double magA = Math.Sqrt(ax * ax + ay * ay);
            double magB = Math.Sqrt(bx * bx + by * by);

            // Avoid division by zero bugs
            if (magA == 0 || magB == 0) return 0;

            double cosTheta = dot / (magA * magB);
            // Clamp to avoid tiny floating point precision overflow errors outside [-1, 1]
            cosTheta = Math.Max(-1.0, Math.Min(1.0, cosTheta));

            double angle = Math.Acos(cosTheta);
            return angle * 180.0 / Math.PI;
        }

        private static void DrawAngleWithDegreeSymbol(Mat image, double angle, Point origin)
        {
            // OpenCV's Hershey font does not contain the Unicode degree glyph and
            // renders it as '?'. Draw the value with one decimal place, then draw
            // a small outline circle in the degree-symbol position.
            const double fontScale = 0.55;
            const int thickness = 1;
            Scalar color = Scalar.Cyan;
            string angleText = angle.ToString("F1");

            Cv2.PutText(image, angleText, origin, HersheyFonts.HersheySimplex,
                fontScale, color, thickness, LineTypes.AntiAlias);

            int baseline;
            OpenCvSharp.Size textSize = Cv2.GetTextSize(angleText,
                HersheyFonts.HersheySimplex, fontScale, thickness, out baseline);
            Point degreeCenter = new Point(origin.X + textSize.Width + 3,
                origin.Y - Math.Max(3, textSize.Height - 3));
            Cv2.Circle(image, degreeCenter, 3, color, 1, LineTypes.AntiAlias);
        }

        static Point[] SortCornersClockwise(Point[] points)
        {
            Point center = new Point((int)points.Average(p => p.X), (int)points.Average(p => p.Y));
            return points.OrderBy(p => Math.Atan2(p.Y - center.Y, p.X - center.X)).ToArray();
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
    }
}
