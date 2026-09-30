using System;
using System.Drawing;
using System.Drawing.Printing;
using System.Windows.Forms;

namespace Matric_scope
{
    /// <summary>
    /// Themed print preview with the useful controls normally available
    /// in PrintPreviewDialog, while matching the Matric_scope theme.
    /// </summary>
    public sealed class StoneReportPreviewForm : Form
    {
        private readonly StoneReportPrinter reportPrinter;
        private readonly PrintPreviewControl previewControl =
            new PrintPreviewControl();

        private readonly Panel toolBar = new Panel();

        private readonly Button btnPrint = new Button();
        private readonly Button btnPageSetup = new Button();
        private readonly Button btnSavePdf = new Button();

        private readonly Button btnPrev = new Button();
        private readonly Button btnNext = new Button();

        private readonly Button btnOnePage = new Button();
        private readonly Button btnTwoPages = new Button();

        private readonly ComboBox cmbZoom = new ComboBox();
        private readonly Label lblPage = new Label();

        private int currentPage = 0;

        public StoneReportPreviewForm(
            StoneReportPrinter reportPrinter)
        {
            if (reportPrinter == null)
                throw new ArgumentNullException("reportPrinter");

            this.reportPrinter = reportPrinter;

            BuildUi();
        }

        private void BuildUi()
        {
            Text = "Print Preview";
            StartPosition = FormStartPosition.CenterParent;
            WindowState = FormWindowState.Maximized;
            MinimumSize = new Size(1000, 700);

            MatricTheme.ApplyForm(this);

            Panel titleBar =
                MatricTheme.CreateTitleBar(
                    this,
                    "Print Preview");

            // ---------------------------------------------------------
            // TOOLBAR
            // ---------------------------------------------------------
            toolBar.Dock = DockStyle.Top;
            toolBar.Height = 58;
            toolBar.BackColor = Color.White;
            toolBar.Padding = new Padding(10, 9, 10, 9);

            Controls.Add(toolBar);

            btnPrint.Text = "Print";
            btnPrint.Size = new Size(90, 38);
            btnPrint.Location = new Point(10, 10);
            MatricTheme.StylePrimaryButton(btnPrint);
            btnPrint.Click += BtnPrint_Click;
            toolBar.Controls.Add(btnPrint);

            btnPageSetup.Text = "Page Setup";
            btnPageSetup.Size = new Size(115, 38);
            btnPageSetup.Location = new Point(108, 10);
            MatricTheme.StyleSecondaryButton(btnPageSetup);
            btnPageSetup.Click += BtnPageSetup_Click;
            toolBar.Controls.Add(btnPageSetup);

            btnSavePdf.Text = "Save PDF";
            btnSavePdf.Size = new Size(105, 38);
            btnSavePdf.Location = new Point(231, 10);
            MatricTheme.StyleSecondaryButton(btnSavePdf);
            btnSavePdf.Click += BtnSavePdf_Click;
            toolBar.Controls.Add(btnSavePdf);

            AddSeparator(346);

            btnPrev.Text = "<";
            btnPrev.Size = new Size(42, 38);
            btnPrev.Location = new Point(361, 10);
            MatricTheme.StyleSecondaryButton(btnPrev);
            btnPrev.Click += BtnPrev_Click;
            toolBar.Controls.Add(btnPrev);

            lblPage.Text = "Page 1";
            lblPage.Size = new Size(78, 38);
            lblPage.Location = new Point(409, 10);
            lblPage.TextAlign = ContentAlignment.MiddleCenter;
            lblPage.Font = new Font(
                "Microsoft Sans Serif",
                10F,
                FontStyle.Bold);
            lblPage.ForeColor = Color.Black;
            lblPage.BackColor = Color.White;
            toolBar.Controls.Add(lblPage);

            btnNext.Text = ">";
            btnNext.Size = new Size(42, 38);
            btnNext.Location = new Point(493, 10);
            MatricTheme.StyleSecondaryButton(btnNext);
            btnNext.Click += BtnNext_Click;
            toolBar.Controls.Add(btnNext);

            AddSeparator(550);

            Label lblZoom = new Label();
            lblZoom.Text = "Zoom";
            lblZoom.Size = new Size(48, 38);
            lblZoom.Location = new Point(565, 10);
            lblZoom.TextAlign = ContentAlignment.MiddleLeft;
            lblZoom.Font = new Font(
                "Microsoft Sans Serif",
                10F,
                FontStyle.Bold);
            lblZoom.ForeColor = Color.Black;
            lblZoom.BackColor = Color.White;
            toolBar.Controls.Add(lblZoom);

            cmbZoom.DropDownStyle =
                ComboBoxStyle.DropDownList;
            cmbZoom.Location = new Point(614, 16);
            cmbZoom.Size = new Size(115, 28);
            MatricTheme.StyleComboBox(cmbZoom);

            cmbZoom.Items.Add("Fit Page");
            cmbZoom.Items.Add("Fit Width");
            cmbZoom.Items.Add("50%");
            cmbZoom.Items.Add("75%");
            cmbZoom.Items.Add("100%");
            cmbZoom.Items.Add("125%");
            cmbZoom.Items.Add("150%");
            cmbZoom.Items.Add("200%");
            cmbZoom.SelectedIndex = 0;
            cmbZoom.SelectedIndexChanged +=
                CmbZoom_SelectedIndexChanged;

            toolBar.Controls.Add(cmbZoom);

            btnOnePage.Text = "1 Page";
            btnOnePage.Size = new Size(82, 38);
            btnOnePage.Location = new Point(741, 10);
            MatricTheme.StylePrimaryButton(btnOnePage);
            btnOnePage.Click += delegate
            {
                previewControl.Rows = 1;
                previewControl.Columns = 1;
                ApplyPageViewButtonState();
            };
            toolBar.Controls.Add(btnOnePage);

            btnTwoPages.Text = "2 Pages";
            btnTwoPages.Size = new Size(88, 38);
            btnTwoPages.Location = new Point(831, 10);
            MatricTheme.StyleSecondaryButton(btnTwoPages);
            btnTwoPages.Click += delegate
            {
                previewControl.Rows = 1;
                previewControl.Columns = 2;
                ApplyPageViewButtonState();
            };
            toolBar.Controls.Add(btnTwoPages);

            // ---------------------------------------------------------
            // PREVIEW CONTROL
            // ---------------------------------------------------------
            previewControl.Dock = DockStyle.Fill;
            previewControl.BackColor =
                Color.FromArgb(220, 220, 220);
            previewControl.Document =
                reportPrinter.PreviewDocument;
            previewControl.AutoZoom = true;
            previewControl.Rows = 1;
            previewControl.Columns = 1;
            previewControl.StartPage = 0;

            Controls.Add(previewControl);
            previewControl.BringToFront();

            titleBar.BringToFront();
            toolBar.BringToFront();

            Shown += delegate
            {
                RefreshPreview();
                UpdatePageLabel();
            };

            Resize += delegate
            {
                if (cmbZoom.SelectedItem != null &&
                    cmbZoom.SelectedItem.ToString() == "Fit Page")
                {
                    ApplyZoomSelection();
                }
            };
        }

        private void AddSeparator(int x)
        {
            Panel line = new Panel();
            line.Location = new Point(x, 12);
            line.Size = new Size(1, 34);
            line.BackColor =
                Color.FromArgb(185, 185, 185);
            toolBar.Controls.Add(line);
        }

        private void BtnPrint_Click(
            object sender,
            EventArgs e)
        {
            reportPrinter.ShowPrinterSelection(this);
        }

        private void BtnPageSetup_Click(
            object sender,
            EventArgs e)
        {
            if (reportPrinter.ShowPageSetup(this))
            {
                RefreshPreview();
            }
        }

        private void BtnSavePdf_Click(
            object sender,
            EventArgs e)
        {
            try
            {
                if (reportPrinter.SaveAsPdf(this))
                {
                    MessageBox.Show(
                        "PDF saved successfully.",
                        "Stone Report",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
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

        private void BtnPrev_Click(
            object sender,
            EventArgs e)
        {
            if (currentPage <= 0)
                return;

            currentPage--;
            previewControl.StartPage =
                currentPage;

            UpdatePageLabel();
        }

        private void BtnNext_Click(
            object sender,
            EventArgs e)
        {
            // PrintPreviewControl safely ignores an unavailable page.
            // This also keeps the control useful if the report later grows
            // to multiple pages.
            currentPage++;
            previewControl.StartPage =
                currentPage;

            // If StartPage is normalized back by the control,
            // use the normalized value.
            currentPage =
                previewControl.StartPage;

            UpdatePageLabel();
        }

        private void UpdatePageLabel()
        {
            lblPage.Text =
                "Page " +
                (currentPage + 1).ToString();

            btnPrev.Enabled =
                currentPage > 0;
        }

        private void CmbZoom_SelectedIndexChanged(
            object sender,
            EventArgs e)
        {
            ApplyZoomSelection();
        }

        private void ApplyZoomSelection()
        {
            if (cmbZoom.SelectedItem == null)
                return;

            string zoom =
                cmbZoom.SelectedItem.ToString();

            if (zoom == "Fit Page")
            {
                previewControl.AutoZoom = true;
                previewControl.Rows = 1;
                previewControl.Columns = 1;
            }
            else if (zoom == "Fit Width")
            {
                previewControl.AutoZoom = false;

                double controlWidth =
                    Math.Max(
                        100.0,
                        previewControl.ClientSize.Width - 80.0);

                // A4 page width in hundredths of an inch is about 827.
                // PrintPreviewControl Zoom is a scale factor.
                double factor =
                    controlWidth / 827.0 / 4.0;

                if (factor < 0.1)
                    factor = 0.1;

                if (factor > 5.0)
                    factor = 5.0;

                previewControl.Zoom =
                    factor;
            }
            else
            {
                previewControl.AutoZoom = false;

                string value =
                    zoom.Replace("%", "");

                double percent;

                if (double.TryParse(
                    value,
                    out percent))
                {
                    previewControl.Zoom =
                        percent / 100.0;
                }
            }
        }

        private void ApplyPageViewButtonState()
        {
            if (previewControl.Columns == 1)
            {
                MatricTheme.StylePrimaryButton(
                    btnOnePage);

                MatricTheme.StyleSecondaryButton(
                    btnTwoPages);
            }
            else
            {
                MatricTheme.StyleSecondaryButton(
                    btnOnePage);

                MatricTheme.StylePrimaryButton(
                    btnTwoPages);
            }
        }

        private void RefreshPreview()
        {
            currentPage = 0;
            previewControl.StartPage = 0;

            // Re-assigning the document forces the preview to regenerate
            // after Page Setup changes.
            previewControl.Document = null;
            previewControl.Document =
                reportPrinter.PreviewDocument;

            ApplyZoomSelection();
            UpdatePageLabel();
        }
    }
}
