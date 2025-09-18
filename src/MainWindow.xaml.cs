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
        private static readonly string appConfigFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "AriesLR", "VSModUpdateTool");

        private static readonly string modlinksPath = Path.Combine(appConfigFolder, "ModLinks.json");

        private static readonly string appSettingsPath = Path.Combine(appConfigFolder, "AppSettings.json");

        private string? selectedModPath;

        public MainWindow()
        {
            InitializeComponent();

            // Ensure config directory exists
            if (!Directory.Exists(appConfigFolder))
                Directory.CreateDirectory(appConfigFolder);

            this.Loaded += (s, e) =>
            {
                this.Dispatcher.BeginInvoke(new Action(async () =>
                {
                    await PromptOnStart();
                }), System.Windows.Threading.DispatcherPriority.ApplicationIdle);
            };
        }

        // ============ App Settings ============

        // Load AppSettings.json
        private AppSettings? LoadAppSettings()
        {
            if (!File.Exists(appSettingsPath))
                return null;

            try
            {
                string json = File.ReadAllText(appSettingsPath);
                return JsonConvert.DeserializeObject<AppSettings>(json);
            }
            catch
            {
                return null;
            }
        }

        // Save AppSettings.json
        private void SaveAppSettings(AppSettings settings)
        {
            string json = JsonConvert.SerializeObject(settings, Formatting.Indented);
            File.WriteAllText(appSettingsPath, json);
        }

        // ============ Title Bar Buttons ============

        // Open Github Repo
        private void OpenGithubRepo_Click(object sender, RoutedEventArgs e)
        {
            UrlService.OpenUrlAsync("https://github.com/AriesLR/VS-Mod-Update-Tool");
        }

        // Refresh mods folder
        private async void RefreshMods_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(selectedModPath) && Directory.Exists(selectedModPath))
                await LoadModsFromFolderAsync(selectedModPath);
            else
                await MessageService.ShowError("No mods folder selected.");
        }

        // Browse Folder Button
        private async void BrowseFolder_Click(object sender, RoutedEventArgs e)
        {
            await SelectModsFolderAsync();
        }

        // Open Buy Me A Coffee
        private void OpenBuyMeACoffee_Click(object sender, RoutedEventArgs e)
        {
            UrlService.OpenUrlAsync("https://buymeacoffee.com/arieslr");
        }

        // Check For Updates
        private async void CheckForUpdates_Click(object sender, RoutedEventArgs e)
        {
            await UpdateService.CheckForUpdatesAsync("https://raw.githubusercontent.com/AriesLR/VS-Mod-Update-Tool/refs/heads/main/docs/version/update.json");
        }

        // ============ App Startup ============

        // Splash screen / folder browser
        private async Task PromptOnStart()
        {
            // Load saved file path (if any)
            var settings = LoadAppSettings();

            string? savedPath = settings?.ModsFolderPath;

            if (!string.IsNullOrWhiteSpace(savedPath) && Directory.Exists(savedPath))
            {
                // Load saved path
                ModsFolderTextBox.Text = savedPath;
                await LoadModsFromFolderAsync(savedPath);
                selectedModPath = savedPath;
                return;
            }

            // If no valid saved path, prompt user to select folder
            bool userConfirmed = await MessageService.ShowBrowseCancel("Where are your mods?", "Please select the folder where your mods are stored.");

            if (userConfirmed)
            {
                await SelectModsFolderAsync();
            }
            else
            {
                selectedModPath = null;
            }
        }

        // ============ Button Clicks ============

        // Check For Mod Updates Button
        private async void CheckUpdatesButton_Click(object sender, RoutedEventArgs e)
        {
            await CheckForModUpdatesAsync();
        }

        // Updates All Mods Button
        private async void UpdateAllModsButton_Click(object sender, RoutedEventArgs e)
        {
            bool result = await MessageService.ShowYesCancel("Confirm Update", "Are you sure you want to update all mods?");

            if (result)
            {
                await UpdateAllModsAsync();
            }
            else
            {
                // Cancel
            }
        }

        // Update Single Mod Button
        private async void UpdateModButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement fe && fe.DataContext is ModInfo mod)
            {
                try
                {
                    await UpdateSingleModAsync(mod);
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

        // Open Mod Page Button
        private async void OpenModPage_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement fe && fe.DataContext is ModInfo mod)
            {
                if (!string.IsNullOrWhiteSpace(mod.ModPageUrl))
                {
                    try
                    {
                        UrlService.OpenUrlAsync($"{mod.ModPageUrl}");
                    }
                    catch (Exception ex)
                    {
                        await MessageService.ShowError($"Failed to open URL: {ex.Message}");
                    }
                }
            }
            else
            {
                Debug.WriteLine("Failed to get ModInfo from DataContext");
            }
        }

        // ============ Check For Mod Updates ============

        // Check For Mod Updates
        private async Task CheckForModUpdatesAsync()
        {
            if (!string.IsNullOrWhiteSpace(selectedModPath) && Directory.Exists(selectedModPath))
            {
                await LoadModsFromFolderAsync(selectedModPath);
            }
            else
            {
                await MessageService.ShowError("No mods folder selected.");
                return;
            }

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

                    try
                    {
                        using var archive = ZipFile.OpenRead(zipPath);
                        var entry = archive.GetEntry("modinfo.json")
                            ?? throw new InvalidDataException($"modinfo.json not found in {mod.FileName}");

                        using var stream = entry.Open();
                        using var reader = new StreamReader(stream);
                        var json = reader.ReadToEnd();
                        var root = JObject.Parse(json);

                        string modId = root.Properties()
                            .FirstOrDefault(p => string.Equals(p.Name, "ModID", StringComparison.OrdinalIgnoreCase))?
                            .Value?.ToString()
                            ?? throw new InvalidDataException($"ModID missing in {mod.FileName}");

                        string version = root.Properties()
                            .FirstOrDefault(p => string.Equals(p.Name, "version", StringComparison.OrdinalIgnoreCase))?
                            .Value?.ToString()
                            ?? throw new InvalidDataException($"Version missing in {mod.FileName}");

                        mod.Version = version;

                        // Fetch API Info
                        string apiUrl = $"https://mods.vintagestory.at/api/mod/{modId}";
                        string response = await client.GetStringAsync(apiUrl);
                        var apiData = JObject.Parse(response);

                        var releases = apiData["mod"]?["releases"] as JArray;
                        if (releases != null)
                        {
                            foreach (var release in releases)
                            {
                                var tags = release["tags"]?.Select(t => t.ToString()) ?? Enumerable.Empty<string>();
                                if (tags.Any(t => t.StartsWith(selectedVersionPrefix)))
                                {
                                    string latestVersion = release["modversion"]?.ToString()?.TrimStart('v').Trim()
                                        ?? throw new InvalidDataException($"Release modversion missing for {mod.Name}");
                                    string currentVersion = mod.Version.TrimStart('v').Trim();

                                    string downloadLink = release["mainfile"]?.ToString()
                                        ?? throw new InvalidDataException($"Release download url missing for {mod.Name}");

                                    // Get AssetID for ModPageUrl
                                    int assetId = apiData["mod"]?["assetid"]?.Value<int>()
                                        ?? throw new InvalidDataException($"AssetID missing for {mod.Name}");
                                    string modPageUrl = $"https://mods.vintagestory.at/show/mod/{assetId}";

                                    mod.LatestVersion = latestVersion;
                                    mod.ModDownloadUrl = downloadLink;
                                    mod.ModPageUrl = modPageUrl;

                                    // Update Saved Links
                                    savedLinks[modId] = new ModLinkInfo
                                    {
                                        ModDownloadUrl = downloadLink,
                                        ModPageUrl = modPageUrl
                                    };

                                    if (IsVersionGreater(latestVersion, currentVersion))
                                        modsNeedingUpdate.Add(mod);

                                    break; // Stop when first matching release is found
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        await MessageService.ShowError($"Failed to check mod {mod.FileName}: {ex.Message}");
                    }

                    processedModsChecked++;
                    progress.Report((double)processedModsChecked / totalModsChecked);
                }

                // Save Updated Links
                var sortedLinks = savedLinks.OrderBy(kvp => kvp.Key).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                SaveModLinks(sortedLinks);

                ModsDataGrid.Items.Refresh();

                string message;
                if (modsNeedingUpdate.Count > 0)
                {
                    var modNames = string.Join("\n- ", modsNeedingUpdate.Select(m => m.Name));
                    message = $"Total Mods: {totalModsChecked}\n\n{modsNeedingUpdate.Count} mod(s) need to be updated:\n\n- {modNames}";
                }
                else
                {
                    message = "All mods are up to date!";
                }

                await MessageService.ShowInfo("Update Check Complete", message);
            });
        }

        // ============ Update Mods ============

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

            var modsToUpdate = mods.Where(m => m.HasUpdate && !string.IsNullOrWhiteSpace(m.ModDownloadUrl)).ToList();
            if (modsToUpdate.Count == 0)
            {
                await MessageService.ShowInfo("Oops!", "You tried to update all mods when none of the mods required an update.");
                return;
            }

            int processed = 0;

            await MessageService.ShowProgress("Updating Mods", "Please wait...", async progress =>
            {
                foreach (var mod in modsToUpdate)
                {
                    var result = await UpdateModAsync(mod, ModsFolderTextBox.Text, savedLinks, client);

                    if (result.Success)
                        updateResults.Add(result.Message);
                    else
                        await MessageService.ShowError(result.Message);

                    processed++;
                    progress.Report((double)processed / modsToUpdate.Count);
                }

                ModsDataGrid.Items.Refresh();
            });

            // Show mods downloaded
            if (updateResults.Count > 0)
            {
                string summary = string.Join("\n", updateResults);
                await MessageService.ShowModOutput("Updating Mods Complete", "Finished updating mods!\n", summary);
            }
            else
            {
                await MessageService.ShowInfo("Oops!", "You tried to update all mods when none of the mods required an update.");
            }
        }

        // Update Single Mod
        private async Task UpdateSingleModAsync(ModInfo mod)
        {
            if (string.IsNullOrWhiteSpace(ModsFolderTextBox.Text) || !Directory.Exists(ModsFolderTextBox.Text))
            {
                await MessageService.ShowError("Mods folder is not selected or does not exist.");
                return;
            }

            if (mod == null || string.IsNullOrWhiteSpace(mod.ModDownloadUrl))
            {
                await MessageService.ShowError($"Invalid mod or missing download link for {mod?.Name ?? "Unknown"}.");
                return;
            }

            var savedLinks = LoadSavedModLinks();
            var updateResults = new List<string>();

            await MessageService.ShowProgress("Updating Mods", $"Downloading {mod.Name}...\n\nPlease wait...", async progress =>
            {
                try
                {
                    using var client = new HttpClient();

                    // Simulate download progress
                    for (int i = 0; i <= 100; i += 10)
                    {
                        await Task.Delay(20);
                        progress.Report(i / 100.0);
                    }

                    var result = await UpdateModAsync(mod, ModsFolderTextBox.Text, savedLinks, client);

                    if (result.Success)
                        updateResults.Add(result.Message);
                    else
                        await MessageService.ShowError(result.Message);

                    ModsDataGrid.Items.Refresh();

                    progress.Report(1.0);
                }
                catch (Exception ex)
                {
                    await MessageService.ShowError($"Failed to update {mod.Name}: {ex.Message}");
                }
            });

            // Show mods downloaded
            if (updateResults.Count > 0)
            {
                string summary = string.Join("\n", updateResults);
                await MessageService.ShowModOutput("Updating Mod Complete", "Finished updating mod!\n", summary);
            }
            else
            {
                await MessageService.ShowInfo("How?", "There shouldn't be any situation where you ever see this.\n\nThis means there were no mods to update on a single mod download.\n\nPlease make an issue on the github if you see this and tell me how you even managed to get here.");
            }
        }

        // ============ Check For Mod Update Helpers ============

        // Load Saved Mod Links From JSON
        private Dictionary<string, ModLinkInfo> LoadSavedModLinks()
        {
            if (!File.Exists(modlinksPath))
                return new Dictionary<string, ModLinkInfo>();

            string json = File.ReadAllText(modlinksPath);
            return JsonConvert.DeserializeObject<Dictionary<string, ModLinkInfo>>(json) ?? new Dictionary<string, ModLinkInfo>();
        }

        // Save Mod Links To JSON
        private void SaveModLinks(Dictionary<string, ModLinkInfo> links)
        {
            string json = JsonConvert.SerializeObject(links, Formatting.Indented);
            File.WriteAllText(modlinksPath, json);
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

        // ============ Mod Update Helpers ============
        private class ModUpdateResult
        {
            public bool Success { get; set; }
            public string Message { get; set; } = string.Empty;
            public ModInfo Mod { get; set; } = null!;
        }

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

        // Update Mod Logic
        private async Task<ModUpdateResult> UpdateModAsync(ModInfo mod, string modsFolder, Dictionary<string, ModLinkInfo> savedLinks, HttpClient client)
        {
            string zipPath = Path.Combine(modsFolder, mod.FileName);
            string? modId = null;

            // Read modId from modinfo.json
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
                return new ModUpdateResult { Success = false, Message = $"Error reading modId from {mod.FileName}: {ex.Message}", Mod = mod };
            }

            if (string.IsNullOrEmpty(modId))
                return new ModUpdateResult { Success = false, Message = $"Could not read modId for {mod.Name}, skipping update.", Mod = mod };

            if (!savedLinks.ContainsKey(modId) || string.IsNullOrWhiteSpace(savedLinks[modId].ModDownloadUrl))
                return new ModUpdateResult { Success = false, Message = $"No download link found for {mod.Name}, skipping update.", Mod = mod };

            string downloadUrl = savedLinks[modId].ModDownloadUrl;
            string tempFile;

            try
            {
                // Download the updated mod using helpers
                tempFile = await DownloadFileToTempAsync(downloadUrl, client);
            }
            catch (Exception ex)
            {
                return new ModUpdateResult { Success = false, Message = $"Failed to download {mod.Name}: {ex.Message}", Mod = mod };
            }

            try
            {
                // Delete the old mod modName.zip
                if (File.Exists(zipPath))
                    File.Delete(zipPath);

                // Move downloaded file to mods folder with correct name
                string destPath = Path.Combine(modsFolder, GetFileNameFromDownloadUrl(downloadUrl));
                File.Move(tempFile, destPath);

                // Store old mod version
                string oldVersion = mod.Version;

                // Update ModInfo model
                mod.FileName = Path.GetFileName(destPath);
                mod.Version = mod.LatestVersion;

                // Record old/new versions for summary
                return new ModUpdateResult { Success = true, Message = $"- Updated **{modId}** from **{oldVersion}** to **{mod.LatestVersion}**", Mod = mod };
            }
            catch (Exception ex)
            {
                return new ModUpdateResult { Success = false, Message = $"Failed to replace {mod.Name}: {ex.Message}", Mod = mod };
            }
        }

        // ============ Mods Folder Helpers ============

        // Select Mods Folder
        private async Task<bool> SelectModsFolderAsync()
        {
            using (var dialog = new FolderBrowserDialog())
            {
                dialog.Description = "Select your Vintage Story mods folder";
                dialog.ShowNewFolderButton = false;

                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    ModsFolderTextBox.Text = dialog.SelectedPath;
                    await LoadModsFromFolderAsync(dialog.SelectedPath);
                    selectedModPath = dialog.SelectedPath;

                    // Save selected path
                    SaveAppSettings(new AppSettings
                    {
                        ModsFolderPath = dialog.SelectedPath
                    });

                    return true;
                }
            }
            return false;
        }

        // Load Mods Folder
        private async Task LoadModsFromFolderAsync(string folderPath)
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

        // ============ Models ============

        // App Settings Model
        private class AppSettings
        {
            public string ModsFolderPath { get; set; } = "";
        }

        // DataGrid Model
        public class ModInfo
        {
            public string FileName { get; set; } = "";
            public string Name { get; set; } = "";
            public string Version { get; set; } = "";
            public string? Game { get; set; }
            public string? Description { get; set; }
            public string LatestVersion { get; set; } = "";

            public string ModPageUrl { get; set; } = "";
            public string ModDownloadUrl { get; set; } = "";

            public bool HasUpdate => !string.IsNullOrWhiteSpace(LatestVersion) && Version != LatestVersion;
        }

        // ModLink Model
        private class ModLinkInfo
        {
            public string ModPageUrl { get; set; } = "";
            public string ModDownloadUrl { get; set; } = "";
        }

        // End of class
    }
}