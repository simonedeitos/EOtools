namespace DiskSpaceMonitor
{
    partial class MainForm
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
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            this.lblDrivePath = new System.Windows.Forms.Label();
            this.lblTotalSpace = new System.Windows.Forms.Label();
            this.lblUsedSpace = new System.Windows.Forms.Label();
            this.lblFreeSpace = new System.Windows.Forms.Label();
            this.progressBarDisk = new System.Windows.Forms.ProgressBar();
            this.btnCheckNow = new System.Windows.Forms.Button();
            this.btnSettings = new System.Windows.Forms.Button();
            this.lblLastCheck = new System.Windows.Forms.Label();
            this.lblNextCheck = new System.Windows.Forms.Label();
            this.notifyIcon = new System.Windows.Forms.NotifyIcon(this.components);
            this.contextMenuStrip = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.openToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.checkNowToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.settingsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            this.exitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.timerChecker = new System.Windows.Forms.Timer(this.components);
            this.timerClock = new System.Windows.Forms.Timer(this.components);
            this.groupBoxDiskInfo = new System.Windows.Forms.GroupBox();
            this.lblThreshold = new System.Windows.Forms.Label();
            this.btnReport = new System.Windows.Forms.Button();
            this.contextMenuStrip.SuspendLayout();
            this.groupBoxDiskInfo.SuspendLayout();
            this.SuspendLayout();
            // 
            // lblDrivePath
            // 
            this.lblDrivePath.AutoSize = true;
            this.lblDrivePath.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblDrivePath.Location = new System.Drawing.Point(12, 9);
            this.lblDrivePath.Name = "lblDrivePath";
            this.lblDrivePath.Size = new System.Drawing.Size(155, 16);
            this.lblDrivePath.TabIndex = 0;
            this.lblDrivePath.Text = "Drive: Not Configured";
            // 
            // lblTotalSpace
            // 
            this.lblTotalSpace.AutoSize = true;
            this.lblTotalSpace.Location = new System.Drawing.Point(6, 25);
            this.lblTotalSpace.Name = "lblTotalSpace";
            this.lblTotalSpace.Size = new System.Drawing.Size(68, 13);
            this.lblTotalSpace.TabIndex = 1;
            this.lblTotalSpace.Text = "Total Space:";
            // 
            // lblUsedSpace
            // 
            this.lblUsedSpace.AutoSize = true;
            this.lblUsedSpace.Location = new System.Drawing.Point(6, 48);
            this.lblUsedSpace.Name = "lblUsedSpace";
            this.lblUsedSpace.Size = new System.Drawing.Size(69, 13);
            this.lblUsedSpace.TabIndex = 2;
            this.lblUsedSpace.Text = "Used Space:";
            // 
            // lblFreeSpace
            // 
            this.lblFreeSpace.AutoSize = true;
            this.lblFreeSpace.Location = new System.Drawing.Point(6, 71);
            this.lblFreeSpace.Name = "lblFreeSpace";
            this.lblFreeSpace.Size = new System.Drawing.Size(65, 13);
            this.lblFreeSpace.TabIndex = 3;
            this.lblFreeSpace.Text = "Free Space:";
            // 
            // progressBarDisk
            // 
            this.progressBarDisk.Location = new System.Drawing.Point(12, 38);
            this.progressBarDisk.Name = "progressBarDisk";
            this.progressBarDisk.Size = new System.Drawing.Size(360, 30);
            this.progressBarDisk.TabIndex = 4;
            // 
            // btnCheckNow
            // 
            this.btnCheckNow.Location = new System.Drawing.Point(12, 219);
            this.btnCheckNow.Name = "btnCheckNow";
            this.btnCheckNow.Size = new System.Drawing.Size(110, 30);
            this.btnCheckNow.TabIndex = 5;
            this.btnCheckNow.Text = "Check Now";
            this.btnCheckNow.UseVisualStyleBackColor = true;
            this.btnCheckNow.Click += new System.EventHandler(this.btnCheckNow_Click);
            // 
            // btnSettings
            // 
            this.btnSettings.Location = new System.Drawing.Point(262, 219);
            this.btnSettings.Name = "btnSettings";
            this.btnSettings.Size = new System.Drawing.Size(110, 30);
            this.btnSettings.TabIndex = 6;
            this.btnSettings.Text = "Settings";
            this.btnSettings.UseVisualStyleBackColor = true;
            this.btnSettings.Click += new System.EventHandler(this.btnSettings_Click);
            // 
            // lblLastCheck
            // 
            this.lblLastCheck.AutoSize = true;
            this.lblLastCheck.Location = new System.Drawing.Point(12, 195);
            this.lblLastCheck.Name = "lblLastCheck";
            this.lblLastCheck.Size = new System.Drawing.Size(96, 13);
            this.lblLastCheck.TabIndex = 7;
            this.lblLastCheck.Text = "Last Check: Never";
            // 
            // lblNextCheck
            // 
            this.lblNextCheck.AutoSize = true;
            this.lblNextCheck.Location = new System.Drawing.Point(192, 195);
            this.lblNextCheck.Name = "lblNextCheck";
            this.lblNextCheck.Size = new System.Drawing.Size(105, 13);
            this.lblNextCheck.TabIndex = 8;
            this.lblNextCheck.Text = "Next Check: Not Set";
            // 
            // notifyIcon
            // 
            this.notifyIcon.ContextMenuStrip = this.contextMenuStrip;
            this.notifyIcon.Text = "Disk Space Monitor";
            this.notifyIcon.Visible = true;
            this.notifyIcon.DoubleClick += new System.EventHandler(this.notifyIcon_DoubleClick);
            // 
            // contextMenuStrip
            // 
            this.contextMenuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.openToolStripMenuItem,
            this.toolStripSeparator1,
            this.checkNowToolStripMenuItem,
            this.settingsToolStripMenuItem,
            this.toolStripSeparator2,
            this.exitToolStripMenuItem});
            this.contextMenuStrip.Name = "contextMenuStrip";
            this.contextMenuStrip.Size = new System.Drawing.Size(136, 104);
            // 
            // openToolStripMenuItem
            // 
            this.openToolStripMenuItem.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.openToolStripMenuItem.Name = "openToolStripMenuItem";
            this.openToolStripMenuItem.Size = new System.Drawing.Size(135, 22);
            this.openToolStripMenuItem.Text = "Open";
            this.openToolStripMenuItem.Click += new System.EventHandler(this.openToolStripMenuItem_Click);
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(132, 6);
            // 
            // checkNowToolStripMenuItem
            // 
            this.checkNowToolStripMenuItem.Name = "checkNowToolStripMenuItem";
            this.checkNowToolStripMenuItem.Size = new System.Drawing.Size(135, 22);
            this.checkNowToolStripMenuItem.Text = "Check Now";
            this.checkNowToolStripMenuItem.Click += new System.EventHandler(this.checkNowToolStripMenuItem_Click);
            // 
            // settingsToolStripMenuItem
            // 
            this.settingsToolStripMenuItem.Name = "settingsToolStripMenuItem";
            this.settingsToolStripMenuItem.Size = new System.Drawing.Size(135, 22);
            this.settingsToolStripMenuItem.Text = "Settings";
            this.settingsToolStripMenuItem.Click += new System.EventHandler(this.settingsToolStripMenuItem_Click);
            // 
            // toolStripSeparator2
            // 
            this.toolStripSeparator2.Name = "toolStripSeparator2";
            this.toolStripSeparator2.Size = new System.Drawing.Size(132, 6);
            // 
            // exitToolStripMenuItem
            // 
            this.exitToolStripMenuItem.Name = "exitToolStripMenuItem";
            this.exitToolStripMenuItem.Size = new System.Drawing.Size(135, 22);
            this.exitToolStripMenuItem.Text = "Exit";
            this.exitToolStripMenuItem.Click += new System.EventHandler(this.exitToolStripMenuItem_Click);
            // 
            // timerChecker
            // 
            this.timerChecker.Interval = 60000;
            this.timerChecker.Tick += new System.EventHandler(this.timerChecker_Tick);
            // 
            // timerClock
            // 
            this.timerClock.Interval = 1000;
            this.timerClock.Tick += new System.EventHandler(this.timerClock_Tick);
            // 
            // groupBoxDiskInfo
            // 
            this.groupBoxDiskInfo.Controls.Add(this.lblThreshold);
            this.groupBoxDiskInfo.Controls.Add(this.lblTotalSpace);
            this.groupBoxDiskInfo.Controls.Add(this.lblUsedSpace);
            this.groupBoxDiskInfo.Controls.Add(this.lblFreeSpace);
            this.groupBoxDiskInfo.Location = new System.Drawing.Point(12, 84);
            this.groupBoxDiskInfo.Name = "groupBoxDiskInfo";
            this.groupBoxDiskInfo.Size = new System.Drawing.Size(360, 99);
            this.groupBoxDiskInfo.TabIndex = 9;
            this.groupBoxDiskInfo.TabStop = false;
            this.groupBoxDiskInfo.Text = "Disk Information";
            // 
            // lblThreshold
            // 
            this.lblThreshold.AutoSize = true;
            this.lblThreshold.Location = new System.Drawing.Point(180, 71);
            this.lblThreshold.Name = "lblThreshold";
            this.lblThreshold.Size = new System.Drawing.Size(57, 13);
            this.lblThreshold.TabIndex = 4;
            this.lblThreshold.Text = "Threshold:";
            // 
            // btnReport
            // 
            this.btnReport.Location = new System.Drawing.Point(151, 219);
            this.btnReport.Name = "btnReport";
            this.btnReport.Size = new System.Drawing.Size(86, 30);
            this.btnReport.TabIndex = 10;
            this.btnReport.Text = "Report";
            this.btnReport.UseVisualStyleBackColor = true;
            this.btnReport.Click += new System.EventHandler(this.btnReport_Click);
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(384, 261);
            this.Controls.Add(this.btnReport);
            this.Controls.Add(this.groupBoxDiskInfo);
            this.Controls.Add(this.lblNextCheck);
            this.Controls.Add(this.lblLastCheck);
            this.Controls.Add(this.btnSettings);
            this.Controls.Add(this.btnCheckNow);
            this.Controls.Add(this.progressBarDisk);
            this.Controls.Add(this.lblDrivePath);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.Name = "MainForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Disk Space Monitor";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MainForm_FormClosing);
            this.Load += new System.EventHandler(this.MainForm_Load);
            this.Resize += new System.EventHandler(this.MainForm_Resize);
            this.contextMenuStrip.ResumeLayout(false);
            this.groupBoxDiskInfo.ResumeLayout(false);
            this.groupBoxDiskInfo.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label lblDrivePath;
        private System.Windows.Forms.Label lblTotalSpace;
        private System.Windows.Forms.Label lblUsedSpace;
        private System.Windows.Forms.Label lblFreeSpace;
        private System.Windows.Forms.ProgressBar progressBarDisk;
        private System.Windows.Forms.Button btnCheckNow;
        private System.Windows.Forms.Button btnSettings;
        private System.Windows.Forms.Label lblLastCheck;
        private System.Windows.Forms.Label lblNextCheck;
        private System.Windows.Forms.NotifyIcon notifyIcon;
        private System.Windows.Forms.ContextMenuStrip contextMenuStrip;
        private System.Windows.Forms.ToolStripMenuItem openToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripMenuItem checkNowToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem settingsToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator2;
        private System.Windows.Forms.ToolStripMenuItem exitToolStripMenuItem;
        private System.Windows.Forms.Timer timerChecker;
        private System.Windows.Forms.Timer timerClock;
        private System.Windows.Forms.GroupBox groupBoxDiskInfo;
        private System.Windows.Forms.Label lblThreshold;
        private System.Windows.Forms.Button btnReport;
    }
}