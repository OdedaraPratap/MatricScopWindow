using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using OpenCvSharp;

namespace Matric_scope
{
    /// <summary>
    /// Registers measurements against the complete outline of a trained shape.
    /// The old implementation used a convex hull and four rays from the centroid;
    /// that loses concavities and is incorrect when a selected point is not radial.
    /// </summary>
    public class CustomShapeEngine
    {
        public double PixelToMmRatio { get; set; } = 1.0;

        public string MeasureCustomShape(Mat frame, ShapeData activeShape, Mat backgroundGray = null, int thresholdValue = 100)
        {
            if (frame == null || frame.Empty()) return "Error: Empty camera frame";
            if (activeShape == null) return "Error: No Active Shape Selected";

            try
            {
                object regVal = new ModifyRegistry().Read("ppm");
                double ppm;
                if (regVal != null && double.TryParse(regVal.ToString(), out ppm) && ppm > 0)
                    PixelToMmRatio = 1.0 / ppm;
            }
            catch { return "Error: Please do calibration"; }

            OpenCvSharp.Point[] contour;
            Point2f center;
            double angle;
            float span1, span2;
            if (!TryDetectGeometry(frame, backgroundGray, thresholdValue, out contour, out center, out angle, out span1, out span2))
                return "Object not present";

            Point2f[] templateContour = DeserializeContour(activeShape.ContourData);
            int transform = templateContour.Length >= 8
                ? FindBestTransform(templateContour, NormalizeContour(contour, center, angle, span1, span2))
                : 0; // Backward compatibility for shapes saved by older versions.

            Point2f w1 = MapAndSnap(activeShape.WidthPt1, transform, center, angle, span1, span2, contour, activeShape.SnapToEdge);
            Point2f w2 = MapAndSnap(activeShape.WidthPt2, transform, center, angle, span1, span2, contour, activeShape.SnapToEdge);
            Point2f l1 = MapAndSnap(activeShape.LengthPt1, transform, center, angle, span1, span2, contour, activeShape.SnapToEdge);
            Point2f l2 = MapAndSnap(activeShape.LengthPt2, transform, center, angle, span1, span2, contour, activeShape.SnapToEdge);

            double length = ApplyVariation(l1.DistanceTo(l2) * PixelToMmRatio, true);
            double width = ApplyVariation(w1.DistanceTo(w2) * PixelToMmRatio, false);

            // Masking the strokes to the real (non-convex) contour guarantees that
            // an annotation can never be painted beyond the detected object.
            DrawClippedLine(frame, contour, w1, w2, Scalar.Red);
            DrawClippedLine(frame, contour, l1, l2, Scalar.Blue);
            Cv2.Circle(frame, (OpenCvSharp.Point)center, 4, Scalar.Green, -1);
            PutMeasurement(frame, w1, w2, width, Scalar.Red, -10);
            PutMeasurement(frame, l1, l2, length, Scalar.Blue, 20);

            frame.ImWrite("CUSTOMS.png");
            return string.Format(CultureInfo.InvariantCulture, "Length: {0:F2} \nWidth: {1:F2}", length, width);
        }

        private static Point2f MapAndSnap(Point2f point, int transform, Point2f center, double angle,
            float span1, float span2, OpenCvSharp.Point[] contour, bool snapToEdge)
        {
            Point2f projected = ProjectToScreen(Transform(point, transform), center, angle, span1, span2);
            // Preserve intentionally internal training points unless edge snapping was
            // requested, but always rescue a projection that landed outside the object.
            return snapToEdge || Cv2.PointPolygonTest(contour, projected, false) < 0
                ? NearestPointOnContour(projected, contour)
                : projected;
        }

        /// <summary>Detects the actual outline, avoiding border contours and optionally using a clean background.</summary>
        public static bool TryDetectGeometry(Mat frame, Mat background, int thresholdValue,
            out OpenCvSharp.Point[] contour, out Point2f center, out double angle, out float span1, out float span2)
        {
            contour = null; center = new Point2f(); angle = 0; span1 = span2 = 0;
            using (Mat gray = ToGray(frame))
            using (Mat binary = new Mat())
            {
                if (background != null && !background.IsDisposed && !background.Empty() && background.Size() == frame.Size())
                {
                    using (Mat bg = ToGray(background))
                    {
                        Cv2.Absdiff(gray, bg, binary);
                        Cv2.Threshold(binary, binary, Math.Max(5, thresholdValue), 255, ThresholdTypes.Binary);
                    }
                }
                else
                {
                    Cv2.GaussianBlur(gray, gray, new Size(5, 5), 0);
                    Cv2.Canny(gray, binary, 35, 110);
                }

                using (Mat element = Cv2.GetStructuringElement(MorphShapes.Ellipse, new Size(7, 7)))
                    Cv2.MorphologyEx(binary, binary, MorphTypes.Close, element);

                OpenCvSharp.Point[][] contours;
                HierarchyIndex[] hierarchy;
                Cv2.FindContours(binary, out contours, out hierarchy, RetrievalModes.External, ContourApproximationModes.ApproxNone);
                double imageArea = frame.Width * frame.Height;
                contour = contours
                    .Where(c => c.Length >= 8 && Cv2.ContourArea(c) >= Math.Max(200, imageArea * 0.001))
                    .Where(c => !TouchesImageBorder(c, frame.Width, frame.Height))
                    .OrderByDescending(c => ContourScore(c, frame.Width, frame.Height))
                    .FirstOrDefault();

                if (contour == null) return false;
                GetInvariantTransform(contour, out center, out angle, out span1, out span2);
                return span1 > 1 && span2 > 1;
            }
        }

        private static Mat ToGray(Mat source)
        {
            Mat gray = new Mat();
            if (source.Channels() == 1) source.CopyTo(gray);
            else Cv2.CvtColor(source, gray, ColorConversionCodes.BGR2GRAY);
            return gray;
        }

        private static bool TouchesImageBorder(OpenCvSharp.Point[] c, int width, int height)
        {
            Rect r = Cv2.BoundingRect(c);
            return r.X <= 1 || r.Y <= 1 || r.Right >= width - 1 || r.Bottom >= height - 1;
        }

        private static double ContourScore(OpenCvSharp.Point[] c, int width, int height)
        {
            Moments m = Cv2.Moments(c);
            if (Math.Abs(m.M00) < 1e-8) return 0;
            double x = m.M10 / m.M00 - width / 2.0;
            double y = m.M01 / m.M00 - height / 2.0;
            double centrality = 1.0 / (1.0 + Math.Sqrt(x * x + y * y) / Math.Max(width, height));
            return Math.Abs(m.M00) * centrality;
        }

        public static void GetInvariantTransform(OpenCvSharp.Point[] contour, out Point2f centroid,
            out double angle, out float span1, out float span2)
        {
            Moments m = Cv2.Moments(contour);
            if (Math.Abs(m.M00) < 1e-8) throw new ArgumentException("Contour has no area.", "contour");
            centroid = new Point2f((float)(m.M10 / m.M00), (float)(m.M01 / m.M00));
            angle = 0.5 * Math.Atan2(2 * m.Mu11, m.Mu20 - m.Mu02);
            GetSpans(contour, centroid, angle, out span1, out span2);
            if (span2 > span1)
            {
                angle += Math.PI / 2.0;
                GetSpans(contour, centroid, angle, out span1, out span2);
            }
            // PCA is inherently ambiguous by 180 degrees. Template contour matching
            // resolves that ambiguity later; this only makes results deterministic.
            while (angle < 0) angle += Math.PI;
            while (angle >= Math.PI) angle -= Math.PI;
        }

        private static void GetSpans(OpenCvSharp.Point[] contour, Point2f center, double angle,
            out float span1, out float span2)
        {
            double min1 = double.MaxValue, max1 = double.MinValue;
            double min2 = double.MaxValue, max2 = double.MinValue;
            double ca = Math.Cos(angle), sa = Math.Sin(angle);
            foreach (OpenCvSharp.Point p in contour)
            {
                double x = p.X - center.X, y = p.Y - center.Y;
                double a = x * ca + y * sa, b = -x * sa + y * ca;
                min1 = Math.Min(min1, a); max1 = Math.Max(max1, a);
                min2 = Math.Min(min2, b); max2 = Math.Max(max2, b);
            }
            span1 = (float)(max1 - min1); span2 = (float)(max2 - min2);
        }

        public static Point2f ProjectToLocal(Point2f pt, Point2f center, double angle, float span1, float span2)
        {
            double x = pt.X - center.X, y = pt.Y - center.Y;
            double ca = Math.Cos(angle), sa = Math.Sin(angle);
            return new Point2f((float)((x * ca + y * sa) / Math.Max(span1, 1)),
                (float)((-x * sa + y * ca) / Math.Max(span2, 1)));
        }

        public static Point2f ProjectToScreen(Point2f pt, Point2f center, double angle, float span1, float span2)
        {
            double a = pt.X * span1, b = pt.Y * span2;
            double ca = Math.Cos(angle), sa = Math.Sin(angle);
            return new Point2f((float)(center.X + a * ca - b * sa), (float)(center.Y + a * sa + b * ca));
        }

        public static string SerializeContour(OpenCvSharp.Point[] contour, Point2f center, double angle, float span1, float span2)
        {
            Point2f[] normalized = NormalizeContour(contour, center, angle, span1, span2);
            StringBuilder value = new StringBuilder("v2|");
            // At most 256 evenly spaced outline samples keeps the database compact.
            int count = Math.Min(256, normalized.Length);
            for (int i = 0; i < count; i++)
            {
                Point2f p = normalized[i * normalized.Length / count];
                if (i > 0) value.Append(';');
                value.Append(p.X.ToString("R", CultureInfo.InvariantCulture)).Append(',')
                    .Append(p.Y.ToString("R", CultureInfo.InvariantCulture));
            }
            return value.ToString();
        }

        private static Point2f[] DeserializeContour(string value)
        {
            if (string.IsNullOrWhiteSpace(value) || !value.StartsWith("v2|", StringComparison.Ordinal)) return new Point2f[0];
            List<Point2f> points = new List<Point2f>();
            foreach (string pair in value.Substring(3).Split(';'))
            {
                string[] xy = pair.Split(','); float x, y;
                if (xy.Length == 2 && float.TryParse(xy[0], NumberStyles.Float, CultureInfo.InvariantCulture, out x)
                    && float.TryParse(xy[1], NumberStyles.Float, CultureInfo.InvariantCulture, out y))
                    points.Add(new Point2f(x, y));
            }
            return points.ToArray();
        }

        private static Point2f[] NormalizeContour(OpenCvSharp.Point[] contour, Point2f center, double angle, float span1, float span2)
        {
            return contour.Select(p => ProjectToLocal(new Point2f(p.X, p.Y), center, angle, span1, span2)).ToArray();
        }

        private static int FindBestTransform(Point2f[] template, Point2f[] live)
        {
            int best = 0; double bestError = double.MaxValue;
            for (int t = 0; t < 4; t++)
            {
                double error = 0;
                int step = Math.Max(1, template.Length / 96);
                for (int i = 0; i < template.Length; i += step)
                {
                    Point2f p = Transform(template[i], t);
                    double nearest = double.MaxValue;
                    foreach (Point2f q in live)
                    {
                        double dx = p.X - q.X, dy = p.Y - q.Y;
                        nearest = Math.Min(nearest, dx * dx + dy * dy);
                    }
                    error += nearest;
                }
                if (error < bestError) { bestError = error; best = t; }
            }
            return best;
        }

        // PCA axes permit four equivalent representations (180 degree and axis swaps).
        private static Point2f Transform(Point2f p, int transform)
        {
            switch (transform)
            {
                case 1: return new Point2f(-p.X, -p.Y);
                case 2: return new Point2f(p.Y, -p.X);
                case 3: return new Point2f(-p.Y, p.X);
                default: return p;
            }
        }

        public static Point2f NearestPointOnContour(Point2f point, OpenCvSharp.Point[] contour)
        {
            Point2f best = point; double bestDistance = double.MaxValue;
            for (int i = 0; i < contour.Length; i++)
            {
                Point2f a = contour[i], b = contour[(i + 1) % contour.Length];
                double dx = b.X - a.X, dy = b.Y - a.Y;
                double denominator = dx * dx + dy * dy;
                double t = denominator < 1e-8 ? 0 : ((point.X - a.X) * dx + (point.Y - a.Y) * dy) / denominator;
                t = Math.Max(0, Math.Min(1, t));
                Point2f candidate = new Point2f((float)(a.X + t * dx), (float)(a.Y + t * dy));
                double ex = point.X - candidate.X, ey = point.Y - candidate.Y;
                double distance = ex * ex + ey * ey;
                if (distance < bestDistance) { bestDistance = distance; best = candidate; }
            }
            return best;
        }

        private static void DrawClippedLine(Mat frame, OpenCvSharp.Point[] contour, Point2f a, Point2f b, Scalar color)
        {
            using (Mat overlay = frame.Clone())
            using (Mat mask = Mat.Zeros(frame.Size(), MatType.CV_8UC1))
            using (Mat lineMask = Mat.Zeros(frame.Size(), MatType.CV_8UC1))
            using (Mat combined = new Mat())
            {
                Cv2.Line(overlay, (OpenCvSharp.Point)a, (OpenCvSharp.Point)b, color, 2, LineTypes.AntiAlias);
                Cv2.Line(lineMask, (OpenCvSharp.Point)a, (OpenCvSharp.Point)b, Scalar.White, 2, LineTypes.AntiAlias);
                Cv2.FillPoly(mask, new[] { contour }, Scalar.White);
                Cv2.BitwiseAnd(mask, lineMask, combined);
                overlay.CopyTo(frame, combined);
            }
        }

        private static void PutMeasurement(Mat frame, Point2f a, Point2f b, double value, Scalar color, int yOffset)
        {
            OpenCvSharp.Point midpoint = new OpenCvSharp.Point((a.X + b.X) / 2 + 10, (a.Y + b.Y) / 2 + yOffset);
            Cv2.PutText(frame, value.ToString("F2", CultureInfo.InvariantCulture) + " mm", midpoint,
                HersheyFonts.HersheySimplex, 0.7, color, 2, LineTypes.AntiAlias);
        }

        private static double ApplyVariation(double measurement, bool isLength)
        {
            int index = Math.Max(0, Math.Min(24, (int)Math.Floor(measurement)));
            try
            {
                string value = new ModifyRegistry().Read((isLength ? "LenVar_" : "WidVar_") + index);
                double variation;
                if (double.TryParse(value, out variation)) return measurement + variation;
            }
            catch { }
            return measurement;
        }
    }
}
