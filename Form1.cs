using MvCamCtrl.NET;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace Matric_scope
{
    public partial class Form1 : Form
    {
        public MyCamera.cbOutputExdelegate ImageCallback;
        Object mBufferDriverLock = new Object();
        uint m_BuffersizeForDriver = 0;
        public MyCamera device;
        ColorPalette cp;

        private Stopwatch stopWatch = System.Diagnostics.Stopwatch.StartNew();

        bool calibclick = false;
        bool roundClick = false;
        bool pearClick = false;
        bool polyClick = false;
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            /*Mat pearmat = Cv2.ImRead(@"C:\Users\Administrator\MVS\Data\Marquise.bmp");
            MeasurePearShape mesure = new MeasurePearShape();
            mesure.MeasurePearWithOpenCvSharp(pearmat, this);

            Bitmap bcpy = CreateNonIndexedImage(new Bitmap(@"C:\Users\Administrator\MVS\Data\SQUAre.bmp"));
            Mat b = BitmapConverter.ToMat(bcpy);
            PolygonMeasurement.DetectAndMeasurePolygon(b, this);*/
            
            InitCameraMV();
        }

        public void InitCameraMV()
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
                catch (Exception)
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
                device.MV_CC_SetEnumValue_NET("ExposureAuto", 0);
                device.MV_CC_SetFloatValue_NET("ExposureTime",50000);
                //device.MV_CC_SetEnumValue_NET("GainAuto", 0);
                //device.MV_CC_SetFloatValue_NET("Gain", float.Parse(mr.Read("trackBarGainMaster1")));
            }
            catch (Exception exceptt)
            {
                MessageBox.Show("2 " + exceptt.ToString());
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

        void ImageCallBackFunc(IntPtr pData, ref MyCamera.MV_FRAME_OUT_INFO_EX pFrameInfo, IntPtr pUser)
        {
            if(!stopWatch.IsRunning || stopWatch.ElapsedMilliseconds > 50)
            {
                lock (mBufferDriverLock)
                {
                    if (pFrameInfo.nLostPacket == 0)
                    {
                        if (pData == IntPtr.Zero || pFrameInfo.nFrameLen >= m_BuffersizeForDriver)
                        {
                            Bitmap bmp = new Bitmap(pFrameInfo.nWidth, pFrameInfo.nHeight, pFrameInfo.nWidth * 1, System.Drawing.Imaging.PixelFormat.Format8bppIndexed, pData);
                            bmp.Palette = cp;
                            try
                            {
                                if (calibclick)
                                {
                                    Bitmap bcpy = CreateNonIndexedImage(bmp);
                                    docalib(bcpy);
                                    calibclick = false;
                                }
                                if (roundClick)
                                {
                                    Bitmap bcpy = CreateNonIndexedImage(bmp);
                                    doMesure(bcpy);
                                    roundClick = false;

                                }
                                if (pearClick)
                                {
                                    Bitmap bcpy = CreateNonIndexedImage(bmp);
                                    Mat b = BitmapConverter.ToMat(bcpy);
                                    string pearMesure = new MeasurePearShape().MeasurePearWithOpenCvSharp(b);
                                   
                                    this.Invoke((MethodInvoker)delegate
                                    {
                                        label1.Text = pearMesure;
                                    });
                                    
                                    pearClick = false;
                                }
                                if(polyClick)
                                {
                                    Bitmap bcpy = CreateNonIndexedImage(bmp);
                                    Mat b = BitmapConverter.ToMat(bcpy);
                                    string PolyMesure = PolygonMeasurement.DetectAndMeasurePolygon(b);

                                    this.Invoke((MethodInvoker)delegate
                                    {
                                        label1.Text = PolyMesure;
                                    });

                                    polyClick = false;
                                }
                                this.Invoke((MethodInvoker)delegate
                                {
                                    pictureBox1.Image = bmp;
                                });
                                GC.Collect();
                            }
                            catch (Exception)
                            {
                                calibclick = false;
                                roundClick = false;
                                pearClick = false;
                                polyClick = false;
                            }
                            calibclick = false;
                            roundClick = false;
                            pearClick = false;
                            polyClick = false;
                        }        
                    }
                }
                stopWatch.Restart();
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            calibclick = true;
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
                double ppm = metrology.Calibrate(circles[0].Radius, Convert.ToDouble(textBox1.Text));
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

        public void doMesure(Bitmap bmp)
        {
            var metrology = new ImageMetrology();

            Mat src = BitmapConverter.ToMat(bmp);//Cv2.ImRead(@"C:\Users\Administrator\MVS\Data\3.57.bmp", ImreadModes.Color);
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
                double realSize = metrology.GetRealDiameter(circles[0]);
                Console.WriteLine($"Found object: {realSize:F3} mm");
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
        
        private void button2_Click(object sender, EventArgs e)
        {
            roundClick = true;
        }

        private void btnPear_Click(object sender, EventArgs e)
        {
            pearClick = true;
        }

        private void lblmarqu_Click(object sender, EventArgs e)
        {
            pearClick = true;
        }

        private void btnHeart_Click(object sender, EventArgs e)
        {
            pearClick=true;
        }

        private void btnOvel_Click(object sender, EventArgs e)
        {
            pearClick = true;
        }

        private void btnPoly_Click(object sender, EventArgs e)
        {
            polyClick = true;
        }

        private void btnSettings_Click(object sender, EventArgs e)
        {
            //new frmSettings().Show();
        }
    }
}
