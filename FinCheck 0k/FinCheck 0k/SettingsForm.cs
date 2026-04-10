using System;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Windows.Forms;
using Microsoft.Win32;

namespace FinCheck_0k
{
    public partial class SettingsForm : Form
    {
        private const string REG_KEY_PATH = @"SOFTWARE\FinCheck0k";

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
                        txtFolderPath.Text = key.GetValue("FolderPath", "").ToString();
                        txtServerName.Text = key.GetValue("ServerName", "").ToString();
                        numIntervalMinutes.Value = Convert.ToDecimal(key.GetValue("IntervalMinutes", 60));

                        txtSmtpServer.Text = key.GetValue("SmtpServer", "").ToString();
                        numSmtpPort.Value = Convert.ToDecimal(key.GetValue("SmtpPort", 25));
                        chkUseSsl.Checked = Convert.ToBoolean(key.GetValue("SmtpUseSsl", false));
                        txtSmtpUsername.Text = key.GetValue("SmtpUsername", "").ToString();
                        txtSmtpPassword.Text = key.GetValue("SmtpPassword", "").ToString();
                        txtEmailFrom.Text = key.GetValue("EmailFrom", "").ToString();
                        txtEmailTo.Text = key.GetValue("EmailTo", "").ToString();
                        txtEmailSubject.Text = key.GetValue("EmailSubject", "FinCheck 0k Alert").ToString();
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
                        key.SetValue("FolderPath", txtFolderPath.Text);
                        key.SetValue("ServerName", txtServerName.Text);
                        key.SetValue("IntervalMinutes", (int)numIntervalMinutes.Value);

                        key.SetValue("SmtpServer", txtSmtpServer.Text);
                        key.SetValue("SmtpPort", (int)numSmtpPort.Value);
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
                dialog.Description = "Select the folder to monitor";
                dialog.ShowNewFolderButton = false;

                if (dialog.ShowDialog() == DialogResult.OK)
                    txtFolderPath.Text = dialog.SelectedPath;
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
                        client.Credentials = new NetworkCredential(txtSmtpUsername.Text, txtSmtpPassword.Text);

                    using (MailMessage mail = new MailMessage())
                    {
                        mail.From = new MailAddress(txtEmailFrom.Text);

                        foreach (string address in txtEmailTo.Text.Split(new char[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries))
                            mail.To.Add(address.Trim());

                        mail.Subject = "Test Email from FinCheck 0k";
                        mail.Body = "This is a test email from the FinCheck 0k application. If you are receiving this, your email settings are correct.";

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
            if (string.IsNullOrEmpty(txtFolderPath.Text))
            {
                MessageBox.Show("Please select a folder to monitor.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                tabControl1.SelectedTab = tabFolder;
                return;
            }

            if (!Directory.Exists(txtFolderPath.Text))
            {
                MessageBox.Show("The specified folder path does not exist or is not accessible.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                tabControl1.SelectedTab = tabFolder;
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
