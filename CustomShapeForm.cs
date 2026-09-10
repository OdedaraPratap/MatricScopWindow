using Matric_scope;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using System;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Runtime.InteropServices; // Required for Custom Cursor

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
        private Label lblStatus;

        private Mat sourceFrame;
        private Mat displayFrame;
        private Mat bgFrame;

        private Point2f centroid;
        private float baseAngle;
        private Size2f refBoxSize;

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

        // ==========================================
        // WINDOWS API FOR CUSTOM CURSOR HOTSPOT
        // ==========================================
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

        // ==========================================
        // CONSTRUCTOR
        // ==========================================
        public CustomShapeForm(Mat capturedFrame, Mat backgroundFrame = null)
        {
            this.sourceFrame = capturedFrame.Clone();
            this.displayFrame = capturedFrame.Clone();

            if (backgroundFrame != null && !backgroundFrame.IsDisposed && !backgroundFrame.Empty())
            {
                this.bgFrame = backgroundFrame.Clone();
            }

            precisionCursor = CreatePrecisionCursor();
            LoadCalibration();
            InitializeUI();
            DetectBaseOrientation();
            RedrawOverlay();
        }

        private Cursor CreatePrecisionCursor()
        {
            int size = 22;   // Even size works better for 2-pixel thickness
            int center = 10; // Top-left coordinate of the 2x2 center dot
            int length = 7;  // Length of the four lines
            int gap = 2;     // Exact gap between the lines and the dot

            using (Bitmap bmp = new Bitmap(size, size))
            {
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    g.Clear(Color.Transparent);
                    Brush brush = Brushes.Black;

                    // 2-pixel thick lines (Width/Height changed to 2)
                    g.FillRectangle(brush, center, 0, 2, length); // Top
                    g.FillRectangle(brush, center, center + gap + 2, 2, length); // Bottom
                    g.FillRectangle(brush, 0, center, length, 2); // Left
                    g.FillRectangle(brush, center + gap + 2, center, length, 2); // Right

                    // 2x2 pixel Red center dot
                    g.FillRectangle(Brushes.Red, center, center, 2, 2);
                }

                IntPtr ptr = bmp.GetHicon();
                IconInfo tmp = new IconInfo();
                GetIconInfo(ptr, ref tmp);

                // Hotspot set to the exact middle of the 2x2 dot (center + 1)
                tmp.xHotspot = center + 1;
                tmp.yHotspot = center + 1;
                tmp.fIcon = false;

                IntPtr ptrCursor = CreateIconIndirect(ref tmp);
                return new Cursor(ptrCursor);
            }
        }
        private void LoadCalibration()
        {
            try
            {
                object regVal = new ModifyRegistry().Read("ppm");
                if (regVal != null && double.TryParse(regVal.ToString(), out double ppm) && ppm > 0)
                {
                    pixelToMmRatio = 1.0 / ppm;
                }
            }
            catch
            {
                MessageBox.Show("Warning: Calibration (ppm) not found. Measurements will be shown in pixels.", "Calibration Error");
            }
        }

        private void InitializeUI()
        {
            this.Text = "Custom Shape Trainer (Affine Mapping)";
            this.Size = new System.Drawing.Size(1100, 720);
            this.StartPosition = FormStartPosition.CenterScreen;

            Panel panel = new Panel { Dock = DockStyle.Top, Height = 55 };
            btnSetWidth = new Button { Text = "1. Set Width (Drag)", Location = new System.Drawing.Point(10, 12), Size = new System.Drawing.Size(140, 30) };
            btnSetLength = new Button { Text = "2. Set Length (Drag)", Location = new System.Drawing.Point(160, 12), Size = new System.Drawing.Size(140, 30) };

            chkSnapToEdge = new CheckBox
            {
                Text = "Snap to Edge",
                Location = new System.Drawing.Point(310, 17),
                Size = new System.Drawing.Size(110, 20),
                Checked = false
            };

            btnResetZoom = new Button { Text = "Reset Zoom", Location = new System.Drawing.Point(430, 12), Size = new System.Drawing.Size(95, 30) };
            btnSave = new Button { Text = "3. Save Shape", Location = new System.Drawing.Point(535, 12), Size = new System.Drawing.Size(105, 30) };
            lblStatus = new Label { Text = "Status: Select Width or Length mode.", Location = new System.Drawing.Point(650, 17), Size = new System.Drawing.Size(420, 20) };

            panel.Controls.AddRange(new Control[] { btnSetWidth, btnSetLength, chkSnapToEdge, btnResetZoom, btnSave, lblStatus });
            this.Controls.Add(panel);

            pictureBox = new PictureBox
            {
                Dock = DockStyle.Fill,
                BorderStyle = BorderStyle.Fixed3D,
                BackColor = Color.LightGray // Softened background to see black crosshair clearly off the stone
            };

            pictureBox.MouseWheel += PictureBox_MouseWheel;
            pictureBox.MouseDown += PictureBox_MouseDown;
            pictureBox.MouseMove += PictureBox_MouseMove;
            pictureBox.MouseUp += PictureBox_MouseUp;
            pictureBox.Paint += PictureBox_Paint;

            this.Controls.Add(pictureBox);

            btnSetWidth.Click += (s, e) => {
                currentState = ClickState.Width;
                lblStatus.Text = "Drag from the centroid to set WIDTH, or drag either red endpoint.";
                pictureBox.Cursor = precisionCursor;
            };
            btnSetLength.Click += (s, e) => {
                currentState = ClickState.Length;
                lblStatus.Text = "Drag from the centroid to set LENGTH, or drag either blue endpoint.";
                pictureBox.Cursor = precisionCursor;
            };

            btnResetZoom.Click += (s, e) => { ResetZoomAndPan(); };
            btnSave.Click += BtnSave_Click;
        }

        private void DetectBaseOrientation()
        {
            using (Mat gray = new Mat())
            using (Mat edges = new Mat())
            {
                Cv2.CvtColor(sourceFrame, gray, ColorConversionCodes.BGR2GRAY);
                Cv2.MedianBlur(gray, gray, 5);
                Cv2.Canny(gray, edges, 40, 120);

                using (Mat element = Cv2.GetStructuringElement(MorphShapes.Rect, new OpenCvSharp.Size(9, 9)))
                {
                    Cv2.MorphologyEx(edges, edges, MorphTypes.Close, element);
                }

                Cv2.FindContours(edges, out OpenCvSharp.Point[][] contours, out _, RetrievalModes.External, ContourApproximationModes.ApproxSimple);

                var largest = contours?.Where(c => Cv2.ContourArea(c) > 1200)
                                     .OrderByDescending(c => Cv2.ContourArea(c))
                                     .FirstOrDefault();

                if (largest != null)
                {
                    var hull = Cv2.ConvexHull(largest);
                    RotatedRect box = Cv2.MinAreaRect(hull);
                    centroid = box.Center;
                    baseAngle = box.Angle;
                    refBoxSize = box.Size;
                }
                else
                {
                    MessageBox.Show("Vision Error: Stone contour not detected.", "Vision Warning");
                    centroid = new Point2f(sourceFrame.Width / 2f, sourceFrame.Height / 2f);
                    baseAngle = 0f;
                    refBoxSize = new Size2f(100, 100);
                }
            }
        }

        private void PictureBox_MouseWheel(object sender, MouseEventArgs e)
        {
            float oldZoom = zoomFactor;
            if (e.Delta > 0)
                zoomFactor = Math.Min(zoomFactor * 1.25f, MAX_ZOOM);
            else
                zoomFactor = Math.Max(zoomFactor / 1.25f, MIN_ZOOM);

            if (zoomFactor == MIN_ZOOM)
            {
                panOffset = new System.Drawing.Point(0, 0);
            }
            else
            {
                float zoomRatio = zoomFactor / oldZoom;
                panOffset.X = (int)(e.X - (e.X - panOffset.X) * zoomRatio);
                panOffset.Y = (int)(e.Y - (e.Y - panOffset.Y) * zoomRatio);
            }
            pictureBox.Invalidate();
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

            if (e.Button == MouseButtons.Left && sourceFrame != null)
            {
                Point2f imgPt = MapScreenToImageCoordinates(e.Location);
                if (imgPt.X >= 0 && imgPt.X < sourceFrame.Width && imgPt.Y >= 0 && imgPt.Y < sourceFrame.Height)
                {
                    activeHandle = HitTestMeasurementHandle(e.Location);

                    if (activeHandle == DragHandle.None && currentState != ClickState.None)
                    {
                        // One click-drag defines a complete axis. The opposite end
                        // is mirrored through the detected centroid automatically.
                        if (currentState == ClickState.Width)
                        {
                            activeHandle = DragHandle.WidthPoint2;
                            SetSymmetricEndpoint(activeHandle, imgPt);
                        }
                        else
                        {
                            activeHandle = DragHandle.LengthPoint2;
                            SetSymmetricEndpoint(activeHandle, imgPt);
                        }
                    }

                    if (activeHandle != DragHandle.None)
                    {
                        pictureBox.Capture = true;
                        pictureBox.Cursor = Cursors.SizeAll;
                        SetSymmetricEndpoint(activeHandle, imgPt);
                        UpdateCustomMeasurementStatus();
                        RedrawOverlay();
                    }
                }
            }
        }

        private void PictureBox_MouseMove(object sender, MouseEventArgs e)
        {
            if (isPanning)
            {
                panOffset.X += (e.X - dragStart.X);
                panOffset.Y += (e.Y - dragStart.Y);
                dragStart = e.Location;
                pictureBox.Invalidate();
            }
            else if (activeHandle != DragHandle.None)
            {
                Point2f imgPt = ClampToSource(MapScreenToImageCoordinates(e.Location));
                SetSymmetricEndpoint(activeHandle, imgPt);
                UpdateCustomMeasurementStatus();
                RedrawOverlay();
                // MouseMove can fire faster than normal invalidated paints. Force
                // one synchronous repaint so the operator sees every new value
                // during the drag rather than only after releasing the mouse.
                lblStatus.Refresh();
                pictureBox.Refresh();
            }
            else
            {
                DragHandle hoverHandle = HitTestMeasurementHandle(e.Location);
                pictureBox.Cursor = hoverHandle != DragHandle.None
                    ? Cursors.SizeAll
                    : (currentState != ClickState.None ? precisionCursor : Cursors.Default);
            }
        }

        private void PictureBox_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                isPanning = false;
                pictureBox.Cursor = (currentState != ClickState.None) ? precisionCursor : Cursors.Default;
            }
            else if (e.Button == MouseButtons.Left && activeHandle != DragHandle.None)
            {
                activeHandle = DragHandle.None;
                currentState = ClickState.None;
                pictureBox.Capture = false;
                pictureBox.Cursor = Cursors.Default;
                UpdateCustomMeasurementStatus();
                RedrawOverlay();
                lblStatus.Refresh();
                pictureBox.Refresh();
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
            zoomFactor = 1.0f;
            panOffset = new System.Drawing.Point(0, 0);
            pictureBox.Invalidate();
        }

        private Rectangle GetAspectFitRectangle()
        {
            int imgW = sourceFrame.Width;
            int imgH = sourceFrame.Height;
            int boxW = pictureBox.Width;
            int boxH = pictureBox.Height;
            float ratio = Math.Min((float)boxW / imgW, (float)boxH / imgH);
            int targetW = (int)(imgW * ratio);
            int targetH = (int)(imgH * ratio);
            int targetX = (boxW - targetW) / 2;
            int targetY = (boxH - targetH) / 2;
            return new Rectangle(targetX, targetY, targetW, targetH);
        }

        private Point2f MapScreenToImageCoordinates(System.Drawing.Point screenPt)
        {
            float x = screenPt.X - panOffset.X;
            float y = screenPt.Y - panOffset.Y;
            x /= zoomFactor;
            y /= zoomFactor;
            Rectangle targetRect = GetAspectFitRectangle();
            float imgX = (x - targetRect.X) * ((float)sourceFrame.Width / targetRect.Width);
            float imgY = (y - targetRect.Y) * ((float)sourceFrame.Height / targetRect.Height);
            return new Point2f(imgX, imgY);
        }

        private PointF MapImageToScreenCoordinates(Point2f imagePoint)
        {
            Rectangle targetRect = GetAspectFitRectangle();
            float fittedX = targetRect.X + imagePoint.X * targetRect.Width / sourceFrame.Width;
            float fittedY = targetRect.Y + imagePoint.Y * targetRect.Height / sourceFrame.Height;
            return new PointF(fittedX * zoomFactor + panOffset.X,
                fittedY * zoomFactor + panOffset.Y);
        }

        private DragHandle HitTestMeasurementHandle(System.Drawing.Point mousePoint)
        {
            const double grabRadius = 10.0;
            PointF mouse = new PointF(mousePoint.X, mousePoint.Y);

            if (wPt1.HasValue && ScreenDistance(mouse, MapImageToScreenCoordinates(wPt1.Value)) <= grabRadius)
                return DragHandle.WidthPoint1;
            if (wPt2.HasValue && ScreenDistance(mouse, MapImageToScreenCoordinates(wPt2.Value)) <= grabRadius)
                return DragHandle.WidthPoint2;
            if (lPt1.HasValue && ScreenDistance(mouse, MapImageToScreenCoordinates(lPt1.Value)) <= grabRadius)
                return DragHandle.LengthPoint1;
            if (lPt2.HasValue && ScreenDistance(mouse, MapImageToScreenCoordinates(lPt2.Value)) <= grabRadius)
                return DragHandle.LengthPoint2;

            return DragHandle.None;
        }

        private static double ScreenDistance(PointF first, PointF second)
        {
            double dx = second.X - first.X;
            double dy = second.Y - first.Y;
            return Math.Sqrt(dx * dx + dy * dy);
        }

        private Point2f ClampToSource(Point2f point)
        {
            return new Point2f(
                Math.Max(0f, Math.Min(sourceFrame.Width - 1f, point.X)),
                Math.Max(0f, Math.Min(sourceFrame.Height - 1f, point.Y)));
        }

        private void SetSymmetricEndpoint(DragHandle handle, Point2f draggedPoint)
        {
            draggedPoint = ClampToSource(draggedPoint);
            float dx = draggedPoint.X - centroid.X;
            float dy = draggedPoint.Y - centroid.Y;
            float scale = Math.Min(SymmetricAxisScale(centroid.X, dx, sourceFrame.Width - 1f),
                SymmetricAxisScale(centroid.Y, dy, sourceFrame.Height - 1f));
            draggedPoint = new Point2f(centroid.X + dx * scale, centroid.Y + dy * scale);
            Point2f opposite = new Point2f(centroid.X - dx * scale, centroid.Y - dy * scale);

            switch (handle)
            {
                case DragHandle.WidthPoint1:
                    wPt1 = draggedPoint;
                    wPt2 = opposite;
                    break;
                case DragHandle.WidthPoint2:
                    wPt2 = draggedPoint;
                    wPt1 = opposite;
                    break;
                case DragHandle.LengthPoint1:
                    lPt1 = draggedPoint;
                    lPt2 = opposite;
                    break;
                case DragHandle.LengthPoint2:
                    lPt2 = draggedPoint;
                    lPt1 = opposite;
                    break;
            }
        }

        private static float SymmetricAxisScale(float center, float delta, float maximum)
        {
            if (Math.Abs(delta) < 0.0001f) return 1f;

            float forwardRoom = delta > 0f ? maximum - center : center;
            float oppositeRoom = delta > 0f ? center : maximum - center;
            float required = Math.Abs(delta);
            return Math.Min(1f, Math.Min(forwardRoom / required, oppositeRoom / required));
        }

        private void UpdateCustomMeasurementStatus()
        {
            string widthText = wPt1.HasValue && wPt2.HasValue
                ? string.Format("Width {0:F2} mm", wPt1.Value.DistanceTo(wPt2.Value) * pixelToMmRatio)
                : "Width --";
            string lengthText = lPt1.HasValue && lPt2.HasValue
                ? string.Format("Length {0:F2} mm", lPt1.Value.DistanceTo(lPt2.Value) * pixelToMmRatio)
                : "Length --";
            lblStatus.Text = widthText + "  |  " + lengthText;
        }

        private void RedrawOverlay()
        {
            displayFrame?.Dispose();
            displayFrame = sourceFrame.Clone();

            // Draw the width axis; its live value is rendered in screen space.
            if (wPt1.HasValue && wPt2.HasValue)
            {
                Cv2.Line(displayFrame, (OpenCvSharp.Point)wPt1.Value, (OpenCvSharp.Point)wPt2.Value, Scalar.Red, 2);
            }

            // Draw the length axis; its live value is rendered in screen space.
            if (lPt1.HasValue && lPt2.HasValue)
            {
                Cv2.Line(displayFrame, (OpenCvSharp.Point)lPt1.Value, (OpenCvSharp.Point)lPt2.Value, Scalar.Blue, 2);
            }

            Bitmap oldBmp = pictureBox.Image as Bitmap;
            pictureBox.Image = BitmapConverter.ToBitmap(displayFrame);
            oldBmp?.Dispose();
            pictureBox.Invalidate();
        }

        private void DrawEditableHandles(Graphics graphics)
        {
            DrawScreenHandle(graphics, MapImageToScreenCoordinates(centroid));
            if (wPt1.HasValue) DrawScreenHandle(graphics, MapImageToScreenCoordinates(wPt1.Value));
            if (wPt2.HasValue) DrawScreenHandle(graphics, MapImageToScreenCoordinates(wPt2.Value));
            if (lPt1.HasValue) DrawScreenHandle(graphics, MapImageToScreenCoordinates(lPt1.Value));
            if (lPt2.HasValue) DrawScreenHandle(graphics, MapImageToScreenCoordinates(lPt2.Value));

            // Draw values as screen-space overlays as well as in the saved image.
            // These labels repaint synchronously on every drag movement and stay
            // the same readable size at every zoom level.
            if (wPt1.HasValue && wPt2.HasValue)
            {
                PointF first = MapImageToScreenCoordinates(wPt1.Value);
                PointF second = MapImageToScreenCoordinates(wPt2.Value);
                string width = string.Format("Width {0:F2} mm",
                    wPt1.Value.DistanceTo(wPt2.Value) * pixelToMmRatio);
                DrawLiveValue(graphics, width, OffsetFromLineMidpoint(first, second, 24f), Color.Red);
            }

            if (lPt1.HasValue && lPt2.HasValue)
            {
                PointF first = MapImageToScreenCoordinates(lPt1.Value);
                PointF second = MapImageToScreenCoordinates(lPt2.Value);
                string length = string.Format("Length {0:F2} mm",
                    lPt1.Value.DistanceTo(lPt2.Value) * pixelToMmRatio);
                DrawLiveValue(graphics, length, OffsetFromLineMidpoint(first, second, -24f), Color.DeepSkyBlue);
            }
        }

        private static PointF OffsetFromLineMidpoint(PointF first, PointF second, float offset)
        {
            float middleX = (first.X + second.X) / 2f;
            float middleY = (first.Y + second.Y) / 2f;
            float dx = second.X - first.X;
            float dy = second.Y - first.Y;
            float length = (float)Math.Sqrt(dx * dx + dy * dy);
            if (length < 0.001f) return new PointF(middleX, middleY);
            return new PointF(middleX - dy / length * offset, middleY + dx / length * offset);
        }

        private static void DrawLiveValue(Graphics graphics, string text, PointF center, Color color)
        {
            using (var font = new Font("Microsoft Sans Serif", 11f, FontStyle.Bold))
            using (var background = new SolidBrush(Color.FromArgb(210, Color.Black)))
            using (var foreground = new SolidBrush(color))
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
            {
                graphics.DrawEllipse(outline, center.X - 5f, center.Y - 5f, 10f, 10f);
            }
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            if (wPt1 == null || wPt2 == null || lPt1 == null || lPt2 == null)
            {
                MessageBox.Show("Please click 2 points for Width and 2 points for Length before saving.");
                return;
            }

            string shapeName = Microsoft.VisualBasic.Interaction.InputBox("Enter Custom Shape Name:", "Save Shape", "NewShape");
            if (string.IsNullOrWhiteSpace(shapeName)) return;

            Point2f refCenter;
            double refAngle;
            float span1, span2;

            using (Mat gray = new Mat())
            using (Mat edges = new Mat())
            {
                Cv2.CvtColor(sourceFrame, gray, ColorConversionCodes.BGR2GRAY);
                Cv2.MedianBlur(gray, gray, 5);
                Cv2.Canny(gray, edges, 40, 120);

                using (Mat element = Cv2.GetStructuringElement(MorphShapes.Rect, new OpenCvSharp.Size(9, 9)))
                {
                    Cv2.MorphologyEx(edges, edges, MorphTypes.Close, element);
                }

                Cv2.FindContours(edges, out OpenCvSharp.Point[][] contours, out _, RetrievalModes.External, ContourApproximationModes.ApproxSimple);
                var largest = contours?.Where(c => Cv2.ContourArea(c) > 1200).OrderByDescending(c => Cv2.ContourArea(c)).FirstOrDefault();

                if (largest == null)
                {
                    MessageBox.Show("Error detecting stone outline for training.");
                    return;
                }

                var hull = Cv2.ConvexHull(largest);

                // Extact Bi-Axial independent dimensions based on the new Spatial Mathematics
                CustomShapeEngine.GetInvariantTransform(hull, out refCenter, out refAngle, out span1, out span2);
            }

            string recordsFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "CustomShapes");
            if (!Directory.Exists(recordsFolder)) Directory.CreateDirectory(recordsFolder);
            string imagePath = Path.Combine(recordsFolder, $"CustomShape_{shapeName}_{DateTime.Now:yyyyMMdd_HHmmss}.png");
            Cv2.ImWrite(imagePath, displayFrame);

            // Project screen clicks using Bi-Axial scaling
            Point2f normW1 = CustomShapeEngine.ProjectToLocal(wPt1.Value, refCenter, refAngle, span1, span2);
            Point2f normW2 = CustomShapeEngine.ProjectToLocal(wPt2.Value, refCenter, refAngle, span1, span2);
            Point2f normL1 = CustomShapeEngine.ProjectToLocal(lPt1.Value, refCenter, refAngle, span1, span2);
            Point2f normL2 = CustomShapeEngine.ProjectToLocal(lPt2.Value, refCenter, refAngle, span1, span2);

            ShapeData shape = new ShapeData
            {
                Name = shapeName,
                ImagePath = imagePath,
                WidthPt1 = normW1,
                WidthPt2 = normW2,
                LengthPt1 = normL1,
                LengthPt2 = normL2,
                RefAngle = 0f,
                ContourData = "",
                SnapToEdge = chkSnapToEdge.Checked
            };

            DatabaseHelper.SaveShape(shape);

            MessageBox.Show($"Custom Shape '{shapeName}' Saved Successfully!");
            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}
