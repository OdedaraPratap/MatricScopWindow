using System.Windows.Forms;
namespace Matric_scope
{
    partial class Camera_Setting1
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Camera_Setting1));
            this.metroLabel5 = new System.Windows.Forms.Label();
            this.udexposure = new System.Windows.Forms.NumericUpDown();
            this.metroLabel1 = new System.Windows.Forms.Label();
            this.numericUpDownGamma = new System.Windows.Forms.NumericUpDown();
            this.trackBarGamma = new System.Windows.Forms.TrackBar();
            this.udmastergain = new System.Windows.Forms.NumericUpDown();
            this.btnOK = new System.Windows.Forms.Button();
            this.trackBarExposure = new System.Windows.Forms.TrackBar();
            this.trackBarGainMaster = new System.Windows.Forms.TrackBar();
            this.metroLabel2 = new System.Windows.Forms.Label();
            this.panel1 = new System.Windows.Forms.Panel();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.label1 = new System.Windows.Forms.Label();
            this.btnClose = new System.Windows.Forms.Button();
            this.chkSave = new System.Windows.Forms.CheckBox();
            this.chkPrint = new System.Windows.Forms.CheckBox();
            this.btnHistory = new System.Windows.Forms.Button();
            this.btnMesure = new System.Windows.Forms.Button();
            this.button1 = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.udexposure)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownGamma)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.trackBarGamma)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.udmastergain)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.trackBarExposure)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.trackBarGainMaster)).BeginInit();
            this.panel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.SuspendLayout();
            // 
            // metroLabel5
            // 
            this.metroLabel5.AutoSize = true;
            this.metroLabel5.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.metroLabel5.ForeColor = System.Drawing.Color.Black;
            this.metroLabel5.Location = new System.Drawing.Point(28, 121);
            this.metroLabel5.Name = "metroLabel5";
            this.metroLabel5.Size = new System.Drawing.Size(71, 20);
            this.metroLabel5.TabIndex = 27;
            this.metroLabel5.Text = "Gamma";
            // 
            // udexposure
            // 
            this.udexposure.BackColor = System.Drawing.Color.White;
            this.udexposure.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.udexposure.ForeColor = System.Drawing.Color.Black;
            this.udexposure.Location = new System.Drawing.Point(328, 76);
            this.udexposure.Maximum = new decimal(new int[] {
            1320,
            0,
            0,
            0});
            this.udexposure.Name = "udexposure";
            this.udexposure.Size = new System.Drawing.Size(96, 26);
            this.udexposure.TabIndex = 25;
            this.udexposure.ValueChanged += new System.EventHandler(this.udexposure_ValueChanged);
            // 
            // metroLabel1
            // 
            this.metroLabel1.AutoSize = true;
            this.metroLabel1.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.metroLabel1.ForeColor = System.Drawing.Color.Black;
            this.metroLabel1.Location = new System.Drawing.Point(25, 36);
            this.metroLabel1.Name = "metroLabel1";
            this.metroLabel1.Size = new System.Drawing.Size(107, 20);
            this.metroLabel1.TabIndex = 16;
            this.metroLabel1.Text = "Master Gain";
            // 
            // numericUpDownGamma
            // 
            this.numericUpDownGamma.BackColor = System.Drawing.Color.White;
            this.numericUpDownGamma.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.numericUpDownGamma.ForeColor = System.Drawing.Color.Black;
            this.numericUpDownGamma.Increment = new decimal(new int[] {
            1,
            0,
            0,
            65536});
            this.numericUpDownGamma.Location = new System.Drawing.Point(329, 119);
            this.numericUpDownGamma.Maximum = new decimal(new int[] {
            22,
            0,
            0,
            65536});
            this.numericUpDownGamma.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numericUpDownGamma.Name = "numericUpDownGamma";
            this.numericUpDownGamma.Size = new System.Drawing.Size(96, 26);
            this.numericUpDownGamma.TabIndex = 29;
            this.numericUpDownGamma.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numericUpDownGamma.ValueChanged += new System.EventHandler(this.numericUpDownGamma_ValueChanged);
            // 
            // trackBarGamma
            // 
            this.trackBarGamma.Location = new System.Drawing.Point(123, 118);
            this.trackBarGamma.Maximum = 220;
            this.trackBarGamma.Minimum = 100;
            this.trackBarGamma.Name = "trackBarGamma";
            this.trackBarGamma.Size = new System.Drawing.Size(195, 45);
            this.trackBarGamma.TabIndex = 28;
            this.trackBarGamma.Text = "trackGamma";
            this.trackBarGamma.Value = 100;
            this.trackBarGamma.Scroll += new System.EventHandler(this.trackBarGamma_Scroll);
            // 
            // udmastergain
            // 
            this.udmastergain.BackColor = System.Drawing.Color.White;
            this.udmastergain.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.udmastergain.ForeColor = System.Drawing.Color.Black;
            this.udmastergain.Location = new System.Drawing.Point(328, 39);
            this.udmastergain.Name = "udmastergain";
            this.udmastergain.Size = new System.Drawing.Size(96, 26);
            this.udmastergain.TabIndex = 24;
            this.udmastergain.ValueChanged += new System.EventHandler(this.udmastergain_ValueChanged);
            // 
            // btnOK
            // 
            this.btnOK.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(250)))), ((int)(((byte)(186)))), ((int)(((byte)(105)))));
            this.btnOK.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            this.btnOK.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnOK.ForeColor = System.Drawing.Color.Black;
            this.btnOK.Location = new System.Drawing.Point(453, 70);
            this.btnOK.Name = "btnOK";
            this.btnOK.Size = new System.Drawing.Size(76, 36);
            this.btnOK.TabIndex = 20;
            this.btnOK.Text = "OK";
            this.btnOK.UseVisualStyleBackColor = false;
            this.btnOK.Visible = false;
            this.btnOK.Click += new System.EventHandler(this.btnOK_Click);
            // 
            // trackBarExposure
            // 
            this.trackBarExposure.Location = new System.Drawing.Point(126, 76);
            this.trackBarExposure.Maximum = 1320;
            this.trackBarExposure.Name = "trackBarExposure";
            this.trackBarExposure.Size = new System.Drawing.Size(195, 45);
            this.trackBarExposure.TabIndex = 19;
            this.trackBarExposure.Text = "metroTrackBar1";
            this.trackBarExposure.Scroll += new System.EventHandler(this.trackBarExposure_Scroll);
            // 
            // trackBarGainMaster
            // 
            this.trackBarGainMaster.Location = new System.Drawing.Point(126, 36);
            this.trackBarGainMaster.Maximum = 100;
            this.trackBarGainMaster.Name = "trackBarGainMaster";
            this.trackBarGainMaster.Size = new System.Drawing.Size(195, 45);
            this.trackBarGainMaster.TabIndex = 18;
            this.trackBarGainMaster.Text = "metroTrackBar1";
            this.trackBarGainMaster.Scroll += new System.EventHandler(this.trackBarGainMaster_Scroll);
            // 
            // metroLabel2
            // 
            this.metroLabel2.AutoSize = true;
            this.metroLabel2.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.metroLabel2.ForeColor = System.Drawing.Color.Black;
            this.metroLabel2.Location = new System.Drawing.Point(28, 74);
            this.metroLabel2.Name = "metroLabel2";
            this.metroLabel2.Size = new System.Drawing.Size(84, 20);
            this.metroLabel2.TabIndex = 17;
            this.metroLabel2.Text = "Exposure";
            // 
            // panel1
            // 
            this.panel1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(250)))), ((int)(((byte)(182)))), ((int)(((byte)(105)))));
            this.panel1.Controls.Add(this.pictureBox1);
            this.panel1.Controls.Add(this.label1);
            this.panel1.Controls.Add(this.btnClose);
            this.panel1.Location = new System.Drawing.Point(0, 0);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(544, 32);
            this.panel1.TabIndex = 30;
            this.panel1.MouseDown += new System.Windows.Forms.MouseEventHandler(this.panel1_MouseDown);
            // 
            // pictureBox1
            // 
            this.pictureBox1.Image = global::Matric_scope.Properties.Resources.Logo__2_;
            this.pictureBox1.Location = new System.Drawing.Point(4, 0);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(35, 35);
            this.pictureBox1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.pictureBox1.TabIndex = 16;
            this.pictureBox1.TabStop = false;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.ForeColor = System.Drawing.Color.Black;
            this.label1.Location = new System.Drawing.Point(53, 6);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(71, 20);
            this.label1.TabIndex = 15;
            this.label1.Text = "Camera";
            // 
            // btnClose
            // 
            this.btnClose.BackColor = System.Drawing.Color.Transparent;
            this.btnClose.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("btnClose.BackgroundImage")));
            this.btnClose.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.btnClose.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            this.btnClose.Location = new System.Drawing.Point(516, 4);
            this.btnClose.Name = "btnClose";
            this.btnClose.Size = new System.Drawing.Size(23, 23);
            this.btnClose.TabIndex = 14;
            this.btnClose.UseVisualStyleBackColor = false;
            this.btnClose.Click += new System.EventHandler(this.btnClose_Click);
            // 
            // chkSave
            // 
            this.chkSave.AutoSize = true;
            this.chkSave.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.chkSave.ForeColor = System.Drawing.Color.Black;
            this.chkSave.Location = new System.Drawing.Point(106, 176);
            this.chkSave.Name = "chkSave";
            this.chkSave.Size = new System.Drawing.Size(102, 24);
            this.chkSave.TabIndex = 34;
            this.chkSave.Text = "Auto Save";
            this.chkSave.UseVisualStyleBackColor = true;
            this.chkSave.CheckedChanged += new System.EventHandler(this.chkSave_CheckedChanged);
            // 
            // chkPrint
            // 
            this.chkPrint.AutoSize = true;
            this.chkPrint.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.chkPrint.ForeColor = System.Drawing.Color.Black;
            this.chkPrint.Location = new System.Drawing.Point(7, 176);
            this.chkPrint.Name = "chkPrint";
            this.chkPrint.Size = new System.Drawing.Size(98, 24);
            this.chkPrint.TabIndex = 31;
            this.chkPrint.Text = "Auto Print";
            this.chkPrint.UseVisualStyleBackColor = true;
            this.chkPrint.CheckedChanged += new System.EventHandler(this.chkPrint_CheckedChanged);
            // 
            // btnHistory
            // 
            this.btnHistory.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(128)))), ((int)(((byte)(0)))));
            this.btnHistory.BackgroundImage = global::Matric_scope.Properties.Resources.HISTORY;
            this.btnHistory.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.btnHistory.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            this.btnHistory.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnHistory.ForeColor = System.Drawing.Color.White;
            this.btnHistory.Location = new System.Drawing.Point(328, 170);
            this.btnHistory.Name = "btnHistory";
            this.btnHistory.Size = new System.Drawing.Size(108, 36);
            this.btnHistory.TabIndex = 33;
            this.btnHistory.UseVisualStyleBackColor = false;
            this.btnHistory.Click += new System.EventHandler(this.btnHistory_Click);
            // 
            // btnMesure
            // 
            this.btnMesure.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(128)))), ((int)(((byte)(0)))));
            this.btnMesure.BackgroundImage = global::Matric_scope.Properties.Resources.TRAY_SETTING;
            this.btnMesure.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.btnMesure.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            this.btnMesure.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnMesure.ForeColor = System.Drawing.Color.White;
            this.btnMesure.Location = new System.Drawing.Point(209, 169);
            this.btnMesure.Name = "btnMesure";
            this.btnMesure.Size = new System.Drawing.Size(109, 37);
            this.btnMesure.TabIndex = 32;
            this.btnMesure.UseVisualStyleBackColor = false;
            this.btnMesure.Click += new System.EventHandler(this.btnMesure_Click);
            // 
            // button1
            // 
            this.button1.BackgroundImage = global::Matric_scope.Properties.Resources.Callibrationin;
            this.button1.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.button1.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            this.button1.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.button1.ForeColor = System.Drawing.Color.White;
            this.button1.Location = new System.Drawing.Point(448, 163);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(63, 48);
            this.button1.TabIndex = 35;
            this.button1.UseVisualStyleBackColor = false;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // Camera_Setting1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.White;
            this.ClientSize = new System.Drawing.Size(544, 220);
            this.ControlBox = false;
            this.Controls.Add(this.button1);
            this.Controls.Add(this.chkSave);
            this.Controls.Add(this.btnHistory);
            this.Controls.Add(this.btnMesure);
            this.Controls.Add(this.chkPrint);
            this.Controls.Add(this.panel1);
            this.Controls.Add(this.metroLabel5);
            this.Controls.Add(this.udexposure);
            this.Controls.Add(this.metroLabel1);
            this.Controls.Add(this.numericUpDownGamma);
            this.Controls.Add(this.trackBarGamma);
            this.Controls.Add(this.udmastergain);
            this.Controls.Add(this.btnOK);
            this.Controls.Add(this.trackBarExposure);
            this.Controls.Add(this.trackBarGainMaster);
            this.Controls.Add(this.metroLabel2);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Location = new System.Drawing.Point(600, 500);
            this.Name = "Camera_Setting1";
            this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
            this.Text = "Camera Setting";
            this.Load += new System.EventHandler(this.Camera_Setting1_Load);
            ((System.ComponentModel.ISupportInitialize)(this.udexposure)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownGamma)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.trackBarGamma)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.udmastergain)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.trackBarExposure)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.trackBarGainMaster)).EndInit();
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private Label metroLabel5;
        private System.Windows.Forms.NumericUpDown udexposure;
        private Label metroLabel1;
        private System.Windows.Forms.NumericUpDown numericUpDownGamma;
        private TrackBar trackBarGamma;
        private System.Windows.Forms.NumericUpDown udmastergain;
        private Button btnOK;
        private TrackBar trackBarExposure;
        private TrackBar trackBarGainMaster;
        private Label metroLabel2;
        private Panel panel1;
        private Button btnClose;
        private Label label1;
        private PictureBox pictureBox1;
        private CheckBox chkSave;
        private Button btnHistory;
        private Button btnMesure;
        private CheckBox chkPrint;
        private Button button1;
    }
}