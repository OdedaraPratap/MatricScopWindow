using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Matric_scope
{
    public class PentagonMeasurement
    {
        private class FittedLine
        {
            public Point2d P;
            public Point2d D;
        }

        public static string Measure(Mat src, Action<Mat> captureWithoutAngles = null)
        {
            if (src == null || src.Empty())
                return "No Image Data";

            double ppm;
            try
            {
                ppm = Convert.ToDouble(new ModifyRegistry().Read("ppm"));
            }
            catch
            {
                return "Calibration Error";
            }

            if (ppm <= 0)
                return "Please Calibrate First";

            using (Mat gray = new Mat())
            using (Mat blur = new Mat())
            using (Mat thresh = new Mat())
            {
                // ============================================================
                // 1. PREPROCESSING
                // ============================================================
                if (src.Channels() == 1)
                    src.CopyTo(gray);
                else
                    Cv2.CvtColor(src, gray, ColorConversionCodes.BGR2GRAY);

                Cv2.GaussianBlur(gray, blur, new Size(3, 3), 0);

                // Black/dark object on light background.
                Cv2.Threshold(
                    blur,
                    thresh,
                    0,
                    255,
                    ThresholdTypes.BinaryInv | ThresholdTypes.Otsu);

                // Close only tiny gaps. Do not OPEN, because opening can shorten tips.
                using (Mat kernel = Cv2.GetStructuringElement(
                    MorphShapes.Ellipse,
                    new Size(3, 3)))
                {
                    Cv2.MorphologyEx(thresh, thresh, MorphTypes.Close, kernel);
                }

                // ============================================================
                // 2. FIND MAIN CONTOUR
                // ============================================================
                Cv2.FindContours(
                    thresh,
                    out Point[][] contours,
                    out HierarchyIndex[] hierarchy,
                    RetrievalModes.External,
                    ContourApproximationModes.ApproxNone);

                if (contours == null || contours.Length == 0)
                    return "No Pentagon Found";

                Point[] contour = contours
                    .OrderByDescending(c => Math.Abs(Cv2.ContourArea(c)))
                    .First();

                double area = Math.Abs(Cv2.ContourArea(contour));
                if (area < 800)
                    return "No Object Detected (Noise Ignored)";

                Point[] hull = Cv2.ConvexHull(contour);
                if (hull == null || hull.Length < 5)
                    return "Invalid Pentagon";

                double perimeter = Cv2.ArcLength(hull, true);

                // ============================================================
                // 3. GET EXACTLY 5 ROUGH CORNERS
                // ============================================================
                Point2d[] rough = GetFiveCornerPolygon(hull);
                if (rough == null || rough.Length != 5)
                    return "Pentagon corners not found";

                rough = SortAroundCenter(rough);

                // ============================================================
                // 4. FIT EACH SIDE TO THE REAL CONTOUR PIXELS
                // ============================================================
                double sideBand = Math.Max(2.5, perimeter * 0.0075);
                FittedLine[] lines = new FittedLine[5];

                for (int i = 0; i < 5; i++)
                {
                    Point2d a = rough[i];
                    Point2d b = rough[(i + 1) % 5];

                    List<Point2d> sidePoints = CollectSidePoints(
                        contour,
                        a,
                        b,
                        sideBand);

                    lines[i] = sidePoints.Count >= 6
                        ? FitLinePca(sidePoints)
                        : CreateLine(a, b);
                }

                // ============================================================
                // 5. INTERSECT ADJACENT SIDE LINES -> CLEAN 5 VERTICES
                // ============================================================
                Point2d[] fitted = new Point2d[5];
                double maxCornerShift = Math.Max(10.0, perimeter * 0.055);

                for (int i = 0; i < 5; i++)
                {
                    int previousLine = (i + 4) % 5;
                    int currentLine = i;

                    if (IntersectLines(
                            lines[previousLine],
                            lines[currentLine],
                            out Point2d intersection)
                        && Distance(intersection, rough[i]) <= maxCornerShift)
                    {
                        fitted[i] = intersection;
                    }
                    else
                    {
                        fitted[i] = rough[i];
                    }
                }

                fitted = SortAroundCenter(fitted);

                // ============================================================
                // 6. YOUR EXACT MEASUREMENT DEFINITION
                //
                // From your marked image:
                //
                //   BLUE   = WIDTH
                //            the longest OUTER pentagon side
                //
                //   RED    = HEIGHT
                //            perpendicular distance from the opposite/farthest
                //            pentagon vertex to the BLUE width line
                //
                //   YELLOW = SHOULDER
                //            chord between the two vertices immediately next
                //            to the HEIGHT vertex
                //
                // This is fully rotation independent.
                // ============================================================

                // ------------------------------------------------------------
                // 6A. WIDTH SIDE = LONGEST OF THE 5 OUTER SIDES
                // ------------------------------------------------------------
                int widthSideIndex = 0;
                double widthPx = -1.0;

                for (int i = 0; i < 5; i++)
                {
                    double d = Distance(fitted[i], fitted[(i + 1) % 5]);
                    if (d > widthPx)
                    {
                        widthPx = d;
                        widthSideIndex = i;
                    }
                }

                // Reorder so SIDE 1 is always the WIDTH side.
                Point2d[] p = new Point2d[5];
                for (int i = 0; i < 5; i++)
                    p[i] = fitted[(widthSideIndex + i) % 5];

                Point2d widthA = p[0];
                Point2d widthB = p[1];

                // ------------------------------------------------------------
                // 6B. HEIGHT VERTEX = FARTHEST REMAINING VERTEX FROM WIDTH LINE
                // ------------------------------------------------------------
                int heightVertexIndex = -1;
                double heightPx = -1.0;

                // p[0] and p[1] belong to the width side, so only test 2..4.
                for (int i = 2; i < 5; i++)
                {
                    double d = DistancePointToLine(p[i], widthA, widthB);
                    if (d > heightPx)
                    {
                        heightPx = d;
                        heightVertexIndex = i;
                    }
                }

                if (heightVertexIndex < 0)
                    return "Pentagon height vertex not found";

                Point2d heightVertex = p[heightVertexIndex];

                // Exact perpendicular foot on the width line.
                Point2d heightFoot = ProjectPointToLine(
                    heightVertex,
                    widthA,
                    widthB,
                    out double heightProjectionT);

                // In the expected pentagon geometry the foot is on the blue segment.
                // Clamp tiny numerical/outlier drift so the drawn height terminates on
                // the actual width segment, not an infinite extension.
                if (heightProjectionT < 0.0 || heightProjectionT > 1.0)
                {
                    double clampedT = Math.Max(0.0, Math.Min(1.0, heightProjectionT));
                    heightFoot = new Point2d(
                        widthA.X + (widthB.X - widthA.X) * clampedT,
                        widthA.Y + (widthB.Y - widthA.Y) * clampedT);

                    heightPx = Distance(heightVertex, heightFoot);
                }

                // ------------------------------------------------------------
                // 6C. SHOULDER = NEIGHBOR-TO-NEIGHBOR CHORD AROUND HEIGHT VERTEX
                //
                // This is exactly the yellow line in your marked image.
                // ------------------------------------------------------------
                int shoulderIndex1 = (heightVertexIndex + 4) % 5;
                int shoulderIndex2 = (heightVertexIndex + 1) % 5;

                Point2d shoulderA = p[shoulderIndex1];
                Point2d shoulderB = p[shoulderIndex2];

                double shoulderPx = Distance(shoulderA, shoulderB);

                // ============================================================
                // 7. ALL 5 OUTER SIDES AND 5 INTERNAL ANGLES
                // ============================================================
                double[] sidePx = new double[5];
                double[] angleDeg = new double[5];

                for (int i = 0; i < 5; i++)
                {
                    sidePx[i] = Distance(p[i], p[(i + 1) % 5]);

                    angleDeg[i] = InteriorAngle(
                        p[(i + 4) % 5],
                        p[i],
                        p[(i + 1) % 5]);
                }

                double widthMM = widthPx / ppm;
                double heightMM = heightPx / ppm;
                double shoulderMM = shoulderPx / ppm;

                double[] sideMM = sidePx
                    .Select(v => v / ppm)
                    .ToArray();

                // ============================================================
                // 8. DRAW GREEN OUTER 5 SIDES FIRST
                // ============================================================
                for (int i = 0; i < 5; i++)
                {
                    Point2d a = p[i];
                    Point2d b = p[(i + 1) % 5];

                    DrawLine(src, a, b, Scalar.Lime, 2);

                    Point sideText = GetSideTextPosition(p, a, b);

                    DrawingTextSettings.PutText(
                        src,
                        string.Format(
                            CultureInfo.InvariantCulture,
                            "S{0}:{1:F2}mm",
                            i + 1,
                            sideMM[i]),
                        sideText,
                        HersheyFonts.HersheySimplex,
                        0.45,
                        Scalar.Lime,
                        1,
                        LineTypes.AntiAlias);
                }

                // ============================================================
                // 9. DRAW YOUR THREE PRIMARY MEASUREMENTS
                //
                // BLUE   WIDTH
                // RED    HEIGHT
                // YELLOW SHOULDER
                // ============================================================

                // WIDTH: exact longest outer side.
                DrawLine(src, widthA, widthB, Scalar.Blue, 4);

                Cv2.Circle(src, ToPoint(widthA), 5, Scalar.Blue, -1, LineTypes.AntiAlias);
                Cv2.Circle(src, ToPoint(widthB), 5, Scalar.Blue, -1, LineTypes.AntiAlias);

                Point2d widthMid = MidPoint(widthA, widthB);
                DrawTextWithBackground(
                    src,
                    string.Format(CultureInfo.InvariantCulture, "W:{0:F2}mm", widthMM),
                    OffsetPerpendicular(widthMid, widthA, widthB, -18.0),
                    Scalar.Blue);

                // HEIGHT: exact perpendicular from opposite/farthest vertex to width line.
                DrawLine(src, heightVertex, heightFoot, Scalar.Red, 4);

                Cv2.Circle(src, ToPoint(heightVertex), 5, Scalar.Red, -1, LineTypes.AntiAlias);
                Cv2.Circle(src, ToPoint(heightFoot), 5, Scalar.Red, -1, LineTypes.AntiAlias);

                Point2d heightMid = MidPoint(heightVertex, heightFoot);
                DrawTextWithBackground(
                    src,
                    string.Format(CultureInfo.InvariantCulture, "H:{0:F2}mm", heightMM),
                    OffsetPerpendicular(heightMid, heightVertex, heightFoot, 18.0),
                    Scalar.Red);

                // SHOULDER: chord joining the two neighbours of the height vertex.
                DrawLine(src, shoulderA, shoulderB, Scalar.Yellow, 4);

                Cv2.Circle(src, ToPoint(shoulderA), 5, Scalar.Yellow, -1, LineTypes.AntiAlias);
                Cv2.Circle(src, ToPoint(shoulderB), 5, Scalar.Yellow, -1, LineTypes.AntiAlias);

                Point2d shoulderMid = MidPoint(shoulderA, shoulderB);
                DrawTextWithBackground(
                    src,
                    string.Format(CultureInfo.InvariantCulture, "SH:{0:F2}mm", shoulderMM),
                    OffsetPerpendicular(shoulderMid, shoulderA, shoulderB, 18.0),
                    Scalar.Yellow);

                // Capture the non-angle drawing for immediate checkbox toggling.
                if (captureWithoutAngles != null) captureWithoutAngles(src);

                // ============================================================
                // 10. DRAW ALL 5 INTERNAL ANGLES
                // ============================================================
                for (int i = 0; i < 5; i++)
                {
                    Point vertex = ToPoint(p[i]);

                    Cv2.Circle(
                        src,
                        vertex,
                        4,
                        Scalar.Magenta,
                        -1,
                        LineTypes.AntiAlias);

                    Point angleText = GetAngleTextPosition(p, i);

                    DrawingTextSettings.PutText(
                        src,
                        string.Format(
                            CultureInfo.InvariantCulture,
                            "A{0}:{1:F1}",
                            i + 1,
                            angleDeg[i]),
                        angleText,
                        HersheyFonts.HersheySimplex,
                        0.42,
                        Scalar.Cyan,
                        1,
                        LineTypes.AntiAlias);
                }

                // Save print shape + debug result.
                CreateShapeForLabel(src.Size(), hull);
                src.ImWrite("result.png");

                // ============================================================
                // 11. RETURN STRING FOR YOUR EXISTING PENTAGON UI
                // ============================================================
                StringBuilder result = new StringBuilder();

                result.AppendLine("Detected Shape: Pentagon");
                result.AppendLine(string.Format(CultureInfo.InvariantCulture, "Height: {0:F2} mm", heightMM));
                result.AppendLine(string.Format(CultureInfo.InvariantCulture, "Width: {0:F2} mm", widthMM));
                result.AppendLine(string.Format(CultureInfo.InvariantCulture, "Shoulder: {0:F2} mm", shoulderMM));

                for (int i = 0; i < 5; i++)
                {
                    result.AppendLine(string.Format(
                        CultureInfo.InvariantCulture,
                        "Side {0}: {1:F2} mm",
                        i + 1,
                        sideMM[i]));
                }

                for (int i = 0; i < 5; i++)
                {
                    result.AppendLine(string.Format(
                        CultureInfo.InvariantCulture,
                        "Angle {0}: {1:F2} deg",
                        i + 1,
                        angleDeg[i]));
                }

                return result.ToString();
            }
        }

        // ====================================================================
        // PROJECT A POINT ONTO AN INFINITE LINE A->B
        // ====================================================================
        private static Point2d ProjectPointToLine(
            Point2d p,
            Point2d a,
            Point2d b,
            out double t)
        {
            double vx = b.X - a.X;
            double vy = b.Y - a.Y;
            double lengthSquared = vx * vx + vy * vy;

            if (lengthSquared < 0.000001)
            {
                t = 0.0;
                return a;
            }

            t = ((p.X - a.X) * vx + (p.Y - a.Y) * vy) / lengthSquared;

            return new Point2d(
                a.X + t * vx,
                a.Y + t * vy);
        }

        private static double DistancePointToLine(
            Point2d p,
            Point2d a,
            Point2d b)
        {
            double vx = b.X - a.X;
            double vy = b.Y - a.Y;
            double length = Math.Sqrt(vx * vx + vy * vy);

            if (length < 0.000001)
                return 0.0;

            return Math.Abs(
                vx * (p.Y - a.Y) -
                vy * (p.X - a.X)) / length;
        }

        private static Point2d MidPoint(Point2d a, Point2d b)
        {
            return new Point2d(
                (a.X + b.X) / 2.0,
                (a.Y + b.Y) / 2.0);
        }

        private static Point OffsetPerpendicular(
            Point2d mid,
            Point2d a,
            Point2d b,
            double offset)
        {
            double vx = b.X - a.X;
            double vy = b.Y - a.Y;
            double len = Math.Sqrt(vx * vx + vy * vy);

            if (len < 0.000001)
                return ToPoint(mid);

            // Perpendicular unit vector.
            double nx = -vy / len;
            double ny = vx / len;

            return ToPoint(new Point2d(
                mid.X + nx * offset,
                mid.Y + ny * offset));
        }

        // ====================================================================
        // DRAW LEGIBLE TEXT WITH DARK BACKGROUND
        // ====================================================================
        private static void DrawTextWithBackground(
            Mat image,
            string text,
            Point origin,
            Scalar textColor)
        {
            const double scale = 0.52;
            const int thickness = 1;
            int baseline;

            OpenCvSharp.Size textSize = Cv2.GetTextSize(
                text,
                HersheyFonts.HersheySimplex,
                scale,
                thickness,
                out baseline);

            int x = Math.Max(2, Math.Min(origin.X, image.Width - textSize.Width - 8));
            int y = Math.Max(textSize.Height + 6, Math.Min(origin.Y, image.Height - baseline - 4));

            OpenCvSharp.Rect bg = new OpenCvSharp.Rect(
                x - 3,
                y - textSize.Height - 3,
                textSize.Width + 6,
                textSize.Height + baseline + 6);

            bg.Intersect(new OpenCvSharp.Rect(0, 0, image.Width, image.Height));

            if (bg.Width > 0 && bg.Height > 0)
                Cv2.Rectangle(image, bg, Scalar.Black, -1);

            DrawingTextSettings.PutText(
                image,
                text,
                new Point(x, y),
                HersheyFonts.HersheySimplex,
                scale,
                textColor,
                thickness,
                LineTypes.AntiAlias);
        }

        // ====================================================================
        // GET EXACTLY 5 CORNERS
        // ====================================================================
        private static Point2d[] GetFiveCornerPolygon(Point[] hull)
        {
            double perimeter = Cv2.ArcLength(hull, true);

            Point[] best = null;
            int consecutiveFive = 0;

            for (double eps = 0.003; eps <= 0.045; eps += 0.00075)
            {
                Point[] approx = Cv2.ApproxPolyDP(hull, perimeter * eps, true);

                if (approx.Length == 5)
                {
                    consecutiveFive++;
                    best = approx;

                    if (consecutiveFive >= 2)
                    {
                        return best
                            .Select(q => new Point2d(q.X, q.Y))
                            .ToArray();
                    }
                }
                else
                {
                    consecutiveFive = 0;
                }
            }

            if (best != null)
            {
                return best
                    .Select(q => new Point2d(q.X, q.Y))
                    .ToArray();
            }

            // Fallback: retain detail then remove tiny/least-important corners.
            Point[] initial = Cv2.ApproxPolyDP(hull, perimeter * 0.0025, true);

            List<Point2d> points = initial
                .Select(q => new Point2d(q.X, q.Y))
                .ToList();

            if (points.Count < 5)
                return null;

            while (points.Count > 5)
            {
                int removeIndex = -1;
                double smallestImportance = double.MaxValue;

                for (int i = 0; i < points.Count; i++)
                {
                    Point2d previous = points[(i - 1 + points.Count) % points.Count];
                    Point2d current = points[i];
                    Point2d next = points[(i + 1) % points.Count];

                    double importance = TriangleArea(previous, current, next);

                    if (importance < smallestImportance)
                    {
                        smallestImportance = importance;
                        removeIndex = i;
                    }
                }

                if (removeIndex < 0)
                    break;

                points.RemoveAt(removeIndex);
            }

            return points.Count == 5 ? points.ToArray() : null;
        }

        private static Point2d[] SortAroundCenter(Point2d[] points)
        {
            double cx = points.Average(q => q.X);
            double cy = points.Average(q => q.Y);

            return points
                .OrderBy(q => Math.Atan2(q.Y - cy, q.X - cx))
                .ToArray();
        }

        // ====================================================================
        // COLLECT ACTUAL CONTOUR POINTS NEAR ONE SIDE
        // ====================================================================
        private static List<Point2d> CollectSidePoints(
            Point[] contour,
            Point2d a,
            Point2d b,
            double band)
        {
            List<Point2d> result = new List<Point2d>();

            double vx = b.X - a.X;
            double vy = b.Y - a.Y;
            double lengthSquared = vx * vx + vy * vy;

            if (lengthSquared < 0.000001)
                return result;

            double lineLength = Math.Sqrt(lengthSquared);

            foreach (Point q in contour)
            {
                double px = q.X - a.X;
                double py = q.Y - a.Y;

                double t = (px * vx + py * vy) / lengthSquared;

                // Exclude corner neighbourhoods so adjacent sides do not skew fit.
                if (t < 0.10 || t > 0.90)
                    continue;

                double perpendicularDistance =
                    Math.Abs(vx * py - vy * px) / lineLength;

                if (perpendicularDistance <= band)
                    result.Add(new Point2d(q.X, q.Y));
            }

            return result;
        }

        private static FittedLine FitLinePca(List<Point2d> points)
        {
            double meanX = points.Average(q => q.X);
            double meanY = points.Average(q => q.Y);

            double xx = 0.0;
            double yy = 0.0;
            double xy = 0.0;

            foreach (Point2d q in points)
            {
                double dx = q.X - meanX;
                double dy = q.Y - meanY;

                xx += dx * dx;
                yy += dy * dy;
                xy += dx * dy;
            }

            double angle = 0.5 * Math.Atan2(2.0 * xy, xx - yy);

            return new FittedLine
            {
                P = new Point2d(meanX, meanY),
                D = Normalize(new Point2d(Math.Cos(angle), Math.Sin(angle)))
            };
        }

        private static FittedLine CreateLine(Point2d a, Point2d b)
        {
            return new FittedLine
            {
                P = a,
                D = Normalize(new Point2d(b.X - a.X, b.Y - a.Y))
            };
        }

        private static bool IntersectLines(
            FittedLine a,
            FittedLine b,
            out Point2d intersection)
        {
            double denominator = Cross(a.D, b.D);

            if (Math.Abs(denominator) < 0.000001)
            {
                intersection = new Point2d();
                return false;
            }

            Point2d difference = new Point2d(
                b.P.X - a.P.X,
                b.P.Y - a.P.Y);

            double t = Cross(difference, b.D) / denominator;

            intersection = new Point2d(
                a.P.X + t * a.D.X,
                a.P.Y + t * a.D.Y);

            return true;
        }

        private static double InteriorAngle(
            Point2d previous,
            Point2d center,
            Point2d next)
        {
            Point2d v1 = new Point2d(
                previous.X - center.X,
                previous.Y - center.Y);

            Point2d v2 = new Point2d(
                next.X - center.X,
                next.Y - center.Y);

            double len1 = VectorLength(v1);
            double len2 = VectorLength(v2);

            if (len1 < 0.000001 || len2 < 0.000001)
                return 0.0;

            double cosine = Dot(v1, v2) / (len1 * len2);
            cosine = Math.Max(-1.0, Math.Min(1.0, cosine));

            return Math.Acos(cosine) * 180.0 / Math.PI;
        }

        private static double TriangleArea(Point2d a, Point2d b, Point2d c)
        {
            return Math.Abs(
                (a.X * (b.Y - c.Y) +
                 b.X * (c.Y - a.Y) +
                 c.X * (a.Y - b.Y)) / 2.0);
        }

        private static double Distance(Point2d a, Point2d b)
        {
            double dx = b.X - a.X;
            double dy = b.Y - a.Y;
            return Math.Sqrt(dx * dx + dy * dy);
        }

        private static double VectorLength(Point2d v)
        {
            return Math.Sqrt(v.X * v.X + v.Y * v.Y);
        }

        private static Point2d Normalize(Point2d v)
        {
            double length = VectorLength(v);

            if (length < 0.000001)
                return new Point2d(1.0, 0.0);

            return new Point2d(v.X / length, v.Y / length);
        }

        private static double Dot(Point2d a, Point2d b)
        {
            return a.X * b.X + a.Y * b.Y;
        }

        private static double Cross(Point2d a, Point2d b)
        {
            return a.X * b.Y - a.Y * b.X;
        }

        private static Point ToPoint(Point2d p)
        {
            return new Point(
                (int)Math.Round(p.X),
                (int)Math.Round(p.Y));
        }

        private static void DrawLine(
            Mat image,
            Point2d a,
            Point2d b,
            Scalar color,
            int thickness)
        {
            Cv2.Line(
                image,
                ToPoint(a),
                ToPoint(b),
                color,
                thickness,
                LineTypes.AntiAlias);
        }

        private static Point GetSideTextPosition(
            Point2d[] polygon,
            Point2d a,
            Point2d b)
        {
            double cx = polygon.Average(q => q.X);
            double cy = polygon.Average(q => q.Y);

            double mx = (a.X + b.X) / 2.0;
            double my = (a.Y + b.Y) / 2.0;

            double dx = mx - cx;
            double dy = my - cy;
            double length = Math.Sqrt(dx * dx + dy * dy);

            if (length > 0.001)
            {
                dx /= length;
                dy /= length;
            }

            const double offset = 12.0;

            return new Point(
                (int)Math.Round(mx + dx * offset),
                (int)Math.Round(my + dy * offset));
        }

        private static Point GetAngleTextPosition(Point2d[] polygon, int index)
        {
            double cx = polygon.Average(q => q.X);
            double cy = polygon.Average(q => q.Y);

            Point2d vertex = polygon[index];

            double dx = cx - vertex.X;
            double dy = cy - vertex.Y;
            double length = Math.Sqrt(dx * dx + dy * dy);

            if (length > 0.001)
            {
                dx /= length;
                dy /= length;
            }

            return new Point(
                (int)Math.Round(vertex.X + dx * 18),
                (int)Math.Round(vertex.Y + dy * 18));
        }

        private static void CreateShapeForLabel(
            OpenCvSharp.Size imageSize,
            Point[] hull)
        {
            using (Mat printCanvas = new Mat(
                imageSize,
                MatType.CV_8UC3,
                Scalar.White))
            {
                Cv2.Polylines(
                    printCanvas,
                    new[] { hull },
                    true,
                    Scalar.Black,
                    3,
                    LineTypes.AntiAlias);

                OpenCvSharp.Rect cropRect = Cv2.BoundingRect(hull);
                cropRect.Inflate(15, 15);
                cropRect.Intersect(new OpenCvSharp.Rect(
                    0,
                    0,
                    printCanvas.Width,
                    printCanvas.Height));

                if (cropRect.Width > 0 && cropRect.Height > 0)
                {
                    using (Mat cropped = new Mat(printCanvas, cropRect))
                    {
                        cropped.ImWrite("ShapeForLabel.png");
                    }
                }
            }
        }
    }
}
