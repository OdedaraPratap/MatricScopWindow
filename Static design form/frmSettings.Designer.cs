namespace opencvsharp
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
            this.cmbshape = new System.Windows.Forms.ComboBox();
            this.lblFromLength = new System.Windows.Forms.Label();
            this.lblToLength = new System.Windows.Forms.Label();
            this.txtFromLen = new System.Windows.Forms.TextBox();
            this.txtToLen = new System.Windows.Forms.TextBox();
            this.txtToWid = new System.Windows.Forms.TextBox();
            this.txtFromWid = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.lblFromwidth = new System.Windows.Forms.Label();
            this.txtSquareNumber = new System.Windows.Forms.TextBox();
            this.lblsquareNo = new System.Windows.Forms.Label();
            this.btnTextAdd = new System.Windows.Forms.Button();
            this.btnTextUpdate = new System.Windows.Forms.Button();
            this.btnTextDelete = new System.Windows.Forms.Button();
            this.panel3 = new System.Windows.Forms.Panel();
            this.label1 = new System.Windows.Forms.Label();
            this.btnCamera = new System.Windows.Forms.Button();
            this.pictureBox2 = new System.Windows.Forms.PictureBox();
            this.btnMin = new System.Windows.Forms.Button();
            this.button3 = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.dgvRules)).BeginInit();
            this.panel3.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox2)).BeginInit();
            this.SuspendLayout();
            // 
            // dgvRules
            // 
            this.dgvRules.BackgroundColor = System.Drawing.Color.Black;
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
            dataGridViewCellStyle2.BackColor = System.Drawing.Color.Black;
            dataGridViewCellStyle2.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            dataGridViewCellStyle2.ForeColor = System.Drawing.Color.White;
            dataGridViewCellStyle2.SelectionBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(128)))), ((int)(((byte)(0)))));
            dataGridViewCellStyle2.SelectionForeColor = System.Drawing.Color.White;
            dataGridViewCellStyle2.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            this.dgvRules.DefaultCellStyle = dataGridViewCellStyle2;
            this.dgvRules.Location = new System.Drawing.Point(10, 67);
            this.dgvRules.Name = "dgvRules";
            this.dgvRules.Size = new System.Drawing.Size(686, 517);
            this.dgvRules.TabIndex = 0;
            this.dgvRules.SelectionChanged += new System.EventHandler(this.dataGridView1_SelectionChanged);
            // 
            // cmbshape
            // 
            this.cmbshape.BackColor = System.Drawing.Color.Black;
            this.cmbshape.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
            this.cmbshape.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbshape.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.cmbshape.ForeColor = System.Drawing.Color.White;
            this.cmbshape.FormattingEnabled = true;
            this.cmbshape.Location = new System.Drawing.Point(8, 36);
            this.cmbshape.Name = "cmbshape";
            this.cmbshape.Size = new System.Drawing.Size(138, 27);
            this.cmbshape.TabIndex = 1;
            this.cmbshape.DrawItem += new System.Windows.Forms.DrawItemEventHandler(this.cmbshape_DrawItem);
            // 
            // lblFromLength
            // 
            this.lblFromLength.AutoSize = true;
            this.lblFromLength.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblFromLength.ForeColor = System.Drawing.Color.White;
            this.lblFromLength.Location = new System.Drawing.Point(150, 587);
            this.lblFromLength.Name = "lblFromLength";
            this.lblFromLength.Size = new System.Drawing.Size(100, 20);
            this.lblFromLength.TabIndex = 2;
            this.lblFromLength.Text = "From Length";
            // 
            // lblToLength
            // 
            this.lblToLength.AutoSize = true;
            this.lblToLength.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblToLength.ForeColor = System.Drawing.Color.White;
            this.lblToLength.Location = new System.Drawing.Point(288, 587);
            this.lblToLength.Name = "lblToLength";
            this.lblToLength.Size = new System.Drawing.Size(81, 20);
            this.lblToLength.TabIndex = 3;
            this.lblToLength.Text = "To Length";
            // 
            // txtFromLen
            // 
            this.txtFromLen.BackColor = System.Drawing.Color.Black;
            this.txtFromLen.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtFromLen.ForeColor = System.Drawing.Color.White;
            this.txtFromLen.Location = new System.Drawing.Point(148, 616);
            this.txtFromLen.Name = "txtFromLen";
            this.txtFromLen.Size = new System.Drawing.Size(100, 26);
            this.txtFromLen.TabIndex = 4;
            // 
            // txtToLen
            // 
            this.txtToLen.BackColor = System.Drawing.Color.Black;
            this.txtToLen.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtToLen.ForeColor = System.Drawing.Color.White;
            this.txtToLen.Location = new System.Drawing.Point(278, 616);
            this.txtToLen.Name = "txtToLen";
            this.txtToLen.Size = new System.Drawing.Size(100, 26);
            this.txtToLen.TabIndex = 5;
            // 
            // txtToWid
            // 
            this.txtToWid.BackColor = System.Drawing.Color.Black;
            this.txtToWid.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtToWid.ForeColor = System.Drawing.Color.White;
            this.txtToWid.Location = new System.Drawing.Point(525, 616);
            this.txtToWid.Name = "txtToWid";
            this.txtToWid.Size = new System.Drawing.Size(100, 26);
            this.txtToWid.TabIndex = 9;
            // 
            // txtFromWid
            // 
            this.txtFromWid.BackColor = System.Drawing.Color.Black;
            this.txtFromWid.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtFromWid.ForeColor = System.Drawing.Color.White;
            this.txtFromWid.Location = new System.Drawing.Point(405, 616);
            this.txtFromWid.Name = "txtFromWid";
            this.txtFromWid.Size = new System.Drawing.Size(100, 26);
            this.txtFromWid.TabIndex = 8;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.ForeColor = System.Drawing.Color.White;
            this.label2.Location = new System.Drawing.Point(542, 586);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(72, 20);
            this.label2.TabIndex = 7;
            this.label2.Text = "To Width";
            // 
            // lblFromwidth
            // 
            this.lblFromwidth.AutoSize = true;
            this.lblFromwidth.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblFromwidth.ForeColor = System.Drawing.Color.White;
            this.lblFromwidth.Location = new System.Drawing.Point(411, 587);
            this.lblFromwidth.Name = "lblFromwidth";
            this.lblFromwidth.Size = new System.Drawing.Size(87, 20);
            this.lblFromwidth.TabIndex = 6;
            this.lblFromwidth.Text = "From width";
            // 
            // txtSquareNumber
            // 
            this.txtSquareNumber.BackColor = System.Drawing.Color.Black;
            this.txtSquareNumber.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtSquareNumber.ForeColor = System.Drawing.Color.White;
            this.txtSquareNumber.Location = new System.Drawing.Point(17, 616);
            this.txtSquareNumber.Name = "txtSquareNumber";
            this.txtSquareNumber.Size = new System.Drawing.Size(100, 26);
            this.txtSquareNumber.TabIndex = 10;
            // 
            // lblsquareNo
            // 
            this.lblsquareNo.AutoSize = true;
            this.lblsquareNo.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblsquareNo.ForeColor = System.Drawing.Color.White;
            this.lblsquareNo.Location = new System.Drawing.Point(11, 586);
            this.lblsquareNo.Name = "lblsquareNo";
            this.lblsquareNo.Size = new System.Drawing.Size(121, 20);
            this.lblsquareNo.TabIndex = 11;
            this.lblsquareNo.Text = "Square Number";
            // 
            // btnTextAdd
            // 
            this.btnTextAdd.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(128)))), ((int)(((byte)(0)))));
            this.btnTextAdd.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            this.btnTextAdd.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnTextAdd.ForeColor = System.Drawing.Color.White;
            this.btnTextAdd.Location = new System.Drawing.Point(158, 657);
            this.btnTextAdd.Name = "btnTextAdd";
            this.btnTextAdd.Size = new System.Drawing.Size(75, 25);
            this.btnTextAdd.TabIndex = 12;
            this.btnTextAdd.Text = "ADD";
            this.btnTextAdd.UseVisualStyleBackColor = false;
            this.btnTextAdd.Click += new System.EventHandler(this.btnTextAdd_Click);
            // 
            // btnTextUpdate
            // 
            this.btnTextUpdate.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(128)))), ((int)(((byte)(0)))));
            this.btnTextUpdate.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            this.btnTextUpdate.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnTextUpdate.ForeColor = System.Drawing.Color.White;
            this.btnTextUpdate.Location = new System.Drawing.Point(281, 657);
            this.btnTextUpdate.Name = "btnTextUpdate";
            this.btnTextUpdate.Size = new System.Drawing.Size(90, 25);
            this.btnTextUpdate.TabIndex = 13;
            this.btnTextUpdate.Text = "UPDATE";
            this.btnTextUpdate.UseVisualStyleBackColor = false;
            this.btnTextUpdate.Click += new System.EventHandler(this.btnTextUpdate_Click);
            // 
            // btnTextDelete
            // 
            this.btnTextDelete.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(128)))), ((int)(((byte)(0)))));
            this.btnTextDelete.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            this.btnTextDelete.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnTextDelete.ForeColor = System.Drawing.Color.White;
            this.btnTextDelete.Location = new System.Drawing.Point(409, 656);
            this.btnTextDelete.Name = "btnTextDelete";
            this.btnTextDelete.Size = new System.Drawing.Size(93, 25);
            this.btnTextDelete.TabIndex = 14;
            this.btnTextDelete.Text = "DELETE";
            this.btnTextDelete.UseVisualStyleBackColor = false;
            this.btnTextDelete.Click += new System.EventHandler(this.btnTextDelete_Click);
            // 
            // panel3
            // 
            this.panel3.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(128)))), ((int)(((byte)(0)))));
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
            this.label1.Location = new System.Drawing.Point(59, 6);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(206, 24);
            this.label1.TabIndex = 17;
            this.label1.Text = "Mesurement Settings";
            // 
            // btnCamera
            // 
            this.btnCamera.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(128)))), ((int)(((byte)(0)))));
            this.btnCamera.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            this.btnCamera.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnCamera.ForeColor = System.Drawing.Color.White;
            this.btnCamera.Location = new System.Drawing.Point(204, 38);
            this.btnCamera.Name = "btnCamera";
            this.btnCamera.Size = new System.Drawing.Size(93, 25);
            this.btnCamera.TabIndex = 18;
            this.btnCamera.Text = "CAMERA";
            this.btnCamera.UseVisualStyleBackColor = false;
            this.btnCamera.Click += new System.EventHandler(this.btnCamera_Click);
            // 
            // pictureBox2
            // 
            this.pictureBox2.Image = global::opencvsharp.Properties.Resources.Logo__2_;
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
            // frmSettings
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.Black;
            this.ClientSize = new System.Drawing.Size(708, 692);
            this.Controls.Add(this.btnCamera);
            this.Controls.Add(this.panel3);
            this.Controls.Add(this.btnTextDelete);
            this.Controls.Add(this.btnTextUpdate);
            this.Controls.Add(this.btnTextAdd);
            this.Controls.Add(this.lblsquareNo);
            this.Controls.Add(this.txtSquareNumber);
            this.Controls.Add(this.txtToWid);
            this.Controls.Add(this.txtFromWid);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.lblFromwidth);
            this.Controls.Add(this.txtToLen);
            this.Controls.Add(this.txtFromLen);
            this.Controls.Add(this.lblToLength);
            this.Controls.Add(this.lblFromLength);
            this.Controls.Add(this.cmbshape);
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
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.DataGridView dgvRules;
        private System.Windows.Forms.ComboBox cmbshape;
        private System.Windows.Forms.Label lblFromLength;
        private System.Windows.Forms.Label lblToLength;
        private System.Windows.Forms.TextBox txtFromLen;
        private System.Windows.Forms.TextBox txtToLen;
        private System.Windows.Forms.TextBox txtToWid;
        private System.Windows.Forms.TextBox txtFromWid;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label lblFromwidth;
        private System.Windows.Forms.TextBox txtSquareNumber;
        private System.Windows.Forms.Label lblsquareNo;
        private System.Windows.Forms.Button btnTextAdd;
        private System.Windows.Forms.Button btnTextUpdate;
        private System.Windows.Forms.Button btnTextDelete;
        private System.Windows.Forms.Panel panel3;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.PictureBox pictureBox2;
        private System.Windows.Forms.Button btnMin;
        private System.Windows.Forms.Button button3;
        private System.Windows.Forms.Button btnCamera;
    }
}