using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32;
using Newtonsoft.Json.Linq;

namespace PlaylistDownloader
{
    // Persistent settings stored in HKCU\SOFTWARE\PlaylistDownloader
    internal static class AppSettings
    {
        private const string KeyPath = @"SOFTWARE\PlaylistDownloader";
        private const string ArlsValue = "Arls";

        public static bool ShowPlaylistSection
        {
            get { return GetInt("ShowPlaylistSection", 1) != 0; }
            set { SetInt("ShowPlaylistSection", value ? 1 : 0); }
        }

        public static int LookupThreads
        {
            get { return Clamp(GetInt("LookupThreads", 16), 1, 64); }
            set { SetInt("LookupThreads", Clamp(value, 1, 64)); }
        }

        // Deezer public API quota is about 50 requests every 5 seconds per IP
        public static int LookupRatePer5s
        {
            get { return Clamp(GetInt("LookupRatePer5s", 50), 5, 500); }
            set { SetInt("LookupRatePer5s", Clamp(value, 5, 500)); }
        }

        public static int DownloadThreads
        {
            get { return Clamp(GetInt("DownloadThreads", 10), 1, 32); }
            set { SetInt("DownloadThreads", Clamp(value, 1, 32)); }
        }

        public static string DownloadBitrate
        {
            get { return GetString("DownloadBitrate", "128"); }
            set { SetString("DownloadBitrate", value); }
        }

        public static string DeemixCommand
        {
            get { return GetString("DeemixCommand", "deemix"); }
            set { SetString("DeemixCommand", string.IsNullOrWhiteSpace(value) ? "deemix" : value.Trim()); }
        }

        // Working directory of deemix (its "config" folder, with .arl, lives here)
        public static string DeemixWorkDir
        {
            get
            {
                string dir = GetString("DeemixWorkDir", "");
                if (string.IsNullOrWhiteSpace(dir))
                {
                    dir = System.IO.Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                        "PlaylistDownloader", "deemix");
                }
                return dir;
            }
            set { SetString("DeemixWorkDir", value ?? ""); }
        }

        public static List<string> GetArls()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(KeyPath))
                {
                    string[] arls = key == null ? null : key.GetValue(ArlsValue) as string[];
                    if (arls != null)
                        return arls.Where(a => !string.IsNullOrWhiteSpace(a)).Select(a => a.Trim()).Distinct().ToList();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error reading ARLs: " + ex.Message);
            }
            return new List<string>();
        }

        public static void SetArls(IEnumerable<string> arls)
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.CreateSubKey(KeyPath))
                {
                    key.SetValue(ArlsValue, arls.Distinct().ToArray(), RegistryValueKind.MultiString);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error saving ARLs: " + ex.Message);
            }
        }

        public static void AddArl(string arl)
        {
            List<string> arls = GetArls();
            if (!arls.Contains(arl))
            {
                arls.Add(arl);
                SetArls(arls);
            }
        }

        private static int Clamp(int v, int min, int max)
        {
            return v < min ? min : (v > max ? max : v);
        }

        private static int GetInt(string name, int def)
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(KeyPath))
                {
                    object v = key == null ? null : key.GetValue(name);
                    if (v != null) return Convert.ToInt32(v);
                }
            }
            catch { }
            return def;
        }

        private static void SetInt(string name, int value)
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.CreateSubKey(KeyPath))
                    key.SetValue(name, value, RegistryValueKind.DWord);
            }
            catch { }
        }

        private static string GetString(string name, string def)
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(KeyPath))
                {
                    string v = key == null ? null : key.GetValue(name) as string;
                    if (!string.IsNullOrEmpty(v)) return v;
                }
            }
            catch { }
            return def;
        }

        private static void SetString(string name, string value)
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.CreateSubKey(KeyPath))
                    key.SetValue(name, value ?? "", RegistryValueKind.String);
            }
            catch { }
        }
    }

    internal sealed class DeezerTrackResult
    {
        public string TrackId;
        public string Artist;
        public string Title;
        public string Link;
        public bool Found;
        public bool Error;
    }

    // Sliding-window limiter shared by all workers, with a global pause used on quota errors
    internal sealed class RateLimiter
    {
        private readonly object gate = new object();
        private readonly Queue<long> stamps = new Queue<long>();
        private readonly Stopwatch clock = Stopwatch.StartNew();
        private readonly int max;
        private readonly long windowMs;
        private long pauseUntil;

        public RateLimiter(int maxPerWindow, long windowMs)
        {
            this.max = maxPerWindow;
            this.windowMs = windowMs;
        }

        public void PauseFor(long ms)
        {
            lock (gate)
            {
                long until = clock.ElapsedMilliseconds + ms;
                if (until > pauseUntil) pauseUntil = until;
            }
        }

        public async Task WaitAsync(CancellationToken ct)
        {
            while (true)
            {
                long wait;
                lock (gate)
                {
                    long now = clock.ElapsedMilliseconds;
                    while (stamps.Count > 0 && now - stamps.Peek() >= windowMs)
                        stamps.Dequeue();

                    if (now < pauseUntil)
                    {
                        wait = pauseUntil - now;
                    }
                    else if (stamps.Count < max)
                    {
                        stamps.Enqueue(now);
                        return;
                    }
                    else
                    {
                        wait = windowMs - (now - stamps.Peek());
                    }
                }

                await Task.Delay((int)Math.Max(wait, 10), ct).ConfigureAwait(false);
            }
        }
    }

    // Resolves ISRC -> Deezer track using several concurrent workers
    internal sealed class DeezerLookupService
    {
        private readonly HttpClient http;

        public DeezerLookupService(HttpClient http)
        {
            this.http = http;
        }

        public async Task<Dictionary<string, DeezerTrackResult>> LookupAsync(
            IEnumerable<string> isrcs, int threads, int maxPer5s, IProgress<int> progress, CancellationToken ct)
        {
            var list = isrcs.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            var queue = new ConcurrentQueue<string>(list);
            var results = new ConcurrentDictionary<string, DeezerTrackResult>(StringComparer.OrdinalIgnoreCase);
            var limiter = new RateLimiter(maxPer5s, 5000);
            int total = list.Count;
            int done = 0;

            Task[] workers = Enumerable.Range(0, Math.Max(1, threads)).Select(i => Task.Run(async () =>
            {
                string isrc;
                while (!ct.IsCancellationRequested && queue.TryDequeue(out isrc))
                {
                    results[isrc] = await LookupOneAsync(isrc, limiter, ct).ConfigureAwait(false);
                    int d = Interlocked.Increment(ref done);
                    if (progress != null && (d % 25 == 0 || d == total))
                        progress.Report(d);
                }
            })).ToArray();

            await Task.WhenAll(workers).ConfigureAwait(false);
            return new Dictionary<string, DeezerTrackResult>(results, StringComparer.OrdinalIgnoreCase);
        }

        private async Task<DeezerTrackResult> LookupOneAsync(string isrc, RateLimiter limiter, CancellationToken ct)
        {
            string url = "https://api.deezer.com/track/isrc:" + Uri.EscapeDataString(isrc);

            for (int attempt = 1; attempt <= 5; attempt++)
            {
                await limiter.WaitAsync(ct).ConfigureAwait(false);
                try
                {
                    using (HttpResponseMessage response = await http.GetAsync(url, ct).ConfigureAwait(false))
                    {
                        int status = (int)response.StatusCode;
                        if (status == 429 || status >= 500)
                        {
                            limiter.PauseFor(2000 * attempt);
                            continue;
                        }

                        string body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                        JObject obj = JObject.Parse(body);

                        JObject err = obj["error"] as JObject;
                        if (err != null)
                        {
                            int code = err["code"] != null ? (int)err["code"] : 0;
                            // 4 = quota exceeded, 700 = service busy: wait and retry
                            if (code == 4 || code == 700)
                            {
                                limiter.PauseFor(5000);
                                continue;
                            }
                            return new DeezerTrackResult();
                        }

                        string id = (string)obj["id"];
                        if (string.IsNullOrEmpty(id))
                            return new DeezerTrackResult();

                        JObject artist = obj["artist"] as JObject;
                        return new DeezerTrackResult
                        {
                            Found = true,
                            TrackId = id,
                            Title = (string)obj["title"],
                            Artist = artist != null ? (string)artist["name"] : null,
                            Link = (string)obj["link"]
                        };
                    }
                }
                catch (OperationCanceledException)
                {
                    if (ct.IsCancellationRequested) throw;
                    limiter.PauseFor(1000 * attempt); // HTTP timeout
                }
                catch (Exception)
                {
                    limiter.PauseFor(1000 * attempt);
                }
            }

            return new DeezerTrackResult { Error = true };
        }
    }
}
