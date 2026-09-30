using System;
using System.Drawing;
using System.Windows.Forms;

namespace Matric_scope
{
    public class StonePrintSettingsForm : Form
    {
        private StonePrintSettings settings;

        private readonly TableLayoutPanel table =
            new TableLayoutPanel();

        private readonly CheckBox chkStoneName = new CheckBox();
        private readonly CheckBox chkCustomerName = new CheckBox();
        private readonly CheckBox chkShapeName = new CheckBox();
        private readonly CheckBox chkLength = new CheckBox();
        private readonly CheckBox chkWidth = new CheckBox();
        private readonly CheckBox chkRatio = new CheckBox();
        private readonly CheckBox chkWeight = new CheckBox();
        private readonly CheckBox chkDateTime = new CheckBox();
        private readonly CheckBox chkOperator = new CheckBox();
        private readonly CheckBox chkRemarks = new CheckBox();

        private readonly TextBox txtStoneNameTitle = new TextBox();
        private readonly TextBox txtCustomerTitle = new TextBox();
        private readonly TextBox txtShapeTitle = new TextBox();
        private readonly TextBox txtLengthTitle = new TextBox();
        private readonly TextBox txtWidthTitle = new TextBox();
        private readonly TextBox txtRatioTitle = new TextBox();
        private readonly TextBox txtWeightTitle = new TextBox();
        private readonly TextBox txtDateTimeTitle = new TextBox();
        private readonly TextBox txtOperatorTitle = new TextBox();
        private readonly TextBox txtRemarksTitle = new TextBox();

        private readonly Button btnSave = new Button();
        private readonly Button btnPreview = new Button();
        private readonly Button btnResetTitles = new Button();
        private readonly Button btnCancel = new Button();

        public StonePrintSettingsForm()
        {
            settings = StonePrintSettings.Load();

            BuildUi();
            LoadValues();
        }

        private void BuildUi()
        {
            Text = "Print Setting";
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(770, 610);
            MinimumSize = new Size(770, 610);
            MaximumSize = new Size(770, 610);

            MatricTheme.ApplyForm(this);

            Panel titleBar =
                MatricTheme.CreateTitleBar(
                    this,
                    "Print Setting");

            Panel bottom =
                MatricTheme.CreateButtonBar();

            bottom.Padding =
                new Padding(12, 10, 12, 10);

            Controls.Add(bottom);
            bottom.BringToFront();

            btnSave.Text = "Save Setting";
            btnSave.Size = new Size(130, 40);
            btnSave.Location = new Point(618, 10);
            btnSave.Anchor = AnchorStyles.Top |
                             AnchorStyles.Right;
            MatricTheme.StylePrimaryButton(btnSave);
            btnSave.Click += BtnSave_Click;

            btnPreview.Text = "Preview A4";
            btnPreview.Size = new Size(120, 40);
            btnPreview.Location = new Point(488, 10);
            btnPreview.Anchor = AnchorStyles.Top |
                                AnchorStyles.Right;
            MatricTheme.StyleSecondaryButton(btnPreview);
            btnPreview.Click += BtnPreview_Click;

            btnResetTitles.Text = "Reset Titles";
            btnResetTitles.Size = new Size(120, 40);
            btnResetTitles.Location = new Point(358, 10);
            btnResetTitles.Anchor = AnchorStyles.Top |
                                    AnchorStyles.Right;
            MatricTheme.StyleSecondaryButton(btnResetTitles);
            btnResetTitles.Click += BtnResetTitles_Click;

            btnCancel.Text = "Cancel";
            btnCancel.Size = new Size(100, 40);
            btnCancel.Location = new Point(12, 10);
            MatricTheme.StyleSecondaryButton(btnCancel);
            btnCancel.Click += delegate
            {
                Close();
            };

            bottom.Controls.Add(btnCancel);
            bottom.Controls.Add(btnResetTitles);
            bottom.Controls.Add(btnPreview);
            bottom.Controls.Add(btnSave);

            Panel content = new Panel();
            content.Dock = DockStyle.Fill;
            content.BackColor = MatricTheme.PageWhite;
            content.Padding = new Padding(22, 14, 22, 12);
            Controls.Add(content);
            content.BringToFront();

            Label heading = new Label();
            heading.Text = "Report Fields";
            heading.Dock = DockStyle.Top;
            heading.Height = 32;
            heading.ForeColor = Color.Black;
            heading.Font = new Font(
                "Microsoft Sans Serif",
                12F,
                FontStyle.Bold);
            heading.TextAlign = ContentAlignment.MiddleLeft;
            content.Controls.Add(heading);

            Label info = new Label();
            info.Text =
                "Select fields to print and edit their titles. " +
                "Length, Width and Ratio titles are fixed.";
            info.Dock = DockStyle.Top;
            info.Height = 34;
            info.ForeColor = Color.Black;
            info.Font = new Font(
                "Microsoft Sans Serif",
                10F,
                FontStyle.Regular);
            info.TextAlign = ContentAlignment.MiddleLeft;
            info.Padding = new Padding(0, 20, 0, 0);
            content.Controls.Add(info);

            Panel tableHost = new Panel();
            tableHost.Dock = DockStyle.Fill;
            tableHost.BackColor = Color.White;
            tableHost.AutoScroll = true;
            tableHost.Padding = new Padding(0, 6, 0, 0);
            content.Controls.Add(tableHost);
            tableHost.BringToFront();

            table.Dock = DockStyle.Top;
            table.AutoSize = true;
            table.BackColor = Color.White;
            table.ColumnCount = 3;
            table.ColumnStyles.Add(
                new ColumnStyle(SizeType.Absolute, 80F));
            table.ColumnStyles.Add(
                new ColumnStyle(SizeType.Absolute, 205F));
            table.ColumnStyles.Add(
                new ColumnStyle(SizeType.Percent, 100F));
            tableHost.Controls.Add(table);

            AddHeader(0, "Print");
            AddHeader(1, "Field");
            AddHeader(2, "Printed Title");

            int row = 1;

            AddEditableRow(row++, chkStoneName, "Stone", txtStoneNameTitle);
            AddEditableRow(row++, chkCustomerName, "Customer", txtCustomerTitle);
            AddEditableRow(row++, chkShapeName, "Shape", txtShapeTitle);
            AddFixedMandatoryRow(row++, chkLength, "Length", txtLengthTitle);
            AddFixedMandatoryRow(row++, chkWidth, "Width", txtWidthTitle);
            AddFixedSelectableRow(row++, chkRatio, "Ratio", txtRatioTitle);
            AddEditableRow(row++, chkWeight, "Weight", txtWeightTitle);
            AddEditableRow(row++, chkDateTime, "Date / Time", txtDateTimeTitle);
            AddEditableRow(row++, chkOperator, "Operator / Machine", txtOperatorTitle);
            AddEditableRow(row++, chkRemarks, "Remarks", txtRemarksTitle);

            // Keep title bar and button bar visible over Dock.Fill content.
            titleBar.BringToFront();
            bottom.BringToFront();
        }

        private void AddHeader(int column, string text)
        {
            Label label = new Label();
            label.Text = text;
            label.Dock = DockStyle.Fill;
            label.TextAlign = ContentAlignment.MiddleLeft;
            label.ForeColor = Color.Black;
            label.BackColor = MatricTheme.HeaderOrange;
            label.Font = new Font(
                "Microsoft Sans Serif",
                11F,
                FontStyle.Bold);
            label.Margin = new Padding(0);
            label.Padding = new Padding(6, 0, 0, 0);
            table.Controls.Add(label, column, 0);
        }

        private void AddEditableRow(
            int row,
            CheckBox checkBox,
            string fieldName,
            TextBox titleBox)
        {
            EnsureRow(row);

            MatricTheme.StyleCheckBox(checkBox);
            checkBox.Dock = DockStyle.Fill;
            checkBox.TextAlign = ContentAlignment.MiddleCenter;
            checkBox.Margin = new Padding(5);
            table.Controls.Add(checkBox, 0, row);

            Label fieldLabel = new Label();
            fieldLabel.Text = fieldName;
            fieldLabel.Dock = DockStyle.Fill;
            fieldLabel.TextAlign = ContentAlignment.MiddleLeft;
            fieldLabel.Margin = new Padding(6, 4, 3, 4);
            MatricTheme.StyleFieldLabel(fieldLabel);
            table.Controls.Add(fieldLabel, 1, row);

            titleBox.Dock = DockStyle.Fill;
            titleBox.MaxLength = 60;
            titleBox.Margin = new Padding(4, 7, 4, 7);
            MatricTheme.StyleTextBox(titleBox);
            table.Controls.Add(titleBox, 2, row);
        }

        private void AddFixedMandatoryRow(
            int row,
            CheckBox checkBox,
            string fieldName,
            TextBox titleBox)
        {
            AddEditableRow(
                row,
                checkBox,
                fieldName,
                titleBox);

            checkBox.Checked = true;
            checkBox.Enabled = false;

            titleBox.ReadOnly = true;
            titleBox.BackColor = MatricTheme.SoftGray;
        }

        private void AddFixedSelectableRow(
            int row,
            CheckBox checkBox,
            string fieldName,
            TextBox titleBox)
        {
            AddEditableRow(
                row,
                checkBox,
                fieldName,
                titleBox);

            titleBox.ReadOnly = true;
            titleBox.BackColor = MatricTheme.SoftGray;
        }

        private void EnsureRow(int row)
        {
            while (table.RowStyles.Count <= row)
            {
                table.RowStyles.Add(
                    new RowStyle(
                        SizeType.Absolute,
                        43F));
            }
        }

        private void LoadValues()
        {
            chkStoneName.Checked = settings.PrintStoneName;
            chkCustomerName.Checked = settings.PrintCustomerName;
            chkShapeName.Checked = settings.PrintShapeName;
            chkLength.Checked = true;
            chkWidth.Checked = true;
            chkRatio.Checked = settings.PrintLengthWidthRatio;
            chkWeight.Checked = settings.PrintWeight;
            chkDateTime.Checked = settings.PrintDateTime;
            chkOperator.Checked = settings.PrintOperatorMachine;
            chkRemarks.Checked = settings.PrintRemarks;

            txtStoneNameTitle.Text = settings.StoneNameTitle;
            txtCustomerTitle.Text = settings.CustomerNameTitle;
            txtShapeTitle.Text = settings.ShapeNameTitle;
            txtLengthTitle.Text = settings.LengthTitle;
            txtWidthTitle.Text = settings.WidthTitle;
            txtRatioTitle.Text = settings.RatioTitle;
            txtWeightTitle.Text = settings.WeightTitle;
            txtDateTimeTitle.Text = settings.DateTimeTitle;
            txtOperatorTitle.Text = settings.OperatorMachineTitle;
            txtRemarksTitle.Text = settings.RemarksTitle;
        }

        private StonePrintSettings CreateSettingsFromControls()
        {
            StonePrintSettings result =
                new StonePrintSettings();

            result.PrintLength = true;
            result.PrintWidth = true;
            result.PrintStoneName = chkStoneName.Checked;
            result.PrintCustomerName = chkCustomerName.Checked;
            result.PrintShapeName = chkShapeName.Checked;
            result.PrintLengthWidthRatio = chkRatio.Checked;
            result.PrintWeight = chkWeight.Checked;
            result.PrintDateTime = chkDateTime.Checked;
            result.PrintOperatorMachine = chkOperator.Checked;
            result.PrintRemarks = chkRemarks.Checked;

            result.StoneNameTitle = txtStoneNameTitle.Text;
            result.CustomerNameTitle = txtCustomerTitle.Text;
            result.ShapeNameTitle = txtShapeTitle.Text;
            result.WeightTitle = txtWeightTitle.Text;
            result.DateTimeTitle = txtDateTimeTitle.Text;
            result.OperatorMachineTitle = txtOperatorTitle.Text;
            result.RemarksTitle = txtRemarksTitle.Text;

            result.NormalizeTitles();

            return result;
        }

        private void BtnPreview_Click(
            object sender,
            EventArgs e)
        {
            try
            {
                StonePrintSettings previewSettings =
                    CreateSettingsFromControls();

                using (Bitmap blankImage =
                    new Bitmap(1200, 800))
                {
                    using (Graphics g =
                        Graphics.FromImage(blankImage))
                    {
                        g.Clear(Color.White);
                    }

                    StoneReportData sampleData =
                        new StoneReportData();

                    sampleData.StoneName = "ST-000125";
                    sampleData.CustomerName = "Sample Customer";
                    sampleData.ShapeName = "Sample Shape";
                    sampleData.LengthMm = 5.36;
                    sampleData.WidthMm = 4.63;
                    sampleData.WeightCarat = "0.72";
                    sampleData.MeasurementDateTime = DateTime.Now;
                    sampleData.OperatorMachine = "Machine-01";
                    sampleData.Remarks = "Sample remarks / notes";
                    sampleData.StoneImage = blankImage;

                    using (StoneReportPrinter printer =
                        new StoneReportPrinter(
                            sampleData,
                            previewSettings))
                    {
                        printer.ShowPreview(this);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Unable to show report preview.\n\n" +
                    ex.Message,
                    "Stone Report Preview",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void BtnSave_Click(
            object sender,
            EventArgs e)
        {
            settings = CreateSettingsFromControls();

            try
            {
                settings.Save();

                MessageBox.Show(
                    "Report field settings saved successfully.",
                    "Stone Report",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Unable to save settings.\n\n" +
                    ex.Message,
                    "Stone Report",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void BtnResetTitles_Click(
            object sender,
            EventArgs e)
        {
            StonePrintSettings defaults =
                new StonePrintSettings();

            txtStoneNameTitle.Text = defaults.StoneNameTitle;
            txtCustomerTitle.Text = defaults.CustomerNameTitle;
            txtShapeTitle.Text = defaults.ShapeNameTitle;
            txtLengthTitle.Text = defaults.LengthTitle;
            txtWidthTitle.Text = defaults.WidthTitle;
            txtRatioTitle.Text = defaults.RatioTitle;
            txtWeightTitle.Text = defaults.WeightTitle;
            txtDateTimeTitle.Text = defaults.DateTimeTitle;
            txtOperatorTitle.Text = defaults.OperatorMachineTitle;
            txtRemarksTitle.Text = defaults.RemarksTitle;
        }
    }
}
