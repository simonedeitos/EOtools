using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PlaylistDownloader
{
    public class DownloadJob
    {
        public const string Queued = "In coda";
        public const string Running = "In corso";
        public const string Done = "Completato";
        public const string Failed = "Errore";
        public const string Skipped = "Saltato";
        public const string Cancelled = "Annullato";

        public int Index;
        public string Label;
        public string Url;
        public string Folder;

        // Written by worker threads, read by the UI timer
        public volatile string Status = Queued;
        public volatile string Detail = "";
        public volatile int Progress;
        public volatile bool Dirty = true;

        public void Set(string status, string detail, int? progress = null)
        {
            Status = status;
            if (detail != null) Detail = detail;
            if (progress.HasValue) Progress = progress.Value;
            Dirty = true;
        }
    }

    // Shared list of ARLs: hands out a working one, skips rejected ones, asks the user when all are exhausted
    internal sealed class ArlPool
    {
        private readonly object sync = new object();
        private readonly List<string> arls;
        private readonly HashSet<string> rejected = new HashSet<string>();
        private readonly Func<string, string> askUser;
        private string current;
        private bool cancelled;

        public ArlPool(Func<string, string> askUser)
        {
            this.askUser = askUser;
            arls = AppSettings.GetArls();
        }

        public bool Cancelled { get { lock (sync) return cancelled; } }

        public string Current { get { lock (sync) return current; } }

        // rejectedArl: the ARL this caller was given before and deemix refused (null on first request)
        public string Next(string rejectedArl)
        {
            lock (sync)
            {
                if (rejectedArl != null)
                    rejected.Add(rejectedArl);

                if (current != null && !rejected.Contains(current))
                    return current;

                current = arls.FirstOrDefault(a => !rejected.Contains(a));
                if (current != null)
                    return current;

                if (cancelled)
                    return null;

                string message = arls.Count == 0
                    ? "Nessun ARL salvato. Inserisci un ARL Deezer per continuare."
                    : "Tutti gli ARL salvati sono stati rifiutati (scaduti?). Inserisci un nuovo ARL.";
                string arl = askUser(message);
                if (string.IsNullOrWhiteSpace(arl))
                {
                    cancelled = true;
                    return null;
                }

                arl = arl.Trim();
                arls.Add(arl);
                rejected.Remove(arl);
                AppSettings.AddArl(arl);
                current = arl;
                return current;
            }
        }
    }

    // Runs deemix (python) processes in parallel and reports status through DownloadJob
    internal sealed class DeemixEngine
    {
        private static readonly Regex ProgressRegex = new Regex(@"Download at (\d+)%", RegexOptions.Compiled);

        private readonly List<DownloadJob> jobs;
        private readonly DownloadOptions options;
        private readonly int threads;
        private readonly string bitrate;
        private readonly ArlPool pool;
        private readonly Action<string> log;
        private readonly object fileLock = new object();
        private readonly Stopwatch clock = Stopwatch.StartNew();
        private readonly string workDir;
        private readonly string command;

        private volatile bool aborted;
        private long delayUntilMs;

        public volatile bool Paused;
        public string FatalMessage { get; private set; }

        public DeemixEngine(List<DownloadJob> jobs, DownloadOptions options, int threads, string bitrate, ArlPool pool, Action<string> log)
        {
            this.jobs = jobs;
            this.options = options;
            this.threads = Math.Max(1, threads);
            this.bitrate = bitrate;
            this.pool = pool;
            this.log = log;
            workDir = AppSettings.DeemixWorkDir;
            command = AppSettings.DeemixCommand;
        }

        public async Task RunAsync(CancellationToken ct)
        {
            var runnable = jobs.Where(j => j.Status == DownloadJob.Queued).ToList();
            if (runnable.Count == 0)
                return;

            try
            {
                PrepareWorkDir();

                if (pool.Next(null) == null)
                {
                    Fatal("Nessun ARL disponibile: download interrotto.");
                    return;
                }

                int next = 0;

                // The first job runs alone so deemix can create its config files without races
                next = 1;
                await RunJobAsync(runnable[0], ct).ConfigureAwait(false);

                Func<Task> worker = async () =>
                {
                    try
                    {
                        while (!ct.IsCancellationRequested && !aborted)
                        {
                            while (Paused && !ct.IsCancellationRequested)
                                await Task.Delay(200, ct).ConfigureAwait(false);

                            int i = Interlocked.Increment(ref next) - 1;
                            if (i >= runnable.Count)
                                return;

                            if (options.UseDelay && options.DelaySeconds > 0 && options.DelayInterval > 0 && i % options.DelayInterval == 0)
                                Interlocked.Exchange(ref delayUntilMs, clock.ElapsedMilliseconds + options.DelaySeconds * 1000L);

                            while (clock.ElapsedMilliseconds < Interlocked.Read(ref delayUntilMs) && !ct.IsCancellationRequested)
                                await Task.Delay(250, ct).ConfigureAwait(false);

                            await RunJobAsync(runnable[i], ct).ConfigureAwait(false);
                        }
                    }
                    catch (OperationCanceledException) { }
                };

                if (!aborted && !ct.IsCancellationRequested)
                    await Task.WhenAll(Enumerable.Range(0, threads).Select(n => Task.Run(worker))).ConfigureAwait(false);
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                Fatal("Errore imprevisto: " + ex.Message);
            }
        }

        private void Fatal(string message)
        {
            aborted = true;
            FatalMessage = message;
            log(message);
        }

        // Creates the config folder and, the first time, reuses an existing portable deemix config (and its ARL) from the desktop
        private void PrepareWorkDir()
        {
            string configDir = Path.Combine(workDir, "config");
            bool firstRun = !Directory.Exists(configDir);
            Directory.CreateDirectory(configDir);

            if (!firstRun)
                return;

            string legacy = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "config");
            if (!Directory.Exists(legacy))
                return;

            foreach (string file in Directory.GetFiles(legacy))
            {
                string name = Path.GetFileName(file);
                try
                {
                    if (name == ".arl")
                    {
                        string arl = File.ReadAllText(file).Trim();
                        if (arl.Length > 0)
                        {
                            AppSettings.AddArl(arl);
                            log("ARL esistente di deemix importato dal desktop.");
                        }
                    }
                    else
                    {
                        File.Copy(file, Path.Combine(configDir, name), true);
                    }
                }
                catch (Exception ex)
                {
                    log("Impossibile copiare " + name + ": " + ex.Message);
                }
            }
        }

        private void EnsureArlFile(string arl)
        {
            if (string.IsNullOrEmpty(arl))
                return;

            lock (fileLock)
            {
                string path = Path.Combine(workDir, "config", ".arl");
                try
                {
                    if (File.Exists(path) && File.ReadAllText(path).Trim() == arl)
                        return;
                    File.WriteAllText(path, arl, new UTF8Encoding(false));
                }
                catch (IOException) { }
            }
        }

        private static string Quote(string s)
        {
            // Backslashes right before the closing quote must be doubled
            int trailing = s.Length - s.TrimEnd('\\').Length;
            return "\"" + s.Replace("\"", "\\\"") + new string('\\', trailing) + "\"";
        }

        private ProcessStartInfo BuildStartInfo(DownloadJob job)
        {
            string file = command;
            string prefix = "";
            if (!File.Exists(command))
            {
                int sp = command.IndexOf(' ');
                if (sp > 0)
                {
                    file = command.Substring(0, sp);
                    prefix = command.Substring(sp + 1).Trim() + " ";
                }
            }

            var psi = new ProcessStartInfo
            {
                FileName = file,
                Arguments = prefix + "--portable " + Quote(job.Url) + " -b " + bitrate + " -p " + Quote(job.Folder),
                WorkingDirectory = workDir,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };
            // Without these python buffers its output and the ARL prompt would never reach us
            psi.EnvironmentVariables["PYTHONUNBUFFERED"] = "1";
            psi.EnvironmentVariables["PYTHONIOENCODING"] = "utf-8";
            psi.EnvironmentVariables["PYTHONUTF8"] = "1";
            return psi;
        }

        private static void KillTree(Process p)
        {
            try
            {
                if (p.HasExited) return;
                var kill = Process.Start(new ProcessStartInfo("taskkill", "/PID " + p.Id + " /T /F")
                {
                    CreateNoWindow = true,
                    UseShellExecute = false
                });
                if (kill != null) kill.WaitForExit(3000);
            }
            catch
            {
                try { p.Kill(); } catch { }
            }
        }

        private async Task RunJobAsync(DownloadJob job, CancellationToken ct)
        {
            if (ct.IsCancellationRequested || aborted)
                return;

            job.Set(DownloadJob.Running, "Avvio...", 0);

            try
            {
                Directory.CreateDirectory(job.Folder);
            }
            catch (Exception ex)
            {
                job.Set(DownloadJob.Failed, "Cartella non valida: " + ex.Message);
                return;
            }

            EnsureArlFile(pool.Current);

            Process process;
            try
            {
                process = Process.Start(BuildStartInfo(job));
            }
            catch (Win32Exception ex)
            {
                job.Set(DownloadJob.Failed, "deemix non avviabile: " + ex.Message);
                Fatal("Impossibile avviare \"" + command + "\". Imposta il comando in ARL / deemix. (" + ex.Message + ")");
                return;
            }

            bool success = false;
            string lastLine = "";
            string lastError = "";
            string lastSupplied = null;
            int lastReported = 0;

            using (process)
            using (ct.Register(() => KillTree(process)))
            {
                try { process.StandardInput.AutoFlush = true; } catch { }

                Task errTask = Task.Run(async () =>
                {
                    string line;
                    while ((line = await process.StandardError.ReadLineAsync().ConfigureAwait(false)) != null)
                    {
                        if (line.Trim().Length == 0) continue;
                        lastError = line.Trim();
                        log("[#" + job.Index + "] " + line);
                    }
                });

                var buf = new char[1024];
                var sb = new StringBuilder();
                int n;
                while ((n = await process.StandardOutput.ReadAsync(buf, 0, buf.Length).ConfigureAwait(false)) > 0)
                {
                    for (int i = 0; i < n; i++)
                    {
                        char c = buf[i];
                        if (c == '\n')
                        {
                            string line = sb.ToString().Trim();
                            sb.Clear();
                            if (line.Length == 0) continue;

                            log("[#" + job.Index + "] " + line);
                            lastLine = line;

                            Match m = ProgressRegex.Match(line);
                            if (m.Success)
                            {
                                int pct = int.Parse(m.Groups[1].Value);
                                if (pct >= 100 || pct - lastReported >= 10)
                                {
                                    lastReported = pct;
                                    job.Set(DownloadJob.Running, null, pct);
                                }
                            }
                            else if (line.IndexOf("Completed download of", StringComparison.OrdinalIgnoreCase) >= 0
                                  || line.IndexOf("already downloaded", StringComparison.OrdinalIgnoreCase) >= 0
                                  || line.IndexOf("Finished downloading", StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                success = true;
                            }

                            if (!success && !m.Success)
                                job.Set(DownloadJob.Running, line);
                        }
                        else if (c != '\r')
                        {
                            sb.Append(c);
                        }
                    }

                    // The ARL prompt has no trailing newline, so it has to be detected on the partial buffer
                    if (sb.Length > 0 && sb.ToString().IndexOf("your arl", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        sb.Clear();
                        string arl = await Task.Run(() => pool.Next(lastSupplied)).ConfigureAwait(false);
                        if (arl == null)
                        {
                            Fatal("ARL richiesto ma non disponibile: download interrotto.");
                            KillTree(process);
                            break;
                        }

                        lastSupplied = arl;
                        log("[#" + job.Index + "] ARL richiesto da deemix: inserito automaticamente (" + arl.Substring(0, Math.Min(6, arl.Length)) + "...)");
                        try
                        {
                            await process.StandardInput.WriteLineAsync(arl).ConfigureAwait(false);
                        }
                        catch (IOException) { }
                    }
                }

                await errTask.ConfigureAwait(false);
                process.WaitForExit();

                if (ct.IsCancellationRequested)
                    job.Set(DownloadJob.Cancelled, "Annullato", null);
                else if (success)
                    job.Set(DownloadJob.Done, "OK", 100);
                else
                    job.Set(DownloadJob.Failed, !string.IsNullOrEmpty(lastError) ? lastError : (lastLine.Length > 0 ? lastLine : "Exit code " + process.ExitCode));
            }
        }
    }

    public class DownloadForm : Form
    {
        private readonly List<DownloadJob> jobs;
        private readonly DownloadOptions options;
        private readonly DataTable table = new DataTable();
        private readonly DataGridView grid;
        private readonly TextBox txtLog;
        private readonly NumericUpDown numThreads;
        private readonly ComboBox cboBitrate;
        private readonly Button btnStart, btnPause, btnStop, btnRetry;
        private readonly ProgressBar bar;
        private readonly Label lblSummary;
        private readonly System.Windows.Forms.Timer timer;
        private readonly ConcurrentQueue<string> logQueue = new ConcurrentQueue<string>();

        private DeemixEngine engine;
        private CancellationTokenSource cts;
        private bool running;

        public DownloadForm(List<DownloadJob> jobs, DownloadOptions options)
        {
            this.jobs = jobs;
            this.options = options;

            Text = "Download (deemix)";
            ClientSize = new Size(960, 640);
            MinimumSize = new Size(760, 420);
            StartPosition = FormStartPosition.CenterParent;

            // Top bar
            var top = new Panel { Dock = DockStyle.Top, Height = 72 };
            top.Controls.Add(new Label { Text = "Thread:", Location = new Point(10, 14), AutoSize = true });
            numThreads = new NumericUpDown { Location = new Point(60, 10), Size = new Size(50, 22), Minimum = 1, Maximum = 32, Value = AppSettings.DownloadThreads };
            top.Controls.Add(numThreads);
            top.Controls.Add(new Label { Text = "Bitrate:", Location = new Point(124, 14), AutoSize = true });
            cboBitrate = new ComboBox { Location = new Point(172, 10), Size = new Size(70, 22), DropDownStyle = ComboBoxStyle.DropDownList };
            cboBitrate.Items.AddRange(new object[] { "128", "320", "flac" });
            cboBitrate.SelectedItem = cboBitrate.Items.Contains(AppSettings.DownloadBitrate) ? AppSettings.DownloadBitrate : "128";
            top.Controls.Add(cboBitrate);

            btnStart = DialogHelper.AddButton(top, "Avvia", 260, 8, 80, (s, e) => StartRun());
            btnPause = DialogHelper.AddButton(top, "Pausa", 346, 8, 80, (s, e) => TogglePause());
            btnStop = DialogHelper.AddButton(top, "Stop", 432, 8, 80, (s, e) => StopRun());
            btnRetry = DialogHelper.AddButton(top, "Riprova falliti", 518, 8, 110, (s, e) => RetryFailed());

            bar = new ProgressBar { Location = new Point(10, 42), Size = new Size(300, 20), Maximum = Math.Max(1, jobs.Count) };
            lblSummary = new Label { Location = new Point(320, 45), AutoSize = true };
            top.Controls.Add(bar);
            top.Controls.Add(lblSummary);

            // Grid + log
            var split = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Horizontal };
            table.Columns.Add("#", typeof(int));
            table.Columns.Add("Brano", typeof(string));
            table.Columns.Add("Stato", typeof(string));
            table.Columns.Add("Progresso", typeof(string));
            table.Columns.Add("Dettaglio", typeof(string));
            table.BeginLoadData();
            foreach (var j in jobs)
                table.Rows.Add(j.Index, j.Label, j.Status, "", j.Detail);
            table.EndLoadData();

            grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                DataSource = table,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                RowHeadersVisible = false,
                AllowUserToResizeColumns = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                BackgroundColor = SystemColors.Window
            };
            grid.CellFormatting += Grid_CellFormatting;
            split.Panel1.Controls.Add(grid);

            txtLog = new TextBox { Dock = DockStyle.Fill, Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Both, WordWrap = false, Font = new Font("Consolas", 8.5F), BackColor = Color.Black, ForeColor = Color.LightGray };
            split.Panel2.Controls.Add(txtLog);

            Controls.Add(split);
            Controls.Add(top);

            foreach (DataGridViewColumn col in grid.Columns)
            {
                col.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                col.Resizable = DataGridViewTriState.True;
            }
            grid.Columns[0].Width = 50;
            grid.Columns[1].Width = 360;
            grid.Columns[2].Width = 90;
            grid.Columns[3].Width = 80;
            grid.Columns[4].Width = 330;

            timer = new System.Windows.Forms.Timer { Interval = 300 };
            timer.Tick += (s, e) => RefreshUi();
            timer.Start();

            Shown += (s, e) =>
            {
                split.SplitterDistance = (int)(split.Height * 0.65);
                StartRun();
            };
            FormClosing += DownloadForm_FormClosing;
            UpdateButtons();
            RefreshUi();
        }

        private void Grid_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex != 2) return;
            switch (e.Value as string)
            {
                case DownloadJob.Done: e.CellStyle.BackColor = Color.LightGreen; break;
                case DownloadJob.Failed: e.CellStyle.BackColor = Color.LightPink; break;
                case DownloadJob.Running: e.CellStyle.BackColor = Color.LightYellow; break;
                case DownloadJob.Skipped: e.CellStyle.BackColor = Color.LightGray; break;
                case DownloadJob.Cancelled: e.CellStyle.BackColor = Color.LightSalmon; break;
            }
        }

        private void AppendLog(string line)
        {
            logQueue.Enqueue(line);
        }

        private string AskArl(string message)
        {
            try
            {
                if (IsDisposed || !IsHandleCreated) return null;
                return (string)Invoke(new Func<string>(() =>
                {
                    using (var f = new ArlPromptForm(message))
                    {
                        return f.ShowDialog(this) == DialogResult.OK && f.Arl.Length > 0 ? f.Arl : null;
                    }
                }));
            }
            catch (Exception)
            {
                return null;
            }
        }

        private void RefreshUi()
        {
            // Push changed jobs into the grid
            table.BeginLoadData();
            for (int i = 0; i < jobs.Count; i++)
            {
                DownloadJob j = jobs[i];
                if (!j.Dirty) continue;
                j.Dirty = false;
                DataRow row = table.Rows[i];
                row[2] = j.Status;
                row[3] = j.Status == DownloadJob.Running || j.Status == DownloadJob.Done ? j.Progress + "%" : "";
                row[4] = j.Detail;
            }
            table.EndLoadData();

            // Log
            if (!logQueue.IsEmpty)
            {
                var sb = new StringBuilder();
                string line;
                int count = 0;
                while (count++ < 500 && logQueue.TryDequeue(out line))
                    sb.AppendLine(line);

                if (txtLog.TextLength > 200000)
                    txtLog.Text = txtLog.Text.Substring(txtLog.TextLength / 2);
                txtLog.AppendText(sb.ToString());
            }

            int done = jobs.Count(j => j.Status == DownloadJob.Done);
            int failed = jobs.Count(j => j.Status == DownloadJob.Failed);
            int skipped = jobs.Count(j => j.Status == DownloadJob.Skipped);
            int active = jobs.Count(j => j.Status == DownloadJob.Running);
            bar.Value = Math.Min(bar.Maximum, done + failed + skipped);
            lblSummary.Text = $"Completati {done}/{jobs.Count}  |  In corso {active}  |  Errori {failed}  |  Saltati {skipped}";
        }

        private void UpdateButtons()
        {
            btnStart.Enabled = !running;
            btnPause.Enabled = running;
            btnStop.Enabled = running;
            btnRetry.Enabled = !running;
            numThreads.Enabled = !running;
            cboBitrate.Enabled = !running;
        }

        private async void StartRun()
        {
            if (running) return;

            running = true;
            AppSettings.DownloadThreads = (int)numThreads.Value;
            AppSettings.DownloadBitrate = (string)cboBitrate.SelectedItem;
            btnPause.Text = "Pausa";
            UpdateButtons();

            cts = new CancellationTokenSource();
            var pool = new ArlPool(AskArl);
            engine = new DeemixEngine(jobs, options, (int)numThreads.Value, (string)cboBitrate.SelectedItem, pool, AppendLog);

            try
            {
                await Task.Run(() => engine.RunAsync(cts.Token));
            }
            catch (Exception ex)
            {
                AppendLog("Errore: " + ex.Message);
            }

            running = false;
            if (IsDisposed) return;

            // Jobs interrupted by a stop go back to a retryable state
            foreach (var j in jobs.Where(j => j.Status == DownloadJob.Running))
                j.Set(DownloadJob.Cancelled, "Interrotto");

            UpdateButtons();
            RefreshUi();

            if (engine.FatalMessage != null)
                MessageBox.Show(this, engine.FatalMessage, "Download interrotto", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            else if (!cts.IsCancellationRequested)
                AppendLog("Download terminato.");
        }

        private void TogglePause()
        {
            if (engine == null) return;
            engine.Paused = !engine.Paused;
            btnPause.Text = engine.Paused ? "Riprendi" : "Pausa";
            AppendLog(engine.Paused ? "In pausa (i download in corso terminano, nessun nuovo brano parte)." : "Ripreso.");
        }

        private void StopRun()
        {
            if (cts != null) cts.Cancel();
            AppendLog("Stop richiesto...");
        }

        private void RetryFailed()
        {
            foreach (var j in jobs.Where(j => j.Status == DownloadJob.Failed || j.Status == DownloadJob.Cancelled))
                j.Set(DownloadJob.Queued, "");
            StartRun();
        }

        private void DownloadForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (running)
            {
                if (MessageBox.Show(this, "I download sono in corso. Interromperli e chiudere?", "Download",
                        MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
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
