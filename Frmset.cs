using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using uEye;

namespace Matric_scope
{
    public partial class Frmset: Form
    {
        ModifyRegistry mr = new ModifyRegistry();
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern bool ReleaseCapture();

        public const int WM_NCLBUTTONDOWN = 0xA1;
        public const int HT_CAPTION = 0x2;
        Camera camera = null;
        public Frmset(Camera cam)
        {
            camera = cam;
            InitializeComponent();
        }

        
        private void panel3_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
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

        private void Frmset_Load(object sender, EventArgs e)
        {
            if (mr.Read("chkPrint")!=null && mr.Read("chkPrint")!= "" && mr.Read("chkPrint")=="true")
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

        private void btnCamera_Click(object sender, EventArgs e)
        {
            new Camera_Setting1(camera).Show();
        }

        private void btnMesure_Click(object sender, EventArgs e)
        {
            new frmSettings().Show();
        }

        private void chkSave_CheckedChanged(object sender, EventArgs e)
        {
            if (chkSave.Checked)
            {
                mr.Write("chkSave", "true");
                FrmAuto.autosave = true ;
            }
            else
            {
                mr.Write("chkSave", "false");
                FrmAuto.autosave = false ;
            }
        }

        private void btnHistory_Click(object sender, EventArgs e)
        {
            new frmHistory().Show();
        }
    }
}
