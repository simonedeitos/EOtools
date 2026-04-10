using System;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Windows.Forms;
using Microsoft.Win32;

namespace DiskSpaceMonitor
{
    public partial class SettingsForm : Form
    {
        private const string REG_KEY_PATH = @"SOFTWARE\DiskSpaceMonitor";

        public SettingsForm()
        {
            InitializeComponent();
        }

        private void SettingsForm_Load(object sender, EventArgs e)
        {
            LoadSettings();
        }

        private void LoadSettings()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(REG_KEY_PATH))
                {
                    if (key != null)
                    {
                        txtDrivePath.Text = key.GetValue("DrivePath", "").ToString();
                        numThreshold.Value = Convert.ToDecimal(key.GetValue("ThresholdPercent", 10));

                        numHour.Value = Convert.ToDecimal(key.GetValue("CheckHour", 9));
                        numMinute.Value = Convert.ToDecimal(key.GetValue("CheckMinute", 0));

                        txtSmtpServer.Text = key.GetValue("SmtpServer", "").ToString();
                        numSmtpPort.Value = Convert.ToDecimal(key.GetValue("SmtpPort", 25));
                        chkUseSsl.Checked = Convert.ToBoolean(key.GetValue("SmtpUseSsl", false));
                        txtSmtpUsername.Text = key.GetValue("SmtpUsername", "").ToString();
                        txtSmtpPassword.Text = key.GetValue("SmtpPassword", "").ToString();
                        txtEmailFrom.Text = key.GetValue("EmailFrom", "").ToString();
                        txtEmailTo.Text = key.GetValue("EmailTo", "").ToString();
                        txtEmailSubject.Text = key.GetValue("EmailSubject", "Disk Space Alert").ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading settings: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SaveSettings()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.CreateSubKey(REG_KEY_PATH))
                {
                    if (key != null)
                    {
                        key.SetValue("DrivePath", txtDrivePath.Text);
                        key.SetValue("ThresholdPercent", numThreshold.Value);

                        key.SetValue("CheckHour", numHour.Value);
                        key.SetValue("CheckMinute", numMinute.Value);

                        key.SetValue("SmtpServer", txtSmtpServer.Text);
                        key.SetValue("SmtpPort", numSmtpPort.Value);
                        key.SetValue("SmtpUseSsl", chkUseSsl.Checked);
                        key.SetValue("SmtpUsername", txtSmtpUsername.Text);
                        key.SetValue("SmtpPassword", txtSmtpPassword.Text);
                        key.SetValue("EmailFrom", txtEmailFrom.Text);
                        key.SetValue("EmailTo", txtEmailTo.Text);
                        key.SetValue("EmailSubject", txtEmailSubject.Text);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving settings: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnBrowse_Click(object sender, EventArgs e)
        {
            using (FolderBrowserDialog dialog = new FolderBrowserDialog())
            {
                dialog.Description = "Select the network drive to monitor";
                dialog.ShowNewFolderButton = false;

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    txtDrivePath.Text = dialog.SelectedPath;
                }
            }
        }

        private void btnTestEmail_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(txtSmtpServer.Text) ||
                string.IsNullOrEmpty(txtEmailFrom.Text) ||
                string.IsNullOrEmpty(txtEmailTo.Text))
            {
                MessageBox.Show("Please fill in all required email settings.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                using (SmtpClient client = new SmtpClient(txtSmtpServer.Text, (int)numSmtpPort.Value))
                {
                    client.EnableSsl = chkUseSsl.Checked;

                    if (!string.IsNullOrEmpty(txtSmtpUsername.Text))
                    {
                        client.Credentials = new NetworkCredential(txtSmtpUsername.Text, txtSmtpPassword.Text);
                    }

                    using (MailMessage mail = new MailMessage())
                    {
                        mail.From = new MailAddress(txtEmailFrom.Text);

                        foreach (string address in txtEmailTo.Text.Split(new char[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries))
                        {
                            mail.To.Add(address.Trim());
                        }

                        mail.Subject = "Test Email from Disk Space Monitor";
                        mail.Body = "This is a test email from the Disk Space Monitor application. If you're receiving this, your email settings are correct.";

                        client.Send(mail);
                    }
                }

                MessageBox.Show("Test email sent successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to send test email: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(txtDrivePath.Text))
            {
                MessageBox.Show("Please select a network drive to monitor.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                tabControl1.SelectedTab = tabDrive;
                return;
            }

            if (!Directory.Exists(txtDrivePath.Text))
            {
                MessageBox.Show("The specified drive path does not exist or is not accessible.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                tabControl1.SelectedTab = tabDrive;
                return;
            }

            SaveSettings();
            DialogResult = DialogResult.OK;
            Close();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }
    }
}