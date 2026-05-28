using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using Microsoft.Win32;

namespace DumpCheck
{
    public partial class MainForm : Form
    {
        private const string REG_KEY_PATH = @"SOFTWARE\DumpCheck";
        private const long ALERT_THRESHOLD_BYTES = 10240;
        private static readonly string[] WatchedFiles = { "datasize.txt", "dump.bin", "song.bin" };

        private bool realtimeEnabled;
        private string realtimePath = string.Empty;
        private readonly bool[] realtimeSlotEnabled = new bool[4];
        private readonly int[] realtimeSlotHour = new int[4];
        private readonly int[] realtimeSlotMinute = new int[4];

        private bool ampliatoEnabled;
        private string ampliatoPath = string.Empty;
        private int ampliatoCheckHour = 9;
        private int ampliatoCheckMinute;

        private DateTime? nextRealtimeCheckTime;
        private DateTime? nextAmpliatoCheckTime;
        private bool realtimeCheckInProgress;
        private bool ampliatoCheckInProgress;

        private string smtpServer = string.Empty;
        private int smtpPort = 25;
        private bool smtpUseSsl;
        private string smtpUsername = string.Empty;
        private string smtpPassword = string.Empty;
        private string emailFrom = string.Empty;
        private string emailTo = string.Empty;
        private string emailSubject = "DumpCheck Alert";

        private Label lblRealtimePath;
        private Label lblRealtimeEnabled;
        private Label lblRealtimeLastCheck;
        private Label lblRealtimeNextCheck;
        private Label lblAmpliatoPath;
        private Label lblAmpliatoEnabled;
        private Label lblAmpliatoLastCheck;
        private Label lblAmpliatoNextCheck;

        private readonly Dictionary<string, Label> realtimeCurrentLabels = new Dictionary<string, Label>();
        private readonly Dictionary<string, Label> realtimeDeltaLabels = new Dictionary<string, Label>();
        private readonly Dictionary<string, Label> realtimeStatusLabels = new Dictionary<string, Label>();
        private readonly Dictionary<string, Label> ampliatoCurrentLabels = new Dictionary<string, Label>();
        private readonly Dictionary<string, Label> ampliatoDeltaLabels = new Dictionary<string, Label>();
        private readonly Dictionary<string, Label> ampliatoStatusLabels = new Dictionary<string, Label>();

        public MainForm()
        {
            InitializeComponent();
            BuildDynamicUi();
            SetupNotifyIcon();
        }

        private void BuildDynamicUi()
        {
            GroupBox groupRealtime = BuildMonitorGroup("Dump Realtime", new Point(12, 12), true);
            GroupBox groupAmpliato = BuildMonitorGroup("Dump Ampliato", new Point(12, 228), false);
            Controls.Add(groupRealtime);
            Controls.Add(groupAmpliato);

            Button btnSettings = new Button();
            btnSettings.Text = "Settings";
            btnSettings.Size = new Size(85, 30);
            btnSettings.Location = new Point(602, 444);
            btnSettings.Click += btnSettings_Click;
            Controls.Add(btnSettings);

            Button btnExit = new Button();
            btnExit.Text = "Exit";
            btnExit.Size = new Size(79, 30);
            btnExit.Location = new Point(693, 444);
            btnExit.Click += btnExit_Click;
            Controls.Add(btnExit);
        }

        private GroupBox BuildMonitorGroup(string title, Point location, bool realtime)
        {
            GroupBox group = new GroupBox();
            group.Text = title;
            group.Location = location;
            group.Size = new Size(760, 210);

            Label lblPath = new Label { Location = new Point(33, 24), AutoSize = true, Text = "Percorso: -" };
            Label lblEnabled = new Label { Location = new Point(33, 42), AutoSize = true, Text = "Stato: -" };
            Label lblCurrent = new Label { Location = new Point(247, 65), AutoSize = true, Text = "Dimensione att.", Font = new Font(Font, FontStyle.Bold) };
            Label lblDelta = new Label { Location = new Point(423, 65), AutoSize = true, Text = "Delta", Font = new Font(Font, FontStyle.Bold) };
            Label lblStatus = new Label { Location = new Point(607, 65), AutoSize = true, Text = "Stato", Font = new Font(Font, FontStyle.Bold) };

            group.Controls.Add(lblPath);
            group.Controls.Add(lblEnabled);
            group.Controls.Add(lblCurrent);
            group.Controls.Add(lblDelta);
            group.Controls.Add(lblStatus);

            Label lblLastCheck = new Label { Location = new Point(33, 144), AutoSize = true, Text = "Ultimo check: Never" };
            Label lblNextCheck = new Label { Location = new Point(33, 162), AutoSize = true, Text = "Prossimo check: Non impostato" };
            group.Controls.Add(lblLastCheck);
            group.Controls.Add(lblNextCheck);

            if (realtime)
            {
                lblRealtimePath = lblPath;
                lblRealtimeEnabled = lblEnabled;
                lblRealtimeLastCheck = lblLastCheck;
                lblRealtimeNextCheck = lblNextCheck;
            }
            else
            {
                lblAmpliatoPath = lblPath;
                lblAmpliatoEnabled = lblEnabled;
                lblAmpliatoLastCheck = lblLastCheck;
                lblAmpliatoNextCheck = lblNextCheck;
            }

            for (int i = 0; i < WatchedFiles.Length; i++)
            {
                string file = WatchedFiles[i];
                int y = 86 + i * 23;
                group.Controls.Add(new Label { Location = new Point(33, y), AutoSize = true, Text = file });

                Label currentValue = new Label { Location = new Point(247, y), AutoSize = true, Text = "-" };
                Label deltaValue = new Label { Location = new Point(423, y), AutoSize = true, Text = "-" };
                Label statusValue = new Label { Location = new Point(607, y), AutoSize = true, Text = "-" };

                group.Controls.Add(currentValue);
                group.Controls.Add(deltaValue);
                group.Controls.Add(statusValue);

                if (realtime)
                {
                    realtimeCurrentLabels[file] = currentValue;
                    realtimeDeltaLabels[file] = deltaValue;
                    realtimeStatusLabels[file] = statusValue;
                }
                else
                {
                    ampliatoCurrentLabels[file] = currentValue;
                    ampliatoDeltaLabels[file] = deltaValue;
                    ampliatoStatusLabels[file] = statusValue;
                }
            }

            Button btnCheckNow = new Button { Text = "Check Now", Size = new Size(93, 23), Location = new Point(557, 177) };
            Button btnHistory = new Button { Text = "Storico", Size = new Size(85, 23), Location = new Point(656, 177) };
            btnCheckNow.Click += realtime ? (EventHandler)btnRealtimeCheckNow_Click : btnAmpliatoCheckNow_Click;
            btnHistory.Click += realtime ? (EventHandler)btnRealtimeHistory_Click : btnAmpliatoHistory_Click;

            group.Controls.Add(btnCheckNow);
            group.Controls.Add(btnHistory);
            return group;
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            timerRealtimeClock.Interval = 1000;
            timerAmpliatoClock.Interval = 1000;
            timerRealtimeChecker.Interval = 10000;
            timerAmpliatoChecker.Interval = 10000;

            LoadSettings();
            ApplySettingsToUi();
            RestartSchedulers();
        }

        private void SetupNotifyIcon()
        {
            notifyIcon.Icon = Icon ?? SystemIcons.Application;
            notifyIcon.ContextMenuStrip = contextMenuStrip;
            notifyIcon.Text = "DumpCheck";
            notifyIcon.Visible = false;
        }

        private void LoadSettings()
        {
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(REG_KEY_PATH))
            {
                if (key == null)
                {
                    for (int i = 0; i < 4; i++)
                    {
                        realtimeSlotEnabled[i] = i == 0;
                        realtimeSlotHour[i] = 9;
                        realtimeSlotMinute[i] = 0;
                    }
                    return;
                }

                realtimeEnabled = Convert.ToBoolean(key.GetValue("RealtimeEnabled", false));
                realtimePath = key.GetValue("RealtimePath", "").ToString();

                for (int i = 0; i < 4; i++)
                {
                    int slot = i + 1;
                    realtimeSlotEnabled[i] = Convert.ToBoolean(key.GetValue("RealtimeSlot" + slot + "Enabled", i == 0));
                    realtimeSlotHour[i] = Convert.ToInt32(key.GetValue("RealtimeSlot" + slot + "Hour", 9));
                    realtimeSlotMinute[i] = Convert.ToInt32(key.GetValue("RealtimeSlot" + slot + "Minute", 0));
                }

                ampliatoEnabled = Convert.ToBoolean(key.GetValue("AmpliatoEnabled", false));
                ampliatoPath = key.GetValue("AmpliatoPath", "").ToString();
                ampliatoCheckHour = Convert.ToInt32(key.GetValue("AmpliatoCheckHour", 9));
                ampliatoCheckMinute = Convert.ToInt32(key.GetValue("AmpliatoCheckMinute", 0));

                smtpServer = key.GetValue("SmtpServer", "").ToString();
                smtpPort = Convert.ToInt32(key.GetValue("SmtpPort", 25));
                smtpUseSsl = Convert.ToBoolean(key.GetValue("SmtpUseSsl", false));
                smtpUsername = key.GetValue("SmtpUsername", "").ToString();
                smtpPassword = key.GetValue("SmtpPassword", "").ToString();
                emailFrom = key.GetValue("EmailFrom", "").ToString();
                emailTo = key.GetValue("EmailTo", "").ToString();
                emailSubject = key.GetValue("EmailSubject", "DumpCheck Alert").ToString();
            }
        }

        private void ApplySettingsToUi()
        {
            lblRealtimePath.Text = "Percorso: " + (string.IsNullOrWhiteSpace(realtimePath) ? "Non configurato" : realtimePath);
            lblRealtimeEnabled.Text = "Stato: " + (realtimeEnabled ? "Abilitato" : "Disabilitato");
            lblAmpliatoPath.Text = "Percorso: " + (string.IsNullOrWhiteSpace(ampliatoPath) ? "Non configurato" : ampliatoPath);
            lblAmpliatoEnabled.Text = "Stato: " + (ampliatoEnabled ? "Abilitato" : "Disabilitato");

            if (!realtimeEnabled)
            {
                SetMonitorUiNotAvailable(true, "Disabilitato");
            }
            if (!ampliatoEnabled)
            {
                SetMonitorUiNotAvailable(false, "Disabilitato");
            }
        }

        private void RestartSchedulers()
        {
            nextRealtimeCheckTime = CalculateNextRealtimeCheck(DateTime.Now);
            nextAmpliatoCheckTime = CalculateNextAmpliatoCheck(DateTime.Now);
            timerRealtimeClock.Enabled = realtimeEnabled;
            timerRealtimeChecker.Enabled = realtimeEnabled;
            timerAmpliatoClock.Enabled = ampliatoEnabled;
            timerAmpliatoChecker.Enabled = ampliatoEnabled;
            UpdateCountdownLabel(true);
            UpdateCountdownLabel(false);
        }

        private DateTime? CalculateNextRealtimeCheck(DateTime now)
        {
            if (!realtimeEnabled)
            {
                return null;
            }

            DateTime? candidate = null;
            for (int i = 0; i < 4; i++)
            {
                if (!realtimeSlotEnabled[i])
                {
                    continue;
                }
                DateTime slotTime = new DateTime(now.Year, now.Month, now.Day, realtimeSlotHour[i], realtimeSlotMinute[i], 0);
                if (slotTime <= now)
                {
                    slotTime = slotTime.AddDays(1);
                }
                if (!candidate.HasValue || slotTime < candidate.Value)
                {
                    candidate = slotTime;
                }
            }

            return candidate;
        }

        private DateTime? CalculateNextAmpliatoCheck(DateTime now)
        {
            if (!ampliatoEnabled)
            {
                return null;
            }
            DateTime checkTime = new DateTime(now.Year, now.Month, now.Day, ampliatoCheckHour, ampliatoCheckMinute, 0);
            if (checkTime <= now)
            {
                checkTime = checkTime.AddDays(1);
            }
            return checkTime;
        }

        private void ExecuteScheduledCheck(bool realtime)
        {
            PerformCheck(realtime, false);
            if (realtime)
            {
                nextRealtimeCheckTime = CalculateNextRealtimeCheck(DateTime.Now);
            }
            else
            {
                nextAmpliatoCheckTime = CalculateNextAmpliatoCheck(DateTime.Now);
            }
            UpdateCountdownLabel(realtime);
        }

        private void PerformCheck(bool realtime, bool manual)
        {
            bool enabled = realtime ? realtimeEnabled : ampliatoEnabled;
            if (!enabled && !manual)
            {
                return;
            }

            if (realtime)
            {
                if (realtimeCheckInProgress) return;
                realtimeCheckInProgress = true;
            }
            else
            {
                if (ampliatoCheckInProgress) return;
                ampliatoCheckInProgress = true;
            }

            try
            {
                string monitorType = realtime ? "Realtime" : "Ampliato";
                string monitorPath = realtime ? realtimePath : ampliatoPath;
                string xmlName = realtime ? "realtime.xml" : "ampliato.xml";
                string xmlPath = Path.Combine(Path.GetDirectoryName(Application.ExecutablePath), xmlName);

                CheckSnapshot previous = LoadLastSnapshot(xmlPath);
                CheckSnapshot current = BuildCurrentSnapshot(monitorPath);
                CheckResult result = EvaluateResult(monitorType, monitorPath, previous, current);
                AppendSnapshot(xmlPath, result);
                UpdateMonitorUi(realtime, result);

                if (result.ShouldAlert)
                {
                    SendAlert(result);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Errore durante il check: " + ex.Message, "Errore", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                if (realtime) realtimeCheckInProgress = false; else ampliatoCheckInProgress = false;
            }
        }

        private CheckSnapshot BuildCurrentSnapshot(string monitorPath)
        {
            CheckSnapshot snapshot = new CheckSnapshot();
            foreach (string file in WatchedFiles)
            {
                long? size = null;
                bool exists = false;

                if (!string.IsNullOrWhiteSpace(monitorPath) && Directory.Exists(monitorPath))
                {
                    string fullPath = Path.Combine(monitorPath, file);
                    if (File.Exists(fullPath))
                    {
                        exists = true;
                        size = new FileInfo(fullPath).Length;
                    }
                }

                snapshot.Files[file] = new FileSnapshot { Exists = exists, SizeBytes = size };
            }

            return snapshot;
        }

        private CheckResult EvaluateResult(string monitorType, string monitorPath, CheckSnapshot previous, CheckSnapshot current)
        {
            CheckResult result = new CheckResult
            {
                Timestamp = DateTime.Now,
                MonitorType = monitorType,
                MonitorPath = monitorPath,
                IsFirstCheck = previous == null
            };

            foreach (string file in WatchedFiles)
            {
                FileSnapshot currentFile = current.Files[file];
                FileSnapshot previousFile = previous != null && previous.Files.ContainsKey(file) ? previous.Files[file] : null;

                FileEvaluation eval = new FileEvaluation
                {
                    Name = file,
                    CurrentExists = currentFile != null && currentFile.Exists,
                    CurrentSizeBytes = currentFile != null ? currentFile.SizeBytes : null,
                    PreviousSizeBytes = previousFile != null ? previousFile.SizeBytes : null
                };

                if (!eval.CurrentExists)
                {
                    eval.Status = "MISSING";
                    if (!result.IsFirstCheck) result.Problems.Add("File mancante: " + file);
                }
                else if (eval.PreviousSizeBytes.HasValue && eval.CurrentSizeBytes.HasValue)
                {
                    eval.DeltaBytes = eval.CurrentSizeBytes.Value - eval.PreviousSizeBytes.Value;
                    if (!result.IsFirstCheck && eval.DeltaBytes.Value <= -ALERT_THRESHOLD_BYTES)
                    {
                        eval.Status = "ALERT";
                        result.Problems.Add(file + " diminuito di " + Math.Abs(eval.DeltaBytes.Value) + " byte");
                    }
                    else
                    {
                        eval.Status = "OK";
                    }
                }
                else
                {
                    eval.Status = "OK";
                }

                result.Files.Add(eval);
            }

            result.ShouldAlert = !result.IsFirstCheck && result.Problems.Count > 0;
            return result;
        }

        private CheckSnapshot LoadLastSnapshot(string xmlPath)
        {
            if (!File.Exists(xmlPath))
            {
                return null;
            }

            XmlDocument doc = new XmlDocument();
            doc.Load(xmlPath);
            XmlNode lastNode = doc.SelectSingleNode("/History/Check[last()]");
            if (lastNode == null)
            {
                return null;
            }

            CheckSnapshot snapshot = new CheckSnapshot();
            foreach (XmlNode fileNode in lastNode.SelectNodes("Files/File"))
            {
                string name = fileNode.Attributes["name"] != null ? fileNode.Attributes["name"].Value : string.Empty;
                if (string.IsNullOrEmpty(name))
                {
                    continue;
                }

                bool exists = fileNode.Attributes["exists"] != null && Convert.ToBoolean(fileNode.Attributes["exists"].Value);
                long parsed;
                long? size = fileNode.Attributes["sizeBytes"] != null && long.TryParse(fileNode.Attributes["sizeBytes"].Value, out parsed)
                    ? (long?)parsed
                    : null;

                snapshot.Files[name] = new FileSnapshot { Exists = exists, SizeBytes = size };
            }
            return snapshot;
        }

        private void AppendSnapshot(string xmlPath, CheckResult result)
        {
            XmlDocument doc = new XmlDocument();
            XmlElement root;
            if (File.Exists(xmlPath))
            {
                doc.Load(xmlPath);
                root = doc.DocumentElement;
                if (root == null || root.Name != "History")
                {
                    doc.RemoveAll();
                    root = doc.CreateElement("History");
                    doc.AppendChild(root);
                }
            }
            else
            {
                doc.AppendChild(doc.CreateXmlDeclaration("1.0", "utf-8", null));
                root = doc.CreateElement("History");
                doc.AppendChild(root);
            }

            XmlElement checkNode = doc.CreateElement("Check");
            checkNode.SetAttribute("timestamp", result.Timestamp.ToString("yyyy-MM-dd HH:mm:ss"));
            checkNode.SetAttribute("type", result.MonitorType);
            checkNode.SetAttribute("path", result.MonitorPath ?? string.Empty);
            checkNode.SetAttribute("hostname", Environment.MachineName);

            XmlElement filesNode = doc.CreateElement("Files");
            foreach (FileEvaluation file in result.Files)
            {
                XmlElement fileNode = doc.CreateElement("File");
                fileNode.SetAttribute("name", file.Name);
                fileNode.SetAttribute("exists", file.CurrentExists.ToString());
                fileNode.SetAttribute("sizeBytes", file.CurrentSizeBytes.HasValue ? file.CurrentSizeBytes.Value.ToString() : string.Empty);
                fileNode.SetAttribute("previousSizeBytes", file.PreviousSizeBytes.HasValue ? file.PreviousSizeBytes.Value.ToString() : string.Empty);
                fileNode.SetAttribute("deltaBytes", file.DeltaBytes.HasValue ? file.DeltaBytes.Value.ToString() : string.Empty);
                fileNode.SetAttribute("status", file.Status);
                filesNode.AppendChild(fileNode);
            }
            checkNode.AppendChild(filesNode);

            XmlElement problemsNode = doc.CreateElement("Problems");
            foreach (string problem in result.Problems)
            {
                XmlElement p = doc.CreateElement("Problem");
                p.InnerText = problem;
                problemsNode.AppendChild(p);
            }
            checkNode.AppendChild(problemsNode);
            root.AppendChild(checkNode);
            doc.Save(xmlPath);
        }

        private void SendAlert(CheckResult result)
        {
            if (string.IsNullOrWhiteSpace(smtpServer) || string.IsNullOrWhiteSpace(emailFrom) || string.IsNullOrWhiteSpace(emailTo))
            {
                return;
            }

            StringBuilder body = new StringBuilder();
            body.AppendLine("DumpCheck ALERT");
            body.AppendLine("Timestamp: " + result.Timestamp.ToString("yyyy-MM-dd HH:mm:ss"));
            body.AppendLine("Tipo check: " + result.MonitorType);
            body.AppendLine("Percorso: " + (result.MonitorPath ?? ""));
            body.AppendLine("Hostname: " + Environment.MachineName);
            body.AppendLine();
            body.AppendLine("Dettaglio file:");
            foreach (FileEvaluation file in result.Files)
            {
                body.AppendLine(string.Format(
                    "- {0}: Current={1}, Previous={2}, Delta={3} ({4}), Status={5}",
                    file.Name,
                    FormatSizeOrMissing(file.CurrentExists, file.CurrentSizeBytes),
                    file.PreviousSizeBytes.HasValue ? file.PreviousSizeBytes.Value.ToString() : "N/A",
                    file.DeltaBytes.HasValue ? file.DeltaBytes.Value.ToString() : "N/A",
                    file.DeltaBytes.HasValue ? FormatBytes(file.DeltaBytes.Value) : "N/A",
                    file.Status));
            }
            body.AppendLine();
            body.AppendLine("Problemi rilevati:");
            foreach (string problem in result.Problems)
            {
                body.AppendLine("- " + problem);
            }

            using (SmtpClient client = new SmtpClient(smtpServer, smtpPort))
            {
                client.EnableSsl = smtpUseSsl;
                if (!string.IsNullOrWhiteSpace(smtpUsername))
                {
                    client.Credentials = new NetworkCredential(smtpUsername, smtpPassword);
                }

                using (MailMessage message = new MailMessage())
                {
                    message.From = new MailAddress(emailFrom);
                    foreach (string address in emailTo.Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries))
                    {
                        message.To.Add(address.Trim());
                    }
                    message.Subject = emailSubject;
                    message.Body = body.ToString();
                    client.Send(message);
                }
            }
        }

        private void UpdateMonitorUi(bool realtime, CheckResult result)
        {
            Label lastCheckLabel = realtime ? lblRealtimeLastCheck : lblAmpliatoLastCheck;
            lastCheckLabel.Text = "Ultimo check: " + result.Timestamp.ToString("yyyy-MM-dd HH:mm:ss");

            foreach (FileEvaluation file in result.Files)
            {
                Dictionary<string, Label> currentLabels = realtime ? realtimeCurrentLabels : ampliatoCurrentLabels;
                Dictionary<string, Label> deltaLabels = realtime ? realtimeDeltaLabels : ampliatoDeltaLabels;
                Dictionary<string, Label> statusLabels = realtime ? realtimeStatusLabels : ampliatoStatusLabels;

                currentLabels[file.Name].Text = file.CurrentExists && file.CurrentSizeBytes.HasValue ? FormatBytes(file.CurrentSizeBytes.Value) : "MISSING";
                deltaLabels[file.Name].Text = file.DeltaBytes.HasValue ? FormatDelta(file.DeltaBytes.Value) : "N/A";
                statusLabels[file.Name].Text = file.Status;
                statusLabels[file.Name].ForeColor = file.Status == "OK" ? Color.DarkGreen : Color.Red;
            }
        }

        private void SetMonitorUiNotAvailable(bool realtime, string status)
        {
            Dictionary<string, Label> currentLabels = realtime ? realtimeCurrentLabels : ampliatoCurrentLabels;
            Dictionary<string, Label> deltaLabels = realtime ? realtimeDeltaLabels : ampliatoDeltaLabels;
            Dictionary<string, Label> stateLabels = realtime ? realtimeStatusLabels : ampliatoStatusLabels;

            foreach (string file in WatchedFiles)
            {
                currentLabels[file].Text = "MISSING";
                deltaLabels[file].Text = "N/A";
                stateLabels[file].Text = status;
                stateLabels[file].ForeColor = Color.Red;
            }
        }

        private string FormatBytes(long bytes)
        {
            string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
            decimal value = Math.Abs(bytes);
            int index = 0;
            while (value >= 1024 && index < suffixes.Length - 1)
            {
                value /= 1024;
                index++;
            }
            return (bytes < 0 ? "-" : "") + value.ToString("N2") + " " + suffixes[index];
        }

        private string FormatDelta(long deltaBytes)
        {
            string sign = deltaBytes >= 0 ? "+" : "";
            return sign + deltaBytes + " bytes (" + sign + FormatBytes(deltaBytes) + ")";
        }

        private string FormatSizeOrMissing(bool exists, long? sizeBytes)
        {
            return exists && sizeBytes.HasValue ? sizeBytes.Value + " bytes (" + FormatBytes(sizeBytes.Value) + ")" : "MISSING";
        }

        private void UpdateCountdownLabel(bool realtime)
        {
            bool enabled = realtime ? realtimeEnabled : ampliatoEnabled;
            DateTime? next = realtime ? nextRealtimeCheckTime : nextAmpliatoCheckTime;
            Label target = realtime ? lblRealtimeNextCheck : lblAmpliatoNextCheck;
            if (!enabled)
            {
                target.Text = "Prossimo check: Disabilitato";
                return;
            }
            if (!next.HasValue)
            {
                target.Text = "Prossimo check: Non impostato";
                return;
            }
            TimeSpan remaining = next.Value - DateTime.Now;
            if (remaining.TotalSeconds < 0) remaining = TimeSpan.Zero;
            target.Text = string.Format("Prossimo check: {0} ({1}h {2}m {3}s)",
                next.Value.ToString("yyyy-MM-dd HH:mm"),
                (int)remaining.TotalHours,
                remaining.Minutes,
                remaining.Seconds);
        }

        private void ShowSettingsForm()
        {
            using (SettingsForm settingsForm = new SettingsForm())
            {
                if (settingsForm.ShowDialog() == DialogResult.OK)
                {
                    LoadSettings();
                    ApplySettingsToUi();
                    RestartSchedulers();
                }
            }
        }

        private void btnSettings_Click(object sender, EventArgs e) => ShowSettingsForm();
        private void btnExit_Click(object sender, EventArgs e) { notifyIcon.Visible = false; Application.Exit(); }
        private void btnRealtimeCheckNow_Click(object sender, EventArgs e) => PerformCheck(true, true);
        private void btnAmpliatoCheckNow_Click(object sender, EventArgs e) => PerformCheck(false, true);
        private void btnRealtimeHistory_Click(object sender, EventArgs e) => OpenHistoryFile("realtime.xml");
        private void btnAmpliatoHistory_Click(object sender, EventArgs e) => OpenHistoryFile("ampliato.xml");

        private void OpenHistoryFile(string fileName)
        {
            string filePath = Path.Combine(Path.GetDirectoryName(Application.ExecutablePath), fileName);
            if (!File.Exists(filePath))
            {
                MessageBox.Show("File storico non presente: " + fileName, "Storico", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            Process.Start(filePath);
        }

        private void timerRealtimeClock_Tick(object sender, EventArgs e)
        {
            UpdateCountdownLabel(true);
            if (nextRealtimeCheckTime.HasValue && DateTime.Now >= nextRealtimeCheckTime.Value) ExecuteScheduledCheck(true);
        }

        private void timerRealtimeChecker_Tick(object sender, EventArgs e)
        {
            if (nextRealtimeCheckTime.HasValue && DateTime.Now > nextRealtimeCheckTime.Value && (DateTime.Now - nextRealtimeCheckTime.Value).TotalMinutes >= 1) ExecuteScheduledCheck(true);
        }

        private void timerAmpliatoClock_Tick(object sender, EventArgs e)
        {
            UpdateCountdownLabel(false);
            if (nextAmpliatoCheckTime.HasValue && DateTime.Now >= nextAmpliatoCheckTime.Value) ExecuteScheduledCheck(false);
        }

        private void timerAmpliatoChecker_Tick(object sender, EventArgs e)
        {
            if (nextAmpliatoCheckTime.HasValue && DateTime.Now > nextAmpliatoCheckTime.Value && (DateTime.Now - nextAmpliatoCheckTime.Value).TotalMinutes >= 1) ExecuteScheduledCheck(false);
        }

        private void MainForm_Resize(object sender, EventArgs e)
        {
            if (WindowState == FormWindowState.Minimized)
            {
                Hide();
                notifyIcon.Visible = true;
                notifyIcon.ShowBalloonTip(2000, "DumpCheck", "Applicazione minimizzata nella tray.", ToolTipIcon.Info);
            }
        }

        private void notifyIcon_DoubleClick(object sender, EventArgs e)
        {
            Show();
            WindowState = FormWindowState.Normal;
            notifyIcon.Visible = false;
            BringToFront();
            Activate();
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                WindowState = FormWindowState.Minimized;
                Hide();
                notifyIcon.Visible = true;
            }
            else
            {
                notifyIcon.Visible = false;
            }
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e) => notifyIcon_DoubleClick(sender, e);
        private void checkRealtimeNowToolStripMenuItem_Click(object sender, EventArgs e) => PerformCheck(true, true);
        private void checkAmpliatoNowToolStripMenuItem_Click(object sender, EventArgs e) => PerformCheck(false, true);
        private void settingsToolStripMenuItem_Click(object sender, EventArgs e) { notifyIcon_DoubleClick(sender, e); ShowSettingsForm(); }
        private void exitToolStripMenuItem_Click(object sender, EventArgs e) => btnExit_Click(sender, e);

        private sealed class CheckSnapshot
        {
            public readonly Dictionary<string, FileSnapshot> Files = new Dictionary<string, FileSnapshot>();
        }

        private sealed class FileSnapshot
        {
            public bool Exists;
            public long? SizeBytes;
        }

        private sealed class CheckResult
        {
            public DateTime Timestamp;
            public string MonitorType;
            public string MonitorPath;
            public bool IsFirstCheck;
            public bool ShouldAlert;
            public readonly List<FileEvaluation> Files = new List<FileEvaluation>();
            public readonly List<string> Problems = new List<string>();
        }

        private sealed class FileEvaluation
        {
            public string Name;
            public bool CurrentExists;
            public long? CurrentSizeBytes;
            public long? PreviousSizeBytes;
            public long? DeltaBytes;
            public string Status;
        }
    }
}
