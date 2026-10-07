namespace opencvsharp
{
    partial class FrmAuto
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FrmAuto));
            this.label1 = new System.Windows.Forms.Label();
            this.btnHm = new System.Windows.Forms.Button();
            this.btnTilt = new System.Windows.Forms.Button();
            this.panel3 = new System.Windows.Forms.Panel();
            this.label2 = new System.Windows.Forms.Label();
            this.pictureBox2 = new System.Windows.Forms.PictureBox();
            this.btnMin = new System.Windows.Forms.Button();
            this.button3 = new System.Windows.Forms.Button();
            this.btnEM = new System.Windows.Forms.Button();
            this.btnSettings = new System.Windows.Forms.Button();
            this.btnStop = new System.Windows.Forms.Button();
            this.btnPoly = new System.Windows.Forms.Button();
            this.btnOvel = new System.Windows.Forms.Button();
            this.btnHeart = new System.Windows.Forms.Button();
            this.btnmarqu = new System.Windows.Forms.Button();
            this.btnPear = new System.Windows.Forms.Button();
            this.btnRound = new System.Windows.Forms.Button();
            this.btnCalib = new System.Windows.Forms.Button();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.btnBlank = new System.Windows.Forms.Button();
            this.panel3.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox2)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 15.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.ForeColor = System.Drawing.Color.Red;
            this.label1.Location = new System.Drawing.Point(998, 42);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(129, 25);
            this.label1.TabIndex = 4;
            this.label1.Text = "Information";
            // 
            // btnHm
            // 
            this.btnHm.Location = new System.Drawing.Point(676, 53);
            this.btnHm.Name = "btnHm";
            this.btnHm.Size = new System.Drawing.Size(86, 35);
            this.btnHm.TabIndex = 13;
            this.btnHm.Text = "Half Moon";
            this.btnHm.UseVisualStyleBackColor = true;
            this.btnHm.Visible = false;
            // 
            // btnTilt
            // 
            this.btnTilt.Location = new System.Drawing.Point(675, 93);
            this.btnTilt.Name = "btnTilt";
            this.btnTilt.Size = new System.Drawing.Size(86, 35);
            this.btnTilt.TabIndex = 14;
            this.btnTilt.Text = "Tilt";
            this.btnTilt.UseVisualStyleBackColor = true;
            this.btnTilt.Visible = false;
            this.btnTilt.Click += new System.EventHandler(this.btnTilt_Click);
            // 
            // panel3
            // 
            this.panel3.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(128)))), ((int)(((byte)(0)))));
            this.panel3.Controls.Add(this.label2);
            this.panel3.Controls.Add(this.pictureBox2);
            this.panel3.Controls.Add(this.btnMin);
            this.panel3.Controls.Add(this.button3);
            this.panel3.Dock = System.Windows.Forms.DockStyle.Top;
            this.panel3.Location = new System.Drawing.Point(0, 0);
            this.panel3.Name = "panel3";
            this.panel3.Size = new System.Drawing.Size(1291, 32);
            this.panel3.TabIndex = 16;
            this.panel3.MouseDown += new System.Windows.Forms.MouseEventHandler(this.panel3_MouseDown);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("Microsoft Sans Serif", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.Location = new System.Drawing.Point(52, 5);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(133, 24);
            this.label2.TabIndex = 17;
            this.label2.Text = "Metric Scope";
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
            this.btnMin.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnMin.BackColor = System.Drawing.Color.Transparent;
            this.btnMin.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("btnMin.BackgroundImage")));
            this.btnMin.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.btnMin.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            this.btnMin.Location = new System.Drawing.Point(1231, 4);
            this.btnMin.Name = "btnMin";
            this.btnMin.Size = new System.Drawing.Size(23, 23);
            this.btnMin.TabIndex = 15;
            this.btnMin.UseVisualStyleBackColor = false;
            this.btnMin.Click += new System.EventHandler(this.btnMin_Click);
            // 
            // button3
            // 
            this.button3.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.button3.BackColor = System.Drawing.Color.Transparent;
            this.button3.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("button3.BackgroundImage")));
            this.button3.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.button3.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            this.button3.Location = new System.Drawing.Point(1262, 4);
            this.button3.Name = "button3";
            this.button3.Size = new System.Drawing.Size(23, 23);
            this.button3.TabIndex = 14;
            this.button3.UseVisualStyleBackColor = false;
            this.button3.Click += new System.EventHandler(this.button3_Click);
            // 
            // btnEM
            // 
            this.btnEM.BackgroundImage = global::opencvsharp.Properties.Resources.EMERALD;
            this.btnEM.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.btnEM.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnEM.Location = new System.Drawing.Point(506, 53);
            this.btnEM.Name = "btnEM";
            this.btnEM.Size = new System.Drawing.Size(64, 64);
            this.btnEM.TabIndex = 15;
            this.btnEM.UseVisualStyleBackColor = true;
            this.btnEM.Click += new System.EventHandler(this.btnEM_Click);
            // 
            // btnSettings
            // 
            this.btnSettings.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("btnSettings.BackgroundImage")));
            this.btnSettings.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.btnSettings.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnSettings.Location = new System.Drawing.Point(648, 52);
            this.btnSettings.Name = "btnSettings";
            this.btnSettings.Size = new System.Drawing.Size(64, 64);
            this.btnSettings.TabIndex = 10;
            this.btnSettings.UseVisualStyleBackColor = true;
            this.btnSettings.Click += new System.EventHandler(this.btnSettings_Click);
            // 
            // btnStop
            // 
            this.btnStop.BackgroundImage = global::opencvsharp.Properties.Resources.Stop_processed;
            this.btnStop.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.btnStop.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnStop.Location = new System.Drawing.Point(577, 52);
            this.btnStop.Name = "btnStop";
            this.btnStop.Size = new System.Drawing.Size(64, 64);
            this.btnStop.TabIndex = 11;
            this.btnStop.UseVisualStyleBackColor = true;
            this.btnStop.Click += new System.EventHandler(this.btnStop_Click);
            // 
            // btnPoly
            // 
            this.btnPoly.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("btnPoly.BackgroundImage")));
            this.btnPoly.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.btnPoly.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnPoly.Location = new System.Drawing.Point(435, 53);
            this.btnPoly.Name = "btnPoly";
            this.btnPoly.Size = new System.Drawing.Size(64, 64);
            this.btnPoly.TabIndex = 9;
            this.btnPoly.UseVisualStyleBackColor = true;
            this.btnPoly.Click += new System.EventHandler(this.btnPoly_Click);
            // 
            // btnOvel
            // 
            this.btnOvel.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("btnOvel.BackgroundImage")));
            this.btnOvel.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.btnOvel.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnOvel.Location = new System.Drawing.Point(364, 52);
            this.btnOvel.Name = "btnOvel";
            this.btnOvel.Size = new System.Drawing.Size(64, 64);
            this.btnOvel.TabIndex = 8;
            this.btnOvel.UseVisualStyleBackColor = true;
            this.btnOvel.Click += new System.EventHandler(this.btnOvel_Click);
            // 
            // btnHeart
            // 
            this.btnHeart.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("btnHeart.BackgroundImage")));
            this.btnHeart.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.btnHeart.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnHeart.Location = new System.Drawing.Point(293, 53);
            this.btnHeart.Name = "btnHeart";
            this.btnHeart.Size = new System.Drawing.Size(64, 64);
            this.btnHeart.TabIndex = 7;
            this.btnHeart.UseVisualStyleBackColor = true;
            this.btnHeart.Click += new System.EventHandler(this.btnHeart_Click);
            // 
            // btnmarqu
            // 
            this.btnmarqu.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("btnmarqu.BackgroundImage")));
            this.btnmarqu.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.btnmarqu.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnmarqu.Location = new System.Drawing.Point(222, 53);
            this.btnmarqu.Name = "btnmarqu";
            this.btnmarqu.Size = new System.Drawing.Size(64, 64);
            this.btnmarqu.TabIndex = 6;
            this.btnmarqu.UseVisualStyleBackColor = true;
            this.btnmarqu.Click += new System.EventHandler(this.btnmarqu_Click);
            // 
            // btnPear
            // 
            this.btnPear.BackgroundImage = global::opencvsharp.Properties.Resources.PEAR;
            this.btnPear.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.btnPear.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnPear.Location = new System.Drawing.Point(151, 53);
            this.btnPear.Name = "btnPear";
            this.btnPear.Size = new System.Drawing.Size(64, 64);
            this.btnPear.TabIndex = 5;
            this.btnPear.UseVisualStyleBackColor = true;
            this.btnPear.Click += new System.EventHandler(this.btnPear_Click);
            // 
            // btnRound
            // 
            this.btnRound.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("btnRound.BackgroundImage")));
            this.btnRound.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.btnRound.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnRound.Location = new System.Drawing.Point(80, 52);
            this.btnRound.Name = "btnRound";
            this.btnRound.Size = new System.Drawing.Size(64, 64);
            this.btnRound.TabIndex = 2;
            this.btnRound.UseVisualStyleBackColor = true;
            this.btnRound.Click += new System.EventHandler(this.btnRound_Click);
            // 
            // btnCalib
            // 
            this.btnCalib.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("btnCalib.BackgroundImage")));
            this.btnCalib.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.btnCalib.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnCalib.Location = new System.Drawing.Point(9, 51);
            this.btnCalib.Name = "btnCalib";
            this.btnCalib.Size = new System.Drawing.Size(64, 64);
            this.btnCalib.TabIndex = 0;
            this.btnCalib.UseVisualStyleBackColor = true;
            this.btnCalib.Click += new System.EventHandler(this.btnCalib_Click);
            // 
            // pictureBox1
            // 
            this.pictureBox1.Location = new System.Drawing.Point(8, 133);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(1152, 921);
            this.pictureBox1.TabIndex = 3;
            this.pictureBox1.TabStop = false;
            // 
            // btnBlank
            // 
            this.btnBlank.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnBlank.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnBlank.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(128)))), ((int)(((byte)(0)))));
            this.btnBlank.Location = new System.Drawing.Point(1168, 140);
            this.btnBlank.Name = "btnBlank";
            this.btnBlank.Size = new System.Drawing.Size(113, 46);
            this.btnBlank.TabIndex = 17;
            this.btnBlank.Text = "Background Calibration";
            this.btnBlank.UseVisualStyleBackColor = true;
            this.btnBlank.Click += new System.EventHandler(this.btnBlank_Click);
            // 
            // FrmAuto
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.Black;
            this.ClientSize = new System.Drawing.Size(1291, 1061);
            this.Controls.Add(this.btnBlank);
            this.Controls.Add(this.panel3);
            this.Controls.Add(this.btnEM);
            this.Controls.Add(this.btnSettings);
            this.Controls.Add(this.btnStop);
            this.Controls.Add(this.btnTilt);
            this.Controls.Add(this.btnHm);
            this.Controls.Add(this.btnPoly);
            this.Controls.Add(this.btnOvel);
            this.Controls.Add(this.btnHeart);
            this.Controls.Add(this.btnmarqu);
            this.Controls.Add(this.btnPear);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.btnRound);
            this.Controls.Add(this.btnCalib);
            this.Controls.Add(this.pictureBox1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "FrmAuto";
            this.Text = "Matric Scope";
            this.WindowState = System.Windows.Forms.FormWindowState.Maximized;
            this.Load += new System.EventHandler(this.Form1_Load);
            this.panel3.ResumeLayout(false);
            this.panel3.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox2)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btnCalib;
        private System.Windows.Forms.Button btnRound;
        private System.Windows.Forms.PictureBox pictureBox1;
        public System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button btnPear;
        private System.Windows.Forms.Button btnmarqu;
        private System.Windows.Forms.Button btnHeart;
        private System.Windows.Forms.Button btnOvel;
        private System.Windows.Forms.Button btnPoly;
        private System.Windows.Forms.Button btnSettings;
        private System.Windows.Forms.Button btnStop;
        private System.Windows.Forms.Button btnHm;
        private System.Windows.Forms.Button btnTilt;
        private System.Windows.Forms.Button btnEM;
        private System.Windows.Forms.Panel panel3;
        private System.Windows.Forms.PictureBox pictureBox2;
        private System.Windows.Forms.Button btnMin;
        private System.Windows.Forms.Button button3;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Button btnBlank;
    }
}

