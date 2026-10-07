using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Matric_scope
{
    public partial class FrmCalib : Form
    {
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern bool ReleaseCapture();

        public const int WM_NCLBUTTONDOWN = 0xA1;
        public const int HT_CAPTION = 0x2;

        public FrmCalib()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            FrmAuto.calibclick = true;
            new ModifyRegistry().Write("calibval", txtCalib.Text);
            this.Close();
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

        private void txtCalib_KeyPress(object sender, KeyPressEventArgs e)
        {
            // 1. Allow control characters (like Backspace, Delete, or Arrow keys)
            if (char.IsControl(e.KeyChar))
            {
                return;
            }

            // 2. Allow numeric digits (0-9)
            if (char.IsDigit(e.KeyChar))
            {
                return;
            }

            // 3. Allow only ONE decimal point
            if (e.KeyChar == '.')
            {
                TextBox textBox = sender as TextBox;

                // If the textbox already contains a decimal point, reject this keypress
                if (textBox != null && textBox.Text.Contains("."))
                {
                    e.Handled = true; // Handles (blocks) the character
                    return;
                }

                // Otherwise, it's the first decimal point, so allow it
                return;
            }

            // 4. Reject everything else (letters, spaces, special symbols)
            e.Handled = true;
        }

    }
}
