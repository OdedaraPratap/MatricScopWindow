using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Linq;

namespace Matric_scope
{
    public partial class VariationForm : Form
    {
        // API for dragging the borderless form
        public const int WM_NCLBUTTONDOWN = 0xA1;
        public const int HT_CAPTION = 0x2;

        [DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
        [DllImport("user32.dll")]
        public static extern bool ReleaseCapture();

        private NumericUpDown[] lengthCorrections = new NumericUpDown[25];
        private NumericUpDown[] widthCorrections = new NumericUpDown[25];

        // Global variables for the separate master adjust controls
        private NumericUpDown numGlobalLength;
        private NumericUpDown numGlobalWidth;
        private decimal previousGlobalLen = 0M;
        private decimal previousGlobalWid = 0M;

        // Match the orange/peach color from the uploaded image
        private Color themeOrange = Color.FromArgb(250, 182, 105);

        public VariationForm()
        {
            InitializeCustomUI();
        }

        private void InitializeCustomUI()
        {
            // 1. Form Core Settings (Light Theme)
            this.FormBorderStyle = FormBorderStyle.None;
            this.BackColor = Color.White;
            this.Size = new Size(420, 650);
            this.StartPosition = FormStartPosition.CenterScreen;

            // Add a border to the entire form to contain the white background cleanly
            this.Paint += (s, e) => {
                ControlPaint.DrawBorder(e.Graphics, this.ClientRectangle, Color.Black, ButtonBorderStyle.Solid);
            };

            // ==========================================
            // 2. Custom Orange Title Bar
            // ==========================================
            Panel titleBar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 35,
                BackColor = themeOrange
            };
            titleBar.MouseDown += TitleBar_MouseDown;

            Label lblTitle = new Label
            {
                Text = "Size Variations",
                ForeColor = Color.Black,
                Font = new Font("Arial", 11, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(40, 9)
            };
            lblTitle.MouseDown += TitleBar_MouseDown;

            Label btnClose = new Label
            {
                Text = "X",
                ForeColor = Color.White,
                BackColor = Color.Crimson,
                Font = new Font("Arial", 10, FontStyle.Bold),
                Size = new Size(35, 35),
                Location = new Point(this.Width - 35, 0),
                TextAlign = ContentAlignment.MiddleCenter,
                Cursor = Cursors.Hand
            };
            btnClose.Click += (s, e) => this.Close();
            btnClose.MouseEnter += (s, e) => btnClose.BackColor = Color.Red;
            btnClose.MouseLeave += (s, e) => btnClose.BackColor = Color.Crimson;

            // Draw a bottom border for the title bar
            titleBar.Paint += (s, e) => {
                ControlPaint.DrawBorder(e.Graphics, titleBar.ClientRectangle,
                    Color.Transparent, 0, ButtonBorderStyle.None,
                    Color.Transparent, 0, ButtonBorderStyle.None,
                    Color.Transparent, 0, ButtonBorderStyle.None,
                    Color.Black, 1, ButtonBorderStyle.Solid);
            };

            titleBar.Controls.Add(lblTitle);
            titleBar.Controls.Add(btnClose);

            // ==========================================
            // 3. Toolbar (Reset All & Two Global Adjusts)
            // ==========================================
            Panel toolsPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 45,
                BackColor = Color.White
            };

            // Global Length Adjuster
            Label lblGlobalLen = new Label
            {
                Text = "All Len +/-",
                ForeColor = Color.Black,
                Font = new Font("Arial", 9, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(5, 14)
            };
            numGlobalLength = CreateCustomNumeric();
            numGlobalLength.Width = 65;
            numGlobalLength.Location = new Point(75, 12);
            numGlobalLength.ValueChanged += NumGlobalLength_ValueChanged;

            // Global Width Adjuster
            Label lblGlobalWid = new Label
            {
                Text = "All Wid +/-",
                ForeColor = Color.Black,
                Font = new Font("Arial", 9, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(145, 14)
            };
            numGlobalWidth = CreateCustomNumeric();
            numGlobalWidth.Width = 65;
            numGlobalWidth.Location = new Point(215, 12);
            numGlobalWidth.ValueChanged += NumGlobalWidth_ValueChanged;

            Button btnReset = new Button
            {
                Text = "RESET ALL",
                ForeColor = Color.Black,
                BackColor = themeOrange,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Arial", 8, FontStyle.Bold),
                Size = new Size(85, 26),
                Location = new Point(325, 10),
                Cursor = Cursors.Hand
            };
            btnReset.FlatAppearance.BorderSize = 1;
            btnReset.FlatAppearance.BorderColor = Color.Black;
            btnReset.Click += BtnReset_Click;

            toolsPanel.Controls.Add(lblGlobalLen);
            toolsPanel.Controls.Add(numGlobalLength);
            toolsPanel.Controls.Add(lblGlobalWid);
            toolsPanel.Controls.Add(numGlobalWidth);
            toolsPanel.Controls.Add(btnReset);

            // ==========================================
            // 4. Main Data Grid (THE WRAPPER PANEL FIX)
            // ==========================================
            Panel scrollPanel = new Panel
            {
                Location = new Point(0, 80),
                Size = new Size(this.Width, this.Height - 125),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                AutoScroll = true,
                BackColor = Color.White
            };

            TableLayoutPanel table = new TableLayoutPanel
            {
                ColumnCount = 3,
                RowCount = 26,
                AutoScroll = false,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Dock = DockStyle.Top,
                BackColor = Color.White,
                Padding = new Padding(10)
            };

            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30));

            table.Controls.Add(CreateHeaderLabel("Size Range"), 0, 0);
            table.Controls.Add(CreateHeaderLabel("Length +/-"), 1, 0);
            table.Controls.Add(CreateHeaderLabel("Width +/-"), 2, 0);

            for (int i = 0; i < 25; i++)
            {
                Label lblRange = new Label
                {
                    Text = $"{i} to {i + 1} mm",
                    ForeColor = Color.Black,
                    BackColor = Color.White,
                    Font = new Font("Arial", 9, FontStyle.Bold),
                    AutoSize = true,
                    Anchor = AnchorStyles.Left | AnchorStyles.Top,
                    Margin = new Padding(0, 5, 0, 0)
                };

                lengthCorrections[i] = CreateCustomNumeric();
                widthCorrections[i] = CreateCustomNumeric();

                table.Controls.Add(lblRange, 0, i + 1);
                table.Controls.Add(lengthCorrections[i], 1, i + 1);
                table.Controls.Add(widthCorrections[i], 2, i + 1);
            }

            scrollPanel.Controls.Add(table);

            // ==========================================
            // 5. Custom Orange Save Button
            // ==========================================
            Button btnSave = new Button
            {
                Text = "SAVE",
                Location = new Point(10, this.Height - 40),
                Size = new Size(this.Width - 20, 30),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                Font = new Font("Arial", 11, FontStyle.Bold),
                BackColor = themeOrange,
                ForeColor = Color.Black,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnSave.FlatAppearance.BorderSize = 1;
            btnSave.FlatAppearance.BorderColor = Color.Black;
            btnSave.Click += BtnSave_Click;

            this.Controls.Add(scrollPanel);
            this.Controls.Add(toolsPanel);
            this.Controls.Add(titleBar);
            this.Controls.Add(btnSave);

            // ==========================================
            // 6. Load Data from Registry
            // ==========================================
            LoadSavedVariations();
        }

        // ==========================================
        // Data Loading Logic
        // ==========================================
        private void LoadSavedVariations()
        {
            ModifyRegistry mr = new ModifyRegistry();
            for (int i = 0; i < 25; i++)
            {
                try
                {
                    string lenStr = mr.Read($"LenVar_{i}");
                    string widStr = mr.Read($"WidVar_{i}");

                    if (!string.IsNullOrEmpty(lenStr) && decimal.TryParse(lenStr, out decimal lenVal))
                    {
                        if (lenVal > lengthCorrections[i].Maximum) lenVal = lengthCorrections[i].Maximum;
                        if (lenVal < lengthCorrections[i].Minimum) lenVal = lengthCorrections[i].Minimum;
                        lengthCorrections[i].Value = lenVal;
                    }

                    if (!string.IsNullOrEmpty(widStr) && decimal.TryParse(widStr, out decimal widVal))
                    {
                        if (widVal > widthCorrections[i].Maximum) widVal = widthCorrections[i].Maximum;
                        if (widVal < widthCorrections[i].Minimum) widVal = widthCorrections[i].Minimum;
                        widthCorrections[i].Value = widVal;
                    }
                }
                catch
                {
                    // If reading fails for any row, it safely remains at 0
                }
            }
        }

        // ==========================================
        // UI Interaction Methods
        // ==========================================

        private void NumGlobalLength_ValueChanged(object sender, EventArgs e)
        {
            decimal delta = numGlobalLength.Value - previousGlobalLen;
            for (int i = 0; i < 25; i++)
            {
                decimal newLen = lengthCorrections[i].Value + delta;
                if (newLen > lengthCorrections[i].Maximum) newLen = lengthCorrections[i].Maximum;
                if (newLen < lengthCorrections[i].Minimum) newLen = lengthCorrections[i].Minimum;
                lengthCorrections[i].Value = newLen;
            }
            previousGlobalLen = numGlobalLength.Value;
        }

        private void NumGlobalWidth_ValueChanged(object sender, EventArgs e)
        {
            decimal delta = numGlobalWidth.Value - previousGlobalWid;
            for (int i = 0; i < 25; i++)
            {
                decimal newWid = widthCorrections[i].Value + delta;
                if (newWid > widthCorrections[i].Maximum) newWid = widthCorrections[i].Maximum;
                if (newWid < widthCorrections[i].Minimum) newWid = widthCorrections[i].Minimum;
                widthCorrections[i].Value = newWid;
            }
            previousGlobalWid = numGlobalWidth.Value;
        }

        private void BtnReset_Click(object sender, EventArgs e)
        {
            numGlobalLength.Value = 0;
            previousGlobalLen = 0;
            numGlobalWidth.Value = 0;
            previousGlobalWid = 0;

            for (int i = 0; i < 25; i++)
            {
                lengthCorrections[i].Value = 0;
                widthCorrections[i].Value = 0;
            }
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            ModifyRegistry mr = new ModifyRegistry();
            for (int i = 0; i < 25; i++)
            {
                mr.Write($"LenVar_{i}", lengthCorrections[i].Value.ToString());
                mr.Write($"WidVar_{i}", widthCorrections[i].Value.ToString());
            }
            MessageBox.Show("Variations saved successfully!", "Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
            this.Close();
        }

        // ==========================================
        // UI Helpers
        // ==========================================

        private NumericUpDown CreateCustomNumeric()
        {
            return new NumericUpDown
            {
                DecimalPlaces = 3,
                Increment = 0.005M,
                Minimum = -5.000M,
                Maximum = 5.000M,
                Width = 80,
                BackColor = Color.White,
                ForeColor = Color.Black,
                BorderStyle = BorderStyle.Fixed3D
            };
        }

        private Label CreateHeaderLabel(string text)
        {
            return new Label
            {
                Text = text,
                ForeColor = Color.Black,
                BackColor = Color.White,
                Font = new Font("Arial", 10, FontStyle.Bold),
                AutoSize = true
            };
        }

        private void TitleBar_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
            }
        }
    }
}