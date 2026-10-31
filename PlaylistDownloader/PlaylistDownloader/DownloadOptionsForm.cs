using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace PlaylistDownloader
{
    public partial class DownloadOptionsForm : Form
    {
        public DownloadOptions DownloadOptions { get; private set; }

        public DownloadOptionsForm()
        {
            InitializeComponent();
            InitializeDefaultValues();
        }

        private void InitializeDefaultValues()
        {
            // Set default download path to Music folder
            txtDownloadPath.Text = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic);

            // Set default values
            chkUseSubfolders.Checked = true;
            numDelaySeconds.Value = 5;
            numDelaySeconds.Enabled = false;
            numTracksPerBatch.Value = 1000;
            numTracksPerBatch.Enabled = false;
        }

        private void btnBrowse_Click(object sender, EventArgs e)
        {
            using (FolderBrowserDialog folderDialog = new FolderBrowserDialog())
            {
                folderDialog.Description = "Select download destination folder";
                folderDialog.SelectedPath = txtDownloadPath.Text;

                if (folderDialog.ShowDialog() == DialogResult.OK)
                {
                    txtDownloadPath.Text = folderDialog.SelectedPath;
                }
            }
        }

        private void chkUseDelay_CheckedChanged(object sender, EventArgs e)
        {
            numDelaySeconds.Enabled = chkUseDelay.Checked;
        }

        private void chkSplitBatch_CheckedChanged(object sender, EventArgs e)
        {
            numTracksPerBatch.Enabled = chkSplitBatch.Checked;
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            // Validate input
            if (string.IsNullOrWhiteSpace(txtDownloadPath.Text))
            {
                MessageBox.Show("Please select a download path.", "Missing Path",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!Directory.Exists(txtDownloadPath.Text))
            {
                try
                {
                    Directory.CreateDirectory(txtDownloadPath.Text);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Cannot create download directory: {ex.Message}",
                        "Invalid Path", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }

            // Create options object
            DownloadOptions = new DownloadOptions
            {
                DownloadPath = txtDownloadPath.Text,
                UseSubfolders = chkUseSubfolders.Checked,
                UseDelay = chkUseDelay.Checked,
                DelaySeconds = (int)numDelaySeconds.Value,
                DelayInterval = 50, // Fixed at 50 songs
                SplitBatchFiles = chkSplitBatch.Checked,
                TracksPerBatch = (int)numTracksPerBatch.Value,
                SaveDuplicatesWithISRC = chkSaveDuplicatesWithISRC.Checked
            };

            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }
    }
}