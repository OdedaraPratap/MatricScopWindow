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
            btnSetWidth = new Button { Text = "1. Set Width (2 Clicks)", Location = new System.Drawing.Point(10, 12), Size = new System.Drawing.Size(140, 30) };
            btnSetLength = new Button { Text = "2. Set Length (2 Clicks)", Location = new System.Drawing.Point(160, 12), Size = new System.Drawing.Size(140, 30) };

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
                lblStatus.Text = "Click 2 points for WIDTH on the image.";
                pictureBox.Cursor = precisionCursor;
            };
            btnSetLength.Click += (s, e) => {
                currentState = ClickState.Length;
                lblStatus.Text = "Click 2 points for LENGTH on the image.";
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

            if (e.Button == MouseButtons.Left && currentState != ClickState.None && sourceFrame != null)
            {
                Point2f imgPt = MapScreenToImageCoordinates(e.Location);
                if (imgPt.X >= 0 && imgPt.X < sourceFrame.Width && imgPt.Y >= 0 && imgPt.Y < sourceFrame.Height)
                {
                    if (currentState == ClickState.Width)
                    {
                        if (wPt1 == null) wPt1 = imgPt;
                        else
                        {
                            wPt2 = imgPt;
                            currentState = ClickState.None;
                            lblStatus.Text = "Width points recorded.";
                            pictureBox.Cursor = Cursors.Default;
                        }
                    }
                    else if (currentState == ClickState.Length)
                    {
                        if (lPt1 == null) lPt1 = imgPt;
                        else
                        {
                            lPt2 = imgPt;
                            currentState = ClickState.None;
                            lblStatus.Text = "Length points recorded.";
                            pictureBox.Cursor = Cursors.Default;
                        }
                    }
                    RedrawOverlay();
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
        }

        private void PictureBox_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                isPanning = false;
                pictureBox.Cursor = (currentState != ClickState.None) ? precisionCursor : Cursors.Default;
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

        private void RedrawOverlay()
        {
            displayFrame = sourceFrame.Clone();
            Cv2.Circle(displayFrame, (OpenCvSharp.Point)centroid, 5, Scalar.Yellow, -1);

            // Draw Width and Real-Time mm label
            if (wPt1.HasValue && wPt2.HasValue)
            {
                Cv2.Line(displayFrame, (OpenCvSharp.Point)wPt1.Value, (OpenCvSharp.Point)wPt2.Value, Scalar.Red, 2);

                double widthPixels = wPt1.Value.DistanceTo(wPt2.Value);
                double widthMm = widthPixels * pixelToMmRatio;

                OpenCvSharp.Point wMid = new OpenCvSharp.Point((wPt1.Value.X + wPt2.Value.X) / 2, (wPt1.Value.Y + wPt2.Value.Y) / 2);
                Cv2.PutText(displayFrame, $"{widthMm:F2} mm", new OpenCvSharp.Point(wMid.X + 10, wMid.Y - 10),
                    HersheyFonts.HersheySimplex, 0.7, Scalar.Red, 2, LineTypes.AntiAlias);
            }

            // Draw Length and Real-Time mm label
            if (lPt1.HasValue && lPt2.HasValue)
            {
                Cv2.Line(displayFrame, (OpenCvSharp.Point)lPt1.Value, (OpenCvSharp.Point)lPt2.Value, Scalar.Blue, 2);

                double lengthPixels = lPt1.Value.DistanceTo(lPt2.Value);
                double lengthMm = lengthPixels * pixelToMmRatio;

                OpenCvSharp.Point lMid = new OpenCvSharp.Point((lPt1.Value.X + lPt2.Value.X) / 2, (lPt1.Value.Y + lPt2.Value.Y) / 2);
                Cv2.PutText(displayFrame, $"{lengthMm:F2} mm", new OpenCvSharp.Point(lMid.X + 10, lMid.Y + 20),
                    HersheyFonts.HersheySimplex, 0.7, Scalar.Blue, 2, LineTypes.AntiAlias);
            }

            Bitmap oldBmp = pictureBox.Image as Bitmap;
            pictureBox.Image = BitmapConverter.ToBitmap(displayFrame);
            oldBmp?.Dispose();
            pictureBox.Invalidate();
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
            string contourData;

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
                contourData = CustomShapeEngine.CreateOrientationSignature(hull, refCenter, refAngle);
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
                ContourData = contourData,
                SnapToEdge = chkSnapToEdge.Checked
            };

            DatabaseHelper.SaveShape(shape);

            MessageBox.Show($"Custom Shape '{shapeName}' Saved Successfully!");
            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}
