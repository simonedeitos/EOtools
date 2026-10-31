using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json.Linq;

namespace PlaylistDownloader
{
    internal static class CsvUtil
    {
        public static string Escape(string s, char sep)
        {
            if (s == null) return "";
            s = s.Replace("\r", " ").Replace("\n", " ");
            if (s.IndexOf(sep) >= 0 || s.IndexOf('"') >= 0)
                return "\"" + s.Replace("\"", "\"\"") + "\"";
            return s;
        }

        public static List<string> ParseLine(string line, char sep)
        {
            var fields = new List<string>();
            var sb = new StringBuilder();
            bool quoted = false;
            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (quoted)
                {
                    if (c == '"')
                    {
                        if (i + 1 < line.Length && line[i + 1] == '"') { sb.Append('"'); i++; }
                        else quoted = false;
                    }
                    else sb.Append(c);
                }
                else if (c == '"') quoted = true;
                else if (c == sep) { fields.Add(sb.ToString()); sb.Clear(); }
                else sb.Append(c);
            }
            fields.Add(sb.ToString());
            return fields;
        }

        public static char DetectSeparator(string line)
        {
            int semi = line.Count(c => c == ';');
            int comma = line.Count(c => c == ',');
            int tab = line.Count(c => c == '\t');
            if (semi >= comma && semi >= tab && semi > 0) return ';';
            if (tab > comma) return '\t';
            return ',';
        }
    }

    // Thin Deezer public API client with a shared rate limiter and retry on quota errors
    internal sealed class DeezerApiClient
    {
        private readonly HttpClient http;
        private readonly RateLimiter limiter;

        public Action<string> Trace;

        public DeezerApiClient(HttpClient http, int maxPer5s)
        {
            this.http = http;
            limiter = new RateLimiter(maxPer5s, 5000);
        }

        public static bool TryParseDate(string s, out DateTime date)
        {
            return DateTime.TryParseExact(s, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out date);
        }

        // Returns null when the resource does not exist; quota errors are waited out, other failures throw after 5 attempts
        public async Task<JObject> GetJsonAsync(string url, CancellationToken ct)
        {
            int failures = 0, quotaHits = 0;
            string lastError = "";
            while (failures < 5)
            {
                await limiter.WaitAsync(ct).ConfigureAwait(false);
                try
                {
                    using (var cts = CancellationTokenSource.CreateLinkedTokenSource(ct))
                    {
                        cts.CancelAfter(TimeSpan.FromSeconds(30));
                        using (HttpResponseMessage response = await http.GetAsync(url, cts.Token).ConfigureAwait(false))
                        {
                            int status = (int)response.StatusCode;
                            string body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                            JObject obj = null;
                            try { obj = JObject.Parse(body); } catch (Exception) { }

                            JObject err = obj == null ? null : obj["error"] as JObject;
                            int code = err != null && err["code"] != null ? (int)err["code"] : 0;

                            if (status == 429 || code == 4 || code == 700)
                            {
                                quotaHits++;
                                int wait = Math.Min(60000, 5000 * quotaHits);
                                if (Trace != null) Trace("Quota Deezer superata, pausa di " + wait / 1000 + "s");
                                limiter.PauseFor(wait);
                                if (quotaHits > 30) throw new IOException("Quota Deezer superata troppe volte di seguito. Riduci le richieste ogni 5s e riprova.");
                                continue;
                            }
                            if (status >= 500 || obj == null)
                            {
                                lastError = "HTTP " + status;
                                failures++;
                                if (Trace != null) Trace(lastError + " da Deezer, riprovo (tentativo " + failures + ")");
                                limiter.PauseFor(2000 * failures);
                                continue;
                            }
                            if (err != null) return null;
                            return obj;
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    if (ct.IsCancellationRequested) throw;
                    lastError = "timeout";
                    failures++;
                    if (Trace != null) Trace("Timeout (tentativo " + failures + ")");
                    limiter.PauseFor(1000 * failures);
                }
                catch (IOException) when (quotaHits > 30)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    lastError = ex.Message;
                    failures++;
                    if (Trace != null) Trace("Errore di rete: " + ex.Message + " (tentativo " + failures + ")");
                    limiter.PauseFor(1000 * failures);
                }
            }
            throw new IOException("Deezer non risponde (" + lastError + "): " + url);
        }

        public async Task<JObject> GetAlbumAsync(long id, CancellationToken ct)
        {
            JObject obj = await GetJsonAsync("https://api.deezer.com/album/" + id, ct).ConfigureAwait(false);
            return obj == null || obj["id"] == null ? null : obj;
        }

        private async Task<JObject> GetAlbumOrNullAsync(long id, CancellationToken ct)
        {
            try { return await GetAlbumAsync(id, ct).ConfigureAwait(false); }
            catch (OperationCanceledException) { throw; }
            catch (Exception) { return null; }
        }

        // Newest albums known to Deezer: gives the current highest album id
        public async Task<long> GetLatestAlbumIdAsync(CancellationToken ct)
        {
            long max = 0;
            foreach (string url in new[] { "https://api.deezer.com/editorial/0/releases?limit=100", "https://api.deezer.com/chart/0/albums?limit=100" })
            {
                try
                {
                    JObject obj = await GetJsonAsync(url, ct).ConfigureAwait(false);
                    JArray data = obj == null ? null : obj["data"] as JArray;
                    if (data == null) continue;
                    foreach (JObject a in data.OfType<JObject>())
                    {
                        long id;
                        if (long.TryParse((string)a["id"], out id) && id > max) max = id;
                    }
                }
                catch (OperationCanceledException) { throw; }
                catch (Exception) { }
            }
            return max;
        }

        // Album ids grow with time; the median release date of a few neighbouring albums is a robust time marker
        public async Task<DateTime?> ProbeDateAsync(long start, CancellationToken ct)
        {
            var dates = new List<DateTime>();
            long id = start;
            for (int round = 0; round < 8 && dates.Count < 3; round++)
            {
                long first = id;
                JObject[] albums = await Task.WhenAll(Enumerable.Range(0, 10).Select(i => GetAlbumOrNullAsync(first + i, ct))).ConfigureAwait(false);
                foreach (JObject a in albums)
                {
                    DateTime d;
                    if (a != null && TryParseDate((string)a["release_date"], out d))
                        dates.Add(d);
                }
                id += 10;
            }
            if (dates.Count == 0)
            {
                if (Trace != null) Trace("Sondaggio ID " + start + ": nessun album valido");
                return null;
            }
            dates.Sort();
            DateTime median = dates[dates.Count / 2];
            if (Trace != null) Trace("Sondaggio ID " + start + ": data " + median.ToString("dd/MM/yyyy"));
            return median;
        }

        public async Task<long> FindUpperBoundAsync(DateTime stopDate, CancellationToken ct)
        {
            long latest = await GetLatestAlbumIdAsync(ct).ConfigureAwait(false);
            if (latest > 0)
            {
                if (Trace != null) Trace("Album piu' recente noto: ID " + latest);
                return latest + latest / 20 + 100000;
            }

            if (Trace != null) Trace("Impossibile leggere le novita' Deezer: stima dell'ID massimo per raddoppio");
            long hi = 1000000;
            while (hi < (1L << 40))
            {
                DateTime? d = await ProbeDateAsync(hi, ct).ConfigureAwait(false);
                if (d == null || d.Value >= stopDate) return hi;
                hi *= 2;
            }
            return hi;
        }

        // First album id whose median release date is >= threshold (or that does not exist)
        public async Task<long> FindFirstIdAsync(DateTime threshold, long hi, CancellationToken ct)
        {
            long lo = 1;
            while (hi - lo > 200)
            {
                long mid = lo + (hi - lo) / 2;
                DateTime? d = await ProbeDateAsync(mid, ct).ConfigureAwait(false);
                if (d == null || d.Value >= threshold) hi = mid;
                else lo = mid + 1;
            }
            return lo;
        }
    }

    // Menu "Operazioni > Ricerca Brani per Release Date"
    public class ReleaseSearchForm : Form
    {
        private readonly HttpClient http;
        private RadioButton rbMonth, rbMonths, rbRange;
        private DateTimePicker dtFrom, dtTo;
        private Label lblPeriod, lblTo, lblStats;
        private NumericUpDown numThreads, numRate, numBefore, numAfter;
        private TextBox txtIdFrom, txtIdTo, txtLog;
        private CheckBox chkIsrc, chkSaveAll;
        private Button btnStart, btnPause, btnStop, btnExport;
        private ProgressBar bar;
        private System.Windows.Forms.Timer timer;

        private CancellationTokenSource cts;
        private volatile bool running;
        private volatile bool paused;
        private readonly Stopwatch active = new Stopwatch();
        private readonly object writerLock = new object();
        private StreamWriter writer;
        private string currentFile;
        private ConcurrentBag<long> failedIds = new ConcurrentBag<long>();

        private long loId, hiId, nextId;
        private long scanned, albumsValid, albumsInRange, tracksFound, tracksInPeriod, failedCount, isrcDone;
        private DateTime periodFrom, periodTo;
        private bool saveAll;
        private string phase = "";

        public bool IsRunning { get { return running; } }

        public ReleaseSearchForm(HttpClient http)
        {
            this.http = http;
            Text = "Ricerca Brani per Release Date";
            ClientSize = new Size(760, 600);
            MinimumSize = new Size(640, 520);
            StartPosition = FormStartPosition.CenterParent;
            BuildUi();
            UpdatePeriodText();

            timer = new System.Windows.Forms.Timer { Interval = 500 };
            timer.Tick += (s, e) => RefreshStats();
            timer.Start();
            FormClosing += OnFormClosing;
        }

        private void BuildUi()
        {
            var gbPeriod = new GroupBox { Text = "Periodo (release date)", Location = new Point(10, 8), Size = new Size(740, 96), Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };
            rbMonth = new RadioButton { Text = "Mese intero", Location = new Point(15, 22), Size = new Size(120, 22), Checked = true };
            rbMonths = new RadioButton { Text = "Più mesi", Location = new Point(145, 22), Size = new Size(110, 22) };
            rbRange = new RadioButton { Text = "Intervallo di date (da - a)", Location = new Point(265, 22), Size = new Size(200, 22) };
            DialogHelper.AddLabel(gbPeriod, "Da:", 15, 62, 28);
            dtFrom = new DateTimePicker { Location = new Point(45, 58), Size = new Size(110, 22), Value = DateTime.Today };
            lblTo = DialogHelper.AddLabel(gbPeriod, "A:", 175, 62, 24);
            dtTo = new DateTimePicker { Location = new Point(203, 58), Size = new Size(110, 22), Value = DateTime.Today };
            lblPeriod = new Label { Location = new Point(335, 62), Size = new Size(395, 20), Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };
            gbPeriod.Controls.AddRange(new Control[] { rbMonth, rbMonths, rbRange, dtFrom, dtTo, lblPeriod });
            rbMonth.CheckedChanged += ModeChanged;
            rbMonths.CheckedChanged += ModeChanged;
            rbRange.CheckedChanged += ModeChanged;
            dtFrom.ValueChanged += (s, e) => UpdatePeriodText();
            dtTo.ValueChanged += (s, e) => UpdatePeriodText();
            ModeChanged(null, EventArgs.Empty);

            var gbOpt = new GroupBox { Text = "Opzioni", Location = new Point(10, 110), Size = new Size(740, 138), Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };
            DialogHelper.AddLabel(gbOpt, "Thread:", 15, 24, 55);
            numThreads = new NumericUpDown { Location = new Point(72, 21), Size = new Size(55, 22), Minimum = 1, Maximum = 64, Value = AppSettings.LookupThreads };
            DialogHelper.AddLabel(gbOpt, "Richieste / 5s:", 145, 24, 90);
            numRate = new NumericUpDown { Location = new Point(237, 21), Size = new Size(60, 22), Minimum = 5, Maximum = 500, Value = AppSettings.LookupRatePer5s };
            DialogHelper.AddLabel(gbOpt, "Margine ID (giorni) prima:", 320, 24, 150);
            numBefore = new NumericUpDown { Location = new Point(472, 21), Size = new Size(50, 22), Minimum = 0, Maximum = 365, Value = 14 };
            DialogHelper.AddLabel(gbOpt, "dopo:", 535, 24, 40);
            numAfter = new NumericUpDown { Location = new Point(577, 21), Size = new Size(50, 22), Minimum = 0, Maximum = 365, Value = 2 };
            DialogHelper.AddLabel(gbOpt, "ID album da (opz.):", 15, 56, 110);
            txtIdFrom = new TextBox { Location = new Point(127, 53), Size = new Size(110, 22) };
            DialogHelper.AddLabel(gbOpt, "a (opz.):", 255, 56, 55);
            txtIdTo = new TextBox { Location = new Point(312, 53), Size = new Size(110, 22) };
            chkSaveAll = new CheckBox { Text = "Salva TUTTI i brani scansionati con la Release Date (anche fuori periodo): poi filtri il CSV senza rilanciare la ricerca", Location = new Point(15, 82), Size = new Size(715, 22), Checked = true };
            chkIsrc = new CheckBox { Text = "Recupera anche l'ISRC dei brani nel periodo (1 richiesta in più per brano: molto più lento)", Location = new Point(15, 106), Size = new Size(700, 22) };
            gbOpt.Controls.AddRange(new Control[] { numThreads, numRate, numBefore, numAfter, txtIdFrom, txtIdTo, chkSaveAll, chkIsrc });

            btnStart = DialogHelper.AddButton(this, "Avvia ricerca", 10, 258, 120, btnStart_Click);
            btnPause = DialogHelper.AddButton(this, "Pausa", 138, 258, 90, btnPause_Click);
            btnStop = DialogHelper.AddButton(this, "Ferma", 236, 258, 90, btnStop_Click);
            btnExport = DialogHelper.AddButton(this, "Esporta CSV...", 334, 258, 130, btnExport_Click);
            btnPause.Enabled = btnStop.Enabled = btnExport.Enabled = false;

            bar = new ProgressBar { Location = new Point(10, 294), Size = new Size(740, 20), Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right, Maximum = 1000 };
            lblStats = new Label { Location = new Point(10, 320), Size = new Size(740, 60), Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };
            txtLog = new TextBox
            {
                Location = new Point(10, 386),
                Size = new Size(740, 204),
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };
            Controls.AddRange(new Control[] { gbPeriod, gbOpt, bar, lblStats, txtLog });
        }

        private void ModeChanged(object sender, EventArgs e)
        {
            bool single = rbMonth.Checked;
            bool month = !rbRange.Checked;
            dtFrom.Format = dtTo.Format = DateTimePickerFormat.Custom;
            dtFrom.CustomFormat = dtTo.CustomFormat = month ? "MM/yyyy" : "dd/MM/yyyy";
            dtFrom.ShowUpDown = dtTo.ShowUpDown = month;
            dtTo.Visible = lblTo.Visible = !single;
            UpdatePeriodText();
        }

        private bool TryGetRange(out DateTime from, out DateTime to)
        {
            if (rbRange.Checked)
            {
                from = dtFrom.Value.Date;
                to = dtTo.Value.Date;
            }
            else
            {
                from = new DateTime(dtFrom.Value.Year, dtFrom.Value.Month, 1);
                DateTime last = rbMonth.Checked ? dtFrom.Value : dtTo.Value;
                to = new DateTime(last.Year, last.Month, 1).AddMonths(1).AddDays(-1);
            }
            return from <= to;
        }

        private void UpdatePeriodText()
        {
            if (lblPeriod == null) return;
            DateTime from, to;
            lblPeriod.Text = TryGetRange(out from, out to)
                ? string.Format("Periodo: {0:dd/MM/yyyy} - {1:dd/MM/yyyy}", from, to)
                : "Periodo non valido (la data iniziale è dopo quella finale)";
        }

        private void Log(string text)
        {
            if (IsDisposed) return;
            txtLog.AppendText(DateTime.Now.ToString("HH:mm:ss") + "  " + text + Environment.NewLine);
        }

        private void SetRunning(bool value)
        {
            running = value;
            btnStart.Enabled = !value;
            btnPause.Enabled = btnStop.Enabled = value;
            btnPause.Text = "Pausa";
            paused = false;
            btnExport.Enabled = !value && currentFile != null && File.Exists(currentFile);
            foreach (Control c in new Control[] { rbMonth, rbMonths, rbRange, dtFrom, dtTo, numThreads, numRate, numBefore, numAfter, txtIdFrom, txtIdTo, chkSaveAll, chkIsrc })
                c.Enabled = !value;
        }

        private static bool TryParseId(string text, out long id, out bool provided)
        {
            id = 0;
            provided = !string.IsNullOrWhiteSpace(text);
            return !provided || long.TryParse(text.Trim(), out id);
        }

        private async void btnStart_Click(object sender, EventArgs e)
        {
            DateTime from, to;
            if (!TryGetRange(out from, out to))
            {
                MessageBox.Show("Periodo non valido.", Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            long manualFrom, manualTo;
            bool hasFrom, hasTo;
            if (!TryParseId(txtIdFrom.Text, out manualFrom, out hasFrom) || !TryParseId(txtIdTo.Text, out manualTo, out hasTo))
            {
                MessageBox.Show("Gli ID album devono essere numeri.", Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int threads = (int)numThreads.Value;
            var client = new DeezerApiClient(http, (int)numRate.Value);
            client.Trace = msg =>
            {
                try { if (!IsDisposed) BeginInvoke(new Action(() => Log(msg))); } catch (Exception) { }
            };
            cts = new CancellationTokenSource();
            CancellationToken ct = cts.Token;

            scanned = albumsValid = albumsInRange = tracksFound = tracksInPeriod = failedCount = isrcDone = 0;
            failedIds = new ConcurrentBag<long>();
            periodFrom = from;
            periodTo = to;
            saveAll = chkSaveAll.Checked;
            bar.Value = 0;
            SetRunning(true);
            active.Reset();
            active.Start();

            try
            {
                Log(string.Format("Ricerca brani con release date {0:dd/MM/yyyy} - {1:dd/MM/yyyy}", from, to));
                loId = manualFrom;
                hiId = manualTo;
                if (!hasFrom || !hasTo)
                {
                    phase = "Ricerca dell'intervallo di ID album...";
                    Log(phase + " (ogni sondaggio richiede alcune decine di richieste: con il limite di quota possono servire diversi minuti)");
                    DateTime stopDate = to.AddDays(1 + (int)numAfter.Value);
                    long bound = await client.FindUpperBoundAsync(stopDate, ct);
                    if (!hasFrom) loId = await client.FindFirstIdAsync(from.AddDays(-(int)numBefore.Value), bound, ct);
                    if (!hasTo) hiId = await client.FindFirstIdAsync(stopDate, bound, ct);
                }
                if (hiId <= loId)
                    throw new InvalidOperationException("Intervallo ID album non valido: " + loId + " - " + hiId);

                Log(string.Format("Scansione ID album da {0} a {1} ({2:N0} ID)", loId, hiId, hiId - loId));
                string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PlaylistDownloader", "releasesearch");
                Directory.CreateDirectory(dir);
                currentFile = Path.Combine(dir, string.Format("release_{0:yyyyMMdd}_{1:yyyyMMdd}_{2:yyyyMMdd_HHmmss}.csv", from, to, DateTime.Now));
                Log("File di lavoro (salvataggio continuo): " + currentFile);

                writer = new StreamWriter(new FileStream(currentFile, FileMode.Create, FileAccess.Write, FileShare.ReadWrite), new UTF8Encoding(true));
                writer.WriteLine("Artista;Titolo;ISRC;Track ID;Release Date");
                writer.Flush();

                phase = "Scansione album";
                await ScanAsync(client, from, to, threads, ct);

                if (failedIds.Count > 0)
                {
                    phase = "Nuovo tentativo ID falliti";
                    Log(string.Format("Ritento {0} ID falliti", failedIds.Count));
                    await RetryFailedAsync(client, from, to, threads, ct);
                }
                CloseWriter();

                Log(string.Format("Scansione completata: {0:N0} album nel periodo, {1:N0} brani nel periodo ({2:N0} brani salvati in totale). ID falliti: {3}", albumsInRange, tracksInPeriod, tracksFound, failedIds.Count));

                if (chkIsrc.Checked && tracksInPeriod > 0)
                {
                    phase = "Recupero ISRC";
                    Log(phase);
                    await FetchIsrcAsync(client, threads, ct);
                    Log("ISRC recuperati. File: " + currentFile);
                }
                phase = "Completato";
            }
            catch (OperationCanceledException)
            {
                phase = "Interrotto";
                Log(string.Format("Interrotto. Ultimo ID scansionato circa {0}. Puoi esportare quanto trovato o riprendere da quell'ID (campo \"ID album da\").", Interlocked.Read(ref nextId)));
            }
            catch (Exception ex)
            {
                phase = "Errore";
                Log("Errore: " + ex.Message);
                if (!IsDisposed)
                    MessageBox.Show(ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                CloseWriter();
                active.Stop();
                if (!IsDisposed)
                {
                    SetRunning(false);
                    RefreshStats();
                }
            }
        }

        private void CloseWriter()
        {
            lock (writerLock)
            {
                if (writer != null)
                {
                    writer.Flush();
                    writer.Dispose();
                    writer = null;
                }
            }
        }

        private async Task ScanAsync(DeezerApiClient client, DateTime from, DateTime to, int threads, CancellationToken ct)
        {
            nextId = loId - 1;
            Task[] workers = Enumerable.Range(0, threads).Select(i => Task.Run(async () =>
            {
                while (!ct.IsCancellationRequested)
                {
                    while (paused && !ct.IsCancellationRequested)
                        await Task.Delay(200, ct).ConfigureAwait(false);
                    long id = Interlocked.Increment(ref nextId);
                    if (id >= hiId) break;
                    await ProcessAlbumAsync(client, id, from, to, ct).ConfigureAwait(false);
                    Interlocked.Increment(ref scanned);
                }
            })).ToArray();
            await Task.WhenAll(workers);
            ct.ThrowIfCancellationRequested();
        }

        private async Task RetryFailedAsync(DeezerApiClient client, DateTime from, DateTime to, int threads, CancellationToken ct)
        {
            long[] ids = failedIds.ToArray();
            failedIds = new ConcurrentBag<long>();
            Interlocked.Exchange(ref failedCount, 0);
            int idx = -1;
            Task[] workers = Enumerable.Range(0, threads).Select(i => Task.Run(async () =>
            {
                while (!ct.IsCancellationRequested)
                {
                    int k = Interlocked.Increment(ref idx);
                    if (k >= ids.Length) break;
                    await ProcessAlbumAsync(client, ids[k], from, to, ct).ConfigureAwait(false);
                }
            })).ToArray();
            await Task.WhenAll(workers);
            ct.ThrowIfCancellationRequested();
        }

        private async Task ProcessAlbumAsync(DeezerApiClient client, long id, DateTime from, DateTime to, CancellationToken ct)
        {
            try
            {
                JObject album = await client.GetAlbumAsync(id, ct).ConfigureAwait(false);
                if (album == null) return;
                Interlocked.Increment(ref albumsValid);

                DateTime rd;
                bool hasDate = DeezerApiClient.TryParseDate((string)album["release_date"], out rd);
                bool inPeriod = hasDate && rd >= from && rd <= to;
                if (!inPeriod && !saveAll)
                    return;
                if (inPeriod) Interlocked.Increment(ref albumsInRange);
                string dateText = hasDate ? rd.ToString("yyyy-MM-dd") : "";

                var rows = new List<string>();
                JObject tracks = album["tracks"] as JObject;
                while (tracks != null)
                {
                    JArray data = tracks["data"] as JArray;
                    if (data != null)
                    {
                        foreach (JObject t in data.OfType<JObject>())
                        {
                            string tid = (string)t["id"];
                            if (string.IsNullOrEmpty(tid) || tid == "0") continue;
                            JObject artist = t["artist"] as JObject;
                            string name = artist != null ? (string)artist["name"] : null;
                            if (string.IsNullOrEmpty(name))
                            {
                                JObject albumArtist = album["artist"] as JObject;
                                name = albumArtist != null ? (string)albumArtist["name"] : "";
                            }
                            rows.Add(CsvUtil.Escape(name, ';') + ";" + CsvUtil.Escape((string)t["title"], ';') + ";;" + tid + ";" + dateText);
                        }
                    }
                    string next = (string)tracks["next"];
                    tracks = string.IsNullOrEmpty(next) ? null : await client.GetJsonAsync(next, ct).ConfigureAwait(false);
                }

                lock (writerLock)
                {
                    if (writer != null)
                        foreach (string r in rows) writer.WriteLine(r);
                }
                Interlocked.Add(ref tracksFound, rows.Count);
                if (inPeriod) Interlocked.Add(ref tracksInPeriod, rows.Count);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception)
            {
                failedIds.Add(id);
                Interlocked.Increment(ref failedCount);
            }
        }

        private async Task FetchIsrcAsync(DeezerApiClient client, int threads, CancellationToken ct)
        {
            string src = currentFile;
            string dst = src.Substring(0, src.Length - 4) + "-isrc.csv";
            Interlocked.Exchange(ref isrcDone, 0);

            using (var reader = new StreamReader(src, Encoding.UTF8))
            using (var w = new StreamWriter(new FileStream(dst, FileMode.Create, FileAccess.Write, FileShare.ReadWrite), new UTF8Encoding(true)))
            using (var sem = new SemaphoreSlim(threads))
            {
                w.WriteLine(await reader.ReadLineAsync());
                while (true)
                {
                    var batch = new List<List<string>>();
                    string line;
                    while (batch.Count < 2000 && (line = await reader.ReadLineAsync()) != null)
                    {
                        if (line.Length == 0) continue;
                        List<string> f = CsvUtil.ParseLine(line, ';');
                        if (f.Count >= 4) batch.Add(f);
                    }
                    if (batch.Count == 0) break;

                    await Task.WhenAll(batch.Select(async f =>
                    {
                        await sem.WaitAsync(ct).ConfigureAwait(false);
                        try
                        {
                            DateTime d;
                            bool inPeriod = f.Count < 5 || !DeezerApiClient.TryParseDate(f[4], out d) || (d >= periodFrom && d <= periodTo);
                            if (inPeriod)
                            {
                                JObject t = await client.GetJsonAsync("https://api.deezer.com/track/" + f[3], ct).ConfigureAwait(false);
                                f[2] = t == null ? "" : ((string)t["isrc"] ?? "");
                            }
                        }
                        catch (OperationCanceledException)
                        {
                            throw;
                        }
                        catch (Exception)
                        {
                        }
                        finally
                        {
                            sem.Release();
                            Interlocked.Increment(ref isrcDone);
                        }
                    }));

                    foreach (List<string> f in batch)
                        w.WriteLine(string.Join(";", f.Select(x => CsvUtil.Escape(x, ';'))));
                }
            }
            currentFile = dst;
        }

        private void RefreshStats()
        {
            if (IsDisposed || lblStats == null) return;

            long total = Math.Max(1, hiId - loId);
            long sc = Interlocked.Read(ref scanned);
            double secs = Math.Max(1, active.Elapsed.TotalSeconds);
            var sb = new StringBuilder();
            sb.AppendLine("Stato: " + phase);

            if (phase == "Recupero ISRC")
            {
                long done = Interlocked.Read(ref isrcDone);
                long all = Math.Max(1, Interlocked.Read(ref tracksFound));
                bar.Value = (int)Math.Min(1000, done * 1000 / all);
                sb.AppendLine(string.Format("ISRC: {0:N0} / {1:N0}", done, all));
            }
            else if (hiId > loId)
            {
                bar.Value = (int)Math.Min(1000, sc * 1000 / total);
                double rate = sc / secs;
                string eta = "";
                if (running && rate > 0 && sc < total && phase == "Scansione album")
                {
                    TimeSpan left = TimeSpan.FromSeconds((total - sc) / rate);
                    eta = string.Format(" - tempo residuo stimato: {0}g {1:hh\\:mm\\:ss}", left.Days, left);
                }
                sb.AppendLine(string.Format("ID scansionati: {0:N0} / {1:N0} ({2:0.0}%) - {3:0.0} ID/s{4}", sc, total, sc * 100.0 / total, rate, eta));
            }

            sb.Append(string.Format("Album validi: {0:N0}   Album nel periodo: {1:N0}   Brani nel periodo: {2:N0}   Brani salvati: {3:N0}   ID falliti: {4:N0}",
                Interlocked.Read(ref albumsValid), Interlocked.Read(ref albumsInRange), Interlocked.Read(ref tracksInPeriod), Interlocked.Read(ref tracksFound), Interlocked.Read(ref failedCount)));
            lblStats.Text = sb.ToString();
        }

        private void btnPause_Click(object sender, EventArgs e)
        {
            paused = !paused;
            btnPause.Text = paused ? "Riprendi" : "Pausa";
            if (paused) active.Stop(); else active.Start();
            Log(paused ? "In pausa" : "Ripreso");
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            if (cts != null) cts.Cancel();
        }

        private void btnExport_Click(object sender, EventArgs e)
        {
            if (currentFile == null || !File.Exists(currentFile))
                return;

            using (var dlg = new SaveFileDialog())
            {
                dlg.Filter = "CSV (*.csv)|*.csv";
                dlg.FileName = Path.GetFileName(currentFile);
                if (dlg.ShowDialog(this) != DialogResult.OK) return;
                try
                {
                    bool onlyPeriod = false;
                    if (saveAll && tracksFound > tracksInPeriod)
                    {
                        onlyPeriod = MessageBox.Show(
                            string.Format("Il file contiene anche brani fuori periodo.\n\nSì = esporta solo i brani con release date {0:dd/MM/yyyy} - {1:dd/MM/yyyy}\nNo = esporta tutto (con la colonna Release Date)", periodFrom, periodTo),
                            Text, MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes;
                    }
                    if (onlyPeriod) ExportPeriodOnly(currentFile, dlg.FileName);
                    else CopySharedFile(currentFile, dlg.FileName);
                    Log("Esportato: " + dlg.FileName);
                    MessageBox.Show("CSV esportato:\n" + dlg.FileName, Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void ExportPeriodOnly(string src, string dst)
        {
            lock (writerLock)
            {
                if (writer != null) writer.Flush();
                using (var input = new FileStream(src, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (var reader = new StreamReader(input, Encoding.UTF8))
                using (var output = new StreamWriter(new FileStream(dst, FileMode.Create, FileAccess.Write), new UTF8Encoding(true)))
                {
                    output.WriteLine(reader.ReadLine());
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        if (line.Length == 0) continue;
                        List<string> f = CsvUtil.ParseLine(line, ';');
                        DateTime d;
                        if (f.Count >= 5 && DeezerApiClient.TryParseDate(f[4], out d) && d >= periodFrom && d <= periodTo)
                            output.WriteLine(line);
                    }
                }
            }
        }

        private void CopySharedFile(string src, string dst)
        {
            lock (writerLock)
            {
                if (writer != null) writer.Flush();
                using (var input = new FileStream(src, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (var output = new FileStream(dst, FileMode.Create, FileAccess.Write))
                    input.CopyTo(output);
            }
        }

        private void OnFormClosing(object sender, FormClosingEventArgs e)
        {
            if (running)
            {
                if (MessageBox.Show("Una ricerca è in corso. Fermarla e chiudere?", Text, MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                {
                    e.Cancel = true;
                    return;
                }
                if (cts != null) cts.Cancel();
            }
            timer.Stop();
        }
    }
}
