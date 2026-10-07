using System;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Windows.Forms;

namespace Matric_scope
{
    public class StoneReportForm : Form
    {
        private StonePrintSettings settings;

        private readonly StoneReportData reportData =
            new StoneReportData();

        private readonly PictureBox pictureBox =
            new PictureBox();

        private readonly TableLayoutPanel fieldsTable =
            new TableLayoutPanel();

        private readonly Button btnSelectImage =
            new Button();

        private readonly Button btnSettings =
            new Button();

        private readonly Button btnPreview =
            new Button();

        private readonly Button btnPrint =
            new Button();

        private readonly Button btnClose =
            new Button();

        private TextBox txtStoneName;
        private TextBox txtCustomer;
        private TextBox txtShape;
        private TextBox txtLength;
        private TextBox txtWidth;
        private TextBox txtRatio;
        private TextBox txtWeight;
        private DateTimePicker dtMeasurement;
        private TextBox txtOperator;
        private TextBox txtRemarks;

        private Image ownedImage;
        private Panel fieldHost;

        public StoneReportForm()
            : this(
                  (Image)null,
                  0,
                  0,
                  "",
                  "",
                  "")
        {
        }

        public StoneReportForm(
            Image measuredStoneImage,
            double lengthMm,
            double widthMm,
            string shapeName = "",
            string stoneName = "",
            string customerName = "")
        {
            settings = StonePrintSettings.Load();

            reportData.LengthMm = lengthMm;
            reportData.WidthMm = widthMm;
            reportData.ShapeName = shapeName ?? "";
            reportData.StoneName = stoneName ?? "";
            reportData.CustomerName = customerName ?? "";
            reportData.MeasurementDateTime = DateTime.Now;

            if (measuredStoneImage != null)
            {
                ownedImage =
                    new Bitmap(measuredStoneImage);

                reportData.StoneImage =
                    ownedImage;
            }

            BuildUi();
            BuildSelectedFields();
            LoadInitialValues();
        }

        public StoneReportForm(
            string imagePath,
            double lengthMm,
            double widthMm,
            string shapeName = "",
            string stoneName = "",
            string customerName = "")
            : this(
                  LoadImageWithoutLock(imagePath),
                  lengthMm,
                  widthMm,
                  shapeName,
                  stoneName,
                  customerName)
        {
        }

        private void BuildUi()
        {
            Text = "Stone A4 Report";
            StartPosition = FormStartPosition.CenterParent;
            WindowState = FormWindowState.Maximized;
            MinimumSize = new Size(1000, 680);

            MatricTheme.ApplyForm(this);

            Panel titleBar =
                MatricTheme.CreateTitleBar(
                    this,
                    "Stone A4 Report");

            Panel bottom =
                MatricTheme.CreateButtonBar();

            Controls.Add(bottom);

            btnPrint.Text = "Printers / Save PDF";
            btnPrint.Size = new Size(190, 40);
            btnPrint.Location = new Point(
                Math.Max(12, ClientSize.Width - 202),
                11);
            btnPrint.Anchor =
                AnchorStyles.Top |
                AnchorStyles.Right;
            MatricTheme.StylePrimaryButton(btnPrint);
            btnPrint.Click += Print_Click;

            btnPreview.Text = "Print Preview";
            btnPreview.Size = new Size(135, 40);
            btnPreview.Location = new Point(
                Math.Max(12, ClientSize.Width - 347),
                11);
            btnPreview.Anchor =
                AnchorStyles.Top |
                AnchorStyles.Right;
            MatricTheme.StyleSecondaryButton(btnPreview);
            btnPreview.Click += Preview_Click;

            btnSettings.Text = "Report Setting";
            btnSettings.Size = new Size(140, 40);
            btnSettings.Location = new Point(
                Math.Max(12, ClientSize.Width - 497),
                11);
            btnSettings.Anchor =
                AnchorStyles.Top |
                AnchorStyles.Right;
            MatricTheme.StyleSecondaryButton(btnSettings);
            btnSettings.Click += Settings_Click;

            btnClose.Text = "Close";
            btnClose.Size = new Size(100, 40);
            btnClose.Location = new Point(12, 11);
            MatricTheme.StyleSecondaryButton(btnClose);
            btnClose.Click += delegate
            {
                Close();
            };

            bottom.Controls.Add(btnClose);
            bottom.Controls.Add(btnSettings);
            bottom.Controls.Add(btnPreview);
            bottom.Controls.Add(btnPrint);

            TableLayoutPanel main =
                new TableLayoutPanel();

            main.Dock = DockStyle.Fill;
            main.BackColor = Color.White;
            main.Padding = new Padding(12, 12, 12, 12);
            main.ColumnCount = 2;
            main.RowCount = 1;

            main.ColumnStyles.Add(
                new ColumnStyle(
                    SizeType.Percent,
                    58F));

            main.ColumnStyles.Add(
                new ColumnStyle(
                    SizeType.Percent,
                    42F));

            Controls.Add(main);
            main.BringToFront();

            // LEFT: measured image.
            Panel left = new Panel();
            left.Dock = DockStyle.Fill;
            left.BackColor = Color.White;
            left.Padding = new Padding(10);
            main.Controls.Add(left, 0, 0);

            Label imageTitle =
                MatricTheme.CreateSectionLabel(
                    "Measured Stone Image");

            left.Controls.Add(imageTitle);

            btnSelectImage.Text =
                "Select / Change Stone Image";

            btnSelectImage.Dock =
                DockStyle.Bottom;

            btnSelectImage.Height = 40;
            MatricTheme.StyleSecondaryButton(btnSelectImage);
            btnSelectImage.Click += BtnSelectImage_Click;
            left.Controls.Add(btnSelectImage);

            Panel imageBorder = new Panel();
            imageBorder.Dock = DockStyle.Fill;
            imageBorder.BackColor = MatricTheme.BorderGray;
            imageBorder.Padding = new Padding(1);
            left.Controls.Add(imageBorder);
            imageBorder.BringToFront();

            pictureBox.Dock = DockStyle.Fill;
            pictureBox.SizeMode = PictureBoxSizeMode.Zoom;
            pictureBox.BackColor = Color.White;
            imageBorder.Controls.Add(pictureBox);

            // RIGHT: data entry.
            Panel right = new Panel();
            right.Dock = DockStyle.Fill;
            right.BackColor = Color.White;
            right.Padding = new Padding(14, 10, 8, 10);
            main.Controls.Add(right, 1, 0);

            Label reportTitle =
                MatricTheme.CreateSectionLabel(
                    "A4 Report Information");

            right.Controls.Add(reportTitle);

            fieldHost = new Panel();
            fieldHost.Dock = DockStyle.Fill;
            fieldHost.BackColor = Color.White;
            fieldHost.AutoScroll = true;
            fieldHost.Padding = new Padding(0, 8, 0, 0);
            right.Controls.Add(fieldHost);
            fieldHost.BringToFront();

            titleBar.BringToFront();
            bottom.BringToFront();
        }

        private void BuildSelectedFields()
        {
            ReadVisibleOptionalValues();

            if (fieldHost == null)
                return;

            fieldHost.Controls.Clear();
            fieldsTable.Controls.Clear();
            fieldsTable.RowStyles.Clear();
            fieldsTable.ColumnStyles.Clear();

            fieldsTable.Dock = DockStyle.Top;
            fieldsTable.AutoSize = true;
            fieldsTable.BackColor = Color.White;
            fieldsTable.ColumnCount = 2;

            fieldsTable.ColumnStyles.Add(
                new ColumnStyle(
                    SizeType.Absolute,
                    205F));

            fieldsTable.ColumnStyles.Add(
                new ColumnStyle(
                    SizeType.Percent,
                    100F));

            fieldHost.Controls.Add(fieldsTable);

            txtStoneName = null;
            txtCustomer = null;
            txtShape = null;
            txtLength = null;
            txtWidth = null;
            txtRatio = null;
            txtWeight = null;
            dtMeasurement = null;
            txtOperator = null;
            txtRemarks = null;

            int row = 0;

            if (settings.PrintStoneName)
            {
                txtStoneName =
                    AddTextRow(
                        settings.StoneNameTitle,
                        ref row);
            }

            if (settings.PrintCustomerName)
            {
                txtCustomer =
                    AddTextRow(
                        settings.CustomerNameTitle,
                        ref row);
            }

            if (settings.PrintShapeName)
            {
                txtShape =
                    AddTextRow(
                        settings.ShapeNameTitle,
                        ref row);
            }

            txtLength =
                AddTextRow(
                    settings.LengthTitle +
                    " (mm) *",
                    ref row);

            txtWidth =
                AddTextRow(
                    settings.WidthTitle +
                    " (mm) *",
                    ref row);

            txtLength.TextChanged +=
                Dimension_TextChanged;

            txtWidth.TextChanged +=
                Dimension_TextChanged;

            if (settings.PrintLengthWidthRatio)
            {
                txtRatio =
                    AddTextRow(
                        settings.RatioTitle,
                        ref row);

                txtRatio.ReadOnly = true;
                txtRatio.BackColor =
                    MatricTheme.SoftGray;
            }

            if (settings.PrintWeight)
            {
                txtWeight =
                    AddTextRow(
                        settings.WeightTitle,
                        ref row);
            }

            if (settings.PrintDateTime)
            {
                AddLabel(
                    settings.DateTimeTitle,
                    row);

                dtMeasurement =
                    new DateTimePicker();

                dtMeasurement.Format =
                    DateTimePickerFormat.Custom;

                dtMeasurement.CustomFormat =
                    "dd-MM-yyyy hh:mm tt";

                dtMeasurement.Dock =
                    DockStyle.Fill;

                dtMeasurement.Margin =
                    new Padding(
                        4,
                        6,
                        4,
                        7);

                MatricTheme.StyleDateTimePicker(
                    dtMeasurement);

                EnsureRow(row, 43F);

                fieldsTable.Controls.Add(
                    dtMeasurement,
                    1,
                    row);

                row++;
            }

            if (settings.PrintOperatorMachine)
            {
                txtOperator =
                    AddTextRow(
                        settings.OperatorMachineTitle,
                        ref row);
            }

            if (settings.PrintRemarks)
            {
                AddLabel(
                    settings.RemarksTitle,
                    row);

                txtRemarks =
                    new TextBox();

                txtRemarks.Multiline = true;
                txtRemarks.Height = 110;
                txtRemarks.ScrollBars =
                    ScrollBars.Vertical;

                txtRemarks.Dock =
                    DockStyle.Fill;

                txtRemarks.Margin =
                    new Padding(
                        4,
                        6,
                        4,
                        7);

                MatricTheme.StyleTextBox(
                    txtRemarks);

                EnsureRow(row, 120F);

                fieldsTable.Controls.Add(
                    txtRemarks,
                    1,
                    row);

                row++;
            }

            LoadInitialValues();
        }

        private TextBox AddTextRow(
            string labelText,
            ref int row)
        {
            AddLabel(labelText, row);

            TextBox box = new TextBox();
            box.Dock = DockStyle.Fill;
            box.Margin = new Padding(
                4,
                6,
                4,
                7);

            MatricTheme.StyleTextBox(box);

            EnsureRow(row, 43F);

            fieldsTable.Controls.Add(
                box,
                1,
                row);

            row++;

            return box;
        }

        private void AddLabel(
            string text,
            int row)
        {
            EnsureRow(row, 43F);

            Label label = new Label();
            label.Text = text;
            label.Dock = DockStyle.Fill;
            label.TextAlign =
                ContentAlignment.MiddleLeft;

            label.Margin =
                new Padding(
                    4,
                    4,
                    4,
                    7);

            MatricTheme.StyleFieldLabel(label);

            fieldsTable.Controls.Add(
                label,
                0,
                row);
        }

        private void EnsureRow(
            int row,
            float height)
        {
            while (fieldsTable.RowStyles.Count <= row)
            {
                fieldsTable.RowStyles.Add(
                    new RowStyle(
                        SizeType.Absolute,
                        height));
            }

            fieldsTable.RowStyles[row].Height =
                height;
        }

        private void LoadInitialValues()
        {
            if (reportData.StoneImage != null)
                pictureBox.Image = reportData.StoneImage;

            if (txtStoneName != null)
                txtStoneName.Text = reportData.StoneName;

            if (txtCustomer != null)
                txtCustomer.Text = reportData.CustomerName;

            if (txtShape != null)
                txtShape.Text = reportData.ShapeName;

            if (txtLength != null)
            {
                txtLength.Text =
                    reportData.LengthMm > 0
                    ? reportData.LengthMm.ToString("F2")
                    : "";
            }

            if (txtWidth != null)
            {
                txtWidth.Text =
                    reportData.WidthMm > 0
                    ? reportData.WidthMm.ToString("F2")
                    : "";
            }

            if (txtWeight != null)
                txtWeight.Text = reportData.WeightCarat;

            if (dtMeasurement != null)
            {
                DateTime date =
                    reportData.MeasurementDateTime;

                if (date < dtMeasurement.MinDate)
                    date = DateTime.Now;

                dtMeasurement.Value = date;
            }

            if (txtOperator != null)
                txtOperator.Text = reportData.OperatorMachine;

            if (txtRemarks != null)
                txtRemarks.Text = reportData.Remarks;

            UpdateRatio();
        }

        private void Dimension_TextChanged(
            object sender,
            EventArgs e)
        {
            UpdateRatio();
        }

        private void UpdateRatio()
        {
            if (txtRatio == null)
                return;

            double length;
            double width;

            if (TryReadDouble(
                    txtLength == null
                    ? ""
                    : txtLength.Text,
                    out length) &&
                TryReadDouble(
                    txtWidth == null
                    ? ""
                    : txtWidth.Text,
                    out width) &&
                width > 0)
            {
                txtRatio.Text =
                    (length / width)
                        .ToString("F3");
            }
            else
            {
                txtRatio.Text = "";
            }
        }

        private bool ReadForm()
        {
            double length;
            double width;

            if (txtLength == null ||
                !TryReadDouble(
                    txtLength.Text,
                    out length) ||
                length <= 0)
            {
                MessageBox.Show(
                    "Please enter a valid Length in mm.",
                    "Stone Report",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                if (txtLength != null)
                    txtLength.Focus();

                return false;
            }

            if (txtWidth == null ||
                !TryReadDouble(
                    txtWidth.Text,
                    out width) ||
                width <= 0)
            {
                MessageBox.Show(
                    "Please enter a valid Width in mm.",
                    "Stone Report",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                if (txtWidth != null)
                    txtWidth.Focus();

                return false;
            }

            if (pictureBox.Image == null)
            {
                MessageBox.Show(
                    "Please select the measured stone image.",
                    "Stone Report",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                return false;
            }

            ReadVisibleOptionalValues();

            reportData.LengthMm = length;
            reportData.WidthMm = width;
            reportData.StoneImage =
                pictureBox.Image;

            return true;
        }

        private void ReadVisibleOptionalValues()
        {
            if (txtStoneName != null)
                reportData.StoneName =
                    txtStoneName.Text.Trim();

            if (txtCustomer != null)
                reportData.CustomerName =
                    txtCustomer.Text.Trim();

            if (txtShape != null)
                reportData.ShapeName =
                    txtShape.Text.Trim();

            if (txtWeight != null)
                reportData.WeightCarat =
                    txtWeight.Text.Trim();

            if (dtMeasurement != null)
                reportData.MeasurementDateTime =
                    dtMeasurement.Value;

            if (txtOperator != null)
                reportData.OperatorMachine =
                    txtOperator.Text.Trim();

            if (txtRemarks != null)
                reportData.Remarks =
                    txtRemarks.Text.Trim();

            double temp;

            if (txtLength != null &&
                TryReadDouble(
                    txtLength.Text,
                    out temp))
                reportData.LengthMm = temp;

            if (txtWidth != null &&
                TryReadDouble(
                    txtWidth.Text,
                    out temp))
                reportData.WidthMm = temp;
        }

        private void Settings_Click(
            object sender,
            EventArgs e)
        {
            ReadVisibleOptionalValues();

            using (StonePrintSettingsForm frm =
                new StonePrintSettingsForm())
            {
                if (frm.ShowDialog(this) ==
                    DialogResult.OK)
                {
                    settings =
                        StonePrintSettings.Load();

                    BuildSelectedFields();
                }
            }
        }

        private void Preview_Click(
            object sender,
            EventArgs e)
        {
            if (!ReadForm())
                return;

            using (StoneReportPrinter printer =
                new StoneReportPrinter(
                    reportData.CloneForPrint(),
                    settings))
            {
                printer.ShowPreview(this);
            }
        }

        private void Print_Click(
            object sender,
            EventArgs e)
        {
            if (!ReadForm())
                return;

            using (PrinterSelectionForm selector =
                new PrinterSelectionForm(
                    reportData.CloneForPrint(),
                    settings))
            {
                selector.ShowDialog(this);
            }
        }

        private void BtnSelectImage_Click(
            object sender,
            EventArgs e)
        {
            using (OpenFileDialog dlg =
                new OpenFileDialog())
            {
                dlg.Title =
                    "Select measured stone image";

                dlg.Filter =
                    "Image Files|" +
                    "*.png;*.jpg;*.jpeg;" +
                    "*.bmp;*.tif;*.tiff|" +
                    "All Files|*.*";

                if (dlg.ShowDialog(this) !=
                    DialogResult.OK)
                    return;

                try
                {
                    Image image =
                        LoadImageWithoutLock(
                            dlg.FileName);

                    ReplaceOwnedImage(image);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        "Unable to load image.\n\n" +
                        ex.Message,
                        "Stone Report",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
            }
        }

        private void ReplaceOwnedImage(
            Image image)
        {
            if (ownedImage != null)
            {
                if (object.ReferenceEquals(
                    pictureBox.Image,
                    ownedImage))
                {
                    pictureBox.Image = null;
                }

                ownedImage.Dispose();
            }

            ownedImage = image;
            reportData.StoneImage = ownedImage;
            pictureBox.Image = ownedImage;
        }

        private static bool TryReadDouble(
            string text,
            out double value)
        {
            text =
                (text ?? "").Trim();

            if (double.TryParse(
                text,
                NumberStyles.Float,
                CultureInfo.CurrentCulture,
                out value))
                return true;

            return double.TryParse(
                text.Replace(',', '.'),
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out value);
        }

        private static Image
            LoadImageWithoutLock(
                string path)
        {
            if (string.IsNullOrWhiteSpace(path) ||
                !File.Exists(path))
                return null;

            using (Image temp =
                Image.FromFile(path))
            {
                return new Bitmap(temp);
            }
        }

        private void StoneReportForm_FormClosed(
            object sender,
            FormClosedEventArgs e)
        {
            pictureBox.Image = null;

            if (ownedImage != null)
            {
                ownedImage.Dispose();
                ownedImage = null;
            }
        }

        protected override void OnFormClosed(
            FormClosedEventArgs e)
        {
            StoneReportForm_FormClosed(
                this,
                e);

            base.OnFormClosed(e);
        }
    }
}
