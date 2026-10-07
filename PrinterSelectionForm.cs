using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace Matric_scope
{
    public sealed class PrinterSelectionForm : Form
    {
        private readonly StoneReportData reportData;
        private readonly StonePrintSettings settings;

        private readonly ComboBox cmbPrinters =
            new ComboBox();

        private readonly Label lblCount =
            new Label();

        private readonly Button btnRefresh =
            new Button();

        private readonly Button btnPrint =
            new Button();

        private readonly Button btnPdf =
            new Button();

        private readonly Button btnCancel =
            new Button();

        public PrinterSelectionForm(
            StoneReportData reportData,
            StonePrintSettings settings)
        {
            this.reportData = reportData;
            this.settings =
                settings ?? StonePrintSettings.Load();

            BuildUi();
            LoadPrinters();
        }

        private void BuildUi()
        {
            Text = "Printer";
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(650, 280);
            MinimumSize = new Size(650, 280);
            MaximumSize = new Size(650, 280);

            MatricTheme.ApplyForm(this);

            Panel titleBar =
                MatricTheme.CreateTitleBar(
                    this,
                    "Printer / PDF");

            Label label = new Label();
            label.Text = "Select Printer";
            label.Location = new Point(25, 58);
            label.Size = new Size(170, 25);
            MatricTheme.StyleFieldLabel(label);
            Controls.Add(label);

            cmbPrinters.DropDownStyle =
                ComboBoxStyle.DropDownList;

            cmbPrinters.Location =
                new Point(25, 88);

            cmbPrinters.Size =
                new Size(485, 28);

            MatricTheme.StyleComboBox(cmbPrinters);
            Controls.Add(cmbPrinters);

            btnRefresh.Text = "Refresh";
            btnRefresh.Location =
                new Point(520, 84);
            btnRefresh.Size =
                new Size(105, 36);
            MatricTheme.StyleSecondaryButton(btnRefresh);
            btnRefresh.Click += delegate
            {
                LoadPrinters();
            };
            Controls.Add(btnRefresh);

            lblCount.Location =
                new Point(25, 129);
            lblCount.Size =
                new Size(600, 26);
            lblCount.ForeColor = Color.Black;
            lblCount.Font = new Font(
                "Microsoft Sans Serif",
                10F,
                FontStyle.Regular);
            Controls.Add(lblCount);

            Label pdfInfo = new Label();
            pdfInfo.Text =
                "Save as PDF works directly and does not require a PDF printer.";
            pdfInfo.Location =
                new Point(25, 157);
            pdfInfo.Size =
                new Size(600, 28);
            pdfInfo.ForeColor = Color.Black;
            pdfInfo.Font = new Font(
                "Microsoft Sans Serif",
                10F,
                FontStyle.Regular);
            Controls.Add(pdfInfo);

            btnPrint.Text =
                "Print to Selected Printer";
            btnPrint.Location =
                new Point(25, 207);
            btnPrint.Size =
                new Size(215, 42);
            MatricTheme.StylePrimaryButton(btnPrint);
            btnPrint.Click += BtnPrint_Click;
            Controls.Add(btnPrint);

            btnPdf.Text =
                "Save as PDF";
            btnPdf.Location =
                new Point(250, 207);
            btnPdf.Size =
                new Size(145, 42);
            MatricTheme.StyleSecondaryButton(btnPdf);
            btnPdf.Click += BtnPdf_Click;
            Controls.Add(btnPdf);

            btnCancel.Text = "Cancel";
            btnCancel.Location =
                new Point(500, 207);
            btnCancel.Size =
                new Size(125, 42);
            MatricTheme.StyleSecondaryButton(btnCancel);
            btnCancel.Click += delegate
            {
                Close();
            };
            Controls.Add(btnCancel);

            titleBar.BringToFront();
        }

        private void LoadPrinters()
        {
            string previous =
                cmbPrinters.SelectedItem as string;

            string defaultPrinter =
                StoneReportPrinter.GetDefaultPrinter();

            List<string> printers =
                StoneReportPrinter.GetInstalledPrinters();

            cmbPrinters.BeginUpdate();
            cmbPrinters.Items.Clear();

            foreach (string printer in printers)
                cmbPrinters.Items.Add(printer);

            cmbPrinters.EndUpdate();

            lblCount.Text =
                printers.Count +
                " installed printer(s) found.";

            if (cmbPrinters.Items.Count == 0)
            {
                btnPrint.Enabled = false;
                return;
            }

            btnPrint.Enabled = true;

            int index = -1;

            if (!string.IsNullOrWhiteSpace(previous))
                index =
                    cmbPrinters.FindStringExact(
                        previous);

            if (index < 0 &&
                !string.IsNullOrWhiteSpace(defaultPrinter))
            {
                index =
                    cmbPrinters.FindStringExact(
                        defaultPrinter);
            }

            cmbPrinters.SelectedIndex =
                index >= 0 ? index : 0;
        }

        private void BtnPrint_Click(
            object sender,
            EventArgs e)
        {
            string printerName =
                cmbPrinters.SelectedItem as string;

            if (string.IsNullOrWhiteSpace(printerName))
            {
                MessageBox.Show(
                    "Please select a printer.",
                    "Stone Report",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                return;
            }

            try
            {
                using (StoneReportPrinter printer =
                    new StoneReportPrinter(
                        reportData,
                        settings))
                {
                    printer.PrintToPrinter(
                        printerName);
                }

                MessageBox.Show(
                    "Report sent to:\n" +
                    printerName,
                    "Stone Report",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Unable to print.\n\n" +
                    ex.Message,
                    "Stone Report",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void BtnPdf_Click(
            object sender,
            EventArgs e)
        {
            try
            {
                using (StoneReportPrinter printer =
                    new StoneReportPrinter(
                        reportData,
                        settings))
                {
                    if (printer.SaveAsPdf(this))
                    {
                        MessageBox.Show(
                            "PDF saved successfully.",
                            "Stone Report",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Unable to save PDF.\n\n" +
                    ex.Message,
                    "Stone Report",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }
    }
}
