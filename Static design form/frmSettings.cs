using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using uEye;

namespace opencvsharp
{
    public partial class frmSettings : Form
    {
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern bool ReleaseCapture();

        public const int WM_NCLBUTTONDOWN = 0xA1;
        public const int HT_CAPTION = 0x2;

        private DataTable dtRules;
        private readonly string xmlFilePath = Path.Combine(Application.StartupPath, "DiamondRules.xml");
        private DataView dvFilteredRules;
        Camera camera;

        private bool isInitializing = true;

        public frmSettings(uEye.Camera cam)
        {
            camera = cam;
            InitializeComponent();
            InitializeDataSchema();
            SetupUI();
        }

        private void SaveToXml()
        {
            try
            {
                dtRules.WriteXml(xmlFilePath, XmlWriteMode.WriteSchema);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to save to XML: {ex.Message}", "Save Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void InitializeDataSchema()
        {
            dtRules = new DataTable("Rule");

            // === NEW FIELD: FileName added as the first column ===
            dtRules.Columns.Add("FileName", typeof(string));
            dtRules.Columns.Add("Number", typeof(int));
            dtRules.Columns.Add("ShapeType", typeof(string));
            dtRules.Columns.Add("FromLength", typeof(double));
            dtRules.Columns.Add("ToLength", typeof(double));
            dtRules.Columns.Add("FromWidth", typeof(double));
            dtRules.Columns.Add("ToWidth", typeof(double));

            // This allows numbers 1-20 to repeat across different shapes AND different files safely
            dtRules.PrimaryKey = new DataColumn[] {dtRules.Columns["FileName"],dtRules.Columns["ShapeType"],dtRules.Columns["Number"]};

            if (File.Exists(xmlFilePath))
            {
                try
                {
                    dtRules.ReadXml(xmlFilePath);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error reading XML: {ex.Message}", "Data Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }

            dvFilteredRules = new DataView(dtRules);
            dgvRules.DataSource = dvFilteredRules;
            dgvRules.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        }

        private bool ValidateInputs(out int sqNum, out double fromLen, out double toLen, out double fromWid, out double toWid)
        {
            sqNum = 0; fromLen = 0; toLen = 0; fromWid = 0; toWid = 0;

            if (string.IsNullOrWhiteSpace(cmbFile.Text))
            {
                MessageBox.Show("Please select or enter a valid File Name profile.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            if (!int.TryParse(txtSquareNumber.Text, out sqNum) || sqNum < 1 || sqNum > 20)
            {
                MessageBox.Show("Square Number must be an integer from 1 to 20.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            if (!double.TryParse(txtFromLen.Text, out fromLen) || !double.TryParse(txtToLen.Text, out toLen))
            {
                MessageBox.Show("Please enter valid numeric lengths/diameters.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            if (fromLen > toLen)
            {
                MessageBox.Show("'From' value cannot be greater than 'To' value.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            if (cmbshape.SelectedItem.ToString() != "Round")
            {
                if (!double.TryParse(txtFromWid.Text, out fromWid) || !double.TryParse(txtToWid.Text, out toWid))
                {
                    MessageBox.Show("Please enter valid numeric widths.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return false;
                }
                if (fromWid > toWid)
                {
                    MessageBox.Show("'From Width' cannot be greater than 'To Width'.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return false;
                }
            }

            return true;
        }

        private void SetupUI()
        {
            isInitializing = true;

            PopulateFileComboBox();
            cmbshape.Items.Clear();
            cmbshape.Items.AddRange(new string[] { "Round", "Pear", "Marquise", "Heart", "Oval", "General", "Poly", "GeneralC" });
            cmbshape.SelectedIndex = 0;

            // 3. Attach Events
            cmbshape.SelectedIndexChanged += cmbshape_SelectedIndexChanged;
            cmbFile.SelectedIndexChanged += cmbFile_SelectedIndexChanged;

            isInitializing = false;

            // Run initial filter
            UpdateTextBoxStates();
        }

        private void PopulateFileComboBox()
        {
            string currentSelection = cmbFile.SelectedItem?.ToString();

            cmbFile.Items.Clear();

            var uniqueFiles = dtRules.AsEnumerable().Select(row => row.Field<string>("FileName")).Where(name => !string.IsNullOrEmpty(name)).Distinct().ToList();

            if (!uniqueFiles.Contains("Default_Profile")) uniqueFiles.Insert(0, "Default_Profile");

            foreach (var file in uniqueFiles)
            {
                cmbFile.Items.Add(file);
            }

            if (!string.IsNullOrEmpty(currentSelection) && cmbFile.Items.Contains(currentSelection))
                cmbFile.SelectedItem = currentSelection;
            else
                cmbFile.SelectedIndex = 0;
        }

        private void UpdateTextBoxStates()
        {
            if (isInitializing || cmbshape.SelectedItem == null || cmbFile.SelectedItem == null || dvFilteredRules == null) return;

            string selectedFile = cmbFile.SelectedItem.ToString();
            string selectedShape = cmbshape.SelectedItem.ToString();
            bool isRound = selectedShape == "Round";

            dvFilteredRules.RowFilter = $"FileName = '{selectedFile.Replace("'", "''")}' AND ShapeType = '{selectedShape}'";

            if (isRound)
            {
                lblFromLength.Text = "From Diameter:";
                lblToLength.Text = "To Diameter:";

                txtFromLen.Enabled = true;
                txtToLen.Enabled = true;
                txtFromWid.Enabled = false;
                txtToWid.Enabled = false;

                txtFromWid.Text = "0.0";
                txtToWid.Text = "0.0";
            }
            else
            {
                lblFromLength.Text = "From Length:";
                lblToLength.Text = "To Length:";

                txtFromLen.Enabled = true;
                txtToLen.Enabled = true;
                txtFromWid.Enabled = true;
                txtToWid.Enabled = true;
            }
        }

        private void cmbshape_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateTextBoxStates();
        }

        private void cmbFile_SelectedIndexChanged(object sender, EventArgs e)
        {
            new ModifyRegistry().Write("cmbFile", cmbFile.Text);
            UpdateTextBoxStates();
            if (this.Owner is FrmAuto autofrm)
            {
                autofrm.resetCounts();
            }

        }

        private void btnTextAdd_Click(object sender, EventArgs e)
        {
            if (!ValidateInputs(out int sqNum, out double fromLen, out double toLen, out double fromWid, out double toWid))
                return;

            string currentFile = cmbFile.Text.Trim(); // Allows users to type custom names directly into the dropdown!
            string currentShape = cmbshape.SelectedItem.ToString();

            object[] compositeKeySearchValues = new object[] { currentFile, currentShape, sqNum };
            if (dtRules.Rows.Contains(compositeKeySearchValues))
            {
                MessageBox.Show($"Square Number {sqNum} already exists for '{currentShape}' inside the '{currentFile}' profile.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                DataRow newRow = dtRules.NewRow();
                newRow["FileName"] = currentFile;
                newRow["Number"] = sqNum;
                newRow["ShapeType"] = currentShape;
                newRow["FromLength"] = fromLen;
                newRow["ToLength"] = toLen;
                newRow["FromWidth"] = fromWid;
                newRow["ToWidth"] = toWid;

                dtRules.Rows.Add(newRow);
                SaveToXml();

                PopulateFileComboBox();
                cmbFile.Text = currentFile;
                new ModifyRegistry().Write("cmbFile", cmbFile.Text);
                //MessageBox.Show($"Rule #{sqNum} added successfully for {currentShape} under {currentFile}!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex) { MessageBox.Show($"Error adding data: {ex.Message}"); }
        }

        private void btnTextUpdate_Click(object sender, EventArgs e)
        {
            if (!ValidateInputs(out int sqNum, out double fromLen, out double toLen, out double fromWid, out double toWid))
                return;

            string currentFile = cmbFile.Text.Trim();
            string currentShape = cmbshape.SelectedItem.ToString();

            object[] compositeKeySearchValues = new object[] { currentFile, currentShape, sqNum };
            DataRow targetRow = dtRules.Rows.Find(compositeKeySearchValues);

            if (targetRow == null)
            {
                MessageBox.Show($"Square Number {sqNum} was not found under '{currentShape}' for file '{currentFile}'.", "Search Failure", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                targetRow.BeginEdit();
                targetRow["FromLength"] = fromLen;
                targetRow["ToLength"] = toLen;
                targetRow["FromWidth"] = fromWid;
                targetRow["ToWidth"] = toWid;
                targetRow.EndEdit();

                SaveToXml();
                new ModifyRegistry().Write("cmbFile", cmbFile.Text);
                //MessageBox.Show("Rule updated successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex) { targetRow.CancelEdit(); MessageBox.Show($"Error updating data: {ex.Message}"); }
        }

        private void btnTextDelete_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtSquareNumber.Text) || !int.TryParse(txtSquareNumber.Text, out int sqNum))
            {
                MessageBox.Show("Select a valid row grid entry or input a target Square Number to delete.", "Input Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string currentFile = cmbFile.Text.Trim();
            string currentShape = cmbshape.SelectedItem.ToString();

            object[] compositeKeySearchValues = new object[] { currentFile, currentShape, sqNum };
            DataRow targetRow = dtRules.Rows.Find(compositeKeySearchValues);

            if (targetRow == null)
            {
                MessageBox.Show($"Square Number {sqNum} does not exist under '{currentShape}' in profile '{currentFile}'.", "Not Found", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            DialogResult confirm = MessageBox.Show($"Are you sure you want to delete Square Number {sqNum} from '{currentShape}' inside configuration '{currentFile}'?", "Confirm Deletion", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (confirm == DialogResult.Yes)
            {
                targetRow.Delete();
                SaveToXml();
                PopulateFileComboBox(); // Refresh listing options in case a profile went obsolete
                //MessageBox.Show("Rule configuration deleted successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (this.Owner is FrmAuto autofrm)
            {
                autofrm.RefreshActiveRules(cmbFile.Text.Trim());
            }
            this.Close();
        }

        private void btnMin_Click(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Minimized;
        }

        private void cmbshape_DrawItem(object sender, DrawItemEventArgs e)
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

        private void panel3_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
            }
        }

        private void btnCamera_Click(object sender, EventArgs e)
        {
            new Camera_Setting1(camera).Show();
        }

        private void btnDeleteEntireFile_Click(object sender, EventArgs e)
        {
            string currentFile = cmbFile.Text.Trim();

            if (string.IsNullOrWhiteSpace(currentFile))
            {
                MessageBox.Show("Please select or type a valid file profile name to delete.", "Input Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Find all rows matching the selected file name
            DataRow[] rowsToDelete = dtRules.Select($"FileName = '{currentFile.Replace("'", "''")}'");

            if (rowsToDelete.Length == 0)
            {
                MessageBox.Show($"No data records found for the file profile '{currentFile}'.", "Not Found", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Prompt for confirmation
            DialogResult confirm = MessageBox.Show(
                $"Are you sure you want to completely delete the file '{currentFile}'?\nThis will permanently wipe out ALL shapes and rules ({rowsToDelete.Length} rows) inside this profile.",
                "Confirm Entire File Deletion",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (confirm == DialogResult.Yes)
            {
                try
                {
                    // Delete all matching rows out of the main DataTable
                    foreach (DataRow row in rowsToDelete)
                    {
                        row.Delete();
                    }

                    SaveToXml();
                    PopulateFileComboBox(); // Refresh listing options to drop the deleted profile name

                    //MessageBox.Show($"File profile '{currentFile}' and all its underlying data configurations have been deleted.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error while removing file profile: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void btnResetShapeData_Click(object sender, EventArgs e)
        {
            string currentFile = cmbFile.Text.Trim();

            if (cmbshape.SelectedItem == null || string.IsNullOrWhiteSpace(currentFile))
            {
                MessageBox.Show("Please select both a valid file profile and a shape target to reset.", "Input Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string currentShape = cmbshape.SelectedItem.ToString();

            // Find rows matching both File AND Shape criteria
            DataRow[] rowsToReset = dtRules.Select($"FileName = '{currentFile.Replace("'", "''")}' AND ShapeType = '{currentShape}'");

            if (rowsToReset.Length == 0)
            {
                MessageBox.Show($"There is no shape data to reset for '{currentShape}' inside the '{currentFile}' profile.", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Prompt for confirmation
            DialogResult confirm = MessageBox.Show(
                $"Are you sure you want to reset all '{currentShape}' data within the '{currentFile}' profile?\nThis will clear all 1-20 square number allocations for this shape only.",
                "Confirm Shape Data Reset",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (confirm == DialogResult.Yes)
            {
                try
                {
                    // Wipe out rules for this shape layout only
                    foreach (DataRow row in rowsToReset)
                    {
                        row.Delete();
                    }

                    SaveToXml();
                    UpdateTextBoxStates(); // Instantly update DataGridView UI layout to show a clean grid

                    //MessageBox.Show($"Shape records for '{currentShape}' inside configuration '{currentFile}' have been reset successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error while resetting shape configuration: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void dgvRules_SelectionChanged(object sender, EventArgs e)
        {
            if (dgvRules.CurrentRow != null && dgvRules.CurrentRow.Index >= 0)
            {
                DataRowView currentPosition = dgvRules.CurrentRow.DataBoundItem as DataRowView;
                if (currentPosition != null)
                {
                    DataRow row = currentPosition.Row;
                    txtSquareNumber.Text = row["Number"].ToString();
                    cmbFile.SelectedItem = row["FileName"].ToString();
                    cmbshape.SelectedItem = row["ShapeType"].ToString();
                    txtFromLen.Text = row["FromLength"].ToString();
                    txtToLen.Text = row["ToLength"].ToString();
                    txtFromWid.Text = row["FromWidth"].ToString();
                    txtToWid.Text = row["ToWidth"].ToString();
                }
            }
        }
    }
}