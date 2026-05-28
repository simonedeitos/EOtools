namespace DumpCheck
{
    partial class MainForm
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

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.notifyIcon = new System.Windows.Forms.NotifyIcon(this.components);
            this.contextMenuStrip = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.openToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.checkRealtimeNowToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.checkAmpliatoNowToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.settingsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            this.exitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.timerRealtimeClock = new System.Windows.Forms.Timer(this.components);
            this.timerRealtimeChecker = new System.Windows.Forms.Timer(this.components);
            this.timerAmpliatoClock = new System.Windows.Forms.Timer(this.components);
            this.timerAmpliatoChecker = new System.Windows.Forms.Timer(this.components);
            this.contextMenuStrip.SuspendLayout();
            this.SuspendLayout();
            // 
            // notifyIcon
            // 
            this.notifyIcon.DoubleClick += new System.EventHandler(this.notifyIcon_DoubleClick);
            // 
            // contextMenuStrip
            // 
            this.contextMenuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.openToolStripMenuItem,
            this.toolStripSeparator1,
            this.checkRealtimeNowToolStripMenuItem,
            this.checkAmpliatoNowToolStripMenuItem,
            this.settingsToolStripMenuItem,
            this.toolStripSeparator2,
            this.exitToolStripMenuItem});
            this.contextMenuStrip.Name = "contextMenuStrip";
            this.contextMenuStrip.Size = new System.Drawing.Size(177, 126);
            // 
            // openToolStripMenuItem
            // 
            this.openToolStripMenuItem.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.openToolStripMenuItem.Name = "openToolStripMenuItem";
            this.openToolStripMenuItem.Size = new System.Drawing.Size(176, 22);
            this.openToolStripMenuItem.Text = "Open";
            this.openToolStripMenuItem.Click += new System.EventHandler(this.openToolStripMenuItem_Click);
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(173, 6);
            // 
            // checkRealtimeNowToolStripMenuItem
            // 
            this.checkRealtimeNowToolStripMenuItem.Name = "checkRealtimeNowToolStripMenuItem";
            this.checkRealtimeNowToolStripMenuItem.Size = new System.Drawing.Size(176, 22);
            this.checkRealtimeNowToolStripMenuItem.Text = "Check Realtime";
            this.checkRealtimeNowToolStripMenuItem.Click += new System.EventHandler(this.checkRealtimeNowToolStripMenuItem_Click);
            // 
            // checkAmpliatoNowToolStripMenuItem
            // 
            this.checkAmpliatoNowToolStripMenuItem.Name = "checkAmpliatoNowToolStripMenuItem";
            this.checkAmpliatoNowToolStripMenuItem.Size = new System.Drawing.Size(176, 22);
            this.checkAmpliatoNowToolStripMenuItem.Text = "Check Ampliato";
            this.checkAmpliatoNowToolStripMenuItem.Click += new System.EventHandler(this.checkAmpliatoNowToolStripMenuItem_Click);
            // 
            // settingsToolStripMenuItem
            // 
            this.settingsToolStripMenuItem.Name = "settingsToolStripMenuItem";
            this.settingsToolStripMenuItem.Size = new System.Drawing.Size(176, 22);
            this.settingsToolStripMenuItem.Text = "Settings";
            this.settingsToolStripMenuItem.Click += new System.EventHandler(this.settingsToolStripMenuItem_Click);
            // 
            // toolStripSeparator2
            // 
            this.toolStripSeparator2.Name = "toolStripSeparator2";
            this.toolStripSeparator2.Size = new System.Drawing.Size(173, 6);
            // 
            // exitToolStripMenuItem
            // 
            this.exitToolStripMenuItem.Name = "exitToolStripMenuItem";
            this.exitToolStripMenuItem.Size = new System.Drawing.Size(176, 22);
            this.exitToolStripMenuItem.Text = "Exit";
            this.exitToolStripMenuItem.Click += new System.EventHandler(this.exitToolStripMenuItem_Click);
            // 
            // timerRealtimeClock
            // 
            this.timerRealtimeClock.Tick += new System.EventHandler(this.timerRealtimeClock_Tick);
            // 
            // timerRealtimeChecker
            // 
            this.timerRealtimeChecker.Tick += new System.EventHandler(this.timerRealtimeChecker_Tick);
            // 
            // timerAmpliatoClock
            // 
            this.timerAmpliatoClock.Tick += new System.EventHandler(this.timerAmpliatoClock_Tick);
            // 
            // timerAmpliatoChecker
            // 
            this.timerAmpliatoChecker.Tick += new System.EventHandler(this.timerAmpliatoChecker_Tick);
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(784, 486);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.Name = "MainForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "DumpCheck";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MainForm_FormClosing);
            this.Load += new System.EventHandler(this.MainForm_Load);
            this.Resize += new System.EventHandler(this.MainForm_Resize);
            this.contextMenuStrip.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        private System.Windows.Forms.NotifyIcon notifyIcon;
        private System.Windows.Forms.ContextMenuStrip contextMenuStrip;
        private System.Windows.Forms.ToolStripMenuItem openToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripMenuItem checkRealtimeNowToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem checkAmpliatoNowToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem settingsToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator2;
        private System.Windows.Forms.ToolStripMenuItem exitToolStripMenuItem;
        private System.Windows.Forms.Timer timerRealtimeClock;
        private System.Windows.Forms.Timer timerRealtimeChecker;
        private System.Windows.Forms.Timer timerAmpliatoClock;
        private System.Windows.Forms.Timer timerAmpliatoChecker;
    }
}
