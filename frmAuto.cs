using uEye;
using System;
using System.IO;
using System.Linq;
using OpenCvSharp;
using System.Data;
using uEye.Defines;
using System.Drawing;
using System.Xml.Linq;
using System.IO.Ports;
using System.Text.Json;
using System.Threading;
using System.Diagnostics;
using System.Data.SQLite;
using System.Windows.Forms;
using System.Reflection.Emit;
using System.Drawing.Imaging;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using MvCamCtrl.NET;
using OpenCvSharp.ML;
using OpenCvSharp.Extensions;

namespace Matric_scope
{
    public partial class FrmAuto : Form
    {
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern bool ReleaseCapture();

        public const int WM_NCLBUTTONDOWN = 0xA1, HT_CAPTION = 0x2;
        private bool isInitializing = true;
        private ShapeData activeCustomShape = null;
        private CustomShapeEngine customEngine = new CustomShapeEngine();
        enum MeasurementMode { None,Round, Pear, Oval, Heart, Marquise, Poly, General, GeneralC, Custom }
        MeasurementMode currentMode = MeasurementMode.None;
        //private bool isMeasurementStopped = false;
        public static string currentfile = "Default_Profile";
        private readonly Dictionary<int, int> lightBlinkCounters = new Dictionary<int, int>();
        private readonly object counterLock = new object(), frameLock = new object();
        public MyCamera.cbOutputExdelegate ImageCallback;
        Object mBufferDriverLock = new Object();
        uint m_BuffersizeForDriver = 0;
        ModifyRegistry mr = new ModifyRegistry();
        private SerialPort arduinoPort;
        private static DataTable dtRules;
        public MyCamera device;
        private uEye.Camera Camera;
        ColorPalette cp;
        private bool isObjectPresent = false, hasMeasuredCurrentObject = false, captureFlag = false, isRenderingSnapshot = false, isSerialConnected = false;
        private bool showLiveCenterAxes = false;
        private double liveAxisPixelsPerMillimeter = 0.0;
        private OpenCvSharp.Point lastCentroid = new OpenCvSharp.Point(0, 0);
        private int stableFrameCount = 0, thresholdValue = 25;
        private const int FRAMES_TO_STABILIZE = 8; 
        private const double MOVEMENT_THRESHOLD = 9.0; 
        private Stopwatch stopWatch = System.Diagnostics.Stopwatch.StartNew();
        public static bool calibclick = false;
        private Mat liveMat = new Mat(), lastProcessedFrame = new Mat(), motionAnalysisMat = new Mat(), backgroundGray = null, stableDisplayMat = new Mat();
        private static string xmlFilePath = Path.Combine(Application.StartupPath, "DiamondRules.xml");
        private readonly string storageFilePath = Path.Combine(Application.StartupPath, "tray_counters.json");
        private readonly List<double> stableLengthSamples = new List<double>();
        private readonly List<double> stableWidthSamples = new List<double>();
        private const int REQUIRED_SMOOTHING_SAMPLES = 8; 
        private bool isBatchProcessed = false;
        public static bool autoPrint = false, autosave = false;
        string targetedDevice;
        private Camera_Setting1 camsetInstance = null;
        private FrmCalib calibforminstance = null;

        private void SwitchMeasurementMode(MeasurementMode newMode)
        {
            lock (frameLock) // Protects camera loops from modifying currentMode mid-transit
            {
                // Scenario A: Software just started up (currentMode is None)
                if (currentMode == MeasurementMode.None)
                {
                    currentMode = newMode;
                    this.BeginInvoke((MethodInvoker)delegate { UpdateMeasurementUI($"Mode: Auto {newMode} Measurement"); });
                    return;
                }

                // Scenario B: User clicked the EXACT SAME button again 
                if (currentMode == newMode)
                {
                    return;
                }

                // Scenario C: User is changing to a completely DIFFERENT shape batch mid-session
                if (currentMode != newMode)
                {
                    currentMode = newMode;

                    // Clean up old sample averages instantly so the new shape engine starts clean
                    stableLengthSamples.Clear();
                    stableWidthSamples.Clear();
                    stableFrameCount = 0;
                    isBatchProcessed = false;
                    hasMeasuredCurrentObject = false;
                    isRenderingSnapshot = false;

                    lock (counterLock)
                    {
                        lightBlinkCounters.Clear(); // Wipe the RAM memory map
                        if (File.Exists(storageFilePath))
                        {
                            File.WriteAllText(storageFilePath, "{}"); // Empty the JSON save file on disk
                        }
                    }
                    this .BeginInvoke((MethodInvoker)delegate {lblCurrentMode.Text = "Mode : "+currentMode.ToString(); });
                    this.BeginInvoke((MethodInvoker)delegate { UpdateMeasurementUI($"Mode: Auto {newMode} Measurement. Counters Reset."); });
                }
            }
        }

        private void LoadCountersFromFile()
        {
            try
            {
                if (File.Exists(storageFilePath))
                {
                    string jsonString = File.ReadAllText(storageFilePath);

                    lock (counterLock)
                    {
                        var loadedCounters = JsonSerializer.Deserialize<Dictionary<int, int>>(jsonString);

                        if (loadedCounters != null)
                        {
                            lightBlinkCounters.Clear();
                            foreach (var kvp in loadedCounters)
                            {
                                lightBlinkCounters[kvp.Key] = kvp.Value;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading historical sorting data: {ex.Message}", "Storage Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void SaveCountersToFile()
        {
            try
            {
                lock (counterLock)
                {
                    // Convert the dictionary into a clean text string layout
                    string jsonString = JsonSerializer.Serialize(lightBlinkCounters);
                    File.WriteAllText(storageFilePath, jsonString);
                }
            }
            catch (Exception ex)
            {
                // Fail-safe logging so an I/O lag doesn't freeze your measurement thread
                System.Diagnostics.Debug.WriteLine($"Failed to save counters: {ex.Message}");
            }
        }

        private void UpdatePictureBoxAspectRatio()
        {
            // Define your panel widths and top bar heights
            int leftPanelWidth = 220;  // Matches the new Left Panel we will create
            int rightPanelWidth = 160; // Matches your panelRightSide width
            int topBarsHeight = 32 + 80; // Title bar (32) + FlowLayoutButtons (120)

            // 1. Calculate available space between the two sidebars
            int availableWidth = this.ClientSize.Width - leftPanelWidth - rightPanelWidth;
            int availableHeight = this.ClientSize.Height - topBarsHeight;

            if (availableWidth <= 0 || availableHeight <= 0) return;

            // 2. Camera target aspect ratio (1280 / 1024 = 1.25)
            double targetAspectRatio = 1280.0 / 1024.0;
            int newWidth, newHeight;

            // 3. Mathematical check
            if ((double)availableWidth / availableHeight > targetAspectRatio)
            {
                // Screen is too wide: constrain by available height
                newHeight = availableHeight;
                newWidth = (int)(newHeight * targetAspectRatio);
            }
            else
            {
                // Screen is too tall: constrain by available width
                newWidth = availableWidth;
                newHeight = (int)(newWidth / targetAspectRatio);
            }

            // 4. Center the picture box in the remaining empty space
            // Offset X starts AFTER the left panel
            int offsetX = leftPanelWidth + ((availableWidth - newWidth) / 2);
            // Offset Y starts AFTER the top buttons
            int offsetY = topBarsHeight + ((availableHeight - newHeight) / 2);

            // 5. Apply the calculated coordinates
            this.pictureBox1.Size = new System.Drawing.Size(newWidth, newHeight);
            this.pictureBox1.Location = new System.Drawing.Point(offsetX, offsetY);
        }

        private void PopulateCustomShapesMenu()
        {
            if (customShapesMenuItem == null) return;

            customShapesMenuItem.DropDownItems.Clear();
            List<ShapeData> shapes = DatabaseHelper.GetAllShapes();

            if (shapes.Count == 0)
            {
                customShapesMenuItem.DropDownItems.Add(new ToolStripMenuItem("No Custom Shapes Saved") { Enabled = false });
                return;
            }

            foreach (var shape in shapes)
            {
                var shapeItem = new ToolStripMenuItem($"{shape.Name}");

                // Support both Right-Click and Normal Click
                shapeItem.MouseDown += (s, e) =>
                {
                    // If user RIGHT-CLICKS a shape in the menu:
                    if (e.Button == MouseButtons.Right)
                    {
                        var confirm = MessageBox.Show($"Are you sure you want to delete '{shape.Name}'?",
                                                      "Delete Custom Shape",
                                                      MessageBoxButtons.YesNo,
                                                      MessageBoxIcon.Warning);

                        if (confirm == DialogResult.Yes)
                        {
                            DatabaseHelper.DeleteShape(shape.Id);

                            // Clear active shape if we deleted the current active one
                            if (activeCustomShape != null && activeCustomShape.Id == shape.Id)
                            {
                                activeCustomShape = null;
                                btnGeneralC_Click(null, null);
                            }
                            customShapesMenuItem.DropDown.Close();
                            PopulateCustomShapesMenu(); // Refresh menu
                        }
                    }
                    // If user LEFT-CLICKS a shape:
                    else if (e.Button == MouseButtons.Left)
                    {
                        activeCustomShape = shape;
                        currentMode = MeasurementMode.Custom;
                        UpdateMeasurementUI($"Active Custom Shape: {shape.Name}");
                        picPreview.Image = CreateNonIndexedImage(new Bitmap(activeCustomShape.ImagePath));
                        //customEngine.MeasureCustomShape(BitmapConverter.ToMat(CreateNonIndexedImage(new Bitmap(@"C:\Users\Administrator\Desktop\Marquise_Result - Copy.png"))), activeCustomShape, backgroundGray);
                    }
                };

                customShapesMenuItem.DropDownItems.Add(shapeItem);
            }
        }

        public FrmAuto()
        {
            InitializeComponent();
            DatabaseHelper.InitializeDatabase();
            PopulateCustomShapesMenu();
        }

        private Bitmap GetLiveFrameCopy()
        {
            lock (frameLock)
            {
                if (liveMat == null || liveMat.IsDisposed || liveMat.Empty()) return null;
                return BitmapConverter.ToBitmap(liveMat);
            }
        }

        private void btnManualMeasure_Click(object sender, EventArgs e)
        {
            using (var manualForm = new ManualMeasurementForm(GetLiveFrameCopy))
            {
                manualForm.ShowDialog(this);
            }
        }

        private void btnLiveCenterAxes_Click(object sender, EventArgs e)
        {
            showLiveCenterAxes = !showLiveCenterAxes;
            liveAxisPixelsPerMillimeter = 0.0;

            if (showLiveCenterAxes)
            {
                string ppmValue = mr.Read("ppm");
                double.TryParse(ppmValue, out liveAxisPixelsPerMillimeter);
                if (liveAxisPixelsPerMillimeter < 0.0) liveAxisPixelsPerMillimeter = 0.0;
            }

            btnLiveCenterAxes.Text = showLiveCenterAxes
                ? "HIDE CENTER\r\nSCALE"
                : "SHOW CENTER\r\nSCALE";
            btnLiveCenterAxes.BackColor = showLiveCenterAxes
                ? Color.LightGreen
                : Color.FromArgb(250, 182, 105);

            // Repaint immediately as well as on subsequent camera frames. This
            // also removes the overlay immediately on the second button click.
            RefreshCameraDisplay();
        }

        private void RefreshCameraDisplay()
        {
            lock (frameLock)
            {
                Mat frame = isRenderingSnapshot ? lastProcessedFrame : liveMat;
                if (frame == null || frame.IsDisposed || frame.Empty()) return;

                Bitmap oldBitmap = pictureBox1.Image as Bitmap;
                pictureBox1.Image = CreateCameraDisplayBitmap(frame);
                oldBitmap?.Dispose();
            }
        }

        private Bitmap CreateCameraDisplayBitmap(Mat frame)
        {
            Bitmap bitmap = BitmapConverter.ToBitmap(frame);
            if (!showLiveCenterAxes) return bitmap;

            int centerX = bitmap.Width / 2;
            int centerY = bitmap.Height / 2;

            using (Graphics graphics = Graphics.FromImage(bitmap))
            using (var horizontalPen = new Pen(Color.Lime, 2f))
            using (var verticalPen = new Pen(Color.DeepSkyBlue, 2f))
            {
                graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                // Full-frame ruler axes. These are a visual scale only; no object
                // is detected and no automatic object measurement is performed.
                graphics.DrawLine(horizontalPen, 0, centerY, centerX - 6, centerY);
                graphics.DrawLine(horizontalPen, centerX + 6, centerY, bitmap.Width - 1, centerY);
                graphics.DrawLine(verticalPen, centerX, 0, centerX, centerY - 6);
                graphics.DrawLine(verticalPen, centerX, centerY + 6, centerX, bitmap.Height - 1);
                DrawCenteredRulerScale(graphics, bitmap.Size, centerX, centerY);
                graphics.DrawEllipse(Pens.White, centerX - 5, centerY - 5, 10, 10);
            }

            return bitmap;
        }

        private void DrawCenteredRulerScale(Graphics graphics, System.Drawing.Size frameSize, int centerX, int centerY)
        {
            bool calibrated = liveAxisPixelsPerMillimeter > 0.0;
            double minorStepPixels = calibrated ? liveAxisPixelsPerMillimeter / 10.0 : 10.0;
            if (minorStepPixels < 2.0) minorStepPixels = 2.0;

            using (var horizontalTickPen = new Pen(Color.Lime, 1f))
            using (var verticalTickPen = new Pen(Color.DeepSkyBlue, 1f))
            using (var font = new Font("Microsoft Sans Serif", 8f, FontStyle.Bold))
            using (var horizontalBrush = new SolidBrush(Color.Lime))
            using (var verticalBrush = new SolidBrush(Color.DeepSkyBlue))
            {
                int maxTicks = (int)(Math.Max(frameSize.Width, frameSize.Height) / minorStepPixels) + 1;
                for (int tick = -maxTicks; tick <= maxTicks; tick++)
                {
                    if (tick == 0) continue;
                    bool major = Math.Abs(tick) % 10 == 0;
                    bool medium = !major && Math.Abs(tick) % 5 == 0;
                    int tickLength = major ? 18 : (medium ? 12 : 7);

                    int x = (int)Math.Round(centerX + tick * minorStepPixels);
                    if (x >= 0 && x < frameSize.Width)
                    {
                        graphics.DrawLine(horizontalTickPen, x, centerY - tickLength, x, centerY + tickLength);
                        if (major)
                        {
                            string value = calibrated ? (tick / 10.0).ToString("0") : (tick * 10).ToString();
                            graphics.DrawString(value, font, horizontalBrush, x + 2, centerY + tickLength + 2);
                        }
                    }

                    int y = (int)Math.Round(centerY + tick * minorStepPixels);
                    if (y >= 0 && y < frameSize.Height)
                    {
                        graphics.DrawLine(verticalTickPen, centerX - tickLength, y, centerX + tickLength, y);
                        if (major)
                        {
                            string value = calibrated ? (-tick / 10.0).ToString("0") : (-tick * 10).ToString();
                            graphics.DrawString(value, font, verticalBrush, centerX + tickLength + 2, y + 2);
                        }
                    }
                }

                string units = calibrated ? "CENTER SCALE (mm)" : "CENTER SCALE (px)";
                graphics.DrawString(units, font, Brushes.Yellow, centerX + 14, centerY - 30);
            }
        }

        private void InitializeArduinoConnection()
        {
            try
            {
                arduinoPort = new SerialPort();
                arduinoPort.PortName = "COM3"; // CHANGE THIS to your exact Arduino COM Port from Device Manager
                arduinoPort.BaudRate = 9600;
                arduinoPort.Open();
                isSerialConnected = true;
            }
            catch (Exception ex)
            {
                isSerialConnected = false;
                MessageBox.Show($"Could not connect to hardware: {ex.Message}", "Hardware Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        public static void RefreshActiveRules(string updatedFileProfile)
        {
            currentfile = updatedFileProfile;

            // Find the open instance of the form to update its instance controls safely
            FrmAuto activeForm = Application.OpenForms.OfType<FrmAuto>().FirstOrDefault();

            if (activeForm != null)
            {
                try
                {
                    activeForm.lblFile.Text = "File: " + currentfile;
                }
                catch
                {
                    activeForm.lblFile.Text = "File: None";
                }

                // Call the instance method to reload the database
                FrmAuto.LoadRulesDatabase();
            }
            else
            {
                MessageBox.Show("Rules File not selected or form is not open.");
            }
        }

        public static void LoadRulesDatabase()
        {
            // 1. Create a temporary master table to hold everything pulled out of the XML file
            DataTable dtMaster = new DataTable("Rule");
            dtMaster.Columns.Add("FileName", typeof(string));
            dtMaster.Columns.Add("Number", typeof(int));
            dtMaster.Columns.Add("FromLength", typeof(double));
            dtMaster.Columns.Add("ToLength", typeof(double));
            dtMaster.Columns.Add("FromWidth", typeof(double));
            dtMaster.Columns.Add("ToWidth", typeof(double));

            if (File.Exists(xmlFilePath))
            {
                try
                {
                    dtMaster.ReadXml(xmlFilePath);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Detection Form failed to load database layout: {ex.Message}", "Data Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }

            dtRules = new DataTable("Rule");
            dtRules.Columns.Add("FileName", typeof(string));
            dtRules.Columns.Add("Number", typeof(int));
            dtRules.Columns.Add("FromLength", typeof(double));
            dtRules.Columns.Add("ToLength", typeof(double));
            dtRules.Columns.Add("FromWidth", typeof(double));
            dtRules.Columns.Add("ToWidth", typeof(double));

            dtRules.PrimaryKey = new DataColumn[] { dtRules.Columns["FileName"], dtRules.Columns["Number"] };

            if (string.IsNullOrWhiteSpace(currentfile))
            {
                currentfile = "Default_Profile";
            }

            var filteredRows = dtMaster.AsEnumerable().Where(row => row.Field<string>("FileName") == currentfile);

            foreach (DataRow row in filteredRows)
            {
                dtRules.ImportRow(row);
            }

            for (int i = 1; i <= 20; i++)
            {
                object[] key = new object[] { currentfile, i };
                if (dtRules.Rows.Find(key) == null)
                {
                    DataRow blankRow = dtRules.NewRow();
                    blankRow["FileName"] = currentfile;
                    blankRow["Number"] = i;
                    blankRow["FromLength"] = 0.0;
                    blankRow["ToLength"] = 0.0;
                    blankRow["FromWidth"] = 0.0;
                    blankRow["ToWidth"] = 0.0;
                    dtRules.Rows.Add(blankRow);
                }
            }
        }
        
        private void Form1_Load(object sender, EventArgs e)
        {
            isInitializing = true;

            /*using (CustomShapeForm form = new CustomShapeForm(BitmapConverter.ToMat(CreateNonIndexedImage(new Bitmap(@"C:\Users\Administrator\Desktop\Marquise_Result.png"))),BitmapConverter.ToMat(new Bitmap("Blank_Bg.png"))))
            {
                if (form.ShowDialog() == DialogResult.OK)
                {
                    PopulateCustomShapesMenu(); // Refresh menu with newly added shape
                }
            }*/

            //new MeasurePearShape().MeasureShapeWithVertexAxis(BitmapConverter.ToMat(CreateNonIndexedImage(new Bitmap(@"Custom 6.74x3.75.png"))));
            UpdatePictureBoxAspectRatio();
            InitializeArduinoConnection();
            
            if (mr.Read("chkPrint") != null && mr.Read("chkPrint") != "" && mr.Read("chkPrint") == "true")
            {
                autoPrint = true;
            }
            else
            {
                autoPrint = false;
            }


            if (mr.Read("chkSave") != null && mr.Read("chkSave") != "" && mr.Read("chkSave") == "true")
            {
                autosave = true;
            }
            else
            {
                autosave = false;
            }


            /*try
            {
                currentfile = new ModifyRegistry().Read("cmbFile").ToString();
                lblFile.Text ="File:" +currentfile;
            }
            catch
            {
                MessageBox.Show("Rules File not selected please select rules file");
                lblFile.Text = "File:None";
            }*/

            try
            {
                targetedDevice = new ModifyRegistry().Read("cmbPrinters");
            }
            catch
            {
                targetedDevice = "TSC M23";
            }

            PopulateRulesComboBox();

            try
            {
                currentfile = new ModifyRegistry().Read("cmbFile").ToString();

                if (cmbRulesFile.Items.Contains(currentfile))
                {
                    cmbRulesFile.SelectedItem = currentfile;
                }
            }
            catch
            {
                currentfile = cmbRulesFile.SelectedItem?.ToString();
            }

            LoadRulesDatabase();
            LoadCountersFromFile();
            isInitializing = false;

            try
            {
                /*trackBar1.Minimum = 0;
                trackBar1.Maximum = 99000;
                trackBar1.Value = 1000;*/

                //trackBar1.Minimum = 0;
                //trackBar1.Maximum = 1320;

                using (Mat bgSrc = Cv2.ImRead(@"Blank_Bg.png"))
                {
                    if (!bgSrc.Empty())
                    {
                        backgroundGray = new Mat();
                        Cv2.CvtColor(bgSrc, backgroundGray, ColorConversionCodes.BGR2GRAY);
                        // Optional smoothing matching your processing pipeline
                        Cv2.MedianBlur(backgroundGray, backgroundGray, 7);
                    }
                    else
                    {
                        MessageBox.Show("Warning: Reference background image could not be loaded. Auto-detection disabled.");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading background template: " + ex.Message);
            }
            //InitCameraMV();
            InitCameraUeye();
            btnGeneralC_Click(null, null);
        }

        private void PopulateRulesComboBox()
        {
            string currentSelection = cmbRulesFile.SelectedItem?.ToString();
            cmbRulesFile.Items.Clear();

            string xmlFilePath = Path.Combine(Application.StartupPath, "DiamondRules.xml");
            List<string> uniqueFiles = new List<string>();

            if (File.Exists(xmlFilePath))
            {
                try
                {
                    // Load the XML file directly
                    XDocument doc = XDocument.Load(xmlFilePath);

                    // Extract all unique names from the <FileName> nodes
                    uniqueFiles = doc.Descendants("FileName")
                                     .Select(node => node.Value)
                                     .Where(name => !string.IsNullOrWhiteSpace(name))
                                     .Distinct()
                                     .ToList();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error reading DiamondRules.xml: " + ex.Message);
                }
            }

            // Force "Default_Profile" to always exist and be exactly at the top (Index 0)
            if (uniqueFiles.Contains("Default_Profile"))
            {
                uniqueFiles.Remove("Default_Profile");
            }
            uniqueFiles.Insert(0, "Default_Profile");

            // Add all extracted names to the ComboBox
            foreach (string file in uniqueFiles)
            {
                cmbRulesFile.Items.Add(file);
            }

            // Restore the user's selection if they are just reloading, otherwise pick Default
            if (!string.IsNullOrEmpty(currentSelection) && cmbRulesFile.Items.Contains(currentSelection))
            {
                cmbRulesFile.SelectedItem = currentSelection;
            }
            else
            {
                cmbRulesFile.SelectedIndex = 0;
            }
        }

        /*public void InitCameraMV()
        {
            var b = new Bitmap(1, 1, System.Drawing.Imaging.PixelFormat.Format8bppIndexed);
            cp = b.Palette;
            for (int c = 0; c < 256; c++)
            {
                cp.Entries[c] = System.Drawing.Color.FromArgb(c, c, c);
            }
            int nRet = MyCamera.MV_OK;
            try
            {
                device = new MyCamera();
                MyCamera.MV_CC_DEVICE_INFO_LIST stDevList = new MyCamera.MV_CC_DEVICE_INFO_LIST();
                nRet = MyCamera.MV_CC_EnumDevices_NET(MyCamera.MV_GIGE_DEVICE | MyCamera.MV_USB_DEVICE, ref stDevList);
                if (MyCamera.MV_OK != nRet)
                {
                    MessageBox.Show("Enum Device Failed");
                    return;
                }
                if (0 == stDevList.nDeviceNum)
                {
                    MessageBox.Show("No Camera Found");
                    return;
                }
                MyCamera.MV_CC_DEVICE_INFO stDevInfo = (MyCamera.MV_CC_DEVICE_INFO)Marshal.PtrToStructure(stDevList.pDeviceInfo[0], typeof(MyCamera.MV_CC_DEVICE_INFO));
                nRet = device.MV_CC_CreateDevice_NET(ref stDevInfo);
                if (MyCamera.MV_OK != nRet)
                {
                    MessageBox.Show("Can Not Create Camera");
                    return;
                }
                nRet = device.MV_CC_OpenDevice_NET();
                if (MyCamera.MV_OK != nRet)
                {
                    MessageBox.Show("Can Not Open Camera");
                    return;
                }
                try
                {
                    nRet = device.MV_CC_FeatureLoad_NET("cam.pfs");
                }
                catch (Exception ee)
                {
                    MessageBox.Show("Parameters not Loaded");
                }
                if (stDevInfo.nTLayerType == MyCamera.MV_GIGE_DEVICE)
                {
                    int nPacketSize = device.MV_CC_GetOptimalPacketSize_NET();
                    if (nPacketSize > 0)
                    {
                        nRet = device.MV_CC_SetIntValue_NET("GevSCPSPacketSize", (uint)nPacketSize);
                    }
                }
                nRet = device.MV_CC_SetEnumValue_NET("TriggerMode", 0);
                if (MyCamera.MV_OK != nRet)
                {
                    MessageBox.Show("Can Not Set Trigger");
                    return;
                }
            }
            catch (Exception exceptii)
            {
                MessageBox.Show("1 "+exceptii.ToString());
            }
            try
            {
                ImageCallback = new MyCamera.cbOutputExdelegate(ImageCallBackFunc);
                nRet = device.MV_CC_RegisterImageCallBackEx_NET(ImageCallback, IntPtr.Zero);
                device.MV_CC_StartGrabbing_NET();
                MyCamera.MVCC_INTVALUE stParam = new MyCamera.MVCC_INTVALUE();
                nRet = device.MV_CC_GetIntValue_NET("PayloadSize", ref stParam);
                if (MyCamera.MV_OK != nRet)
                {
                    return;
                }
                //device.MV_CC_SetEnumValue_NET("ExposureAuto", 0);
                //device.MV_CC_SetFloatValue_NET("ExposureTime",40000);
                //device.MV_CC_SetEnumValue_NET("GainAuto", 0);
                //device.MV_CC_SetFloatValue_NET("Gain", float.Parse(mr.Read("trackBarGainMaster1")));

                if (new ModifyRegistry().Read("trackBarExposure1") != null && new ModifyRegistry().Read("trackBarExposure1") != "")
                {
                    trackBar1.Value = Convert.ToInt32(new ModifyRegistry().Read("trackBarExposure1"));
                }

            }
            catch (Exception exceptt)
            {
                MessageBox.Show("2 " + exceptt.ToString());
            }
        }
        */

        private void onFrameEvent(object sender, EventArgs e)
        {
            // Throttle frame captures to every 50ms (~20 FPS)
            if (stopWatch.IsRunning && stopWatch.ElapsedMilliseconds < 50)
            {
                return;
            }
            stopWatch.Restart();

            uEye.Camera camera = sender as uEye.Camera;
            if (camera == null) return;

            Int32 s32MemID;
            Bitmap bmp = null;
            camera.Memory.GetActive(out s32MemID);
            camera.Memory.CopyToBitmap(s32MemID, out bmp);

            if (bmp == null) return;

            try
            {
                Bitmap bcpy = CreateNonIndexedImage(bmp);

                using (Mat rawCamFrame = BitmapConverter.ToMat(bcpy))
                {
                    lock (frameLock)
                    {
                        if (liveMat == null || liveMat.IsDisposed) liveMat = new Mat();
                        rawCamFrame.CopyTo(liveMat);
                    }
                }

                bmp.Dispose();
                bcpy.Dispose();

                // Handle Calibration Snapshots
                if (calibclick)
                {
                    lock (frameLock)
                    {
                        using (Bitmap calibBmp = BitmapConverter.ToBitmap(liveMat)) { docalib(calibBmp); }
                    }
                    calibclick = false;
                }

                // Handle Background Captures
                if (captureFlag)
                {
                    lock (frameLock)
                    {
                        using (Bitmap calibBmp = BitmapConverter.ToBitmap(liveMat)) { calibBmp.Save("Blank_Bg.png"); }
                    }
                    captureFlag = false;
                }

                // ==========================================================
                // HIGH-SPEED ALGORITHM PROCESSING ENGINE (Conditional)
                // ==========================================================
                if (backgroundGray != null && currentMode != MeasurementMode.None)
                {
                    Mat processingCopy = new Mat();
                    lock (frameLock)
                    {
                        if (liveMat != null && !liveMat.Empty())
                        {
                            liveMat.CopyTo(processingCopy);
                        }
                    }

                    if (!processingCopy.Empty())
                    {
                        System.Threading.Tasks.Task.Run(() =>
                        {
                            try
                            {
                                using (processingCopy)
                                using (Mat liveGray = new Mat())
                                {
                                    Cv2.CvtColor(processingCopy, liveGray, ColorConversionCodes.BGR2GRAY);
                                    Cv2.MedianBlur(liveGray, liveGray, 7);

                                    using (Mat diff = new Mat())
                                    using (Mat thresh = new Mat())
                                    {
                                        Cv2.Absdiff(backgroundGray, liveGray, diff);
                                        Cv2.Threshold(diff, thresh, thresholdValue, 255, ThresholdTypes.Binary);

                                        int changedPixels = Cv2.CountNonZero(thresh);

                                        if (changedPixels > 1250)
                                        {
                                            OpenCvSharp.Point[][] contours;
                                            HierarchyIndex[] hierarchy;
                                            Cv2.FindContours(thresh, out contours, out hierarchy, RetrievalModes.External, ContourApproximationModes.ApproxSimple);

                                            bool validDiamondDetected = false;
                                            OpenCvSharp.Point currentCentroid = new OpenCvSharp.Point(0, 0);

                                            if (contours.Length > 0)
                                            {
                                                var largestContour = contours.OrderByDescending(c => Cv2.ContourArea(c)).First();
                                                double contourArea = Cv2.ContourArea(largestContour);
                                                Rect boundingBox = Cv2.BoundingRect(largestContour);
                                                double aspect = (double)boundingBox.Width / boundingBox.Height;

                                                if (aspect > 0.22 && aspect < 4.5 && contourArea > 800)
                                                {
                                                    validDiamondDetected = true;
                                                    Moments mu = Cv2.Moments(largestContour, binaryImage: true);
                                                    if (mu.M00 > 0)
                                                    {
                                                        currentCentroid = new OpenCvSharp.Point((int)(mu.M10 / mu.M00), (int)(mu.M01 / mu.M00));
                                                    }
                                                }
                                            }

                                            if (validDiamondDetected)
                                            {
                                                isObjectPresent = true;
                                                double distanceMoved = Math.Sqrt(Math.Pow(currentCentroid.X - lastCentroid.X, 2) + Math.Pow(currentCentroid.Y - lastCentroid.Y, 2));

                                                if (distanceMoved > MOVEMENT_THRESHOLD)
                                                {
                                                    ResetSnapshotState();
                                                    this.BeginInvoke((MethodInvoker)delegate { UpdateMeasurementUI("Object moving..."); });
                                                }
                                                else
                                                {
                                                    if (!hasMeasuredCurrentObject)
                                                    {
                                                        stableFrameCount++;
                                                        if (stableFrameCount >= FRAMES_TO_STABILIZE)
                                                        {
                                                            hasMeasuredCurrentObject = true;

                                                            TriggerAutoMeasurement(processingCopy);

                                                            lock (frameLock)
                                                            {
                                                                if (lastProcessedFrame == null || lastProcessedFrame.IsDisposed)
                                                                    lastProcessedFrame = new Mat();
                                                                processingCopy.CopyTo(lastProcessedFrame);
                                                            }
                                                            isRenderingSnapshot = true;
                                                        }
                                                    }
                                                }
                                                lastCentroid = currentCentroid;
                                            }
                                            else
                                            {
                                                ResetSnapshotState();
                                            }
                                        }
                                        else
                                        {
                                            ResetSnapshotState();
                                        }
                                    }
                                }
                            }
                            catch (Exception) { }
                        });
                    }
                }
                else
                {
                    // If mode is None or stopped, ensure snapshot rendering mode drops back to live feed
                    isRenderingSnapshot = false;
                }

                // ==========================================================
                // UNIFIED DISPLAY RENDERING PIPELINE (Runs Always!)
                // ==========================================================
                this.BeginInvoke((MethodInvoker)delegate
                {
                    try
                    {
                        lock (frameLock)
                        {
                            // 1. Is an auto-measurement static snapshot requested?
                            if (isRenderingSnapshot && lastProcessedFrame != null && !lastProcessedFrame.IsDisposed && !lastProcessedFrame.Empty())
                            {
                                Bitmap oldBmp = pictureBox1.Image as Bitmap;
                                pictureBox1.Image = CreateCameraDisplayBitmap(lastProcessedFrame);
                                oldBmp?.Dispose();
                            }
                            // 2. Otherwise, stream the zero-lag live feed unmodified
                            else if (liveMat != null && !liveMat.IsDisposed && !liveMat.Empty())
                            {
                                Bitmap oldBmp = pictureBox1.Image as Bitmap;
                                pictureBox1.Image = CreateCameraDisplayBitmap(liveMat);
                                oldBmp?.Dispose();
                            }
                        }
                    }
                    catch (Exception) { }
                });
            }
            catch (Exception) { }
        }

        private void ResetSnapshotState()
        {
            stableFrameCount = 0;
            isObjectPresent = false;
            hasMeasuredCurrentObject = false;
            isRenderingSnapshot = false; // Instantly switches UI back to the live camera feed
            lastCentroid = new OpenCvSharp.Point(0, 0);

            this.BeginInvoke((MethodInvoker)delegate { UpdateMeasurementUI("Waiting for object..."); });
        }

        private void ResetTrackingState()
        {
            stableFrameCount = 0;
            isObjectPresent = false;
            hasMeasuredCurrentObject = false;
            lastCentroid = new OpenCvSharp.Point(0, 0);

            lock (frameLock)
            {
                if (lastProcessedFrame != null && !lastProcessedFrame.IsDisposed && !lastProcessedFrame.Empty())
                {
                    lastProcessedFrame.SetTo(new Scalar(0)); // Reset frame buffer to pure black securely
                }
            }
            this.BeginInvoke((MethodInvoker)delegate { UpdateMeasurementUI("Waiting for object..."); });
        }

        /*void ImageCallBackFunc(IntPtr pData, ref MyCamera.MV_FRAME_OUT_INFO_EX pFrameInfo, IntPtr pUser)
        {
            if (!stopWatch.IsRunning || stopWatch.ElapsedMilliseconds > 50)
            {
                lock (mBufferDriverLock)
                {
                    if (pFrameInfo.nLostPacket == 0)
                    {
                        if (pData == IntPtr.Zero || pFrameInfo.nFrameLen < m_BuffersizeForDriver) return;

                        Bitmap bmp = new Bitmap(pFrameInfo.nWidth, pFrameInfo.nHeight, pFrameInfo.nWidth * 1, System.Drawing.Imaging.PixelFormat.Format8bppIndexed, pData);
                        bmp.Palette = cp;

                        try
                        {
                            if (calibclick)
                            {
                                Bitmap bc = CreateNonIndexedImage(bmp);
                                docalib(bc);
                                calibclick = false;
                            }

                            Bitmap bcpy = CreateNonIndexedImage(bmp);
                            Mat liveMat = BitmapConverter.ToMat(bcpy);

                            // ---- AUTO DETECTION & MOTION LOGIC ----
                            if (backgroundGray != null && currentMode != MeasurementMode.None)
                            {
                                using (Mat liveGray = new Mat())
                                {
                                    Cv2.CvtColor(liveMat, liveGray, ColorConversionCodes.BGR2GRAY);
                                    Cv2.MedianBlur(liveGray, liveGray, 7);

                                    using (Mat diff = new Mat())
                                    using (Mat thresh = new Mat())
                                    {
                                        Cv2.Absdiff(backgroundGray, liveGray, diff);
                                        Cv2.Threshold(diff, thresh, thresholdValue, 255, ThresholdTypes.Binary);

                                        int changedPixels = Cv2.CountNonZero(thresh);

                                        // Scenario A: An object is detected on the stage
                                        if (changedPixels > 5000)
                                        {
                                            isObjectPresent = true;

                                            // Calculate the center of mass (Centroid) of the object using Image Moments
                                            Moments mu = Cv2.Moments(thresh, binaryImage: true);
                                            if (mu.M00 > 0)
                                            {
                                                int cX = (int)(mu.M10 / mu.M00);
                                                int cY = (int)(mu.M01 / mu.M00);
                                                OpenCvSharp.Point currentCentroid = new OpenCvSharp.Point(cX, cY);

                                                // Calculate how far the object moved since the last frame
                                                double distanceMoved = Math.Sqrt(Math.Pow(currentCentroid.X - lastCentroid.X, 2) +
                                                                                 Math.Pow(currentCentroid.Y - lastCentroid.Y, 2));

                                                if (distanceMoved > MOVEMENT_THRESHOLD)
                                                {
                                                    // The object is moving! Reset the tracking flags to prepare for a new measurement
                                                    hasMeasuredCurrentObject = false;
                                                    stableFrameCount = 0;

                                                    this.Invoke((MethodInvoker)delegate
                                                    {
                                                        label1.Text = "Object moving...";
                                                    });
                                                }
                                                else
                                                {
                                                    // The object is resting in place
                                                    if (!hasMeasuredCurrentObject)
                                                    {
                                                        stableFrameCount++;

                                                        // Wait until the object remains perfectly still for enough continuous frames
                                                        if (stableFrameCount >= FRAMES_TO_STABILIZE)
                                                        {
                                                            hasMeasuredCurrentObject = true; // Lock it
                                                            // Trigger measurement in a safe background thread
                                                            System.Threading.Tasks.Task.Run(() => { TriggerAutoMeasurement(liveMat); });
                                                        }
                                                    }
                                                }

                                                // Save the current position for comparison in the next frame
                                                lastCentroid = currentCentroid;
                                            }
                                        }
                                        // Scenario B: Stage completely cleared
                                        else
                                        {
                                            stableFrameCount = 0;
                                            isObjectPresent = false;
                                            hasMeasuredCurrentObject = false;
                                            lastCentroid = new OpenCvSharp.Point(0, 0);

                                            this.Invoke((MethodInvoker)delegate
                                            {
                                                label1.Text = "Waiting for object...";
                                            });
                                        }
                                    }
                                }
                            }
                            this.Invoke((MethodInvoker)delegate
                            {
                                pictureBox1.Image = bmp;
                            });
                        }
                        catch (Exception excc)
                        {
                            System.Diagnostics.Debug.WriteLine(excc.Message);
                        }
                    }
                }
                stopWatch.Restart();
            }
        }*/
        
        private void InitCameraUeye()
        {
            Camera = new uEye.Camera();
            uEye.Defines.Status statusRet = 0;
            
            statusRet = Camera.Init();
            if (statusRet != uEye.Defines.Status.SUCCESS)
            {
                MessageBox.Show("Camera initializing failed");
            }
            // Allocate Memory
            Int32 s32MemID;
            statusRet = Camera.Memory.Allocate(out s32MemID, true);
            if (statusRet != uEye.Defines.Status.SUCCESS)
            {
                MessageBox.Show("Allocate Memory failed");
            }
            // Start Live Video
            statusRet = Camera.Acquisition.Capture();
            Camera.Parameter.Load("cam.ini");
            //Int32 s32Value = Convert.ToInt32(mr.Read("trackBarGainMaster1"));
            //Camera.Gain.Hardware.Scaled.SetMaster(s32Value);
            uEye.Types.Range<Double> range;
            statusRet = Camera.Timing.Exposure.GetRange(out range);
            Double dValue = range.Minimum + Convert.ToInt32(mr.Read("trackBarExposure1")) * range.Increment;
            statusRet = Camera.Timing.Exposure.Set(dValue);
            if (statusRet != uEye.Defines.Status.SUCCESS)
            {
                MessageBox.Show("Start Live Video failed");
            }
            Camera.EventFrame += onFrameEvent;
            Camera.Gain.Hardware.Scaled.SetMaster(Convert.ToInt32(mr.Read("trackBarGainMaster1")));
            statusRet = Camera.Timing.Exposure.GetRange(out range);
            Double dValue1 = range.Minimum + Convert.ToInt32(mr.Read("trackBarExposure1")) * range.Increment;
            statusRet = Camera.Timing.Exposure.Set(dValue1);
            if (mr.Read("trackBarGamma") == null || mr.Read("trackBarGamma") == "" || mr.Read("trackBarGamma") == "0")
            {
                Camera.Gamma.Software.Set(100);
            }
            else
            {
                Camera.Gamma.Software.Set(Convert.ToInt32(mr.Read("trackBarGamma")));
            }
        }

        public Bitmap CreateNonIndexedImage(Bitmap src)
        {
            Bitmap newBmp = new Bitmap(src.Width, src.Height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            using (Graphics gfx = Graphics.FromImage(newBmp))
            {
                gfx.DrawImage(src, 0, 0);
            }
            return newBmp;
        }

        private void TriggerAutoMeasurement(Mat frame)
        {
            try
            {
                string cvDisplayString = "";

                // ==========================================
                // 1. RUN DETECTION ON BACKGROUND THREAD
                // ==========================================
                switch (currentMode)
                {
                    case MeasurementMode.Round:
                        //this.Invoke((MethodInvoker)delegate { doMesure(frame); });
                        cvDisplayString = doMesure(frame);
                        break;
                    case MeasurementMode.Pear:
                        cvDisplayString = new MeasurePearShape().MeasurePearWithOpenCvSharp(frame);
                        break;
                    case MeasurementMode.Oval:
                        cvDisplayString = new MeasurePearShape().MeasureOvelWithOpenCvSharp(frame);
                        break;
                    case MeasurementMode.Heart:
                        cvDisplayString = new MeasurePearShape().MeasureHeartWithOpenCvSharp(frame);
                        break;
                    case MeasurementMode.Marquise:
                        cvDisplayString = new MeasurePearShape().MeasureMarkWithOpenCvSharp(frame);
                        break;
                    case MeasurementMode.Poly:
                        cvDisplayString = PolygonMeasurement.DetectAndMeasurePolygon(frame);
                        break;
                    case MeasurementMode.General:
                        cvDisplayString = new MeasurePearShape().MeasurePearWithOpenCvSharp1(frame);
                        break;
                    case MeasurementMode.GeneralC:
                        cvDisplayString = new MeasurePearShape().MeasureShapeWithVertexAxis(frame);
                        break;
                    case MeasurementMode.Custom:
                        cvDisplayString = customEngine.MeasureCustomShape(frame, activeCustomShape, backgroundGray);
                        break;
                }
                this.BeginInvoke((MethodInvoker)delegate
                {
                    if (!string.IsNullOrEmpty(cvDisplayString))
                        UpdateMeasurementUI(cvDisplayString);
                });

                if (string.IsNullOrEmpty(cvDisplayString) || cvDisplayString.Contains("Error") || cvDisplayString.Contains("Please Calibrate") || cvDisplayString.ToLower().Contains("object"))
                    return;

                double extractedLength = 0.0;
                double extractedWidth = 0.0;

                // ==========================================
                // 2. CENTRAL STRING DECODER ENGINE
                // ==========================================
                if (currentMode == MeasurementMode.Round)
                {
                    string[] parts = cvDisplayString.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length >= 3) double.TryParse(parts[2], out extractedLength);
                }
                else if (cvDisplayString.Contains("Side"))
                {
                    MatchCollection sideMatches = Regex.Matches(cvDisplayString, @"Side\s+\d+:\s+([0-9]+(?:\.[0-9]+)?)");
                    double maxSide = 0.0;
                    double minSide = double.MaxValue;

                    foreach (Match match in sideMatches)
                    {
                        if (match.Groups.Count > 1 && double.TryParse(match.Groups[1].Value, out double currentSideValue))
                        {
                            if (currentSideValue > maxSide) maxSide = currentSideValue;
                            if (currentSideValue < minSide) minSide = currentSideValue;
                        }
                    }
                    extractedLength = maxSide;
                    extractedWidth = (minSide == double.MaxValue) ? 0.0 : minSide;
                }
                else
                {
                    MatchCollection numbers = Regex.Matches(cvDisplayString, @"[0-9]+(?:\.[0-9]+)?");
                    if (numbers.Count >= 1) double.TryParse(numbers[0].Value, out extractedLength);
                    if (numbers.Count >= 2) double.TryParse(numbers[1].Value, out extractedWidth);
                }

                // ==========================================
                // 3. DATATABLE RULE MATCHING ENGINE
                // ==========================================
                DataRow matchedSortingRule = null;
                string currentShapeNameString = currentMode.ToString();

                if (dtRules != null)
                {
                    foreach (DataRow row in dtRules.Rows)
                    {
                        double fromLen = Convert.ToDouble(row["FromLength"]);
                        double toLen = Convert.ToDouble(row["ToLength"]);
                        double fromWid = Convert.ToDouble(row["FromWidth"]);
                        double toWid = Convert.ToDouble(row["ToWidth"]);

                        if (currentMode == MeasurementMode.Round)
                        {
                            // Round shape: Consider FromLength and ToLength only
                            if (extractedLength >= fromLen && extractedLength <= toLen)
                            {
                                matchedSortingRule = row;
                                break;
                            }
                        }
                        else
                        {
                            // All other shapes: Consider all 4 parameters (Length + Width ranges)
                            if (extractedLength >= fromLen && extractedLength <= toLen && extractedWidth >= fromWid && extractedWidth <= toWid)
                            {
                                matchedSortingRule = row;
                                break;
                            }
                        }
                    }
                }

                // ==========================================
                // 4. HARDWARE SERIAL TRIGGER
                // ==========================================
                if (matchedSortingRule != null)
                {
                    int targetSquareNumber = Convert.ToInt32(matchedSortingRule["Number"]);

                    // --- INCREMENT COUNTER ---
                    lock (counterLock)
                    {
                        if (lightBlinkCounters.ContainsKey(targetSquareNumber))
                        {
                            lightBlinkCounters[targetSquareNumber]++;
                        }
                        else
                        {
                            lightBlinkCounters[targetSquareNumber] = 1;
                        }
                        SaveCountersToFile();
                    }

                    if (isSerialConnected && arduinoPort != null && arduinoPort.IsOpen)
                    {
                        arduinoPort.WriteLine(targetSquareNumber.ToString() + "\n");
                    }
                }

                // ==========================================
                // 5. AUTOMATED DATA LOGGING ENGINE (WITH DRAWINGS)
                // ==========================================
                if (autosave)
                {
                    try
                    {
                        string recordsFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "CapturedGemstones");
                        if (!Directory.Exists(recordsFolder))
                        {
                            Directory.CreateDirectory(recordsFolder);
                        }

                        string fileName = $"Gem_{DateTime.Now:yyyyMMdd_HHmmssfff}.png";
                        string fullImagePath = Path.Combine(recordsFolder, fileName);

                        OpenCvSharp.Cv2.ImWrite(fullImagePath, frame);

                        string dbConnectionString = "Data Source=History.db";

                        using (var connection = new SQLiteConnection(dbConnectionString))
                        {
                            connection.Open();
                            string insertSql = @"INSERT INTO Records (Date, Shape, Image, Length, Width) VALUES (@date, @shape, @image, @length, @width);";

                            using (var command = new SQLiteCommand(insertSql, connection))
                            {
                                command.Parameters.AddWithValue("@date", DateTime.Now.ToString("yyyy-MM-dd"));
                                command.Parameters.AddWithValue("@shape", currentShapeNameString);
                                command.Parameters.AddWithValue("@image", fullImagePath);
                                command.Parameters.AddWithValue("@length", extractedLength);
                                command.Parameters.AddWithValue("@width", extractedWidth);

                                command.ExecuteNonQuery();
                            }
                        }
                    }
                    catch (Exception dbEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"Autosave write error: {dbEx.Message}");
                    }
                }

                if (autoPrint)
                {
                    string printData = "";

                    // Check if the UI is currently displaying a Diameter or standard Length/Width
                    if (lblLengthTitle.Text.ToLower() == "diameter")
                    {
                        printData = $"Diameter: {lblLengthVal.Text} mm";
                    }
                    else
                    {
                        // Build the 3-line string exactly as it used to be
                        printData = $"Length: {lblLengthVal.Text} mm\n" +
                                    $"Width: {lblWidthVal.Text} mm\n" +
                                    $"Ratio: {lblRatioVal.Text}";
                    }

                    // Pass the reconstructed string to your printing engine
                    //engine.PrintSingleDiamond(printData, targetedDevice);

                    LabelPrintingEngine engine = new LabelPrintingEngine();
                    engine.PrintSingleDiamond(printData, targetedDevice);
                }
            }
            catch (Exception except) { }
        }

        private void ProcessFinalAveragedMeasurement(double cleanLength, double cleanWidth)
        {
            try
            {
                // Update user interface with perfectly smoothed data metrics
                if (currentMode == MeasurementMode.Round)
                    UpdateMeasurementUI($"Diameter : {cleanLength:F2} mm");
                else
                    UpdateMeasurementUI($"Length : {cleanLength:F2} mm\nWidth : {cleanWidth:F2} mm");

                // Match against database rules parameters
                DataRow matchedSortingRule = null;
                string currentShapeNameString = currentMode.ToString();

                foreach (DataRow row in dtRules.Rows)
                {
                    if (row["ShapeType"].ToString() == currentShapeNameString)
                    {
                        double fromLen = Convert.ToDouble(row["FromLength"]);
                        double toLen = Convert.ToDouble(row["ToLength"]);
                        double fromWid = Convert.ToDouble(row["FromWidth"]);
                        double toWid = Convert.ToDouble(row["ToWidth"]);

                        if (currentMode == MeasurementMode.Round)
                        {
                            if (cleanLength >= fromLen && cleanLength <= toLen)
                            {
                                matchedSortingRule = row;
                                break;
                            }
                        }
                        else
                        {
                            if (cleanLength >= fromLen && cleanLength <= toLen && cleanWidth >= fromWid && cleanWidth <= toWid)
                            {
                                matchedSortingRule = row;
                                break;
                            }
                        }
                    }
                }

                // Handle counters persistence tracking maps and notify hardware via Serial COM
                if (matchedSortingRule != null)
                {
                    int targetSquareNumber = Convert.ToInt32(matchedSortingRule["Number"]);

                    lock (counterLock)
                    {
                        if (lightBlinkCounters.ContainsKey(targetSquareNumber))
                            lightBlinkCounters[targetSquareNumber]++;
                        else
                            lightBlinkCounters[targetSquareNumber] = 1;

                        SaveCountersToFile();
                    }

                    if (isSerialConnected && arduinoPort != null && arduinoPort.IsOpen)
                    {
                        arduinoPort.WriteLine(targetSquareNumber.ToString() + "\n");
                    }
                }
            }
            catch (Exception) { }
        }

        private void btnCalib_Click(object sender, EventArgs e)
        {
            resetCounts();
            if (calibforminstance == null || calibforminstance.IsDisposed)
            {
                calibforminstance = new FrmCalib();
                calibforminstance.Show(this);
            }
            else
            {
                // 3. If it is already open, restore it if minimized and bring it to the top
                if (calibforminstance.WindowState == FormWindowState.Minimized)
                {
                    calibforminstance.WindowState = FormWindowState.Normal;
                }
                calibforminstance.BringToFront();
            }
        }

        private void btnRound_Click(object sender, EventArgs e)
        {
            btnRound.BackgroundImage = Properties.Resources.ROUND_Select;
            btnPear.BackgroundImage = Properties.Resources.PEAR;
            btnHeart.BackgroundImage = Properties.Resources.HEART;
            btnOvel.BackgroundImage = Properties.Resources.OVAL;
            btnmarqu.BackgroundImage = Properties.Resources.MARQUISE;
            btnPoly.BackgroundImage = Properties.Resources.POLYGON;
            btnEM.BackgroundImage = Properties.Resources.GENERAL;
            btnGeneralC.BackgroundImage = Properties.Resources.GENERALC;
            //resetCounts();
            picPreview.Image = null;
            SwitchMeasurementMode(MeasurementMode.Round);
        }

        private void btnPear_Click(object sender, EventArgs e)
        {
            btnRound.BackgroundImage = Properties.Resources.ROUND;
            btnPear.BackgroundImage = Properties.Resources.PEAR_Select;
            btnHeart.BackgroundImage = Properties.Resources.HEART;
            btnOvel.BackgroundImage = Properties.Resources.OVAL;
            btnmarqu.BackgroundImage = Properties.Resources.MARQUISE;
            btnPoly.BackgroundImage = Properties.Resources.POLYGON;
            btnEM.BackgroundImage = Properties.Resources.GENERAL;
            btnGeneralC.BackgroundImage = Properties.Resources.GENERALC;
            picPreview.Image = null;
            SwitchMeasurementMode(MeasurementMode.Pear);
        }

        private void btnHeart_Click(object sender, EventArgs e)
        {
            btnRound.BackgroundImage = Properties.Resources.ROUND;
            btnPear.BackgroundImage = Properties.Resources.PEAR;
            btnHeart.BackgroundImage = Properties.Resources.HEART_Select;
            btnOvel.BackgroundImage = Properties.Resources.OVAL;
            btnmarqu.BackgroundImage = Properties.Resources.MARQUISE;
            btnPoly.BackgroundImage = Properties.Resources.POLYGON;
            btnEM.BackgroundImage = Properties.Resources.GENERAL;
            btnGeneralC.BackgroundImage = Properties.Resources.GENERALC;
            picPreview.Image = null;
            SwitchMeasurementMode(MeasurementMode.Heart);
        }

        private void btnOvel_Click(object sender, EventArgs e)
        {
            btnRound.BackgroundImage = Properties.Resources.ROUND;
            btnPear.BackgroundImage = Properties.Resources.PEAR;
            btnHeart.BackgroundImage = Properties.Resources.HEART;
            btnOvel.BackgroundImage = Properties.Resources.OVAL_Select;
            btnmarqu.BackgroundImage = Properties.Resources.MARQUISE;
            btnPoly.BackgroundImage = Properties.Resources.POLYGON;
            btnEM.BackgroundImage = Properties.Resources.GENERAL;
            btnGeneralC.BackgroundImage = Properties.Resources.GENERALC;
            picPreview.Image = null;
            SwitchMeasurementMode(MeasurementMode.Oval);
        }

        private void btnPoly_Click(object sender, EventArgs e)
        {
            btnRound.BackgroundImage = Properties.Resources.ROUND;
            btnPear.BackgroundImage = Properties.Resources.PEAR;
            btnHeart.BackgroundImage = Properties.Resources.HEART;
            btnOvel.BackgroundImage = Properties.Resources.OVAL;
            btnmarqu.BackgroundImage = Properties.Resources.MARQUISE;
            btnPoly.BackgroundImage = Properties.Resources.POLYGON_Select;
            btnEM.BackgroundImage = Properties.Resources.GENERAL;
            btnGeneralC.BackgroundImage = Properties.Resources.GENERALC;
            picPreview.Image = null;
            SwitchMeasurementMode(MeasurementMode.Poly);
        }

        private void btnmarqu_Click(object sender, EventArgs e)
        {
            btnRound.BackgroundImage = Properties.Resources.ROUND;
            btnPear.BackgroundImage = Properties.Resources.PEAR;
            btnHeart.BackgroundImage = Properties.Resources.HEART;
            btnOvel.BackgroundImage = Properties.Resources.OVAL;
            btnmarqu.BackgroundImage = Properties.Resources.MARQUISE_Select;
            btnPoly.BackgroundImage = Properties.Resources.POLYGON;
            btnEM.BackgroundImage = Properties.Resources.GENERAL;
            btnGeneralC.BackgroundImage = Properties.Resources.GENERALC;
            picPreview.Image = null;
            SwitchMeasurementMode(MeasurementMode.Marquise);
            //TriggerAutoMeasurement(BitmapConverter.ToMat(new Bitmap("MR.png")));
        }

        private void btnEM_Click(object sender, EventArgs e)
        {
            btnRound.BackgroundImage = Properties.Resources.ROUND;
            btnPear.BackgroundImage = Properties.Resources.PEAR;
            btnHeart.BackgroundImage = Properties.Resources.HEART;
            btnOvel.BackgroundImage = Properties.Resources.OVAL;
            btnmarqu.BackgroundImage = Properties.Resources.MARQUISE;
            btnPoly.BackgroundImage = Properties.Resources.POLYGON;
            btnGeneralC.BackgroundImage = Properties.Resources.GENERALC;
            btnEM.BackgroundImage = Properties.Resources.GENERAL_Selected;
            picPreview.Image = null;
            SwitchMeasurementMode(MeasurementMode.General);
        }

        private void btnSettings_Click(object sender, EventArgs e)
        {
            if (camsetInstance == null || camsetInstance.IsDisposed)
            {
                camsetInstance = new Camera_Setting1(Camera);
                camsetInstance.Show(this);
            }
            else
            {
                if (camsetInstance.WindowState == FormWindowState.Minimized)
                {
                    camsetInstance.WindowState = FormWindowState.Normal;
                }
                camsetInstance.BringToFront();
            }
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            //isMeasurementStopped = true;
            currentMode = MeasurementMode.None; // Force the background state back to GeneralC

            UpdateMeasurementUI("Auto Measurement Stopped.");
            resetCounts();

            // Reset all buttons to unselected
            btnRound.BackgroundImage = Properties.Resources.ROUND;
            btnPear.BackgroundImage = Properties.Resources.PEAR;
            btnHeart.BackgroundImage = Properties.Resources.HEART;
            btnOvel.BackgroundImage = Properties.Resources.OVAL;
            btnmarqu.BackgroundImage = Properties.Resources.MARQUISE;
            btnPoly.BackgroundImage = Properties.Resources.POLYGON;
            btnEM.BackgroundImage = Properties.Resources.GENERAL;

            // Visually highlight GeneralC as the active fallback mode
            btnGeneralC.BackgroundImage = Properties.Resources.GENERALC;
            //btnGeneralC_Click(null,null);
            picPreview.Image = null;
        }

        private void btnTilt_Click(object sender, EventArgs e)
        {
            //showTiltCalibration = !showTiltCalibration;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            lock (counterLock)
            {
                // Instantiates and opens the print dashboard window with your active counts
                frmPrint printWindow = new frmPrint(lightBlinkCounters);
                printWindow.ShowDialog();
            }
        }

        private void btnGeneralC_Click(object sender, EventArgs e)
        {
            btnRound.BackgroundImage = Properties.Resources.ROUND;
            btnPear.BackgroundImage = Properties.Resources.PEAR;
            btnHeart.BackgroundImage = Properties.Resources.HEART;
            btnOvel.BackgroundImage = Properties.Resources.OVAL;
            btnmarqu.BackgroundImage = Properties.Resources.MARQUISE;
            btnPoly.BackgroundImage = Properties.Resources.POLYGON;
            btnGeneralC.BackgroundImage = Properties.Resources.GENERALC;
            btnEM.BackgroundImage = Properties.Resources.GENERAL;
            btnGeneralC.BackgroundImage = Properties.Resources.GENERALC_selected;
            picPreview.Image = null;
            SwitchMeasurementMode(MeasurementMode.GeneralC);
        }

        private void btnCaptureCustomShape_Click(object sender, EventArgs e)
        {
            Mat capturedSnapshot = new Mat();

            lock (frameLock)
            {
                if (liveMat == null || liveMat.Empty())
                {
                    MessageBox.Show("No active camera frame available.");
                    return;
                }
                liveMat.CopyTo(capturedSnapshot);
            }

            using (CustomShapeForm form = new CustomShapeForm(capturedSnapshot))
            {
                if (form.ShowDialog() == DialogResult.OK)
                {
                    PopulateCustomShapesMenu(); // Refresh menu with newly added shape
                }
            }
        }
        
        public string MeasureHalfMoonWithOpenCvSharp(Mat inputImage)
        {
            Mat src = inputImage.Clone();
            Mat gray = new Mat();
            Cv2.CvtColor(src, gray, ColorConversionCodes.BGR2GRAY);

            // ==========================================
            // Preprocessing (Retained from your code)
            // ==========================================
            Mat blur = new Mat();
            Cv2.GaussianBlur(gray, blur, new OpenCvSharp.Size(7, 7), 0);

            Mat edges = new Mat();
            Cv2.Canny(blur, edges, 30, 100);

            // Using smaller kernels to prevent "swelling" of the shape
            Mat kernel = Cv2.GetStructuringElement(MorphShapes.Ellipse, new OpenCvSharp.Size(3, 3));
            Cv2.MorphologyEx(edges, edges, MorphTypes.Close, kernel);
            Cv2.Dilate(edges, edges, kernel, iterations: 1);

            // ==========================================
            // Contour Processing (Standard)
            // ==========================================
            OpenCvSharp.Point[][] contours;
            HierarchyIndex[] hierarchy;
            Cv2.FindContours(edges, out contours, out hierarchy, RetrievalModes.External, ContourApproximationModes.ApproxSimple);

            if (contours.Length == 0) return "No Shape Found";

            double maxArea = 0;
            int largestIndex = 0;
            for (int i = 0; i < contours.Length; i++)
            {
                double area = Cv2.ContourArea(contours[i]);
                if (area > maxArea) { maxArea = area; largestIndex = i; }
            }

            // ==========================================
            // --- PERFECTED DETECTION LOGIC ---
            // DO NOT USE CONVEX HULL FOR HALF-MOON
            // ==========================================
            OpenCvSharp.Point[] rawContour = contours[largestIndex];

            // 1. Find the two points farthest apart (the tips of the stone)
            double maxDistanceSq = 0;
            OpenCvSharp.Point tipPoint1 = rawContour[0];
            OpenCvSharp.Point tipPoint2 = rawContour[0];

            for (int i = 0; i < rawContour.Length; i++)
            {
                for (int j = i + 1; j < rawContour.Length; j++)
                {
                    double dx = rawContour[i].X - rawContour[j].X;
                    double dy = rawContour[i].Y - rawContour[j].Y;
                    double distSq = (dx * dx) + (dy * dy);
                    if (distSq > maxDistanceSq)
                    {
                        maxDistanceSq = distSq;
                        tipPoint1 = rawContour[i];
                        tipPoint2 = rawContour[j];
                    }
                }
            }

            // This line is now the *perfect* blue baseline
            OpenCvSharp.Point basePt1 = tipPoint1;
            OpenCvSharp.Point basePt2 = tipPoint2;
            double baselineLengthPx = Math.Sqrt(maxDistanceSq);

            // 2. Line equation: Ax + By + C = 0 for the baseline
            double A = basePt2.Y - basePt1.Y;
            double B = basePt1.X - basePt2.X;
            double C = (basePt2.X * basePt1.Y) - (basePt1.X * basePt2.Y);
            double denominator = Math.Sqrt(A * A + B * B);

            // 3. Find maximum perpendicular height *from* the baseline *to* the curve
            double maxHeight = 0;
            OpenCvSharp.Point peakPoint = rawContour[0];

            foreach (OpenCvSharp.Point p in rawContour)
            {
                double perpendicularDist = Math.Abs(A * p.X + B * p.Y + C) / denominator;
                if (perpendicularDist > maxHeight)
                {
                    maxHeight = perpendicularDist;
                    peakPoint = p;
                }
            }

            // Find intersection point on the baseline for the visual line
            double k = ((peakPoint.X - basePt1.X) * (basePt2.X - basePt1.X) + (peakPoint.Y - basePt1.Y) * (basePt2.Y - basePt1.Y)) / (denominator * denominator);
            OpenCvSharp.Point intersectionPoint = new OpenCvSharp.Point((int)(basePt1.X + k * (basePt2.X - basePt1.X)), (int)(basePt1.Y + k * (basePt2.Y - basePt1.Y)));

            // ==========================================
            // Conversion and Centering (For Calibration)
            // ==========================================
            // Using a default of 100 ppm for demonstration. You must calibrate this value.
            double ppm = 100.0;
            ppm = Convert.ToDouble(new ModifyRegistry().Read("ppm"));

            double baseLengthMM = baselineLengthPx / ppm;
            double heightMM = maxHeight / ppm;
            double ratio = baseLengthMM / heightMM;

            // Recenter the frame to the geometric center of the baseline (for calibration use)
            Mat finalOutput = src.Clone();
            int shiftX = (src.Width / 2) - ((basePt1.X + basePt2.X) / 2);
            int shiftY = (src.Height / 2) - ((basePt1.Y + basePt2.Y) / 2);
            Cv2.WarpAffine(finalOutput, finalOutput, Cv2.GetRotationMatrix2D(new Point2f(0, 0), 0, 1.0), finalOutput.Size(), InterpolationFlags.Linear, BorderTypes.Constant, Scalar.All(100));
            // In a real application, you would just recenter the drawings, but we physically shift the image for clarity here.

            // ==========================================
            // Visual Overlays (Drawing on recentered frame)
            // ==========================================
            // Blue Baseline (Farthest points)
            Cv2.Line(finalOutput, basePt1 + new OpenCvSharp.Point(shiftX, shiftY), basePt2 + new OpenCvSharp.Point(shiftX, shiftY), Scalar.Blue, 2);
            Cv2.Circle(finalOutput, basePt1 + new OpenCvSharp.Point(shiftX, shiftY), 4, Scalar.Red, -1);
            Cv2.Circle(finalOutput, basePt2 + new OpenCvSharp.Point(shiftX, shiftY), 4, Scalar.Red, -1);

            // Yellow Height Line (Max distance from baseline to curve)
            Cv2.Line(finalOutput, peakPoint + new OpenCvSharp.Point(shiftX, shiftY), intersectionPoint + new OpenCvSharp.Point(shiftX, shiftY), Scalar.Yellow, 2);
            Cv2.Circle(finalOutput, peakPoint + new OpenCvSharp.Point(shiftX, shiftY), 4, Scalar.Magenta, -1);

            // Draw Central Crosshair (1px line, Magenta)
            Cv2.Line(finalOutput, new OpenCvSharp.Point(finalOutput.Width / 2, 0), new OpenCvSharp.Point(finalOutput.Width / 2, finalOutput.Height), Scalar.Magenta, 1);
            Cv2.Line(finalOutput, new OpenCvSharp.Point(0, finalOutput.Height / 2), new OpenCvSharp.Point(finalOutput.Width, finalOutput.Height / 2), Scalar.Magenta, 1);

            // Save and return results
            finalOutput.ImWrite("Perfect_HalfMoon_Result.png");

            return $"Length: {baseLengthMM:F2} mm\n" +
                   $"Width : {heightMM:F2} mm\n" +
                   $"L/W Ratio      : {ratio:F2}";
        
        }

        private void button2_Click(object sender, EventArgs e)
        {
            string printData = "";

            // Check if the UI is currently displaying a Diameter or standard Length/Width
            if (lblLengthTitle.Text.ToLower() == "diameter")
            {
                printData = $"Diameter: {lblLengthVal.Text} mm";
            }
            else
            {
                // Build the 3-line string exactly as it used to be
                printData = $"Length: {lblLengthVal.Text} mm\n" +
                            $"Width: {lblWidthVal.Text} mm\n" +
                            $"Ratio: {lblRatioVal.Text}";
            }

            // Pass the reconstructed string to your printing engine
            



            LabelPrintingEngine engine = new LabelPrintingEngine();
            engine.PrintSingleDiamond(printData, targetedDevice);
        }

        private void cmbRulesFile_SelectedIndexChanged(object sender, EventArgs e)
        {
            // 1. IF THE FORM IS STILL LOADING, DO NOT OVERWRITE THE REGISTRY!
            if (isInitializing) return;

            if (cmbRulesFile.SelectedItem != null)
            {
                currentfile = cmbRulesFile.SelectedItem.ToString();
                new ModifyRegistry().Write("cmbFile", currentfile);
                LoadRulesDatabase();
            }
        }

        private void cmbRulesFile_DrawItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index < 0) return;
            System.Windows.Forms.ComboBox combo = sender as System.Windows.Forms.ComboBox;
            string itemText = combo.Items[e.Index].ToString();
            System.Drawing.Color backColor;
            System.Drawing.Color foreColor;

            if ((e.State & DrawItemState.Selected) == DrawItemState.Selected)
            {
                backColor = System.Drawing.Color.FromArgb(255, 128, 0);
                foreColor = System.Drawing.Color.White;
            }
            else
            {
                backColor = combo.BackColor;
                foreColor = combo.ForeColor;
            }

            using (SolidBrush bgBrush = new SolidBrush(backColor))
            {
                e.Graphics.FillRectangle(bgBrush, e.Bounds);
            }
            using (SolidBrush textBrush = new SolidBrush(foreColor))
            {
                e.Graphics.DrawString(itemText, e.Font, textBrush, e.Bounds);
            }
            e.DrawFocusRectangle();
        }

        public void docalibold(Bitmap bmp)
        {
            ModifyRegistry mr = new ModifyRegistry();
            var metrology = new ImageMetrology();
            Mat src = BitmapConverter.ToMat(bmp);//Cv2.ImRead(@"C:\Users\Administrator\MVS\Data\2.67.bmp", ImreadModes.Color);
            if (src.Empty()) return;

            Mat gray = new Mat();
            Cv2.CvtColor(src, gray, ColorConversionCodes.BGR2GRAY);

            Cv2.MedianBlur(gray, gray, 7);

            CircleSegment[] circles = Cv2.HoughCircles(
                gray,
                HoughModes.Gradient,
                dp: 1.2,
                minDist: 100,
                param1: 35,
                param2: 25,
                minRadius: 100,
                maxRadius: 500
            );
            if (circles.Length > 0)
            {
                double ppm = metrology.Calibrate(circles[0].Radius, Convert.ToDouble(mr.Read("calibval")));
                mr.Write("ppm", ppm);
                this.Invoke((MethodInvoker)delegate
                {
                    UpdateMeasurementUI("Calibration complete");
                });
            }
            else
            {
                this.Invoke((MethodInvoker)delegate
                {
                    UpdateMeasurementUI("Calibration Not Done");
                });
            }
        }

        public void doMesulureold(Mat src)
        {
            var metrology = new ImageMetrology();
            //Mat src = BitmapConverter.ToMat(bmp);
            if (src.Empty()) return;
            Mat gray = new Mat();
            Cv2.CvtColor(src, gray, ColorConversionCodes.BGR2GRAY);
            Cv2.MedianBlur(gray, gray, 7);
            //CircleSegment[] circles = Cv2.HoughCircles(gray,HoughModes.Gradient,dp: 1.2,minDist: 100,param1: 35,param2: 25,minRadius: 100,maxRadius: 500);
            CircleSegment[] circles = Cv2.HoughCircles(
                gray,
                HoughModes.Gradient,
                dp: 1.0,           // Strict 1:1 resolution to match precise boundaries
                minDist: 100,
                param1: 50,         // Higher value = filters out weak internal texture edges
                param2: 30,         // Higher value = requires a more complete circular edge to trigger
                minRadius: 60,
                maxRadius: 600
            );
            if (circles.Length > 0)
            {
                CircleSegment targetCircle = circles[0];
                double realSize = metrology.GetRealDiameter(targetCircle);
                //realSize = RoundToDecimals((float)realSize, 2);
                // 2. DRAW SHAPES DIRECTLY ONTO THE INCOMING MAT
                // Draw green outer ring
                Cv2.Circle(src, (int)targetCircle.Center.X, (int)targetCircle.Center.Y, (int)targetCircle.Radius, Scalar.Lime, 1);
                // Draw red center point
                Cv2.Circle(src, (int)targetCircle.Center.X, (int)targetCircle.Center.Y, 5, Scalar.Red, -1);
                //src.SaveImage("round.png");
                OpenCvSharp.Point textPosition = new OpenCvSharp.Point((int)targetCircle.Center.X + 15, (int)targetCircle.Center.Y + 5);
                Cv2.PutText(src, $"{realSize:F2} mm",textPosition,HersheyFonts.HersheySimplex,0.6,Scalar.Yellow,2);
                src.ImWrite("Circle.png");
                this.Invoke((MethodInvoker)delegate
                {
                    UpdateMeasurementUI($"Diameter : {realSize:F2} mm");
                });
            }
            else
            {
                this.Invoke((MethodInvoker)delegate
                {
                    UpdateMeasurementUI($"Diameter Not Detected");
                });
            }
        }

        /*public void docalib(Bitmap bmp)
        {
            ModifyRegistry mr = new ModifyRegistry();
            Mat src = BitmapConverter.ToMat(bmp);
            if (src.Empty()) return;

            using (Mat gray = new Mat())
            using (Mat blur = new Mat())
            using (Mat thresh = new Mat())
            {
                Cv2.CvtColor(src, gray, ColorConversionCodes.BGR2GRAY);
                Cv2.GaussianBlur(gray, blur, new OpenCvSharp.Size(5, 5), 0);

                // Universal Otsu Thresholding
                Cv2.Threshold(blur, thresh, 0, 255, ThresholdTypes.BinaryInv | ThresholdTypes.Otsu);

                using (Mat kernel = Cv2.GetStructuringElement(MorphShapes.Ellipse, new OpenCvSharp.Size(5, 5)))
                {
                    Cv2.MorphologyEx(thresh, thresh, MorphTypes.Close, kernel);
                    Cv2.MorphologyEx(thresh, thresh, MorphTypes.Open, kernel);
                }

                OpenCvSharp.Point[][] contours;
                HierarchyIndex[] hierarchy;
                Cv2.FindContours(thresh, out contours, out hierarchy, RetrievalModes.External, ContourApproximationModes.ApproxSimple);

                if (contours.Length > 0)
                {
                    // Isolate the true calibration target
                    var largestContour = contours.OrderByDescending(c => Cv2.ContourArea(c)).First();

                    // FIX: Use BoundingRect instead of MinEnclosingCircle. 
                    // This captures the exact pixel width (X) and height (Y) as seen by the camera sensor.
                    OpenCvSharp.Rect boundingRect = Cv2.BoundingRect(largestContour);

                    if (boundingRect.Width > 50 && boundingRect.Height > 50)
                    {
                        // Read the physical diameter of your calibration dot (e.g., 10.0 mm)
                        double physicalDiameter = Convert.ToDouble(mr.Read("calibval"));

                        // Calculate independent PPM for X and Y axes
                        double ppmX = boundingRect.Width / physicalDiameter;
                        double ppmY = boundingRect.Height / physicalDiameter;

                        // Save both to registry
                        mr.Write("ppmX", ppmX);
                        mr.Write("ppmY", ppmY);

                        // Draw visual verification
                        Cv2.Rectangle(src, boundingRect, Scalar.Lime, 2, LineTypes.AntiAlias);

                        int centerX = boundingRect.X + (boundingRect.Width / 2);
                        int centerY = boundingRect.Y + (boundingRect.Height / 2);
                        Cv2.Circle(src, centerX, centerY, 3, Scalar.Red, -1, LineTypes.AntiAlias);

                        // Overlay the calculated PPMs on the image for debugging
                        Cv2.PutText(src, $"ppmX: {ppmX:F3}", new OpenCvSharp.Point(10, 30), HersheyFonts.HersheySimplex, 0.6, Scalar.Yellow, 2);
                        Cv2.PutText(src, $"ppmY: {ppmY:F3}", new OpenCvSharp.Point(10, 60), HersheyFonts.HersheySimplex, 0.6, Scalar.Yellow, 2);

                        src.ImWrite("calib_result.png");

                        this.Invoke((MethodInvoker)delegate
                        {
                            label1.Text = "Calibration complete";
                        });
                        return;
                    }
                }

                this.Invoke((MethodInvoker)delegate
                {
                    label1.Text = "Calibration Not Done";
                });
            }
        }*/

        public void docalib(Bitmap bmp)
        {
            ModifyRegistry mr = new ModifyRegistry();
            var metrology = new ImageMetrology();
            Mat src = BitmapConverter.ToMat(bmp);
            if (src.Empty()) return;

            using (Mat gray = new Mat())
            using (Mat blur = new Mat())
            using (Mat thresh = new Mat())
            {
                Cv2.CvtColor(src, gray, ColorConversionCodes.BGR2GRAY);
                Cv2.GaussianBlur(gray, blur, new OpenCvSharp.Size(5, 5), 0);

                // Universal Otsu Thresholding: Instantly locks onto the object and ignores background noise/hand shadows
                Cv2.Threshold(blur, thresh, 0, 255, ThresholdTypes.BinaryInv | ThresholdTypes.Otsu);

                using (Mat kernel = Cv2.GetStructuringElement(MorphShapes.Ellipse, new OpenCvSharp.Size(5, 5)))
                {
                    Cv2.MorphologyEx(thresh, thresh, MorphTypes.Close, kernel);
                    Cv2.MorphologyEx(thresh, thresh, MorphTypes.Open, kernel);
                }

                OpenCvSharp.Point[][] contours;
                HierarchyIndex[] hierarchy;
                Cv2.FindContours(thresh, out contours, out hierarchy, RetrievalModes.External, ContourApproximationModes.ApproxSimple);

                if (contours.Length > 0)
                {
                    // Isolate the true calibration target by picking the largest solid object
                    var largestContour = contours.OrderByDescending(c => Cv2.ContourArea(c)).First();

                    // Fit a precise sub-pixel minimum enclosing circle around the contour
                    Cv2.MinEnclosingCircle(largestContour, out Point2f center, out float radius);

                    if (radius > 50) // Minimum pixel check to ignore tiny dust particles
                    {
                        double ppm = metrology.Calibrate(radius, Convert.ToDouble(mr.Read("calibval")));
                        mr.Write("ppm", ppm);

                        // Draw visual verification
                        Cv2.Circle(src, (int)center.X, (int)center.Y, (int)radius, Scalar.Lime, 2, LineTypes.AntiAlias);
                        Cv2.Circle(src, (int)center.X, (int)center.Y, 3, Scalar.Red, -1, LineTypes.AntiAlias);
                        src.ImWrite("calib_result.png");

                        this.Invoke((MethodInvoker)delegate
                        {
                            UpdateMeasurementUI("Calibration complete");
                        });
                        return;
                    }
                }

                this.Invoke((MethodInvoker)delegate
                {
                    UpdateMeasurementUI("Calibration Not Done");
                });
            }
        }

        public string doMesure(Mat src)
        {
            var metrology = new ImageMetrology();
            if (src == null || src.Empty()) return "";

            double ppm = 1.0;
            try
            {
                ppm = Convert.ToDouble(new ModifyRegistry().Read("ppm"));
            }
            catch
            {
                ppm = 1.0;
            }
            if (ppm <= 0) ppm = 1.0;

            using (Mat gray = new Mat())
            using (Mat blur = new Mat())
            using (Mat thresh = new Mat())
            {
                Cv2.CvtColor(src, gray, ColorConversionCodes.BGR2GRAY);
                Cv2.GaussianBlur(gray, blur, new OpenCvSharp.Size(5, 5), 0);

                // Adaptive Otsu Thresholding guarantees zero fluctuation when lighting shifts or hands move
                Cv2.Threshold(blur, thresh, 0, 255, ThresholdTypes.BinaryInv | ThresholdTypes.Otsu);

                using (Mat kernel = Cv2.GetStructuringElement(MorphShapes.Ellipse, new OpenCvSharp.Size(5, 5)))
                {
                    Cv2.MorphologyEx(thresh, thresh, MorphTypes.Close, kernel);
                    Cv2.MorphologyEx(thresh, thresh, MorphTypes.Open, kernel);
                }

                OpenCvSharp.Point[][] contours;
                HierarchyIndex[] hierarchy;
                Cv2.FindContours(thresh, out contours, out hierarchy, RetrievalModes.External, ContourApproximationModes.ApproxSimple);

                if (contours.Length > 0)
                {
                    var largestContour = contours.OrderByDescending(c => Cv2.ContourArea(c)).First();
                    double area = Cv2.ContourArea(largestContour);

                    if (area > 500) // Ignore small noise blobs
                    {
                        // Sub-pixel accurate bounding circle calculation
                        Cv2.MinEnclosingCircle(largestContour, out Point2f center, out float radius);

                        // Calculate diameter in millimeters using PPM
                        double realSize = (radius * 2.0) / ppm;

                        // Render Order Fix: Draw measurement overlays cleanly
                        Cv2.Circle(src, (int)Math.Round(center.X), (int)Math.Round(center.Y), (int)Math.Round(radius), Scalar.Lime, 1, LineTypes.AntiAlias);
                        Cv2.Circle(src, (int)Math.Round(center.X), (int)Math.Round(center.Y), 4, Scalar.Red, -1, LineTypes.AntiAlias);

                        OpenCvSharp.Point textPosition = new OpenCvSharp.Point((int)Math.Round(center.X) + 15, (int)Math.Round(center.Y) + 5);
                        Cv2.PutText(src, $"{realSize:F2} mm", textPosition, HersheyFonts.HersheySimplex, 0.6, Scalar.Yellow, 2, LineTypes.AntiAlias);

                        // ==========================================================
                        // CREATE ISOLATED SHAPE IMAGE FOR LABEL PRINTING
                        // ==========================================================
                        using (Mat printCanvas = new Mat(src.Size(), MatType.CV_8UC3, Scalar.White))
                        {
                            // Draw ONLY the shape outline in thick black
                            Cv2.Polylines(printCanvas, new[] { largestContour }, true, Scalar.Black, 3, LineTypes.AntiAlias);

                            // Find the bounding box to crop away empty white space
                            OpenCvSharp.Rect cropRect = Cv2.BoundingRect(largestContour);

                            // Add a 15-pixel margin around the shape
                            cropRect.Inflate(15, 15);

                            // Safety check: Ensure the crop box doesn't go outside the image boundaries
                            cropRect.Intersect(new OpenCvSharp.Rect(0, 0, printCanvas.Width, printCanvas.Height));

                            // Crop the canvas and save it specifically for the label printer/report
                            using (Mat croppedForPrint = new Mat(printCanvas, cropRect))
                            {
                                croppedForPrint.ImWrite("ShapeForLabel.png");
                            }
                        }

                        src.ImWrite("Circle.png");

                        //this.Invoke((MethodInvoker)delegate
                        //{
                            return $"Diameter : {realSize:F2} mm";
                        //});
                        //return;
                    }
                }

                //this.Invoke((MethodInvoker)delegate
                //{
                    return $"Diameter Not Detected";
                //});
            }
        }

        public void UpdateMeasurementUI(string resultText)
        {
            if (string.IsNullOrWhiteSpace(resultText)) return;

            // Extract all numbers (including decimals) from the returned string
            var numbers = System.Text.RegularExpressions.Regex.Matches(resultText, @"[\d\.]+");

            // ---------------------------------------------------------
            // SCENARIO 1: ROUND SHAPE (DIAMETER)
            // ---------------------------------------------------------
            // Using .ToLower() directly in the condition check
            if (resultText.ToLower().Contains("diameter") && numbers.Count >= 1)
            {
                // Change the title to DIAMETER and show the value
                lblLengthTitle.Text = "DIAMETER";
                lblLengthVal.Text = numbers[0].Value;

                // Hide the Width and Ratio labels since they don't apply to a perfect round shape
                lblWidthTitle.Visible = false;
                lblWidthVal.Visible = false;
                lblRatioTitle.Visible = false;
                lblRatioVal.Visible = false;
            }
            // ---------------------------------------------------------
            // SCENARIO 2: STANDARD SHAPE (LENGTH & WIDTH)
            // ---------------------------------------------------------
            else if (resultText.ToLower().Contains("length") && resultText.ToLower().Contains("width") && numbers.Count >= 2)
            {
                // Restore standard titles and visibility
                lblLengthTitle.Text = "LENGTH";
                lblWidthTitle.Visible = true;
                lblWidthVal.Visible = true;
                lblRatioTitle.Visible = true;
                lblRatioVal.Visible = true;

                string lengthStr = numbers[0].Value;
                string widthStr = numbers[1].Value;

                lblLengthVal.Text = lengthStr;
                lblWidthVal.Text = widthStr;

                // Calculate and display the ratio
                if (double.TryParse(lengthStr, out double len) && double.TryParse(widthStr, out double wid) && wid > 0)
                {
                    double ratio = len / wid;
                    lblRatioVal.Text = ratio.ToString("F2");
                }
                else
                {
                    lblRatioVal.Text = "0.00";
                }
            }
            // ---------------------------------------------------------
            // SCENARIO 3: ERRORS OR NO SHAPE DETECTED
            // ---------------------------------------------------------
            else
            {
                lblLengthTitle.Text = "STATUS";
                lblLengthVal.Text = "--";

                // Hide other labels to keep the UI clean during an error
                lblWidthTitle.Visible = false;
                lblWidthVal.Visible = false;
                lblRatioTitle.Visible = false;
                lblRatioVal.Visible = false;
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void btnMin_Click(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Minimized;
        }
        
        private void panel3_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
            }
        }

        private void btnBlank_Click(object sender, EventArgs e)
        {
            captureFlag = true;
        }

        private void FrmAuto_Resize(object sender, EventArgs e)
        {
            UpdatePictureBoxAspectRatio();
        }

        private void btnResetCounters_Click(object sender, EventArgs e)
        {
            resetCounts();
        }

        public void resetCounts()
        {
            lock (counterLock)
            {
                lightBlinkCounters.Clear();
                if (File.Exists(storageFilePath))
                {
                    File.WriteAllText(storageFilePath, "{}");
                }
            }
        }
    }
}
