using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PlaylistDownloader
{
    public partial class MainForm
    {
        private Button btnImportReleaseCsv;
        private ReleaseSearchForm releaseSearchForm;

        private void mnuReleaseSearch_Click(object sender, EventArgs e)
        {
            if (releaseSearchForm != null && !releaseSearchForm.IsDisposed)
            {
                releaseSearchForm.Activate();
                return;
            }
            releaseSearchForm = new ReleaseSearchForm(httpClient);
            releaseSearchForm.Show(this);
        }

        // Imports the CSV exported by the release date search (Artista;Titolo;ISRC;Track ID)
        private async void btnImportReleaseCsv_Click(object sender, EventArgs e)
        {
            string path;
            using (var open = new OpenFileDialog())
            {
                open.Filter = "CSV files (*.csv)|*.csv";
                open.Title = "Importa CSV ricerca per release date";
                if (open.ShowDialog(this) != DialogResult.OK) return;
                path = open.FileName;
            }

            DialogResult clear = MessageBox.Show("Svuotare i risultati esistenti prima di importare?", "Importa CSV release date",
                MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
            if (clear == DialogResult.Cancel) return;
            if (clear == DialogResult.Yes)
            {
                resultsTable.Clear();
                UpdateResultCount();
            }

            var existing = new HashSet<string>();
            int idCol = resultsTable.Columns["Track ID"].Ordinal;
            foreach (DataRow row in resultsTable.Rows)
                existing.Add(row[idCol] as string ?? "");

            btnImportReleaseCsv.Enabled = false;
            Cursor = Cursors.WaitCursor;
            try
            {
                string playlistName = "Release " + Path.GetFileNameWithoutExtension(path);
                var counts = new int[2];
                List<TrackInfo> tracks = await Task.Run(() => ReadReleaseCsv(path, playlistName, existing, counts));
                AddTracksToResults(tracks);
                UpdateResultCount();
                UpdateButtonStates();

                MessageBox.Show(
                    string.Format("Importati {0:N0} brani da {1}.\nDuplicati ignorati: {2:N0}\nRighe non valide: {3:N0}", tracks.Count, Path.GetFileName(path), counts[1], counts[0]),
                    "Importa CSV release date", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Errore durante l'import: " + ex.Message, "Importa CSV release date", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                Cursor = Cursors.Default;
                btnImportReleaseCsv.Enabled = true;
            }
        }

        private static List<TrackInfo> ReadReleaseCsv(string path, string playlistName, HashSet<string> existing, int[] counts)
        {
            var tracks = new List<TrackInfo>();
            int iArtist = 0, iTitle = 1, iIsrc = 2, iId = 3;
            char sep = ';';
            bool first = true;

            foreach (string line in File.ReadLines(path))
            {
                if (line.Length == 0) continue;
                if (first)
                {
                    first = false;
                    sep = CsvUtil.DetectSeparator(line);
                    string lower = line.ToLowerInvariant();
                    if (lower.Contains("track") || lower.Contains("isrc") || lower.Contains("artist") || lower.Contains("titol") || lower.Contains("title"))
                    {
                        List<string> h = CsvUtil.ParseLine(line, sep).Select(x => x.Trim().Trim('\uFEFF').ToLowerInvariant()).ToList();
                        int a = h.FindIndex(x => x.StartsWith("artist"));
                        int t = h.FindIndex(x => x.StartsWith("titol") || x.StartsWith("title"));
                        int s = h.FindIndex(x => x.Contains("isrc"));
                        int d = h.FindIndex(x => x.Contains("track") || x == "id");
                        if (a >= 0) iArtist = a;
                        if (t >= 0) iTitle = t;
                        if (s >= 0) iIsrc = s;
                        if (d >= 0) iId = d;
                        continue;
                    }
                }

                List<string> f = CsvUtil.ParseLine(line, sep);
                string id = iId < f.Count ? f[iId].Trim() : "";
                if (!IsDeezerId(id)) { counts[0]++; continue; }
                if (!existing.Add(id)) { counts[1]++; continue; }

                string isrc = iIsrc < f.Count ? f[iIsrc].Trim() : "";
                tracks.Add(new TrackInfo
                {
                    Artist = iArtist < f.Count && f[iArtist].Length > 0 ? f[iArtist] : "N/A",
                    Title = iTitle < f.Count && f[iTitle].Length > 0 ? f[iTitle] : "Deezer track " + id,
                    ISRC = isrc.Length > 0 ? isrc : "N/A",
                    TrackID = id,
                    TrackLink = "https://www.deezer.com/track/" + id,
                    Platform = "Deezer",
                    PlaylistName = playlistName
                });
            }
            return tracks;
        }
    }
}
