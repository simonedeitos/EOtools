using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PlaylistDownloader
{
    public partial class MainForm
    {
        private MenuStrip menuStrip;
        private StatusStrip statusStrip;
        private ToolStripStatusLabel lblStatusTotal;
        private ToolStripStatusLabel lblStatusFound;
        private ToolStripStatusLabel lblStatusNotFound;
        private FlowLayoutPanel pnlImport;
        private Button btnPasteLinks;
        private DownloadForm downloadForm;

        private void InitEnhancements()
        {
            BuildMenu();
            BuildImportPanel();
            BuildStatusStrip();
            ApplyPlaylistSectionVisibility();
            UpdateCounters();
        }

        private void BuildMenu()
        {
            menuStrip = new MenuStrip();
            var mnuImport = new ToolStripMenuItem("Importazioni");
            mnuImport.Click += mnuImport_Click;
            var mnuArl = new ToolStripMenuItem("ARL / deemix");
            mnuArl.Click += mnuArl_Click;
            menuStrip.Items.Add(mnuImport);
            menuStrip.Items.Add(mnuArl);
            menuStrip.Dock = DockStyle.Top;

            Controls.Add(menuStrip);
            MainMenuStrip = menuStrip;
        }

        private void BuildStatusStrip()
        {
            statusStrip = new StatusStrip();
            lblStatusTotal = new ToolStripStatusLabel();
            lblStatusFound = new ToolStripStatusLabel();
            lblStatusNotFound = new ToolStripStatusLabel();
            lblStatusFound.ForeColor = Color.DarkGreen;
            lblStatusNotFound.ForeColor = Color.Firebrick;
            lblStatusTotal.BorderSides = lblStatusFound.BorderSides = ToolStripStatusLabelBorderSides.Right;
            statusStrip.Items.AddRange(new ToolStripItem[] { lblStatusTotal, lblStatusFound, lblStatusNotFound });
            statusStrip.Dock = DockStyle.Bottom;
            Controls.Add(statusStrip);
        }

        // Import buttons live above the results grid so they stay visible when the playlist section is hidden
        private void BuildImportPanel()
        {
            pnlImport = new FlowLayoutPanel();
            pnlImport.Dock = DockStyle.Top;
            pnlImport.AutoSize = true;
            pnlImport.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            pnlImport.Padding = new Padding(10, 6, 10, 4);

            btnPasteLinks = new Button();
            btnPasteLinks.Text = "Incolla link Deezer";
            btnPasteLinks.UseVisualStyleBackColor = true;
            btnPasteLinks.Click += btnPasteLinks_Click;

            panel1.Controls.Remove(btnImportCSV);
            panel1.Controls.Remove(btnImportISRC);

            foreach (Button b in new[] { btnImportCSV, btnImportISRC, btnPasteLinks })
            {
                b.Size = new Size(150, 27);
                b.Margin = new Padding(3, 3, 8, 3);
                pnlImport.Controls.Add(b);
            }

            dgvResults.Dock = DockStyle.Fill;
            splitContainer1.Panel2.Controls.Add(pnlImport);
            // Docking is evaluated from the last control to the first: grid (fill) last, import bar topmost
            splitContainer1.Panel2.Controls.SetChildIndex(dgvResults, 0);
            splitContainer1.Panel2.Controls.SetChildIndex(panel2, 1);
            splitContainer1.Panel2.Controls.SetChildIndex(pnlImport, 2);
        }

        private void ApplyPlaylistSectionVisibility()
        {
            bool show = AppSettings.ShowPlaylistSection;
            panel1.Visible = show;
            splitContainer1.Panel1Collapsed = !show;
        }

        private void ApplyColumnDefaults(DataGridView grid, Dictionary<string, int> widths)
        {
            grid.AllowUserToResizeColumns = true;
            foreach (DataGridViewColumn col in grid.Columns)
            {
                col.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                col.Resizable = DataGridViewTriState.True;
                int w;
                if (widths.TryGetValue(col.Name, out w))
                    col.Width = w;
            }
        }

        private static bool IsDeezerId(string id)
        {
            return !string.IsNullOrEmpty(id) && id.All(c => c >= '0' && c <= '9');
        }

        private void UpdateCounters()
        {
            if (lblStatusTotal == null || resultsTable == null)
                return;

            int idCol = resultsTable.Columns["Track ID"].Ordinal;
            int found = 0, notFound = 0;
            foreach (DataRow row in resultsTable.Rows)
            {
                string id = row[idCol] as string;
                if (id == "Not found") notFound++;
                else if (IsDeezerId(id)) found++;
            }

            lblStatusTotal.Text = $"Totale brani: {resultsTable.Rows.Count}";
            lblStatusFound.Text = $"Trovati su Deezer: {found}";
            lblStatusNotFound.Text = $"Not found: {notFound}";
        }

        private void AddTracksToResults(IEnumerable<TrackInfo> tracks)
        {
            resultsTable.BeginLoadData();
            try
            {
                foreach (var track in tracks)
                {
                    resultsTable.Rows.Add(
                        track.Platform,
                        track.PlaylistName,
                        track.Artist,
                        track.Title,
                        track.ISRC,
                        track.TrackID,
                        track.TrackLink);
                }
            }
            finally
            {
                resultsTable.EndLoadData();
            }
        }

        // Resolves the Deezer track id of every track still flagged "Searching..." using parallel lookups
        private async Task<int> ResolveTrackIdsAsync(List<TrackInfo> tracks, string label, bool fillMetadata, bool updateMainBar = true)
        {
            var pending = tracks.Where(t => t.TrackID == "Searching..." && !string.IsNullOrWhiteSpace(t.ISRC) && t.ISRC != "N/A").ToList();
            var isrcs = pending.Select(t => t.ISRC.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            int total = isrcs.Count;

            if (total > 0)
            {
                progressBarTrack.Maximum = total;
                progressBarTrack.Value = 0;
                if (updateMainBar)
                {
                    progressBarMain.Maximum = total;
                    progressBarMain.Value = 0;
                }
                lblProgress.Text = $"{label}: 0/{total}";

                var progress = new Progress<int>(n =>
                {
                    int v = Math.Min(n, total);
                    progressBarTrack.Value = v;
                    if (updateMainBar) progressBarMain.Value = v;
                    lblProgress.Text = $"{label}: {v}/{total}";
                });

                var service = new DeezerLookupService(httpClient);
                var results = await service.LookupAsync(isrcs, AppSettings.LookupThreads, AppSettings.LookupRatePer5s, progress, CancellationToken.None);

                foreach (var track in pending)
                {
                    DeezerTrackResult r;
                    if (results.TryGetValue(track.ISRC.Trim(), out r) && r.Found)
                    {
                        track.TrackID = r.TrackId;
                        track.TrackLink = string.IsNullOrEmpty(r.Link) ? $"https://www.deezer.com/track/{r.TrackId}" : r.Link;
                        if (fillMetadata)
                        {
                            track.Artist = string.IsNullOrEmpty(r.Artist) ? "Unknown Artist" : r.Artist;
                            track.Title = string.IsNullOrEmpty(r.Title) ? "Unknown Title" : r.Title;
                        }
                    }
                    else
                    {
                        track.TrackID = "Not found";
                    }
                }
            }

            return tracks.Count(t => IsDeezerId(t.TrackID));
        }

        private void mnuImport_Click(object sender, EventArgs e)
        {
            using (var form = new ImportSettingsForm())
            {
                if (form.ShowDialog(this) == DialogResult.OK)
                    ApplyPlaylistSectionVisibility();
            }
        }

        private void mnuArl_Click(object sender, EventArgs e)
        {
            using (var form = new ArlManagerForm())
            {
                form.ShowDialog(this);
            }
        }

        private void btnPasteLinks_Click(object sender, EventArgs e)
        {
            using (var form = new PasteLinksForm())
            {
                if (form.ShowDialog(this) != DialogResult.OK)
                    return;

                if (form.ClearExisting)
                    resultsTable.Clear();

                int idCol = resultsTable.Columns["Track ID"].Ordinal;
                var existing = new HashSet<string>();
                foreach (DataRow row in resultsTable.Rows)
                    existing.Add(row[idCol] as string ?? "");

                var tracks = new List<TrackInfo>();
                int duplicates = 0;
                foreach (string id in form.TrackIds)
                {
                    if (!existing.Add(id)) { duplicates++; continue; }
                    tracks.Add(new TrackInfo
                    {
                        Artist = "N/A",
                        Title = "Deezer track " + id,
                        ISRC = "N/A",
                        TrackID = id,
                        TrackLink = "https://www.deezer.com/track/" + id,
                        Platform = "Deezer",
                        PlaylistName = "Imported Links"
                    });
                }

                AddTracksToResults(tracks);
                UpdateResultCount();
                UpdateButtonStates();

                MessageBox.Show(
                    $"Importati {tracks.Count} brani (senza interrogare Deezer).\nDuplicati ignorati: {duplicates}\nRighe non valide: {form.InvalidLines}",
                    "Import link", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private List<DownloadJob> BuildDownloadJobs(DownloadOptions options)
        {
            var byPlaylist = new Dictionary<string, List<TrackInfo>>();
            foreach (DataRow row in resultsTable.Rows)
            {
                string trackId = row["Track ID"].ToString();
                if (!IsDeezerId(trackId))
                    continue;

                var track = new TrackInfo
                {
                    Artist = row["Artist"].ToString(),
                    Title = row["Title"].ToString(),
                    TrackID = trackId,
                    PlaylistName = row["Playlist Name"].ToString(),
                    Platform = row["Platform"].ToString(),
                    ISRC = row["ISRC"].ToString()
                };

                string folderName = SanitizeFolderName(track.PlaylistName);
                List<TrackInfo> list;
                if (!byPlaylist.TryGetValue(folderName, out list))
                    byPlaylist[folderName] = list = new List<TrackInfo>();
                list.Add(track);
            }

            var jobs = new List<DownloadJob>();
            var seen = new HashSet<string>();
            foreach (var track in byPlaylist.Values.SelectMany(l => l))
            {
                string folder = options.UseSubfolders
                    ? System.IO.Path.Combine(options.DownloadPath, SanitizeFolderName(track.PlaylistName))
                    : options.DownloadPath;

                var job = new DownloadJob
                {
                    Index = jobs.Count + 1,
                    Label = $"{track.Artist} - {track.Title}",
                    Url = "https://www.deezer.com/track/" + track.TrackID,
                    Folder = folder
                };

                // Same duplicate rule used by the .bat generator
                string key = $"{track.Artist}_{track.Title}".ToLowerInvariant();
                bool isDuplicate = !seen.Add(key);
                if (isDuplicate && !(options.SaveDuplicatesWithISRC && !string.IsNullOrWhiteSpace(track.ISRC) && track.ISRC != "N/A"))
                {
                    job.Status = DownloadJob.Skipped;
                    job.Detail = "Duplicato";
                }
                jobs.Add(job);
            }
            return jobs;
        }

        private void OpenDownloadWindow(DownloadOptions options)
        {
            if (downloadForm != null && !downloadForm.IsDisposed)
            {
                MessageBox.Show("La finestra download è già aperta.", "Download", MessageBoxButtons.OK, MessageBoxIcon.Information);
                downloadForm.Activate();
                return;
            }

            var jobs = BuildDownloadJobs(options);
            if (jobs.Count == 0)
            {
                MessageBox.Show("No valid Deezer tracks found to download.", "No Tracks", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            downloadForm = new DownloadForm(jobs, options);
            downloadForm.Show(this);
        }
    }
}
