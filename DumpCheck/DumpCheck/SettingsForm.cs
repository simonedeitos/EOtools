using System;
using System.Drawing;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;
using Microsoft.Win32;

namespace DumpCheck
{
    public partial class SettingsForm : Form
    {
        private const string REG_KEY_PATH = @"SOFTWARE\DumpCheck";

        private CheckBox chkRealtimeEnabled;
        private TextBox txtRealtimePath;
        private readonly CheckBox[] chkRealtimeSlots = new CheckBox[4];
        private readonly NumericUpDown[] numRealtimeHour = new NumericUpDown[4];
        private readonly NumericUpDown[] numRealtimeMinute = new NumericUpDown[4];

        private CheckBox chkAmpliatoEnabled;
        private TextBox txtAmpliatoPath;
        private NumericUpDown numAmpliatoHour;
        private NumericUpDown numAmpliatoMinute;

        private TextBox txtSmtpServer;
        private NumericUpDown numSmtpPort;
        private CheckBox chkUseSsl;
        private TextBox txtSmtpUsername;
        private TextBox txtSmtpPassword;
        private TextBox txtEmailFrom;
        private TextBox txtEmailTo;
        private TextBox txtEmailSubject;

        private TabControl tabControl;

        public SettingsForm()
        {
            InitializeComponent();
            BuildDynamicUi();
        }

        private void BuildDynamicUi()
        {
            tabControl = new TabControl { Location = new Point(12, 12), Size = new Size(616, 466) };
            TabPage tabRealtime = new TabPage("Dump Realtime");
            TabPage tabAmpliato = new TabPage("Dump Ampliato");
            TabPage tabEmail = new TabPage("Email / SMTP");
            tabControl.TabPages.Add(tabRealtime);
            tabControl.TabPages.Add(tabAmpliato);
            tabControl.TabPages.Add(tabEmail);
            Controls.Add(tabControl);

            BuildRealtimeTab(tabRealtime);
            BuildAmpliatoTab(tabAmpliato);
            BuildEmailTab(tabEmail);

            Button btnSave = new Button { Text = "Save", Location = new Point(472, 489), Size = new Size(75, 23) };
            Button btnCancel = new Button { Text = "Cancel", Location = new Point(553, 489), Size = new Size(75, 23) };
            btnSave.Click += btnSave_Click;
            btnCancel.Click += btnCancel_Click;
            Controls.Add(btnSave);
            Controls.Add(btnCancel);
        }

        private void BuildRealtimeTab(TabPage tab)
        {
            chkRealtimeEnabled = new CheckBox { Text = "Abilitato", Location = new Point(16, 16), AutoSize = true };
            tab.Controls.Add(chkRealtimeEnabled);

            tab.Controls.Add(new Label { Text = "Percorso cartella:", Location = new Point(16, 46), AutoSize = true });
            txtRealtimePath = new TextBox { Location = new Point(16, 63), Size = new Size(490, 20) };
            Button btnBrowseRealtime = new Button { Text = "Browse...", Location = new Point(512, 61), Size = new Size(75, 23) };
            btnBrowseRealtime.Click += delegate { BrowseFolder(txtRealtimePath); };
            tab.Controls.Add(txtRealtimePath);
            tab.Controls.Add(btnBrowseRealtime);

            tab.Controls.Add(new Label { Text = "Slot orari (max 4):", Location = new Point(16, 102), AutoSize = true });
            for (int i = 0; i < 4; i++)
            {
                int y = 128 + i * 32;
                chkRealtimeSlots[i] = new CheckBox { Text = "Slot " + (i + 1) + " abilitato", Location = new Point(16, y), AutoSize = true };
                numRealtimeHour[i] = new NumericUpDown { Location = new Point(190, y - 2), Minimum = 0, Maximum = 23, Size = new Size(55, 20) };
                numRealtimeMinute[i] = new NumericUpDown { Location = new Point(269, y - 2), Minimum = 0, Maximum = 59, Size = new Size(55, 20) };

                tab.Controls.Add(chkRealtimeSlots[i]);
                tab.Controls.Add(new Label { Text = "Ora", Location = new Point(251, y), AutoSize = true });
                tab.Controls.Add(numRealtimeHour[i]);
                tab.Controls.Add(new Label { Text = "Min", Location = new Point(330, y), AutoSize = true });
                tab.Controls.Add(numRealtimeMinute[i]);
            }
        }

        private void BuildAmpliatoTab(TabPage tab)
        {
            chkAmpliatoEnabled = new CheckBox { Text = "Abilitato", Location = new Point(16, 16), AutoSize = true };
            tab.Controls.Add(chkAmpliatoEnabled);

            tab.Controls.Add(new Label { Text = "Percorso cartella:", Location = new Point(16, 46), AutoSize = true });
            txtAmpliatoPath = new TextBox { Location = new Point(16, 63), Size = new Size(490, 20) };
            Button btnBrowseAmpliato = new Button { Text = "Browse...", Location = new Point(512, 61), Size = new Size(75, 23) };
            btnBrowseAmpliato.Click += delegate { BrowseFolder(txtAmpliatoPath); };
            tab.Controls.Add(txtAmpliatoPath);
            tab.Controls.Add(btnBrowseAmpliato);

            tab.Controls.Add(new Label { Text = "Check giornaliero:", Location = new Point(16, 102), AutoSize = true });
            numAmpliatoHour = new NumericUpDown { Location = new Point(132, 100), Minimum = 0, Maximum = 23, Size = new Size(55, 20) };
            numAmpliatoMinute = new NumericUpDown { Location = new Point(210, 100), Minimum = 0, Maximum = 59, Size = new Size(55, 20) };
            tab.Controls.Add(numAmpliatoHour);
            tab.Controls.Add(new Label { Text = "Ora", Location = new Point(193, 102), AutoSize = true });
            tab.Controls.Add(numAmpliatoMinute);
            tab.Controls.Add(new Label { Text = "Min", Location = new Point(271, 102), AutoSize = true });
        }

        private void BuildEmailTab(TabPage tab)
        {
            int y = 20;
            tab.Controls.Add(new Label { Text = "SMTP Server:", Location = new Point(16, y + 3), AutoSize = true });
            txtSmtpServer = new TextBox { Location = new Point(120, y), Size = new Size(467, 20) };
            tab.Controls.Add(txtSmtpServer);

            y += 30;
            tab.Controls.Add(new Label { Text = "SMTP Port:", Location = new Point(16, y + 3), AutoSize = true });
            numSmtpPort = new NumericUpDown { Location = new Point(120, y), Minimum = 1, Maximum = 65535, Value = 25, Size = new Size(70, 20) };
            chkUseSsl = new CheckBox { Text = "Use SSL", Location = new Point(208, y + 2), AutoSize = true };
            tab.Controls.Add(numSmtpPort);
            tab.Controls.Add(chkUseSsl);

            y += 30;
            tab.Controls.Add(new Label { Text = "SMTP Username:", Location = new Point(16, y + 3), AutoSize = true });
            txtSmtpUsername = new TextBox { Location = new Point(120, y), Size = new Size(467, 20) };
            tab.Controls.Add(txtSmtpUsername);

            y += 30;
            tab.Controls.Add(new Label { Text = "SMTP Password:", Location = new Point(16, y + 3), AutoSize = true });
            txtSmtpPassword = new TextBox { Location = new Point(120, y), Size = new Size(467, 20), PasswordChar = '*' };
            tab.Controls.Add(txtSmtpPassword);

            y += 30;
            tab.Controls.Add(new Label { Text = "Email From:", Location = new Point(16, y + 3), AutoSize = true });
            txtEmailFrom = new TextBox { Location = new Point(120, y), Size = new Size(467, 20) };
            tab.Controls.Add(txtEmailFrom);

            y += 30;
            tab.Controls.Add(new Label { Text = "Email To:", Location = new Point(16, y + 3), AutoSize = true });
            txtEmailTo = new TextBox { Location = new Point(120, y), Size = new Size(467, 20) };
            tab.Controls.Add(txtEmailTo);

            y += 30;
            tab.Controls.Add(new Label { Text = "Email Subject:", Location = new Point(16, y + 3), AutoSize = true });
            txtEmailSubject = new TextBox { Location = new Point(120, y), Size = new Size(467, 20), Text = "DumpCheck Alert" };
            tab.Controls.Add(txtEmailSubject);

            y += 35;
            Button btnTestEmail = new Button { Text = "Test Email", Location = new Point(489, y), Size = new Size(98, 23) };
            btnTestEmail.Click += btnTestEmail_Click;
            tab.Controls.Add(btnTestEmail);
        }

        private void SettingsForm_Load(object sender, EventArgs e)
        {
            LoadSettings();
        }

        private void LoadSettings()
        {
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(REG_KEY_PATH))
            {
                if (key == null)
                {
                    chkRealtimeSlots[0].Checked = true;
                    return;
                }

                chkRealtimeEnabled.Checked = Convert.ToBoolean(key.GetValue("RealtimeEnabled", false));
                txtRealtimePath.Text = key.GetValue("RealtimePath", "").ToString();
                for (int i = 0; i < 4; i++)
                {
                    int slot = i + 1;
                    chkRealtimeSlots[i].Checked = Convert.ToBoolean(key.GetValue("RealtimeSlot" + slot + "Enabled", i == 0));
                    numRealtimeHour[i].Value = Convert.ToDecimal(key.GetValue("RealtimeSlot" + slot + "Hour", 9));
                    numRealtimeMinute[i].Value = Convert.ToDecimal(key.GetValue("RealtimeSlot" + slot + "Minute", 0));
                }

                chkAmpliatoEnabled.Checked = Convert.ToBoolean(key.GetValue("AmpliatoEnabled", false));
                txtAmpliatoPath.Text = key.GetValue("AmpliatoPath", "").ToString();
                numAmpliatoHour.Value = Convert.ToDecimal(key.GetValue("AmpliatoCheckHour", 9));
                numAmpliatoMinute.Value = Convert.ToDecimal(key.GetValue("AmpliatoCheckMinute", 0));

                txtSmtpServer.Text = key.GetValue("SmtpServer", "").ToString();
                numSmtpPort.Value = Convert.ToDecimal(key.GetValue("SmtpPort", 25));
                chkUseSsl.Checked = Convert.ToBoolean(key.GetValue("SmtpUseSsl", false));
                txtSmtpUsername.Text = key.GetValue("SmtpUsername", "").ToString();
                txtSmtpPassword.Text = DecryptStringFromRegistry(key.GetValue("SmtpPassword", "").ToString());
                txtEmailFrom.Text = key.GetValue("EmailFrom", "").ToString();
                txtEmailTo.Text = key.GetValue("EmailTo", "").ToString();
                txtEmailSubject.Text = key.GetValue("EmailSubject", "DumpCheck Alert").ToString();
            }
        }

        private void SaveSettings()
        {
            using (RegistryKey key = Registry.CurrentUser.CreateSubKey(REG_KEY_PATH))
            {
                key.SetValue("RealtimeEnabled", chkRealtimeEnabled.Checked);
                key.SetValue("RealtimePath", txtRealtimePath.Text.Trim());
                for (int i = 0; i < 4; i++)
                {
                    int slot = i + 1;
                    key.SetValue("RealtimeSlot" + slot + "Enabled", chkRealtimeSlots[i].Checked);
                    key.SetValue("RealtimeSlot" + slot + "Hour", (int)numRealtimeHour[i].Value);
                    key.SetValue("RealtimeSlot" + slot + "Minute", (int)numRealtimeMinute[i].Value);
                }

                key.SetValue("AmpliatoEnabled", chkAmpliatoEnabled.Checked);
                key.SetValue("AmpliatoPath", txtAmpliatoPath.Text.Trim());
                key.SetValue("AmpliatoCheckHour", (int)numAmpliatoHour.Value);
                key.SetValue("AmpliatoCheckMinute", (int)numAmpliatoMinute.Value);

                key.SetValue("SmtpServer", txtSmtpServer.Text.Trim());
                key.SetValue("SmtpPort", (int)numSmtpPort.Value);
                key.SetValue("SmtpUseSsl", chkUseSsl.Checked);
                key.SetValue("SmtpUsername", txtSmtpUsername.Text.Trim());
                key.SetValue("SmtpPassword", EncryptStringForRegistry(txtSmtpPassword.Text));
                key.SetValue("EmailFrom", txtEmailFrom.Text.Trim());
                key.SetValue("EmailTo", txtEmailTo.Text.Trim());
                key.SetValue("EmailSubject", txtEmailSubject.Text.Trim());
            }
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            if (chkRealtimeEnabled.Checked && (string.IsNullOrWhiteSpace(txtRealtimePath.Text) || !Directory.Exists(txtRealtimePath.Text)))
            {
                MessageBox.Show("Percorso Dump Realtime non valido.", "Validazione", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                tabControl.SelectedIndex = 0;
                return;
            }

            if (chkAmpliatoEnabled.Checked && (string.IsNullOrWhiteSpace(txtAmpliatoPath.Text) || !Directory.Exists(txtAmpliatoPath.Text)))
            {
                MessageBox.Show("Percorso Dump Ampliato non valido.", "Validazione", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                tabControl.SelectedIndex = 1;
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

        private void btnTestEmail_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtSmtpServer.Text) || string.IsNullOrWhiteSpace(txtEmailFrom.Text) || string.IsNullOrWhiteSpace(txtEmailTo.Text))
            {
                MessageBox.Show("Compilare Server SMTP, Email From, Email To.", "Validazione", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                using (SmtpClient client = new SmtpClient(txtSmtpServer.Text, (int)numSmtpPort.Value))
                {
                    client.EnableSsl = chkUseSsl.Checked;
                    if (!string.IsNullOrWhiteSpace(txtSmtpUsername.Text))
                    {
                        client.Credentials = new NetworkCredential(txtSmtpUsername.Text, txtSmtpPassword.Text);
                    }

                    using (MailMessage mail = new MailMessage())
                    {
                        mail.From = new MailAddress(txtEmailFrom.Text);
                        foreach (string address in txtEmailTo.Text.Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries))
                        {
                            mail.To.Add(address.Trim());
                        }

                        mail.Subject = "Test Email from DumpCheck";
                        mail.Body = "Test SMTP DumpCheck completato con successo.";
                        client.Send(mail);
                    }
                }

                MessageBox.Show("Test email inviata con successo.", "Successo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Invio email fallito: " + ex.Message, "Errore", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BrowseFolder(TextBox target)
        {
            using (FolderBrowserDialog dialog = new FolderBrowserDialog())
            {
                dialog.ShowNewFolderButton = false;
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    target.Text = dialog.SelectedPath;
                }

                private static string EncryptStringForRegistry(string input)
                {
                    if (string.IsNullOrEmpty(input))
                    {
                        return string.Empty;
                    }

                    byte[] protectedBytes = ProtectedData.Protect(Encoding.UTF8.GetBytes(input), null, DataProtectionScope.CurrentUser);
                    return Convert.ToBase64String(protectedBytes);
                }

                private static string DecryptStringFromRegistry(string input)
                {
                    if (string.IsNullOrEmpty(input))
                    {
                        return string.Empty;
                    }

                    try
                    {
                        byte[] raw = Convert.FromBase64String(input);
                        byte[] unprotected = ProtectedData.Unprotect(raw, null, DataProtectionScope.CurrentUser);
                        return Encoding.UTF8.GetString(unprotected);
                    }
                    catch
                    {
                        return input;
                    }
                }
            }
        }
    }
}
