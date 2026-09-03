using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Matric_scope
{
    public partial class ChangePasswordForm : Form
    {
        private TextBox txtOldPassword;
        private TextBox txtNewPassword;
        private Button btnSave;

        // API for dragging the borderless form (replaces manual Point tracking)
        public const int WM_NCLBUTTONDOWN = 0xA1;
        public const int HT_CAPTION = 0x2;

        [DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
        [DllImport("user32.dll")]
        public static extern bool ReleaseCapture();

        // Match the orange/peach color from the uploaded image
        private Color themeOrange = Color.FromArgb(250, 182, 105);

        public ChangePasswordForm()
        {
            // Light Theme Settings
            BackColor = Color.White;
            ForeColor = Color.Black;
            Size = new Size(300, 180);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.None;
            TopMost = true;

            // Add a border to the entire form to contain the white background cleanly
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
                Text = "Change Password",
                ForeColor = Color.Black,
                Font = new Font("Arial", 10, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(10, 6)
            };
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
            Label lblOld = new Label { Text = "Old Password:", Location = new Point(20, 50), Width = 100, ForeColor = Color.Black, Font = new Font("Arial", 9, FontStyle.Bold) };
            txtOldPassword = new TextBox
            {
                Location = new Point(130, 48),
                Width = 140,
                UseSystemPasswordChar = true,
                BackColor = Color.White,
                ForeColor = Color.Black,
                BorderStyle = BorderStyle.FixedSingle
            };

            Label lblNew = new Label { Text = "New Password:", Location = new Point(20, 90), Width = 100, ForeColor = Color.Black, Font = new Font("Arial", 9, FontStyle.Bold) };
            txtNewPassword = new TextBox
            {
                Location = new Point(130, 88),
                Width = 140,
                UseSystemPasswordChar = true,
                BackColor = Color.White,
                ForeColor = Color.Black,
                BorderStyle = BorderStyle.FixedSingle
            };

            btnSave = new Button
            {
                Text = "Save",
                Location = new Point(190, 130),
                Width = 80,
                FlatStyle = FlatStyle.Flat,
                BackColor = themeOrange,
                ForeColor = Color.Black,
                Font = new Font("Arial", 9, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnSave.FlatAppearance.BorderSize = 1;
            btnSave.FlatAppearance.BorderColor = Color.Black;
            btnSave.Click += BtnSave_Click;

            Controls.Add(titleBar);
            Controls.Add(lblOld);
            Controls.Add(txtOldPassword);
            Controls.Add(lblNew);
            Controls.Add(txtNewPassword);
            Controls.Add(btnSave);
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

        private void BtnSave_Click(object sender, EventArgs e)
        {
            if (PasswordManager.VerifyPassword(txtOldPassword.Text))
            {
                if (!string.IsNullOrWhiteSpace(txtNewPassword.Text))
                {
                    PasswordManager.SetPassword(txtNewPassword.Text);
                    MessageBox.Show("Password changed successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    Close();
                }
                else
                {
                    MessageBox.Show("New password cannot be empty.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            else
            {
                MessageBox.Show("Old password is incorrect.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ChangePasswordForm_Load(object sender, EventArgs e)
        {

        }
    }
}