using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace PlaylistDownloader
{
    internal static class DialogHelper
    {
        public static Form CreateDialog(string title, int width, int height)
        {
            var f = new Form();
            f.Text = title;
            f.ClientSize = new Size(width, height);
            f.FormBorderStyle = FormBorderStyle.FixedDialog;
            f.MaximizeBox = false;
            f.MinimizeBox = false;
            f.ShowInTaskbar = false;
            f.StartPosition = FormStartPosition.CenterParent;
            return f;
        }

        public static Label AddLabel(Control parent, string text, int x, int y, int width, int height = 18)
        {
            var l = new Label { Text = text, Location = new Point(x, y), Size = new Size(width, height) };
            parent.Controls.Add(l);
            return l;
        }

        public static Button AddButton(Control parent, string text, int x, int y, int width, EventHandler click)
        {
            var b = new Button { Text = text, Location = new Point(x, y), Size = new Size(width, 26), UseVisualStyleBackColor = true };
            if (click != null) b.Click += click;
            parent.Controls.Add(b);
            return b;
        }
    }

    // "Importazioni" menu: layout preference and lookup tuning, persisted in the registry
    public class ImportSettingsForm : Form
    {
        private readonly CheckBox chkShowPlaylists;
        private readonly NumericUpDown numThreads;
        private readonly NumericUpDown numRate;

        public ImportSettingsForm()
        {
            Text = "Importazioni";
            ClientSize = new Size(420, 210);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;

            chkShowPlaylists = new CheckBox
            {
                Text = "Mostra la sezione Playlist in alto",
                Location = new Point(16, 16),
                Size = new Size(380, 22),
                Checked = AppSettings.ShowPlaylistSection
            };
            Controls.Add(chkShowPlaylists);
            DialogHelper.AddLabel(this, "Se disattivata restano solo i pulsanti di import e la tabella dei brani.", 34, 40, 370, 30);

            DialogHelper.AddLabel(this, "Thread ricerca ISRC su Deezer:", 16, 82, 220);
            numThreads = new NumericUpDown { Location = new Point(250, 79), Size = new Size(60, 22), Minimum = 1, Maximum = 64, Value = AppSettings.LookupThreads };
            Controls.Add(numThreads);

            DialogHelper.AddLabel(this, "Max richieste Deezer ogni 5 secondi:", 16, 112, 230);
            numRate = new NumericUpDown { Location = new Point(250, 109), Size = new Size(60, 22), Minimum = 5, Maximum = 500, Value = AppSettings.LookupRatePer5s };
            Controls.Add(numRate);
            DialogHelper.AddLabel(this, "Deezer limita le API a circa 50 richieste/5s per IP: oltre il limite il programma rallenta da solo.", 16, 138, 390, 30);

            var ok = DialogHelper.AddButton(this, "Salva", 230, 172, 80, null);
            ok.Click += (s, e) =>
            {
                AppSettings.ShowPlaylistSection = chkShowPlaylists.Checked;
                AppSettings.LookupThreads = (int)numThreads.Value;
                AppSettings.LookupRatePer5s = (int)numRate.Value;
                DialogResult = DialogResult.OK;
            };
            var cancel = DialogHelper.AddButton(this, "Annulla", 320, 172, 80, null);
            cancel.DialogResult = DialogResult.Cancel;
            AcceptButton = ok;
            CancelButton = cancel;
        }
    }

    // Paste one Deezer track link per line; ids are extracted without calling Deezer
    public class PasteLinksForm : Form
    {
        private static readonly Regex TrackRegex =
            new Regex(@"deezer\.com/(?:[a-z]{2}(?:-[a-z]{2})?/)?track/(\d+)", RegexOptions.IgnoreCase);

        private readonly TextBox txtLinks;
        private readonly CheckBox chkClear;

        public List<string> TrackIds { get; private set; }
        public int InvalidLines { get; private set; }
        public bool ClearExisting { get { return chkClear.Checked; } }

        public PasteLinksForm()
        {
            TrackIds = new List<string>();
            Text = "Incolla link brani Deezer";
            ClientSize = new Size(560, 400);
            MinimumSize = new Size(420, 300);
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;

            var lbl = new Label
            {
                Text = "Un link per riga (es. https://www.deezer.com/track/123456). I brani vengono importati senza interrogare Deezer.",
                Dock = DockStyle.Top,
                Height = 36,
                Padding = new Padding(8, 8, 8, 0)
            };

            txtLinks = new TextBox { Multiline = true, ScrollBars = ScrollBars.Both, WordWrap = false, Dock = DockStyle.Fill, AcceptsReturn = true, MaxLength = int.MaxValue };

            var bottom = new Panel { Dock = DockStyle.Bottom, Height = 44 };
            chkClear = new CheckBox { Text = "Svuota i risultati esistenti", Location = new Point(10, 12), Size = new Size(180, 22) };
            var paste = DialogHelper.AddButton(bottom, "Incolla dagli appunti", 200, 9, 140, (s, e) =>
            {
                if (Clipboard.ContainsText())
                    txtLinks.Text = Clipboard.GetText();
            });
            var ok = DialogHelper.AddButton(bottom, "Importa", 350, 9, 90, (s, e) => Import());
            var cancel = DialogHelper.AddButton(bottom, "Annulla", 450, 9, 90, null);
            cancel.DialogResult = DialogResult.Cancel;
            bottom.Controls.Add(chkClear);
            foreach (var b in new[] { paste, ok, cancel })
                b.Anchor = AnchorStyles.Top | AnchorStyles.Right;

            Controls.Add(txtLinks);
            Controls.Add(bottom);
            Controls.Add(lbl);
            CancelButton = cancel;
        }

        private void Import()
        {
            var ids = new List<string>();
            var seen = new HashSet<string>();
            int invalid = 0;

            foreach (string raw in txtLinks.Lines)
            {
                string line = raw.Trim();
                if (line.Length == 0) continue;

                Match m = TrackRegex.Match(line);
                if (!m.Success) { invalid++; continue; }

                string id = m.Groups[1].Value;
                if (seen.Add(id)) ids.Add(id);
            }

            if (ids.Count == 0)
            {
                MessageBox.Show("Nessun link di brano Deezer valido trovato.", "Incolla link", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            TrackIds = ids;
            InvalidLines = invalid;
            DialogResult = DialogResult.OK;
        }
    }

    // Saved ARLs (registry, plain text) plus deemix settings
    public class ArlManagerForm : Form
    {
        private readonly ListBox lstArls;
        private readonly TextBox txtNew;
        private readonly TextBox txtCommand;
        private readonly TextBox txtWorkDir;
        private readonly List<string> arls;

        public ArlManagerForm()
        {
            arls = AppSettings.GetArls();

            Text = "ARL e deemix";
            ClientSize = new Size(560, 400);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;

            DialogHelper.AddLabel(this, "ARL salvati (usati in ordine; se uno è scaduto si passa al successivo):", 12, 12, 530);
            lstArls = new ListBox { Location = new Point(12, 34), Size = new Size(536, 130), HorizontalScrollbar = true };
            Controls.Add(lstArls);

            DialogHelper.AddLabel(this, "Nuovo ARL:", 12, 174, 80);
            txtNew = new TextBox { Location = new Point(92, 171), Size = new Size(366, 22) };
            Controls.Add(txtNew);
            DialogHelper.AddButton(this, "Aggiungi", 466, 169, 82, (s, e) => AddArl());
            DialogHelper.AddButton(this, "Rimuovi selezionato", 12, 202, 150, (s, e) =>
            {
                if (lstArls.SelectedIndex >= 0)
                {
                    arls.RemoveAt(lstArls.SelectedIndex);
                    RefreshList();
                }
            });

            DialogHelper.AddLabel(this, "Comando deemix (deemix-cli, es. \"deemix\" o percorso completo):", 12, 248, 530);
            txtCommand = new TextBox { Location = new Point(12, 268), Size = new Size(536, 22), Text = AppSettings.DeemixCommand };
            Controls.Add(txtCommand);

            DialogHelper.AddLabel(this, "Cartella di lavoro deemix (contiene la sottocartella \"config\" con .arl):", 12, 300, 530);
            txtWorkDir = new TextBox { Location = new Point(12, 320), Size = new Size(446, 22), Text = AppSettings.DeemixWorkDir };
            Controls.Add(txtWorkDir);
            DialogHelper.AddButton(this, "Sfoglia...", 466, 318, 82, (s, e) =>
            {
                using (var dlg = new FolderBrowserDialog())
                {
                    dlg.SelectedPath = txtWorkDir.Text;
                    if (dlg.ShowDialog(this) == DialogResult.OK)
                        txtWorkDir.Text = dlg.SelectedPath;
                }
            });

            var ok = DialogHelper.AddButton(this, "Salva", 376, 362, 82, (s, e) => Save());
            var cancel = DialogHelper.AddButton(this, "Annulla", 466, 362, 82, null);
            cancel.DialogResult = DialogResult.Cancel;
            CancelButton = cancel;

            RefreshList();
        }

        private static string Mask(string arl)
        {
            return arl.Length > 24 ? arl.Substring(0, 12) + "..." + arl.Substring(arl.Length - 8) + $"  ({arl.Length} car.)" : arl;
        }

        private void RefreshList()
        {
            lstArls.Items.Clear();
            foreach (string a in arls)
                lstArls.Items.Add(Mask(a));
        }

        private void AddArl()
        {
            string arl = txtNew.Text.Trim();
            if (arl.Length == 0) return;
            if (!arls.Contains(arl)) arls.Add(arl);
            txtNew.Clear();
            RefreshList();
        }

        private void Save()
        {
            AddArl();
            AppSettings.SetArls(arls);
            AppSettings.DeemixCommand = txtCommand.Text;
            AppSettings.DeemixWorkDir = txtWorkDir.Text.Trim();
            DialogResult = DialogResult.OK;
        }
    }

    // Asked when no saved ARL works
    public class ArlPromptForm : Form
    {
        private readonly TextBox txtArl;
        public string Arl { get { return txtArl.Text.Trim(); } }

        public ArlPromptForm(string message)
        {
            Text = "ARL richiesto";
            ClientSize = new Size(500, 150);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            TopMost = true;

            DialogHelper.AddLabel(this, message, 12, 12, 476, 40);
            txtArl = new TextBox { Location = new Point(12, 58), Size = new Size(476, 22) };
            Controls.Add(txtArl);

            var ok = DialogHelper.AddButton(this, "Usa e salva", 306, 100, 90, null);
            ok.DialogResult = DialogResult.OK;
            var cancel = DialogHelper.AddButton(this, "Annulla", 398, 100, 90, null);
            cancel.DialogResult = DialogResult.Cancel;
            AcceptButton = ok;
            CancelButton = cancel;
        }
    }
}
