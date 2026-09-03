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

namespace Matric_scope
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
        
        private bool isInitializing = true;
        private bool isSavingChanges = false;
        
        public frmSettings()
        {
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

            dtRules.Columns.Add("FileName", typeof(string));
            dtRules.Columns.Add("Number", typeof(int));
            //dtRules.Columns.Add("ShapeType", typeof(string));
            dtRules.Columns.Add("FromLength", typeof(double));
            dtRules.Columns.Add("ToLength", typeof(double));
            dtRules.Columns.Add("FromWidth", typeof(double));
            dtRules.Columns.Add("ToWidth", typeof(double));

            dtRules.PrimaryKey = new DataColumn[] { dtRules.Columns["FileName"], dtRules.Columns["Number"] };

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

            // Configure Grid Settings for Direct In-Grid Editing
            dgvRules.AllowUserToAddRows = false;
            dgvRules.AllowUserToDeleteRows = false;

            // Wire up grid edit events
            dgvRules.CellValueChanged += dgvRules_CellValueChanged;
        }

        private void SetupUI()
        {
            isInitializing = true;

            PopulateFileComboBox();

            cmbFile.SelectedIndexChanged += cmbFile_SelectedIndexChanged;

            isInitializing = false;

            UpdateGridStates();
        }

        private void PopulateFileComboBox()
        {
            cmbFile.Items.Clear();

            // 1. Get unique file names from the DataTable
            var uniqueFiles = dtRules.AsEnumerable()
                .Select(row => row.Field<string>("FileName"))
                .Where(name => !string.IsNullOrEmpty(name))
                .Distinct()
                .ToList();

            if (!uniqueFiles.Contains("Default_Profile"))
                uniqueFiles.Insert(0, "Default_Profile");

            // 2. Populate the ComboBox
            foreach (var file in uniqueFiles)
            {
                cmbFile.Items.Add(file);
            }

            // 3. Read the previously selected profile from the Registry
            string savedProfile = "Default_Profile"; // Fallback default
            try
            {
                object regVal = new ModifyRegistry().Read("cmbFile");
                if (regVal != null && !string.IsNullOrWhiteSpace(regVal.ToString()))
                {
                    savedProfile = regVal.ToString();
                }
            }
            catch
            {
                // Ignore registry read errors and use the default
            }

            // 4. Apply the selection
            if (cmbFile.Items.Contains(savedProfile))
            {
                cmbFile.SelectedItem = savedProfile;
            }
            else if (cmbFile.Items.Count > 0)
            {
                // Fallback to the first item if the registry value was deleted or not found
                cmbFile.SelectedIndex = 0;
            }
        }

        private void EnsureTwentyRowsExist(string fileName)
        {
            if (string.IsNullOrEmpty(fileName)) return;

            bool dataAdded = false;

            for (int i = 1; i <= 20; i++)
            {
                object[] key = new object[] { fileName,i };
                DataRow existingRow = dtRules.Rows.Find(key);

                if (existingRow == null)
                {
                    DataRow newRow = dtRules.NewRow();
                    newRow["FileName"] = fileName;           // Automatically filled
                    newRow["Number"] = i;                   // Automatically filled (1 to 20)
                    //newRow["ShapeType"] = DefaultShape;     // Automatically filled
                    newRow["FromLength"] = 0.0;
                    newRow["ToLength"] = 0.0;
                    newRow["FromWidth"] = 0.0;
                    newRow["ToWidth"] = 0.0;

                    dtRules.Rows.Add(newRow);
                    dataAdded = true;
                }
                else
                {
                    // Ensure existing rows always have the correct automated FileName mapping if switched
                    if (existingRow["FileName"].ToString() != fileName)
                    {
                        existingRow["FileName"] = fileName;
                        dataAdded = true;
                    }
                }
            }

            if (dataAdded)
            {
                SaveToXml();
            }
        }

        private void UpdateGridStates()
        {
            if (isInitializing || cmbFile.SelectedItem == null || dvFilteredRules == null) return;

            string selectedFile = cmbFile.SelectedItem.ToString();

            // Ensure 1 to 20 rows are automatically generated and mapped for this file
            EnsureTwentyRowsExist(selectedFile);

            dvFilteredRules.RowFilter = $"FileName = '{selectedFile.Replace("'", "''")}'";
            dvFilteredRules.Sort = "Number ASC";

            // Lock automated/system columns so users cannot manually type into them
            if (dgvRules.Columns["FileName"] != null) dgvRules.Columns["FileName"].ReadOnly = true;
            //if (dgvRules.Columns["ShapeType"] != null) dgvRules.Columns["ShapeType"].ReadOnly = true;
            if (dgvRules.Columns["Number"] != null) dgvRules.Columns["Number"].ReadOnly = true;

            // Keep all four length and width columns visible and editable
            if (dgvRules.Columns["FromLength"] != null) dgvRules.Columns["FromLength"].Visible = true;
            if (dgvRules.Columns["ToLength"] != null) dgvRules.Columns["ToLength"].Visible = true;
            if (dgvRules.Columns["FromWidth"] != null) dgvRules.Columns["FromWidth"].Visible = true;
            if (dgvRules.Columns["ToWidth"] != null) dgvRules.Columns["ToWidth"].Visible = true;
        }

        private void cmbFile_SelectedIndexChanged(object sender, EventArgs e)
        {
            new ModifyRegistry().Write("cmbFile", cmbFile.Text);
            UpdateGridStates();
            if (this.Owner is FrmAuto autofrm)
            {
                autofrm.resetCounts();
            }
        }

        private void dgvRules_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (isInitializing || isSavingChanges) return;
            if (e.RowIndex < 0) return;

            try
            {
                isSavingChanges = true;
                dgvRules.EndEdit();
                SaveToXml();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving grid change: {ex.Message}", "Save Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                isSavingChanges = false;
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            //if (this.Owner is FrmAuto autofrm)
            {
                FrmAuto.RefreshActiveRules(cmbFile.Text.Trim());
            }
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

        private void btnDeleteEntireFile_Click(object sender, EventArgs e)
        {
            string currentFile = cmbFile.Text.Trim();

            if (string.IsNullOrWhiteSpace(currentFile) || currentFile.Equals("Default_Profile", StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show("Please select a valid custom file profile to delete. 'Default_Profile' cannot be completely removed.", "Input Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            DataRow[] rowsToDelete = dtRules.Select($"FileName = '{currentFile.Replace("'", "''")}'");

            if (rowsToDelete.Length == 0)
            {
                MessageBox.Show($"No data records found for the file profile '{currentFile}'.", "Not Found", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            DialogResult confirm = MessageBox.Show(
                $"Are you sure you want to completely delete the file profile '{currentFile}'?\nThis will permanently wipe out all corresponding rule entries.",
                "Confirm Entire File Deletion",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (confirm == DialogResult.Yes)
            {
                try
                {
                    foreach (DataRow row in rowsToDelete)
                    {
                        row.Delete();
                    }
                    SaveToXml();
                    PopulateFileComboBox(); // Refreshes dropdown list and switches back to default
                    UpdateGridStates();
                    MessageBox.Show($"File profile '{currentFile}' has been successfully deleted.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
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

            if (string.IsNullOrWhiteSpace(currentFile))
            {
                MessageBox.Show("Please select a valid file profile to reset.", "Input Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            DataRow[] rowsToReset = dtRules.Select($"FileName = '{currentFile.Replace("'", "''")}'");

            if (rowsToReset.Length == 0)
            {
                MessageBox.Show($"There is no data to reset inside the '{currentFile}' profile.", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            DialogResult confirm = MessageBox.Show(
                $"Are you sure you want to reset all data within the '{currentFile}' profile?\nThis will clear all 1-20 square number allocations.",
                "Confirm Data Reset",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (confirm == DialogResult.Yes)
            {
                try
                {
                    foreach (DataRow row in rowsToReset)
                    {
                        row.Delete();
                    }

                    SaveToXml();
                    UpdateGridStates();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error while resetting configuration: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void btnTextDelete_Click(object sender, EventArgs e)
        {
            if (dgvRules.CurrentRow == null || dgvRules.CurrentRow.Index < 0)
            {
                MessageBox.Show("Please select a row in the grid to clear.", "Selection Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            DataRowView drv = dgvRules.CurrentRow.DataBoundItem as DataRowView;
            if (drv != null)
            {
                int squareNum = Convert.ToInt32(drv["Number"]);
                DialogResult confirm = MessageBox.Show(
                    $"Are you sure you want to clear the values for Square Number {squareNum}?",
                    "Confirm Row Clear",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (confirm == DialogResult.Yes)
                {
                    drv.BeginEdit();
                    drv["FromLength"] = 0.0;
                    drv["ToLength"] = 0.0;
                    drv["FromWidth"] = 0.0;
                    drv["ToWidth"] = 0.0;
                    drv.EndEdit();

                    SaveToXml();
                    dgvRules.Refresh(); // Forces immediate UI grid refresh layout
                    MessageBox.Show("Row values cleared successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }

        private void btnTextAdd_Click(object sender, EventArgs e)
        {
            string newFileName = cmbFile.Text.Trim();

            if (string.IsNullOrWhiteSpace(newFileName) || newFileName.Equals("Default_Profile", StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show("Please type a valid, unique file profile name into the dropdown box first.", "Input Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                cmbFile.Focus();
                return;
            }

            // Check if file already exists in the combo items
            if (!cmbFile.Items.Contains(newFileName))
            {
                cmbFile.Items.Add(newFileName);
            }

            cmbFile.SelectedItem = newFileName;

            // Automatically generate 1-20 empty rows for this new file profile
            EnsureTwentyRowsExist(newFileName);
            UpdateGridStates();
            dgvRules.Refresh();
            MessageBox.Show($"File profile '{newFileName}' created with 20 rows successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void btnTextUpdate_Click(object sender, EventArgs e)
        {
            try
            {
                dgvRules.EndEdit();
                SaveToXml();
                UpdateGridStates();
                dgvRules.Refresh();
                MessageBox.Show("All changes saved and updated successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving changes: {ex.Message}", "Save Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void cmbFile_DrawItem(object sender, DrawItemEventArgs e)
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
    }
}