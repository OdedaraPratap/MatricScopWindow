using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Matric_scope
{
    /// <summary>
    /// Shared WinForms theme matching the existing Matric_scope Camera_Setting1 UI.
    /// </summary>
    public static class MatricTheme
    {
        public static readonly Color HeaderOrange =
            Color.FromArgb(250, 182, 105);

        public static readonly Color LightOrange =
            Color.FromArgb(250, 186, 105);

        public static readonly Color ActionOrange =
            Color.FromArgb(255, 128, 0);

        public static readonly Color PageWhite = Color.White;
        public static readonly Color TextBlack = Color.Black;
        public static readonly Color SoftGray = Color.FromArgb(245, 245, 245);
        public static readonly Color BorderGray = Color.FromArgb(190, 190, 190);

        private const int WM_NCLBUTTONDOWN = 0xA1;
        private const int HT_CAPTION = 0x2;

        [DllImport("user32.dll")]
        private static extern int SendMessage(
            IntPtr hWnd,
            int Msg,
            int wParam,
            int lParam);

        [DllImport("user32.dll")]
        private static extern bool ReleaseCapture();

        public static void ApplyForm(Form form)
        {
            form.BackColor = PageWhite;
            form.ForeColor = TextBlack;
            form.FormBorderStyle = FormBorderStyle.None;
            form.ControlBox = false;
            form.Font = new Font(
                "Microsoft Sans Serif",
                10F,
                FontStyle.Regular,
                GraphicsUnit.Point);

            form.Paint += delegate(object sender, PaintEventArgs e)
            {
                using (Pen pen = new Pen(HeaderOrange, 1F))
                {
                    Rectangle r = form.ClientRectangle;
                    r.Width -= 1;
                    r.Height -= 1;
                    e.Graphics.DrawRectangle(pen, r);
                }
            };
        }

        public static Panel CreateTitleBar(
            Form form,
            string title)
        {
            Panel bar = new Panel();
            bar.Name = "matricTitleBar";
            bar.Dock = DockStyle.Top;
            bar.Height = 32;
            bar.BackColor = HeaderOrange;

            PictureBox logo = new PictureBox();
            logo.Name = "matricTitleLogo";
            logo.Location = new Point(4, 0);
            logo.Size = new Size(35, 32);
            logo.SizeMode = PictureBoxSizeMode.StretchImage;
            logo.BackColor = Color.Transparent;

            // This resource already exists in the supplied Camera_Setting1 designer.
            try
            {
                logo.Image = Properties.Resources.Logo__2_;
            }
            catch
            {
                logo.Image = null;
            }

            Label label = new Label();
            label.Name = "matricTitleLabel";
            label.AutoSize = false;
            label.Location = new Point(48, 0);
            label.Anchor = AnchorStyles.Top |
                           AnchorStyles.Left |
                           AnchorStyles.Right;
            label.Size = new Size(
                Math.Max(100, form.ClientSize.Width - 92),
                32);
            label.Text = title;
            label.TextAlign = ContentAlignment.MiddleLeft;
            label.ForeColor = TextBlack;
            label.BackColor = Color.Transparent;
            label.Font = new Font(
                "Microsoft Sans Serif",
                12F,
                FontStyle.Bold,
                GraphicsUnit.Point);

            Button close = new Button();
            close.Name = "matricTitleClose";
            close.Text = "X";
            close.Dock = DockStyle.Right;
            close.Width = 34;
            close.BackColor = Color.Transparent;
            close.ForeColor = TextBlack;
            close.FlatStyle = FlatStyle.Popup;
            close.Font = new Font(
                "Microsoft Sans Serif",
                10F,
                FontStyle.Bold,
                GraphicsUnit.Point);
            close.UseVisualStyleBackColor = false;
            close.TabStop = false;
            close.Click += delegate
            {
                form.Close();
            };

            MouseEventHandler drag = delegate(object sender, MouseEventArgs e)
            {
                if (e.Button == MouseButtons.Left)
                {
                    ReleaseCapture();
                    SendMessage(
                        form.Handle,
                        WM_NCLBUTTONDOWN,
                        HT_CAPTION,
                        0);
                }
            };

            bar.MouseDown += drag;
            label.MouseDown += drag;
            logo.MouseDown += drag;

            bar.Controls.Add(close);
            bar.Controls.Add(logo);
            bar.Controls.Add(label);

            form.Controls.Add(bar);
            bar.BringToFront();

            return bar;
        }

        public static Label CreateSectionLabel(string text)
        {
            Label label = new Label();
            label.Text = text;
            label.AutoSize = false;
            label.Height = 31;
            label.Dock = DockStyle.Top;
            label.TextAlign = ContentAlignment.MiddleLeft;
            label.ForeColor = TextBlack;
            label.BackColor = PageWhite;
            label.Font = new Font(
                "Microsoft Sans Serif",
                12F,
                FontStyle.Bold,
                GraphicsUnit.Point);
            return label;
        }

        public static void StyleFieldLabel(Label label)
        {
            label.ForeColor = TextBlack;
            label.BackColor = PageWhite;
            label.Font = new Font(
                "Microsoft Sans Serif",
                11F,
                FontStyle.Bold,
                GraphicsUnit.Point);
        }

        public static void StyleTextBox(TextBox box)
        {
            box.BackColor = PageWhite;
            box.ForeColor = TextBlack;
            box.BorderStyle = BorderStyle.FixedSingle;
            box.Font = new Font(
                "Microsoft Sans Serif",
                11F,
                FontStyle.Bold,
                GraphicsUnit.Point);
        }

        public static void StyleComboBox(ComboBox box)
        {
            box.BackColor = PageWhite;
            box.ForeColor = TextBlack;
            box.FlatStyle = FlatStyle.Popup;
            box.Font = new Font(
                "Microsoft Sans Serif",
                11F,
                FontStyle.Bold,
                GraphicsUnit.Point);
        }

        public static void StyleDateTimePicker(DateTimePicker picker)
        {
            picker.CalendarForeColor = TextBlack;
            picker.CalendarMonthBackground = PageWhite;
            picker.Font = new Font(
                "Microsoft Sans Serif",
                11F,
                FontStyle.Bold,
                GraphicsUnit.Point);
        }

        public static void StyleCheckBox(CheckBox check)
        {
            check.ForeColor = TextBlack;
            check.BackColor = PageWhite;
            check.Font = new Font(
                "Microsoft Sans Serif",
                11F,
                FontStyle.Regular,
                GraphicsUnit.Point);
            check.UseVisualStyleBackColor = true;
        }

        public static void StylePrimaryButton(Button button)
        {
            button.BackColor = ActionOrange;
            button.ForeColor = Color.White;
            button.FlatStyle = FlatStyle.Popup;
            button.Font = new Font(
                "Microsoft Sans Serif",
                11F,
                FontStyle.Bold,
                GraphicsUnit.Point);
            button.UseVisualStyleBackColor = false;
            button.Cursor = Cursors.Hand;
        }

        public static void StyleSecondaryButton(Button button)
        {
            button.BackColor = LightOrange;
            button.ForeColor = TextBlack;
            button.FlatStyle = FlatStyle.Popup;
            button.Font = new Font(
                "Microsoft Sans Serif",
                11F,
                FontStyle.Bold,
                GraphicsUnit.Point);
            button.UseVisualStyleBackColor = false;
            button.Cursor = Cursors.Hand;
        }

        public static Panel CreateButtonBar()
        {
            Panel panel = new Panel();
            panel.Dock = DockStyle.Bottom;
            panel.Height = 62;
            panel.BackColor = PageWhite;
            return panel;
        }
    }
}
