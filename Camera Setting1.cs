using Matric_scope.Properties;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using uEye;

namespace Matric_scope
{
    public partial class Camera_Setting1 : Form
    {
        
        ModifyRegistry mr = new ModifyRegistry();
        Camera camera;
        public static bool cameraflag;
        public const int WM_NCLBUTTONDOWN = 0xA1;
        public const int HT_CAPTION = 0x2;
        private frmHistory historyFormInstance = null;
        private frmSettings settingsFormInstance = null;

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern bool ReleaseCapture();
        public Camera_Setting1(Camera c)
        {
            camera = c;
            InitializeComponent();
        }
 
        private void panel1_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
            }
        }

        private void Camera_Setting1_Load(object sender, EventArgs e)
        {
            this.ShowInTaskbar = true;
            this.TopMost = true;
            trackBarGamma.Minimum = 100;
            trackBarGamma.Maximum = 220;
            numericUpDownGamma.Minimum = new Decimal(1.0);
            numericUpDownGamma.Maximum = new Decimal(2.2);
            numericUpDownGamma.Increment = new Decimal(0.1);
            if(mr.Read("trackBarGamma")==null || mr.Read("trackBarGamma")=="" || mr.Read("trackBarGamma")=="0")
            {
                trackBarGamma.Value = 100;
                numericUpDownGamma.Value = (decimal)1.0;
                camera.Gamma.Software.Set(100);
            }
            else
            {
                trackBarGamma.Value = (int)(Convert.ToDouble(mr.Read("trackBarGamma")));
                numericUpDownGamma.Value = (decimal)(Convert.ToDouble(mr.Read("trackBarGamma"))/100);
                camera.Gamma.Software.Set(Convert.ToInt32(mr.Read("trackBarGamma")));
            }
            if (mr.Read("trackBarGainMaster1") == "")
            {
                trackBarGainMaster.Value = 0;
                udmastergain.Value = 0;
            }
            else
            {
                trackBarGainMaster.Value = Convert.ToInt32(mr.Read("trackBarGainMaster1"));
                udmastergain.Value = Convert.ToInt32(mr.Read("trackBarGainMaster1"));
                // numericUpDownGainMaster.Value = Convert.ToInt32(mr.Read("trackBarGainMaster"));
            }

            //************exposure***********************

            if (mr.Read("trackBarExposure1") == "")
            {
                trackBarExposure.Value = 0;
                udexposure.Value = 0;
            }
            else
            {
                trackBarExposure.Value = Convert.ToInt32(mr.Read("trackBarExposure1"));
                udexposure.Value = Convert.ToInt32(mr.Read("trackBarExposure1"));
                // numericUpDownExposure.Value = Convert.ToInt32(mr.Read("trackBarExposure"));

            }

            if (mr.Read("chkPrint") != null && mr.Read("chkPrint") != "" && mr.Read("chkPrint") == "true")
            {
                chkPrint.Checked = true;
                FrmAuto.autoPrint = true;
            }
            else
            {
                chkPrint.Checked = false;
                FrmAuto.autoPrint = false;
            }

            if (mr.Read("chkSave") != null && mr.Read("chkSave") != "" && mr.Read("chkSave") == "true")
            {
                chkSave.Checked = true;
                FrmAuto.autosave = true;
            }
            else
            {
                chkSave.Checked = false;
                FrmAuto.autosave = false;
            }

        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            mr.Write("trackBarGainMaster1", trackBarGainMaster.Value.ToString());
            mr.Write("trackBarExposure1", trackBarExposure.Value.ToString());
            Camera_Setting1.cameraflag = true;
            this.Close();
        }

        private void udexposure_ValueChanged(object sender, EventArgs e)
        {
            try
            {
                trackBarExposure.Value = (int)udexposure.Value;
                uEye.Defines.Status statusRet;
                uEye.Types.Range<Double> range;

                statusRet = camera.Timing.Exposure.GetRange(out range);

                // calculate exposure
                Double dValue = range.Minimum + (int)udexposure.Value * range.Increment;
            //update numeric
           
                //numericUpDownExposure.Value = Convert.ToDecimal(dValue > range.Maximum ? Convert.ToDecimal(range.Maximum) : Convert.ToDecimal(dValue));
            //set exposure
                statusRet = camera.Timing.Exposure.Set(dValue);
            }
            catch
            {

            }
        }

        private void udmastergain_ValueChanged(object sender, EventArgs e)
        {
            try
            {
                trackBarGainMaster.Value = (int)udmastergain.Value;
                Int32 s32Value = (int)udmastergain.Value;
                camera.Gain.Hardware.Scaled.SetMaster(s32Value);
                // numericUpDownGainMaster.Value = s32Value;
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        private void numericUpDownGamma_ValueChanged(object sender, EventArgs e)
        {
            trackBarGamma.Value = (int)(numericUpDownGamma.Value * 100);
            mr.Write("trackBarGamma", trackBarGamma.Value.ToString());
        }

        private void trackBarGainMaster_Scroll(object sender, EventArgs e)
        {
            udmastergain.Value = trackBarGainMaster.Value;
            Int32 s32Value = trackBarGainMaster.Value;
            camera.Gain.Hardware.Scaled.SetMaster(s32Value);
        }

        private void trackBarExposure_Scroll(object sender, EventArgs e)
        {
            try
            {
                udexposure.Value = trackBarExposure.Value;
                uEye.Defines.Status statusRet;
                uEye.Types.Range<Double> range;

                statusRet = camera.Timing.Exposure.GetRange(out range);

                // calculate exposure
                Double dValue = range.Minimum + trackBarExposure.Value * range.Increment;
                //update numeric

                //numericUpDownExposure.Value = Convert.ToDecimal(dValue > range.Maximum ? Convert.ToDecimal(range.Maximum) : Convert.ToDecimal(dValue));
                //set exposure
                statusRet = camera.Timing.Exposure.Set(dValue);
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.ToString());
            }
        }

        private void trackBarGamma_Scroll(object sender, EventArgs e)
        {
            try
            {
                numericUpDownGamma.Value = (decimal)(trackBarGamma.Value / 100.0f);
                //exposure.SetValuePercentOfRange(SliderToPercentValue(trackBarExposure.Value));
                camera.Gamma.Software.Set(trackBarGamma.Value);
                mr.Write("trackBarGamma", trackBarGamma.Value.ToString());
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.ToString());
            }
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            mr.Write("trackBarGainMaster1", trackBarGainMaster.Value.ToString());
            mr.Write("trackBarExposure1", trackBarExposure.Value.ToString());
            Camera_Setting1.cameraflag = true;
            this.Close();
        }

        private void btnMax_Click(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Maximized;
        }

        private void btnMin_Click(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Minimized;
        }

        private void chkPrint_CheckedChanged(object sender, EventArgs e)
        {
            if (chkPrint.Checked)
            {
                mr.Write("chkPrint", "true");
                FrmAuto.autoPrint = true;
            }
            else
            {
                mr.Write("chkPrint", "false");
                FrmAuto.autoPrint = false;
            }
        }

        private void chkSave_CheckedChanged(object sender, EventArgs e)
        {
            if (chkSave.Checked)
            {
                mr.Write("chkSave", "true");
                FrmAuto.autosave = true;
            }
            else
            {
                mr.Write("chkSave", "false");
                FrmAuto.autosave = false;
            }
        }

        private void btnMesure_Click(object sender, EventArgs e)
        {
            if (settingsFormInstance == null || settingsFormInstance.IsDisposed)
            {
                settingsFormInstance = new frmSettings();
                settingsFormInstance.Show(this);
            }
            else
            {
                // 3. If it is already open, restore it if minimized and bring it to the top
                if (settingsFormInstance.WindowState == FormWindowState.Minimized)
                {
                    settingsFormInstance.WindowState = FormWindowState.Normal;
                }
                settingsFormInstance.BringToFront();
            }
        }

        private void btnHistory_Click(object sender, EventArgs e)
        {
            if (historyFormInstance == null || historyFormInstance.IsDisposed)
            {
                historyFormInstance = new frmHistory();
                historyFormInstance.Show(this);
            }
            else
            {
                // 3. If it is already open, restore it if minimized and bring it to the top
                if (historyFormInstance.WindowState == FormWindowState.Minimized)
                {
                    historyFormInstance.WindowState = FormWindowState.Normal;
                }
                historyFormInstance.BringToFront();
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (new HaspDemo().LogingDOG())
            {
                new VariationForm().Show(this);
            }
            else
            {
                using (PasswordForm pwdForm = new PasswordForm())
                {
                    if (pwdForm.ShowDialog(this) == DialogResult.OK)
                    {
                        new VariationForm().Show(this);
                    }
                }
            }

            
        }
    }
}
