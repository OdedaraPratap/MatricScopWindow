using OpenCvSharp;
using System;
using System.Linq;

namespace Matric_scope
{
    // ================================================================
    // AUTO DETECTED SHAPE
    // ================================================================
    public enum AutoDetectedShape
    {
        Unknown,
        Round,
        Pear,
        Oval,
        Heart,
        Marquise,
        Polygon,
        General,
        GeneralC
    }


    // ================================================================
    // AUTO SHAPE RESULT
    // ================================================================
    public class AutoShapeResult
    {
        public AutoDetectedShape Shape { get; set; }

        public double AspectRatio { get; set; }

        public double Circularity { get; set; }

        public double Solidity { get; set; }

        public int ApproxCorners { get; set; }

        public int SharpCorners { get; set; }

        public double EndWidth1 { get; set; }

        public double EndWidth2 { get; set; }

        public double HeartDefectRatio { get; set; }


        public string Name
        {
            get
            {
                switch (Shape)
                {
                    case AutoDetectedShape.Round:
                        return "Round";

                    case AutoDetectedShape.Pear:
                        return "Pear";

                    case AutoDetectedShape.Oval:
                        return "Oval";

                    case AutoDetectedShape.Heart:
                        return "Heart";

                    case AutoDetectedShape.Marquise:
                        return "Marquise";

                    case AutoDetectedShape.Polygon:
                        return "Polygon";

                    case AutoDetectedShape.General:
                        return "General";

                    case AutoDetectedShape.GeneralC:
                        return "GeneralC";

                    default:
                        return "Unknown";
                }
            }
        }
    }


    // Black stone on a light background. Pass a clean, unannotated frame.
    // Thresholds are silhouette heuristics; tune against real camera samples.
    public class AutoShapeDetector
    {
        public double RoundMaxAspectRatio { get; set; } = 1.03;
        public double RoundMaxCircleError { get; set; } = 0.015;

        public double MinimumArea { get; set; } = 80.0;
        public double MinimumAreaFraction { get; set; } = 0.00005;
        public bool RejectBorderObjects { get; set; } = true;
        public bool CloseSmallGaps { get; set; } = false;

        public AutoShapeResult Detect(Mat src)
        {
            var r = new AutoShapeResult { Shape = AutoDetectedShape.Unknown };
            if (src == null || src.Empty()) return r;
            if (src.Depth() != MatType.CV_8U)
                throw new ArgumentException("Detector requires an 8-bit image.", "src");

            using (var gray = new Mat())
            using (var blur = new Mat())
            using (var mask = new Mat())
            {
                switch (src.Channels())
                {
                    case 1: src.CopyTo(gray); break;
                    case 3: Cv2.CvtColor(src, gray, ColorConversionCodes.BGR2GRAY); break;
                    case 4: Cv2.CvtColor(src, gray, ColorConversionCodes.BGRA2GRAY); break;
                    default: throw new ArgumentException("Use gray, BGR or BGRA input.", "src");
                }
                Cv2.GaussianBlur(gray, blur, new Size(3, 3), 0);
                Cv2.Threshold(blur, mask, 0, 255,
                    ThresholdTypes.BinaryInv | ThresholdTypes.Otsu);
                // Closing is optional: it can erase a shallow heart cleft.
                if (CloseSmallGaps)
                {
                    using (var k = Cv2.GetStructuringElement(MorphShapes.Ellipse, new Size(3, 3)))
                        Cv2.MorphologyEx(mask, mask, MorphTypes.Close, k);
                }
                Point[][] contours;
                HierarchyIndex[] hierarchy;
                Cv2.FindContours(mask, out contours, out hierarchy,
                    RetrievalModes.External, ContourApproximationModes.ApproxNone);
                Point[] c = null;
                double area = 0;
                double minArea = Math.Max(MinimumArea,
                    MinimumAreaFraction * src.Rows * (double)src.Cols);
                foreach (var candidate in contours)
                {
                    if (candidate.Length < 5) continue;
                    var box = Cv2.BoundingRect(candidate);
                    if (RejectBorderObjects && (box.X <= 0 || box.Y <= 0 ||
                        box.X + box.Width >= src.Cols || box.Y + box.Height >= src.Rows)) continue;
                    double a = Math.Abs(Cv2.ContourArea(candidate));
                    if (a >= minArea && a > area) { c = candidate; area = a; }
                }
                if (c == null) return r;
                var hull = Cv2.ConvexHull(c);
                double ha = Math.Abs(Cv2.ContourArea(hull));
                double perimeter = Cv2.ArcLength(c, true);
                if (ha <= 0 || perimeter <= 0) return r;
                r.Solidity = area / ha;
                r.Circularity = 4 * Math.PI * area / (perimeter * perimeter);

                // Area moments give the principal axis, independent of rotation.
                // MinAreaRect can align with sloping sides rather than the tips.
                var m = Cv2.Moments(c);
                double angle = 0.5 * Math.Atan2(2 * m.Mu11, m.Mu20 - m.Mu02);
                double ux = Math.Cos(angle), uy = Math.Sin(angle);
                double[] x = c.Select(p => p.X * ux + p.Y * uy).ToArray();
                double[] y = c.Select(p => -p.X * uy + p.Y * ux).ToArray();
                double length = x.Max() - x.Min(), width = y.Max() - y.Min();
                if (length < width)
                {
                    var temp = x; x = y; y = temp;
                    length = x.Max() - x.Min(); width = y.Max() - y.Min();
                }
                if (width < 4 || length < 4) return r;
                r.AspectRatio = length / width;
                double xmin = x.Min();
                r.EndWidth1 = Slice(x, y, xmin + 0.12 * length) / width;
                r.EndWidth2 = Slice(x, y, xmin + 0.88 * length) / width;

                var rect = Cv2.MinAreaRect(hull);
                double shortSide = Math.Min(rect.Size.Width, rect.Size.Height);
                var poly = Cv2.ApproxPolyDP(c, Math.Max(1.0, shortSide * 0.006), true);
                r.ApproxCorners = poly.Length;
                r.SharpCorners = CountCorners(poly, 150);
                bool heart = IsHeart(c, shortSide, out double defect);
                r.HeartDefectRatio = defect;
                if (heart && r.Solidity < 0.985)
                {
                    r.Shape = AutoDetectedShape.Heart; return r;
                }
                if (TryPolygon(c, hull, shortSide, area, out poly))
                {
                    r.ApproxCorners = poly.Length;
                    r.SharpCorners = CountCorners(poly, 150);
                    r.Shape = AutoDetectedShape.Polygon; return r;
                }

                // Pointed silhouettes can still fit an ellipse well. Test
                // the ends FIRST so Oval cannot swallow Marquise or Pear.
                double narrow = Math.Min(r.EndWidth1, r.EndWidth2);
                double broad = Math.Max(r.EndWidth1, r.EndWidth2);
                double nearTip1 = Slice(x, y, xmin + 0.06 * length) / width;
                double nearTip2 = Slice(x, y, xmin + 0.94 * length) / width;
                if (r.Solidity >= 0.94 && r.AspectRatio >= 1.25 &&
                    broad < 0.60 && broad - narrow < 0.14 &&
                    nearTip1 > 0 && nearTip2 > 0 &&
                    nearTip1 < 0.40 && nearTip2 < 0.40)
                {
                    r.Shape = AutoDetectedShape.Marquise;
                    return r;
                }
                if (r.Solidity >= 0.94 && r.AspectRatio >= 1.12 &&
                    narrow < 0.56 && broad > 0.60 && broad - narrow > 0.18 &&
                    broad / Math.Max(narrow, 0.01) > 1.4 &&
                    Math.Min(nearTip1, nearTip2) < 0.40)
                {
                    r.Shape = AutoDetectedShape.Pear;
                    return r;
                }

                // Circularity and ellipse fit alone accept rounded square outlines.
                // Require a nearly constant radius for Round, then use fitted
                // ellipse axes to distinguish an elongated oval from General.
                var fitted = Cv2.FitEllipse(c);
                double fittedShort = Math.Min(fitted.Size.Width, fitted.Size.Height);
                double fittedLong = Math.Max(fitted.Size.Width, fitted.Size.Height);
                double fittedRatio = fittedShort > 0 ? fittedLong / fittedShort : double.PositiveInfinity;
                double circleError = CircleError(c, fitted.Center);
                double ellipseError = EllipseError(c);
                if (r.Solidity >= 0.965 && fittedRatio <= RoundMaxAspectRatio &&
                    circleError <= RoundMaxCircleError)
                {
                    r.Shape = AutoDetectedShape.Round;
                    return r;
                }
                if (r.Solidity >= 0.965 && fittedRatio > RoundMaxAspectRatio && ellipseError <= 0.035)
                {
                    r.Shape = AutoDetectedShape.Oval;
                    return r;
                }
                if (r.SharpCorners >= 1 && r.AspectRatio > 1.15)
                    r.Shape = AutoDetectedShape.GeneralC;
                else r.Shape = AutoDetectedShape.General;
                return r;
            }
        }

        // Intersect every closed-contour edge with an exact cross-section.
        // A thick sampling band overestimates widths near tapered ends.
        private static double Slice(double[] x, double[] y, double at)
        {
            double lo = double.PositiveInfinity, hi = double.NegativeInfinity;
            int count = 0;
            for (int i = 0; i < x.Length; i++)
            {
                int j = (i + 1) % x.Length;
                if ((x[i] <= at && x[j] > at) || (x[j] <= at && x[i] > at))
                {
                    double t = (at - x[i]) / (x[j] - x[i]);
                    double v = y[i] + t * (y[j] - y[i]);
                    lo = Math.Min(lo, v); hi = Math.Max(hi, v); count++;
                }
            }
            return count >= 2 ? hi - lo : 0;
        }

        private static double CircleError(Point[] contour, Point2f center)
        {
            double sum = 0, sumSquares = 0;
            foreach (Point p in contour)
            {
                double dx = p.X - center.X, dy = p.Y - center.Y;
                double radius = Math.Sqrt(dx * dx + dy * dy);
                sum += radius;
                sumSquares += radius * radius;
            }
            double mean = sum / contour.Length;
            if (mean <= 0) return double.PositiveInfinity;
            double variance = Math.Max(0, sumSquares / contour.Length - mean * mean);
            return Math.Sqrt(variance) / mean;
        }

        private static double EllipseError(Point[] c)
        {
            var e = Cv2.FitEllipse(c);
            double a = e.Size.Width / 2.0, b = e.Size.Height / 2.0;
            if (a <= 1 || b <= 1) return double.PositiveInfinity;
            double t = e.Angle * Math.PI / 180, co = Math.Cos(t), si = Math.Sin(t);
            double sum = 0;
            foreach (var p in c)
            {
                double dx = p.X - e.Center.X, dy = p.Y - e.Center.Y;
                double xx = (dx * co + dy * si) / a;
                double yy = (-dx * si + dy * co) / b;
                double residual = Math.Sqrt(xx * xx + yy * yy) - 1;
                sum += residual * residual;
            }
            return Math.Sqrt(sum / c.Length);
        }

        private static bool TryPolygon(Point[] c, Point[] hull, double size, double area,
            out Point[] polygon)
        {
            polygon = null;
            // Several tolerances remove raster corner noise without mistaking
            // every curved outline for a polygon. Each candidate must pass
            // the independent straight-edge residual and area tests below.
            foreach (double fraction in new double[] { 0.010, 0.015, 0.020 })
            {
                var candidate = Cv2.ApproxPolyDP(hull, Math.Max(1.5, size * fraction), true);
                if (IsPolygon(c, candidate, size, area))
                {
                    polygon = candidate; return true;
                }
            }
            return false;
        }

        private static bool IsPolygon(Point[] c, Point[] poly, double size, double area)
        {
            if (poly.Length < 3 || poly.Length > 10 ||
                CountCorners(poly, 155) < Math.Ceiling(poly.Length * 0.70)) return false;
            if (Math.Abs(Math.Abs(Cv2.ContourArea(poly)) - area) / area > 0.045) return false;
            var distances = new double[c.Length];
            for (int i = 0; i < c.Length; i++)
            {
                double best = double.PositiveInfinity;
                for (int j = 0; j < poly.Length; j++)
                    best = Math.Min(best, SegmentDistance(c[i], poly[j], poly[(j + 1) % poly.Length]));
                distances[i] = best;
            }
            Array.Sort(distances);
            // Pixel-scale allowance prevents rejecting small raster polygons.
            bool closeToVertices = distances.Average() <= Math.Max(0.65, size * 0.004) &&
                distances[(int)((distances.Length - 1) * 0.95)] <= Math.Max(1.5, size * 0.012);
            return closeToVertices || HasStraightSideInteriors(c, poly, size);
        }

        // Camera noise and rounded/chipped vertices can displace the hull
        // endpoints even when the actual sides are straight. Fit each side's
        // interior independently, excluding its corner transitions. Curved
        // shapes must still pass every side, not merely a global average.
        private static bool HasStraightSideInteriors(Point[] contour, Point[] polygon, double size)
        {
            int n = polygon.Length;
            var count = new int[n];
            var sx = new double[n]; var sy = new double[n];
            var sxx = new double[n]; var syy = new double[n]; var sxy = new double[n];
            foreach (Point p in contour)
            {
                int nearest = -1;
                double best = double.PositiveInfinity, along = 0;
                for (int j = 0; j < n; j++)
                {
                    Point a = polygon[j], b = polygon[(j + 1) % n];
                    double dx = b.X - a.X, dy = b.Y - a.Y;
                    double l2 = dx * dx + dy * dy;
                    if (l2 <= 0) continue;
                    double distance = SegmentDistance(p, a, b);
                    if (distance < best)
                    {
                        best = distance; nearest = j;
                        along = ((p.X - a.X) * dx + (p.Y - a.Y) * dy) / l2;
                    }
                }
                if (nearest < 0 || along <= 0.10 || along >= 0.90) continue;
                // Use local coordinates to avoid cancellation with large image coordinates.
                double x = p.X - polygon[nearest].X, y = p.Y - polygon[nearest].Y;
                count[nearest]++;
                sx[nearest] += x; sy[nearest] += y;
                sxx[nearest] += x * x; syy[nearest] += y * y; sxy[nearest] += x * y;
            }
            double tolerance = Math.Max(0.70, size * 0.0035);
            for (int i = 0; i < n; i++)
            {
                if (count[i] < 5) return false;
                double k = count[i];
                double xx = sxx[i] / k - Math.Pow(sx[i] / k, 2);
                double yy = syy[i] / k - Math.Pow(sy[i] / k, 2);
                double xy = sxy[i] / k - sx[i] * sy[i] / (k * k);
                // Smaller covariance eigenvalue = squared perpendicular RMS
                // distance from the best-fitting line through these samples.
                double variance = 0.5 * (xx + yy - Math.Sqrt((xx - yy) * (xx - yy) + 4 * xy * xy));
                if (Math.Sqrt(Math.Max(0, variance)) > tolerance) return false;
            }
            return true;
        }

        private static bool IsHeart(Point[] c, double size, out double maxRatio)
        {
            maxRatio = 0;
            var indices = Cv2.ConvexHullIndices(c);
            if (indices.Length < 3 || indices.Length >= c.Length) return false;
            var defects = Cv2.ConvexityDefects(c, indices);
            if (defects == null) return false;
            bool found = false;
            foreach (var d in defects)
            {
                // Vec4i Item3 is fixed-point depth, not the far-point index.
                double depth = d.Item3 / 256.0;
                maxRatio = Math.Max(maxRatio, depth / size);
                if (depth < Math.Max(2.0, size * 0.06)) continue;
                var a = c[d.Item0]; var b = c[d.Item1]; var f = c[d.Item2];
                double dx = b.X - a.X, dy = b.Y - a.Y;
                double chord = Math.Sqrt(dx * dx + dy * dy);
                if (chord < size * 0.25 || chord > size * 0.95) continue;
                double t = ((f.X - a.X) * dx + (f.Y - a.Y) * dy) / (chord * chord);
                if (t < 0.30 || t > 0.70) continue; // cleft between two lobes
                double midX = (a.X + b.X) * 0.5, midY = (a.Y + b.Y) * 0.5;
                double vx = f.X - midX, vy = f.Y - midY;
                double norm = Math.Sqrt(vx * vx + vy * vy);
                if (norm < 1) continue;
                vx /= norm; vy /= norm;
                int tip = 0; double maxForward = double.NegativeInfinity;
                for (int i = 0; i < c.Length; i++)
                {
                    double forward = (c[i].X - midX) * vx + (c[i].Y - midY) * vy;
                    if (forward > maxForward) { maxForward = forward; tip = i; }
                }
                double lateral = Math.Abs(-(c[tip].X - midX) * vy + (c[tip].Y - midY) * vx);
                int k = Math.Max(2, c.Length / 16);
                double tipAngle = Angle(c[(tip - k + c.Length) % c.Length], c[tip], c[(tip + k) % c.Length]);
                if (maxForward > size * 0.65 && lateral < size * 0.25 && tipAngle < 115)
                    found = true;
            }
            return found;
        }

        private static int CountCorners(Point[] p, double limit)
        {
            int n = 0;
            for (int i = 0; i < p.Length; i++)
                if (Angle(p[(i + p.Length - 1) % p.Length], p[i], p[(i + 1) % p.Length]) < limit) n++;
            return n;
        }

        private static double Angle(Point a, Point p, Point b)
        {
            double ax = a.X - p.X, ay = a.Y - p.Y, bx = b.X - p.X, by = b.Y - p.Y;
            double norm = Math.Sqrt((ax * ax + ay * ay) * (bx * bx + by * by));
            if (norm < 1e-9) return 180;
            return Math.Acos(Math.Max(-1, Math.Min(1, (ax * bx + ay * by) / norm))) * 180 / Math.PI;
        }

        private static double SegmentDistance(Point p, Point a, Point b)
        {
            double dx = b.X - a.X, dy = b.Y - a.Y, l2 = dx * dx + dy * dy;
            double t = l2 <= 1e-9 ? 0 : Math.Max(0, Math.Min(1,
                ((p.X - a.X) * dx + (p.Y - a.Y) * dy) / l2));
            double ex = p.X - a.X - t * dx, ey = p.Y - a.Y - t * dy;
            return Math.Sqrt(ex * ex + ey * ey);
        }
    }
}
