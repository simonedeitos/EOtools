using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Win32;
using System.Xml;
using System.Xml.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace PlaylistDownloader
{
    public partial class MainForm : Form
    {
        private DataTable playlistTable;
        private DataTable resultsTable;
        private HttpClient httpClient;

        // Registry keys for saving settings
        private const string REG_KEY_PATH = @"SOFTWARE\PlaylistDownloader";
        private const string REG_PLAYLISTS_KEY = "Playlists";
        private const string REG_COLUMN_WIDTH_PREFIX = "ColumnWidth_";

        // Make.com webhook URL
        private const string MAKE_SPOTIFY_WEBHOOK_URL = "https://hook.eu1.make.com/cxpk95bpw3iiiunnaag1geje8kzppgog";

        // Track processing status
        private int processedTracks = 0;
        private int totalTracks = 0;

        public MainForm()
        {
            InitializeComponent();
            InitializeTables();
            httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromMinutes(5); // Increased timeout for API calls

            this.Load += MainForm_Load;
            this.FormClosing += MainForm_FormClosing;

            // Set up event handlers for grid formatting
            dgvPlaylists.CellFormatting += DgvPlaylists_CellFormatting;
            dgvResults.CellFormatting += DgvResults_CellFormatting;
        }

        private void InitializeTables()
        {
            // Setup playlists table
            playlistTable = new DataTable();
            playlistTable.Columns.Add("Selected", typeof(bool));
            playlistTable.Columns.Add("ID", typeof(int));
            playlistTable.Columns.Add("Platform", typeof(string));
            playlistTable.Columns.Add("Playlist URL", typeof(string));
            playlistTable.Columns.Add("Name", typeof(string));
            playlistTable.Columns.Add("Status", typeof(string));

            // Set default value for Selected column
            playlistTable.Columns["Selected"].DefaultValue = true;

            dgvPlaylists.DataSource = playlistTable;
            dgvPlaylists.RowHeadersVisible = false;

            // Setup results table - add Platform and Playlist Name columns
            resultsTable = new DataTable();
            resultsTable.Columns.Add("Platform", typeof(string));
            resultsTable.Columns.Add("Playlist Name", typeof(string));
            resultsTable.Columns.Add("Artist", typeof(string));
            resultsTable.Columns.Add("Title", typeof(string));
            resultsTable.Columns.Add("ISRC", typeof(string));
            resultsTable.Columns.Add("Track ID", typeof(string));
            resultsTable.Columns.Add("Track Link", typeof(string));
            dgvResults.DataSource = resultsTable;
            dgvResults.Columns["Track Link"].Visible = false;
            dgvResults.RowHeadersVisible = false;

            // Set auto-size modes for better appearance
            foreach (DataGridViewColumn col in dgvPlaylists.Columns)
            {
                col.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            }

            foreach (DataGridViewColumn col in dgvResults.Columns)
            {
                col.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            }

            // Convert the "Selected" column to a checkbox
            if (dgvPlaylists.Columns["Selected"] is DataGridViewCheckBoxColumn == false)
            {
                DataGridViewCheckBoxColumn checkBoxColumn = new DataGridViewCheckBoxColumn();
                checkBoxColumn.HeaderText = "Select";
                checkBoxColumn.DataPropertyName = "Selected";
                checkBoxColumn.Name = "Selected";
                checkBoxColumn.Width = 50;
                dgvPlaylists.Columns.Remove("Selected");
                dgvPlaylists.Columns.Insert(0, checkBoxColumn);
            }

            UpdateResultCount();
        }

        private void UpdateResultCount()
        {
            int count = resultsTable != null ? resultsTable.Rows.Count : 0;
            lblResults.Text = $"Results ({count} tracks)";

            // Force UI update
            lblResults.Update();
            dgvResults.Update();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            LoadColumnWidths();
            LoadSavedPlaylists();

            // Set initial button states
            UpdateButtonStates();
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            SaveColumnWidths();
            SavePlaylistsToRegistry();
        }

        private void UpdateButtonStates()
        {
            bool hasPlaylists = playlistTable.Rows.Count > 0;
            bool hasResults = resultsTable.Rows.Count > 0;
            bool isProcessing = progressBarMain.Visible;

            btnSearch.Enabled = hasPlaylists && !isProcessing;
            btnRemovePlaylist.Enabled = hasPlaylists && !isProcessing && dgvPlaylists.SelectedRows.Count > 0;
            btnExportCsv.Enabled = hasResults && !isProcessing;
            btnDownload.Enabled = hasResults && !isProcessing;
            btnExportPlaylists.Enabled = hasPlaylists && !isProcessing;
            btnImportPlaylists.Enabled = !isProcessing;
            btnAddPlaylist.Enabled = !isProcessing;
            btnImportCSV.Enabled = !isProcessing;
            btnImportISRC.Enabled = !isProcessing; // Add this line
        }

        #region UI Formatting

        private void DgvPlaylists_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex >= 0 && e.ColumnIndex == dgvPlaylists.Columns["Status"].Index)
            {
                string status = e.Value?.ToString();
                if (status == "Completed")
                {
                    e.CellStyle.BackColor = Color.LightGreen;
                    e.CellStyle.SelectionBackColor = Color.MediumSeaGreen;
                }
                else if (status == "Processing...")
                {
                    e.CellStyle.BackColor = Color.LightYellow;
                    e.CellStyle.SelectionBackColor = Color.Khaki;
                }
                else if (status == "Error")
                {
                    e.CellStyle.BackColor = Color.LightPink;
                    e.CellStyle.SelectionBackColor = Color.LightCoral;
                }
            }
            else if (e.RowIndex >= 0 && e.ColumnIndex == dgvPlaylists.Columns["Platform"].Index)
            {
                string platform = e.Value?.ToString();
                if (platform == "Deezer")
                {
                    e.CellStyle.BackColor = Color.LightBlue;
                    e.CellStyle.SelectionBackColor = Color.SkyBlue;
                }
                else if (platform == "Spotify")
                {
                    e.CellStyle.BackColor = Color.LightGreen;
                    e.CellStyle.SelectionBackColor = Color.MediumSeaGreen;
                }
            }
        }

        private void DgvResults_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            // Format Track ID column
            if (e.RowIndex >= 0 && e.ColumnIndex == dgvResults.Columns["Track ID"].Index)
            {
                string trackId = e.Value?.ToString();
                if (trackId == "Searching..." || trackId == "Pending")
                {
                    e.CellStyle.BackColor = Color.LightYellow;
                    e.CellStyle.SelectionBackColor = Color.Khaki;
                }
                else if (trackId == "Not found")
                {
                    e.CellStyle.BackColor = Color.LightPink;
                    e.CellStyle.SelectionBackColor = Color.LightCoral;
                }
                else if (trackId != "N/A")
                {
                    e.CellStyle.BackColor = Color.LightGreen;
                    e.CellStyle.SelectionBackColor = Color.MediumSeaGreen;
                }
            }

            // Format Platform column
            if (e.RowIndex >= 0 && e.ColumnIndex == dgvResults.Columns["Platform"].Index)
            {
                string platform = e.Value?.ToString();
                if (platform == "Deezer")
                {
                    e.CellStyle.BackColor = Color.LightBlue;
                    e.CellStyle.SelectionBackColor = Color.SkyBlue;
                }
                else if (platform == "Spotify")
                {
                    e.CellStyle.BackColor = Color.LightGreen;
                    e.CellStyle.SelectionBackColor = Color.MediumSeaGreen;
                }
                else if (platform == "CSV")
                {
                    e.CellStyle.BackColor = Color.LavenderBlush;
                    e.CellStyle.SelectionBackColor = Color.Plum;
                }
            }
        }

        #endregion

        #region Registry Saving/Loading

        private void LoadColumnWidths()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(REG_KEY_PATH))
                {
                    if (key != null)
                    {
                        for (int i = 0; i < dgvPlaylists.Columns.Count; i++)
                        {
                            string valueName = $"{REG_COLUMN_WIDTH_PREFIX}Playlists_{i}";
                            object value = key.GetValue(valueName);
                            if (value != null)
                            {
                                dgvPlaylists.Columns[i].Width = Convert.ToInt32(value);
                            }
                        }

                        for (int i = 0; i < dgvResults.Columns.Count; i++)
                        {
                            string valueName = $"{REG_COLUMN_WIDTH_PREFIX}Results_{i}";
                            object value = key.GetValue(valueName);
                            if (value != null)
                            {
                                dgvResults.Columns[i].Width = Convert.ToInt32(value);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading column widths: {ex.Message}");
            }
        }

        private void SaveColumnWidths()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.CreateSubKey(REG_KEY_PATH))
                {
                    if (key != null)
                    {
                        for (int i = 0; i < dgvPlaylists.Columns.Count; i++)
                        {
                            string valueName = $"{REG_COLUMN_WIDTH_PREFIX}Playlists_{i}";
                            key.SetValue(valueName, dgvPlaylists.Columns[i].Width, RegistryValueKind.DWord);
                        }

                        for (int i = 0; i < dgvResults.Columns.Count; i++)
                        {
                            string valueName = $"{REG_COLUMN_WIDTH_PREFIX}Results_{i}";
                            key.SetValue(valueName, dgvResults.Columns[i].Width, RegistryValueKind.DWord);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving column widths: {ex.Message}");
            }
        }

        private void LoadSavedPlaylists()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(REG_KEY_PATH))
                {
                    if (key != null)
                    {
                        string playlistsXml = key.GetValue(REG_PLAYLISTS_KEY) as string;
                        if (!string.IsNullOrEmpty(playlistsXml))
                        {
                            PlaylistInfoList playlists = DeserializeFromXml<PlaylistInfoList>(playlistsXml);

                            if (playlists != null && playlists.Playlists.Count > 0)
                            {
                                playlistTable.Clear();
                                int id = 1;

                                foreach (var playlist in playlists.Playlists)
                                {
                                    playlistTable.Rows.Add(true, id++, playlist.Platform, playlist.Url, playlist.Name, "Ready");
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading playlists: {ex.Message}");
            }
        }

        private void SavePlaylistsToRegistry()
        {
            try
            {
                PlaylistInfoList playlists = new PlaylistInfoList();

                foreach (DataRow row in playlistTable.Rows)
                {
                    playlists.Playlists.Add(new PlaylistInfo
                    {
                        Platform = row["Platform"].ToString(),
                        Url = row["Playlist URL"].ToString(),
                        Name = row["Name"].ToString()
                    });
                }

                string playlistsXml = SerializeToXml(playlists);

                using (RegistryKey key = Registry.CurrentUser.CreateSubKey(REG_KEY_PATH))
                {
                    if (key != null)
                    {
                        key.SetValue(REG_PLAYLISTS_KEY, playlistsXml, RegistryValueKind.String);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving playlists: {ex.Message}");
            }
        }

        private string SerializeToXml<T>(T obj)
        {
            if (obj == null)
                return string.Empty;

            try
            {
                XmlSerializer serializer = new XmlSerializer(typeof(T));
                using (StringWriter writer = new StringWriter())
                {
                    serializer.Serialize(writer, obj);
                    return writer.ToString();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Serialization error: {ex.Message}");
                return string.Empty;
            }
        }

        private T DeserializeFromXml<T>(string xml) where T : new()
        {
            if (string.IsNullOrEmpty(xml))
                return new T();

            try
            {
                XmlSerializer serializer = new XmlSerializer(typeof(T));
                using (StringReader reader = new StringReader(xml))
                {
                    return (T)serializer.Deserialize(reader);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Deserialization error: {ex.Message}");
                return new T();
            }
        }

        #endregion

        #region Button Event Handlers

        private void btnAddPlaylist_Click(object sender, EventArgs e)
        {
            // Show add playlist dialog
            ShowAddPlaylistDialog();
        }

        private void ShowAddPlaylistDialog()
        {
            // Create a dialog to add playlists
            Form addDialog = new Form();
            addDialog.Text = "Add Playlist";
            addDialog.Size = new Size(500, 200);
            addDialog.StartPosition = FormStartPosition.CenterParent;
            addDialog.FormBorderStyle = FormBorderStyle.FixedDialog;
            addDialog.MaximizeBox = false;
            addDialog.MinimizeBox = false;

            Label lblUrl = new Label();
            lblUrl.Text = "Playlist URL:";
            lblUrl.Location = new Point(20, 20);
            lblUrl.Size = new Size(100, 20);

            TextBox txtUrl = new TextBox();
            txtUrl.Location = new Point(120, 20);
            txtUrl.Size = new Size(340, 20);

            Label lblName = new Label();
            lblName.Text = "Playlist Name:";
            lblName.Location = new Point(20, 50);
            lblName.Size = new Size(100, 20);

            TextBox txtName = new TextBox();
            txtName.Location = new Point(120, 50);
            txtName.Size = new Size(340, 20);

            Button btnOk = new Button();
            btnOk.Text = "OK";
            btnOk.DialogResult = DialogResult.OK;
            btnOk.Location = new Point(290, 100);
            btnOk.Size = new Size(75, 30);

            Button btnCancel = new Button();
            btnCancel.Text = "Cancel";
            btnCancel.DialogResult = DialogResult.Cancel;
            btnCancel.Location = new Point(380, 100);
            btnCancel.Size = new Size(75, 30);

            addDialog.Controls.Add(lblUrl);
            addDialog.Controls.Add(txtUrl);
            addDialog.Controls.Add(lblName);
            addDialog.Controls.Add(txtName);
            addDialog.Controls.Add(btnOk);
            addDialog.Controls.Add(btnCancel);

            addDialog.AcceptButton = btnOk;
            addDialog.CancelButton = btnCancel;

            // Validate input before accepting
            btnOk.Click += (sender, e) => {
                if (string.IsNullOrWhiteSpace(txtUrl.Text))
                {
                    MessageBox.Show("Please enter a playlist URL", "Missing URL", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    e = null;
                    return;
                }

                if (string.IsNullOrWhiteSpace(txtName.Text))
                {
                    MessageBox.Show("Please enter a playlist name", "Missing Name", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    e = null;
                    return;
                }

                // Check if URL is valid (contains deezer or spotify)
                if (!txtUrl.Text.Contains("deezer.com") && !txtUrl.Text.Contains("spotify.com"))
                {
                    MessageBox.Show("Please enter a valid Deezer or Spotify playlist URL", "Invalid URL",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    e = null;
                    return;
                }
            };

            if (addDialog.ShowDialog() == DialogResult.OK)
            {
                string url = txtUrl.Text.Trim();
                string name = txtName.Text.Trim();

                int id = playlistTable.Rows.Count + 1;
                // Platform is determined by the URL (Deezer or Spotify)
                string platform = url.Contains("deezer") ? "Deezer" :
                               url.Contains("spotify") ? "Spotify" : "Unknown";

                playlistTable.Rows.Add(true, id, platform, url, name, "Ready");
                SavePlaylistsToRegistry();
                UpdateButtonStates();
            }
        }

        private void btnRemovePlaylist_Click(object sender, EventArgs e)
        {
            if (dgvPlaylists.SelectedRows.Count > 0)
            {
                DataRowView rowView = dgvPlaylists.SelectedRows[0].DataBoundItem as DataRowView;
                if (rowView != null)
                {
                    DataRow row = rowView.Row;
                    playlistTable.Rows.Remove(row);
                    SavePlaylistsToRegistry();

                    for (int i = 0; i < playlistTable.Rows.Count; i++)
                    {
                        playlistTable.Rows[i]["ID"] = i + 1;
                    }
                }
                UpdateButtonStates();
            }
            else
            {
                MessageBox.Show("Please select a playlist to remove.", "No Selection", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private async void btnSearch_Click(object sender, EventArgs e)
        {
            if (playlistTable.Rows.Count == 0)
            {
                MessageBox.Show("Please add at least one playlist before searching.", "No Playlists", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Count selected playlists
            int selectedCount = 0;
            foreach (DataRow row in playlistTable.Rows)
            {
                if ((bool)row["Selected"])
                {
                    selectedCount++;
                }
            }

            if (selectedCount == 0)
            {
                MessageBox.Show("Please select at least one playlist to search.", "No Playlists Selected", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Clear results for a new search
            if (!chkAppendResults.Checked)
            {
                resultsTable.Clear();
                UpdateResultCount();
            }

            // Update UI for processing
            SetProcessingUI(true);

            progressBarMain.Value = 0;
            progressBarMain.Maximum = selectedCount;

            try
            {
                int processedPlaylists = 0;

                foreach (DataRow row in playlistTable.Rows)
                {
                    // Skip if not selected
                    if (!(bool)row["Selected"])
                        continue;

                    string playlistUrl = row["Playlist URL"].ToString();
                    string platform = row["Platform"].ToString();
                    string playlistName = row["Name"].ToString();
                    row["Status"] = "Processing...";
                    Application.DoEvents();

                    List<TrackInfo> tracks = new List<TrackInfo>();

                    try
                    {
                        if (platform == "Deezer")
                        {
                            tracks = await ExtractFromDeezer(playlistUrl, platform, playlistName);
                        }
                        else if (platform == "Spotify")
                        {
                            tracks = await ExtractFromSpotify(playlistUrl, platform, playlistName);
                        }
                        else
                        {
                            row["Status"] = "Error";
                            MessageBox.Show($"Unsupported platform: {platform}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            continue;
                        }

                        foreach (var track in tracks)
                        {
                            resultsTable.Rows.Add(
                                track.Platform,
                                track.PlaylistName,
                                track.Artist,
                                track.Title,
                                track.ISRC,
                                track.TrackID,
                                track.TrackLink
                            );
                        }

                        row["Status"] = "Completed";
                    }
                    catch (Exception ex)
                    {
                        row["Status"] = "Error";
                        MessageBox.Show($"Error processing {platform} playlist: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }

                    processedPlaylists++;
                    progressBarMain.Value = processedPlaylists;
                    Application.DoEvents();
                }

                UpdateResultCount();

                if (resultsTable.Rows.Count > 0)
                {
                    MessageBox.Show($"Found {resultsTable.Rows.Count} tracks across all playlists.", "Search Completed", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show("No tracks found in the provided playlists.", "Search Completed", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error during search: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                // Reset UI
                SetProcessingUI(false);
            }
        }

        private void SetProcessingUI(bool isProcessing)
        {
            btnSearch.Enabled = !isProcessing;
            btnAddPlaylist.Enabled = !isProcessing;
            btnExportCsv.Enabled = !isProcessing && resultsTable.Rows.Count > 0;
            btnExportPlaylists.Enabled = !isProcessing;
            btnImportPlaylists.Enabled = !isProcessing;
            btnRemovePlaylist.Enabled = !isProcessing;
            btnDownload.Enabled = !isProcessing && resultsTable.Rows.Count > 0;
            btnImportCSV.Enabled = !isProcessing;
            progressBarMain.Visible = isProcessing;
            progressBarTrack.Visible = isProcessing;
            lblProgress.Visible = isProcessing;
        }

        private void btnExportCsv_Click(object sender, EventArgs e)
        {
            if (resultsTable.Rows.Count == 0)
            {
                MessageBox.Show("No data to export.", "Export Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using (SaveFileDialog saveDialog = new SaveFileDialog())
            {
                saveDialog.Filter = "CSV files (*.csv)|*.csv";
                saveDialog.Title = "Export Track Data";
                saveDialog.FileName = "track_data_" + DateTime.Now.ToString("yyyyMMdd_HHmmss");

                if (saveDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        StringBuilder csv = new StringBuilder();

                        csv.AppendLine("Platform;Playlist Name;Artist;Title;ISRC;Track ID;Track Link");

                        foreach (DataRow row in resultsTable.Rows)
                        {
                            string platform = EscapeCsvField(row["Platform"].ToString());
                            string playlistName = EscapeCsvField(row["Playlist Name"].ToString());
                            string artist = EscapeCsvField(row["Artist"].ToString());
                            string title = EscapeCsvField(row["Title"].ToString());
                            string isrc = EscapeCsvField(row["ISRC"].ToString());
                            string trackId = EscapeCsvField(row["Track ID"].ToString());
                            string trackLink = EscapeCsvField(row["Track Link"].ToString());

                            csv.AppendLine($"{platform};{playlistName};{artist};{title};{isrc};{trackId};{trackLink}");
                        }

                        File.WriteAllText(saveDialog.FileName, csv.ToString());

                        MessageBox.Show("CSV file saved successfully!", "Export Completed", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error exporting CSV: {ex.Message}", "Export Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private string EscapeCsvField(string field)
        {
            if (field.Contains(",") || field.Contains("\"") || field.Contains("\n"))
            {
                return "\"" + field.Replace("\"", "\"\"") + "\"";
            }
            return field;
        }

        private void btnExportPlaylists_Click(object sender, EventArgs e)
        {
            if (playlistTable.Rows.Count == 0)
            {
                MessageBox.Show("No playlists to export.", "Export Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using (SaveFileDialog saveDialog = new SaveFileDialog())
            {
                saveDialog.Filter = "CSV files (*.csv)|*.csv";
                saveDialog.Title = "Export Playlists";
                saveDialog.FileName = "playlists_" + DateTime.Now.ToString("yyyyMMdd_HHmmss");

                if (saveDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        StringBuilder csv = new StringBuilder();

                        csv.AppendLine("Platform,Name,Playlist URL");

                        foreach (DataRow row in playlistTable.Rows)
                        {
                            string platform = EscapeCsvField(row["Platform"].ToString());
                            string name = EscapeCsvField(row["Name"].ToString());
                            string url = EscapeCsvField(row["Playlist URL"].ToString());

                            csv.AppendLine($"{platform},{name},{url}");
                        }

                        File.WriteAllText(saveDialog.FileName, csv.ToString());

                        MessageBox.Show("Playlists exported successfully!", "Export Completed", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error during export: {ex.Message}", "Export Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void btnImportPlaylists_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openDialog = new OpenFileDialog())
            {
                openDialog.Filter = "CSV files (*.csv)|*.csv";
                openDialog.Title = "Import Playlists";

                if (openDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        int importedCount = 0;
                        int nextId = playlistTable.Rows.Count + 1;

                        using (StreamReader reader = new StreamReader(openDialog.FileName))
                        {
                            string headerLine = reader.ReadLine();

                            while (!reader.EndOfStream)
                            {
                                string line = reader.ReadLine();
                                string[] values = ParseCsvLine(line);

                                if (values.Length >= 3)
                                {
                                    string platform = values[0];
                                    string name = values[1];
                                    string url = values[2];

                                    if (!string.IsNullOrWhiteSpace(url) && !string.IsNullOrWhiteSpace(name))
                                    {
                                        // Auto-detect platform if not specified
                                        if (string.IsNullOrWhiteSpace(platform))
                                        {
                                            platform = url.Contains("deezer") ? "Deezer" :
                                                      url.Contains("spotify") ? "Spotify" : "Unknown";
                                        }

                                        playlistTable.Rows.Add(true, nextId++, platform, url, name, "Ready");
                                        importedCount++;
                                    }
                                }
                            }
                        }

                        if (importedCount > 0)
                        {
                            SavePlaylistsToRegistry();
                            MessageBox.Show($"Successfully imported {importedCount} playlists.", "Import Completed", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                        else
                        {
                            MessageBox.Show("No valid playlists found in the CSV file.", "Import", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }

                        UpdateButtonStates();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error during import: {ex.Message}", "Import Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private string[] ParseCsvLine(string line)
        {
            List<string> values = new List<string>();
            bool inQuotes = false;
            StringBuilder field = new StringBuilder();

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];

                if (c == '"')
                {
                    if (i + 1 < line.Length && line[i + 1] == '"')
                    {
                        field.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }
                }
                else if (c == ',' && !inQuotes)
                {
                    values.Add(field.ToString());
                    field.Clear();
                }
                else
                {
                    field.Append(c);
                }
            }

            values.Add(field.ToString());

            return values.ToArray();
        }

        private void btnDownload_Click(object sender, EventArgs e)
        {
            if (resultsTable.Rows.Count == 0)
            {
                MessageBox.Show("No tracks to download.", "No Tracks", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Mostra il form delle opzioni di download
            DownloadOptionsForm optionsForm = new DownloadOptionsForm();
            if (optionsForm.ShowDialog() == DialogResult.OK)
            {
                var options = optionsForm.DownloadOptions;
                GenerateDownloadBatchFile(options);
            }
        }

        private async void btnImportCSV_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openDialog = new OpenFileDialog())
            {
                openDialog.Filter = "CSV files (*.csv)|*.csv";
                openDialog.Title = "Import Track List from CSV";

                if (openDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        // Ask if user wants to clear existing results
                        DialogResult clearResult = MessageBox.Show(
                            "Do you want to clear existing results before importing?",
                            "Clear Results",
                            MessageBoxButtons.YesNoCancel,
                            MessageBoxIcon.Question);

                        if (clearResult == DialogResult.Cancel)
                            return;

                        if (clearResult == DialogResult.Yes)
                        {
                            resultsTable.Clear();
                            UpdateResultCount();
                        }

                        // Update UI for processing
                        SetProcessingUI(true);

                        List<TrackInfo> importedTracks = await ImportTracksFromCSV(openDialog.FileName);

                        // Add imported tracks to results
                        foreach (var track in importedTracks)
                        {
                            resultsTable.Rows.Add(
                                track.Platform,
                                track.PlaylistName,
                                track.Artist,
                                track.Title,
                                track.ISRC,
                                track.TrackID,
                                track.TrackLink
                            );
                        }

                        UpdateResultCount();

                        if (importedTracks.Count > 0)
                        {
                            MessageBox.Show($"Successfully imported {importedTracks.Count} tracks from CSV.",
                                "Import Completed", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                        else
                        {
                            MessageBox.Show("No valid tracks found in the CSV file.",
                                "Import", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error importing tracks from CSV: {ex.Message}",
                            "Import Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    finally
                    {
                        // Reset UI
                        SetProcessingUI(false);
                    }
                }
            }
        }

        #endregion

        #region Track Processing Methods

        private async Task<List<TrackInfo>> ImportTracksFromCSV(string filePath)
        {
            List<TrackInfo> tracks = new List<TrackInfo>();
            int validTracks = 0;
            int invalidTracks = 0;

            try
            {
                // Read all lines from the CSV file
                string[] allLines = File.ReadAllLines(filePath);

                if (allLines.Length == 0)
                {
                    MessageBox.Show("The CSV file is empty.", "Empty File", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return tracks;
                }

                // Detect separator by analyzing the first few lines
                char separator = DetectCsvSeparator(allLines);

                // Calculate total for progress bar (skip header if present)
                progressBarMain.Value = 0;
                int startLine = 0;

                // Check if first line is a header (contains "artist", "title", "isrc" etc.)
                if (allLines.Length > 0)
                {
                    string firstLine = allLines[0].ToLower();
                    if (firstLine.Contains("artist") || firstLine.Contains("title") || firstLine.Contains("isrc"))
                    {
                        startLine = 1; // Skip header
                    }
                }

                totalTracks = allLines.Length - startLine;
                progressBarMain.Maximum = totalTracks;
                progressBarTrack.Maximum = totalTracks;

                for (int i = startLine; i < allLines.Length; i++)
                {
                    string line = allLines[i];
                    if (string.IsNullOrWhiteSpace(line))
                        continue;

                    // Update progress bars
                    progressBarMain.Value = i - startLine + 1;
                    processedTracks = i - startLine + 1;
                    progressBarTrack.Value = processedTracks;
                    lblProgress.Text = $"Processing tracks: {processedTracks}/{totalTracks}";
                    Application.DoEvents();

                    // Parse the CSV line using detected separator
                    string[] parts = line.Split(separator);

                    // Check if we have enough parts (artist, title, ISRC)
                    if (parts.Length >= 3)
                    {
                        string artist = parts[0].Trim().Trim('"');
                        string title = parts[1].Trim().Trim('"');
                        string isrc = parts[2].Trim().Trim('"');

                        // Skip if ISRC is missing
                        if (string.IsNullOrWhiteSpace(isrc))
                        {
                            invalidTracks++;
                            continue;
                        }

                        // Create the track with Pending TrackID
                        TrackInfo track = new TrackInfo
                        {
                            Artist = artist,
                            Title = title,
                            ISRC = isrc,
                            TrackID = "Searching...",
                            TrackLink = "N/A",
                            Platform = "CSV",
                            PlaylistName = "Imported CSV"
                        };

                        // Lookup the Deezer Track ID using the ISRC
                        string trackId = await GetTrackIdFromISRC(isrc);

                        if (!string.IsNullOrEmpty(trackId))
                        {
                            track.TrackID = trackId;
                            track.TrackLink = $"https://www.deezer.com/track/{trackId}";
                            validTracks++;
                        }
                        else
                        {
                            track.TrackID = "Not found";
                            invalidTracks++;
                        }

                        tracks.Add(track);
                    }
                }

                // Show summary in the progress label
                lblProgress.Text = $"Imported: {validTracks} valid tracks, {invalidTracks} without track ID";

                return tracks;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error reading CSV file: {ex.Message}",
                    "CSV Import Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return new List<TrackInfo>();
            }
        }

        private char DetectCsvSeparator(string[] lines)
        {
            // Analyze first 5 lines (or less if file is smaller)
            int linesToCheck = Math.Min(5, lines.Length);
            int semicolonCount = 0;
            int commaCount = 0;

            for (int i = 0; i < linesToCheck; i++)
            {
                string line = lines[i];
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                // Count separators (ignoring those inside quotes)
                bool inQuotes = false;
                foreach (char c in line)
                {
                    if (c == '"')
                        inQuotes = !inQuotes;
                    else if (!inQuotes)
                    {
                        if (c == ';')
                            semicolonCount++;
                        else if (c == ',')
                            commaCount++;
                    }
                }
            }

            // Return the most common separator
            return semicolonCount > commaCount ? ';' : ',';
        }

        private async Task<List<TrackInfo>> ExtractFromDeezer(string playlistUrl, string platform, string playlistName)
        {
            List<TrackInfo> tracks = new List<TrackInfo>();

            try
            {
                string playlistId = ExtractDeezerPlaylistId(playlistUrl);
                if (string.IsNullOrEmpty(playlistId))
                {
                    throw new Exception("Invalid Deezer playlist URL format.");
                }

                string apiUrl = $"https://api.deezer.com/playlist/{playlistId}/tracks";

                HttpResponseMessage response = await httpClient.GetAsync(apiUrl);
                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception($"Deezer API Error: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}");
                }

                string jsonResponse = await response.Content.ReadAsStringAsync();
                List<TrackInfo> initialTracks = ParseDeezerResponse(jsonResponse, platform, playlistName);
                tracks.AddRange(initialTracks);

                // Set up track progress bar
                totalTracks = GetTotalTrackCount(jsonResponse);
                progressBarTrack.Maximum = totalTracks;
                processedTracks = tracks.Count;
                progressBarTrack.Value = processedTracks;
                lblProgress.Text = $"Processing tracks: {processedTracks}/{totalTracks}";
                Application.DoEvents();

                // Pagination handling
                int limit = 25; // Default Deezer API limit
                int index = limit;

                while (tracks.Count < totalTracks)
                {
                    string nextPageUrl = $"{apiUrl}?index={index}";
                    response = await httpClient.GetAsync(nextPageUrl);
                    if (!response.IsSuccessStatusCode)
                    {
                        break;
                    }

                    jsonResponse = await response.Content.ReadAsStringAsync();
                    List<TrackInfo> moreTracks = ParseDeezerResponse(jsonResponse, platform, playlistName);

                    if (moreTracks.Count == 0)
                        break;

                    tracks.AddRange(moreTracks);

                    // Update progress
                    processedTracks = tracks.Count;
                    progressBarTrack.Value = Math.Min(processedTracks, totalTracks);
                    lblProgress.Text = $"Processing tracks: {processedTracks}/{totalTracks}";
                    Application.DoEvents();

                    if (moreTracks.Count < limit)
                        break;

                    index += limit;
                }

                return RemoveDuplicateTracks(tracks);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error extracting tracks from Deezer: {ex.Message}",
                    "Deezer API", MessageBoxButtons.OK, MessageBoxIcon.Warning);

                return new List<TrackInfo>();
            }
        }

        private int GetTotalTrackCount(string jsonResponse)
        {
            try
            {
                Regex totalRegex = new Regex(@"""total"":\s*(\d+)", RegexOptions.Singleline);
                Match totalMatch = totalRegex.Match(jsonResponse);

                if (totalMatch.Success && int.TryParse(totalMatch.Groups[1].Value, out int total))
                {
                    return total;
                }

                return 0;
            }
            catch
            {
                return 0;
            }
        }

        private List<TrackInfo> ParseDeezerResponse(string jsonResponse, string platform, string playlistName)
        {
            List<TrackInfo> tracks = new List<TrackInfo>();

            try
            {
                Regex dataRegex = new Regex(@"""data"":\s*\[(.*?)\]", RegexOptions.Singleline);
                Match dataMatch = dataRegex.Match(jsonResponse);

                if (!dataMatch.Success)
                    return tracks;

                string dataContent = dataMatch.Groups[1].Value;
                string[] trackObjects = SplitJsonObjects(dataContent);

                foreach (string trackObject in trackObjects)
                {
                    string title = ExtractJsonValue(trackObject, "title");
                    string isrc = ExtractJsonValue(trackObject, "isrc");
                    string trackId = ExtractJsonValue(trackObject, "id");
                    string trackLink = ExtractJsonValue(trackObject, "link");

                    Regex artistRegex = new Regex(@"""artist"":\s*\{(.*?)\}", RegexOptions.Singleline);
                    Match artistMatch = artistRegex.Match(trackObject);
                    string artistName = "Unknown Artist";

                    if (artistMatch.Success)
                    {
                        string artistObject = artistMatch.Groups[1].Value;
                        artistName = ExtractJsonValue(artistObject, "name");
                    }

                    if (!string.IsNullOrEmpty(title) && !string.IsNullOrEmpty(artistName))
                    {
                        tracks.Add(new TrackInfo
                        {
                            Artist = artistName,
                            Title = title,
                            ISRC = string.IsNullOrEmpty(isrc) ? "N/A" : isrc,
                            TrackID = string.IsNullOrEmpty(trackId) ? "N/A" : trackId,
                            TrackLink = string.IsNullOrEmpty(trackLink) ? "N/A" : trackLink,
                            Platform = platform,
                            PlaylistName = playlistName
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error parsing JSON: {ex.Message}");
            }

            return tracks;
        }

        private string[] SplitJsonObjects(string jsonArray)
        {
            List<string> objects = new List<string>();
            int depth = 0;
            int startIndex = 0;
            bool inQuotes = false;

            for (int i = 0; i < jsonArray.Length; i++)
            {
                char c = jsonArray[i];

                if (c == '"' && (i == 0 || jsonArray[i - 1] != '\\'))
                {
                    inQuotes = !inQuotes;
                }

                if (!inQuotes)
                {
                    if (c == '{')
                    {
                        if (depth == 0)
                            startIndex = i;
                        depth++;
                    }
                    else if (c == '}')
                    {
                        depth--;
                        if (depth == 0)
                        {
                            objects.Add(jsonArray.Substring(startIndex, i - startIndex + 1));
                        }
                    }
                }
            }

            return objects.ToArray();
        }

        private string ExtractJsonValue(string jsonObject, string key)
        {
            string pattern = $@"""{key}"":\s*""([^""\\]*(?:\\.[^""\\]*)*)""|""{key}"":\s*(\d+)";
            Match match = Regex.Match(jsonObject, pattern);

            if (match.Success)
            {
                if (!string.IsNullOrEmpty(match.Groups[1].Value))
                    return match.Groups[1].Value.Replace("\\\"", "\"").Replace("\\\\", "\\");
                else
                    return match.Groups[2].Value;
            }

            return string.Empty;
        }

        private string ExtractDeezerPlaylistId(string url)
        {
            var match = Regex.Match(url, @"deezer\.com/(?:[a-z]{2}/)?playlist/(\d+)");
            return match.Success ? match.Groups[1].Value : string.Empty;
        }

        private async Task<List<TrackInfo>> ExtractFromSpotify(string playlistUrl, string platform, string playlistName)
        {
            List<TrackInfo> tracks = new List<TrackInfo>();

            try
            {
                string playlistId = ExtractSpotifyPlaylistId(playlistUrl);
                if (string.IsNullOrEmpty(playlistId))
                {
                    throw new Exception("Invalid Spotify playlist URL format.");
                }

                // Send playlist ID to Make.com webhook
                var content = new StringContent($"{{\"playlistId\":\"{playlistId}\"}}", Encoding.UTF8, "application/json");

                HttpResponseMessage response = await httpClient.PostAsync(MAKE_SPOTIFY_WEBHOOK_URL, content);
                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception($"Make.com webhook error: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}");
                }

                // Read the response body - contains all tracks separated by ^
                string responseContent = await response.Content.ReadAsStringAsync();
                if (string.IsNullOrWhiteSpace(responseContent))
                {
                    throw new Exception("No data received from Make.com webhook.");
                }

                // Split the response by ^ character to separate tracks
                string[] trackEntries = responseContent.Split(new[] { '^' }, StringSplitOptions.RemoveEmptyEntries);

                totalTracks = trackEntries.Length;
                if (totalTracks == 0)
                {
                    MessageBox.Show("No tracks found in the Spotify playlist.", "No Tracks", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return tracks;
                }

                progressBarTrack.Maximum = totalTracks;
                processedTracks = 0;

                foreach (string trackEntry in trackEntries)
                {
                    if (string.IsNullOrWhiteSpace(trackEntry))
                        continue;

                    // Parse track data in format: artist,title,isrc
                    string[] trackParts = trackEntry.Split(',');
                    if (trackParts.Length >= 2) // At least artist and title
                    {
                        string artist = trackParts[0].Trim();
                        string title = trackParts[1].Trim();
                        string isrc = trackParts.Length > 2 ? trackParts[2].Trim() : "N/A";

                        if (!string.IsNullOrWhiteSpace(artist) && !string.IsNullOrWhiteSpace(title))
                        {
                            // Add track to list
                            TrackInfo track = new TrackInfo
                            {
                                Artist = artist,
                                Title = title,
                                ISRC = string.IsNullOrWhiteSpace(isrc) ? "N/A" : isrc,
                                TrackID = "Searching...", // Will be updated later
                                TrackLink = "N/A",
                                Platform = platform,
                                PlaylistName = playlistName
                            };

                            tracks.Add(track);

                            // Lookup Deezer Track ID if we have an ISRC
                            if (!string.IsNullOrWhiteSpace(isrc) && isrc != "N/A")
                            {
                                string trackId = await GetTrackIdFromISRC(isrc);
                                int lastIndex = tracks.Count - 1;

                                if (!string.IsNullOrEmpty(trackId))
                                {
                                    tracks[lastIndex].TrackID = trackId;
                                    tracks[lastIndex].TrackLink = $"https://www.deezer.com/track/{trackId}";
                                }
                                else
                                {
                                    tracks[lastIndex].TrackID = "Not found";
                                }
                            }
                            else
                            {
                                tracks[tracks.Count - 1].TrackID = "N/A";
                            }

                            // Update progress
                            processedTracks++;
                            progressBarTrack.Value = processedTracks;
                            lblProgress.Text = $"Processing tracks: {processedTracks}/{totalTracks}";
                            Application.DoEvents();
                        }
                    }
                }

                return RemoveDuplicateTracks(tracks);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error extracting tracks from Spotify: {ex.Message}",
                    "Spotify Extraction", MessageBoxButtons.OK, MessageBoxIcon.Warning);

                return new List<TrackInfo>();
            }
        }

        private string ExtractSpotifyPlaylistId(string url)
        {
            var match = Regex.Match(url, @"spotify\.com/(?:[a-z]{2}/)?playlist/([a-zA-Z0-9]+)");
            return match.Success ? match.Groups[1].Value : string.Empty;
        }

        private async Task<string> GetTrackIdFromISRC(string isrc)
        {
            try
            {
                string apiUrl = $"https://api.deezer.com/track/isrc:{isrc}";
                HttpResponseMessage response = await httpClient.GetAsync(apiUrl);

                if (response.IsSuccessStatusCode)
                {
                    string jsonResponse = await response.Content.ReadAsStringAsync();
                    if (jsonResponse.Contains("\"error\""))
                        return string.Empty;

                    string trackId = ExtractJsonValue(jsonResponse, "id");
                    return trackId;
                }

                return string.Empty;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error searching ISRC {isrc}: {ex.Message}");
                return string.Empty;
            }
        }

        private List<TrackInfo> RemoveDuplicateTracks(List<TrackInfo> tracks)
        {
            var uniqueTracks = new List<TrackInfo>();
            var processedIdentifiers = new Dictionary<string, TrackInfo>();

            foreach (var track in tracks)
            {
                string identifier;

                // Use ISRC as identifier if available, otherwise use Artist+Title
                if (!string.IsNullOrEmpty(track.ISRC) && track.ISRC != "N/A")
                {
                    identifier = track.ISRC;
                }
                else
                {
                    identifier = track.Artist.ToLowerInvariant() + "|" + track.Title.ToLowerInvariant();
                }

                if (!processedIdentifiers.ContainsKey(identifier))
                {
                    processedIdentifiers.Add(identifier, track);
                    uniqueTracks.Add(track);
                }
            }

            return uniqueTracks;
        }

        // Helper method to sanitize folder names
        private string SanitizeFolderName(string folderName)
        {
            if (string.IsNullOrEmpty(folderName))
                return "Unknown";

            // Replace invalid characters with underscores
            string invalidChars = new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars());
            foreach (char c in invalidChars)
            {
                folderName = folderName.Replace(c, '_');
            }

            // Remove additional problematic symbols for batch files
            string problematicSymbols = "|/$%&=\"?^[]#@";
            foreach (char c in problematicSymbols)
            {
                folderName = folderName.Replace(c, '_');
            }

            // Trim and limit length if needed
            folderName = folderName.Trim();
            if (folderName.Length > 50)
                folderName = folderName.Substring(0, 47) + "...";

            return folderName;
        }

        // Helper method to sanitize any string that will be used in batch commands
        private string SanitizeBatchString(string input)
        {
            if (string.IsNullOrEmpty(input))
                return "Unknown";

            // Remove characters that could interfere with batch execution
            string problematicSymbols = "|/$%&=\"?^[]#@";
            foreach (char c in problematicSymbols)
            {
                input = input.Replace(c, '_');
            }

            // Escape carets which are special in batch files
            input = input.Replace("^", "^^");

            // Escape parentheses
            input = input.Replace("(", "^(");
            input = input.Replace(")", "^)");

            // Escape exclamation marks (for delayed expansion contexts)
            input = input.Replace("!", "^!");

            return input;
        }

        private void GenerateDownloadBatchFile(DownloadOptions options)
        {
            try
            {
                // Get all valid tracks with Track IDs
                Dictionary<string, List<TrackInfo>> tracksByPlaylist = new Dictionary<string, List<TrackInfo>>();
                int totalValidTracks = 0;

                foreach (DataRow row in resultsTable.Rows)
                {
                    string trackId = row["Track ID"].ToString();
                    if (!string.IsNullOrEmpty(trackId) && trackId != "N/A" && trackId != "Not found" && trackId != "Searching..." && trackId != "Pending")
                    {
                        TrackInfo track = new TrackInfo
                        {
                            Artist = row["Artist"].ToString(),
                            Title = row["Title"].ToString(),
                            TrackID = trackId,
                            PlaylistName = row["Playlist Name"].ToString(),
                            Platform = row["Platform"].ToString(),
                            ISRC = row["ISRC"].ToString()
                        };

                        string playlistFolderName = SanitizeFolderName(track.PlaylistName);

                        if (!tracksByPlaylist.ContainsKey(playlistFolderName))
                            tracksByPlaylist[playlistFolderName] = new List<TrackInfo>();

                        tracksByPlaylist[playlistFolderName].Add(track);
                        totalValidTracks++;
                    }
                }

                if (totalValidTracks == 0)
                {
                    MessageBox.Show("No valid Deezer tracks found to download.", "No Tracks", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Create multiple batch files if needed
                List<List<TrackInfo>> batchGroups = new List<List<TrackInfo>>();
                var allTracks = new List<TrackInfo>();

                foreach (var playlist in tracksByPlaylist.Values)
                {
                    allTracks.AddRange(playlist);
                }

                // Split tracks into groups if needed
                if (options.SplitBatchFiles && options.TracksPerBatch > 0)
                {
                    for (int i = 0; i < allTracks.Count; i += options.TracksPerBatch)
                    {
                        batchGroups.Add(allTracks.Skip(i).Take(options.TracksPerBatch).ToList());
                    }
                }
                else
                {
                    batchGroups.Add(allTracks);
                }

                string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

                for (int batchIndex = 0; batchIndex < batchGroups.Count; batchIndex++)
                {
                    var batchTracks = batchGroups[batchIndex];
                    string batchFileName;

                    if (batchGroups.Count > 1)
                    {
                        batchFileName = $"deemix_download_{DateTime.Now:yyyyMMdd_HHmmss}_part{batchIndex + 1}.bat";
                    }
                    else
                    {
                        batchFileName = $"deemix_download_{DateTime.Now:yyyyMMdd_HHmmss}.bat";
                    }

                    string batchFilePath = Path.Combine(desktopPath, batchFileName);

                    using (StreamWriter writer = new StreamWriter(batchFilePath))
                    {
                        writer.WriteLine("@echo off");
                        writer.WriteLine("REM Download tracks from Deezer using deemix");
                        writer.WriteLine("echo.");
                        writer.WriteLine($"echo Starting download of {batchTracks.Count} tracks");
                        if (batchGroups.Count > 1)
                        {
                            writer.WriteLine($"echo Batch file {batchIndex + 1} of {batchGroups.Count}");
                        }
                        writer.WriteLine("echo.");

                        // Create base download folder if it doesn't exist
                        writer.WriteLine($"if not exist \"{options.DownloadPath}\" mkdir \"{options.DownloadPath}\"");

                        var duplicateTracker = new Dictionary<string, int>();
                        int currentTrack = 0;

                        foreach (var track in batchTracks)
                        {
                            currentTrack++;

                            // Handle delay
                            if (options.UseDelay && options.DelayInterval > 0 && currentTrack % 50 == 0 && currentTrack < batchTracks.Count)
                            {
                                writer.WriteLine($"echo Pausing for {options.DelaySeconds} seconds after 50 tracks...");
                                writer.WriteLine($"timeout /t {options.DelaySeconds} /nobreak");
                                writer.WriteLine("echo.");
                            }

                            string url = $"https://www.deezer.com/track/{track.TrackID}";
                            string targetFolder;

                            // Determine target folder
                            if (options.UseSubfolders)
                            {
                                string playlistFolder = SanitizeFolderName(track.PlaylistName);
                                targetFolder = Path.Combine(options.DownloadPath, playlistFolder);
                                writer.WriteLine($"if not exist \"{targetFolder}\" mkdir \"{targetFolder}\"");
                            }
                            else
                            {
                                targetFolder = options.DownloadPath;
                            }

                            // Handle duplicates
                            string trackKey = $"{track.Artist}_{track.Title}".ToLowerInvariant();
                            if (duplicateTracker.ContainsKey(trackKey))
                            {
                                if (options.SaveDuplicatesWithISRC && !string.IsNullOrWhiteSpace(track.ISRC) && track.ISRC != "N/A")
                                {
                                    duplicateTracker[trackKey]++;
                                    writer.WriteLine($"echo Track {currentTrack}/{batchTracks.Count} - {SanitizeBatchString(track.Artist)} - {SanitizeBatchString(track.Title)} (duplicate - including ISRC: {track.ISRC})");
                                    writer.WriteLine($"deemix --portable \"{url}\" -b 128 -p \"{targetFolder}\"");
                                    writer.WriteLine("echo Completed");
                                }
                                else
                                {
                                    writer.WriteLine($"echo Track {currentTrack}/{batchTracks.Count} - {SanitizeBatchString(track.Artist)} - {SanitizeBatchString(track.Title)} (skipping duplicate)");
                                }
                            }
                            else
                            {
                                duplicateTracker[trackKey] = 1;
                                writer.WriteLine($"echo Track {currentTrack}/{batchTracks.Count} - {SanitizeBatchString(track.Artist)} - {SanitizeBatchString(track.Title)}");
                                writer.WriteLine($"deemix --portable \"{url}\" -b 128 -p \"{targetFolder}\"");
                                writer.WriteLine("echo Completed");
                            }

                            writer.WriteLine("echo.");
                        }

                        writer.WriteLine("echo.");
                        writer.WriteLine($"echo Batch {batchIndex + 1} completed successfully!");
                        writer.WriteLine("echo The batch file will be deleted automatically...");
                        writer.WriteLine("pause");
                        writer.WriteLine("start /b \"\" cmd /c del \"%~f0\" & exit /b");
                    }
                }

                string message = $"Created {batchGroups.Count} batch file(s) on desktop with {totalValidTracks} total tracks.\n\n";
                message += $"Download path: {options.DownloadPath}\n";
                message += $"Use subfolders: {(options.UseSubfolders ? "Yes" : "No")}\n";
                if (options.UseDelay)
                    message += $"Delay: {options.DelaySeconds} seconds every 50 tracks\n";
                if (options.SplitBatchFiles)
                    message += $"Split into batches of {options.TracksPerBatch} tracks each\n";
                message += $"Save duplicates with ISRC: {(options.SaveDuplicatesWithISRC ? "Yes" : "No")}\n\n";
                message += "Double-click the batch file(s) on desktop to start downloading.\n";
                message += "Files will be deleted automatically after execution.";

                MessageBox.Show(message, "Download Ready", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        #endregion

        // Add this method in the "Button Event Handlers" region, after btnImportCSV_Click

        private async void btnImportISRC_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openDialog = new OpenFileDialog())
            {
                openDialog.Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*";
                openDialog.Title = "Import ISRC List from TXT";

                if (openDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        // Ask if user wants to clear existing results
                        DialogResult clearResult = MessageBox.Show(
                            "Do you want to clear existing results before importing?",
                            "Clear Results",
                            MessageBoxButtons.YesNoCancel,
                            MessageBoxIcon.Question);

                        if (clearResult == DialogResult.Cancel)
                            return;

                        if (clearResult == DialogResult.Yes)
                        {
                            resultsTable.Clear();
                            UpdateResultCount();
                        }

                        // Update UI for processing
                        SetProcessingUI(true);

                        List<TrackInfo> importedTracks = await ImportISRCsFromTxt(openDialog.FileName);

                        // Add imported tracks to results
                        foreach (var track in importedTracks)
                        {
                            resultsTable.Rows.Add(
                                track.Platform,
                                track.PlaylistName,
                                track.Artist,
                                track.Title,
                                track.ISRC,
                                track.TrackID,
                                track.TrackLink
                            );
                        }

                        UpdateResultCount();

                        if (importedTracks.Count > 0)
                        {
                            MessageBox.Show($"Successfully imported {importedTracks.Count} tracks from ISRC list.",
                                "Import Completed", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                        else
                        {
                            MessageBox.Show("No valid ISRCs found in the text file.",
                                "Import", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error importing ISRCs from text file: {ex.Message}",
                            "Import Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    finally
                    {
                        // Reset UI
                        SetProcessingUI(false);
                    }
                }
            }
        }

        // Add this method in the "Track Processing Methods" region

        private async Task<List<TrackInfo>> ImportISRCsFromTxt(string filePath)
        {
            List<TrackInfo> tracks = new List<TrackInfo>();
            int validTracks = 0;
            int invalidTracks = 0;

            try
            {
                // Read all lines from the text file
                string[] allLines = File.ReadAllLines(filePath);

                // Filter out empty lines and the header line if present
                List<string> isrcList = new List<string>();
                foreach (string line in allLines)
                {
                    string trimmedLine = line.Trim();
                    if (!string.IsNullOrWhiteSpace(trimmedLine) &&
                        trimmedLine.ToUpper() != "ISRC" &&
                        trimmedLine.Length >= 12) // ISRC codes are typically 12 characters
                    {
                        isrcList.Add(trimmedLine);
                    }
                }

                // Calculate total for progress bar
                progressBarMain.Value = 0;
                totalTracks = isrcList.Count;
                progressBarMain.Maximum = totalTracks;
                progressBarTrack.Maximum = totalTracks;

                for (int i = 0; i < isrcList.Count; i++)
                {
                    string isrc = isrcList[i];

                    // Update progress bars
                    progressBarMain.Value = i + 1;
                    processedTracks = i + 1;
                    progressBarTrack.Value = processedTracks;
                    lblProgress.Text = $"Processing ISRCs: {processedTracks}/{totalTracks}";
                    Application.DoEvents();

                    // Create the track with Searching status
                    TrackInfo track = new TrackInfo
                    {
                        Artist = "Unknown",
                        Title = "Unknown",
                        ISRC = isrc,
                        TrackID = "Searching...",
                        TrackLink = "N/A",
                        Platform = "ISRC",
                        PlaylistName = "Imported ISRC List"
                    };

                    // Lookup the Deezer Track ID and metadata using the ISRC
                    var trackData = await GetTrackDataFromISRC(isrc);

                    if (trackData != null)
                    {
                        track.Artist = trackData.Artist;
                        track.Title = trackData.Title;
                        track.TrackID = trackData.TrackID;
                        track.TrackLink = trackData.TrackLink;
                        validTracks++;
                    }
                    else
                    {
                        track.TrackID = "Not found";
                        invalidTracks++;
                    }

                    tracks.Add(track);
                }

                // Show summary in the progress label
                lblProgress.Text = $"Imported: {validTracks} valid tracks, {invalidTracks} not found";

                return tracks;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error reading text file: {ex.Message}",
                    "TXT Import Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return new List<TrackInfo>();
            }
        }

        // Add this helper method to get full track data from ISRC

        private async Task<TrackInfo> GetTrackDataFromISRC(string isrc)
        {
            try
            {
                string apiUrl = $"https://api.deezer.com/track/isrc:{isrc}";
                HttpResponseMessage response = await httpClient.GetAsync(apiUrl);

                if (response.IsSuccessStatusCode)
                {
                    string jsonResponse = await response.Content.ReadAsStringAsync();

                    // Check if there's an error in the response
                    if (jsonResponse.Contains("\"error\""))
                        return null;

                    // Extract track data
                    string trackId = ExtractJsonValue(jsonResponse, "id");
                    string title = ExtractJsonValue(jsonResponse, "title");
                    string trackLink = ExtractJsonValue(jsonResponse, "link");

                    // Extract artist name
                    Regex artistRegex = new Regex(@"""artist"":\s*\{(.*?)\}", RegexOptions.Singleline);
                    Match artistMatch = artistRegex.Match(jsonResponse);
                    string artistName = "Unknown Artist";

                    if (artistMatch.Success)
                    {
                        string artistObject = artistMatch.Groups[1].Value;
                        artistName = ExtractJsonValue(artistObject, "name");
                    }

                    if (!string.IsNullOrEmpty(trackId))
                    {
                        return new TrackInfo
                        {
                            Artist = string.IsNullOrEmpty(artistName) ? "Unknown Artist" : artistName,
                            Title = string.IsNullOrEmpty(title) ? "Unknown Title" : title,
                            ISRC = isrc,
                            TrackID = trackId,
                            TrackLink = string.IsNullOrEmpty(trackLink) ? $"https://www.deezer.com/track/{trackId}" : trackLink,
                            Platform = "ISRC",
                            PlaylistName = "Imported ISRC List"
                        };
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error searching ISRC {isrc}: {ex.Message}");
                return null;
            }
        }



        private void btnSelectAll_Click(object sender, EventArgs e)
        {
            foreach (DataRow row in playlistTable.Rows)
            {
                row["Selected"] = true;
            }
        }

        private void btnDeselectAll_Click(object sender, EventArgs e)
        {
            foreach (DataRow row in playlistTable.Rows)
            {
                row["Selected"] = false;
            }
        }
    }

    [Serializable]
    public class PlaylistInfoList
    {
        public List<PlaylistInfo> Playlists { get; set; } = new List<PlaylistInfo>();
    }

    [Serializable]
    public class PlaylistInfo
    {
        public string Platform { get; set; }
        public string Url { get; set; }
        public string Name { get; set; }
    }

    public class TrackInfo
    {
        public string Artist { get; set; }
        public string Title { get; set; }
        public string ISRC { get; set; }
        public string TrackID { get; set; }
        public string TrackLink { get; set; }
        public string Platform { get; set; }
        public string PlaylistName { get; set; }
    }

    // Classe per le opzioni di download
    public class DownloadOptions
    {
        public string DownloadPath { get; set; }
        public bool UseSubfolders { get; set; }
        public bool UseDelay { get; set; }
        public int DelaySeconds { get; set; }
        public int DelayInterval { get; set; } = 50; // ogni 50 canzoni
        public bool SplitBatchFiles { get; set; }
        public int TracksPerBatch { get; set; } = 1000;
        public bool SaveDuplicatesWithISRC { get; set; }
    }
}