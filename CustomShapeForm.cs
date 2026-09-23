using OpenCvSharp;
using OpenCvSharp.Extensions;
using System;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Matric_scope
{
    public partial class CustomShapeForm : Form
    {
        private PictureBox pictureBox;
        private Button btnSetWidth;
        private Button btnSetLength;
        private Button btnResetZoom;
        private Button btnSave;
        private CheckBox chkSnapToEdge;
        private ComboBox cmbTransformMode;
        private Label lblStatus;

        private Mat sourceFrame;
        private Mat displayFrame;
        private Mat bgFrame;

        private Point2f centroid;

        private Point2f? wPt1 = null, wPt2 = null;
        private Point2f? lPt1 = null, lPt2 = null;

        private enum ClickState { None, Width, Length }
        private ClickState currentState = ClickState.None;

        private enum DragHandle { None, WidthPoint1, WidthPoint2, LengthPoint1, LengthPoint2 }
        private DragHandle activeHandle = DragHandle.None;

        private float zoomFactor = 1.0f;
        private const float MIN_ZOOM = 1.0f;
        private const float MAX_ZOOM = 10.0f;
        private System.Drawing.Point panOffset = new System.Drawing.Point(0, 0);
        private System.Drawing.Point dragStart = new System.Drawing.Point(0, 0);
        private bool isPanning = false;

        private double pixelToMmRatio = 1.0;
        private Cursor precisionCursor;

        [StructLayout(LayoutKind.Sequential)]
        public struct IconInfo
        {
            public bool fIcon;
            public int xHotspot;
            public int yHotspot;
            public IntPtr hbmMask;
            public IntPtr hbmColor;
        }

        [DllImport("user32.dll")]
        public static extern IntPtr CreateIconIndirect(ref IconInfo icon);

        [DllImport("user32.dll")]
        public static extern bool GetIconInfo(IntPtr hIcon, ref IconInfo pIconInfo);

        public CustomShapeForm(Mat capturedFrame, Mat backgroundFrame = null)
        {
            if (capturedFrame == null || capturedFrame.Empty())
                throw new ArgumentException("capturedFrame is empty", nameof(capturedFrame));

            sourceFrame = capturedFrame.Clone();
            displayFrame = capturedFrame.Clone();

            if (backgroundFrame != null && !backgroundFrame.IsDisposed && !backgroundFrame.Empty())
                bgFrame = backgroundFrame.Clone();

            precisionCursor = CreatePrecisionCursor();
            LoadCalibration();
            InitializeUI();
            DetectBaseOrientation();
            RedrawOverlay();
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            Bitmap old = pictureBox?.Image as Bitmap;
            if (pictureBox != null) pictureBox.Image = null;
            old?.Dispose();

            displayFrame?.Dispose();
            sourceFrame?.Dispose();
            bgFrame?.Dispose();
            precisionCursor?.Dispose();

            base.OnFormClosed(e);
        }

        private Cursor CreatePrecisionCursor()
        {
            int size = 22, center = 10, length = 7, gap = 2;

            using (Bitmap bmp = new Bitmap(size, size))
            {
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    g.Clear(Color.Transparent);
                    Brush brush = Brushes.Black;
                    g.FillRectangle(brush, center, 0, 2, length);
                    g.FillRectangle(brush, center, center + gap + 2, 2, length);
                    g.FillRectangle(brush, 0, center, length, 2);
                    g.FillRectangle(brush, center + gap + 2, center, length, 2);
                    g.FillRectangle(Brushes.Red, center, center, 2, 2);
                }

                IntPtr ptr = bmp.GetHicon();
                IconInfo tmp = new IconInfo();
                GetIconInfo(ptr, ref tmp);
                tmp.xHotspot = center + 1;
                tmp.yHotspot = center + 1;
                tmp.fIcon = false;
                return new Cursor(CreateIconIndirect(ref tmp));
            }
        }

        private void LoadCalibration()
        {
            try
            {
                object regVal = new ModifyRegistry().Read("ppm");
                if (regVal != null && double.TryParse(regVal.ToString(), out double ppm) && ppm > 0)
                    pixelToMmRatio = 1.0 / ppm;
            }
            catch
            {
                MessageBox.Show("Warning: Calibration (ppm) not found. Measurements will be shown in pixels.",
                    "Calibration Error");
            }
        }

        private void InitializeUI()
        {
            Text = "Custom Shape Trainer - One Shot";
            Size = new System.Drawing.Size(1180, 760);
            StartPosition = FormStartPosition.CenterScreen;

            Panel panel = new Panel { Dock = DockStyle.Top, Height = 92 };

            btnSetWidth = new Button
            {
                Text = "1. Set Width (Drag)",
                Location = new System.Drawing.Point(10, 12),
                Size = new System.Drawing.Size(140, 30)
            };

            btnSetLength = new Button
            {
                Text = "2. Set Length (Drag)",
                Location = new System.Drawing.Point(160, 12),
                Size = new System.Drawing.Size(140, 30)
            };

            chkSnapToEdge = new CheckBox
            {
                Text = "Snap to Edge",
                Location = new System.Drawing.Point(310, 17),
                Size = new System.Drawing.Size(110, 20),
                Checked = true
            };

            btnResetZoom = new Button
            {
                Text = "Reset Zoom",
                Location = new System.Drawing.Point(430, 12),
                Size = new System.Drawing.Size(95, 30)
            };

            btnSave = new Button
            {
                Text = "3. Train + Save",
                Location = new System.Drawing.Point(535, 12),
                Size = new System.Drawing.Size(115, 30)
            };

            cmbTransformMode = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Location = new System.Drawing.Point(665, 14),
                Size = new System.Drawing.Size(300, 24)
            };

            cmbTransformMode.Items.Add("Centroid Axis - force through center");
            cmbTransformMode.Items.Add("Free Offset / Stable Axis - keep placed line");
            cmbTransformMode.Items.Add("Relative Landmark - keep % position");
            cmbTransformMode.SelectedIndex = 0;

            lblStatus = new Label
            {
                Text = "Status: Select Width or Length mode.",
                Location = new System.Drawing.Point(10, 55),
                Size = new System.Drawing.Size(1130, 28)
            };

            panel.Controls.AddRange(new Control[]
            {
                btnSetWidth, btnSetLength, chkSnapToEdge, btnResetZoom,
                btnSave, cmbTransformMode, lblStatus
            });
            Controls.Add(panel);

            pictureBox = new PictureBox
            {
                Dock = DockStyle.Fill,
                BorderStyle = BorderStyle.Fixed3D,
                BackColor = Color.LightGray
            };

            pictureBox.MouseWheel += PictureBox_MouseWheel;
            pictureBox.MouseDown += PictureBox_MouseDown;
            pictureBox.MouseMove += PictureBox_MouseMove;
            pictureBox.MouseUp += PictureBox_MouseUp;
            pictureBox.Paint += PictureBox_Paint;
            Controls.Add(pictureBox);

            btnSetWidth.Click += (s, e) =>
            {
                currentState = ClickState.Width;
                lblStatus.Text = IsCentroidMode
                    ? "WIDTH: drag one endpoint; opposite endpoint is mirrored through centroid."
                    : "WIDTH: drag freely edge-to-edge. Off-center positions are allowed.";
                pictureBox.Cursor = precisionCursor;
            };

            btnSetLength.Click += (s, e) =>
            {
                currentState = ClickState.Length;
                lblStatus.Text = IsCentroidMode
                    ? "LENGTH: drag one endpoint; opposite endpoint is mirrored through centroid."
                    : "LENGTH: drag freely edge-to-edge. Off-center positions are allowed.";
                pictureBox.Cursor = precisionCursor;
            };

            cmbTransformMode.SelectedIndexChanged += (s, e) =>
            {
                wPt1 = wPt2 = lPt1 = lPt2 = null;
                currentState = ClickState.None;
                DetectBaseOrientation();

                switch (CurrentTransformMode)
                {
                    case ShapeTransformMode.Centroid:
                        lblStatus.Text = "Centroid Axis: lines are forced through the detected center.";
                        break;
                    case ShapeTransformMode.TaperedLongestEdge:
                        lblStatus.Text = "Free Offset/Stable Axis: keeps off-center lines and uses the whole silhouette for stable orientation.";
                        break;
                    case ShapeTransformMode.RelativeLandmark:
                        lblStatus.Text = "Relative Landmark: keeps each line at the same percentage position inside fat/thin live shapes.";
                        break;
                }

                RedrawOverlay();
            };

            btnResetZoom.Click += (s, e) => ResetZoomAndPan();
            btnSave.Click += BtnSave_Click;
        }

        private ShapeTransformMode CurrentTransformMode
        {
            get
            {
                if (cmbTransformMode == null) return ShapeTransformMode.Centroid;
                if (cmbTransformMode.SelectedIndex == 1) return ShapeTransformMode.TaperedLongestEdge;
                if (cmbTransformMode.SelectedIndex == 2) return ShapeTransformMode.RelativeLandmark;
                return ShapeTransformMode.Centroid;
            }
        }

        private bool IsCentroidMode => CurrentTransformMode == ShapeTransformMode.Centroid;

        private void DetectBaseOrientation()
        {
            if (CustomShapeEngine.TryExtractStoneContour(sourceFrame, out OpenCvSharp.Point[] contour))
            {
                CustomShapeEngine.GetInvariantTransform(contour, CurrentTransformMode,
                    out centroid, out _, out _, out _);
            }
            else
            {
                MessageBox.Show("Vision Error: Stone contour not detected.", "Vision Warning");
                centroid = new Point2f(sourceFrame.Width / 2f, sourceFrame.Height / 2f);
            }
        }

        private void PictureBox_MouseWheel(object sender, MouseEventArgs e)
        {
            float oldZoom = zoomFactor;
            zoomFactor = e.Delta > 0
                ? Math.Min(zoomFactor * 1.25f, MAX_ZOOM)
                : Math.Max(zoomFactor / 1.25f, MIN_ZOOM);

            if (zoomFactor == MIN_ZOOM)
            {
                panOffset = new System.Drawing.Point(0, 0);
            }
            else
            {
                float ratio = zoomFactor / oldZoom;
                panOffset.X = (int)(e.X - (e.X - panOffset.X) * ratio);
                panOffset.Y = (int)(e.Y - (e.Y - panOffset.Y) * ratio);
            }
            RedrawOverlay();
        }

        private void PictureBox_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                isPanning = true;
                dragStart = e.Location;
                pictureBox.Cursor = Cursors.Hand;
                return;
            }

            if (e.Button != MouseButtons.Left || sourceFrame == null) return;

            Point2f imgPt = MapScreenToImageCoordinates(e.Location);
            if (imgPt.X < 0 || imgPt.X >= sourceFrame.Width || imgPt.Y < 0 || imgPt.Y >= sourceFrame.Height)
                return;

            activeHandle = HitTestMeasurementHandle(e.Location);

            if (activeHandle == DragHandle.None && currentState != ClickState.None)
            {
                if (currentState == ClickState.Width)
                {
                    activeHandle = DragHandle.WidthPoint2;
                    if (IsCentroidMode)
                        SetMeasurementEndpoint(activeHandle, imgPt);
                    else
                        wPt1 = wPt2 = imgPt;
                }
                else
                {
                    activeHandle = DragHandle.LengthPoint2;
                    if (IsCentroidMode)
                        SetMeasurementEndpoint(activeHandle, imgPt);
                    else
                        lPt1 = lPt2 = imgPt;
                }
            }

            if (activeHandle != DragHandle.None)
            {
                pictureBox.Capture = true;
                pictureBox.Cursor = Cursors.SizeAll;
                SetMeasurementEndpoint(activeHandle, imgPt);
                UpdateCustomMeasurementStatus();
                RedrawOverlay();
            }
        }

        private void PictureBox_MouseMove(object sender, MouseEventArgs e)
        {
            if (isPanning)
            {
                panOffset.X += e.X - dragStart.X;
                panOffset.Y += e.Y - dragStart.Y;
                dragStart = e.Location;
                pictureBox.Invalidate();
                return;
            }

            if (activeHandle != DragHandle.None)
            {
                Point2f imgPt = ClampToSource(MapScreenToImageCoordinates(e.Location));
                SetMeasurementEndpoint(activeHandle, imgPt);
                UpdateCustomMeasurementStatus();
                RedrawOverlay();
                lblStatus.Refresh();
                pictureBox.Refresh();
                return;
            }

            DragHandle hover = HitTestMeasurementHandle(e.Location);
            pictureBox.Cursor = hover != DragHandle.None
                ? Cursors.SizeAll
                : (currentState != ClickState.None ? precisionCursor : Cursors.Default);
        }

        private void PictureBox_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                isPanning = false;
                pictureBox.Cursor = currentState != ClickState.None ? precisionCursor : Cursors.Default;
                return;
            }

            if (e.Button == MouseButtons.Left && activeHandle != DragHandle.None)
            {
                activeHandle = DragHandle.None;
                currentState = ClickState.None;
                pictureBox.Capture = false;
                pictureBox.Cursor = Cursors.Default;
                UpdateCustomMeasurementStatus();
                RedrawOverlay();
            }
        }

        private void PictureBox_Paint(object sender, PaintEventArgs e)
        {
            if (pictureBox.Image == null) return;

            e.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            e.Graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
            e.Graphics.TranslateTransform(panOffset.X, panOffset.Y);
            e.Graphics.ScaleTransform(zoomFactor, zoomFactor);
            Rectangle targetRect = GetAspectFitRectangle();
            e.Graphics.DrawImage(pictureBox.Image, targetRect);
            e.Graphics.ResetTransform();
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            DrawEditableHandles(e.Graphics);
        }

        private void ResetZoomAndPan()
        {
            zoomFactor = 1f;
            panOffset = new System.Drawing.Point(0, 0);
            RedrawOverlay();
        }

        private Rectangle GetAspectFitRectangle()
        {
            int imgW = sourceFrame.Width, imgH = sourceFrame.Height;
            int boxW = Math.Max(1, pictureBox.Width), boxH = Math.Max(1, pictureBox.Height);
            float ratio = Math.Min((float)boxW / imgW, (float)boxH / imgH);
            int targetW = (int)(imgW * ratio), targetH = (int)(imgH * ratio);
            return new Rectangle((boxW - targetW) / 2, (boxH - targetH) / 2, targetW, targetH);
        }

        private Point2f MapScreenToImageCoordinates(System.Drawing.Point screenPt)
        {
            float x = (screenPt.X - panOffset.X) / zoomFactor;
            float y = (screenPt.Y - panOffset.Y) / zoomFactor;
            Rectangle targetRect = GetAspectFitRectangle();
            return new Point2f(
                (x - targetRect.X) * sourceFrame.Width / targetRect.Width,
                (y - targetRect.Y) * sourceFrame.Height / targetRect.Height);
        }

        private PointF MapImageToScreenCoordinates(Point2f imagePoint)
        {
            Rectangle targetRect = GetAspectFitRectangle();
            float fittedX = targetRect.X + imagePoint.X * targetRect.Width / sourceFrame.Width;
            float fittedY = targetRect.Y + imagePoint.Y * targetRect.Height / sourceFrame.Height;
            return new PointF(fittedX * zoomFactor + panOffset.X, fittedY * zoomFactor + panOffset.Y);
        }

        private DragHandle HitTestMeasurementHandle(System.Drawing.Point mousePoint)
        {
            const double grabRadius = 10.0;
            PointF mouse = new PointF(mousePoint.X, mousePoint.Y);

            if (wPt1.HasValue && ScreenDistance(mouse, MapImageToScreenCoordinates(wPt1.Value)) <= grabRadius) return DragHandle.WidthPoint1;
            if (wPt2.HasValue && ScreenDistance(mouse, MapImageToScreenCoordinates(wPt2.Value)) <= grabRadius) return DragHandle.WidthPoint2;
            if (lPt1.HasValue && ScreenDistance(mouse, MapImageToScreenCoordinates(lPt1.Value)) <= grabRadius) return DragHandle.LengthPoint1;
            if (lPt2.HasValue && ScreenDistance(mouse, MapImageToScreenCoordinates(lPt2.Value)) <= grabRadius) return DragHandle.LengthPoint2;
            return DragHandle.None;
        }

        private static double ScreenDistance(PointF a, PointF b)
        {
            double dx = b.X - a.X, dy = b.Y - a.Y;
            return Math.Sqrt(dx * dx + dy * dy);
        }

        private Point2f ClampToSource(Point2f p)
        {
            return new Point2f(
                Math.Max(0f, Math.Min(sourceFrame.Width - 1f, p.X)),
                Math.Max(0f, Math.Min(sourceFrame.Height - 1f, p.Y)));
        }

        private void SetMeasurementEndpoint(DragHandle handle, Point2f draggedPoint)
        {
            draggedPoint = ClampToSource(draggedPoint);

            if (IsCentroidMode)
            {
                SetSymmetricEndpoint(handle, draggedPoint);
                return;
            }

            switch (handle)
            {
                case DragHandle.WidthPoint1: wPt1 = draggedPoint; break;
                case DragHandle.WidthPoint2: wPt2 = draggedPoint; break;
                case DragHandle.LengthPoint1: lPt1 = draggedPoint; break;
                case DragHandle.LengthPoint2: lPt2 = draggedPoint; break;
            }
        }

        private void SetSymmetricEndpoint(DragHandle handle, Point2f draggedPoint)
        {
            float dx = draggedPoint.X - centroid.X;
            float dy = draggedPoint.Y - centroid.Y;
            float scale = Math.Min(
                SymmetricAxisScale(centroid.X, dx, sourceFrame.Width - 1f),
                SymmetricAxisScale(centroid.Y, dy, sourceFrame.Height - 1f));

            Point2f selected = new Point2f(centroid.X + dx * scale, centroid.Y + dy * scale);
            Point2f opposite = new Point2f(centroid.X - dx * scale, centroid.Y - dy * scale);

            switch (handle)
            {
                case DragHandle.WidthPoint1: wPt1 = selected; wPt2 = opposite; break;
                case DragHandle.WidthPoint2: wPt2 = selected; wPt1 = opposite; break;
                case DragHandle.LengthPoint1: lPt1 = selected; lPt2 = opposite; break;
                case DragHandle.LengthPoint2: lPt2 = selected; lPt1 = opposite; break;
            }
        }

        private static float SymmetricAxisScale(float center, float delta, float maximum)
        {
            if (Math.Abs(delta) < 0.0001f) return 1f;
            float forwardRoom = delta > 0 ? maximum - center : center;
            float oppositeRoom = delta > 0 ? center : maximum - center;
            return Math.Min(1f, Math.Min(forwardRoom, oppositeRoom) / Math.Abs(delta));
        }

        private void UpdateCustomMeasurementStatus()
        {
            string widthText = wPt1.HasValue && wPt2.HasValue
                ? $"Width {wPt1.Value.DistanceTo(wPt2.Value) * pixelToMmRatio:F2} mm"
                : "Width --";
            string lengthText = lPt1.HasValue && lPt2.HasValue
                ? $"Length {lPt1.Value.DistanceTo(lPt2.Value) * pixelToMmRatio:F2} mm"
                : "Length --";
            lblStatus.Text = widthText + "  |  " + lengthText + "  |  " + CurrentTransformMode;
        }

        private void RedrawOverlay()
        {
            displayFrame?.Dispose();
            displayFrame = sourceFrame.Clone();
            float radius = GetImageRadiusForScreenPixels(5f);

            if (wPt1.HasValue && wPt2.HasValue)
            {
                if (IsCentroidMode) DrawAxisWithCircleGaps(displayFrame, wPt1.Value, centroid, wPt2.Value, Scalar.Red, radius);
                else DrawSegmentOutsideCircles(displayFrame, wPt1.Value, wPt2.Value, Scalar.Red, radius);
            }

            if (lPt1.HasValue && lPt2.HasValue)
            {
                if (IsCentroidMode) DrawAxisWithCircleGaps(displayFrame, lPt1.Value, centroid, lPt2.Value, Scalar.Blue, radius);
                else DrawSegmentOutsideCircles(displayFrame, lPt1.Value, lPt2.Value, Scalar.Blue, radius);
            }

            Bitmap oldBmp = pictureBox.Image as Bitmap;
            pictureBox.Image = BitmapConverter.ToBitmap(displayFrame);
            oldBmp?.Dispose();
            pictureBox.Invalidate();
        }

        private float GetImageRadiusForScreenPixels(float screenRadius)
        {
            Rectangle targetRect = GetAspectFitRectangle();
            float displayScale = (float)targetRect.Width / sourceFrame.Width * zoomFactor;
            return displayScale > 0 ? screenRadius / displayScale : screenRadius;
        }

        private static void DrawSegmentOutsideCircles(Mat frame, Point2f start, Point2f end, Scalar color, float radius)
        {
            float dx = end.X - start.X, dy = end.Y - start.Y;
            float length = (float)Math.Sqrt(dx * dx + dy * dy);
            if (length <= radius * 2f) return;
            float ux = dx / length, uy = dy / length;
            Point2f a = new Point2f(start.X + ux * radius, start.Y + uy * radius);
            Point2f b = new Point2f(end.X - ux * radius, end.Y - uy * radius);
            Cv2.Line(frame, (OpenCvSharp.Point)a, (OpenCvSharp.Point)b, color, 2, LineTypes.AntiAlias);
        }

        private static void DrawAxisWithCircleGaps(Mat frame, Point2f first, Point2f center, Point2f second,
            Scalar color, float radius)
        {
            DrawSegmentOutsideCircles(frame, first, center, color, radius);
            DrawSegmentOutsideCircles(frame, center, second, color, radius);
        }

        private void DrawEditableHandles(Graphics graphics)
        {
            DrawScreenHandle(graphics, MapImageToScreenCoordinates(centroid));
            if (wPt1.HasValue) DrawScreenHandle(graphics, MapImageToScreenCoordinates(wPt1.Value));
            if (wPt2.HasValue) DrawScreenHandle(graphics, MapImageToScreenCoordinates(wPt2.Value));
            if (lPt1.HasValue) DrawScreenHandle(graphics, MapImageToScreenCoordinates(lPt1.Value));
            if (lPt2.HasValue) DrawScreenHandle(graphics, MapImageToScreenCoordinates(lPt2.Value));

            if (wPt1.HasValue && wPt2.HasValue)
            {
                PointF a = MapImageToScreenCoordinates(wPt1.Value);
                PointF b = MapImageToScreenCoordinates(wPt2.Value);
                DrawLiveValue(graphics,
                    $"Width {wPt1.Value.DistanceTo(wPt2.Value) * pixelToMmRatio:F2} mm",
                    OffsetFromLineMidpoint(a, b, 24f), Color.Red);
            }

            if (lPt1.HasValue && lPt2.HasValue)
            {
                PointF a = MapImageToScreenCoordinates(lPt1.Value);
                PointF b = MapImageToScreenCoordinates(lPt2.Value);
                DrawLiveValue(graphics,
                    $"Length {lPt1.Value.DistanceTo(lPt2.Value) * pixelToMmRatio:F2} mm",
                    OffsetFromLineMidpoint(a, b, -24f), Color.DeepSkyBlue);
            }
        }

        private static PointF OffsetFromLineMidpoint(PointF first, PointF second, float offset)
        {
            float mx = (first.X + second.X) / 2f, my = (first.Y + second.Y) / 2f;
            float dx = second.X - first.X, dy = second.Y - first.Y;
            float len = (float)Math.Sqrt(dx * dx + dy * dy);
            if (len < 0.001f) return new PointF(mx, my);
            return new PointF(mx - dy / len * offset, my + dx / len * offset);
        }

        private static void DrawLiveValue(Graphics graphics, string text, PointF center, Color color)
        {
            using (var font = DrawingTextSettings.CreateDrawingFont())
            using (var background = new SolidBrush(Color.FromArgb(210, Color.Black)))
            using (var foreground = DrawingTextSettings.CreateDrawingBrush())
            {
                SizeF size = graphics.MeasureString(text, font);
                RectangleF box = new RectangleF(center.X - size.Width / 2f - 5f,
                    center.Y - size.Height / 2f - 3f, size.Width + 10f, size.Height + 6f);
                graphics.FillRectangle(background, box);
                graphics.DrawString(text, font, foreground, box.X + 5f, box.Y + 3f);
            }
        }

        private static void DrawScreenHandle(Graphics graphics, PointF center)
        {
            using (var outline = new Pen(Color.White, 1.5f))
                graphics.DrawEllipse(outline, center.X - 5f, center.Y - 5f, 10f, 10f);
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            if (!wPt1.HasValue || !wPt2.HasValue || !lPt1.HasValue || !lPt2.HasValue)
            {
                MessageBox.Show("Please define Width and Length before saving.");
                return;
            }

            string shapeName = Microsoft.VisualBasic.Interaction.InputBox(
                "Enter Custom Shape Name:", "Save Shape", "NewShape");
            if (string.IsNullOrWhiteSpace(shapeName)) return;

            if (!CustomShapeEngine.TryExtractStoneContour(sourceFrame, out OpenCvSharp.Point[] contour))
            {
                MessageBox.Show("Error detecting stone outline for training.");
                return;
            }

            ShapeTransformMode mode = CurrentTransformMode;
            CustomShapeEngine.GetInvariantTransform(contour, mode,
                out Point2f refCenter, out double refAngle, out float span1, out float span2);

            Point2f normW1 = CustomShapeEngine.ProjectToLocal(wPt1.Value, refCenter, refAngle, span1, span2);
            Point2f normW2 = CustomShapeEngine.ProjectToLocal(wPt2.Value, refCenter, refAngle, span1, span2);
            Point2f normL1 = CustomShapeEngine.ProjectToLocal(lPt1.Value, refCenter, refAngle, span1, span2);
            Point2f normL2 = CustomShapeEngine.ProjectToLocal(lPt2.Value, refCenter, refAngle, span1, span2);

            string fingerprint = CustomShapeEngine.BuildFingerprint(
                contour, mode, refCenter, refAngle, span1, span2);

            string recordsFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "CustomShapes");
            Directory.CreateDirectory(recordsFolder);
            string safeName = MakeSafeFileName(shapeName);
            string imagePath = Path.Combine(recordsFolder,
                $"CustomShape_{safeName}_{DateTime.Now:yyyyMMdd_HHmmss}.png");
            Cv2.ImWrite(imagePath, displayFrame);

            ShapeData shape = new ShapeData
            {
                Name = shapeName,
                ImagePath = imagePath,
                TemplateMaskPath = "",
                WidthPt1 = normW1,
                WidthPt2 = normW2,
                LengthPt1 = normL1,
                LengthPt2 = normL2,
                RefAngle = (float)refAngle,
                ContourData = fingerprint,
                SnapToEdge = chkSnapToEdge.Checked,
                TransformMode = mode
            };

            DatabaseHelper.SaveShape(shape);

            MessageBox.Show(
                $"Custom Shape '{shapeName}' trained successfully.\n\n" +
                $"Behavior: {mode}\n" +
                "One-shot contour fingerprint saved.\n" +
                "Different aspect ratios are normalized during recognition.",
                "Shape Saved");

            DialogResult = DialogResult.OK;
            Close();
        }

        private static string MakeSafeFileName(string name)
        {
            foreach (char c in Path.GetInvalidFileNameChars()) name = name.Replace(c, '_');
            return string.IsNullOrWhiteSpace(name) ? "Shape" : name;
        }
    }
}
