using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Matric_scope
{
    public class MeasurePearShape
    {

        public string MeasurePearWithOpenCvSharp1(Mat src)
        {
            if (src == null || src.Empty()) return "No Image Data";

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
                Cv2.CvtColor(src, gray, ColorConversionCodes.BGR2GRAY);
                Cv2.GaussianBlur(gray, blur, new Size(3, 3), 0);

                // Automatic Otsu Thresholding for Black Object on White Background
                Cv2.Threshold(blur, thresh, 0, 255, ThresholdTypes.BinaryInv | ThresholdTypes.Otsu);

                using (Mat kernel = Cv2.GetStructuringElement(MorphShapes.Ellipse, new Size(3, 3)))
                {
                    Cv2.MorphologyEx(thresh, thresh, MorphTypes.Close, kernel);
                    Cv2.MorphologyEx(thresh, thresh, MorphTypes.Open, kernel);
                }

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

                // FIX: Calculate the bounding box using the smoothed contour (green line) to perfectly match the visual borders
                RotatedRect minRect = Cv2.MinAreaRect(smoothContour);

                double boxLengthPx = Math.Max(minRect.Size.Width, minRect.Size.Height);
                double boxWidthPx = Math.Min(minRect.Size.Width, minRect.Size.Height);

                double boxLengthMM = boxLengthPx / ppm;
                double boxWidthMM = boxWidthPx / ppm;
                double ratio = boxLengthMM / boxWidthMM;

                Point2f[] box = minRect.Points();
                Point[] boxPoints = box.Select(p => new Point((int)Math.Round(p.X), (int)Math.Round(p.Y))).ToArray();

                // Render Order Fix: Draw box first, green contour last
                Cv2.Polylines(src, new[] { boxPoints }, true, Scalar.Red, 1, LineTypes.AntiAlias);
                Cv2.Polylines(src, new[] { smoothContour }, true, Scalar.Lime, 2, LineTypes.AntiAlias);

                Point center = new Point((int)minRect.Center.X, (int)minRect.Center.Y);
                Cv2.Circle(src, center, 4, Scalar.White, -1, LineTypes.AntiAlias);

                boxLengthMM = ApplyVariation(boxLengthMM, true);
                boxWidthMM = ApplyVariation(boxWidthMM, false);

                Point pBoxText = new Point(15, 35);
                Cv2.PutText(src, $"Box L: {boxLengthMM:F2}mm | W: {boxWidthMM:F2}mm", pBoxText, HersheyFonts.HersheySimplex, 0.55, Scalar.Red, 2, LineTypes.AntiAlias);

                // ==========================================================
                // CREATE ISOLATED SHAPE IMAGE FOR LABEL PRINTING
                // ==========================================================
                using (Mat printCanvas = new Mat(src.Size(), MatType.CV_8UC3, Scalar.White))
                {
                    // Draw ONLY the shape outline in thick black
                    Cv2.Polylines(printCanvas, new[] { smoothContour }, true, Scalar.Black, 3, LineTypes.AntiAlias);

                    // Find the bounding box to crop away empty white space
                    OpenCvSharp.Rect cropRect = Cv2.BoundingRect(smoothContour);

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

                src.ImWrite("Box_Shape_Result.png");

                return $"Length : {boxLengthMM:F2} mm\n" +
                       $"Width  : {boxWidthMM:F2} mm\n" +
                       $"L/W Ratio : {ratio:F2}";
            }
        }
        

        /*public string MeasurePearWithOpenCvSharp1(Mat src)
        {
            if (src == null || src.Empty()) return "No Image Data";

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
                Cv2.CvtColor(src, gray, ColorConversionCodes.BGR2GRAY);
                Cv2.GaussianBlur(gray, blur, new Size(3, 3), 0);

                // Automatic Otsu Thresholding for Black Object on White Background
                Cv2.Threshold(blur, thresh, 0, 255, ThresholdTypes.BinaryInv | ThresholdTypes.Otsu);

                using (Mat kernel = Cv2.GetStructuringElement(MorphShapes.Ellipse, new Size(3, 3)))
                {
                    Cv2.MorphologyEx(thresh, thresh, MorphTypes.Close, kernel);
                    Cv2.MorphologyEx(thresh, thresh, MorphTypes.Open, kernel);
                }

                Point[][] contours;
                HierarchyIndex[] hierarchy;

                // FIX 1: Use ApproxNone to capture every raw physical edge pixel
                Cv2.FindContours(thresh, out contours, out hierarchy, RetrievalModes.External, ContourApproximationModes.ApproxNone);

                if (contours.Length == 0) return "No Shape Found";

                var largestContour = contours.OrderByDescending(c => Cv2.ContourArea(c)).First();
                double contourArea = Cv2.ContourArea(largestContour);
                if (contourArea < 800) return "No Object Detected (Noise Ignored)";

                // FIX 2: Snap a perfectly tight rubber band around the raw pixels
                Point[] hull = Cv2.ConvexHull(largestContour);

                // FIX 3: Removed ApproxPolyDP completely. 
                // Calculate the bounding box directly from the un-distorted convex hull.
                RotatedRect minRect = Cv2.MinAreaRect(hull);

                double boxLengthPx = Math.Max(minRect.Size.Width, minRect.Size.Height);
                double boxWidthPx = Math.Min(minRect.Size.Width, minRect.Size.Height);

                double boxLengthMM = boxLengthPx / ppm;
                double boxWidthMM = boxWidthPx / ppm;
                double ratio = boxLengthMM / boxWidthMM;

                Point2f[] box = minRect.Points();
                Point[] boxPoints = box.Select(p => new Point((int)Math.Round(p.X), (int)Math.Round(p.Y))).ToArray();

                // Render Order Fix: Draw box first, green contour last
                Cv2.Polylines(src, new[] { boxPoints }, true, Scalar.Red, 1, LineTypes.AntiAlias);

                // Draw the smooth, un-chopped hull instead of the distorted smoothContour
                Cv2.Polylines(src, new[] { hull }, true, Scalar.Lime, 2, LineTypes.AntiAlias);

                Point center = new Point((int)minRect.Center.X, (int)minRect.Center.Y);
                Cv2.Circle(src, center, 4, Scalar.White, -1, LineTypes.AntiAlias);

                boxLengthMM = ApplyVariation(boxLengthMM, true);
                boxWidthMM = ApplyVariation(boxWidthMM, false);

                Point pBoxText = new Point(15, 35);
                Cv2.PutText(src, $"Box L: {boxLengthMM:F2}mm | W: {boxWidthMM:F2}mm", pBoxText, HersheyFonts.HersheySimplex, 0.55, Scalar.Red, 2, LineTypes.AntiAlias);

                // ==========================================================
                // CREATE ISOLATED SHAPE IMAGE FOR LABEL PRINTING
                // ==========================================================
                using (Mat printCanvas = new Mat(src.Size(), MatType.CV_8UC3, Scalar.White))
                {
                    // Draw ONLY the exact hull shape outline in thick black
                    Cv2.Polylines(printCanvas, new[] { hull }, true, Scalar.Black, 3, LineTypes.AntiAlias);

                    // Find the bounding box to crop away empty white space
                    OpenCvSharp.Rect cropRect = Cv2.BoundingRect(hull);

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

                src.ImWrite("Box_Shape_Result.png");

                return $"Length : {boxLengthMM:F2} mm\n" +
                       $"Width  : {boxWidthMM:F2} mm\n" +
                       $"L/W Ratio : {ratio:F2}";
            }
        }
        */
        
        public string MeasureMarkWithOpenCvSharp(Mat src)
        {
            if (src == null || src.Empty()) return "No Image Data";

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
                Cv2.CvtColor(src, gray, ColorConversionCodes.BGR2GRAY);
                Cv2.GaussianBlur(gray, blur, new Size(5, 5), 0);

                Cv2.Threshold(blur, thresh, 0, 255, ThresholdTypes.BinaryInv | ThresholdTypes.Otsu);

                using (Mat kernel = Cv2.GetStructuringElement(MorphShapes.Ellipse, new Size(3, 3)))
                {
                    Cv2.MorphologyEx(thresh, thresh, MorphTypes.Close, kernel);
                }

                Point[][] contours;
                HierarchyIndex[] hierarchy;
                Cv2.FindContours(thresh, out contours, out hierarchy, RetrievalModes.External, ContourApproximationModes.ApproxSimple);

                if (contours.Length == 0) return "No Shape Found";

                var largestContour = contours.OrderByDescending(c => Cv2.ContourArea(c)).First();
                Point[] hull = Cv2.ConvexHull(largestContour);

                double epsilon = 0.01 * Cv2.ArcLength(hull, true);
                Point[] smoothContour = Cv2.ApproxPolyDP(hull, epsilon, true);

                Point tip1 = new Point(0, 0);
                Point tip2 = new Point(0, 0);
                double maxTipDist = 0;

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

                double maxWidthDist = 0;
                Point widthPoint1 = new Point(0, 0);
                Point widthPoint2 = new Point(0, 0);

                double axisX = tip2.X - tip1.X;
                double axisY = tip2.Y - tip1.Y;
                double axisLength = Math.Sqrt(axisX * axisX + axisY * axisY);

                for (int i = 0; i < smoothContour.Length; i++)
                {
                    for (int j = i + 1; j < smoothContour.Length; j++)
                    {
                        double sX = smoothContour[j].X - smoothContour[i].X;
                        double sY = smoothContour[j].Y - smoothContour[i].Y;

                        double crossProduct = Math.Abs(sX * axisY - sY * axisX) / (axisLength == 0 ? 1 : axisLength);

                        if (crossProduct > maxWidthDist)
                        {
                            maxWidthDist = crossProduct;
                            widthPoint1 = smoothContour[i];
                            widthPoint2 = smoothContour[j];
                        }
                    }
                }

                double lengthMM = maxTipDist / ppm;
                double widthMM = maxWidthDist / ppm;
                double ratio = widthMM == 0 ? 0 : lengthMM / widthMM;

                Cv2.Line(src, tip1, tip2, Scalar.Red, 1, LineTypes.AntiAlias);
                Cv2.Line(src, widthPoint1, widthPoint2, Scalar.Yellow, 1, LineTypes.AntiAlias);
                Cv2.Polylines(src, new[] { smoothContour }, true, Scalar.Lime, 2, LineTypes.AntiAlias);

                Point lengthMidPoint = new Point((tip1.X + tip2.X) / 2 + 20, (tip1.Y + tip2.Y) / 2 - 15);
                Point widthMidPoint = new Point((widthPoint1.X + widthPoint2.X) / 2 - 80, (widthPoint1.Y + widthPoint2.Y) / 2 + 25);

                lengthMM = ApplyVariation(lengthMM, true);
                widthMM = ApplyVariation(widthMM, false);

                Cv2.PutText(src, $"L: {lengthMM:F2}mm", lengthMidPoint, HersheyFonts.HersheySimplex, 0.55, Scalar.Cyan, 2, LineTypes.AntiAlias);
                Cv2.PutText(src, $"W: {widthMM:F2}mm", widthMidPoint, HersheyFonts.HersheySimplex, 0.55, Scalar.Yellow, 2, LineTypes.AntiAlias);

                // ==========================================================
                // CREATE ISOLATED SHAPE IMAGE FOR LABEL PRINTING
                // ==========================================================
                using (Mat printCanvas = new Mat(src.Size(), MatType.CV_8UC3, Scalar.White))
                {
                    Cv2.Polylines(printCanvas, new[] { smoothContour }, true, Scalar.Black, 3, LineTypes.AntiAlias);
                    OpenCvSharp.Rect cropRect = Cv2.BoundingRect(smoothContour);
                    cropRect.Inflate(15, 15);
                    cropRect.Intersect(new OpenCvSharp.Rect(0, 0, printCanvas.Width, printCanvas.Height));
                    using (Mat croppedForPrint = new Mat(printCanvas, cropRect))
                    {
                        croppedForPrint.ImWrite("ShapeForLabel.png");
                    }
                }

                src.ImWrite("Marquise_Result.png");

                return $"Length : {lengthMM:F2} mm\n" +
                       $"Width  : {widthMM:F2} mm\n" +
                       $"L/W Ratio : {ratio:F2}";
            }
        }

        /*public string MeasureHeartWithOpenCvSharp(Mat src)
        {
            if (src == null || src.Empty()) return "No Shape Found";

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
                Cv2.CvtColor(src, gray, ColorConversionCodes.BGR2GRAY);
                Cv2.GaussianBlur(gray, blur, new Size(5, 5), 0);

                Cv2.Threshold(blur, thresh, 0, 255, ThresholdTypes.BinaryInv | ThresholdTypes.Otsu);

                using (Mat kernel = Cv2.GetStructuringElement(MorphShapes.Ellipse, new Size(3, 3)))
                {
                    Cv2.MorphologyEx(thresh, thresh, MorphTypes.Close, kernel);
                }

                Point[][] contours;
                HierarchyIndex[] hierarchy;
                Cv2.FindContours(thresh, out contours, out hierarchy, RetrievalModes.External, ContourApproximationModes.ApproxSimple);

                if (contours.Length == 0) return "No Shape Found";

                var largestContour = contours.OrderByDescending(c => Cv2.ContourArea(c)).First();

                double epsilon = 0.008 * Cv2.ArcLength(largestContour, true);
                Point[] smoothContour = Cv2.ApproxPolyDP(largestContour, epsilon, true);

                Point topLobePoint = smoothContour[0];
                Point bottomApexPoint = smoothContour[0];
                Point widthLeft = smoothContour[0];
                Point widthRight = smoothContour[0];

                foreach (Point p in smoothContour)
                {
                    if (p.Y < topLobePoint.Y) topLobePoint = p;
                    if (p.Y > bottomApexPoint.Y) bottomApexPoint = p;
                    if (p.X < widthLeft.X) widthLeft = p;
                    if (p.X > widthRight.X) widthRight = p;
                }

                double lengthPx = Math.Abs(bottomApexPoint.Y - topLobePoint.Y);
                double widthPx = Math.Abs(widthRight.X - widthLeft.X);

                double lengthMM = lengthPx / ppm;
                double widthMM = widthPx / ppm;
                double ratio = widthMM == 0 ? 0 : lengthMM / widthMM;

                int middleY = topLobePoint.Y + (int)(lengthPx * 0.35);

                Cv2.Line(src, new Point(bottomApexPoint.X, topLobePoint.Y), bottomApexPoint, Scalar.Red, 1, LineTypes.AntiAlias);
                Cv2.Line(src, new Point(widthLeft.X, middleY), new Point(widthRight.X, middleY), Scalar.Yellow, 1, LineTypes.AntiAlias);
                Cv2.Polylines(src, new[] { smoothContour }, true, Scalar.Lime, 2, LineTypes.AntiAlias);

                lengthMM = ApplyVariation(lengthMM, true);
                widthMM = ApplyVariation(widthMM, false);

                Cv2.PutText(src, $"L: {lengthMM:F2}mm", new Point(bottomApexPoint.X + 15, topLobePoint.Y + 40), HersheyFonts.HersheySimplex, 0.55, Scalar.Cyan, 2, LineTypes.AntiAlias);
                Cv2.PutText(src, $"W: {widthMM:F2}mm", new Point(widthLeft.X + 20, middleY - 15), HersheyFonts.HersheySimplex, 0.55, Scalar.Yellow, 2, LineTypes.AntiAlias);

                // ==========================================================
                // CREATE ISOLATED SHAPE IMAGE FOR LABEL PRINTING
                // ==========================================================
                using (Mat printCanvas = new Mat(src.Size(), MatType.CV_8UC3, Scalar.White))
                {
                    Cv2.Polylines(printCanvas, new[] { smoothContour }, true, Scalar.Black, 3, LineTypes.AntiAlias);
                    OpenCvSharp.Rect cropRect = Cv2.BoundingRect(smoothContour);
                    cropRect.Inflate(15, 15);
                    cropRect.Intersect(new OpenCvSharp.Rect(0, 0, printCanvas.Width, printCanvas.Height));
                    using (Mat croppedForPrint = new Mat(printCanvas, cropRect))
                    {
                        croppedForPrint.ImWrite("ShapeForLabel.png");
                    }
                }

                src.ImWrite("Heart_Result.png");

                return $"Length : {lengthMM:F2} mm\n" +
                       $"Width  : {widthMM:F2} mm\n" +
                       $"L/W Ratio : {ratio:F2}";
            }
        }
        */

        public string MeasureHeartWithOpenCvSharp(Mat src)
        {
            if (src == null || src.Empty()) return "No Shape Found";

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
                Cv2.CvtColor(src, gray, ColorConversionCodes.BGR2GRAY);

                // REDUCED SMOOTHING: Dropped from 5x5 to 3x3 to keep edges crisp
                Cv2.GaussianBlur(gray, blur, new Size(3, 3), 0);

                Cv2.Threshold(blur, thresh, 0, 255, ThresholdTypes.BinaryInv | ThresholdTypes.Otsu);

                // REMOVED: MorphologyEx (Close) has been completely removed. 
                // This prevents the algorithm from sanding down the sharp bottom tip of the heart!

                Point[][] contours;
                HierarchyIndex[] hierarchy;

                // Capture every raw edge pixel for a perfectly smooth outline
                Cv2.FindContours(thresh, out contours, out hierarchy, RetrievalModes.External, ContourApproximationModes.ApproxNone);

                if (contours.Length == 0) return "No Shape Found";

                var largestContour = contours.OrderByDescending(c => Cv2.ContourArea(c)).First();
                if (largestContour.Length < 5) return "Shape too simple";

                // ==========================================================
                // 1. FIND THE LOBES AND THE CLEFT (DIP)
                // ==========================================================
                int[] hullIndices = Cv2.ConvexHullIndices(largestContour);
                Vec4i[] defects = Cv2.ConvexityDefects(largestContour, hullIndices);

                if (defects == null || defects.Length == 0) return "No Heart Cleft Found";

                var maxDefect = defects.OrderByDescending(d => d.Item3).First();
                Point lobe1 = largestContour[maxDefect.Item0];
                Point lobe2 = largestContour[maxDefect.Item1];
                Point cleft = largestContour[maxDefect.Item2];

                // ==========================================================
                // 2. CALCULATE DIP & TOP BASELINE
                // ==========================================================
                // Calculate the vector of the baseline connecting the two top lobes
                double baseDx = lobe2.X - lobe1.X;
                double baseDy = lobe2.Y - lobe1.Y;
                double baseLenSq = baseDx * baseDx + baseDy * baseDy;
                if (baseLenSq == 0) baseLenSq = 1;

                // Project the cleft point up onto the baseline to find the exact top intersection
                double t = ((cleft.X - lobe1.X) * baseDx + (cleft.Y - lobe1.Y) * baseDy) / baseLenSq;
                Point dipTop = new Point(
                    (int)Math.Round(lobe1.X + t * baseDx),
                    (int)Math.Round(lobe1.Y + t * baseDy)
                );

                // The true visual dip in pixels
                double dipPx = Math.Sqrt(Math.Pow(cleft.X - dipTop.X, 2) + Math.Pow(cleft.Y - dipTop.Y, 2));

                // ==========================================================
                // 3. CALCULATE TOTAL LENGTH (Baseline to Apex)
                // ==========================================================
                Point apex = dipTop;
                double maxDistToDipTop = 0;
                foreach (Point p in largestContour)
                {
                    double dist = Math.Sqrt(Math.Pow(p.X - dipTop.X, 2) + Math.Pow(p.Y - dipTop.Y, 2));
                    if (dist > maxDistToDipTop)
                    {
                        maxDistToDipTop = dist;
                        apex = p;
                    }
                }

                // Total Length axis vector 
                double lenDx = apex.X - dipTop.X;
                double lenDy = apex.Y - dipTop.Y;
                double totalLengthPx = Math.Sqrt(lenDx * lenDx + lenDy * lenDy);

                double dirX = lenDx / (totalLengthPx == 0 ? 1 : totalLengthPx);
                double dirY = lenDy / (totalLengthPx == 0 ? 1 : totalLengthPx);

                // ==========================================================
                // 4. CALCULATE WIDTH (Perpendicular to Length)
                // ==========================================================
                double perpX = -dirY;
                double perpY = dirX;

                double minProj = double.MaxValue;
                double maxProj = double.MinValue;
                Point leftPoint = dipTop;
                Point rightPoint = dipTop;

                foreach (Point p in largestContour)
                {
                    double proj = (p.X - dipTop.X) * perpX + (p.Y - dipTop.Y) * perpY;
                    if (proj < minProj) { minProj = proj; leftPoint = p; }
                    if (proj > maxProj) { maxProj = proj; rightPoint = p; }
                }

                double widthPx = maxProj - minProj;

                // Convert Everything to MM
                double lengthMM = totalLengthPx / ppm;
                double widthMM = widthPx / ppm;
                double dipDepthMM = dipPx / ppm;
                double ratio = widthMM == 0 ? 0 : lengthMM / widthMM;

                lengthMM = ApplyVariation(lengthMM, true);
                widthMM = ApplyVariation(widthMM, false);

                // ==========================================================
                // 5. RENDER GRAPHICS
                // ==========================================================
                // Draw Gray Baseline spanning the top lobes for visual reference
                Cv2.Line(src, lobe1, lobe2, Scalar.Gray, 1, LineTypes.AntiAlias);

                // Draw Orange Dip Line (Cleft up to Baseline)
                Cv2.Line(src, cleft, dipTop, Scalar.Orange, 2, LineTypes.AntiAlias);

                // Draw Red Total Length Line (Baseline all the way down to Apex)
                Cv2.Line(src, dipTop, apex, Scalar.Red, 1, LineTypes.AntiAlias);

                // Draw White Width Line
                Cv2.Line(src, leftPoint, rightPoint, Scalar.White, 1, LineTypes.AntiAlias);

                // Draw perfectly smooth green outer contour
                Cv2.Polylines(src, new[] { largestContour }, true, Scalar.Lime, 2, LineTypes.AntiAlias);

                Point lenTextPt = new Point(dipTop.X + 15, dipTop.Y + (int)(lenDy * 0.5));
                Point widTextPt = new Point(leftPoint.X + 15, leftPoint.Y - 15);
                Point dipTextPt = new Point(cleft.X - 35, cleft.Y + 25);

                Cv2.PutText(src, $"Total L: {lengthMM:F2}mm", lenTextPt, HersheyFonts.HersheySimplex, 0.55, Scalar.Cyan, 2, LineTypes.AntiAlias);
                Cv2.PutText(src, $"W: {widthMM:F2}mm", widTextPt, HersheyFonts.HersheySimplex, 0.55, Scalar.Yellow, 2, LineTypes.AntiAlias);
                Cv2.PutText(src, $"Dip: {dipDepthMM:F2}mm", dipTextPt, HersheyFonts.HersheySimplex, 0.55, Scalar.Orange, 2, LineTypes.AntiAlias);

                // ==========================================================
                // 6. CREATE ISOLATED SHAPE IMAGE FOR LABEL PRINTING
                // ==========================================================
                using (Mat printCanvas = new Mat(src.Size(), MatType.CV_8UC3, Scalar.White))
                {
                    Cv2.Polylines(printCanvas, new[] { largestContour }, true, Scalar.Black, 3, LineTypes.AntiAlias);
                    OpenCvSharp.Rect cropRect = Cv2.BoundingRect(largestContour);
                    cropRect.Inflate(15, 15);
                    cropRect.Intersect(new OpenCvSharp.Rect(0, 0, printCanvas.Width, printCanvas.Height));

                    using (Mat croppedForPrint = new Mat(printCanvas, cropRect))
                    {
                        croppedForPrint.ImWrite("ShapeForLabel.png");
                    }
                }

                src.ImWrite("Heart_Result.png");

                return $"Length : {lengthMM:F2} mm\n" +
                       $"Width  : {widthMM:F2} mm\n" +
                       $"Dip    : {dipDepthMM:F2} mm\n" +
                       $"L/W Ratio : {ratio:F2}";
            }
        }

        /*public string MeasurePearWithOpenCvSharpold(Mat src)
        {
            if (src == null || src.Empty()) return "No Shape Found";

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
                Cv2.CvtColor(src, gray, ColorConversionCodes.BGR2GRAY);
                Cv2.GaussianBlur(gray, blur, new Size(5, 5), 0);

                Cv2.Threshold(blur, thresh, 0, 255, ThresholdTypes.BinaryInv | ThresholdTypes.Otsu);

                using (Mat kernel = Cv2.GetStructuringElement(MorphShapes.Ellipse, new Size(3, 3)))
                {
                    Cv2.MorphologyEx(thresh, thresh, MorphTypes.Close, kernel);
                }

                Point[][] contours;
                HierarchyIndex[] hierarchy;
                Cv2.FindContours(thresh, out contours, out hierarchy, RetrievalModes.External, ContourApproximationModes.ApproxSimple);

                if (contours.Length == 0) return "No Shape Found";

                var largestContour = contours.OrderByDescending(c => Cv2.ContourArea(c)).First();

                double epsilon = 0.005 * Cv2.ArcLength(largestContour, true);
                Point[] smoothContour = Cv2.ApproxPolyDP(largestContour, epsilon, true);

                Moments mu = Cv2.Moments(smoothContour);
                Point centroid = new Point((int)(mu.M10 / mu.M00), (int)(mu.M01 / mu.M00));

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

                double dirX = centroid.X - pearTip.X;
                double dirY = centroid.Y - pearTip.Y;
                double lenVector = Math.Sqrt(dirX * dirX + dirY * dirY);
                dirX /= (lenVector == 0 ? 1 : lenVector);
                dirY /= (lenVector == 0 ? 1 : lenVector);

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

                double maxWidthDist = 0;
                Point widthL = new Point(0, 0);
                Point widthR = new Point(0, 0);

                for (int i = 0; i < smoothContour.Length; i++)
                {
                    for (int j = i + 1; j < smoothContour.Length; j++)
                    {
                        double sX = smoothContour[j].X - smoothContour[i].X;
                        double sY = smoothContour[j].Y - smoothContour[i].Y;

                        double perpDistance = Math.Abs(sX * dirY - sY * dirX);
                        if (perpDistance > maxWidthDist)
                        {
                            maxWidthDist = perpDistance;
                            widthL = smoothContour[i];
                            widthR = smoothContour[j];
                        }
                    }
                }

                double lengthMM = maxBaseProjection / ppm;
                double widthMM = maxWidthDist / ppm;
                double ratio = widthMM == 0 ? 0 : lengthMM / widthMM;

                Cv2.Line(src, pearTip, pearBase, Scalar.Red, 1, LineTypes.AntiAlias);
                Cv2.Line(src, widthL, widthR, Scalar.Yellow, 1, LineTypes.AntiAlias);
                Cv2.Circle(src, centroid, 5, Scalar.Orange, -1, LineTypes.AntiAlias);
                Cv2.Polylines(src, new[] { smoothContour }, true, Scalar.Lime, 2, LineTypes.AntiAlias);

                lengthMM = ApplyVariation(lengthMM, true);
                widthMM = ApplyVariation(widthMM, false);

                Cv2.PutText(src, $"L: {lengthMM:F2}mm", new Point(centroid.X + 25, centroid.Y - 20), HersheyFonts.HersheySimplex, 0.55, Scalar.Cyan, 2, LineTypes.AntiAlias);
                Cv2.PutText(src, $"W: {widthMM:F2}mm", new Point(widthL.X + 15, widthL.Y + 25), HersheyFonts.HersheySimplex, 0.55, Scalar.Yellow, 2, LineTypes.AntiAlias);

                // ==========================================================
                // CREATE ISOLATED SHAPE IMAGE FOR LABEL PRINTING
                // ==========================================================
                using (Mat printCanvas = new Mat(src.Size(), MatType.CV_8UC3, Scalar.White))
                {
                    Cv2.Polylines(printCanvas, new[] { smoothContour }, true, Scalar.Black, 3, LineTypes.AntiAlias);
                    OpenCvSharp.Rect cropRect = Cv2.BoundingRect(smoothContour);
                    cropRect.Inflate(15, 15);
                    cropRect.Intersect(new OpenCvSharp.Rect(0, 0, printCanvas.Width, printCanvas.Height));
                    using (Mat croppedForPrint = new Mat(printCanvas, cropRect))
                    {
                        croppedForPrint.ImWrite("ShapeForLabel.png");
                    }
                }

                src.ImWrite("Pear_Result.png");

                return $"Length : {lengthMM:F2} mm\n" +
                       $"Width  : {widthMM:F2} mm\n" +
                       $"L/W Ratio : {ratio:F2}";
            }
        }
        */

        /*public string MeasurePearWithOpenCvSharp(Mat src)
        {
            if (src == null || src.Empty()) return "No Shape Found";

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
                Cv2.CvtColor(src, gray, ColorConversionCodes.BGR2GRAY);
                Cv2.GaussianBlur(gray, blur, new Size(5, 5), 0);

                Cv2.Threshold(blur, thresh, 0, 255, ThresholdTypes.BinaryInv | ThresholdTypes.Otsu);

                using (Mat kernel = Cv2.GetStructuringElement(MorphShapes.Ellipse, new Size(3, 3)))
                {
                    Cv2.MorphologyEx(thresh, thresh, MorphTypes.Close, kernel);
                }

                Point[][] contours;
                HierarchyIndex[] hierarchy;

                // Use ApproxNone to capture every single raw edge pixel for a perfectly smooth outline
                Cv2.FindContours(thresh, out contours, out hierarchy, RetrievalModes.External, ContourApproximationModes.ApproxNone);

                if (contours.Length == 0) return "No Shape Found";

                var largestContour = contours.OrderByDescending(c => Cv2.ContourArea(c)).First();

                // Use ConvexHull as a "rubber band" to snap a smooth, continuous border around the raw pixels
                Point[] hull = Cv2.ConvexHull(largestContour);

                Moments mu = Cv2.Moments(hull);
                Point centroid = new Point((int)(mu.M10 / mu.M00), (int)(mu.M01 / mu.M00));

                // 1. Find the Tip (Furthest point from the centroid)
                Point pearTip = hull[0];
                double maxDistToCentroid = 0;
                foreach (Point p in hull)
                {
                    double dist = Math.Sqrt(Math.Pow(p.X - centroid.X, 2) + Math.Pow(p.Y - centroid.Y, 2));
                    if (dist > maxDistToCentroid)
                    {
                        maxDistToCentroid = dist;
                        pearTip = p;
                    }
                }

                // Establish the exact center axis vector (Tip to Centroid)
                double dirX = centroid.X - pearTip.X;
                double dirY = centroid.Y - pearTip.Y;
                double lenVector = Math.Sqrt(dirX * dirX + dirY * dirY);
                dirX /= (lenVector == 0 ? 1 : lenVector);
                dirY /= (lenVector == 0 ? 1 : lenVector);

                // 2. Find the Base (Exactly 90 degrees down the central axis)
                Point pearBase = centroid;
                double maxBaseProjection = 0;
                foreach (Point p in hull)
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

                // 3. Find the Maximum Width (Perfectly perpendicular to the central axis)
                double maxWidthDist = 0;
                Point widthL = new Point(0, 0);
                Point widthR = new Point(0, 0);

                for (int i = 0; i < hull.Length; i++)
                {
                    for (int j = i + 1; j < hull.Length; j++)
                    {
                        double sX = hull[j].X - hull[i].X;
                        double sY = hull[j].Y - hull[i].Y;

                        double perpDistance = Math.Abs(sX * dirY - sY * dirX);
                        if (perpDistance > maxWidthDist)
                        {
                            maxWidthDist = perpDistance;
                            widthL = hull[i];
                            widthR = hull[j];
                        }
                    }
                }

                // Calculate final millimeters
                double lengthMM = maxBaseProjection / ppm;
                double widthMM = maxWidthDist / ppm;
                double ratio = widthMM == 0 ? 0 : lengthMM / widthMM;

                lengthMM = ApplyVariation(lengthMM, true);
                widthMM = ApplyVariation(widthMM, false);

                // Render crosshairs
                Cv2.Line(src, pearTip, pearBase, Scalar.Red, 1, LineTypes.AntiAlias);
                Cv2.Line(src, widthL, widthR, Scalar.Yellow, 1, LineTypes.AntiAlias);
                Cv2.Circle(src, centroid, 5, Scalar.Orange, -1, LineTypes.AntiAlias);

                // Draw the smooth, un-chopped rubber band hull
                Cv2.Polylines(src, new[] { hull }, true, Scalar.Lime, 2, LineTypes.AntiAlias);

                Cv2.PutText(src, $"L: {lengthMM:F2}mm", new Point(centroid.X + 25, centroid.Y - 20), HersheyFonts.HersheySimplex, 0.55, Scalar.Cyan, 2, LineTypes.AntiAlias);
                Cv2.PutText(src, $"W: {widthMM:F2}mm", new Point(widthL.X + 15, widthL.Y + 25), HersheyFonts.HersheySimplex, 0.55, Scalar.Yellow, 2, LineTypes.AntiAlias);

                // ==========================================================
                // CREATE ISOLATED SHAPE IMAGE FOR LABEL PRINTING
                // ==========================================================
                using (Mat printCanvas = new Mat(src.Size(), MatType.CV_8UC3, Scalar.White))
                {
                    // Draw ONLY the perfectly smooth hull for the printout
                    Cv2.Polylines(printCanvas, new[] { hull }, true, Scalar.Black, 3, LineTypes.AntiAlias);
                    OpenCvSharp.Rect cropRect = Cv2.BoundingRect(hull);
                    cropRect.Inflate(15, 15);
                    cropRect.Intersect(new OpenCvSharp.Rect(0, 0, printCanvas.Width, printCanvas.Height));
                    using (Mat croppedForPrint = new Mat(printCanvas, cropRect))
                    {
                        croppedForPrint.ImWrite("ShapeForLabel.png");
                    }
                }

                src.ImWrite("Pear_Result.png");

                return $"Length : {lengthMM:F2} mm\n" +
                       $"Width  : {widthMM:F2} mm\n" +
                       $"L/W Ratio : {ratio:F2}";
            }
        }*/

        public string MeasurePearWithOpenCvSharp(Mat src)
        {
            if (src == null || src.Empty()) return "No Shape Found";

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
                Cv2.CvtColor(src, gray, ColorConversionCodes.BGR2GRAY);
                Cv2.GaussianBlur(gray, blur, new Size(5, 5), 0);

                Cv2.Threshold(blur, thresh, 0, 255, ThresholdTypes.BinaryInv | ThresholdTypes.Otsu);

                using (Mat kernel = Cv2.GetStructuringElement(MorphShapes.Ellipse, new Size(3, 3)))
                {
                    Cv2.MorphologyEx(thresh, thresh, MorphTypes.Close, kernel);
                }

                Point[][] contours;
                HierarchyIndex[] hierarchy;

                // ApproxNone ensures we get every raw pixel of the boundary
                Cv2.FindContours(thresh, out contours, out hierarchy, RetrievalModes.External, ContourApproximationModes.ApproxNone);

                if (contours.Length == 0) return "No Shape Found";

                // Get the raw pixel boundary of the shape
                var rawContour = contours.OrderByDescending(c => Cv2.ContourArea(c)).First();

                // Calculate moments and centroid from the RAW pixels (no hull smoothing)
                Moments mu = Cv2.Moments(rawContour);
                Point centroid = new Point((int)(mu.M10 / mu.M00), (int)(mu.M01 / mu.M00));

                // 1. Find the Tip (Furthest raw pixel from the centroid)
                Point pearTip = rawContour[0];
                double maxDistToCentroid = 0;
                foreach (Point p in rawContour)
                {
                    double dist = Math.Sqrt(Math.Pow(p.X - centroid.X, 2) + Math.Pow(p.Y - centroid.Y, 2));
                    if (dist > maxDistToCentroid)
                    {
                        maxDistToCentroid = dist;
                        pearTip = p;
                    }
                }

                // Establish the exact center axis vector (Tip to Centroid)
                double dirX = centroid.X - pearTip.X;
                double dirY = centroid.Y - pearTip.Y;
                double lenVector = Math.Sqrt(dirX * dirX + dirY * dirY);
                dirX /= (lenVector == 0 ? 1 : lenVector);
                dirY /= (lenVector == 0 ? 1 : lenVector);

                // 2. Find the Base (Exactly 90 degrees down the central axis on the raw contour)
                Point pearBase = centroid;
                double maxBaseProjection = 0;
                foreach (Point p in rawContour)
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

                // 3. Find the Maximum Width (Perfectly perpendicular to the central axis across the raw contour)
                double maxWidthDist = 0;
                Point widthL = new Point(0, 0);
                Point widthR = new Point(0, 0);

                for (int i = 0; i < rawContour.Length; i++)
                {
                    for (int j = i + 1; j < rawContour.Length; j++)
                    {
                        double sX = rawContour[j].X - rawContour[i].X;
                        double sY = rawContour[j].Y - rawContour[i].Y;

                        double perpDistance = Math.Abs(sX * dirY - sY * dirX);
                        if (perpDistance > maxWidthDist)
                        {
                            maxWidthDist = perpDistance;
                            widthL = rawContour[i];
                            widthR = rawContour[j];
                        }
                    }
                }

                // Calculate final millimeters from exact pixel bounds
                double lengthMM = maxBaseProjection / ppm;
                double widthMM = maxWidthDist / ppm;
                double ratio = widthMM == 0 ? 0 : lengthMM / widthMM;

                lengthMM = ApplyVariation(lengthMM, true);
                widthMM = ApplyVariation(widthMM, false);

                // Render crosshairs
                Cv2.Line(src, pearTip, pearBase, Scalar.Red, 1, LineTypes.AntiAlias);
                Cv2.Line(src, widthL, widthR, Scalar.Yellow, 1, LineTypes.AntiAlias);
                Cv2.Circle(src, centroid, 5, Scalar.Orange, -1, LineTypes.AntiAlias);

                // Draw the exact raw contour (no stretching or smoothing)
                Cv2.Polylines(src, new[] { rawContour }, true, Scalar.Lime, 2, LineTypes.AntiAlias);

                Cv2.PutText(src, $"L: {lengthMM:F2}mm", new Point(centroid.X + 25, centroid.Y - 20), HersheyFonts.HersheySimplex, 0.55, Scalar.Cyan, 2, LineTypes.AntiAlias);
                Cv2.PutText(src, $"W: {widthMM:F2}mm", new Point(widthL.X + 15, widthL.Y + 25), HersheyFonts.HersheySimplex, 0.55, Scalar.Yellow, 2, LineTypes.AntiAlias);

                // ==========================================================
                // CREATE ISOLATED SHAPE IMAGE FOR LABEL PRINTING
                // ==========================================================
                using (Mat printCanvas = new Mat(src.Size(), MatType.CV_8UC3, Scalar.White))
                {
                    // Draw ONLY the exact shape outline in thick black
                    Cv2.Polylines(printCanvas, new[] { rawContour }, true, Scalar.Black, 3, LineTypes.AntiAlias);
                    OpenCvSharp.Rect cropRect = Cv2.BoundingRect(rawContour);
                    cropRect.Inflate(15, 15);
                    cropRect.Intersect(new OpenCvSharp.Rect(0, 0, printCanvas.Width, printCanvas.Height));
                    using (Mat croppedForPrint = new Mat(printCanvas, cropRect))
                    {
                        croppedForPrint.ImWrite("ShapeForLabel.png");
                    }
                }

                src.ImWrite("Pear_Result.png");

                return $"Length : {lengthMM:F2} mm\n" +
                       $"Width  : {widthMM:F2} mm\n" +
                       $"L/W Ratio : {ratio:F2}";
            }
        }

        public string MeasureOvelWithOpenCvSharp(Mat src)
        {
            if (src == null || src.Empty()) return "No Shape Found";

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
                Cv2.CvtColor(src, gray, ColorConversionCodes.BGR2GRAY);
                Cv2.GaussianBlur(gray, blur, new Size(5, 5), 0);

                Cv2.Threshold(blur, thresh, 0, 255, ThresholdTypes.BinaryInv | ThresholdTypes.Otsu);

                using (Mat kernel = Cv2.GetStructuringElement(MorphShapes.Ellipse, new Size(3, 3)))
                {
                    Cv2.MorphologyEx(thresh, thresh, MorphTypes.Close, kernel);
                }

                Point[][] contours;
                HierarchyIndex[] hierarchy;

                Cv2.FindContours(thresh, out contours, out hierarchy, RetrievalModes.External, ContourApproximationModes.ApproxNone);

                if (contours.Length == 0) return "No Shape Found";

                var largestContour = contours.OrderByDescending(c => Cv2.ContourArea(c)).First();

                Point[] hull = Cv2.ConvexHull(largestContour);

                RotatedRect minRect = Cv2.MinAreaRect(hull);

                double lengthPx = Math.Max(minRect.Size.Width, minRect.Size.Height);
                double widthPx = Math.Min(minRect.Size.Width, minRect.Size.Height);

                double lengthMM = lengthPx / ppm;
                double widthMM = widthPx / ppm;
                double ratio = widthMM == 0 ? 0 : lengthMM / widthMM;

                lengthMM = ApplyVariation(lengthMM, true);
                widthMM = ApplyVariation(widthMM, false);

                Point2f[] rectPoints = minRect.Points();
                Point2f p0 = rectPoints[0], p1 = rectPoints[1], p2 = rectPoints[2], p3 = rectPoints[3];

                Point pt01 = new Point((int)Math.Round((p0.X + p1.X) / 2), (int)Math.Round((p0.Y + p1.Y) / 2));
                Point pt12 = new Point((int)Math.Round((p1.X + p2.X) / 2), (int)Math.Round((p1.Y + p2.Y) / 2));
                Point pt23 = new Point((int)Math.Round((p2.X + p3.X) / 2), (int)Math.Round((p2.Y + p3.Y) / 2));
                Point pt30 = new Point((int)Math.Round((p3.X + p0.X) / 2), (int)Math.Round((p3.Y + p0.Y) / 2));

                double dist01_23 = Math.Sqrt(Math.Pow(pt01.X - pt23.X, 2) + Math.Pow(pt01.Y - pt23.Y, 2));
                double dist12_30 = Math.Sqrt(Math.Pow(pt12.X - pt30.X, 2) + Math.Pow(pt12.Y - pt30.Y, 2));

                Point lenStart, lenEnd, widStart, widEnd;
                if (dist01_23 > dist12_30)
                {
                    lenStart = pt01; lenEnd = pt23;
                    widStart = pt12; widEnd = pt30;
                }
                else
                {
                    lenStart = pt12; lenEnd = pt30;
                    widStart = pt01; widEnd = pt23;
                }

                Point center = new Point((int)Math.Round(minRect.Center.X), (int)Math.Round(minRect.Center.Y));

                Cv2.Line(src, lenStart, lenEnd, Scalar.Red, 1, LineTypes.AntiAlias);
                Cv2.Line(src, widStart, widEnd, Scalar.Yellow, 1, LineTypes.AntiAlias);
                Cv2.Circle(src, center, 5, Scalar.Orange, -1, LineTypes.AntiAlias);

                Cv2.Polylines(src, new[] { hull }, true, Scalar.Lime, 2, LineTypes.AntiAlias);

                Cv2.PutText(src, $"L: {lengthMM:F2}mm", new Point(center.X + 25, center.Y - 20), HersheyFonts.HersheySimplex, 0.55, Scalar.Cyan, 2, LineTypes.AntiAlias);
                Cv2.PutText(src, $"W: {widthMM:F2}mm", new Point(widStart.X + 15, widStart.Y + 25), HersheyFonts.HersheySimplex, 0.55, Scalar.Yellow, 2, LineTypes.AntiAlias);

                // ==========================================================
                // CREATE ISOLATED SHAPE IMAGE FOR LABEL PRINTING
                // ==========================================================
                using (Mat printCanvas = new Mat(src.Size(), MatType.CV_8UC3, Scalar.White))
                {
                    // Specifically using 'hull' here to match the drawn outline of this method
                    Cv2.Polylines(printCanvas, new[] { hull }, true, Scalar.Black, 3, LineTypes.AntiAlias);
                    OpenCvSharp.Rect cropRect = Cv2.BoundingRect(hull);
                    cropRect.Inflate(15, 15);
                    cropRect.Intersect(new OpenCvSharp.Rect(0, 0, printCanvas.Width, printCanvas.Height));
                    using (Mat croppedForPrint = new Mat(printCanvas, cropRect))
                    {
                        croppedForPrint.ImWrite("ShapeForLabel.png");
                    }
                }

                src.ImWrite("ovel_Result.png");

                return $"Length : {lengthMM:F2} mm\n" +
                       $"Width  : {widthMM:F2} mm\n" +
                       $"L/W Ratio : {ratio:F2}";
            }
        }

        public string MeasureShapeWithVertexAxis(Mat src)
        {
            if (src == null || src.Empty()) return "No Image Data";

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
                Cv2.CvtColor(src, gray, ColorConversionCodes.BGR2GRAY);
                Cv2.GaussianBlur(gray, blur, new Size(5, 5), 0);

                Cv2.Threshold(blur, thresh, 0, 255, ThresholdTypes.BinaryInv | ThresholdTypes.Otsu);

                using (Mat kernel = Cv2.GetStructuringElement(MorphShapes.Ellipse, new Size(3, 3)))
                {
                    Cv2.MorphologyEx(thresh, thresh, MorphTypes.Close, kernel);
                }

                Point[][] contours;
                HierarchyIndex[] hierarchy;
                Cv2.FindContours(thresh, out contours, out hierarchy, RetrievalModes.External, ContourApproximationModes.ApproxSimple);

                if (contours.Length == 0) return "No Shape Found";

                var largestContour = contours.OrderByDescending(c => Cv2.ContourArea(c)).First();
                Point[] hull = Cv2.ConvexHull(largestContour);

                double epsilon = 0.01 * Cv2.ArcLength(hull, true);
                Point[] smoothContour = Cv2.ApproxPolyDP(hull, epsilon, true);

                Point tip1 = new Point(0, 0);
                Point tip2 = new Point(0, 0);
                double maxTipDist = 0;

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

                double maxWidthDist = 0;
                Point widthPoint1 = new Point(0, 0);
                Point widthPoint2 = new Point(0, 0);

                double axisX = tip2.X - tip1.X;
                double axisY = tip2.Y - tip1.Y;
                double axisLength = Math.Sqrt(axisX * axisX + axisY * axisY);

                for (int i = 0; i < smoothContour.Length; i++)
                {
                    for (int j = i + 1; j < smoothContour.Length; j++)
                    {
                        double sX = smoothContour[j].X - smoothContour[i].X;
                        double sY = smoothContour[j].Y - smoothContour[i].Y;

                        double crossProduct = Math.Abs(sX * axisY - sY * axisX) / (axisLength == 0 ? 1 : axisLength);

                        if (crossProduct > maxWidthDist)
                        {
                            maxWidthDist = crossProduct;
                            widthPoint1 = smoothContour[i];
                            widthPoint2 = smoothContour[j];
                        }
                    }
                }

                double lengthMM = maxTipDist / ppm;
                double widthMM = maxWidthDist / ppm;
                double ratio = widthMM == 0 ? 0 : lengthMM / widthMM;

                Cv2.Line(src, tip1, tip2, Scalar.Red, 1, LineTypes.AntiAlias);
                Cv2.Line(src, widthPoint1, widthPoint2, Scalar.Yellow, 1, LineTypes.AntiAlias);
                Cv2.Polylines(src, new[] { smoothContour }, true, Scalar.Lime, 2, LineTypes.AntiAlias);

                Point lengthMidPoint = new Point((tip1.X + tip2.X) / 2 + 20, (tip1.Y + tip2.Y) / 2 - 15);
                Point widthMidPoint = new Point((widthPoint1.X + widthPoint2.X) / 2 - 80, (widthPoint1.Y + widthPoint2.Y) / 2 + 25);

                lengthMM = ApplyVariation(lengthMM, true);
                widthMM = ApplyVariation(widthMM, false);

                Cv2.PutText(src, $"L: {lengthMM:F2}mm", lengthMidPoint, HersheyFonts.HersheySimplex, 0.55, Scalar.Cyan, 2, LineTypes.AntiAlias);
                Cv2.PutText(src, $"W: {widthMM:F2}mm", widthMidPoint, HersheyFonts.HersheySimplex, 0.55, Scalar.Yellow, 2, LineTypes.AntiAlias);

                // ==========================================================
                // CREATE ISOLATED SHAPE IMAGE FOR LABEL PRINTING
                // ==========================================================
                using (Mat printCanvas = new Mat(src.Size(), MatType.CV_8UC3, Scalar.White))
                {
                    Cv2.Polylines(printCanvas, new[] { smoothContour }, true, Scalar.Black, 3, LineTypes.AntiAlias);
                    OpenCvSharp.Rect cropRect = Cv2.BoundingRect(smoothContour);
                    cropRect.Inflate(15, 15);
                    cropRect.Intersect(new OpenCvSharp.Rect(0, 0, printCanvas.Width, printCanvas.Height));
                    using (Mat croppedForPrint = new Mat(printCanvas, cropRect))
                    {
                        croppedForPrint.ImWrite("ShapeForLabel.png");
                    }
                }

                src.ImWrite("Marquise_Result.png");

                return $"Length : {lengthMM:F2} mm\n" +
                       $"Width  : {widthMM:F2} mm\n" +
                       $"L/W Ratio : {ratio:F2}";
            }
        }

        private double ApplyVariation(double rawMeasurementMM, bool isLength)
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
