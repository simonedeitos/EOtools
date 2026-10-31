namespace PlaylistDownloader
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            this.panel1 = new System.Windows.Forms.Panel();
            this.btnImportISRC = new System.Windows.Forms.Button();
            this.btnImportCSV = new System.Windows.Forms.Button();
            this.btnDeselectAll = new System.Windows.Forms.Button();
            this.btnSelectAll = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.btnExportPlaylists = new System.Windows.Forms.Button();
            this.btnImportPlaylists = new System.Windows.Forms.Button();
            this.btnRemovePlaylist = new System.Windows.Forms.Button();
            this.btnAddPlaylist = new System.Windows.Forms.Button();
            this.dgvPlaylists = new System.Windows.Forms.DataGridView();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.panel2 = new System.Windows.Forms.Panel();
            this.chkAppendResults = new System.Windows.Forms.CheckBox();
            this.lblProgress = new System.Windows.Forms.Label();
            this.progressBarTrack = new System.Windows.Forms.ProgressBar();
            this.progressBarMain = new System.Windows.Forms.ProgressBar();
            this.btnExportCsv = new System.Windows.Forms.Button();
            this.btnSearch = new System.Windows.Forms.Button();
            this.btnDownload = new System.Windows.Forms.Button();
            this.lblResults = new System.Windows.Forms.Label();
            this.dgvResults = new System.Windows.Forms.DataGridView();
            this.panel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvPlaylists)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.panel2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvResults)).BeginInit();
            this.SuspendLayout();
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.btnImportISRC);
            this.panel1.Controls.Add(this.btnImportCSV);
            this.panel1.Controls.Add(this.btnDeselectAll);
            this.panel1.Controls.Add(this.btnSelectAll);
            this.panel1.Controls.Add(this.label1);
            this.panel1.Controls.Add(this.btnExportPlaylists);
            this.panel1.Controls.Add(this.btnImportPlaylists);
            this.panel1.Controls.Add(this.btnRemovePlaylist);
            this.panel1.Controls.Add(this.btnAddPlaylist);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Top;
            this.panel1.Location = new System.Drawing.Point(0, 0);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(784, 70);
            this.panel1.TabIndex = 0;
            // 
            // btnImportISRC
            // 
            this.btnImportISRC.Location = new System.Drawing.Point(490, 13);
            this.btnImportISRC.Name = "btnImportISRC";
            this.btnImportISRC.Size = new System.Drawing.Size(105, 23);
            this.btnImportISRC.TabIndex = 8;
            this.btnImportISRC.Text = "Import txt ISRC";
            this.btnImportISRC.UseVisualStyleBackColor = true;
            this.btnImportISRC.Click += new System.EventHandler(this.btnImportISRC_Click);
            // 
            // btnImportCSV
            // 
            this.btnImportCSV.Location = new System.Drawing.Point(490, 39);
            this.btnImportCSV.Name = "btnImportCSV";
            this.btnImportCSV.Size = new System.Drawing.Size(105, 23);
            this.btnImportCSV.TabIndex = 7;
            this.btnImportCSV.Text = "Import CSV Tracks";
            this.btnImportCSV.UseVisualStyleBackColor = true;
            this.btnImportCSV.Click += new System.EventHandler(this.btnImportCSV_Click);
            // 
            // btnDeselectAll
            // 
            this.btnDeselectAll.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnDeselectAll.Location = new System.Drawing.Point(688, 39);
            this.btnDeselectAll.Name = "btnDeselectAll";
            this.btnDeselectAll.Size = new System.Drawing.Size(84, 23);
            this.btnDeselectAll.TabIndex = 6;
            this.btnDeselectAll.Text = "Deselect All";
            this.btnDeselectAll.UseVisualStyleBackColor = true;
            this.btnDeselectAll.Click += new System.EventHandler(this.btnDeselectAll_Click);
            // 
            // btnSelectAll
            // 
            this.btnSelectAll.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnSelectAll.Location = new System.Drawing.Point(688, 10);
            this.btnSelectAll.Name = "btnSelectAll";
            this.btnSelectAll.Size = new System.Drawing.Size(84, 23);
            this.btnSelectAll.TabIndex = 5;
            this.btnSelectAll.Text = "Select All";
            this.btnSelectAll.UseVisualStyleBackColor = true;
            this.btnSelectAll.Click += new System.EventHandler(this.btnSelectAll_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Segoe UI", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(12, 9);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(189, 25);
            this.label1.TabIndex = 4;
            this.label1.Text = "Playlist Downloader";
            // 
            // btnExportPlaylists
            // 
            this.btnExportPlaylists.Location = new System.Drawing.Point(350, 39);
            this.btnExportPlaylists.Name = "btnExportPlaylists";
            this.btnExportPlaylists.Size = new System.Drawing.Size(105, 23);
            this.btnExportPlaylists.TabIndex = 3;
            this.btnExportPlaylists.Text = "Export Playlists";
            this.btnExportPlaylists.UseVisualStyleBackColor = true;
            this.btnExportPlaylists.Click += new System.EventHandler(this.btnExportPlaylists_Click);
            // 
            // btnImportPlaylists
            // 
            this.btnImportPlaylists.Location = new System.Drawing.Point(239, 39);
            this.btnImportPlaylists.Name = "btnImportPlaylists";
            this.btnImportPlaylists.Size = new System.Drawing.Size(105, 23);
            this.btnImportPlaylists.TabIndex = 2;
            this.btnImportPlaylists.Text = "Import Playlists";
            this.btnImportPlaylists.UseVisualStyleBackColor = true;
            this.btnImportPlaylists.Click += new System.EventHandler(this.btnImportPlaylists_Click);
            // 
            // btnRemovePlaylist
            // 
            this.btnRemovePlaylist.Location = new System.Drawing.Point(126, 39);
            this.btnRemovePlaylist.Name = "btnRemovePlaylist";
            this.btnRemovePlaylist.Size = new System.Drawing.Size(107, 23);
            this.btnRemovePlaylist.TabIndex = 1;
            this.btnRemovePlaylist.Text = "Remove Playlist";
            this.btnRemovePlaylist.UseVisualStyleBackColor = true;
            this.btnRemovePlaylist.Click += new System.EventHandler(this.btnRemovePlaylist_Click);
            // 
            // btnAddPlaylist
            // 
            this.btnAddPlaylist.Location = new System.Drawing.Point(17, 39);
            this.btnAddPlaylist.Name = "btnAddPlaylist";
            this.btnAddPlaylist.Size = new System.Drawing.Size(103, 23);
            this.btnAddPlaylist.TabIndex = 0;
            this.btnAddPlaylist.Text = "Add Playlist";
            this.btnAddPlaylist.UseVisualStyleBackColor = true;
            this.btnAddPlaylist.Click += new System.EventHandler(this.btnAddPlaylist_Click);
            // 
            // dgvPlaylists
            // 
            this.dgvPlaylists.AllowUserToAddRows = false;
            this.dgvPlaylists.AllowUserToDeleteRows = false;
            this.dgvPlaylists.BackgroundColor = System.Drawing.SystemColors.Window;
            this.dgvPlaylists.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvPlaylists.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvPlaylists.Location = new System.Drawing.Point(0, 0);
            this.dgvPlaylists.MultiSelect = false;
            this.dgvPlaylists.Name = "dgvPlaylists";
            this.dgvPlaylists.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvPlaylists.Size = new System.Drawing.Size(784, 299);
            this.dgvPlaylists.TabIndex = 0;
            // 
            // splitContainer1
            // 
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.Location = new System.Drawing.Point(0, 70);
            this.splitContainer1.Name = "splitContainer1";
            this.splitContainer1.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.dgvPlaylists);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.panel2);
            this.splitContainer1.Panel2.Controls.Add(this.dgvResults);
            this.splitContainer1.Size = new System.Drawing.Size(784, 786);
            this.splitContainer1.SplitterDistance = 299;
            this.splitContainer1.TabIndex = 1;
            // 
            // panel2
            // 
            this.panel2.Controls.Add(this.chkAppendResults);
            this.panel2.Controls.Add(this.lblProgress);
            this.panel2.Controls.Add(this.progressBarTrack);
            this.panel2.Controls.Add(this.progressBarMain);
            this.panel2.Controls.Add(this.btnExportCsv);
            this.panel2.Controls.Add(this.btnSearch);
            this.panel2.Controls.Add(this.btnDownload);
            this.panel2.Controls.Add(this.lblResults);
            this.panel2.Dock = System.Windows.Forms.DockStyle.Top;
            this.panel2.Location = new System.Drawing.Point(0, 0);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(784, 78);
            this.panel2.TabIndex = 1;
            // 
            // chkAppendResults
            // 
            this.chkAppendResults.AutoSize = true;
            this.chkAppendResults.Location = new System.Drawing.Point(126, 47);
            this.chkAppendResults.Name = "chkAppendResults";
            this.chkAppendResults.Size = new System.Drawing.Size(101, 17);
            this.chkAppendResults.TabIndex = 8;
            this.chkAppendResults.Text = "Append Results";
            this.chkAppendResults.UseVisualStyleBackColor = true;
            // 
            // lblProgress
            // 
            this.lblProgress.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.lblProgress.AutoSize = true;
            this.lblProgress.Location = new System.Drawing.Point(514, 21);
            this.lblProgress.Name = "lblProgress";
            this.lblProgress.Size = new System.Drawing.Size(100, 13);
            this.lblProgress.TabIndex = 7;
            this.lblProgress.Text = "Processing tracks...";
            this.lblProgress.Visible = false;
            // 
            // progressBarTrack
            // 
            this.progressBarTrack.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.progressBarTrack.Location = new System.Drawing.Point(371, 43);
            this.progressBarTrack.Name = "progressBarTrack";
            this.progressBarTrack.Size = new System.Drawing.Size(401, 21);
            this.progressBarTrack.TabIndex = 6;
            this.progressBarTrack.Visible = false;
            // 
            // progressBarMain
            // 
            this.progressBarMain.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.progressBarMain.Location = new System.Drawing.Point(371, 11);
            this.progressBarMain.Name = "progressBarMain";
            this.progressBarMain.Size = new System.Drawing.Size(128, 23);
            this.progressBarMain.TabIndex = 5;
            this.progressBarMain.Visible = false;
            // 
            // btnExportCsv
            // 
            this.btnExportCsv.Location = new System.Drawing.Point(239, 11);
            this.btnExportCsv.Name = "btnExportCsv";
            this.btnExportCsv.Size = new System.Drawing.Size(105, 23);
            this.btnExportCsv.TabIndex = 3;
            this.btnExportCsv.Text = "Export CSV";
            this.btnExportCsv.UseVisualStyleBackColor = true;
            this.btnExportCsv.Click += new System.EventHandler(this.btnExportCsv_Click);
            // 
            // btnSearch
            // 
            this.btnSearch.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnSearch.Location = new System.Drawing.Point(17, 43);
            this.btnSearch.Name = "btnSearch";
            this.btnSearch.Size = new System.Drawing.Size(103, 23);
            this.btnSearch.TabIndex = 2;
            this.btnSearch.Text = "Search";
            this.btnSearch.UseVisualStyleBackColor = true;
            this.btnSearch.Click += new System.EventHandler(this.btnSearch_Click);
            // 
            // btnDownload
            // 
            this.btnDownload.BackColor = System.Drawing.Color.LightGreen;
            this.btnDownload.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnDownload.Location = new System.Drawing.Point(239, 41);
            this.btnDownload.Name = "btnDownload";
            this.btnDownload.Size = new System.Drawing.Size(105, 26);
            this.btnDownload.TabIndex = 1;
            this.btnDownload.Text = "DOWNLOAD";
            this.btnDownload.UseVisualStyleBackColor = false;
            this.btnDownload.Click += new System.EventHandler(this.btnDownload_Click);
            // 
            // lblResults
            // 
            this.lblResults.AutoSize = true;
            this.lblResults.Font = new System.Drawing.Font("Segoe UI Semibold", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblResults.Location = new System.Drawing.Point(13, 11);
            this.lblResults.Name = "lblResults";
            this.lblResults.Size = new System.Drawing.Size(132, 21);
            this.lblResults.TabIndex = 0;
            this.lblResults.Text = "Results (0 tracks)";
            // 
            // dgvResults
            // 
            this.dgvResults.AllowUserToAddRows = false;
            this.dgvResults.AllowUserToDeleteRows = false;
            this.dgvResults.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.dgvResults.BackgroundColor = System.Drawing.SystemColors.Window;
            this.dgvResults.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvResults.Location = new System.Drawing.Point(0, 78);
            this.dgvResults.MultiSelect = false;
            this.dgvResults.Name = "dgvResults";
            this.dgvResults.ReadOnly = true;
            this.dgvResults.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvResults.Size = new System.Drawing.Size(784, 405);
            this.dgvResults.TabIndex = 0;
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(784, 856);
            this.Controls.Add(this.splitContainer1);
            this.Controls.Add(this.panel1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MinimumSize = new System.Drawing.Size(800, 600);
            this.Name = "MainForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Music Playlist Downloader";
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvPlaylists)).EndInit();
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            this.panel2.ResumeLayout(false);
            this.panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvResults)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Button btnAddPlaylist;
        private System.Windows.Forms.Button btnRemovePlaylist;
        private System.Windows.Forms.Button btnImportPlaylists;
        private System.Windows.Forms.Button btnExportPlaylists;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button btnSelectAll;
        private System.Windows.Forms.Button btnDeselectAll;
        private System.Windows.Forms.DataGridView dgvPlaylists;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.Panel panel2;
        private System.Windows.Forms.Label lblResults;
        private System.Windows.Forms.Button btnDownload;
        private System.Windows.Forms.Button btnSearch;
        private System.Windows.Forms.Button btnExportCsv;
        private System.Windows.Forms.ProgressBar progressBarMain;
        private System.Windows.Forms.ProgressBar progressBarTrack;
        private System.Windows.Forms.Label lblProgress;
        private System.Windows.Forms.DataGridView dgvResults;
        private System.Windows.Forms.CheckBox chkAppendResults;
        private System.Windows.Forms.Button btnImportCSV;
        private System.Windows.Forms.Button btnImportISRC;
    }
}