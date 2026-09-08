using System.Data.SQLite;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
namespace Matric_scope
{
    public partial class frmHistory : Form
    {
        private const string ConnectionString = "Data Source=History.db";
        private DataTable dataTable = new DataTable();
        private bool isInitializingFilters = false;

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern bool ReleaseCapture();

        public const int WM_NCLBUTTONDOWN = 0xA1;
        public const int HT_CAPTION = 0x2;

        public frmHistory()
        {
            InitializeComponent();

            this.Load += frmHistory_Load;
            dgvGemstones.SelectionChanged += dgvGemstones_SelectionChanged;

            // Wire up real-time filter layout listeners
            cmbFilterShape.SelectedIndexChanged += ApplyMultiFieldFilter;
            dtpFilterDate.ValueChanged += ApplyMultiFieldFilter;
            chkUseDateFilter.CheckedChanged += ApplyMultiFieldFilter;
            txtFilterLength.TextChanged += ApplyMultiFieldFilter;
            txtFilterWidth.TextChanged += ApplyMultiFieldFilter;
        }

        private void ApplyMultiFieldFilter(object sender, EventArgs e)
        {
            if (isInitializingFilters) return;

            try
            {
                string rowFilterString = "1=1"; // Base string template to safely append criteria paths

                // Evaluate ComboBox selection
                if (cmbFilterShape.SelectedIndex > 0) // Ignores "-- All Shapes --"
                {
                    string selectedShape = cmbFilterShape.SelectedItem.ToString().Replace("'", "''");
                    rowFilterString += $" AND Shape = '{selectedShape}'";
                }

                // Evaluate DateTimePicker using toggle confirmation
                if (chkUseDateFilter.Checked)
                {
                    string formattedDate = dtpFilterDate.Value.ToString("yyyy-MM-dd");
                    rowFilterString += $" AND Convert(Date, 'System.String') LIKE '%{formattedDate}%'";
                }

                // Numeric threshold validations
                if (double.TryParse(txtFilterLength.Text, out double minLength))
                {
                    rowFilterString += $" AND Length >= {minLength}";
                }
                if (double.TryParse(txtFilterWidth.Text, out double minWidth))
                {
                    rowFilterString += $" AND Width >= {minWidth}";
                }

                rowFilterString += "ORDER BY ID DESC";

                // Execute the generated dynamic string mapping assignment directly into the UI DataView
                dataTable.DefaultView.RowFilter = rowFilterString;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Filtering operation exception trace: {ex.Message}");
            }
        }

        private void dgvGemstones_SelectionChanged(object sender, EventArgs e)
        {
            if (dgvGemstones.CurrentRow != null)
            {
                string imagePath = dgvGemstones.CurrentRow.Cells["Image"].Value?.ToString();

                // 1. Clear out old memory references and drop the Tag path
                if (picPreview.Image != null)
                {
                    picPreview.Image.Dispose();
                    picPreview.Image = null;
                }
                picPreview.Tag = null;

                // 2. Load the file stream safely if it physically exists on disk
                if (!string.IsNullOrEmpty(imagePath) && File.Exists(imagePath))
                {
                    using (FileStream fs = new FileStream(imagePath, FileMode.Open, FileAccess.Read))
                    {
                        picPreview.Image = System.Drawing.Image.FromStream(fs);
                    }

                    // Store the path string right inside the PictureBox for quick access later
                    picPreview.Tag = imagePath;
                }
            }
        }

        private void frmHistory_Load(object sender, EventArgs e)
        {
            LoadData();
            PopulateShapeFilter();

            // Default setup for date picker layout control
            dtpFilterDate.Format = DateTimePickerFormat.Short;
            chkUseDateFilter.Checked = false; // Do not filter by date initially
        }

        private void LoadData()
        {
            using (var connection = new SQLiteConnection(ConnectionString))
            {
                connection.Open();
                string selectSql = "SELECT ID, Date, Shape, Image, Length, Width FROM Records ORDER BY ID DESC;";

                using (var command = new SQLiteCommand(selectSql, connection))
                {
                    // Execute a simple reader and load it directly into your table container
                    using (var reader = command.ExecuteReader())
                    {
                        dataTable.Clear();
                        dataTable.Load(reader); // Automatically structures columns and data types
                    }
                }
            }

            dgvGemstones.DataSource = dataTable;

            if (dgvGemstones.Columns.Contains("Image"))
            {
                dgvGemstones.Columns["Image"].Visible = false;
            }

            // ADD THE CHECKBOX COLUMN
            if (!dgvGemstones.Columns.Contains("chkSelect"))
            {
                DataGridViewCheckBoxColumn chkCol = new DataGridViewCheckBoxColumn();
                chkCol.Name = "chkSelect";
                chkCol.HeaderText = "Select";
                chkCol.Width = 50;
                chkCol.ReadOnly = false; // Must be false so users can check it
                dgvGemstones.Columns.Insert(0, chkCol);
            }

            // Ensure the grid allows editing for the checkbox, but keep other columns Read-Only
            dgvGemstones.ReadOnly = false;
            foreach (DataGridViewColumn col in dgvGemstones.Columns)
            {
                if (col.Name != "chkSelect")
                {
                    col.ReadOnly = true;
                }
            }
        }

        /*private void LoadData()
        {
            using (var connection = new SQLiteConnection(ConnectionString))
            {
                connection.Open();
                string selectSql = "SELECT ID, Date, Shape, Image, Length, Width FROM Records ORDER BY ID DESC;";

                using (var command = new SQLiteCommand(selectSql, connection))
                {
                    // Execute a simple reader and load it directly into your table container
                    using (var reader = command.ExecuteReader())
                    {
                        dataTable.Clear();
                        dataTable.Load(reader); // Automatically structures columns and data types
                    }
                }
            }

            dgvGemstones.DataSource = dataTable;

            if (dgvGemstones.Columns.Contains("Image"))
            {
                dgvGemstones.Columns["Image"].Visible = false;
            }
        }
        */
        // 3. POPULATE UNIQUE SHAPES IN COMBOBOX
        private void PopulateShapeFilter()
        {
            isInitializingFilters = true;
            cmbFilterShape.Items.Clear();
            cmbFilterShape.Items.Add("-- All Shapes --"); // Index 0 handles reset logic seamlessly

            using (var connection = new SQLiteConnection(ConnectionString))
            {
                connection.Open();
                string distinctQuery = "SELECT DISTINCT Shape FROM Records WHERE Shape IS NOT NULL AND Shape != '' ORDER BY Shape ASC;";

                using (var command = new SQLiteCommand(distinctQuery, connection))
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        cmbFilterShape.Items.Add(reader.GetString(0));
                    }
                }
            }

            cmbFilterShape.SelectedIndex = 0; // Default selection to show all records
            isInitializingFilters = false;
        }

        private void btnViewFull_Click(object sender, EventArgs e)
        {
            if (dgvGemstones.CurrentRow == null)
            {
                MessageBox.Show("Please select a record from the grid row layout grid first.", "Selection Needed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string imagePath = dgvGemstones.CurrentRow.Cells["Image"].Value?.ToString();

            if (!string.IsNullOrEmpty(imagePath) && File.Exists(imagePath))
            {
                Form viewerForm = new Form();
                viewerForm.Text = $"Full Scan View - Shape: {dgvGemstones.CurrentRow.Cells["Shape"].Value}";
                viewerForm.Size = new System.Drawing.Size(800, 600);
                viewerForm.StartPosition = FormStartPosition.CenterParent;

                PictureBox pbFull = new PictureBox();
                pbFull.Dock = DockStyle.Fill;
                pbFull.SizeMode = PictureBoxSizeMode.Zoom;

                using (FileStream fs = new FileStream(imagePath, FileMode.Open, FileAccess.Read))
                {
                    pbFull.Image = System.Drawing.Image.FromStream(fs);
                }

                viewerForm.Controls.Add(pbFull);
                viewerForm.FormClosed += (s, args) => pbFull.Image?.Dispose();
                viewerForm.ShowDialog(this);
            }
            else
            {
                MessageBox.Show("The physical asset cannot be found at that disk mapping location.", "File Missing", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnExportExcel_Click(object sender, EventArgs e)
        {
            DataView recordsToExport = dataTable.DefaultView;
            if (recordsToExport.Count == 0)
            {
                MessageBox.Show("There are no history records matching the current filters.",
                    "Nothing to Export", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using (SaveFileDialog dialog = new SaveFileDialog())
            {
                dialog.Title = "Export History to Excel";
                dialog.Filter = "Excel Workbook (*.xlsx)|*.xlsx";
                dialog.DefaultExt = "xlsx";
                dialog.AddExtension = true;
                dialog.FileName = $"Measurement_History_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

                if (dialog.ShowDialog(this) != DialogResult.OK) return;

                try
                {
                    ExportHistoryWorkbook(dialog.FileName, recordsToExport);
                    MessageBox.Show($"Exported {recordsToExport.Count} record(s) successfully.",
                        "Export Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"The Excel file could not be created.\n\n{ex.Message}",
                        "Export Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private static void ExportHistoryWorkbook(string filePath, DataView records)
        {
            string[] columns = { "ID", "Date", "Shape", "Length", "Width" };

            using (FileStream stream = new FileStream(filePath, FileMode.Create, FileAccess.ReadWrite, FileShare.None))
            using (ZipArchive archive = new ZipArchive(stream, ZipArchiveMode.Create))
            {
                WriteZipEntry(archive, "[Content_Types].xml",
                    "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
                    "<Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\">" +
                    "<Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/>" +
                    "<Default Extension=\"xml\" ContentType=\"application/xml\"/>" +
                    "<Override PartName=\"/xl/workbook.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml\"/>" +
                    "<Override PartName=\"/xl/worksheets/sheet1.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/>" +
                    "</Types>");

                WriteZipEntry(archive, "_rels/.rels",
                    "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
                    "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
                    "<Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\" Target=\"xl/workbook.xml\"/>" +
                    "</Relationships>");

                WriteZipEntry(archive, "xl/workbook.xml",
                    "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
                    "<workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\">" +
                    "<sheets><sheet name=\"Measurement History\" sheetId=\"1\" r:id=\"rId1\"/></sheets></workbook>");

                WriteZipEntry(archive, "xl/_rels/workbook.xml.rels",
                    "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
                    "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
                    "<Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet1.xml\"/>" +
                    "</Relationships>");

                ZipArchiveEntry worksheetEntry = archive.CreateEntry("xl/worksheets/sheet1.xml", CompressionLevel.Optimal);
                using (Stream worksheetStream = worksheetEntry.Open())
                using (XmlWriter writer = XmlWriter.Create(worksheetStream, new XmlWriterSettings { Encoding = new UTF8Encoding(false) }))
                {
                    writer.WriteStartDocument(true);
                    writer.WriteStartElement("worksheet", "http://schemas.openxmlformats.org/spreadsheetml/2006/main");
                    writer.WriteStartElement("sheetData");
                    WriteExcelRow(writer, columns.Cast<object>());

                    foreach (DataRowView record in records)
                    {
                        WriteExcelRow(writer, columns.Select(column => record.Row.Table.Columns.Contains(column) ? record[column] : ""));
                    }

                    writer.WriteEndElement();
                    writer.WriteEndElement();
                    writer.WriteEndDocument();
                }
            }
        }

        private static void WriteZipEntry(ZipArchive archive, string entryName, string contents)
        {
            ZipArchiveEntry entry = archive.CreateEntry(entryName, CompressionLevel.Optimal);
            using (StreamWriter writer = new StreamWriter(entry.Open(), new UTF8Encoding(false)))
            {
                writer.Write(contents);
            }
        }

        private static void WriteExcelRow(XmlWriter writer, IEnumerable<object> values)
        {
            writer.WriteStartElement("row");
            foreach (object value in values)
            {
                writer.WriteStartElement("c");
                writer.WriteAttributeString("t", "inlineStr");
                writer.WriteStartElement("is");
                writer.WriteElementString("t", value == null || value == DBNull.Value ? "" : Convert.ToString(value));
                writer.WriteEndElement();
                writer.WriteEndElement();
            }
            writer.WriteEndElement();
        }

        private void btnDelete_Click(object sender, EventArgs e)
        {
            // 1. Gather all selected IDs from the checkbox column
            List<string> idsToDelete = new List<string>();

            foreach (DataGridViewRow row in dgvGemstones.Rows)
            {
                // Ensure the row isn't a new uncommitted row and the checkbox is checked
                if (!row.IsNewRow && row.Cells["chkSelect"].Value != null && Convert.ToBoolean(row.Cells["chkSelect"].Value) == true)
                {
                    idsToDelete.Add(row.Cells["ID"].Value.ToString());
                }
            }

            // 2. Validate selection
            if (idsToDelete.Count == 0)
            {
                MessageBox.Show("Please check the 'Select' box for at least one record to delete.", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // 3. Confirm Deletion
            DialogResult confirmResult = MessageBox.Show(
                $"Are you sure you want to permanently delete {idsToDelete.Count} selected record(s)?",
                "Confirm Delete",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question
            );

            if (confirmResult == DialogResult.Yes)
            {
                // Clear the preview image in case we delete the one currently being viewed
                if (picPreview.Image != null)
                {
                    picPreview.Image.Dispose();
                    picPreview.Image = null;
                }
                picPreview.Tag = null;

                // 4. Delete from Database using a Transaction for speed and safety
                using (var connection = new SQLiteConnection(ConnectionString))
                {
                    connection.Open();
                    using (var transaction = connection.BeginTransaction())
                    {
                        string deleteSql = "DELETE FROM Records WHERE ID = @id;";

                        using (var command = new SQLiteCommand(deleteSql, connection, transaction))
                        {
                            command.Parameters.Add("@id", DbType.String); // Setup parameter once

                            foreach (string gemstoneId in idsToDelete)
                            {
                                command.Parameters["@id"].Value = gemstoneId;
                                command.ExecuteNonQuery();
                            }
                        }

                        transaction.Commit(); // Commit all deletes at once
                    }
                }

                // 5. Refresh UI
                LoadData();
                PopulateShapeFilter(); // Recalculate unique lists inside the ComboBox dropdown
                MessageBox.Show($"{idsToDelete.Count} record(s) cleared cleanly from database.", "Deleted", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        /*private void btnDelete_Click(object sender, EventArgs e)
        {
            if (dgvGemstones.CurrentRow == null)
            {
                MessageBox.Show("Please select the target row record to delete.", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string gemstoneId = dgvGemstones.CurrentRow.Cells["ID"].Value?.ToString();
            string shapeName = dgvGemstones.CurrentRow.Cells["Shape"].Value?.ToString();

            DialogResult confirmResult = MessageBox.Show(
                $"Are you sure you want to permanently delete record ID: {gemstoneId} ({shapeName})?",
                "Confirm Delete",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question
            );

            if (confirmResult == DialogResult.Yes)
            {
                if (picPreview.Image != null)
                {
                    picPreview.Image.Dispose();
                    picPreview.Image = null;
                }

                using (var connection = new SQLiteConnection(ConnectionString))
                {
                    connection.Open();
                    string deleteSql = "DELETE FROM Records WHERE ID = @id;";

                    using (var command = new SQLiteCommand(deleteSql, connection))
                    {
                        command.Parameters.AddWithValue("@id", gemstoneId);
                        command.ExecuteNonQuery();
                    }
                }

                LoadData();
                PopulateShapeFilter(); // Recalculate unique lists inside the ComboBox dropdown
                MessageBox.Show("Record cleared cleanly from database.", "Deleted", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
        */
        
        private void panel3_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
            }
        }

        private void btnMin_Click(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Minimized;
        }

        private void button3_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void cmbFilterShape_DrawItem(object sender, DrawItemEventArgs e)
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

        private void picPreview_Click(object sender, EventArgs e)
        {
            string targetPath = picPreview.Tag?.ToString();

            // 2. Ensure a path is present and the file hasn't been moved or deleted
            if (!string.IsNullOrEmpty(targetPath) && File.Exists(targetPath))
            {
                try
                {
                    // Prepare the process launch settings
                    System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = targetPath,
                        UseShellExecute = true // This forces Windows to use the default registered app handler
                    };

                    System.Diagnostics.Process.Start(startInfo);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Could not open default image viewer application: {ex.Message}",
                                    "System Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                MessageBox.Show("No image file is currently loaded, or the source file was moved from its directory.",
                                "Asset Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            var result = MessageBox.Show(
        "This will permanently delete ALL custom shapes and their image files.\n\nAre you sure?",
        "Delete All Shapes",
        MessageBoxButtons.YesNo,
        MessageBoxIcon.Warning);

            if (result != DialogResult.Yes)
                return;

            try
            {
                // Delete image files
                string folder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "CapturedGemstones");

                if (Directory.Exists(folder))
                {
                    foreach (string file in Directory.GetFiles(folder))
                    {
                        try
                        {
                            File.Delete(file);
                        }
                        catch
                        {
                            // Ignore files that cannot be deleted
                        }
                    }
                }

                // Delete all database records
                using (var connection = new SQLiteConnection(ConnectionString))
                {
                    connection.Open();
                    string deleteSql = "DELETE FROM Records;";

                    using (var command = new SQLiteCommand(deleteSql, connection))
                    {
                        command.ExecuteNonQuery();
                    }
                }

                // Clear current selection
                //activeCustomShape = null;
                //currentMode = MeasurementMode.None;

                picPreview.Image?.Dispose();
                picPreview.Image = null;
                label1.Text = "No Custom Shape Selected";

                // Refresh menu
                LoadData();
                PopulateShapeFilter();

                MessageBox.Show("All custom shapes have been deleted.",
                                "Completed",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message,
                                "Error",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Error);
            }
        }
    }
}
