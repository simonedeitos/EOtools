namespace PlaylistDownloader
{
    partial class DownloadOptionsForm
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
            this.lblDownloadPath = new System.Windows.Forms.Label();
            this.txtDownloadPath = new System.Windows.Forms.TextBox();
            this.btnBrowse = new System.Windows.Forms.Button();
            this.chkUseSubfolders = new System.Windows.Forms.CheckBox();
            this.chkUseDelay = new System.Windows.Forms.CheckBox();
            this.numDelaySeconds = new System.Windows.Forms.NumericUpDown();
            this.lblSeconds = new System.Windows.Forms.Label();
            this.chkSplitBatch = new System.Windows.Forms.CheckBox();
            this.numTracksPerBatch = new System.Windows.Forms.NumericUpDown();
            this.lblTracks = new System.Windows.Forms.Label();
            this.chkSaveDuplicatesWithISRC = new System.Windows.Forms.CheckBox();
            this.lblHelp = new System.Windows.Forms.Label();
            this.btnOK = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.numDelaySeconds)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numTracksPerBatch)).BeginInit();
            this.SuspendLayout();
            // 
            // lblDownloadPath
            // 
            this.lblDownloadPath.AutoSize = true;
            this.lblDownloadPath.Location = new System.Drawing.Point(17, 24);
            this.lblDownloadPath.Name = "lblDownloadPath";
            this.lblDownloadPath.Size = new System.Drawing.Size(83, 13);
            this.lblDownloadPath.TabIndex = 0;
            this.lblDownloadPath.Text = "Download Path:";
            // 
            // txtDownloadPath
            // 
            this.txtDownloadPath.Location = new System.Drawing.Point(106, 20);
            this.txtDownloadPath.Name = "txtDownloadPath";
            this.txtDownloadPath.Size = new System.Drawing.Size(304, 20);
            this.txtDownloadPath.TabIndex = 1;
            // 
            // btnBrowse
            // 
            this.btnBrowse.Location = new System.Drawing.Point(420, 19);
            this.btnBrowse.Name = "btnBrowse";
            this.btnBrowse.Size = new System.Drawing.Size(60, 22);
            this.btnBrowse.TabIndex = 2;
            this.btnBrowse.Text = "Browse";
            this.btnBrowse.UseVisualStyleBackColor = true;
            this.btnBrowse.Click += new System.EventHandler(this.btnBrowse_Click);
            // 
            // chkUseSubfolders
            // 
            this.chkUseSubfolders.AutoSize = true;
            this.chkUseSubfolders.Location = new System.Drawing.Point(20, 60);
            this.chkUseSubfolders.Name = "chkUseSubfolders";
            this.chkUseSubfolders.Size = new System.Drawing.Size(162, 17);
            this.chkUseSubfolders.TabIndex = 3;
            this.chkUseSubfolders.Text = "Create subfolders for playlists";
            this.chkUseSubfolders.UseVisualStyleBackColor = true;
            // 
            // chkUseDelay
            // 
            this.chkUseDelay.AutoSize = true;
            this.chkUseDelay.Location = new System.Drawing.Point(20, 90);
            this.chkUseDelay.Name = "chkUseDelay";
            this.chkUseDelay.Size = new System.Drawing.Size(151, 17);
            this.chkUseDelay.TabIndex = 4;
            this.chkUseDelay.Text = "Add delay every 50 songs:";
            this.chkUseDelay.UseVisualStyleBackColor = true;
            this.chkUseDelay.CheckedChanged += new System.EventHandler(this.chkUseDelay_CheckedChanged);
            // 
            // numDelaySeconds
            // 
            this.numDelaySeconds.Location = new System.Drawing.Point(230, 89);
            this.numDelaySeconds.Maximum = new decimal(new int[] {
            300,
            0,
            0,
            0});
            this.numDelaySeconds.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numDelaySeconds.Name = "numDelaySeconds";
            this.numDelaySeconds.Size = new System.Drawing.Size(60, 20);
            this.numDelaySeconds.TabIndex = 5;
            this.numDelaySeconds.Value = new decimal(new int[] {
            5,
            0,
            0,
            0});
            // 
            // lblSeconds
            // 
            this.lblSeconds.AutoSize = true;
            this.lblSeconds.Location = new System.Drawing.Point(300, 92);
            this.lblSeconds.Name = "lblSeconds";
            this.lblSeconds.Size = new System.Drawing.Size(47, 13);
            this.lblSeconds.TabIndex = 6;
            this.lblSeconds.Text = "seconds";
            // 
            // chkSplitBatch
            // 
            this.chkSplitBatch.AutoSize = true;
            this.chkSplitBatch.Location = new System.Drawing.Point(20, 120);
            this.chkSplitBatch.Name = "chkSplitBatch";
            this.chkSplitBatch.Size = new System.Drawing.Size(187, 17);
            this.chkSplitBatch.TabIndex = 7;
            this.chkSplitBatch.Text = "Split into multiple batch files every:";
            this.chkSplitBatch.UseVisualStyleBackColor = true;
            this.chkSplitBatch.CheckedChanged += new System.EventHandler(this.chkSplitBatch_CheckedChanged);
            // 
            // numTracksPerBatch
            // 
            this.numTracksPerBatch.Increment = new decimal(new int[] {
            100,
            0,
            0,
            0});
            this.numTracksPerBatch.Location = new System.Drawing.Point(230, 119);
            this.numTracksPerBatch.Maximum = new decimal(new int[] {
            5000,
            0,
            0,
            0});
            this.numTracksPerBatch.Minimum = new decimal(new int[] {
            100,
            0,
            0,
            0});
            this.numTracksPerBatch.Name = "numTracksPerBatch";
            this.numTracksPerBatch.Size = new System.Drawing.Size(60, 20);
            this.numTracksPerBatch.TabIndex = 8;
            this.numTracksPerBatch.Value = new decimal(new int[] {
            1000,
            0,
            0,
            0});
            // 
            // lblTracks
            // 
            this.lblTracks.AutoSize = true;
            this.lblTracks.Location = new System.Drawing.Point(300, 121);
            this.lblTracks.Name = "lblTracks";
            this.lblTracks.Size = new System.Drawing.Size(36, 13);
            this.lblTracks.TabIndex = 9;
            this.lblTracks.Text = "tracks";
            // 
            // chkSaveDuplicatesWithISRC
            // 
            this.chkSaveDuplicatesWithISRC.AutoSize = true;
            this.chkSaveDuplicatesWithISRC.Location = new System.Drawing.Point(20, 150);
            this.chkSaveDuplicatesWithISRC.Name = "chkSaveDuplicatesWithISRC";
            this.chkSaveDuplicatesWithISRC.Size = new System.Drawing.Size(205, 17);
            this.chkSaveDuplicatesWithISRC.TabIndex = 10;
            this.chkSaveDuplicatesWithISRC.Text = "Save duplicates with ISRC in filename";
            this.chkSaveDuplicatesWithISRC.UseVisualStyleBackColor = true;
            // 
            // lblHelp
            // 
            this.lblHelp.ForeColor = System.Drawing.Color.Gray;
            this.lblHelp.Location = new System.Drawing.Point(20, 180);
            this.lblHelp.Name = "lblHelp";
            this.lblHelp.Size = new System.Drawing.Size(450, 35);
            this.lblHelp.TabIndex = 11;
            this.lblHelp.Text = "Note: By default, duplicate tracks are skipped. Enable the option above\r\nto save " +
    "duplicates with ISRC code in the filename.";
            // 
            // btnOK
            // 
            this.btnOK.Location = new System.Drawing.Point(320, 230);
            this.btnOK.Name = "btnOK";
            this.btnOK.Size = new System.Drawing.Size(75, 30);
            this.btnOK.TabIndex = 12;
            this.btnOK.Text = "OK";
            this.btnOK.UseVisualStyleBackColor = true;
            this.btnOK.Click += new System.EventHandler(this.btnOK_Click);
            // 
            // btnCancel
            // 
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.Location = new System.Drawing.Point(405, 230);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(75, 30);
            this.btnCancel.TabIndex = 13;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            // 
            // DownloadOptionsForm
            // 
            this.AcceptButton = this.btnOK;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.btnCancel;
            this.ClientSize = new System.Drawing.Size(500, 280);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnOK);
            this.Controls.Add(this.lblHelp);
            this.Controls.Add(this.chkSaveDuplicatesWithISRC);
            this.Controls.Add(this.lblTracks);
            this.Controls.Add(this.numTracksPerBatch);
            this.Controls.Add(this.chkSplitBatch);
            this.Controls.Add(this.lblSeconds);
            this.Controls.Add(this.numDelaySeconds);
            this.Controls.Add(this.chkUseDelay);
            this.Controls.Add(this.chkUseSubfolders);
            this.Controls.Add(this.btnBrowse);
            this.Controls.Add(this.txtDownloadPath);
            this.Controls.Add(this.lblDownloadPath);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "DownloadOptionsForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Download Options";
            ((System.ComponentModel.ISupportInitialize)(this.numDelaySeconds)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numTracksPerBatch)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label lblDownloadPath;
        private System.Windows.Forms.TextBox txtDownloadPath;
        private System.Windows.Forms.Button btnBrowse;
        private System.Windows.Forms.CheckBox chkUseSubfolders;
        private System.Windows.Forms.CheckBox chkUseDelay;
        private System.Windows.Forms.NumericUpDown numDelaySeconds;
        private System.Windows.Forms.Label lblSeconds;
        private System.Windows.Forms.CheckBox chkSplitBatch;
        private System.Windows.Forms.NumericUpDown numTracksPerBatch;
        private System.Windows.Forms.Label lblTracks;
        private System.Windows.Forms.CheckBox chkSaveDuplicatesWithISRC;
        private System.Windows.Forms.Label lblHelp;
        private System.Windows.Forms.Button btnOK;
        private System.Windows.Forms.Button btnCancel;
    }
}