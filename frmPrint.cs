using OpenCvSharp.ML;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Printing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Matric_scope
{
    public partial class frmPrint : Form
    {
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern bool ReleaseCapture();

        public const int WM_NCLBUTTONDOWN = 0xA1, HT_CAPTION = 0x2;
        private DataTable dtRules;
        private readonly string xmlFilePath = Path.Combine(Application.StartupPath, "DiamondRules.xml");
        private string currentfile;

        private LabelPrintingEngine printingEngine = new LabelPrintingEngine();
        private string targetedDevice = "TSC M23"; // Change to your local physical printer name

        // NEW: Class-level storage to hold the dictionary passed from the Main Form
        private Dictionary<int, int> activeTrayCounters = new Dictionary<int, int>();

        // UPDATED CONSTRUCTOR: Injects your live, tray-by-tray counter dictionary
        public frmPrint(Dictionary<int, int> liveCounters)
        {
            InitializeComponent();

            if (liveCounters != null)
            {
                activeTrayCounters = new Dictionary<int, int>(liveCounters);
            }
        }


        

        private void btnPrint_Click(object sender, EventArgs e)
        {
            if (int.TryParse(txtPrint.Text, out int inputTray))
            {
                printingEngine.PrintSingleTray(dtRules, inputTray, targetedDevice);
            }
            else
            {
                MessageBox.Show("Please type or select a valid numeric Tray Number first.", "Entry Error");
            }
        }

        private void btnPrintAll_Click(object sender, EventArgs e)
        {
            printingEngine.PrintAllTrays(dtRules, targetedDevice);
        }

        public void LoadRulesDatabase()
        {
            DataTable dtMaster = new DataTable("Rule");
            dtMaster.Columns.Add("FileName", typeof(string));
            dtMaster.Columns.Add("Number", typeof(int));
            dtMaster.Columns.Add("ShapeType", typeof(string));
            dtMaster.Columns.Add("FromLength", typeof(double));
            dtMaster.Columns.Add("ToLength", typeof(double));
            dtMaster.Columns.Add("FromWidth", typeof(double));
            dtMaster.Columns.Add("ToWidth", typeof(double));

            if (File.Exists(xmlFilePath))
            {
                try
                {
                    dtMaster.ReadXml(xmlFilePath);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Detection Form failed to load database layout: {ex.Message}", "Data Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }

            dtRules = new DataTable("Rule");
            dtRules.Columns.Add("FileName", typeof(string));
            dtRules.Columns.Add("Number", typeof(int));
            dtRules.Columns.Add("ShapeType", typeof(string));
            dtRules.Columns.Add("FromLength", typeof(double));
            dtRules.Columns.Add("ToLength", typeof(double));
            dtRules.Columns.Add("FromWidth", typeof(double));
            dtRules.Columns.Add("ToWidth", typeof(double));

            // CHANGED: This column now holds the dedicated tray counter from your dictionary
            dtRules.Columns.Add("DiamondCount", typeof(int));

            dtRules.PrimaryKey = new DataColumn[] { dtRules.Columns["ShapeType"], dtRules.Columns["Number"] };

            if (!string.IsNullOrWhiteSpace(currentfile))
            {
                var filteredRows = dtMaster.AsEnumerable()
                                           .Where(row => row.Field<string>("FileName") == currentfile);

                foreach (DataRow row in filteredRows)
                {
                    int trayNumber = Convert.ToInt32(row["Number"]);

                    DataRow newRow = dtRules.NewRow();
                    newRow["FileName"] = row["FileName"];
                    newRow["Number"] = trayNumber;
                    newRow["ShapeType"] = row["ShapeType"];
                    newRow["FromLength"] = row["FromLength"];
                    newRow["ToLength"] = row["ToLength"];
                    newRow["FromWidth"] = row["FromWidth"];
                    newRow["ToWidth"] = row["ToWidth"];

                    // LOOKUP MATCH: Match this specific row's tray number against your live dictionary data
                    if (activeTrayCounters.TryGetValue(trayNumber, out int dynamicCount))
                    {
                        newRow["DiamondCount"] = dynamicCount;
                    }
                    else
                    {
                        newRow["DiamondCount"] = 0; // Fallback default if no blinks tracked for this tray yet
                    }

                    dtRules.Rows.Add(newRow);
                }
            }
        }

        private void frmPrint_Load(object sender, EventArgs e)
        {
            try
            {
                currentfile = new ModifyRegistry().Read("cmbFile").ToString();
                LoadRulesDatabase();
            }
            catch
            {
                MessageBox.Show("Rules File not selected please select rules file");
            }


            foreach (string printer in PrinterSettings.InstalledPrinters)
            {
                cmbPrinters.Items.Add(printer);
            }

        }

        private void cmbPrinters_DrawItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index < 0) return;
            ComboBox combo = sender as ComboBox;
            string itemText = combo.Items[e.Index].ToString();
            System.Drawing.Color backColor;
            System.Drawing.Color foreColor;

            if ((e.State & DrawItemState.Selected) == DrawItemState.Selected)
            {
                backColor = System.Drawing.Color.FromArgb(255, 128, 0);
                foreColor = System.Drawing.Color.White;
            }
            else
            {
                backColor = combo.BackColor;
                foreColor = combo.ForeColor;
            }

            using (SolidBrush bgBrush = new SolidBrush(backColor))
            {
                e.Graphics.FillRectangle(bgBrush, e.Bounds);
            }
            using (SolidBrush textBrush = new SolidBrush(foreColor))
            {
                e.Graphics.DrawString(itemText, e.Font, textBrush, e.Bounds);
            }
            e.DrawFocusRectangle();
        }

        private void cmbPrinters_SelectedIndexChanged(object sender, EventArgs e)
        {
            targetedDevice = cmbPrinters.Text;
            new ModifyRegistry().Write("cmbPrinters", targetedDevice);
        }

        private void button3_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void btnMin_Click(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Minimized;
        }

        private void panel3_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
            }
        }
    }
}