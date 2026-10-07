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
        private ComboBox cmbSegmentationMode;
        private ComboBox cmbPoseSolver;
        private CheckBox chkRejectAmbiguous;
        private CheckBox chkAllowEquivalentSymmetry;
        private Button btnPreviewDetection;
        private Label lblStatus;
        private Label lblModeHelp;
        private Label lblSegHelp;
        private Label lblPoseHelp;
        private Label lblCompactHelp;
        private ToolTip optionToolTip;
        private TableLayoutPanel rootLayout;
        private Panel controlsPanel;
        private Button btnToggleControls;
        private Button btnMinimizeForm;
        private Button btnCloseForm;
        private bool controlsCollapsed = false;
        private const int EXPANDED_CONTROL_HEIGHT = 152;
        private const int COLLAPSED_CONTROL_HEIGHT = 28;

        // Window starts in a normal centered size. F11 alone enters full screen.
        private bool isFullScreen = false;
        private Rectangle normalWindowBounds;

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
            // V5.5 NORMAL-SIZE START + F11 FULL SCREEN
            // The trainer now opens as a normal centered window. It does NOT start as if
            // F11 were already pressed. Press F11 only when you want borderless full screen.
            Text = "Custom Shape Trainer - Robust V5.5 / CSF6";
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.None;
            WindowState = FormWindowState.Normal;
            Size = new System.Drawing.Size(1180, 760);
            MinimumSize = new System.Drawing.Size(900, 620);
            isFullScreen = false;
            BackColor = Color.FromArgb(18, 18, 18);
            ForeColor = Color.White;
            Font = new Font("Segoe UI", 8.5f, FontStyle.Regular);
            KeyPreview = true;
            DoubleBuffered = true;

            optionToolTip = new ToolTip
            {
                AutoPopDelay = 14000,
                InitialDelay = 350,
                ReshowDelay = 100,
                ShowAlways = true
            };

            rootLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 2,
                ColumnCount = 1,
                Margin = Padding.Empty,
                Padding = Padding.Empty,
                BackColor = Color.FromArgb(18, 18, 18)
            };
            rootLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, EXPANDED_CONTROL_HEIGHT));
            rootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            controlsPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(35, 35, 38),
                Margin = Padding.Empty,
                Padding = Padding.Empty
            };

            // V5.4: the title buttons are placed in a dedicated TableLayoutPanel.
            // Do NOT position them with Form.Width during construction: at that time the
            // maximized client width is not guaranteed to be available, which was why the
            // V5.3 Close button could be positioned off-screen or overlap other controls.
            TableLayoutPanel headerBar = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 28,
                RowCount = 1,
                ColumnCount = 2,
                Margin = Padding.Empty,
                Padding = Padding.Empty,
                BackColor = Color.FromArgb(255, 128, 0)
            };
            headerBar.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            headerBar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            headerBar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 232f));

            Label title = new Label
            {
                Text = "  CUSTOM SHAPE TRAINING   |   F2: Hide/Show Controls   |   F11: Full Screen   |   Wheel: Zoom   |   Right Drag: Pan",
                ForeColor = Color.White,
                BackColor = Color.FromArgb(255, 128, 0),
                Font = new Font("Segoe UI", 9.0f, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft,
                Dock = DockStyle.Fill,
                AutoEllipsis = true,
                Margin = Padding.Empty
            };

            TableLayoutPanel headerActions = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 1,
                ColumnCount = 3,
                Margin = Padding.Empty,
                Padding = new Padding(2, 2, 4, 2),
                BackColor = Color.FromArgb(255, 128, 0)
            };
            headerActions.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            headerActions.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 112f));
            headerActions.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 34f));
            headerActions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));

            btnToggleControls = CreateActionButton("Hide Controls", 0, 0, 108, 24,
                Color.FromArgb(60, 60, 64), Color.White);
            btnToggleControls.Dock = DockStyle.Fill;
            btnToggleControls.Margin = new Padding(1, 0, 3, 0);

            btnMinimizeForm = CreateActionButton("_", 0, 0, 28, 24,
                Color.FromArgb(60, 60, 64), Color.White);
            btnMinimizeForm.Dock = DockStyle.Fill;
            btnMinimizeForm.Margin = new Padding(1, 0, 3, 0);

            btnCloseForm = CreateActionButton("CLOSE  X", 0, 0, 78, 24,
                Color.FromArgb(190, 55, 55), Color.White);
            btnCloseForm.Dock = DockStyle.Fill;
            btnCloseForm.Margin = new Padding(1, 0, 0, 0);
            btnCloseForm.TabStop = false;

            headerActions.Controls.Add(btnToggleControls, 0, 0);
            headerActions.Controls.Add(btnMinimizeForm, 1, 0);
            headerActions.Controls.Add(btnCloseForm, 2, 0);
            headerBar.Controls.Add(title, 0, 0);
            headerBar.Controls.Add(headerActions, 1, 0);

            // Row 1: drawing actions + measurement mode.
            btnSetWidth = CreateActionButton("Set Width", 8, 34, 90, 26,
                Color.FromArgb(245, 245, 245), Color.Black);
            btnSetLength = CreateActionButton("Set Length", 102, 34, 90, 26,
                Color.FromArgb(245, 245, 245), Color.Black);
            btnResetZoom = CreateActionButton("Reset", 196, 34, 68, 26,
                Color.FromArgb(225, 225, 225), Color.Black);
            btnPreviewDetection = CreateActionButton("Preview", 268, 34, 80, 26,
                Color.FromArgb(255, 193, 7), Color.Black);
            btnSave = CreateActionButton("Train + Save", 352, 34, 100, 26,
                Color.FromArgb(76, 175, 80), Color.White);

            Label lblMode = CreateCompactCaption("Measurement", 462, 35, 88);
            cmbTransformMode = CreateReadableComboBox(552, 34, 288);
            cmbTransformMode.DropDownWidth = 540;
            cmbTransformMode.Items.Add("Centroid Axis - trained direction through center");
            cmbTransformMode.Items.Add("Free Offset - preserve trained off-center line");
            cmbTransformMode.Items.Add("Relative Landmark - preserve percentage position");
            cmbTransformMode.Items.Add("Template Registration - preserve exact trained line");
            cmbTransformMode.Items.Add("Smart Auto - exact trained line + safe caliper fallback");
            cmbTransformMode.Items.Add("Auto Caliper - orientation independent dimensions");
            cmbTransformMode.SelectedIndex = 4;

            chkSnapToEdge = CreateCompactCheckBox("Snap to contour", 846, 36, 125, true);

            // Row 2: segmentation + pose + safety options.
            Label lblSeg = CreateCompactCaption("Segmentation", 8, 68, 82);
            cmbSegmentationMode = CreateReadableComboBox(92, 67, 300);
            cmbSegmentationMode.DropDownWidth = 410;
            cmbSegmentationMode.Items.Add("Auto - Background + Otsu + Canny");
            cmbSegmentationMode.Items.Add("Otsu - dark object / light background");
            cmbSegmentationMode.Items.Add("Background Difference");
            cmbSegmentationMode.Items.Add("Canny Closed Contour");
            cmbSegmentationMode.SelectedIndex = 0;

            Label lblPose = CreateCompactCaption("Pose", 402, 68, 40);
            cmbPoseSolver = CreateReadableComboBox(444, 67, 260);
            cmbPoseSolver.DropDownWidth = 390;
            cmbPoseSolver.Items.Add("Auto Hybrid - contour + reliable PCA/Rect");
            cmbPoseSolver.Items.Add("Contour Correspondence only");
            cmbPoseSolver.Items.Add("PCA Assisted");
            cmbPoseSolver.Items.Add("MinAreaRect Assisted");
            cmbPoseSolver.SelectedIndex = 0;

            chkRejectAmbiguous = CreateCompactCheckBox("Reject ambiguous", 714, 69, 145, true);
            chkAllowEquivalentSymmetry = CreateCompactCheckBox("Allow equal W/L symmetry", 862, 69, 176, false);

            // Thin visible tip row. These are the same live explanations as V5.1,
            // but arranged horizontally so they consume very little image height.
            lblModeHelp = CreateCompactHelpLabel(8, 94, 430, 30,
                Color.FromArgb(45, 55, 68), Color.FromArgb(225, 235, 250));
            lblSegHelp = CreateCompactHelpLabel(442, 94, 430, 30,
                Color.FromArgb(56, 50, 38), Color.FromArgb(255, 238, 190));
            lblPoseHelp = CreateCompactHelpLabel(876, 94, 430, 30,
                Color.FromArgb(48, 60, 48), Color.FromArgb(220, 245, 220));

            lblStatus = new Label
            {
                Text = "Status: Preview contour, then draw Width and Length.",
                ForeColor = Color.FromArgb(255, 235, 150),
                BackColor = Color.FromArgb(50, 50, 54),
                Font = new Font("Segoe UI", 8.3f, FontStyle.Bold),
                Location = new System.Drawing.Point(8, 128),
                Height = 20,
                TextAlign = ContentAlignment.MiddleLeft,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            lblStatus.Width = Math.Max(600, Width - 16);

            controlsPanel.Controls.AddRange(new Control[]
            {
                headerBar,
                btnSetWidth, btnSetLength, btnResetZoom, btnPreviewDetection, btnSave,
                lblMode, cmbTransformMode, chkSnapToEdge,
                lblSeg, cmbSegmentationMode,
                lblPose, cmbPoseSolver,
                chkRejectAmbiguous, chkAllowEquivalentSymmetry,
                lblModeHelp, lblSegHelp, lblPoseHelp,
                lblStatus
            });

            pictureBox = new PictureBox
            {
                Dock = DockStyle.Fill,
                BorderStyle = BorderStyle.None,
                BackColor = Color.FromArgb(205, 205, 205),
                TabStop = true,
                Margin = Padding.Empty
            };

            rootLayout.Controls.Add(controlsPanel, 0, 0);
            rootLayout.Controls.Add(pictureBox, 0, 1);
            Controls.Add(rootLayout);

            pictureBox.MouseWheel += PictureBox_MouseWheel;
            pictureBox.MouseDown += PictureBox_MouseDown;
            pictureBox.MouseMove += PictureBox_MouseMove;
            pictureBox.MouseUp += PictureBox_MouseUp;
            pictureBox.Paint += PictureBox_Paint;

            optionToolTip.SetToolTip(cmbTransformMode,
                "Measurement behavior. Smart Auto is the normal default; Template/Free preserve off-center trained lines.");
            optionToolTip.SetToolTip(cmbSegmentationMode,
                "How the stone boundary is extracted. Auto is the default; Otsu is strong for dark stones on a light background.");
            optionToolTip.SetToolTip(cmbPoseSolver,
                "How orientation is assisted. Auto Hybrid is the default; contour matching remains primary.");
            optionToolTip.SetToolTip(chkRejectAmbiguous,
                "Recommended ON. Prevents the software from guessing between equally valid rotations.");
            optionToolTip.SetToolTip(chkAllowEquivalentSymmetry,
                "Enable only when symmetric rotations produce the same physical Width/Length result.");
            optionToolTip.SetToolTip(btnToggleControls,
                "F2 hides the controls so almost the entire screen can be used for accurate line placement.");
            optionToolTip.SetToolTip(btnCloseForm,
                "Close the Custom Shape training form. You can also press Esc.");

            btnSetWidth.Click += (s, e) =>
            {
                currentState = ClickState.Width;
                lblStatus.Text = IsCentroidMode
                    ? "WIDTH: drag one endpoint; opposite endpoint is mirrored through centroid."
                    : "WIDTH: drag freely on the exact location you want to preserve.";
                pictureBox.Focus();
                pictureBox.Cursor = precisionCursor;
            };

            btnSetLength.Click += (s, e) =>
            {
                currentState = ClickState.Length;
                lblStatus.Text = IsCentroidMode
                    ? "LENGTH: drag one endpoint; opposite endpoint is mirrored through centroid."
                    : "LENGTH: drag freely on the exact location you want to preserve.";
                pictureBox.Focus();
                pictureBox.Cursor = precisionCursor;
            };

            cmbTransformMode.SelectedIndexChanged += (s, e) =>
            {
                wPt1 = wPt2 = lPt1 = lPt2 = null;
                currentState = ClickState.None;
                DetectBaseOrientation();
                UpdateOptionHelpText();
                UpdateMeasurementModeStatus();
                RedrawOverlay();
            };

            cmbSegmentationMode.SelectedIndexChanged += (s, e) =>
            {
                DetectBaseOrientation();
                UpdateOptionHelpText();
            };

            cmbPoseSolver.SelectedIndexChanged += (s, e) => UpdateOptionHelpText();
            chkRejectAmbiguous.CheckedChanged += (s, e) => UpdateOptionHelpText();
            chkAllowEquivalentSymmetry.CheckedChanged += (s, e) => UpdateOptionHelpText();

            btnResetZoom.Click += (s, e) => ResetZoomAndPan();
            btnPreviewDetection.Click += (s, e) => PreviewDetection();
            btnSave.Click += BtnSave_Click;
            btnToggleControls.Click += (s, e) => ToggleControlsBar();
            btnMinimizeForm.Click += (s, e) => WindowState = FormWindowState.Minimized;
            btnCloseForm.Click += (s, e) => Close();
            KeyDown += CustomShapeForm_KeyDown;

            // Header buttons are docked, so they stay visible regardless of monitor size.
            // Only the tip/status areas need manual width adjustment.
            Action updateResponsiveLayout = () =>
            {
                int cw = controlsPanel.ClientSize.Width;
                if (lblStatus != null) lblStatus.Width = Math.Max(300, cw - 16);

                int gap = 4;
                int left = 8;
                int usable = Math.Max(300, cw - 16);
                int each = Math.Max(90, (usable - gap * 2) / 3);
                if (lblModeHelp != null)
                {
                    lblModeHelp.Location = new System.Drawing.Point(left, 94);
                    lblModeHelp.Width = each;
                }
                if (lblSegHelp != null)
                {
                    lblSegHelp.Location = new System.Drawing.Point(left + each + gap, 94);
                    lblSegHelp.Width = each;
                }
                if (lblPoseHelp != null)
                {
                    lblPoseHelp.Location = new System.Drawing.Point(left + (each + gap) * 2, 94);
                    lblPoseHelp.Width = Math.Max(90, usable - (each + gap) * 2);
                }
            };
            controlsPanel.Resize += (s, e) => updateResponsiveLayout();
            Shown += (s, e) =>
            {
                updateResponsiveLayout();
                btnCloseForm.BringToFront();

                // Save the real centered window position/size for F11 restore.
                if (!isFullScreen && WindowState == FormWindowState.Normal)
                    normalWindowBounds = Bounds;
            };

            UpdateOptionHelpText();
            UpdateMeasurementModeStatus();
        }

        private void CustomShapeForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                Close();
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.F2)
            {
                ToggleControlsBar();
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.F11)
            {
                ToggleFullScreen();
                e.Handled = true;
            }
        }

        private void ToggleFullScreen()
        {
            if (!isFullScreen)
            {
                // Remember exactly where/what size the normal window was.
                if (WindowState == FormWindowState.Normal)
                    normalWindowBounds = Bounds;

                isFullScreen = true;

                // Keep your custom header/close button and fill the current monitor.
                WindowState = FormWindowState.Normal;
                FormBorderStyle = FormBorderStyle.None;
                Bounds = Screen.FromControl(this).Bounds;
                TopMost = false;
            }
            else
            {
                isFullScreen = false;
                WindowState = FormWindowState.Normal;
                FormBorderStyle = FormBorderStyle.None;

                if (normalWindowBounds.Width >= 900 && normalWindowBounds.Height >= 620)
                {
                    Bounds = normalWindowBounds;
                }
                else
                {
                    Size = new System.Drawing.Size(1180, 760);
                    StartPosition = FormStartPosition.CenterScreen;
                    CenterToScreen();
                }
            }

            //updateResponsiveLayout();
            btnCloseForm?.BringToFront();
        }

        private void ToggleControlsBar()
        {
            if (rootLayout == null || controlsPanel == null) return;

            controlsCollapsed = !controlsCollapsed;
            int h = controlsCollapsed ? COLLAPSED_CONTROL_HEIGHT : EXPANDED_CONTROL_HEIGHT;
            rootLayout.RowStyles[0].SizeType = SizeType.Absolute;
            rootLayout.RowStyles[0].Height = h;
            controlsPanel.Height = h;
            btnToggleControls.Text = controlsCollapsed ? "Show Controls" : "Hide Controls";

            if (controlsCollapsed)
                lblStatus.Text = "Controls hidden. Press F2 to show them again.";

            pictureBox.Focus();
            pictureBox.Invalidate();
        }

        private static Button CreateActionButton(string text, int x, int y, int w, int h,
            Color background, Color foreground)
        {
            return new Button
            {
                Text = text,
                Location = new System.Drawing.Point(x, y),
                Size = new System.Drawing.Size(w, h),
                BackColor = background,
                ForeColor = foreground,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.0f, FontStyle.Bold),
                UseVisualStyleBackColor = false
            };
        }

        private static ComboBox CreateReadableComboBox(int x, int y, int width)
        {
            return new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Location = new System.Drawing.Point(x, y),
                Size = new System.Drawing.Size(width, 25),
                BackColor = Color.White,
                ForeColor = Color.Black,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Regular),
                FlatStyle = FlatStyle.Standard
            };
        }

        private static Label CreateCompactCaption(string text, int x, int y, int width)
        {
            return new Label
            {
                Text = text,
                ForeColor = Color.FromArgb(255, 193, 7),
                BackColor = Color.Transparent,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                Location = new System.Drawing.Point(x, y),
                Size = new System.Drawing.Size(width, 24),
                TextAlign = ContentAlignment.MiddleLeft
            };
        }

        private static CheckBox CreateCompactCheckBox(string text, int x, int y, int width, bool isChecked)
        {
            return new CheckBox
            {
                Text = text,
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                Font = new Font("Segoe UI", 8.3f, FontStyle.Bold),
                Location = new System.Drawing.Point(x, y),
                Size = new System.Drawing.Size(width, 22),
                Checked = isChecked,
                UseVisualStyleBackColor = false
            };
        }

        private static Label CreateCompactHelpLabel(int x, int y, int width, int height,
            Color background, Color foreground)
        {
            return new Label
            {
                Text = "Tip",
                ForeColor = foreground,
                BackColor = background,
                Font = new Font("Segoe UI", 7.9f, FontStyle.Regular),
                Location = new System.Drawing.Point(x, y),
                Size = new System.Drawing.Size(width, height),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(6, 1, 6, 1),
                AutoEllipsis = true
            };
        }

        private static Label CreateCaptionLabel(string text, int x, int y, int width)
        {
            return new Label
            {
                Text = text,
                ForeColor = Color.FromArgb(255, 193, 7),
                BackColor = Color.Transparent,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Location = new System.Drawing.Point(x, y),
                Size = new System.Drawing.Size(width, 26),
                TextAlign = ContentAlignment.MiddleLeft
            };
        }

        private static Label CreateHelpLabel(int x, int y, int width, int height)
        {
            return new Label
            {
                ForeColor = Color.FromArgb(220, 220, 220),
                BackColor = Color.FromArgb(46, 46, 50),
                Font = new Font("Segoe UI", 8.5f, FontStyle.Regular),
                Location = new System.Drawing.Point(x, y),
                Size = new System.Drawing.Size(width, height),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(5, 0, 5, 0)
            };
        }

        private void UpdateMeasurementModeStatus()
        {
            if (lblStatus == null) return;

            if (IsTemplateRegistrationMode)
                lblStatus.Text = "Template Registration: exact trained line position/direction follows robust contour registration.";
            else if (IsSmartAutoMode)
                lblStatus.Text = "Smart Auto: exact trained lines are used when pose is unique; true ambiguity falls back to rotation-independent calipers.";
            else if (IsAutoCaliperMode)
                lblStatus.Text = "Auto Caliper: ignores trained line orientation and measures live major/minor dimensions. Best for highly symmetric silhouettes.";
            else if (IsCentroidMode)
                lblStatus.Text = "Centroid Axis: only this mode intentionally forces Width/Length through the live centroid.";
            else if (Convert.ToInt32(CurrentTransformMode) == 1)
                lblStatus.Text = "Free Offset: preserves the trained off-center line position and direction after robust registration.";
            else
                lblStatus.Text = "Relative Landmark: preserves the trained Width/Length at the same relative percentage position in the live shape.";
        }

        private void UpdateOptionHelpText()
        {
            int m = cmbTransformMode?.SelectedIndex ?? 4;
            string modeTip = m == 0
                ? "CENTER: trained direction is kept, but Width and Length are deliberately forced through the live centroid."
                : m == 1
                    ? "FREE OFFSET: preserves the trained off-center line position and direction. Use when exact placement matters."
                    : m == 2
                        ? "RELATIVE: keeps the line at the same percentage position. Good for pear shoulder / heart landmark widths."
                        : m == 3
                            ? "TEMPLATE: maps the exact trained line with full contour registration. Best for asymmetric custom shapes."
                            : m == 5
                                ? "CALIPER: ignores exact trained line placement and measures overall orientation-independent dimensions."
                                : "SMART AUTO: exact registered line when pose is unique; safe caliper fallback only for true ambiguity.";

            int seg = cmbSegmentationMode?.SelectedIndex ?? 0;
            string segTip = seg == 1
                ? "OTSU: use for a consistently dark stone on a clean light background; usually very stable for your chamber."
                : seg == 2
                    ? (bgFrame != null
                        ? "BACKGROUND: compares against the supplied clean background frame; excellent for a fixed camera/chamber."
                        : "BACKGROUND: requires a clean background frame. Capture/provide one before using this option.")
                    : seg == 3
                        ? "CANNY: edge-based fallback when filled masks fail; usually less stable for precision than Otsu/background."
                        : "AUTO SEGMENTATION: evaluates Background Difference (if available), Otsu and Canny and selects the strongest contour.";

            int p = cmbPoseSolver?.SelectedIndex ?? 0;
            string poseTip = p == 1
                ? "CONTOUR ONLY: best for distinctive asymmetric outlines such as pear, heart, triangle, kite and irregular shapes."
                : p == 2
                    ? "PCA ASSISTED: useful for clearly elongated shapes; avoid relying on it for near-square or circular silhouettes."
                    : p == 3
                        ? "MIN AREA RECT: useful for rectangles, emeralds and straight-sided elongated shapes."
                        : "AUTO HYBRID: recommended default. Contour registration is primary; PCA/MinAreaRect assist only when reliable.";

            if (lblModeHelp != null) lblModeHelp.Text = "MEASUREMENT TIP: " + modeTip;
            if (lblSegHelp != null) lblSegHelp.Text = "SEGMENTATION TIP: " + segTip;
            if (lblPoseHelp != null) lblPoseHelp.Text = "POSE TIP: " + poseTip;

            if (optionToolTip != null)
            {
                optionToolTip.SetToolTip(cmbTransformMode, modeTip);
                optionToolTip.SetToolTip(cmbSegmentationMode, segTip);
                optionToolTip.SetToolTip(cmbPoseSolver, poseTip);
            }
        }

        private ShapeTransformMode CurrentTransformMode
        {
            get
            {
                if (cmbTransformMode == null) return ShapeTransformMode.Centroid;
                if (cmbTransformMode.SelectedIndex == 1) return ShapeTransformMode.TaperedLongestEdge;
                if (cmbTransformMode.SelectedIndex == 2) return ShapeTransformMode.RelativeLandmark;
                if (cmbTransformMode.SelectedIndex == 3) return (ShapeTransformMode)3;
                if (cmbTransformMode.SelectedIndex == 4) return (ShapeTransformMode)4;
                if (cmbTransformMode.SelectedIndex == 5) return (ShapeTransformMode)5;
                return ShapeTransformMode.Centroid;
            }
        }

        private bool IsCentroidMode => Convert.ToInt32(CurrentTransformMode) == 0;
        private bool IsTemplateRegistrationMode => Convert.ToInt32(CurrentTransformMode) == 3;
        private bool IsSmartAutoMode => Convert.ToInt32(CurrentTransformMode) == 4;
        private bool IsAutoCaliperMode => Convert.ToInt32(CurrentTransformMode) == 5;

        private string CurrentTransformModeDisplayName
        {
            get
            {
                int mode = Convert.ToInt32(CurrentTransformMode);
                if (mode == 3) return "Template Registration";
                if (mode == 4) return "Smart Auto";
                if (mode == 5) return "Auto Caliper";
                return CurrentTransformMode.ToString();
            }
        }

        private CustomSegmentationMode SelectedSegmentationMode
        {
            get
            {
                if (cmbSegmentationMode == null) return CustomSegmentationMode.Auto;
                return (CustomSegmentationMode)Math.Max(0, cmbSegmentationMode.SelectedIndex);
            }
        }

        private CustomPoseSolver SelectedPoseSolver
        {
            get
            {
                if (cmbPoseSolver == null) return CustomPoseSolver.AutoHybrid;
                return (CustomPoseSolver)Math.Max(0, cmbPoseSolver.SelectedIndex);
            }
        }

        private CustomShapeTrainingOptions CurrentTrainingOptions => new CustomShapeTrainingOptions
        {
            SegmentationMode = SelectedSegmentationMode,
            PoseSolver = SelectedPoseSolver,
            RejectAmbiguousPose = chkRejectAmbiguous == null || chkRejectAmbiguous.Checked,
            AllowSymmetricEquivalentMeasurements = chkAllowEquivalentSymmetry != null && chkAllowEquivalentSymmetry.Checked
        };

        private void PreviewDetection()
        {
            if (!CustomShapeEngine.TryExtractStoneContour(sourceFrame, bgFrame, SelectedSegmentationMode,
                out OpenCvSharp.Point[] contour, out Mat mask))
            {
                lblStatus.Text = "Preview: object contour was not detected. Try another segmentation option or capture a clean background.";
                return;
            }

            try
            {
                displayFrame?.Dispose();
                displayFrame = sourceFrame.Clone();
                Cv2.DrawContours(displayFrame, new[] { contour }, -1, Scalar.LimeGreen, 2, LineTypes.AntiAlias);

                CustomShapeEngine.GetInvariantTransform(contour, CurrentTransformMode,
                    out Point2f c, out _, out _, out _);
                centroid = c;
                Cv2.Circle(displayFrame, (OpenCvSharp.Point)c, 5, Scalar.White, 1, LineTypes.AntiAlias);

                Bitmap oldBmp = pictureBox.Image as Bitmap;
                pictureBox.Image = BitmapConverter.ToBitmap(displayFrame);
                oldBmp?.Dispose();
                pictureBox.Invalidate();

                double area = Math.Abs(Cv2.ContourArea(contour));
                lblStatus.Text = $"Preview OK: contour points={contour.Length}, area={area:F0}px², segmentation={SelectedSegmentationMode}, pose={SelectedPoseSolver}.";
            }
            finally
            {
                mask?.Dispose();
            }
        }

        private void DetectBaseOrientation()
        {
            if (CustomShapeEngine.TryExtractStoneContour(sourceFrame, bgFrame, SelectedSegmentationMode,
                out OpenCvSharp.Point[] contour, out Mat mask))
            {
                try
                {
                    CustomShapeEngine.GetInvariantTransform(contour, CurrentTransformMode,
                        out centroid, out _, out _, out _);
                }
                finally
                {
                    mask?.Dispose();
                }
            }
            else
            {
                centroid = new Point2f(sourceFrame.Width / 2f, sourceFrame.Height / 2f);
                if (lblStatus != null)
                    lblStatus.Text = "Vision warning: stone contour not detected with the selected segmentation mode.";
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
            lblStatus.Text = widthText + "  |  " + lengthText + "  |  " + CurrentTransformModeDisplayName;
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
            if (!IsAutoCaliperMode && (!wPt1.HasValue || !wPt2.HasValue || !lPt1.HasValue || !lPt2.HasValue))
            {
                MessageBox.Show("Please define Width and Length before saving.");
                return;
            }

            string shapeName = Microsoft.VisualBasic.Interaction.InputBox(
                "Enter Custom Shape Name:", "Save Shape", "NewShape");
            if (string.IsNullOrWhiteSpace(shapeName)) return;

            if (!CustomShapeEngine.TryExtractStoneContour(sourceFrame, bgFrame, SelectedSegmentationMode,
                out OpenCvSharp.Point[] contour, out Mat trainingMask))
            {
                MessageBox.Show("Error detecting stone outline for training. Use Preview Detection and select a different segmentation mode.");
                return;
            }

            try
            {
                ShapeTransformMode mode = CurrentTransformMode;
                CustomShapeEngine.GetInvariantTransform(contour, mode,
                    out Point2f refCenter, out double refAngle, out float span1, out float span2);

                // Auto Caliper does not use trained lines, but ShapeData still expects values.
                Point2f widthA = wPt1 ?? new Point2f(refCenter.X - span1 * 0.5f, refCenter.Y);
                Point2f widthB = wPt2 ?? new Point2f(refCenter.X + span1 * 0.5f, refCenter.Y);
                Point2f lengthA = lPt1 ?? new Point2f(refCenter.X, refCenter.Y - span2 * 0.5f);
                Point2f lengthB = lPt2 ?? new Point2f(refCenter.X, refCenter.Y + span2 * 0.5f);

                Point2f normW1 = CustomShapeEngine.ProjectToLocal(widthA, refCenter, refAngle, span1, span2);
                Point2f normW2 = CustomShapeEngine.ProjectToLocal(widthB, refCenter, refAngle, span1, span2);
                Point2f normL1 = CustomShapeEngine.ProjectToLocal(lengthA, refCenter, refAngle, span1, span2);
                Point2f normL2 = CustomShapeEngine.ProjectToLocal(lengthB, refCenter, refAngle, span1, span2);

                string fingerprint = CustomShapeEngine.BuildFingerprint(
                    contour, mode, refCenter, refAngle, span1, span2, CurrentTrainingOptions,
                    normW1, normW2, normL1, normL2, chkSnapToEdge.Checked);

                string recordsFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "CustomShapes");
                Directory.CreateDirectory(recordsFolder);
                string safeName = MakeSafeFileName(shapeName);
                string stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");

                string previewPath = Path.Combine(recordsFolder,
                    $"CustomShape_{safeName}_{stamp}.png");
                string maskPath = Path.Combine(recordsFolder,
                    $"CustomShapeMask_{safeName}_{stamp}.png");

                Cv2.ImWrite(previewPath, displayFrame);
                Cv2.ImWrite(maskPath, trainingMask);

                ShapeData shape = new ShapeData
                {
                    Name = shapeName,
                    ImagePath = previewPath,
                    TemplateMaskPath = maskPath,
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
                    $"Measurement: {CurrentTransformModeDisplayName}\n" +
                    $"Segmentation: {SelectedSegmentationMode}\n" +
                    $"Pose solver: {SelectedPoseSolver}\n" +
                    $"Reject ambiguous: {CurrentTrainingOptions.RejectAmbiguousPose}\n" +
                    "CSF6 self-contained measurement + registration fingerprint saved.\n" +
                    "Training mask saved for diagnostics.\n\n" +
                    "IMPORTANT: retrain old CSF3/CSF4/CSF5 shapes with V5 so the mode and measurement lines are embedded in CSF6.",
                    "Shape Saved");

                DialogResult = DialogResult.OK;
                Close();
            }
            finally
            {
                trainingMask?.Dispose();
            }
        }

        private static string MakeSafeFileName(string name)
        {
            foreach (char c in Path.GetInvalidFileNameChars()) name = name.Replace(c, '_');
            return string.IsNullOrWhiteSpace(name) ? "Shape" : name;
        }
    }
}
