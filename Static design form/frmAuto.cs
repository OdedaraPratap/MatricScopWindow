using MvCamCtrl.NET;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using OpenCvSharp.ML;
using System;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;
using uEye;
using uEye.Defines;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace opencvsharp
{
    public partial class FrmAuto : Form
    {
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern bool ReleaseCapture();

        public const int WM_NCLBUTTONDOWN = 0xA1;
        public const int HT_CAPTION = 0x2;

        enum MeasurementMode { None, Round, Pear, Heart, Marquise, Poly,Emerald }
        MeasurementMode currentMode = MeasurementMode.None;
        public MyCamera.cbOutputExdelegate ImageCallback;
        Object mBufferDriverLock = new Object();
        uint m_BuffersizeForDriver = 0;
        public MyCamera device;
        ColorPalette cp;
        private Mat backgroundGray = null;
        // State management variables
        private bool isObjectPresent = false;
        private bool hasMeasuredCurrentObject = false;
        // Motion and Position Tracking
        private OpenCvSharp.Point lastCentroid = new OpenCvSharp.Point(0, 0);
        private int stableFrameCount = 0;
        private const int FRAMES_TO_STABILIZE = 8; // Wait for ~300ms of absolute stillness before measuring
        private const double MOVEMENT_THRESHOLD = 9.0; // Sensitivity: how many pixels the object can shift before it's considered "moving"
        private int thresholdValue = 25;
        private Stopwatch stopWatch = System.Diagnostics.Stopwatch.StartNew();
        private uEye.Camera Camera;
        public static bool calibclick = false;
        ModifyRegistry mr = new ModifyRegistry();
        private SerialPort arduinoPort;
        private bool isSerialConnected = false;
        private Mat liveMat = new Mat();
        private Mat lastProcessedFrame = new Mat();
        private Mat motionAnalysisMat = new Mat(); 
        private readonly object frameLock = new object();
        private DataTable dtRules;
        private readonly string xmlFilePath = Path.Combine(Application.StartupPath, "DiamondRules.xml");
        bool captureFlag = false;
        private bool isRenderingSnapshot = false;
        
        public FrmAuto()
        {
            InitializeComponent();
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
                MessageBox.Show($"Could not connect to Arduino hardware: {ex.Message}", "Hardware Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
        
        private void LoadRulesDatabase()
        {
            dtRules = new DataTable("Rule");

            // Define the identical layout schema so it can read the XML perfectly
            dtRules.Columns.Add("Number", typeof(int));
            //dtRules.Columns.Add("BoxNumber", typeof(string));
            dtRules.Columns.Add("ShapeType", typeof(string));
            dtRules.Columns.Add("FromLength", typeof(double));
            dtRules.Columns.Add("ToLength", typeof(double));
            dtRules.Columns.Add("FromWidth", typeof(double));
            dtRules.Columns.Add("ToWidth", typeof(double));

            dtRules.PrimaryKey = new DataColumn[] { dtRules.Columns["ShapeType"], dtRules.Columns["Number"] };

            // Read the XML file populated by your other form
            if (File.Exists(xmlFilePath))
            {
                try
                {
                    dtRules.ReadXml(xmlFilePath);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Detection Form failed to load database: {ex.Message}", "Data Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            InitializeArduinoConnection();
            LoadRulesDatabase();
           
            /*Mat pearmat = Cv2.ImRead(@"C:\Users\Administrator\source\repos\opencvsharp\bin\x64\Debug\HalfMoon.png");
            MeasurePearShape mesure = new MeasurePearShape();
            mesure.MeasurePearWithOpenCvSharp(pearmat, this);*/
            //MeasureHalfMoonWithOpenCvSharp(pearmat);
            /*Bitmap bcpy = CreateNonIndexedImage(new Bitmap(@"C:\Users\Administrator\MVS\Data\SQUAre.bmp"));
            Mat b = BitmapConverter.ToMat(bcpy);
            PolygonMeasurement.DetectAndMeasurePolygon(b, this);*/
            //Bitmap bmp = new Bitmap("Image_20260526172635953.png");
            //doMesure(bmp);
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

        private void onFrameEventcontinous(object sender, EventArgs e)
        {
            // Throttle frame captures to every 50ms (~20 FPS)
            if (!stopWatch.IsRunning || stopWatch.ElapsedMilliseconds > 50)
            {
                stopWatch.Reset();

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

                    if (calibclick)
                    {
                        lock (frameLock)
                        {
                            using (Bitmap calibBmp = BitmapConverter.ToBitmap(liveMat)) { docalib(calibBmp); }
                        }
                        calibclick = false;
                    }

                    // ==========================================================
                    // PERFORMANCE ENGINE: ASYNCHRONOUS MOTION DETECTION LOOP
                    // ==========================================================
                    if (backgroundGray != null && currentMode != MeasurementMode.None)
                    {
                        Mat workingCopy = new Mat();
                        lock (frameLock)
                        {
                            liveMat.CopyTo(workingCopy);
                        }

                        System.Threading.Tasks.Task.Run(() =>
                        {
                            try
                            {
                                using (workingCopy)
                                using (Mat liveGray = new Mat())
                                {
                                    Cv2.CvtColor(workingCopy, liveGray, ColorConversionCodes.BGR2GRAY);
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

                                                // TWEEZER SHIELD FILTER
                                                if (aspect > 0.22 && aspect < 4.5 && contourArea > 800)
                                                {
                                                    validDiamondDetected = true;
                                                    Moments mu = Cv2.Moments(largestContour, binaryImage: true);
                                                    if (mu.M00 > 0)
                                                    {
                                                        int cX = (int)(mu.M10 / mu.M00);
                                                        int cY = (int)(mu.M01 / mu.M00);
                                                        currentCentroid = new OpenCvSharp.Point(cX, cY);
                                                    }
                                                }
                                            }

                                            if (validDiamondDetected)
                                            {
                                                isObjectPresent = true;

                                                double distanceMoved = Math.Sqrt(Math.Pow(currentCentroid.X - lastCentroid.X, 2) + Math.Pow(currentCentroid.Y - lastCentroid.Y, 2));

                                                if (distanceMoved > MOVEMENT_THRESHOLD)
                                                {
                                                    hasMeasuredCurrentObject = false;
                                                    stableFrameCount = 0;
                                                    this.BeginInvoke((MethodInvoker)delegate { label1.Text = "Object moving..."; });
                                                }
                                                else
                                                {
                                                    // Object is resting in place
                                                    if (!hasMeasuredCurrentObject)
                                                    {
                                                        stableFrameCount++;
                                                        if (stableFrameCount >= FRAMES_TO_STABILIZE)
                                                        {
                                                            hasMeasuredCurrentObject = true;

                                                            // Trigger measurement and Arduino routines once
                                                            this.BeginInvoke((MethodInvoker)delegate
                                                            {
                                                                lock (frameLock)
                                                                {
                                                                    if (liveMat != null && !liveMat.IsDisposed && !liveMat.Empty())
                                                                    {
                                                                        TriggerAutoMeasurement(liveMat);
                                                                    }
                                                                }
                                                            });
                                                        }
                                                    }
                                                }
                                                lastCentroid = currentCentroid;
                                            }
                                            else
                                            {
                                                ResetTrackingState();
                                            }
                                        }
                                        else
                                        {
                                            ResetTrackingState();
                                        }
                                    }
                                }
                            }
                            catch (Exception) { }
                        });
                    }

                    // ==========================================================
                    // UNIFIED DISPLAY RENDERING PIPELINE (CONTINUOUS LIVE FEED)
                    // ==========================================================
                    this.BeginInvoke((MethodInvoker)delegate
                    {
                        try
                        {
                            if (pictureBox1.Image != null) pictureBox1.Image.Dispose();

                            lock (frameLock)
                            {
                                if (liveMat != null && !liveMat.IsDisposed && !liveMat.Empty())
                                {
                                    // If the object is fully stable and measured, continuously re-draw 
                                    // the overlays onto the live frames so the video never freezes
                                    if (isObjectPresent && hasMeasuredCurrentObject)
                                    {
                                        TriggerAutoMeasurement(liveMat);
                                    }

                                    pictureBox1.Image = BitmapConverter.ToBitmap(liveMat);
                                }
                            }
                        }
                        catch (ObjectDisposedException) { }
                    });
                }
                catch (Exception) { }
            }
        }
   
        private void onFrameEventWorking(object sender, EventArgs e)
        {
            // Throttle frame captures to every 50ms (~20 FPS)
            if (!stopWatch.IsRunning || stopWatch.ElapsedMilliseconds > 50)
            {
                stopWatch.Reset();

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

                    // Safe scoping layout: automatically disposes temporary Mat reference wrapper
                    using (Mat rawCamFrame = BitmapConverter.ToMat(bcpy))
                    {
                        // Synchronize frame modification securely using the lock token
                        lock (frameLock)
                        {
                            if (liveMat == null || liveMat.IsDisposed) liveMat = new Mat();
                            rawCamFrame.CopyTo(liveMat);
                        }
                    }

                    // Explicitly clean up unmanaged bitmap handles to prevent RAM leaks
                    bmp.Dispose();
                    bcpy.Dispose();

                    if (calibclick)
                    {
                        lock (frameLock)
                        {
                            using (Bitmap calibBmp = BitmapConverter.ToBitmap(liveMat)) { docalib(calibBmp); }
                        }
                        calibclick = false;
                    }

                    // ==========================================================
                    // PERFORMANCE ENGINE: ASYNCHRONOUS MOTION DETECTION LOOP
                    // ==========================================================
                    if (backgroundGray != null && currentMode != MeasurementMode.None)
                    {
                        // Create an isolated matrix clone for background calculations to prevent frame lag
                        Mat workingCopy = new Mat();
                        lock (frameLock)
                        {
                            liveMat.CopyTo(workingCopy);
                        }

                        System.Threading.Tasks.Task.Run(() =>
                        {
                            try
                            {
                                using (workingCopy)
                                using (Mat liveGray = new Mat())
                                {
                                    Cv2.CvtColor(workingCopy, liveGray, ColorConversionCodes.BGR2GRAY);
                                    Cv2.MedianBlur(liveGray, liveGray, 7);

                                    using (Mat diff = new Mat())
                                    using (Mat thresh = new Mat())
                                    {
                                        Cv2.Absdiff(backgroundGray, liveGray, diff);
                                        Cv2.Threshold(diff, thresh, thresholdValue, 255, ThresholdTypes.Binary);

                                        int changedPixels = Cv2.CountNonZero(thresh);

                                        // Scenario A: An object footprint is detected on the platform stage area
                                        if (changedPixels > 1250)
                                        {
                                            OpenCvSharp.Point[][] contours;
                                            HierarchyIndex[] hierarchy;
                                            Cv2.FindContours(thresh, out contours, out hierarchy, RetrievalModes.External, ContourApproximationModes.ApproxSimple);

                                            bool validDiamondDetected = false;
                                            OpenCvSharp.Point currentCentroid = new OpenCvSharp.Point(0, 0);

                                            if (contours.Length > 0)
                                            {
                                                // Isolate the single largest silhouette contour on stage
                                                var largestContour = contours.OrderByDescending(c => Cv2.ContourArea(c)).First();
                                                double contourArea = Cv2.ContourArea(largestContour);
                                                Rect boundingBox = Cv2.BoundingRect(largestContour);

                                                // Calculate Aspect Ratio (Width / Height balance ratio)
                                                double aspect = (double)boundingBox.Width / boundingBox.Height;

                                                // ==========================================================
                                                // TWEEZER SHIELD EXCLUSION FILTERS
                                                // ==========================================================
                                                // Tweezers are thin/stretched (unbalanced aspect ratio) and produce low density masks
                                                if (aspect > 0.22 && aspect < 4.5 && contourArea > 800)
                                                {
                                                    validDiamondDetected = true;
                                                    Moments mu = Cv2.Moments(largestContour, binaryImage: true);
                                                    if (mu.M00 > 0)
                                                    {
                                                        int cX = (int)(mu.M10 / mu.M00);
                                                        int cY = (int)(mu.M01 / mu.M00);
                                                        currentCentroid = new OpenCvSharp.Point(cX, cY);
                                                    }
                                                }
                                            }

                                            if (validDiamondDetected)
                                            {
                                                isObjectPresent = true;

                                                // Check structural movement delta distance since last frame loop cycle
                                                double distanceMoved = Math.Sqrt(Math.Pow(currentCentroid.X - lastCentroid.X, 2) + Math.Pow(currentCentroid.Y - lastCentroid.Y, 2));

                                                if (distanceMoved > MOVEMENT_THRESHOLD)
                                                {
                                                    hasMeasuredCurrentObject = false;
                                                    stableFrameCount = 0;
                                                    this.BeginInvoke((MethodInvoker)delegate { label1.Text = "Object moving..."; });
                                                }
                                                else
                                                {
                                                    // Object is sitting completely resting in place
                                                    if (!hasMeasuredCurrentObject)
                                                    {
                                                        stableFrameCount++;
                                                        if (stableFrameCount >= FRAMES_TO_STABILIZE)
                                                        {
                                                            hasMeasuredCurrentObject = true;

                                                            // FIX: Move measurement and snapshot processing directly to the UI thread
                                                            // to guarantee zero memory access crashes on lastProcessedFrame!
                                                            this.BeginInvoke((MethodInvoker)delegate
                                                            {
                                                                lock (frameLock)
                                                                {
                                                                    if (liveMat != null && !liveMat.IsDisposed && !liveMat.Empty())
                                                                    {
                                                                        // 1. Run the shape measurement and draw vectors/lines
                                                                        TriggerAutoMeasurement(liveMat);

                                                                        // 2. Lock freeze snapshot immediately on the same execution thread
                                                                        if (lastProcessedFrame == null || lastProcessedFrame.IsDisposed)
                                                                            lastProcessedFrame = new Mat();

                                                                        liveMat.CopyTo(lastProcessedFrame);
                                                                    }
                                                                }
                                                            });
                                                        }
                                                    }
                                                }
                                                lastCentroid = currentCentroid;
                                            }
                                            else
                                            {
                                                // Something is on stage but it failed the diamond dimensional criteria (tweezers!)
                                                ResetTrackingState();
                                            }
                                        }
                                        // Scenario B: Stage is completely clear / empty space
                                        else
                                        {
                                            ResetTrackingState();
                                        }
                                    }
                                }
                            }
                            catch (Exception) { }
                        });
                    }

                    // ==========================================================
                    // UNIFIED DISPLAY RENDERING PIPELINE (STABLE UI RUNTIME)
                    // ==========================================================
                    /*this.BeginInvoke((MethodInvoker)delegate
                    {
                        try
                        {
                            if (pictureBox1.Image != null) pictureBox1.Image.Dispose();

                            lock (frameLock)
                            {
                                // Render the locked processed snapshot matrix canvas if the stone is measured
                                if (isObjectPresent && hasMeasuredCurrentObject && lastProcessedFrame != null && !lastProcessedFrame.IsDisposed && !lastProcessedFrame.Empty())
                                {
                                    pictureBox1.Image = BitmapConverter.ToBitmap(lastProcessedFrame);
                                }
                                else
                                {
                                    // Otherwise, pass through the raw streaming camera live views feed
                                    if (liveMat != null && !liveMat.IsDisposed && !liveMat.Empty())
                                    {
                                        pictureBox1.Image = BitmapConverter.ToBitmap(liveMat);
                                    }
                                }
                            }
                        }
                        catch (ObjectDisposedException) { }
                    });*/
                    Bitmap bitmapToRender = null;

                    lock (frameLock)
                    {
                        try
                        {
                            // 1. If the stone is measured, snapshot the processed canvas
                            if (isObjectPresent && hasMeasuredCurrentObject && lastProcessedFrame != null && !lastProcessedFrame.IsDisposed && !lastProcessedFrame.Empty())
                            {
                                bitmapToRender = BitmapConverter.ToBitmap(lastProcessedFrame);
                            }
                            // 2. Otherwise, pass through the raw streaming camera live view
                            else if (liveMat != null && !liveMat.IsDisposed && !liveMat.Empty())
                            {
                                bitmapToRender = BitmapConverter.ToBitmap(liveMat);
                            }
                        }
                        catch (Exception)
                        {
                            // Safe fallback if memory was busy
                            bitmapToRender = null;
                        }
                    }

                    // 3. Send the pre-converted, safe .NET Bitmap directly to the UI thread
                    if (bitmapToRender != null)
                    {
                        this.BeginInvoke((MethodInvoker)delegate
                        {
                            try
                            {
                                // Clean up the old picturebox image to prevent memory leaks
                                if (pictureBox1.Image != null)
                                {
                                    pictureBox1.Image.Dispose();
                                }

                                // Assign the new bitmap instantly without needing ANY locks here
                                pictureBox1.Image = bitmapToRender;
                            }
                            catch (ObjectDisposedException) { }
                            catch (Exception)
                            {
                                // If anything goes wrong, dispose the dangling bitmap allocation
                                bitmapToRender.Dispose();
                            }
                        });
                    }
                }
                catch (Exception) { }
            }
        }

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
                                                    this.BeginInvoke((MethodInvoker)delegate { label1.Text = "Object moving..."; });
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
                                pictureBox1.Image = BitmapConverter.ToBitmap(lastProcessedFrame);
                                oldBmp?.Dispose();
                            }
                            // 2. Otherwise, stream the zero-lag live feed unmodified
                            else if (liveMat != null && !liveMat.IsDisposed && !liveMat.Empty())
                            {
                                Bitmap oldBmp = pictureBox1.Image as Bitmap;
                                pictureBox1.Image = BitmapConverter.ToBitmap(liveMat);
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

            this.BeginInvoke((MethodInvoker)delegate { label1.Text = "Waiting for object..."; });
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
            this.BeginInvoke((MethodInvoker)delegate { label1.Text = "Waiting for object..."; });
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
            // Open Camera
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
            statusRet = Camera.Acquisition.Capture(DeviceParameter.Wait);
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
            uEye.Defines.Status statusRet1;
            uEye.Types.Range<Double> range1;
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

        /*private void onFrameEventOLDWithoutDrawing(object sender, EventArgs e)
        {
            // Throttle frame captures to every 50ms (~20 FPS)
            if (!stopWatch.IsRunning || stopWatch.ElapsedMilliseconds > 50)
            {
                stopWatch.Reset();

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

                    // Safe scoping layout: automatically disposes temporary Mat reference wrapper
                    using (Mat rawCamFrame = BitmapConverter.ToMat(bcpy))
                    {
                        // Synchronize frame modification securely using the lock token
                        lock (frameLock)
                        {
                            if (liveMat == null || liveMat.IsDisposed) liveMat = new Mat();
                            rawCamFrame.CopyTo(liveMat);
                        }
                    }

                    // Explicitly clean up unmanaged bitmap handles to prevent RAM leaks
                    bmp.Dispose();
                    bcpy.Dispose();

                    if (calibclick)
                    {
                        lock (frameLock)
                        {
                            using (Bitmap calibBmp = BitmapConverter.ToBitmap(liveMat)) { docalib(calibBmp); }
                        }
                        calibclick = false;
                    }

                    // ---- AUTO DETECTION & MOTION LOGIC ----
                    if (backgroundGray != null && currentMode != MeasurementMode.None)
                    {
                        using (Mat liveGray = new Mat())
                        {
                            lock (frameLock)
                            {
                                Cv2.CvtColor(liveMat, liveGray, ColorConversionCodes.BGR2GRAY);
                            }
                            Cv2.MedianBlur(liveGray, liveGray, 7);

                            using (Mat diff = new Mat())
                            using (Mat thresh = new Mat())
                            {
                                Cv2.Absdiff(backgroundGray, liveGray, diff);
                                Cv2.Threshold(diff, thresh, thresholdValue, 255, ThresholdTypes.Binary);

                                int changedPixels = Cv2.CountNonZero(thresh);

                                // Scenario A: An object is present on the stage
                                if (changedPixels > 1250)
                                {
                                    isObjectPresent = true;

                                    Moments mu = Cv2.Moments(thresh, binaryImage: true);
                                    if (mu.M00 > 0)
                                    {
                                        int cX = (int)(mu.M10 / mu.M00);
                                        int cY = (int)(mu.M01 / mu.M00);
                                        OpenCvSharp.Point currentCentroid = new OpenCvSharp.Point(cX, cY);

                                        double distanceMoved = Math.Sqrt(Math.Pow(currentCentroid.X - lastCentroid.X, 2) +
                                                                         Math.Pow(currentCentroid.Y - lastCentroid.Y, 2));

                                        if (distanceMoved > MOVEMENT_THRESHOLD)
                                        {
                                            // Object is moving! Reset tracking states
                                            hasMeasuredCurrentObject = false;
                                            stableFrameCount = 0;

                                            this.BeginInvoke((MethodInvoker)delegate { label1.Text = "Object moving..."; });
                                        }
                                        else
                                        {
                                            // Object is completely resting in place
                                            if (!hasMeasuredCurrentObject)
                                            {
                                                stableFrameCount++;
                                                if (stableFrameCount >= FRAMES_TO_STABILIZE)
                                                {
                                                    hasMeasuredCurrentObject = true;

                                                    // 1. Run measurement logic synchronously (draws shapes straight onto liveMat)
                                                    TriggerAutoMeasurement(liveMat);


                                                    

                                                    // 2. LOCK SNAPSHOT: Safely copy the annotated live matrix into the persistent display memory
                                                    lock (frameLock)
                                                    {
                                                        if (lastProcessedFrame == null || lastProcessedFrame.IsDisposed)
                                                            lastProcessedFrame = new Mat();

                                                        liveMat.CopyTo(lastProcessedFrame);
                                                    }
                                                }
                                            }
                                        }
                                        lastCentroid = currentCentroid;
                                    }
                                }
                                // Scenario B: Stage is completely cleared / object removed
                                else
                                {
                                    stableFrameCount = 0;
                                    isObjectPresent = false;
                                    hasMeasuredCurrentObject = false;
                                    lastCentroid = new OpenCvSharp.Point(0, 0);

                                    // FIX: Blank out the matrix state memory safely without invoking crash-prone .Release() disposals
                                    lock (frameLock)
                                    {
                                        if (lastProcessedFrame != null && !lastProcessedFrame.IsDisposed && !lastProcessedFrame.Empty())
                                        {
                                            lastProcessedFrame.SetTo(new Scalar(0));
                                        }
                                    }

                                    this.BeginInvoke((MethodInvoker)delegate { label1.Text = "Waiting for object..."; });
                                }
                            }
                        }
                    }
                    // ==========================================================
                    // UNIFIED DISPLAY RENDERING PIPELINE (THREAD-SAFE FIXED)
                    // ==========================================================
                    this.BeginInvoke((MethodInvoker)delegate
                    {
                        try
                        {
                            if (pictureBox1.Image != null) pictureBox1.Image.Dispose();

                            // Lock the evaluation process completely so the tracking blocks can't touch memory structures simultaneously
                            lock (frameLock)
                            {
                                if (isObjectPresent && hasMeasuredCurrentObject && lastProcessedFrame != null && !lastProcessedFrame.IsDisposed && !lastProcessedFrame.Empty())
                                {
                                    pictureBox1.Image = BitmapConverter.ToBitmap(lastProcessedFrame);
                                }
                                else
                                {
                                    if (liveMat != null && !liveMat.IsDisposed && !liveMat.Empty())
                                    {
                                        pictureBox1.Image = BitmapConverter.ToBitmap(liveMat);
                                    }
                                }
                            }
                        }
                        catch (ObjectDisposedException)
                        {
                            // Fail-safe protection layer for clean application teardowns
                        }
                    });
                }
                catch (Exception) { }
            }
        }
        */
        
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
                        // If doMesure updates labels natively, you should ideally modify it to return a string.
                        // For now, if forced onto the UI thread, wrap it minimally:
                        this.Invoke((MethodInvoker)delegate { doMesure(frame); cvDisplayString = label1.Text; });
                        break;

                    case MeasurementMode.Pear:
                        cvDisplayString = new MeasurePearShape().MeasurePearWithOpenCvSharp(frame);
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
                    case MeasurementMode.Emerald:
                        cvDisplayString = new MeasurePearShape().MeasurePearWithOpenCvSharp1(frame);
                        break;
                }

                // Safely dispatch calculated text results back to UI
                this.BeginInvoke((MethodInvoker)delegate
                {
                    if (!string.IsNullOrEmpty(cvDisplayString)) label1.Text = cvDisplayString;
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
                            if (extractedLength >= fromLen && extractedLength <= toLen)
                            {
                                matchedSortingRule = row;
                                break;
                            }
                        }
                        else
                        {
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

                    if (isSerialConnected && arduinoPort != null && arduinoPort.IsOpen)
                    {
                        // FIX: WriteLine adds a standard '\n' termination char so Arduino knows when transmission ends
                        arduinoPort.WriteLine(targetSquareNumber.ToString()+"\n");
                    }
                }
            }
            catch (Exception) { }
        }

        private void TriggerAutoMeasurement1(Mat frame)
        {
            switch (currentMode)
            {
                case MeasurementMode.Round:
                    // Converts frame to bitmap internally inside your method
                    //using (Bitmap bmpConversion = BitmapConverter.ToBitmap(frame))
                    {
                        doMesure(frame);
                    }
                    break;

                case MeasurementMode.Pear:
                    string pearmesure = new MeasurePearShape().MeasurePearWithOpenCvSharp(frame);
                    this.Invoke((MethodInvoker)delegate
                    {
                        label1.Text = pearmesure;
                    });
                    break;
                
                case MeasurementMode.Heart:
                    string heart = new MeasurePearShape().MeasureHeartWithOpenCvSharp(frame);
                    this.Invoke((MethodInvoker)delegate
                    {
                        label1.Text = heart;
                    });
                    break;

                case MeasurementMode.Marquise:
                    string marq = new MeasurePearShape().MeasureMarkWithOpenCvSharp(frame);
                    this.Invoke((MethodInvoker)delegate
                    {
                        label1.Text = marq;
                    });
                    break;

                case MeasurementMode.Poly:

                    string polyMesure = PolygonMeasurement.DetectAndMeasurePolygon(frame);
                    this.Invoke((MethodInvoker)delegate
                    {
                        label1.Text = polyMesure;
                    });
                    break;
                case MeasurementMode.Emerald:
                    string emmesure = new MeasurePearShape().MeasurePearWithOpenCvSharp1(frame);
                    this.Invoke((MethodInvoker)delegate
                    {
                        label1.Text = emmesure;
                    });
                    break;
                    /*case MeasurementMode.HM:

                        string hmMesure = MeasureHalfMoonWithOpenCvSharp(frame);
                        this.Invoke((MethodInvoker)delegate
                        {
                            label1.Text = hmMesure;
                        });
                        break;*/
            }
        }

        private void TriggerAutoMeasurementarduino(Mat frame)
        {
            try
            {
                string cvDisplayString = "";

                // ==========================================
                // 1. RUN YOUR EXISTING DETECTION METHODS
                // ==========================================
                switch (currentMode)
                {
                    case MeasurementMode.Round:
                        //using (Bitmap bmpConversion = BitmapConverter.ToBitmap(frame))
                        {
                            doMesure(frame);
                        }
                        // Since doMesure assigns text directly to label1, we read it back
                        this.Invoke((MethodInvoker)delegate
                        {
                            cvDisplayString = label1.Text;
                        });
                        break;

                    case MeasurementMode.Pear:
                        cvDisplayString = new MeasurePearShape().MeasurePearWithOpenCvSharp(frame);
                        this.Invoke((MethodInvoker)delegate
                        {
                            label1.Text = cvDisplayString;
                        });
                        break;
                    case MeasurementMode.Heart:
                        cvDisplayString = new MeasurePearShape().MeasureHeartWithOpenCvSharp(frame);
                        this.Invoke((MethodInvoker)delegate
                        {
                            label1.Text = cvDisplayString;
                        });
                        break;

                    case MeasurementMode.Marquise:
                        cvDisplayString = new MeasurePearShape().MeasureMarkWithOpenCvSharp(frame);
                        this.Invoke((MethodInvoker)delegate
                        {
                            label1.Text = cvDisplayString;
                        });
                        break;
                    case MeasurementMode.Poly:
                        cvDisplayString = PolygonMeasurement.DetectAndMeasurePolygon(frame);
                        this.Invoke((MethodInvoker)delegate
                        {
                            label1.Text = cvDisplayString;
                        });
                        break;
                    case MeasurementMode.Emerald:
                        cvDisplayString = new MeasurePearShape().MeasurePearWithOpenCvSharp1(frame);
                        this.Invoke((MethodInvoker)delegate
                        {
                            label1.Text = cvDisplayString;
                        });
                        break;

                        /*case MeasurementMode.HM:
                            cvDisplayString = MeasureHalfMoonWithOpenCvSharp(frame);
                            this.Invoke((MethodInvoker)delegate
                            {
                                label1.Text = hmMesure;
                            });
                            break;*/
                }

                // Integrity Check: Stop if the string is empty or contains calibration warnings
                if (string.IsNullOrEmpty(cvDisplayString) ||
                    cvDisplayString.Contains("Error") ||
                    cvDisplayString.Contains("Please Calibrate")|| cvDisplayString.ToLower().Contains("object")) return;

                double extractedLength = 0.0;
                double extractedWidth = 0.0;

                // ==========================================
                // 2. CENTRAL STRING DECODER ENGINE
                // ==========================================
                if (currentMode == MeasurementMode.Round)
                {
                    // Decodes standard format: "Diameter : 4.970 mm"
                    string[] parts = cvDisplayString.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length >= 3)
                    {
                        double.TryParse(parts[2], out extractedLength);
                    }
                }
                else if (cvDisplayString.Contains("Side"))
                {
                    // Decodes Poly: Find all side measurements and isolate the longest (Length) and shortest (Width)
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
                    // Decodes Pear / HalfMoon multi-line standard formats: Extracts numbers sequentially
                    MatchCollection numbers = Regex.Matches(cvDisplayString, @"[0-9]+(?:\.[0-9]+)?");
                    if (numbers.Count >= 1) double.TryParse(numbers[0].Value, out extractedLength);
                    if (numbers.Count >= 2) double.TryParse(numbers[1].Value, out extractedWidth);
                }

                // ==========================================
                // 3. DATATABLE RULE MATCHING ENGINE
                // ==========================================
                DataRow matchedSortingRule = null;
                string currentShapeNameString = currentMode.ToString(); // "Round", "Pear", "Poly", "HM"

                foreach (DataRow row in dtRules.Rows)
                {
                    // Match the shape column type first
                    if (row["ShapeType"].ToString() == currentShapeNameString)
                    {
                        double fromLen = Convert.ToDouble(row["FromLength"]);
                        double toLen = Convert.ToDouble(row["ToLength"]);
                        double fromWid = Convert.ToDouble(row["FromWidth"]);
                        double toWid = Convert.ToDouble(row["ToWidth"]);

                        if (currentMode == MeasurementMode.Round)
                        {
                            if (extractedLength >= fromLen && extractedLength <= toLen)
                            {
                                matchedSortingRule = row;
                                break;
                            }
                        }
                        else
                        {
                            if (extractedLength >= fromLen && extractedLength <= toLen &&
                                extractedWidth >= fromWid && extractedWidth <= toWid)
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
                    // Dispatch command data to Arduino
                    if (isSerialConnected && arduinoPort != null && arduinoPort.IsOpen)
                    {
                        arduinoPort.Write(targetSquareNumber.ToString()); // Send 1 byte containing the square index (1-20)
                    }
                }
            }
            catch (Exception)
            {
                // Thread collision barrier protection
            }
            finally
            {
                // Safe disposal of raw source frame pointer copies
                /*if (frame != null && !frame.IsDisposed)
                {
                    frame.Dispose();
                }*/
            }
        }
        
        private void btnCalib_Click(object sender, EventArgs e)
        {
            new FrmCalib().Show();
        }

        private void btnRound_Click(object sender, EventArgs e)
        {
            //roundClick = true;
            currentMode = MeasurementMode.Round;
            label1.Text = "Mode: Auto Round Measurement";
            
            btnRound.BackgroundImage = Properties.Resources.ROUND_Select;
            btnPear.BackgroundImage = Properties.Resources.PEAR;
            btnHeart.BackgroundImage = Properties.Resources.HEART;
            btnOvel.BackgroundImage = Properties.Resources.OVAL;
            btnmarqu.BackgroundImage = Properties.Resources.MARQUISE;
            btnPoly.BackgroundImage = Properties.Resources.POLYGON;
            btnEM.BackgroundImage = Properties.Resources.EMERALD;
        }

        private void btnPear_Click(object sender, EventArgs e)
        {
            currentMode = MeasurementMode.Pear;
            label1.Text = "Mode: Auto Pear Measurement";

            btnRound.BackgroundImage = Properties.Resources.ROUND;
            btnPear.BackgroundImage = Properties.Resources.PEAR_Select;
            btnHeart.BackgroundImage = Properties.Resources.HEART;
            btnOvel.BackgroundImage = Properties.Resources.OVAL;
            btnmarqu.BackgroundImage = Properties.Resources.MARQUISE;
            btnPoly.BackgroundImage = Properties.Resources.POLYGON;
            btnEM.BackgroundImage = Properties.Resources.EMERALD;
        }

        private void btnHeart_Click(object sender, EventArgs e)
        {
            currentMode = MeasurementMode.Heart;
            label1.Text = "Mode: Auto Heart Measurement";

            btnRound.BackgroundImage = Properties.Resources.ROUND;
            btnPear.BackgroundImage = Properties.Resources.PEAR;
            btnHeart.BackgroundImage = Properties.Resources.HEART_Select;
            btnOvel.BackgroundImage = Properties.Resources.OVAL;
            btnmarqu.BackgroundImage = Properties.Resources.MARQUISE;
            btnPoly.BackgroundImage = Properties.Resources.POLYGON;
            btnEM.BackgroundImage = Properties.Resources.EMERALD;
        }

        private void btnOvel_Click(object sender, EventArgs e)
        {
            currentMode = MeasurementMode.Pear;
            label1.Text = "Mode: Auto Ovel Measurement";

            btnRound.BackgroundImage = Properties.Resources.ROUND;
            btnPear.BackgroundImage = Properties.Resources.PEAR;
            btnHeart.BackgroundImage = Properties.Resources.HEART;
            btnOvel.BackgroundImage = Properties.Resources.OVAL_Select;
            btnmarqu.BackgroundImage = Properties.Resources.MARQUISE;
            btnPoly.BackgroundImage = Properties.Resources.POLYGON;
            btnEM.BackgroundImage = Properties.Resources.EMERALD;
        }

        private void btnPoly_Click(object sender, EventArgs e)
        {
            currentMode = MeasurementMode.Poly;
            label1.Text = "Mode: Auto Polygon Measurement";

            btnRound.BackgroundImage = Properties.Resources.ROUND;
            btnPear.BackgroundImage = Properties.Resources.PEAR;
            btnHeart.BackgroundImage = Properties.Resources.HEART;
            btnOvel.BackgroundImage = Properties.Resources.OVAL;
            btnmarqu.BackgroundImage = Properties.Resources.MARQUISE;
            btnPoly.BackgroundImage = Properties.Resources.POLYGON_Select;
            btnEM.BackgroundImage = Properties.Resources.EMERALD;
        }

        private void btnmarqu_Click(object sender, EventArgs e)
        {
            currentMode = MeasurementMode.Marquise;
            label1.Text = "Mode: Auto Marquise Measurement";

            btnRound.BackgroundImage = Properties.Resources.ROUND;
            btnPear.BackgroundImage = Properties.Resources.PEAR;
            btnHeart.BackgroundImage = Properties.Resources.HEART;
            btnOvel.BackgroundImage = Properties.Resources.OVAL;
            btnmarqu.BackgroundImage = Properties.Resources.MARQUISE_Select;
            btnPoly.BackgroundImage = Properties.Resources.POLYGON;
            btnEM.BackgroundImage = Properties.Resources.EMERALD;
        }

        private void btnEM_Click(object sender, EventArgs e)
        {
            currentMode = MeasurementMode.Emerald;
            label1.Text = "Mode: Auto Emrald Measurement";

            btnRound.BackgroundImage = Properties.Resources.ROUND;
            btnPear.BackgroundImage = Properties.Resources.PEAR;
            btnHeart.BackgroundImage = Properties.Resources.HEART;
            btnOvel.BackgroundImage = Properties.Resources.OVAL;
            btnmarqu.BackgroundImage = Properties.Resources.MARQUISE;
            btnPoly.BackgroundImage = Properties.Resources.POLYGON;
            btnEM.BackgroundImage = Properties.Resources.EMERALD_Select;
        }

        private void btnSettings_Click(object sender, EventArgs e)
        {
            new frmSettings(Camera).Show();
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            currentMode = MeasurementMode.None;
            label1.Text = "Auto Measurement Stopped.";

            btnRound.BackgroundImage = Properties.Resources.ROUND;
            btnPear.BackgroundImage = Properties.Resources.PEAR;
            btnHeart.BackgroundImage = Properties.Resources.HEART;
            btnOvel.BackgroundImage = Properties.Resources.OVAL;
            btnmarqu.BackgroundImage = Properties.Resources.MARQUISE;
            btnPoly.BackgroundImage = Properties.Resources.POLYGON;
            btnEM.BackgroundImage = Properties.Resources.EMERALD;
        }

        /*private void btnHm_Click(object sender, EventArgs e)
        {
            currentMode = MeasurementMode.HM;
            label1.Text = "Mode: Auto Half Moon Measurement";
        }*/

        private void btnTilt_Click(object sender, EventArgs e)
        {
            //showTiltCalibration = !showTiltCalibration;
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

        public void docalib(Bitmap bmp)
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
                    label1.Text = "Calibration complete";
                });
            }
            else
            {
                this.Invoke((MethodInvoker)delegate
                {
                    label1.Text = "Calibration Not Done";
                });
            }
        }

        public void doMesure(Mat src)
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
                minRadius: 100,
                maxRadius: 500
            );
            if (circles.Length > 0)
            {
                CircleSegment targetCircle = circles[0];
                double realSize = metrology.GetRealDiameter(targetCircle);
                // 2. DRAW SHAPES DIRECTLY ONTO THE INCOMING MAT
                // Draw green outer ring
                Cv2.Circle(src, (int)targetCircle.Center.X, (int)targetCircle.Center.Y, (int)targetCircle.Radius, Scalar.Lime, 1);
                // Draw red center point
                Cv2.Circle(src, (int)targetCircle.Center.X, (int)targetCircle.Center.Y, 5, Scalar.Red, -1);
                //src.SaveImage("round.png");
                OpenCvSharp.Point textPosition = new OpenCvSharp.Point((int)targetCircle.Center.X + 15, (int)targetCircle.Center.Y + 5);
                Cv2.PutText(src, $"{realSize:F3} mm",textPosition,HersheyFonts.HersheySimplex,0.6,Scalar.Yellow,2);
                src.ImWrite("Circle.png");
                this.Invoke((MethodInvoker)delegate
                {
                    label1.Text = $"Diameter : {realSize:F3} mm";
                });
            }
            else
            {
                this.Invoke((MethodInvoker)delegate
                {
                    label1.Text = $"Diameter Not Detected";
                });
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
    }
}
