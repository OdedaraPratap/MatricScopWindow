using System.Drawing;

namespace Matric_scope
{
    partial class FrmAuto
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FrmAuto));
            this.label2 = new System.Windows.Forms.Label();
            this.btnHm = new System.Windows.Forms.Button();
            this.btnTilt = new System.Windows.Forms.Button();
            this.panel3 = new System.Windows.Forms.Panel();
            this.pictureBox2 = new System.Windows.Forms.PictureBox();
            this.btnMin = new System.Windows.Forms.Button();
            this.button3 = new System.Windows.Forms.Button();
            this.flowLayoutPanelButtons = new System.Windows.Forms.FlowLayoutPanel();
            this.btnCalib = new System.Windows.Forms.Button();
            this.btnRound = new System.Windows.Forms.Button();
            this.btnPear = new System.Windows.Forms.Button();
            this.btnmarqu = new System.Windows.Forms.Button();
            this.btnHeart = new System.Windows.Forms.Button();
            this.btnOvel = new System.Windows.Forms.Button();
            this.btnPoly = new System.Windows.Forms.Button();
            this.btnEM = new System.Windows.Forms.Button();
            this.btnGeneralC = new System.Windows.Forms.Button();
            this.btnSettings = new System.Windows.Forms.Button();
            this.btnStop = new System.Windows.Forms.Button();
            this.panelRightSide = new System.Windows.Forms.Panel();
            this.cmbRulesFile = new System.Windows.Forms.ComboBox();
            this.picPreview = new System.Windows.Forms.PictureBox();
            this.btnCaptureCustomShape = new System.Windows.Forms.Button();
            this.button2 = new System.Windows.Forms.Button();
            this.button1 = new System.Windows.Forms.Button();
            this.btnResetCounters = new System.Windows.Forms.Button();
            this.lblFile = new System.Windows.Forms.Label();
            this.btnBlank = new System.Windows.Forms.Button();
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.customShapesMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.panelLeftSide = new System.Windows.Forms.Panel();
            this.lblCurrentMode = new System.Windows.Forms.Label();
            this.lblRatioTitle = new System.Windows.Forms.Label();
            this.lblWidthTitle = new System.Windows.Forms.Label();
            this.lblLengthTitle = new System.Windows.Forms.Label();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.lblRatioVal = new Matric_scope.CustomLabel();
            this.lblWidthVal = new Matric_scope.CustomLabel();
            this.lblLengthVal = new Matric_scope.CustomLabel();
            this.panel3.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox2)).BeginInit();
            this.flowLayoutPanelButtons.SuspendLayout();
            this.panelRightSide.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.picPreview)).BeginInit();
            this.menuStrip1.SuspendLayout();
            this.panelLeftSide.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.SuspendLayout();
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("Microsoft Sans Serif", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.Location = new System.Drawing.Point(52, 5);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(167, 24);
            this.label2.TabIndex = 17;
            this.label2.Text = "Metric Scope 1.5";
            // 
            // btnHm
            // 
            this.btnHm.Location = new System.Drawing.Point(773, 3);
            this.btnHm.Name = "btnHm";
            this.btnHm.Size = new System.Drawing.Size(86, 35);
            this.btnHm.TabIndex = 10;
            this.btnHm.Text = "Half Moon";
            this.btnHm.Visible = false;
            // 
            // btnTilt
            // 
            this.btnTilt.Location = new System.Drawing.Point(865, 3);
            this.btnTilt.Name = "btnTilt";
            this.btnTilt.Size = new System.Drawing.Size(86, 35);
            this.btnTilt.TabIndex = 11;
            this.btnTilt.Text = "Tilt";
            this.btnTilt.Visible = false;
            this.btnTilt.Click += new System.EventHandler(this.btnTilt_Click);
            // 
            // panel3
            // 
            this.panel3.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(250)))), ((int)(((byte)(182)))), ((int)(((byte)(105)))));
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
            // flowLayoutPanelButtons
            // 
            this.flowLayoutPanelButtons.BackColor = System.Drawing.Color.White;
            this.flowLayoutPanelButtons.Controls.Add(this.btnCalib);
            this.flowLayoutPanelButtons.Controls.Add(this.btnRound);
            this.flowLayoutPanelButtons.Controls.Add(this.btnPear);
            this.flowLayoutPanelButtons.Controls.Add(this.btnmarqu);
            this.flowLayoutPanelButtons.Controls.Add(this.btnHeart);
            this.flowLayoutPanelButtons.Controls.Add(this.btnOvel);
            this.flowLayoutPanelButtons.Controls.Add(this.btnPoly);
            this.flowLayoutPanelButtons.Controls.Add(this.btnEM);
            this.flowLayoutPanelButtons.Controls.Add(this.btnGeneralC);
            this.flowLayoutPanelButtons.Controls.Add(this.btnSettings);
            this.flowLayoutPanelButtons.Controls.Add(this.btnStop);
            this.flowLayoutPanelButtons.Controls.Add(this.btnHm);
            this.flowLayoutPanelButtons.Controls.Add(this.btnTilt);
            this.flowLayoutPanelButtons.Dock = System.Windows.Forms.DockStyle.Top;
            this.flowLayoutPanelButtons.Location = new System.Drawing.Point(0, 32);
            this.flowLayoutPanelButtons.Margin = new System.Windows.Forms.Padding(0);
            this.flowLayoutPanelButtons.Name = "flowLayoutPanelButtons";
            this.flowLayoutPanelButtons.Size = new System.Drawing.Size(1291, 79);
            this.flowLayoutPanelButtons.TabIndex = 2;
            // 
            // btnCalib
            // 
            this.btnCalib.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("btnCalib.BackgroundImage")));
            this.btnCalib.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.btnCalib.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnCalib.Location = new System.Drawing.Point(3, 3);
            this.btnCalib.Name = "btnCalib";
            this.btnCalib.Size = new System.Drawing.Size(64, 64);
            this.btnCalib.TabIndex = 0;
            this.btnCalib.Click += new System.EventHandler(this.btnCalib_Click);
            // 
            // btnRound
            // 
            this.btnRound.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("btnRound.BackgroundImage")));
            this.btnRound.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.btnRound.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnRound.Location = new System.Drawing.Point(73, 3);
            this.btnRound.Name = "btnRound";
            this.btnRound.Size = new System.Drawing.Size(64, 64);
            this.btnRound.TabIndex = 1;
            this.btnRound.Click += new System.EventHandler(this.btnRound_Click);
            // 
            // btnPear
            // 
            this.btnPear.BackgroundImage = global::Matric_scope.Properties.Resources.PEAR;
            this.btnPear.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.btnPear.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnPear.Location = new System.Drawing.Point(143, 3);
            this.btnPear.Name = "btnPear";
            this.btnPear.Size = new System.Drawing.Size(64, 64);
            this.btnPear.TabIndex = 2;
            this.btnPear.Click += new System.EventHandler(this.btnPear_Click);
            // 
            // btnmarqu
            // 
            this.btnmarqu.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("btnmarqu.BackgroundImage")));
            this.btnmarqu.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.btnmarqu.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnmarqu.Location = new System.Drawing.Point(213, 3);
            this.btnmarqu.Name = "btnmarqu";
            this.btnmarqu.Size = new System.Drawing.Size(64, 64);
            this.btnmarqu.TabIndex = 3;
            this.btnmarqu.Click += new System.EventHandler(this.btnmarqu_Click);
            // 
            // btnHeart
            // 
            this.btnHeart.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("btnHeart.BackgroundImage")));
            this.btnHeart.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.btnHeart.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnHeart.Location = new System.Drawing.Point(283, 3);
            this.btnHeart.Name = "btnHeart";
            this.btnHeart.Size = new System.Drawing.Size(64, 64);
            this.btnHeart.TabIndex = 4;
            this.btnHeart.Click += new System.EventHandler(this.btnHeart_Click);
            // 
            // btnOvel
            // 
            this.btnOvel.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("btnOvel.BackgroundImage")));
            this.btnOvel.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.btnOvel.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnOvel.Location = new System.Drawing.Point(353, 3);
            this.btnOvel.Name = "btnOvel";
            this.btnOvel.Size = new System.Drawing.Size(64, 64);
            this.btnOvel.TabIndex = 5;
            this.btnOvel.Click += new System.EventHandler(this.btnOvel_Click);
            // 
            // btnPoly
            // 
            this.btnPoly.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("btnPoly.BackgroundImage")));
            this.btnPoly.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.btnPoly.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnPoly.Location = new System.Drawing.Point(423, 3);
            this.btnPoly.Name = "btnPoly";
            this.btnPoly.Size = new System.Drawing.Size(64, 64);
            this.btnPoly.TabIndex = 6;
            this.btnPoly.Click += new System.EventHandler(this.btnPoly_Click);
            // 
            // btnEM
            // 
            this.btnEM.BackgroundImage = global::Matric_scope.Properties.Resources.GENERAL;
            this.btnEM.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.btnEM.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnEM.Location = new System.Drawing.Point(493, 3);
            this.btnEM.Name = "btnEM";
            this.btnEM.Size = new System.Drawing.Size(64, 64);
            this.btnEM.TabIndex = 9;
            this.btnEM.Click += new System.EventHandler(this.btnEM_Click);
            // 
            // btnGeneralC
            // 
            this.btnGeneralC.BackgroundImage = global::Matric_scope.Properties.Resources.GENERALC;
            this.btnGeneralC.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.btnGeneralC.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnGeneralC.Location = new System.Drawing.Point(563, 3);
            this.btnGeneralC.Name = "btnGeneralC";
            this.btnGeneralC.Size = new System.Drawing.Size(64, 64);
            this.btnGeneralC.TabIndex = 12;
            this.btnGeneralC.Click += new System.EventHandler(this.btnGeneralC_Click);
            // 
            // btnSettings
            // 
            this.btnSettings.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("btnSettings.BackgroundImage")));
            this.btnSettings.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.btnSettings.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnSettings.Location = new System.Drawing.Point(633, 3);
            this.btnSettings.Name = "btnSettings";
            this.btnSettings.Size = new System.Drawing.Size(64, 64);
            this.btnSettings.TabIndex = 8;
            this.btnSettings.Click += new System.EventHandler(this.btnSettings_Click);
            // 
            // btnStop
            // 
            this.btnStop.BackgroundImage = global::Matric_scope.Properties.Resources.Stop_processed;
            this.btnStop.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.btnStop.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnStop.Location = new System.Drawing.Point(703, 3);
            this.btnStop.Name = "btnStop";
            this.btnStop.Size = new System.Drawing.Size(64, 64);
            this.btnStop.TabIndex = 7;
            this.btnStop.Click += new System.EventHandler(this.btnStop_Click);
            // 
            // panelRightSide
            // 
            this.panelRightSide.BackColor = System.Drawing.Color.White;
            this.panelRightSide.Controls.Add(this.cmbRulesFile);
            this.panelRightSide.Controls.Add(this.picPreview);
            this.panelRightSide.Controls.Add(this.btnCaptureCustomShape);
            this.panelRightSide.Controls.Add(this.button2);
            this.panelRightSide.Controls.Add(this.button1);
            this.panelRightSide.Controls.Add(this.btnResetCounters);
            this.panelRightSide.Controls.Add(this.lblFile);
            this.panelRightSide.Controls.Add(this.btnBlank);
            this.panelRightSide.Controls.Add(this.menuStrip1);
            this.panelRightSide.Dock = System.Windows.Forms.DockStyle.Right;
            this.panelRightSide.Location = new System.Drawing.Point(1131, 111);
            this.panelRightSide.Name = "panelRightSide";
            this.panelRightSide.Size = new System.Drawing.Size(160, 950);
            this.panelRightSide.TabIndex = 1;
            // 
            // cmbRulesFile
            // 
            this.cmbRulesFile.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
            this.cmbRulesFile.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.cmbRulesFile.FormattingEnabled = true;
            this.cmbRulesFile.Location = new System.Drawing.Point(38, 425);
            this.cmbRulesFile.Name = "cmbRulesFile";
            this.cmbRulesFile.Size = new System.Drawing.Size(121, 23);
            this.cmbRulesFile.TabIndex = 14;
            this.cmbRulesFile.DrawItem += new System.Windows.Forms.DrawItemEventHandler(this.cmbRulesFile_DrawItem);
            this.cmbRulesFile.SelectedIndexChanged += new System.EventHandler(this.cmbRulesFile_SelectedIndexChanged);
            // 
            // picPreview
            // 
            this.picPreview.Location = new System.Drawing.Point(7, 463);
            this.picPreview.Name = "picPreview";
            this.picPreview.Size = new System.Drawing.Size(147, 125);
            this.picPreview.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.picPreview.TabIndex = 13;
            this.picPreview.TabStop = false;
            // 
            // btnCaptureCustomShape
            // 
            this.btnCaptureCustomShape.BackgroundImage = global::Matric_scope.Properties.Resources.Add_shape;
            this.btnCaptureCustomShape.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.btnCaptureCustomShape.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            this.btnCaptureCustomShape.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnCaptureCustomShape.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(128)))), ((int)(((byte)(0)))));
            this.btnCaptureCustomShape.Location = new System.Drawing.Point(14, 350);
            this.btnCaptureCustomShape.Name = "btnCaptureCustomShape";
            this.btnCaptureCustomShape.Size = new System.Drawing.Size(140, 50);
            this.btnCaptureCustomShape.TabIndex = 11;
            this.btnCaptureCustomShape.Click += new System.EventHandler(this.btnCaptureCustomShape_Click);
            // 
            // button2
            // 
            this.button2.BackgroundImage = global::Matric_scope.Properties.Resources.Single_Print1;
            this.button2.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.button2.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            this.button2.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.button2.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(128)))), ((int)(((byte)(0)))));
            this.button2.Location = new System.Drawing.Point(14, 281);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(140, 50);
            this.button2.TabIndex = 5;
            this.button2.UseVisualStyleBackColor = true;
            this.button2.Click += new System.EventHandler(this.button2_Click);
            // 
            // button1
            // 
            this.button1.BackgroundImage = global::Matric_scope.Properties.Resources.Print1;
            this.button1.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.button1.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            this.button1.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.button1.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(128)))), ((int)(((byte)(0)))));
            this.button1.Location = new System.Drawing.Point(14, 212);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(140, 50);
            this.button1.TabIndex = 4;
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // btnResetCounters
            // 
            this.btnResetCounters.BackgroundImage = global::Matric_scope.Properties.Resources.Reset_Count1;
            this.btnResetCounters.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.btnResetCounters.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            this.btnResetCounters.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnResetCounters.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(128)))), ((int)(((byte)(0)))));
            this.btnResetCounters.Location = new System.Drawing.Point(14, 143);
            this.btnResetCounters.Name = "btnResetCounters";
            this.btnResetCounters.Size = new System.Drawing.Size(140, 50);
            this.btnResetCounters.TabIndex = 3;
            this.btnResetCounters.UseVisualStyleBackColor = true;
            this.btnResetCounters.Click += new System.EventHandler(this.btnResetCounters_Click);
            // 
            // lblFile
            // 
            this.lblFile.AutoSize = true;
            this.lblFile.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblFile.ForeColor = System.Drawing.Color.Black;
            this.lblFile.Location = new System.Drawing.Point(2, 428);
            this.lblFile.Name = "lblFile";
            this.lblFile.Size = new System.Drawing.Size(35, 15);
            this.lblFile.TabIndex = 2;
            this.lblFile.Text = "File:";
            // 
            // btnBlank
            // 
            this.btnBlank.BackgroundImage = global::Matric_scope.Properties.Resources.Backcaali;
            this.btnBlank.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.btnBlank.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            this.btnBlank.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnBlank.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(128)))), ((int)(((byte)(0)))));
            this.btnBlank.Location = new System.Drawing.Point(14, 74);
            this.btnBlank.Name = "btnBlank";
            this.btnBlank.Size = new System.Drawing.Size(140, 50);
            this.btnBlank.TabIndex = 1;
            this.btnBlank.UseVisualStyleBackColor = true;
            this.btnBlank.Click += new System.EventHandler(this.btnBlank_Click);
            // 
            // menuStrip1
            // 
            this.menuStrip1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(250)))), ((int)(((byte)(182)))), ((int)(((byte)(105)))));
            this.menuStrip1.Font = new System.Drawing.Font("Segoe UI Semibold", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.customShapesMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(160, 29);
            this.menuStrip1.TabIndex = 12;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // customShapesMenuItem
            // 
            this.customShapesMenuItem.Name = "customShapesMenuItem";
            this.customShapesMenuItem.Size = new System.Drawing.Size(134, 25);
            this.customShapesMenuItem.Text = "Custom Shapes";
            // 
            // panelLeftSide
            // 
            this.panelLeftSide.BackColor = System.Drawing.Color.White;
            this.panelLeftSide.Controls.Add(this.lblCurrentMode);
            this.panelLeftSide.Controls.Add(this.lblRatioVal);
            this.panelLeftSide.Controls.Add(this.lblRatioTitle);
            this.panelLeftSide.Controls.Add(this.lblWidthVal);
            this.panelLeftSide.Controls.Add(this.lblWidthTitle);
            this.panelLeftSide.Controls.Add(this.lblLengthVal);
            this.panelLeftSide.Controls.Add(this.lblLengthTitle);
            this.panelLeftSide.Dock = System.Windows.Forms.DockStyle.Left;
            this.panelLeftSide.Location = new System.Drawing.Point(0, 111);
            this.panelLeftSide.Name = "panelLeftSide";
            this.panelLeftSide.Padding = new System.Windows.Forms.Padding(10, 20, 10, 10);
            this.panelLeftSide.Size = new System.Drawing.Size(220, 950);
            this.panelLeftSide.TabIndex = 3;
            // 
            // lblCurrentMode
            // 
            this.lblCurrentMode.AutoSize = true;
            this.lblCurrentMode.Font = new System.Drawing.Font("Microsoft Sans Serif", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblCurrentMode.ForeColor = System.Drawing.Color.Black;
            this.lblCurrentMode.Location = new System.Drawing.Point(16, 341);
            this.lblCurrentMode.Name = "lblCurrentMode";
            this.lblCurrentMode.Size = new System.Drawing.Size(69, 24);
            this.lblCurrentMode.TabIndex = 6;
            this.lblCurrentMode.Text = "Mode:";
            // 
            // lblRatioTitle
            // 
            this.lblRatioTitle.AutoSize = true;
            this.lblRatioTitle.Font = new System.Drawing.Font("Microsoft Sans Serif", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblRatioTitle.ForeColor = System.Drawing.Color.Black;
            this.lblRatioTitle.Location = new System.Drawing.Point(30, 224);
            this.lblRatioTitle.Name = "lblRatioTitle";
            this.lblRatioTitle.Size = new System.Drawing.Size(72, 24);
            this.lblRatioTitle.TabIndex = 4;
            this.lblRatioTitle.Text = "RATIO";
            // 
            // lblWidthTitle
            // 
            this.lblWidthTitle.AutoSize = true;
            this.lblWidthTitle.Font = new System.Drawing.Font("Microsoft Sans Serif", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblWidthTitle.ForeColor = System.Drawing.Color.Black;
            this.lblWidthTitle.Location = new System.Drawing.Point(26, 121);
            this.lblWidthTitle.Name = "lblWidthTitle";
            this.lblWidthTitle.Size = new System.Drawing.Size(76, 24);
            this.lblWidthTitle.TabIndex = 2;
            this.lblWidthTitle.Text = "WIDTH";
            // 
            // lblLengthTitle
            // 
            this.lblLengthTitle.AutoSize = true;
            this.lblLengthTitle.Font = new System.Drawing.Font("Microsoft Sans Serif", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblLengthTitle.ForeColor = System.Drawing.Color.Black;
            this.lblLengthTitle.Location = new System.Drawing.Point(26, 20);
            this.lblLengthTitle.Name = "lblLengthTitle";
            this.lblLengthTitle.Size = new System.Drawing.Size(93, 24);
            this.lblLengthTitle.TabIndex = 0;
            this.lblLengthTitle.Text = "LENGTH";
            // 
            // pictureBox1
            // 
            this.pictureBox1.BackColor = System.Drawing.Color.Transparent;
            this.pictureBox1.Location = new System.Drawing.Point(224, 114);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(900, 941);
            this.pictureBox1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.pictureBox1.TabIndex = 0;
            this.pictureBox1.TabStop = false;
            // 
            // lblRatioVal
            // 
            this.lblRatioVal.AutoSize = true;
            this.lblRatioVal.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(250)))), ((int)(((byte)(182)))), ((int)(((byte)(105)))));
            this.lblRatioVal.BorderRadius = 10;
            this.lblRatioVal.BorderThickness = 3;
            this.lblRatioVal.Font = new System.Drawing.Font("Microsoft Sans Serif", 36F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblRatioVal.ForeColor = System.Drawing.Color.Red;
            this.lblRatioVal.Location = new System.Drawing.Point(10, 255);
            this.lblRatioVal.Name = "lblRatioVal";
            this.lblRatioVal.Size = new System.Drawing.Size(122, 55);
            this.lblRatioVal.TabIndex = 5;
            this.lblRatioVal.Text = "0.00";
            // 
            // lblWidthVal
            // 
            this.lblWidthVal.AutoSize = true;
            this.lblWidthVal.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(250)))), ((int)(((byte)(182)))), ((int)(((byte)(105)))));
            this.lblWidthVal.BorderRadius = 10;
            this.lblWidthVal.BorderThickness = 3;
            this.lblWidthVal.Font = new System.Drawing.Font("Microsoft Sans Serif", 36F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblWidthVal.ForeColor = System.Drawing.Color.Red;
            this.lblWidthVal.Location = new System.Drawing.Point(10, 155);
            this.lblWidthVal.Name = "lblWidthVal";
            this.lblWidthVal.Size = new System.Drawing.Size(122, 55);
            this.lblWidthVal.TabIndex = 3;
            this.lblWidthVal.Text = "0.00";
            // 
            // lblLengthVal
            // 
            this.lblLengthVal.AutoSize = true;
            this.lblLengthVal.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(250)))), ((int)(((byte)(182)))), ((int)(((byte)(105)))));
            this.lblLengthVal.BorderRadius = 10;
            this.lblLengthVal.BorderThickness = 3;
            this.lblLengthVal.Font = new System.Drawing.Font("Microsoft Sans Serif", 36F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblLengthVal.ForeColor = System.Drawing.Color.Red;
            this.lblLengthVal.Location = new System.Drawing.Point(10, 55);
            this.lblLengthVal.Name = "lblLengthVal";
            this.lblLengthVal.Size = new System.Drawing.Size(122, 55);
            this.lblLengthVal.TabIndex = 1;
            this.lblLengthVal.Text = "0.00";
            // 
            // FrmAuto
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.White;
            this.ClientSize = new System.Drawing.Size(1291, 1061);
            this.Controls.Add(this.panelLeftSide);
            this.Controls.Add(this.pictureBox1);
            this.Controls.Add(this.panelRightSide);
            this.Controls.Add(this.flowLayoutPanelButtons);
            this.Controls.Add(this.panel3);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "FrmAuto";
            this.Text = "Metric Scope";
            this.WindowState = System.Windows.Forms.FormWindowState.Maximized;
            this.Load += new System.EventHandler(this.Form1_Load);
            this.Resize += new System.EventHandler(this.FrmAuto_Resize);
            this.panel3.ResumeLayout(false);
            this.panel3.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox2)).EndInit();
            this.flowLayoutPanelButtons.ResumeLayout(false);
            this.panelRightSide.ResumeLayout(false);
            this.panelRightSide.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.picPreview)).EndInit();
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.panelLeftSide.ResumeLayout(false);
            this.panelLeftSide.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button btnCalib;
        private System.Windows.Forms.Button btnRound;
        private System.Windows.Forms.PictureBox pictureBox1;
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
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanelButtons;
        private System.Windows.Forms.Panel panelRightSide;
        public System.Windows.Forms.Label lblFile;
        private System.Windows.Forms.Button btnResetCounters;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Button button2;
        private System.Windows.Forms.Button btnGeneralC;
        private System.Windows.Forms.Button btnCaptureCustomShape;
        private System.Windows.Forms.MenuStrip menuStrip1;
        public System.Windows.Forms.ToolStripMenuItem customShapesMenuItem;
        private System.Windows.Forms.PictureBox picPreview;

        // NEW CONTROLS FOR LEFT PANEL
        private System.Windows.Forms.Panel panelLeftSide;
        public System.Windows.Forms.Label lblLengthTitle;
        //public System.Windows.Forms.Label lblLengthVal;
        public CustomLabel lblLengthVal;
        public System.Windows.Forms.Label lblWidthTitle;
        //public System.Windows.Forms.Label lblWidthVal;
        public CustomLabel lblWidthVal;
        public System.Windows.Forms.Label lblRatioTitle;
        //public System.Windows.Forms.Label lblRatioVal;
        public CustomLabel lblRatioVal;
        private System.Windows.Forms.ComboBox cmbRulesFile;
        public System.Windows.Forms.Label lblCurrentMode;
    }
}