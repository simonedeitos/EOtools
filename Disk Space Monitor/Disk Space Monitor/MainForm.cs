using System;
using System.Drawing;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Windows.Forms;
using Microsoft.Win32;
using System.Diagnostics;

namespace DiskSpaceMonitor
{
    public partial class MainForm : Form
    {
        private const string REG_KEY_PATH = @"SOFTWARE\DiskSpaceMonitor";
        private string drivePath = string.Empty;
        private int thresholdPercent = 10;
        private int checkHour = 9;
        private int checkMinute = 0;
        private DateTime? nextCheckTime = null;
        private DateTime lastScheduledCheckDate = DateTime.MinValue;
        private bool checkExecutionInProgress = false; // Flag per evitare esecuzioni multiple simultanee

        // Aggiungi una variabile per memorizzare lo spazio libero precedente
        private long previousFreeBytes = -1;

        // Email settings
        private string smtpServer = string.Empty;
        private int smtpPort = 25;
        private bool smtpUseSsl = false;
        private string smtpUsername = string.Empty;
        private string smtpPassword = string.Empty;
        private string emailFrom = string.Empty;
        private string emailTo = string.Empty;
        private string emailSubject = "Disk Space Alert";

        public MainForm()
        {
            InitializeComponent();

            // Imposta l'icona per la system tray
            SetupNotifyIcon();
        }

        private void SetupNotifyIcon()
        {
            // Se hai un file .ico nella cartella Resources del progetto, usa quello
            // altrimenti crea un'icona semplice dal form
            try
            {
                // Prova prima a usare l'icona del form
                if (this.Icon != null)
                {
                    notifyIcon.Icon = this.Icon;
                }
                else
                {
                    // Crea un'icona semplice se non ne esiste una
                    notifyIcon.Icon = CreateSimpleIcon();
                }
            }
            catch
            {
                // Se tutto fallisce, crea un'icona di base
                notifyIcon.Icon = CreateSimpleIcon();
            }

            // Imposta il tooltip
            notifyIcon.Text = "Disk Space Monitor";

            // Assicurati che l'icona sia inizialmente nascosta
            notifyIcon.Visible = false;
        }

        private Icon CreateSimpleIcon()
        {
            // Crea un'icona semplice 16x16 pixel
            Bitmap bitmap = new Bitmap(16, 16);
            using (Graphics g = Graphics.FromImage(bitmap))
            {
                // Disegna un cerchio colorato come icona
                g.Clear(Color.Transparent);
                g.FillEllipse(Brushes.Blue, 2, 2, 12, 12);
                g.DrawEllipse(Pens.DarkBlue, 2, 2, 12, 12);
            }

            IntPtr hIcon = bitmap.GetHicon();
            Icon icon = Icon.FromHandle(hIcon);
            return icon;
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            // Imposta il timer checker a 10 secondi per verifiche più frequenti
            timerChecker.Interval = 10000; // 10 secondi invece di 60 secondi/1 minuto

            // Load settings from registry
            LoadSettings();

            // Check if drive is configured
            if (!string.IsNullOrEmpty(drivePath))
            {
                // Update UI with drive path
                lblDrivePath.Text = $"Drive: {drivePath}";

                // Perform initial check
                CheckDriveSpace();

                // Calculate next check time
                CalculateNextCheckTime();

                // Start timers
                timerClock.Start();
                timerChecker.Start();
            }
            else
            {
                // No drive configured, prompt user to configure settings
                MessageBox.Show("No drive is configured for monitoring. Please configure settings.",
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
                        drivePath = key.GetValue("DrivePath", "").ToString();
                        thresholdPercent = Convert.ToInt32(key.GetValue("ThresholdPercent", 10));

                        checkHour = Convert.ToInt32(key.GetValue("CheckHour", 9));
                        checkMinute = Convert.ToInt32(key.GetValue("CheckMinute", 0));

                        smtpServer = key.GetValue("SmtpServer", "").ToString();
                        smtpPort = Convert.ToInt32(key.GetValue("SmtpPort", 25));
                        smtpUseSsl = Convert.ToBoolean(key.GetValue("SmtpUseSsl", false));
                        smtpUsername = key.GetValue("SmtpUsername", "").ToString();
                        smtpPassword = key.GetValue("SmtpPassword", "").ToString();
                        emailFrom = key.GetValue("EmailFrom", "").ToString();
                        emailTo = key.GetValue("EmailTo", "").ToString();
                        emailSubject = key.GetValue("EmailSubject", "Disk Space Alert").ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading settings: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void CalculateNextCheckTime()
        {
            DateTime now = DateTime.Now;
            DateTime checkTime = new DateTime(now.Year, now.Month, now.Day, checkHour, checkMinute, 0);

            if (checkTime <= now)
            {
                // Se l'orario di controllo è già passato per oggi, programmalo per domani
                checkTime = checkTime.AddDays(1);
            }

            nextCheckTime = checkTime;
            UpdateNextCheckLabel();

            // Log per debug
            LogToFile("scheduler_log.txt", $"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")} - Orario controllo calcolato: {nextCheckTime.Value.ToString("yyyy-MM-dd HH:mm:ss")}");
        }

        private void UpdateNextCheckLabel()
        {
            if (nextCheckTime.HasValue)
            {
                lblNextCheck.Text = $"Next Check: {nextCheckTime.Value.ToString("HH:mm:ss")}";
            }
            else
            {
                lblNextCheck.Text = "Next Check: Not Set";
            }
        }

        private bool CheckDriveSpace()
        {
            try
            {
                if (string.IsNullOrEmpty(drivePath) || !Directory.Exists(drivePath))
                {
                    MessageBox.Show("The configured drive path does not exist or is not accessible.",
                        "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }

                DriveInfo driveInfo = new DriveInfo(drivePath);

                // Calculate values
                long totalBytes = driveInfo.TotalSize;
                long freeBytes = driveInfo.AvailableFreeSpace;
                long usedBytes = totalBytes - freeBytes;

                // Calculate percentages
                double freePercent = (double)freeBytes / totalBytes * 100;
                double usedPercent = (double)usedBytes / totalBytes * 100;

                // Calculate threshold in bytes
                long thresholdBytes = (long)(totalBytes * thresholdPercent / 100.0);

                // Update UI
                lblTotalSpace.Text = $"Total Space: {FormatBytes(totalBytes)}";
                lblUsedSpace.Text = $"Used Space: {FormatBytes(usedBytes)} ({usedPercent:N1}%)";
                lblFreeSpace.Text = $"Free Space: {FormatBytes(freeBytes)} ({freePercent:N1}%)";
                lblThreshold.Text = $"Threshold: {thresholdPercent}% ({FormatBytes(thresholdBytes)})";

                // Update progress bar
                progressBarDisk.Value = (int)usedPercent;

                // Change color based on free space and send alert if needed
                if (freePercent <= thresholdPercent)
                {
                    progressBarDisk.ForeColor = Color.Red;

                    // Send alert every time we check and space is below threshold
                    SendAlert(driveInfo);
                }
                else
                {
                    progressBarDisk.ForeColor = Color.Green;
                }

                // Update last check label
                lblLastCheck.Text = $"Last Check: {DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}";

                // Scrivi il report su file
                WriteReportToFile(freeBytes);

                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error checking drive space: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        private void WriteReportToFile(long currentFreeBytes)
        {
            try
            {
                // Ottieni il percorso della directory dell'applicazione
                string appPath = Path.GetDirectoryName(Application.ExecutablePath);
                string reportPath = Path.Combine(appPath, "report.txt");

                // Calcola la differenza rispetto al controllo precedente
                string difference = "";
                if (previousFreeBytes >= 0)
                {
                    long diffBytes = currentFreeBytes - previousFreeBytes;
                    string sign = diffBytes >= 0 ? "+" : "";
                    difference = $"{sign}{FormatBytes(diffBytes)}";
                }
                else
                {
                    difference = "N/A (primo controllo)";
                }

                // Formatta la riga del report
                string reportLine = $"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}, Spazio libero: {FormatBytes(currentFreeBytes)}, Differenza: {difference}";

                // Scrivi su file (append)
                using (StreamWriter writer = new StreamWriter(reportPath, true))
                {
                    writer.WriteLine(reportLine);
                }

                // Aggiorna il valore precedente per il prossimo controllo
                previousFreeBytes = currentFreeBytes;
            }
            catch (Exception ex)
            {
                // Log dell'errore - potresti volerlo gestire diversamente
                MessageBox.Show($"Error writing to report file: {ex.Message}", "Report Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Aggiungi questo metodo di utility per il logging
        private void LogToFile(string fileName, string message)
        {
            try
            {
                string appPath = Path.GetDirectoryName(Application.ExecutablePath);
                string logPath = Path.Combine(appPath, fileName);

                using (StreamWriter writer = new StreamWriter(logPath, true))
                {
                    writer.WriteLine(message);
                }
            }
            catch
            {
                // Ignora eventuali errori di logging, non dovrebbero interrompere l'applicazione
                // In un'applicazione di produzione, potresti voler gestire questi errori in modo più appropriato
            }
        }

        private string FormatBytes(long bytes)
        {
            string[] suffixes = { "B", "KB", "MB", "GB", "TB", "PB" };
            int counter = 0;
            decimal number = Math.Abs(bytes);  // Usa il valore assoluto per il formato

            while (number >= 1024 && counter < suffixes.Length - 1)
            {
                number /= 1024;
                counter++;
            }

            // Aggiungi il segno originale dopo il calcolo del valore assoluto
            string sign = bytes < 0 ? "-" : "";
            return $"{sign}{number:N2} {suffixes[counter]}";
        }

        private void SendAlert(DriveInfo driveInfo)
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
                using (SmtpClient client = new SmtpClient(smtpServer, smtpPort))
                {
                    client.EnableSsl = smtpUseSsl;

                    if (!string.IsNullOrEmpty(smtpUsername))
                    {
                        client.Credentials = new NetworkCredential(smtpUsername, smtpPassword);
                    }

                    using (MailMessage mail = new MailMessage())
                    {
                        mail.From = new MailAddress(emailFrom);

                        foreach (string address in emailTo.Split(new char[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries))
                        {
                            mail.To.Add(address.Trim());
                        }

                        mail.Subject = emailSubject;

                        // Calculate values for email body
                        long totalBytes = driveInfo.TotalSize;
                        long freeBytes = driveInfo.AvailableFreeSpace;
                        long usedBytes = totalBytes - freeBytes;
                        double freePercent = (double)freeBytes / totalBytes * 100;
                        long thresholdBytes = (long)(totalBytes * thresholdPercent / 100.0);

                        mail.Body = $"Low disk space alert for {drivePath}\n\n" +
                                   $"Total space: {FormatBytes(totalBytes)}\n" +
                                   $"Used space: {FormatBytes(usedBytes)}\n" +
                                   $"Free space: {FormatBytes(freeBytes)} ({freePercent:N1}%)\n" +
                                   $"Threshold: {thresholdPercent}% ({FormatBytes(thresholdBytes)})\n\n" +
                                   $"The free space is below the configured threshold of {thresholdPercent}%.\n\n" +
                                   $"This alert was generated on {DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}";

                        client.Send(mail);

                        // Log successful email send (optional - you could also log to a file)
                        string logMessage = $"Alert email sent successfully at {DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")} - Free space: {freePercent:N1}%";

                        // Notify user that alert was sent
                        notifyIcon.ShowBalloonTip(5000, "Disk Space Alert",
                            $"Low disk space alert for {drivePath} sent via email. Free space: {freePercent:N1}%",
                            ToolTipIcon.Warning);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to send alert email: {ex.Message}", "Alert Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ShowSettingsForm()
        {
            using (SettingsForm settingsForm = new SettingsForm())
            {
                if (settingsForm.ShowDialog() == DialogResult.OK)
                {
                    // Reload settings
                    LoadSettings();

                    // Update UI
                    lblDrivePath.Text = $"Drive: {drivePath}";

                    // Perform check
                    CheckDriveSpace();

                    // Calculate next check time
                    CalculateNextCheckTime();

                    // Ensure timers are running
                    timerClock.Start();
                    timerChecker.Start();
                }
            }
        }

        private void btnCheckNow_Click(object sender, EventArgs e)
        {
            CheckDriveSpace();
        }

        private void btnSettings_Click(object sender, EventArgs e)
        {
            ShowSettingsForm();
        }

        // Metodo per eseguire il controllo programmato
        private void ExecuteScheduledCheck()
        {
            if (checkExecutionInProgress)
                return;

            checkExecutionInProgress = true;

            try
            {
                // Registra il momento in cui viene eseguito il check programmato
                string logMessage = $"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")} - ESECUZIONE DEL CONTROLLO PROGRAMMATO";
                LogToFile("scheduler_log.txt", logMessage);

                // Esegui il controllo dello spazio su disco
                bool checkResult = CheckDriveSpace();

                // Registra il risultato
                logMessage = $"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")} - Risultato del controllo: {(checkResult ? "Successo" : "Fallito")}";
                LogToFile("scheduler_log.txt", logMessage);

                // Aggiorna l'ultimo controllo eseguito
                lastScheduledCheckDate = DateTime.Now;

                // Ricalcola il prossimo orario di controllo (per il giorno successivo)
                CalculateNextCheckTime();

                // Registra il prossimo controllo programmato
                logMessage = $"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")} - Prossimo controllo programmato: {nextCheckTime.Value.ToString("yyyy-MM-dd HH:mm:ss")}";
                LogToFile("scheduler_log.txt", logMessage);
            }
            finally
            {
                checkExecutionInProgress = false;
            }
        }

        private void timerChecker_Tick(object sender, EventArgs e)
        {
            // Questo timer è solo di supporto e verifica meno frequentemente 
            // rispetto al timerClock, che gestisce il countdown principale

            // È comunque utile per verificare che nessun controllo sia stato perso
            // nel caso in cui ci siano problemi con il timerClock
            DateTime now = DateTime.Now;

            if (nextCheckTime.HasValue &&
                now > nextCheckTime.Value &&
                (now - nextCheckTime.Value).TotalMinutes >= 1 && // È passato più di 1 minuto
                (lastScheduledCheckDate.Date != now.Date ||
                 lastScheduledCheckDate.Hour != checkHour ||
                 lastScheduledCheckDate.Minute != checkMinute))
            {
                LogToFile("scheduler_log.txt", $"{now.ToString("yyyy-MM-dd HH:mm:ss")} - [timerChecker] Controllo di sicurezza: è stato rilevato un controllo mancato");
                ExecuteScheduledCheck();
            }
        }

        private void timerClock_Tick(object sender, EventArgs e)
        {
            // FONDAMENTALE: questo è il timer che gestisce il countdown e deve eseguire
            // il controllo programmato quando il countdown arriva a zero

            if (nextCheckTime.HasValue)
            {
                DateTime now = DateTime.Now;
                TimeSpan remaining = nextCheckTime.Value - now;

                if (remaining.TotalSeconds > 0)
                {
                    // Aggiorna solo l'etichetta del countdown
                    lblNextCheck.Text = $"Next Check: {nextCheckTime.Value.ToString("HH:mm:ss")} " +
                                       $"({(int)remaining.TotalHours}h {remaining.Minutes}m {remaining.Seconds}s)";
                }
                else
                {
                    // COUNTDOWN TERMINATO! ESEGUI IL CHECK IMMEDIATAMENTE!
                    LogToFile("scheduler_log.txt", $"{now.ToString("yyyy-MM-dd HH:mm:ss")} - [timerClock] COUNTDOWN TERMINATO - Avvio controllo programmato");

                    // Esegui il controllo programmato
                    ExecuteScheduledCheck();

                    // Il metodo ExecuteScheduledCheck ha già calcolato il nuovo nextCheckTime
                    // e aggiornato l'etichetta, quindi non c'è bisogno di fare altro qui
                }
            }
        }

        private void MainForm_Resize(object sender, EventArgs e)
        {
            // When minimized, hide form and show in system tray
            if (WindowState == FormWindowState.Minimized)
            {
                Hide();
                notifyIcon.Visible = true;
                // Mostra un messaggio tooltip quando viene minimizzato
                notifyIcon.ShowBalloonTip(2000, "Disk Space Monitor",
                    "Application minimized to system tray. Double-click to restore.",
                    ToolTipIcon.Info);
            }
        }

        private void notifyIcon_DoubleClick(object sender, EventArgs e)
        {
            Show();
            WindowState = FormWindowState.Normal;
            notifyIcon.Visible = false;
            // Porta la finestra in primo piano
            this.BringToFront();
            this.Activate();
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Show();
            WindowState = FormWindowState.Normal;
            notifyIcon.Visible = false;
            // Porta la finestra in primo piano
            this.BringToFront();
            this.Activate();
        }

        private void checkNowToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CheckDriveSpace();
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
            // Nascondi l'icona prima di uscire
            notifyIcon.Visible = false;
            Application.Exit();
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                // If user clicks X, minimize instead of closing
                e.Cancel = true;
                WindowState = FormWindowState.Minimized;
                Hide();
                notifyIcon.Visible = true;
                notifyIcon.ShowBalloonTip(3000, "Disk Space Monitor",
                    "Application minimized to system tray. Double-click to restore.",
                    ToolTipIcon.Info);
            }
            else
            {
                // Se l'applicazione si sta chiudendo davvero, nascondi l'icona
                notifyIcon.Visible = false;
            }
        }

        // Nuovo metodo per il bottone Report
        private void btnReport_Click(object sender, EventArgs e)
        {
            try
            {
                // Ottieni il percorso del file report
                string appPath = Path.GetDirectoryName(Application.ExecutablePath);
                string reportPath = Path.Combine(appPath, "report.txt");

                // Verifica se il file esiste
                if (File.Exists(reportPath))
                {
                    // Apri il file con l'applicazione predefinita per i file .txt
                    Process.Start(reportPath);
                }
                else
                {
                    MessageBox.Show("Il file report.txt non esiste ancora. Sarà creato al prossimo controllo del disco.",
                        "File non trovato", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Errore nell'apertura del file: {ex.Message}", "Errore", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}