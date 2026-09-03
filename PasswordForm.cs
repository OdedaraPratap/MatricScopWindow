using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Matric_scope
{
    public partial class PasswordForm : Form
    {
        private TextBox txtPassword;
        private Button btnSubmit;
        private Button btnChangePassword;
        public bool IsAuthenticated { get; private set; } = false;

        // API for dragging the borderless form (replaces manual Point tracking)
        public const int WM_NCLBUTTONDOWN = 0xA1;
        public const int HT_CAPTION = 0x2;

        [DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
        [DllImport("user32.dll")]
        public static extern bool ReleaseCapture();

        // Match the orange/peach color from the standardized theme
        private Color themeOrange = Color.FromArgb(250, 182, 105);

        public PasswordForm()
        {
            // Light Theme Settings
            BackColor = Color.White;
            ForeColor = Color.Black;
            Size = new Size(300, 150);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.None;
            TopMost = true;

            // Add a 1-pixel border to the entire form to contain the white background cleanly
            this.Paint += (s, e) => {
                ControlPaint.DrawBorder(e.Graphics, this.ClientRectangle, Color.Black, ButtonBorderStyle.Solid);
            };

            // Custom Orange Title Bar
            Panel titleBar = new Panel
            {
                BackColor = themeOrange,
                Dock = DockStyle.Top,
                Height = 30
            };
            titleBar.MouseDown += TitleBar_MouseDown;

            // Draw a bottom border for the title bar
            titleBar.Paint += (s, e) => {
                ControlPaint.DrawBorder(e.Graphics, titleBar.ClientRectangle,
                    Color.Transparent, 0, ButtonBorderStyle.None,
                    Color.Transparent, 0, ButtonBorderStyle.None,
                    Color.Transparent, 0, ButtonBorderStyle.None,
                    Color.Black, 1, ButtonBorderStyle.Solid);
            };

            Label lblTitle = new Label
            {
                Text = "Enter Password",
                ForeColor = Color.Black,
                Font = new Font("Arial", 10, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(10, 6)
            };
            // Allow dragging by clicking the text too
            lblTitle.MouseDown += TitleBar_MouseDown;

            // Standardized Close Button
            Label btnClose = new Label
            {
                Text = "X",
                ForeColor = Color.White,
                BackColor = Color.Crimson,
                Font = new Font("Arial", 10, FontStyle.Bold),
                Size = new Size(30, 30),
                Location = new Point(Width - 30, 0),
                TextAlign = ContentAlignment.MiddleCenter,
                Cursor = Cursors.Hand
            };
            btnClose.Click += (s, e) => Close();
            btnClose.MouseEnter += (s, e) => btnClose.BackColor = Color.Red;
            btnClose.MouseLeave += (s, e) => btnClose.BackColor = Color.Crimson;

            titleBar.Controls.Add(lblTitle);
            titleBar.Controls.Add(btnClose);

            // Form Controls
            txtPassword = new TextBox
            {
                Location = new Point(30, 55),
                Width = 240,
                UseSystemPasswordChar = true,
                BackColor = Color.White,
                ForeColor = Color.Black,
                BorderStyle = BorderStyle.FixedSingle
            };

            btnChangePassword = new Button
            {
                Text = "Change Password",
                Location = new Point(30, 100),
                Width = 120,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.White,
                ForeColor = Color.Black,
                Font = new Font("Arial", 8, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnChangePassword.FlatAppearance.BorderSize = 1;
            btnChangePassword.FlatAppearance.BorderColor = Color.Black;
            btnChangePassword.Click += BtnChangePassword_Click;

            btnSubmit = new Button
            {
                Text = "Login",
                Location = new Point(190, 100),
                Width = 80,
                FlatStyle = FlatStyle.Flat,
                BackColor = themeOrange,
                ForeColor = Color.Black,
                Font = new Font("Arial", 9, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnSubmit.FlatAppearance.BorderSize = 1;
            btnSubmit.FlatAppearance.BorderColor = Color.Black;
            btnSubmit.Click += BtnSubmit_Click;

            Controls.Add(titleBar);
            Controls.Add(txtPassword);
            Controls.Add(btnChangePassword);
            Controls.Add(btnSubmit);
            AcceptButton = btnSubmit;
        }

        // Clean API Dragging Logic
        private void TitleBar_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
            }
        }

        private void BtnSubmit_Click(object sender, EventArgs e)
        {
            if (PasswordManager.VerifyPassword(txtPassword.Text))
            {
                IsAuthenticated = true;
                DialogResult = DialogResult.OK;
                Close();
            }
            else
            {
                MessageBox.Show("Incorrect Password!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                txtPassword.Clear();
            }
        }

        private void BtnChangePassword_Click(object sender, EventArgs e)
        {
            using (var changeForm = new ChangePasswordForm())
            {
                changeForm.ShowDialog(this);
            }
        }
    }
}