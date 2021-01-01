namespace EmulatorUI
{
    partial class Main
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
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.displayOutput = new EmulatorUI.DisplayOutput();
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.loadSystemToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.nintendoEntertainmentSystemNESToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.exitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.viewToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.smoothOutputToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.outputScalerToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.nearestToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.cRTToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.hqx2xToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.exactScalingToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.systemToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.loadProgramToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.startEmulationToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.stopEmulationToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.debugToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.fullscreenToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.tableLayoutPanel1.SuspendLayout();
            this.menuStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 1;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.Controls.Add(this.displayOutput, 0, 1);
            this.tableLayoutPanel1.Controls.Add(this.menuStrip1, 0, 0);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 2;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(1236, 829);
            this.tableLayoutPanel1.TabIndex = 0;
            // 
            // displayOutput
            // 
            this.displayOutput.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.displayOutput.ExactScaling = false;
            this.displayOutput.Location = new System.Drawing.Point(3, 27);
            this.displayOutput.Name = "displayOutput";
            this.displayOutput.ShowFPS = true;
            this.displayOutput.Size = new System.Drawing.Size(1230, 799);
            this.displayOutput.TabIndex = 0;
            this.displayOutput.KeyDown += new System.Windows.Forms.KeyEventHandler(this.displayOutput_KeyDown);
            this.displayOutput.KeyUp += new System.Windows.Forms.KeyEventHandler(this.displayOutput_KeyUp);
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem,
            this.viewToolStripMenuItem,
            this.systemToolStripMenuItem,
            this.debugToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(1236, 24);
            this.menuStrip1.TabIndex = 4;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // fileToolStripMenuItem
            // 
            this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.loadSystemToolStripMenuItem,
            this.exitToolStripMenuItem});
            this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            this.fileToolStripMenuItem.Size = new System.Drawing.Size(37, 20);
            this.fileToolStripMenuItem.Text = "File";
            // 
            // loadSystemToolStripMenuItem
            // 
            this.loadSystemToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.nintendoEntertainmentSystemNESToolStripMenuItem});
            this.loadSystemToolStripMenuItem.Name = "loadSystemToolStripMenuItem";
            this.loadSystemToolStripMenuItem.Size = new System.Drawing.Size(141, 22);
            this.loadSystemToolStripMenuItem.Text = "Load System";
            // 
            // nintendoEntertainmentSystemNESToolStripMenuItem
            // 
            this.nintendoEntertainmentSystemNESToolStripMenuItem.Name = "nintendoEntertainmentSystemNESToolStripMenuItem";
            this.nintendoEntertainmentSystemNESToolStripMenuItem.Size = new System.Drawing.Size(275, 22);
            this.nintendoEntertainmentSystemNESToolStripMenuItem.Text = "Nintendo Entertainment System (NES)";
            this.nintendoEntertainmentSystemNESToolStripMenuItem.Click += new System.EventHandler(this.nintendoEntertainmentSystemNESToolStripMenuItem_Click);
            // 
            // exitToolStripMenuItem
            // 
            this.exitToolStripMenuItem.Name = "exitToolStripMenuItem";
            this.exitToolStripMenuItem.Size = new System.Drawing.Size(141, 22);
            this.exitToolStripMenuItem.Text = "Exit";
            this.exitToolStripMenuItem.Click += new System.EventHandler(this.exitToolStripMenuItem_Click);
            // 
            // viewToolStripMenuItem
            // 
            this.viewToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fullscreenToolStripMenuItem,
            this.smoothOutputToolStripMenuItem,
            this.outputScalerToolStripMenuItem,
            this.exactScalingToolStripMenuItem});
            this.viewToolStripMenuItem.Name = "viewToolStripMenuItem";
            this.viewToolStripMenuItem.Size = new System.Drawing.Size(44, 20);
            this.viewToolStripMenuItem.Text = "View";
            // 
            // smoothOutputToolStripMenuItem
            // 
            this.smoothOutputToolStripMenuItem.Name = "smoothOutputToolStripMenuItem";
            this.smoothOutputToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            this.smoothOutputToolStripMenuItem.Text = "Smooth Output";
            this.smoothOutputToolStripMenuItem.Click += new System.EventHandler(this.smoothOutputToolStripMenuItem_Click);
            // 
            // outputScalerToolStripMenuItem
            // 
            this.outputScalerToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.nearestToolStripMenuItem,
            this.cRTToolStripMenuItem,
            this.hqx2xToolStripMenuItem});
            this.outputScalerToolStripMenuItem.Name = "outputScalerToolStripMenuItem";
            this.outputScalerToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            this.outputScalerToolStripMenuItem.Text = "Output Scaler";
            // 
            // nearestToolStripMenuItem
            // 
            this.nearestToolStripMenuItem.Name = "nearestToolStripMenuItem";
            this.nearestToolStripMenuItem.Size = new System.Drawing.Size(114, 22);
            this.nearestToolStripMenuItem.Text = "Nearest";
            this.nearestToolStripMenuItem.Click += new System.EventHandler(this.nearestToolStripMenuItem_Click);
            // 
            // cRTToolStripMenuItem
            // 
            this.cRTToolStripMenuItem.Name = "cRTToolStripMenuItem";
            this.cRTToolStripMenuItem.Size = new System.Drawing.Size(114, 22);
            this.cRTToolStripMenuItem.Text = "CRT";
            this.cRTToolStripMenuItem.Click += new System.EventHandler(this.cRTToolStripMenuItem_Click);
            // 
            // hqx2xToolStripMenuItem
            // 
            this.hqx2xToolStripMenuItem.Name = "hqx2xToolStripMenuItem";
            this.hqx2xToolStripMenuItem.Size = new System.Drawing.Size(114, 22);
            this.hqx2xToolStripMenuItem.Text = "Hqx 2x";
            this.hqx2xToolStripMenuItem.Click += new System.EventHandler(this.hqx2xToolStripMenuItem_Click);
            // 
            // exactScalingToolStripMenuItem
            // 
            this.exactScalingToolStripMenuItem.Name = "exactScalingToolStripMenuItem";
            this.exactScalingToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            this.exactScalingToolStripMenuItem.Text = "Exact Scaling";
            this.exactScalingToolStripMenuItem.Click += new System.EventHandler(this.exactScalingToolStripMenuItem_Click);
            // 
            // systemToolStripMenuItem
            // 
            this.systemToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.loadProgramToolStripMenuItem,
            this.startEmulationToolStripMenuItem,
            this.stopEmulationToolStripMenuItem});
            this.systemToolStripMenuItem.Name = "systemToolStripMenuItem";
            this.systemToolStripMenuItem.Size = new System.Drawing.Size(57, 20);
            this.systemToolStripMenuItem.Text = "System";
            // 
            // loadProgramToolStripMenuItem
            // 
            this.loadProgramToolStripMenuItem.Name = "loadProgramToolStripMenuItem";
            this.loadProgramToolStripMenuItem.Size = new System.Drawing.Size(158, 22);
            this.loadProgramToolStripMenuItem.Text = "Load Program...";
            this.loadProgramToolStripMenuItem.Click += new System.EventHandler(this.loadProgramToolStripMenuItem_Click);
            // 
            // startEmulationToolStripMenuItem
            // 
            this.startEmulationToolStripMenuItem.Name = "startEmulationToolStripMenuItem";
            this.startEmulationToolStripMenuItem.Size = new System.Drawing.Size(158, 22);
            this.startEmulationToolStripMenuItem.Text = "Start Emulation";
            this.startEmulationToolStripMenuItem.Click += new System.EventHandler(this.startEmulationToolStripMenuItem_Click);
            // 
            // stopEmulationToolStripMenuItem
            // 
            this.stopEmulationToolStripMenuItem.Name = "stopEmulationToolStripMenuItem";
            this.stopEmulationToolStripMenuItem.Size = new System.Drawing.Size(158, 22);
            this.stopEmulationToolStripMenuItem.Text = "Stop Emulation";
            this.stopEmulationToolStripMenuItem.Click += new System.EventHandler(this.stopEmulationToolStripMenuItem_Click);
            // 
            // debugToolStripMenuItem
            // 
            this.debugToolStripMenuItem.Name = "debugToolStripMenuItem";
            this.debugToolStripMenuItem.Size = new System.Drawing.Size(54, 20);
            this.debugToolStripMenuItem.Text = "Debug";
            // 
            // fullscreenToolStripMenuItem
            // 
            this.fullscreenToolStripMenuItem.Name = "fullscreenToolStripMenuItem";
            this.fullscreenToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            this.fullscreenToolStripMenuItem.Text = "Fullscreen";
            this.fullscreenToolStripMenuItem.Click += new System.EventHandler(this.fullscreenToolStripMenuItem_Click);
            // 
            // Main
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1236, 829);
            this.Controls.Add(this.tableLayoutPanel1);
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "Main";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Emulator";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Main_FormClosing);
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel1.PerformLayout();
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private DisplayOutput displayOutput;
        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem debugToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem exitToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem systemToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem startEmulationToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem stopEmulationToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem loadSystemToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem nintendoEntertainmentSystemNESToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem loadProgramToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem viewToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem smoothOutputToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem outputScalerToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem nearestToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem cRTToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem hqx2xToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem exactScalingToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem fullscreenToolStripMenuItem;
    }
}

