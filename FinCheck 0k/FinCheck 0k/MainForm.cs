using System;
using System.Drawing;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Windows.Forms;
using Microsoft.Win32;

namespace FinCheck_0k
{
    public partial class MainForm : Form
    {
        private const string REG_KEY_PATH = @"SOFTWARE\FinCheck0k";

        private string folderPath = string.Empty;
        private string serverName = string.Empty;
        private int intervalMinutes = 60;
        private DateTime? nextCheckTime = null;
        private bool checkExecutionInProgress = false;

        // Email settings
        private string smtpServer = string.Empty;
        private int smtpPort = 25;
        private bool smtpUseSsl = false;
        private string smtpUsername = string.Empty;
        private string smtpPassword = string.Empty;
        private string emailFrom = string.Empty;
        private string emailTo = string.Empty;
        private string emailSubject = "FinCheck 0k Alert";

        public MainForm()
        {
            InitializeComponent();
            SetupNotifyIcon();
        }

        private void SetupNotifyIcon()
        {
            try
            {
                if (this.Icon != null)
                    notifyIcon.Icon = this.Icon;
                else
                    notifyIcon.Icon = CreateSimpleIcon();
            }
            catch
            {
                notifyIcon.Icon = CreateSimpleIcon();
            }

            notifyIcon.Text = "FinCheck 0k";
            notifyIcon.Visible = false;
        }

        private Icon CreateSimpleIcon()
        {
            Bitmap bitmap = new Bitmap(16, 16);
            using (Graphics g = Graphics.FromImage(bitmap))
            {
                g.Clear(Color.Transparent);
                g.FillEllipse(Brushes.Orange, 2, 2, 12, 12);
                g.DrawEllipse(Pens.DarkOrange, 2, 2, 12, 12);
            }
            IntPtr hIcon = bitmap.GetHicon();
            return Icon.FromHandle(hIcon);
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            timerChecker.Interval = 10000;

            LoadSettings();

            if (!string.IsNullOrEmpty(folderPath))
            {
                lblFolderPath.Text = $"Folder: {folderPath}";
                ScheduleNextCheck();
                timerClock.Start();
                timerChecker.Start();
            }
            else
            {
                MessageBox.Show("No folder is configured for monitoring. Please configure settings.",
                    "Configuration Required", MessageBoxButtons.OK, MessageBoxIcon.Information);
                ShowSettingsForm();
            }
        }

        private void LoadSettings()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(REG_KEY_PATH))
                {
                    if (key != null)
                    {
                        folderPath = key.GetValue("FolderPath", "").ToString();
                        serverName = key.GetValue("ServerName", "").ToString();
                        intervalMinutes = Convert.ToInt32(key.GetValue("IntervalMinutes", 60));

                        smtpServer = key.GetValue("SmtpServer", "").ToString();
                        smtpPort = Convert.ToInt32(key.GetValue("SmtpPort", 25));
                        smtpUseSsl = Convert.ToBoolean(key.GetValue("SmtpUseSsl", false));
                        smtpUsername = key.GetValue("SmtpUsername", "").ToString();
                        smtpPassword = key.GetValue("SmtpPassword", "").ToString();
                        emailFrom = key.GetValue("EmailFrom", "").ToString();
                        emailTo = key.GetValue("EmailTo", "").ToString();
                        emailSubject = key.GetValue("EmailSubject", "FinCheck 0k Alert").ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading settings: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ScheduleNextCheck()
        {
            nextCheckTime = DateTime.Now.AddMinutes(intervalMinutes);
            UpdateNextCheckLabel();
            LogToFile("check_log.txt", $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - Next check scheduled at: {nextCheckTime.Value:yyyy-MM-dd HH:mm:ss}");
        }

        private void UpdateNextCheckLabel()
        {
            if (nextCheckTime.HasValue)
                lblNextCheck.Text = $"Next Check: {nextCheckTime.Value:HH:mm:ss}";
            else
                lblNextCheck.Text = "Next Check: Not Set";
        }

        private void PerformCheck()
        {
            if (checkExecutionInProgress)
                return;

            checkExecutionInProgress = true;

            try
            {
                LogToFile("check_log.txt", $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - Starting check on: {folderPath}");

                if (string.IsNullOrEmpty(folderPath) || !Directory.Exists(folderPath))
                {
                    string msg = "The configured folder path does not exist or is not accessible.";
                    lblResult.Text = $"Result: {msg}";
                    lblResult.ForeColor = Color.Red;
                    LogToFile("check_log.txt", $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - ERROR: {msg}");
                    return;
                }

                string[] allFiles = Directory.GetFiles(folderPath, "*", SearchOption.AllDirectories);
                System.Collections.Generic.List<string> zeroFiles = new System.Collections.Generic.List<string>();

                foreach (string file in allFiles)
                {
                    try
                    {
                        FileInfo fi = new FileInfo(file);
                        if (fi.Length == 0)
                            zeroFiles.Add(file);
                    }
                    catch { /* skip inaccessible files */ }
                }

                lblLastCheck.Text = $"Last Check: {DateTime.Now:yyyy-MM-dd HH:mm:ss}";

                if (zeroFiles.Count == 0)
                {
                    lblResult.Text = "Result: Nessun file a 0 KB trovato";
                    lblResult.ForeColor = Color.Green;
                    LogToFile("check_log.txt", $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - OK: No 0 KB files found.");
                }
                else
                {
                    lblResult.Text = $"Result: Trovati {zeroFiles.Count} file a 0 KB - alert inviato";
                    lblResult.ForeColor = Color.Red;
                    LogToFile("check_log.txt", $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - ALERT: Found {zeroFiles.Count} zero-byte files. Sending email.");
                    SendAlert(zeroFiles);
                }

                ScheduleNextCheck();
            }
            finally
            {
                checkExecutionInProgress = false;
            }
        }

        private void SendAlert(System.Collections.Generic.List<string> zeroFiles)
        {
            if (string.IsNullOrEmpty(smtpServer) ||
                string.IsNullOrEmpty(emailFrom) ||
                string.IsNullOrEmpty(emailTo))
            {
                MessageBox.Show("Email settings are not configured properly. Unable to send alert.",
                    "Alert Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                string fileList = string.Join("\n", zeroFiles);
                string body = "Attenzione:\n" +
                              "Sono stati trovati i seguenti file con dimensione uguale a 0 KB\n\n" +
                              fileList + "\n\n" +
                              "Verificare quanto prima.\n" +
                              $"Server Name: {serverName}";

                using (SmtpClient client = new SmtpClient(smtpServer, smtpPort))
                {
                    client.EnableSsl = smtpUseSsl;

                    if (!string.IsNullOrEmpty(smtpUsername))
                        client.Credentials = new NetworkCredential(smtpUsername, smtpPassword);

                    using (MailMessage mail = new MailMessage())
                    {
                        mail.From = new MailAddress(emailFrom);

                        foreach (string address in emailTo.Split(new char[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries))
                            mail.To.Add(address.Trim());

                        mail.Subject = emailSubject;
                        mail.Body = body;

                        client.Send(mail);
                    }
                }

                notifyIcon.ShowBalloonTip(5000, "FinCheck 0k Alert",
                    $"Found {zeroFiles.Count} zero-byte file(s). Alert email sent.",
                    ToolTipIcon.Warning);

                LogToFile("check_log.txt", $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - Alert email sent successfully.");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to send alert email: {ex.Message}", "Alert Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                LogToFile("check_log.txt", $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - ERROR sending email: {ex.Message}");
            }
        }

        private void LogToFile(string fileName, string message)
        {
            try
            {
                string appPath = Path.GetDirectoryName(Application.ExecutablePath);
                string logPath = Path.Combine(appPath, fileName);
                using (StreamWriter writer = new StreamWriter(logPath, true))
                    writer.WriteLine(message);
            }
            catch { /* ignore logging errors */ }
        }

        private void ShowSettingsForm()
        {
            using (SettingsForm settingsForm = new SettingsForm())
            {
                if (settingsForm.ShowDialog() == DialogResult.OK)
                {
                    LoadSettings();

                    lblFolderPath.Text = string.IsNullOrEmpty(folderPath)
                        ? "Folder: Not Configured"
                        : $"Folder: {folderPath}";

                    if (!string.IsNullOrEmpty(folderPath))
                    {
                        ScheduleNextCheck();
                        timerClock.Start();
                        timerChecker.Start();
                    }
                }
            }
        }

        private void timerClock_Tick(object sender, EventArgs e)
        {
            if (nextCheckTime.HasValue)
            {
                DateTime now = DateTime.Now;
                TimeSpan remaining = nextCheckTime.Value - now;

                if (remaining.TotalSeconds > 0)
                {
                    lblNextCheck.Text = $"Next Check: {nextCheckTime.Value:HH:mm:ss} " +
                                        $"({(int)remaining.TotalHours}h {remaining.Minutes}m {remaining.Seconds}s)";
                }
                else
                {
                    LogToFile("check_log.txt", $"{now:yyyy-MM-dd HH:mm:ss} - [timerClock] Countdown expired. Running check.");
                    PerformCheck();
                }
            }
        }

        private void timerChecker_Tick(object sender, EventArgs e)
        {
            DateTime now = DateTime.Now;
            if (nextCheckTime.HasValue &&
                now > nextCheckTime.Value &&
                (now - nextCheckTime.Value).TotalMinutes >= 1)
            {
                LogToFile("check_log.txt", $"{now:yyyy-MM-dd HH:mm:ss} - [timerChecker] Safety check: missed check detected.");
                PerformCheck();
            }
        }

        private void btnCheckNow_Click(object sender, EventArgs e)
        {
            PerformCheck();
        }

        private void btnSettings_Click(object sender, EventArgs e)
        {
            ShowSettingsForm();
        }

        private void MainForm_Resize(object sender, EventArgs e)
        {
            if (WindowState == FormWindowState.Minimized)
            {
                Hide();
                notifyIcon.Visible = true;
                notifyIcon.ShowBalloonTip(2000, "FinCheck 0k",
                    "Application minimized to system tray. Double-click to restore.",
                    ToolTipIcon.Info);
            }
        }

        private void notifyIcon_DoubleClick(object sender, EventArgs e)
        {
            Show();
            WindowState = FormWindowState.Normal;
            notifyIcon.Visible = false;
            this.BringToFront();
            this.Activate();
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Show();
            WindowState = FormWindowState.Normal;
            notifyIcon.Visible = false;
            this.BringToFront();
            this.Activate();
        }

        private void checkNowToolStripMenuItem_Click(object sender, EventArgs e)
        {
            PerformCheck();
        }

        private void settingsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Show();
            WindowState = FormWindowState.Normal;
            notifyIcon.Visible = false;
            ShowSettingsForm();
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            notifyIcon.Visible = false;
            Application.Exit();
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                WindowState = FormWindowState.Minimized;
                Hide();
                notifyIcon.Visible = true;
                notifyIcon.ShowBalloonTip(3000, "FinCheck 0k",
                    "Application minimized to system tray. Double-click to restore.",
                    ToolTipIcon.Info);
            }
            else
            {
                notifyIcon.Visible = false;
            }
        }
    }
}
