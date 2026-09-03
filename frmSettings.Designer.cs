namespace Matric_scope
{
    partial class frmSettings
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle2 = new System.Windows.Forms.DataGridViewCellStyle();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(frmSettings));
            this.dgvRules = new System.Windows.Forms.DataGridView();
            this.panel3 = new System.Windows.Forms.Panel();
            this.label1 = new System.Windows.Forms.Label();
            this.cmbFile = new System.Windows.Forms.ComboBox();
            this.btnDeleteEntireFile = new System.Windows.Forms.Button();
            this.pictureBox2 = new System.Windows.Forms.PictureBox();
            this.btnMin = new System.Windows.Forms.Button();
            this.button3 = new System.Windows.Forms.Button();
            this.btnTextDelete = new System.Windows.Forms.Button();
            this.btnTextUpdate = new System.Windows.Forms.Button();
            this.btnTextAdd = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.dgvRules)).BeginInit();
            this.panel3.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox2)).BeginInit();
            this.SuspendLayout();
            // 
            // dgvRules
            // 
            this.dgvRules.BackgroundColor = System.Drawing.Color.White;
            dataGridViewCellStyle1.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle1.BackColor = System.Drawing.SystemColors.Control;
            dataGridViewCellStyle1.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            dataGridViewCellStyle1.ForeColor = System.Drawing.SystemColors.WindowText;
            dataGridViewCellStyle1.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle1.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle1.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.dgvRules.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle1;
            this.dgvRules.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridViewCellStyle2.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle2.BackColor = System.Drawing.Color.White;
            dataGridViewCellStyle2.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            dataGridViewCellStyle2.ForeColor = System.Drawing.Color.Black;
            dataGridViewCellStyle2.SelectionBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(250)))), ((int)(((byte)(182)))), ((int)(((byte)(105)))));
            dataGridViewCellStyle2.SelectionForeColor = System.Drawing.Color.White;
            dataGridViewCellStyle2.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            this.dgvRules.DefaultCellStyle = dataGridViewCellStyle2;
            this.dgvRules.Location = new System.Drawing.Point(10, 67);
            this.dgvRules.Name = "dgvRules";
            this.dgvRules.Size = new System.Drawing.Size(686, 517);
            this.dgvRules.TabIndex = 0;
            // 
            // panel3
            // 
            this.panel3.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(250)))), ((int)(((byte)(182)))), ((int)(((byte)(105)))));
            this.panel3.Controls.Add(this.label1);
            this.panel3.Controls.Add(this.pictureBox2);
            this.panel3.Controls.Add(this.btnMin);
            this.panel3.Controls.Add(this.button3);
            this.panel3.Location = new System.Drawing.Point(0, 0);
            this.panel3.Name = "panel3";
            this.panel3.Size = new System.Drawing.Size(710, 32);
            this.panel3.TabIndex = 17;
            this.panel3.MouseDown += new System.Windows.Forms.MouseEventHandler(this.panel3_MouseDown);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(59, 5);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(131, 24);
            this.label1.TabIndex = 17;
            this.label1.Text = "Tray Settings";
            // 
            // cmbFile
            // 
            this.cmbFile.BackColor = System.Drawing.Color.White;
            this.cmbFile.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
            this.cmbFile.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.cmbFile.ForeColor = System.Drawing.Color.Black;
            this.cmbFile.FormattingEnabled = true;
            this.cmbFile.Location = new System.Drawing.Point(35, 37);
            this.cmbFile.Name = "cmbFile";
            this.cmbFile.Size = new System.Drawing.Size(138, 27);
            this.cmbFile.TabIndex = 19;
            this.cmbFile.DrawItem += new System.Windows.Forms.DrawItemEventHandler(this.cmbFile_DrawItem);
            // 
            // btnDeleteEntireFile
            // 
            this.btnDeleteEntireFile.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(128)))), ((int)(((byte)(0)))));
            this.btnDeleteEntireFile.BackgroundImage = global::Matric_scope.Properties.Resources.DELETE_FILE;
            this.btnDeleteEntireFile.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.btnDeleteEntireFile.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            this.btnDeleteEntireFile.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnDeleteEntireFile.ForeColor = System.Drawing.Color.White;
            this.btnDeleteEntireFile.Location = new System.Drawing.Point(514, 617);
            this.btnDeleteEntireFile.Name = "btnDeleteEntireFile";
            this.btnDeleteEntireFile.Size = new System.Drawing.Size(145, 35);
            this.btnDeleteEntireFile.TabIndex = 20;
            this.btnDeleteEntireFile.UseVisualStyleBackColor = false;
            this.btnDeleteEntireFile.Click += new System.EventHandler(this.btnDeleteEntireFile_Click);
            // 
            // pictureBox2
            // 
            this.pictureBox2.Image = global::Matric_scope.Properties.Resources.Logo__2_;
            this.pictureBox2.Location = new System.Drawing.Point(7, 0);
            this.pictureBox2.Name = "pictureBox2";
            this.pictureBox2.Size = new System.Drawing.Size(35, 35);
            this.pictureBox2.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.pictureBox2.TabIndex = 16;
            this.pictureBox2.TabStop = false;
            // 
            // btnMin
            // 
            this.btnMin.BackColor = System.Drawing.Color.Transparent;
            this.btnMin.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("btnMin.BackgroundImage")));
            this.btnMin.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.btnMin.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            this.btnMin.Location = new System.Drawing.Point(651, 4);
            this.btnMin.Name = "btnMin";
            this.btnMin.Size = new System.Drawing.Size(23, 23);
            this.btnMin.TabIndex = 15;
            this.btnMin.UseVisualStyleBackColor = false;
            this.btnMin.Click += new System.EventHandler(this.btnMin_Click);
            // 
            // button3
            // 
            this.button3.BackColor = System.Drawing.Color.Transparent;
            this.button3.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("button3.BackgroundImage")));
            this.button3.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.button3.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            this.button3.Location = new System.Drawing.Point(681, 5);
            this.button3.Name = "button3";
            this.button3.Size = new System.Drawing.Size(23, 23);
            this.button3.TabIndex = 14;
            this.button3.UseVisualStyleBackColor = false;
            this.button3.Click += new System.EventHandler(this.button3_Click);
            // 
            // btnTextDelete
            // 
            this.btnTextDelete.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(128)))), ((int)(((byte)(0)))));
            this.btnTextDelete.BackgroundImage = global::Matric_scope.Properties.Resources.DELETE;
            this.btnTextDelete.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.btnTextDelete.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            this.btnTextDelete.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnTextDelete.ForeColor = System.Drawing.Color.White;
            this.btnTextDelete.Location = new System.Drawing.Point(356, 617);
            this.btnTextDelete.Name = "btnTextDelete";
            this.btnTextDelete.Size = new System.Drawing.Size(124, 35);
            this.btnTextDelete.TabIndex = 14;
            this.btnTextDelete.UseVisualStyleBackColor = false;
            this.btnTextDelete.Click += new System.EventHandler(this.btnTextDelete_Click);
            // 
            // btnTextUpdate
            // 
            this.btnTextUpdate.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(128)))), ((int)(((byte)(0)))));
            this.btnTextUpdate.BackgroundImage = global::Matric_scope.Properties.Resources.SAVE;
            this.btnTextUpdate.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.btnTextUpdate.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            this.btnTextUpdate.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnTextUpdate.ForeColor = System.Drawing.Color.White;
            this.btnTextUpdate.Location = new System.Drawing.Point(58, 617);
            this.btnTextUpdate.Name = "btnTextUpdate";
            this.btnTextUpdate.Size = new System.Drawing.Size(115, 35);
            this.btnTextUpdate.TabIndex = 13;
            this.btnTextUpdate.UseVisualStyleBackColor = false;
            this.btnTextUpdate.Click += new System.EventHandler(this.btnTextUpdate_Click);
            // 
            // btnTextAdd
            // 
            this.btnTextAdd.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(128)))), ((int)(((byte)(0)))));
            this.btnTextAdd.BackgroundImage = global::Matric_scope.Properties.Resources.ADD_FILE;
            this.btnTextAdd.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.btnTextAdd.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            this.btnTextAdd.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnTextAdd.ForeColor = System.Drawing.Color.White;
            this.btnTextAdd.Location = new System.Drawing.Point(207, 617);
            this.btnTextAdd.Name = "btnTextAdd";
            this.btnTextAdd.Size = new System.Drawing.Size(115, 35);
            this.btnTextAdd.TabIndex = 12;
            this.btnTextAdd.UseVisualStyleBackColor = false;
            this.btnTextAdd.Click += new System.EventHandler(this.btnTextAdd_Click);
            // 
            // frmSettings
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.White;
            this.ClientSize = new System.Drawing.Size(708, 664);
            this.Controls.Add(this.btnDeleteEntireFile);
            this.Controls.Add(this.cmbFile);
            this.Controls.Add(this.panel3);
            this.Controls.Add(this.btnTextDelete);
            this.Controls.Add(this.btnTextUpdate);
            this.Controls.Add(this.btnTextAdd);
            this.Controls.Add(this.dgvRules);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "frmSettings";
            this.Text = "Settings";
            ((System.ComponentModel.ISupportInitialize)(this.dgvRules)).EndInit();
            this.panel3.ResumeLayout(false);
            this.panel3.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox2)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.DataGridView dgvRules;
        private System.Windows.Forms.Button btnTextAdd;
        private System.Windows.Forms.Button btnTextUpdate;
        private System.Windows.Forms.Button btnTextDelete;
        private System.Windows.Forms.Panel panel3;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.PictureBox pictureBox2;
        private System.Windows.Forms.Button btnMin;
        private System.Windows.Forms.Button button3;
        private System.Windows.Forms.ComboBox cmbFile;
        private System.Windows.Forms.Button btnDeleteEntireFile;
    }
}