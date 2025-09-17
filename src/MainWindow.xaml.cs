using MahApps.Metro.Controls;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Windows;
using System.Windows.Controls;
using VSModUpdater.Resources.Functions.Services;

namespace VSModUpdater
{
    public partial class MainWindow : MetroWindow
    {
        private readonly string ModLinksJsonPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "AriesLR", "VSModUpdateTool", "ModLinks.json");

        private string selectedModPath;

        public MainWindow()
        {
            InitializeComponent();

            // Ensure the config directory exists
            var directory = Path.GetDirectoryName(ModLinksJsonPath);
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            this.Loaded += (s, e) =>
            {
                this.Dispatcher.BeginInvoke(new Action(async () =>
                {
                    await PromptOnStart();
                }), System.Windows.Threading.DispatcherPriority.ApplicationIdle);
            };
        }

        // Open Github Repo
        private void LaunchBrowserGitHubVSModUpdater(object sender, RoutedEventArgs e)
        {
            UrlService.OpenUrlAsync("https://github.com/AriesLR/VS-Mod-Update-Tool");
        }

        // Open Covert Paradise Discord
        private void LaunchBrowserCVPDiscord(object sender, RoutedEventArgs e)
        {
            UrlService.OpenUrlAsync("https://discord.gg/8n9rfQCd8G");
        }

        // Check For Updates
        private async void CheckForUpdates(object sender, RoutedEventArgs e)
        {
            await UpdateService.CheckForUpdatesAsync("https://raw.githubusercontent.com/AriesLR/VS-Mod-Update-Tool/refs/heads/main/docs/version/update.json");
        }

        // Splash screen / folder browser
        private async Task PromptOnStart()
        {
            bool userConfirmed = await MessageService.ShowBrowseCancel("Where are your mods?", "Please select the folder where your mods are stored.");

            if (userConfirmed)
            {
                using (var dialog = new FolderBrowserDialog())
                {
                    dialog.Description = "Select your Vintage Story mods folder";
                    dialog.ShowNewFolderButton = false;

                    if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    {
                        ModsFolderTextBox.Text = dialog.SelectedPath;
                        LoadModsFromFolder(dialog.SelectedPath);
                        selectedModPath = dialog.SelectedPath;
                    }
                }
            }
        }

        // Method to allow dragging the title bar through the text box
        /*private void ModsFolderTextBox_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                this.DragMove();
        }
        */

        // Browse folder button
        private void BrowseFolder_Click(object sender, RoutedEventArgs e)
        {
            using (var dialog = new FolderBrowserDialog())
            {
                dialog.Description = "Select your Vintage Story mods folder";
                dialog.ShowNewFolderButton = false;

                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    ModsFolderTextBox.Text = dialog.SelectedPath;
                    LoadModsFromFolder(dialog.SelectedPath);
                    selectedModPath = dialog.SelectedPath;
                }
            }
        }

        // Refresh mods folder
        private void RefreshMods_Click(object sender, RoutedEventArgs e)
        {
            LoadModsFromFolder(selectedModPath);
        }

        // Check For Mod Updates Button
        private async void CheckUpdatesButton_Click(object sender, RoutedEventArgs e)
        {
            await CheckForModUpdatesAsync();
        }

        // Load mods from folder
        private async void LoadModsFromFolder(string folderPath)
        {
            var mods = new List<ModInfo>();

            string GetValue(JObject obj, string key)
            {
                return obj.Properties().FirstOrDefault(p => string.Equals(p.Name, key, StringComparison.OrdinalIgnoreCase))?.Value?.ToString() ?? "";
            }

            foreach (var zipFile in Directory.GetFiles(folderPath, "*.zip"))
            {
                try
                {
                    using (var archive = ZipFile.OpenRead(zipFile))
                    {
                        var entry = archive.GetEntry("modinfo.json");
                        if (entry != null)
                        {
                            using (var stream = entry.Open())
                            using (var reader = new StreamReader(stream))
                            {
                                string json = reader.ReadToEnd();
                                var root = JObject.Parse(json);

                                string gameVersion = "";
                                var dependenciesToken = root.Properties().FirstOrDefault(p => string.Equals(p.Name, "dependencies", StringComparison.OrdinalIgnoreCase))?.Value as JObject;
                                if (dependenciesToken != null)
                                {
                                    gameVersion = dependenciesToken.Properties().FirstOrDefault(p => string.Equals(p.Name, "game", StringComparison.OrdinalIgnoreCase))?.Value?.ToString() ?? "";
                                }

                                var mod = new ModInfo
                                {
                                    FileName = Path.GetFileName(zipFile),
                                    Name = string.IsNullOrWhiteSpace(GetValue(root, "name")) ? "N/A" : GetValue(root, "name"),
                                    Version = string.IsNullOrWhiteSpace(GetValue(root, "version")) ? "N/A" : GetValue(root, "version"),
                                    Game = string.IsNullOrWhiteSpace(gameVersion) ? "N/A" : gameVersion,
                                    Description = string.IsNullOrWhiteSpace(GetValue(root, "description")) ? "N/A" : GetValue(root, "description")
                                };

                                mods.Add(mod);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    await MessageService.ShowError($"Error reading {zipFile}: {ex.Message}");
                }
            }

            ModsDataGrid.ItemsSource = mods;
        }

        ///////////////////////////////////
        // CHECK FOR MOD UPDATES SECTION //
        ///////////////////////////////////

        // Model for the table data
        public class ModInfo
        {
            public string FileName { get; set; }
            public string Name { get; set; }
            public string Version { get; set; }
            public string Game { get; set; }
            public string Description { get; set; }

            public string LatestVersion { get; set; } = "";
            public string DownloadLink { get; set; } = "";

            public bool HasUpdate => !string.IsNullOrWhiteSpace(LatestVersion) && Version != LatestVersion;
        }

        // Model for JSON storage
        private class ModLinkInfo
        {
            public string DownloadLink { get; set; }
        }

        // Loads saved mod links from json
        private Dictionary<string, ModLinkInfo> LoadSavedModLinks()
        {
            if (!File.Exists(ModLinksJsonPath))
                return new Dictionary<string, ModLinkInfo>();

            string json = File.ReadAllText(ModLinksJsonPath);
            return JsonConvert.DeserializeObject<Dictionary<string, ModLinkInfo>>(json) ?? new Dictionary<string, ModLinkInfo>();
        }

        // Saves mod links to json
        private void SaveModLinks(Dictionary<string, ModLinkInfo> links)
        {
            string json = JsonConvert.SerializeObject(links, Formatting.Indented);
            File.WriteAllText(ModLinksJsonPath, json);
        }

        // Check for mod updates
        private async Task CheckForModUpdatesAsync()
        {
            if (ModsDataGrid.ItemsSource is not List<ModInfo> mods || mods.Count == 0)
                return;

            var savedLinks = LoadSavedModLinks();

            await MessageService.ShowProgress("Checking for mod updates", "Please wait...", async progress =>
            {
                using var client = new HttpClient();
                int totalModsChecked = mods.Count;
                int processedModsChecked = 0;
                var modsNeedingUpdate = new List<ModInfo>();

                string selectedVersion = (GameVersionDropdown.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "1.21.x";
                string selectedVersionPrefix = selectedVersion.Replace(".x", "");

                foreach (var mod in mods)
                {
                    string zipPath = Path.Combine(ModsFolderTextBox.Text, mod.FileName);
                    string modId = null;

                    try
                    {
                        using var archive = ZipFile.OpenRead(zipPath);
                        var entry = archive.GetEntry("modinfo.json");
                        if (entry != null)
                        {
                            using var stream = entry.Open();
                            using var reader = new StreamReader(stream);
                            var json = reader.ReadToEnd();
                            var root = JObject.Parse(json);

                            modId = root.Properties().FirstOrDefault(p => string.Equals(p.Name, "ModID", StringComparison.OrdinalIgnoreCase))?.Value?.ToString();

                            mod.Version = root.Properties().FirstOrDefault(p => string.Equals(p.Name, "version", StringComparison.OrdinalIgnoreCase))?.Value?.ToString() ?? mod.Version;
                        }
                    }
                    catch
                    {
                        continue;
                    }

                    if (!string.IsNullOrEmpty(modId))
                    {
                        try
                        {
                            string apiUrl = $"https://mods.vintagestory.at/api/mod/{modId}";
                            string response = await client.GetStringAsync(apiUrl);
                            var apiData = JObject.Parse(response);

                            var releases = apiData["mod"]?["releases"] as JArray;
                            if (releases != null && releases.Count > 0)
                            {
                                foreach (var release in releases)
                                {
                                    var tags = release["tags"]?.Select(t => t.ToString()) ?? Enumerable.Empty<string>();
                                    if (tags.Any(t => t.StartsWith(selectedVersionPrefix)))
                                    {
                                        string latestVersion = release["modversion"]?.ToString()?.TrimStart('v').Trim();
                                        string currentVersion = mod.Version?.TrimStart('v').Trim();

                                        mod.LatestVersion = latestVersion;
                                        mod.DownloadLink = release["mainfile"]?.ToString();

                                        // Update saved links
                                        if (!string.IsNullOrEmpty(modId) && !string.IsNullOrEmpty(mod.DownloadLink))
                                        {
                                            savedLinks[modId] = new ModLinkInfo
                                            {
                                                DownloadLink = mod.DownloadLink
                                            };
                                        }

                                        if (!string.IsNullOrEmpty(latestVersion) && !string.IsNullOrEmpty(currentVersion))
                                        {
                                            if (IsVersionGreater(latestVersion, currentVersion))
                                                modsNeedingUpdate.Add(mod);
                                        }

                                        break; // stop after first matching release
                                    }
                                }
                            }
                        }
                        catch { }
                    }

                    processedModsChecked++;
                    progress.Report((double)processedModsChecked / totalModsChecked);
                }

                // Sort and save the updated links
                var sortedLinks = savedLinks.OrderBy(kvp => kvp.Key).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                SaveModLinks(sortedLinks);

                ModsDataGrid.Items.Refresh();

                string message;
                if (modsNeedingUpdate.Count > 0)
                {
                    var modNames = string.Join("\n- ", modsNeedingUpdate.Select(m => m.Name));
                    message = $"Total Mods Checked: {totalModsChecked}\n\n{modsNeedingUpdate.Count} mod(s) need to be updated:\n\n- {modNames}";
                }
                else
                {
                    message = "All mods are up to date!";
                }

                await MessageService.ShowInfo("Update Check Complete", message);
            });
        }

        private bool IsVersionGreater(string v1, string v2)
        {
            string[] splitV1 = v1.Split('-', '.', '+');
            string[] splitV2 = v2.Split('-', '.', '+');

            int maxLen = Math.Max(splitV1.Length, splitV2.Length);

            for (int i = 0; i < maxLen; i++)
            {
                int n1 = i < splitV1.Length && int.TryParse(splitV1[i], out var val1) ? val1 : 0;
                int n2 = i < splitV2.Length && int.TryParse(splitV2[i], out var val2) ? val2 : 0;

                if (n1 > n2) return true;
                if (n1 < n2) return false;
            }

            // If numeric parts are equal, treat release > pre-release
            bool isV1PreRelease = v1.Contains("-");
            bool isV2PreRelease = v2.Contains("-");
            if (isV1PreRelease && !isV2PreRelease) return false;
            if (!isV1PreRelease && isV2PreRelease) return true;

            return false;
        }

        /////////////////////////
        // UPDATE MODS SECTION //
        /////////////////////////

        // Extract the actual download URL (strip ?dl=...)
        private static string GetActualDownloadUrl(string url)
        {
            int qIndex = url.IndexOf("?dl=");
            return qIndex >= 0 ? url.Substring(0, qIndex) : url;
        }

        // Extract the file name from the ?dl= parameter
        private static string GetFileNameFromDownloadUrl(string url)
        {
            int qIndex = url.IndexOf("?dl=");
            string fileName = qIndex >= 0 ? url.Substring(qIndex + 4) : Path.GetFileName(url);

            // Clean invalid file name characters
            foreach (var c in Path.GetInvalidFileNameChars())
                fileName = fileName.Replace(c, '_');

            return fileName;
        }

        // Download a file to temp folder using the stripped URL
        private static async Task<string> DownloadFileToTempAsync(string url, HttpClient client)
        {
            string actualUrl = GetActualDownloadUrl(url);
            string tempFilePath = Path.Combine(Path.GetTempPath(), GetFileNameFromDownloadUrl(url));

            using var response = await client.GetAsync(actualUrl, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            using var fs = new FileStream(tempFilePath, FileMode.Create, FileAccess.Write, FileShare.None);
            await response.Content.CopyToAsync(fs);

            return tempFilePath;
        }

        // Updates All Mods Button
        private async void UpdateAllModsButton_Click(object sender, RoutedEventArgs e)
        {
            bool result = await MessageService.ShowYesCancel(
                "Confirm Update",
                "Are you sure you want to update all mods?");

            if (result)
            {
                await UpdateAllModsAsync();
            }
            else
            {
                // Cancel
            }
        }

        // Update All Mods
        private async Task UpdateAllModsAsync()
        {
            if (string.IsNullOrWhiteSpace(ModsFolderTextBox.Text) || !Directory.Exists(ModsFolderTextBox.Text))
            {
                await MessageService.ShowError("Mods folder is not selected or does not exist.");
                return;
            }

            if (ModsDataGrid.ItemsSource is not List<ModInfo> mods || mods.Count == 0)
            {
                await MessageService.ShowError("No mods loaded to update.");
                return;
            }

            var savedLinks = LoadSavedModLinks();
            using var client = new HttpClient();
            var updateResults = new List<string>();

            int totalModsToUpdate = mods.Count(m => m.HasUpdate && !string.IsNullOrWhiteSpace(m.DownloadLink));
            if (totalModsToUpdate == 0)
            {
                await MessageService.ShowInfo("Updating Mods Complete", "Finished updating mods! (No mods were updated)");
                return;
            }

            int processedModsToUpdate = 0;

            await MessageService.ShowProgress("Updating Mods", "Please wait...", async progress =>
            {
                foreach (var mod in mods)
                {
                    if (!mod.HasUpdate || string.IsNullOrWhiteSpace(mod.DownloadLink))
                        continue;

                    string zipPath = Path.Combine(ModsFolderTextBox.Text, mod.FileName);
                    string modId = null;

                    // Read modId from modName.zip
                    try
                    {
                        using var archive = ZipFile.OpenRead(zipPath);
                        var entry = archive.GetEntry("modinfo.json");
                        if (entry != null)
                        {
                            using var stream = entry.Open();
                            using var reader = new StreamReader(stream);
                            var json = reader.ReadToEnd();
                            var root = JObject.Parse(json);
                            modId = root.Properties().FirstOrDefault(p => string.Equals(p.Name, "modId", StringComparison.OrdinalIgnoreCase))?.Value?.ToString();
                        }
                    }
                    catch (Exception ex)
                    {
                        await MessageService.ShowError($"Error reading modId from {mod.FileName}: {ex.Message}");
                    }

                    if (string.IsNullOrEmpty(modId))
                    {
                        await MessageService.ShowError($"Could not read modId for {mod.Name}, skipping update.");
                        processedModsToUpdate++;
                        progress.Report((double)processedModsToUpdate / totalModsToUpdate);
                        continue;
                    }

                    if (!savedLinks.ContainsKey(modId) || string.IsNullOrWhiteSpace(savedLinks[modId].DownloadLink))
                    {
                        await MessageService.ShowError($"No download link found for {mod.Name}, skipping update.");
                        processedModsToUpdate++;
                        progress.Report((double)processedModsToUpdate / totalModsToUpdate);
                        continue;
                    }

                    string downloadUrl = savedLinks[modId].DownloadLink;
                    string tempFile;

                    try
                    {
                        // Download the updated mod using helpers
                        tempFile = await DownloadFileToTempAsync(downloadUrl, client);
                    }
                    catch (Exception ex)
                    {
                        await MessageService.ShowError($"Failed to download {mod.Name}: {ex.Message}");
                        processedModsToUpdate++;
                        progress.Report((double)processedModsToUpdate / totalModsToUpdate);
                        continue;
                    }

                    try
                    {
                        // Delete the old mod modName.zip
                        if (File.Exists(zipPath))
                            File.Delete(zipPath);

                        // Move downloaded file to mods folder with correct name
                        string destPath = Path.Combine(ModsFolderTextBox.Text, GetFileNameFromDownloadUrl(downloadUrl));
                        File.Move(tempFile, destPath);

                        // Record old/new versions for summary
                        updateResults.Add($"Updated {modId} from version {mod.Version} to version {mod.LatestVersion}");

                        // Update ModInfo model
                        mod.FileName = Path.GetFileName(destPath);
                        mod.Version = mod.LatestVersion;
                    }
                    catch (Exception ex)
                    {
                        await MessageService.ShowError($"Failed to replace {mod.Name}: {ex.Message}");
                        processedModsToUpdate++;
                        progress.Report((double)processedModsToUpdate / totalModsToUpdate);
                        continue;
                    }

                    processedModsToUpdate++;
                    progress.Report((double)processedModsToUpdate / totalModsToUpdate);
                }

                // Refresh DataGrid
                ModsDataGrid.Items.Refresh();
            });

            // Show final summary
            if (updateResults.Count > 0)
            {
                string summary = string.Join("\n", updateResults);
                await MessageService.ShowModOutput("Updating Mods Complete", $"Finished updating mods!\n", $"{summary}");
            }
            else
            {
                await MessageService.ShowInfo("Updating Mods Complete", "Finished updating mods! (No mods were updated)");
            }
        }

        // Update Single Mod
        private async Task UpdateModAsync(ModInfo mod)
        {
            if (string.IsNullOrWhiteSpace(ModsFolderTextBox.Text) || !Directory.Exists(ModsFolderTextBox.Text))
            {
                await MessageService.ShowError("Mods folder is not selected or does not exist.");
                return;
            }

            if (mod == null || string.IsNullOrWhiteSpace(mod.DownloadLink))
            {
                await MessageService.ShowError($"Invalid mod or missing download link for {mod?.Name ?? "Unknown"}.");
                return;
            }

            var savedLinks = LoadSavedModLinks();
            using var client = new HttpClient();

            string zipPath = Path.Combine(ModsFolderTextBox.Text, mod.FileName);
            string modId = null;

            // Read modId from modName.zip
            try
            {
                using var archive = ZipFile.OpenRead(zipPath);
                var entry = archive.GetEntry("modinfo.json");
                if (entry != null)
                {
                    using var stream = entry.Open();
                    using var reader = new StreamReader(stream);
                    var json = reader.ReadToEnd();
                    var root = JObject.Parse(json);
                    modId = root.Properties().FirstOrDefault(p => string.Equals(p.Name, "modId", StringComparison.OrdinalIgnoreCase))?.Value?.ToString();
                }
            }
            catch (Exception ex)
            {
                await MessageService.ShowError($"Error reading modId from {mod.FileName}: {ex.Message}");
                return;
            }

            if (string.IsNullOrEmpty(modId))
            {
                await MessageService.ShowError($"Could not read modId for {mod.Name}, skipping update.");
                return;
            }

            if (!savedLinks.ContainsKey(modId) || string.IsNullOrWhiteSpace(savedLinks[modId].DownloadLink))
            {
                await MessageService.ShowError($"No download link found for {mod.Name}, skipping update.");
                return;
            }

            string downloadUrl = savedLinks[modId].DownloadLink;
            string tempFile;

            try
            {
                // Download the mod using helpers
                tempFile = await DownloadFileToTempAsync(downloadUrl, client);
            }
            catch (Exception ex)
            {
                await MessageService.ShowError($"Failed to download {mod.Name}: {ex.Message}");
                return;
            }

            try
            {
                // Delete old mod
                if (File.Exists(zipPath))
                    File.Delete(zipPath);

                // Move downloaded file with correct name
                string destPath = Path.Combine(ModsFolderTextBox.Text, GetFileNameFromDownloadUrl(downloadUrl));
                File.Move(tempFile, destPath);

                // Update ModInfo
                mod.FileName = Path.GetFileName(destPath);
                mod.Version = mod.LatestVersion;
            }
            catch (Exception ex)
            {
                await MessageService.ShowError($"Failed to replace {mod.Name}: {ex.Message}");
                return;
            }

            // Refresh DataGrid row
            ModsDataGrid.Items.Refresh();
        }

        // Cheesy usage of my ShowProgress method for updating a single mod
        private async Task UpdateModWithProgressAsync(ModInfo mod)
        {
            await MessageService.ShowProgress("Updating Mods", $"Downloading {mod.Name}...\n\nPlease wait...", async progress =>
            {
                try
                {
                    // Simulate download progress
                    for (int i = 0; i <= 100; i += 10)
                    {
                        await Task.Delay(20);
                        progress.Report(i / 100.0);
                    }

                    // Actual update logic
                    await UpdateModAsync(mod);

                    progress.Report(1.0);
                }
                catch (Exception ex)
                {
                    await MessageService.ShowError($"Failed to update {mod.Name}: {ex.Message}");
                }
            });
        }

        // Update single mod button
        private async void UpdateModButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement fe && fe.DataContext is ModInfo mod)
            {
                try
                {
                    await UpdateModWithProgressAsync(mod);
                }
                catch (Exception ex)
                {
                    await MessageService.ShowError($"Failed to update {mod.Name}: {ex.Message}");
                }
            }
            else
            {
                Debug.WriteLine("Failed to get ModInfo from DataContext");
            }
        }

        // End of class
    }
}