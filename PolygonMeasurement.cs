using OpenCvSharp;
using OpenCvSharp;
using System;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Matric_scope
{
    public class PolygonMeasurement
    {
        public static string DetectAndMeasurePolygon(Mat src)
        {
            // ============================================================
            // VALIDATE IMAGE
            // ============================================================
            if (src == null || src.Empty())
            {
                return "No Image Data";
            }


            // ============================================================
            // 1. LOAD PIXELS-PER-MM CALIBRATION
            // ============================================================
            double ppm = 0;

            try
            {
                ppm =
                    Convert.ToDouble(
                        new ModifyRegistry().Read("ppm")
                    );
            }
            catch
            {
                return "Calibration Error";
            }


            if (ppm <= 0)
            {
                return "Please Calibrate First";
            }


            using (Mat gray = new Mat())
            using (Mat blur = new Mat())
            using (Mat thresh = new Mat())
            {
                // ========================================================
                // 2. CONVERT TO GRAYSCALE
                // ========================================================
                if (src.Channels() == 1)
                {
                    src.CopyTo(gray);
                }
                else
                {
                    Cv2.CvtColor(
                        src,
                        gray,
                        ColorConversionCodes.BGR2GRAY
                    );
                }


                // ========================================================
                // 3. SLIGHT BLUR
                // ========================================================
                Cv2.GaussianBlur(
                    gray,
                    blur,
                    new Size(5, 5),
                    0
                );


                // ========================================================
                // 4. AUTOMATIC OTSU THRESHOLD
                //
                // Object = dark
                // Background = light
                // ========================================================
                Cv2.Threshold(
                    blur,
                    thresh,
                    0,
                    255,
                    ThresholdTypes.BinaryInv |
                    ThresholdTypes.Otsu
                );


                // ========================================================
                // 5. CLOSE SMALL GAPS
                //
                // IMPORTANT:
                // No OPEN operation here because opening can shrink
                // polygon tips inward.
                // ========================================================
                using (Mat kernel =
                    Cv2.GetStructuringElement(
                        MorphShapes.Ellipse,
                        new Size(5, 5)
                    ))
                {
                    Cv2.MorphologyEx(
                        thresh,
                        thresh,
                        MorphTypes.Close,
                        kernel
                    );
                }


                // ========================================================
                // 6. FIND EXTERNAL CONTOURS
                // ========================================================
                Point[][] contours;
                HierarchyIndex[] hierarchy;


                Cv2.FindContours(
                    thresh,
                    out contours,
                    out hierarchy,
                    RetrievalModes.External,
                    ContourApproximationModes.ApproxSimple
                );


                if (contours == null ||
                    contours.Length == 0)
                {
                    return "No Polygon Found";
                }


                // ========================================================
                // 7. FIND LARGEST OBJECT
                // ========================================================
                Point[] largestContour =
                    contours
                    .OrderByDescending(
                        c => Cv2.ContourArea(c)
                    )
                    .First();


                double contourArea =
                    Cv2.ContourArea(
                        largestContour
                    );


                // Ignore dust / tiny blobs
                if (contourArea < 800)
                {
                    return "No Object Detected (Noise Ignored)";
                }


                // ========================================================
                // 8. GET CONVEX HULL
                //
                // Hull is used for the real outer boundary.
                // ========================================================
                Point[] hull =
                    Cv2.ConvexHull(
                        largestContour
                    );


                if (hull == null ||
                    hull.Length < 3)
                {
                    return "Invalid Polygon";
                }


                // ========================================================
                // 9. APPROXIMATE INTO POLYGON CORNERS
                // ========================================================
                double perimeter =
                    Cv2.ArcLength(
                        hull,
                        true
                    );


                // Small epsilon preserves the real corners better.
                double epsilon =
                    0.01 * perimeter;


                Point[] polygon =
                    Cv2.ApproxPolyDP(
                        hull,
                        epsilon,
                        true
                    );


                if (polygon == null ||
                    polygon.Length < 3)
                {
                    return "Invalid Polygon Corners";
                }


                // ========================================================
                // 10. STABILIZE CORNER ORDER
                //
                // Clockwise.
                // SIDE 1 always starts from top-most corner.
                // This prevents SIDE numbers randomly changing between
                // camera frames.
                // ========================================================
                polygon =
                    SortCornersClockwiseStable(
                        polygon
                    );


                // ========================================================
                // 11. FIND SHAPE NAME
                // ========================================================
                string shapeName =
                    GetPolygonName(
                        polygon.Length
                    );


                // ========================================================
                // 12. CREATE RETURN STRING
                //
                // IMPORTANT:
                //
                // We DON'T return Length/Width anymore.
                //
                // We return:
                //
                // Detected Shape: Pentagon
                // Side 1: 3.45 mm
                // Side 2: 3.18 mm
                // ...
                //
                // UpdateMeasurementUI() uses these values.
                // ========================================================
                StringBuilder result =
                    new StringBuilder();


                result.AppendLine(
                    $"Detected Shape: {shapeName}"
                );


                // ========================================================
                // 13. DRAW SIMPLIFIED POLYGON SIDES
                // ========================================================
                for (int i = 0;
                     i < polygon.Length;
                     i++)
                {
                    Point p1 =
                        polygon[i];

                    Point p2 =
                        polygon[
                            (i + 1) %
                            polygon.Length
                        ];


                    // ----------------------------------------------------
                    // Side length in pixels
                    // ----------------------------------------------------
                    double pixelDistance =
                        Distance(
                            p1,
                            p2
                        );


                    // ----------------------------------------------------
                    // Convert pixels to millimeters
                    // ----------------------------------------------------
                    double mmDistance =
                        pixelDistance /
                        ppm;


                    // ----------------------------------------------------
                    // Return side measurement to UI
                    // ----------------------------------------------------
                    result.AppendLine(
                        string.Format(
                            CultureInfo.InvariantCulture,
                            "Side {0}: {1:F2} mm",
                            i + 1,
                            mmDistance
                        )
                    );


                    // ----------------------------------------------------
                    // Draw measured polygon side
                    // ----------------------------------------------------
                    Cv2.Line(
                        src,
                        p1,
                        p2,
                        Scalar.Lime,
                        2,
                        LineTypes.AntiAlias
                    );


                    // ----------------------------------------------------
                    // Draw side length near center of side
                    // ----------------------------------------------------
                    Point textPoint =
                        GetSideTextPosition(
                            polygon,
                            p1,
                            p2
                        );


                    DrawingTextSettings.PutText(
                        src,
                        $"{mmDistance:F2}mm",
                        textPoint,
                        HersheyFonts.HersheySimplex,
                        0.55,
                        Scalar.Yellow,
                        1,
                        LineTypes.AntiAlias
                    );
                }


                // ========================================================
                // 14. DRAW TRUE OUTER HULL LAST
                //
                // This keeps the visible green outline on the actual
                // outer silhouette rather than appearing slightly inside.
                // ========================================================
                Cv2.Polylines(
                    src,
                    new[] { hull },
                    true,
                    Scalar.Lime,
                    2,
                    LineTypes.AntiAlias
                );


                // ========================================================
                // 15. CALCULATE AND DISPLAY INTERIOR ANGLES
                // ========================================================
                for (int i = 0;
                     i < polygon.Length;
                     i++)
                {
                    Point previous =
                        polygon[
                            (i - 1 +
                             polygon.Length)
                            %
                            polygon.Length
                        ];


                    Point current =
                        polygon[i];


                    Point next =
                        polygon[
                            (i + 1)
                            %
                            polygon.Length
                        ];


                    double angle =
                        CalculateAngle(
                            previous,
                            current,
                            next
                        );


                    Cv2.Circle(
                        src,
                        current,
                        3,
                        Scalar.Red,
                        -1,
                        LineTypes.AntiAlias
                    );


                    Point angleTextPoint =
                        new Point(
                            current.X + 8,
                            current.Y + 4
                        );


                    DrawingTextSettings.PutText(
                        src,
                        $"{angle:F1}°",
                        angleTextPoint,
                        HersheyFonts.HersheySimplex,
                        0.50,
                        Scalar.Cyan,
                        1,
                        LineTypes.AntiAlias
                    );
                }


                // ========================================================
                // 16. CREATE SHAPE IMAGE FOR LABEL PRINTING
                //
                // IMPORTANT:
                //
                // BoundingRect here is ONLY used to crop the output image.
                // It is NOT drawn and it is NOT used for measurement.
                // ========================================================
                using (Mat printCanvas =
                    new Mat(
                        src.Size(),
                        MatType.CV_8UC3,
                        Scalar.White
                    ))
                {
                    Cv2.Polylines(
                        printCanvas,
                        new[] { hull },
                        true,
                        Scalar.Black,
                        3,
                        LineTypes.AntiAlias
                    );


                    OpenCvSharp.Rect cropRect =
                        Cv2.BoundingRect(
                            hull
                        );


                    // Add blank margin around shape
                    cropRect.Inflate(
                        15,
                        15
                    );


                    // Prevent crop from leaving image bounds
                    cropRect.Intersect(
                        new OpenCvSharp.Rect(
                            0,
                            0,
                            printCanvas.Width,
                            printCanvas.Height
                        )
                    );


                    if (cropRect.Width > 0 &&
                        cropRect.Height > 0)
                    {
                        using (Mat croppedForPrint =
                            new Mat(
                                printCanvas,
                                cropRect
                            ))
                        {
                            croppedForPrint.ImWrite(
                                "ShapeForLabel.png"
                            );
                        }
                    }
                }


                // ========================================================
                // 17. OPTIONAL DEBUG RESULT
                // ========================================================
                src.ImWrite(
                    "result.png"
                );


                // ========================================================
                // 18. RETURN ALL SIDE MEASUREMENTS
                // ========================================================
                return result.ToString();
            }
        }



        // ================================================================
        // DISTANCE BETWEEN TWO POINTS
        // ================================================================
        private static double Distance(
            Point p1,
            Point p2)
        {
            double dx =
                p2.X - p1.X;

            double dy =
                p2.Y - p1.Y;


            return Math.Sqrt(
                (dx * dx) +
                (dy * dy)
            );
        }



        // ================================================================
        // CALCULATE INTERNAL CORNER ANGLE
        // ================================================================
        private static double CalculateAngle(
            Point previous,
            Point current,
            Point next)
        {
            double ax =
                previous.X -
                current.X;

            double ay =
                previous.Y -
                current.Y;


            double bx =
                next.X -
                current.X;

            double by =
                next.Y -
                current.Y;


            double dot =
                (ax * bx) +
                (ay * by);


            double magnitudeA =
                Math.Sqrt(
                    ax * ax +
                    ay * ay
                );


            double magnitudeB =
                Math.Sqrt(
                    bx * bx +
                    by * by
                );


            if (magnitudeA == 0 ||
                magnitudeB == 0)
            {
                return 0;
            }


            double cosTheta =
                dot /
                (magnitudeA *
                 magnitudeB);


            // Protect Math.Acos from tiny floating point errors
            cosTheta =
                Math.Max(
                    -1.0,
                    Math.Min(
                        1.0,
                        cosTheta
                    )
                );


            double angleRadians =
                Math.Acos(
                    cosTheta
                );


            return
                angleRadians *
                180.0 /
                Math.PI;
        }



        // ================================================================
        // SORT CORNERS CLOCKWISE AND KEEP SIDE NUMBERING STABLE
        //
        // The first corner is always the top-most corner.
        // If two have same Y, left-most wins.
        //
        // Example:
        //
        //          P1
        //
        //     P8         P2
        //
        //   P7             P3
        //
        //     P6         P4
        //
        //          P5
        //
        // SIDE 1 = P1 -> P2
        // SIDE 2 = P2 -> P3
        // ...
        // ================================================================
        private static Point[] SortCornersClockwiseStable(
            Point[] points)
        {
            if (points == null ||
                points.Length == 0)
            {
                return points;
            }


            double centerX =
                points.Average(
                    p => (double)p.X
                );


            double centerY =
                points.Average(
                    p => (double)p.Y
                );


            // Since image Y increases downward,
            // ascending Atan2 gives clockwise order:
            //
            // Top -> Right -> Bottom -> Left
            Point[] sorted =
                points
                .OrderBy(
                    p =>
                        Math.Atan2(
                            p.Y - centerY,
                            p.X - centerX
                        )
                )
                .ToArray();


            // Find top-most point
            int startIndex = 0;


            for (int i = 1;
                 i < sorted.Length;
                 i++)
            {
                if (sorted[i].Y <
                    sorted[startIndex].Y)
                {
                    startIndex = i;
                }
                else if (
                    sorted[i].Y ==
                    sorted[startIndex].Y
                    &&
                    sorted[i].X <
                    sorted[startIndex].X)
                {
                    startIndex = i;
                }
            }


            // Rotate array so top-most is element 0
            Point[] stable =
                new Point[
                    sorted.Length
                ];


            for (int i = 0;
                 i < sorted.Length;
                 i++)
            {
                stable[i] =
                    sorted[
                        (startIndex + i)
                        %
                        sorted.Length
                    ];
            }


            return stable;
        }



        // ================================================================
        // PLACE SIDE MEASUREMENT TEXT SLIGHTLY OUTSIDE POLYGON
        //
        // Instead of printing directly on the green side line.
        // ================================================================
        private static Point GetSideTextPosition(
            Point[] polygon,
            Point p1,
            Point p2)
        {
            double centerX =
                polygon.Average(
                    p => (double)p.X
                );


            double centerY =
                polygon.Average(
                    p => (double)p.Y
                );


            double midX =
                (p1.X + p2.X) /
                2.0;


            double midY =
                (p1.Y + p2.Y) /
                2.0;


            // Direction from center towards side
            double dx =
                midX - centerX;

            double dy =
                midY - centerY;


            double length =
                Math.Sqrt(
                    dx * dx +
                    dy * dy
                );


            if (length <= 0.001)
            {
                return new Point(
                    (int)Math.Round(midX),
                    (int)Math.Round(midY)
                );
            }


            dx /= length;
            dy /= length;


            // Move text 10 pixels outward
            double offset =
                10.0;


            return new Point(
                (int)Math.Round(
                    midX +
                    dx * offset
                ),
                (int)Math.Round(
                    midY +
                    dy * offset
                )
            );
        }



        // ================================================================
        // HUMAN-READABLE POLYGON NAME
        // ================================================================
        private static string GetPolygonName(
            int sides)
        {
            switch (sides)
            {
                case 3:
                    return "Triangle";

                case 4:
                    return "Quadrilateral";

                case 5:
                    return "Pentagon";

                case 6:
                    return "Hexagon";

                case 7:
                    return "Heptagon";

                case 8:
                    return "Octagon";

                case 9:
                    return "Nonagon";

                case 10:
                    return "Decagon";

                default:
                    return
                        $"{sides}-sided Polygon";
            }
        }
    }
}


/*using System;
using System.Linq;
using System.Text;

namespace Matric_scope
{
    public class PolygonMeasurement
    {
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
                    // Closing fills small gaps without eroding the polygon tips.
                    // A subsequent 5x5 opening rounded those tips inward.
                    Cv2.MorphologyEx(thresh, thresh, MorphTypes.Close, kernel);
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
                // Keep the corner approximation tight. A large epsilon cuts across the
                // real tips and makes both the overlay and measurements appear inset.
                double epsilon = 0.01 * perimeter;
                Point[] polygon = Cv2.ApproxPolyDP(hull, epsilon, true);

                if (polygon.Length < 3) return "Invalid Polygon Corners";

                // ==========================================
                // 5. TRUE TIP-TO-TIP VECTOR BOUNDING BOX
                // ==========================================
                double maxDistSq = 0;
                Point tip1 = hull[0];
                Point tip2 = hull[0];

                // Measure against the full convex hull, not the simplified polygon.
                // ApproxPolyDP remains useful for identifying sides and angles, but its
                // reduced vertex set can omit the silhouette's outermost boundary points.
                for (int i = 0; i < hull.Length; i++)
                {
                    for (int j = i + 1; j < hull.Length; j++)
                    {
                        double dSq = Math.Pow(hull[i].X - hull[j].X, 2) + Math.Pow(hull[i].Y - hull[j].Y, 2);
                        if (dSq > maxDistSq)
                        {
                            maxDistSq = dSq;
                            tip1 = hull[i];
                            tip2 = hull[j];
                        }
                    }
                }

                double lengthPx = Math.Sqrt(maxDistSq);
                if (lengthPx == 0) return "Invalid Shape Profile";

                double ux = (tip2.X - tip1.X) / lengthPx;
                double uy = (tip2.Y - tip1.Y) / lengthPx;

                double nx = uy;
                double ny = -ux;

                double maxLeftDist = double.NegativeInfinity;
                double maxRightDist = double.PositiveInfinity;

                // Project every boundary point so width reaches both outer support edges.
                foreach (Point p in hull)
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

                // Draw the full detected hull last. The simplified polygon is correct for
                // side/angle labels, but joining only its reduced vertices draws chords
                // inside the real silhouette and makes the green outline look inset.
                Cv2.Polylines(src, new[] { hull }, true, Scalar.Lime, 2, LineTypes.AntiAlias);

                // Measure Sides & Draw Text Overlay
                for (int i = 0; i < polygon.Length; i++)
                {
                    Point p1 = polygon[i];
                    Point p2 = polygon[(i + 1) % polygon.Length];

                    double pixelDistance = Distance(p1, p2);
                    double mmDistance = pixelDistance / ppm;

                    Point mid = new Point((p1.X + p2.X) / 2, (p1.Y + p2.Y) / 2);
                    DrawingTextSettings.PutText(src, $"{mmDistance:F2}mm", mid, HersheyFonts.HersheySimplex, 0.55, Scalar.Yellow, 1, LineTypes.AntiAlias);
                }

                // Measure Angles & Draw Text Overlay
                for (int i = 0; i < polygon.Length; i++)
                {
                    Point prev = polygon[(i - 1 + polygon.Length) % polygon.Length];
                    Point current = polygon[i];
                    Point next = polygon[(i + 1) % polygon.Length];

                    double angle = CalculateAngle(prev, current, next);

                    Cv2.Circle(src, current, 3, Scalar.Red, -1, LineTypes.AntiAlias);
                    DrawingTextSettings.PutText(src, $"{angle:F1}°", new Point(current.X + 10, current.Y), HersheyFonts.HersheySimplex, 0.55, Scalar.Cyan, 1, LineTypes.AntiAlias);
                }

                // ==========================================================
                // 7. CREATE ISOLATED SHAPE IMAGE FOR LABEL PRINTING
                // ==========================================================
                using (Mat printCanvas = new Mat(src.Size(), MatType.CV_8UC3, Scalar.White))
                {
                    // Use the same full outer hull that is displayed on screen.
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

                src.ImWrite("result.png");
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

        static Point[] SortCornersClockwise(Point[] points)
        {
            Point center = new Point((int)points.Average(p => p.X), (int)points.Average(p => p.Y));
            return points.OrderBy(p => Math.Atan2(p.Y - center.Y, p.X - center.X)).ToArray();
        }

        private static double ApplyVariation(double rawMeasurementMM, bool isLength)
        {
            int rangeIndex = (int)Math.Floor(rawMeasurementMM);

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
            catch
            {
                // Keep the raw calibrated measurement if no valid variation is stored.
            }

            return rawMeasurementMM + variation;
        }

    }
}*/
