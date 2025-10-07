using MahApps.Metro.Controls;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NuGet.Versioning;
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

        private int processedModsChecked;

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
            await CheckForModUpdatesLoud();
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

        private async void CopyModlist_Click(object sender, RoutedEventArgs e)
        {
            if (ModsDataGrid.ItemsSource is List<ModInfo> mods && mods.Count > 0)
            {
                // Handle user not checking for updates first (need to check to get urls)
                if (mods.Any(m => string.IsNullOrWhiteSpace(m.ModPageUrl)))
                {
                    await CheckForModUpdatesSilent();

                    mods = (ModsDataGrid.ItemsSource as List<ModInfo>)!;
                }

                // Build modlist + markdown
                var modlistMarkdown = string.Join(Environment.NewLine,
                    mods
                        .Where(m => !string.IsNullOrWhiteSpace(m.Name) && !string.IsNullOrWhiteSpace(m.ModPageUrl))
                        .Select(m => $"- [{m.Name}]({m.ModPageUrl})")
                );

                if (!string.IsNullOrWhiteSpace(modlistMarkdown))
                {
                    try
                    {
                        System.Windows.Clipboard.SetText(modlistMarkdown);
                        await MessageService.ShowInfo("Success", $"Total Mods: {processedModsChecked}\n\nMod list was copied to clipboard using markdown formatting.");
                    }
                    catch (Exception ex)
                    {
                        await MessageService.ShowError($"Failed to copy to clipboard: {ex.Message}");
                    }
                }
                else
                {
                    await MessageService.ShowError("No mod(s) had valid names/URLs to copy.");
                }
            }
            else
            {
                Debug.WriteLine("ModsDataGrid.ItemsSource was empty or null");
            }
        }

        // ============ Check For Mod Updates ============

        // Check for mod updates loud
        private Task CheckForModUpdatesLoud() => CheckModUpdatesAsync(true);

        // Check for mod updates silent
        private Task CheckForModUpdatesSilent() => CheckModUpdatesAsync(false);

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

        // Is Version Greater helper using Nuget.Versioning
        private bool IsVersionGreater(string v1, string v2)
        {
            if (NuGetVersion.TryParse(v1, out var version1) && NuGetVersion.TryParse(v2, out var version2))
            {
                return version1 > version2;
            }

            // If parsing fails, treat v1 as not greater, hopefully shouldn't happen
            return false;
        }

        // Mod update check logic
        private async Task CheckModUpdatesAsync(bool showMessage)
        {
            if (string.IsNullOrWhiteSpace(selectedModPath) || !Directory.Exists(selectedModPath))
            {
                if (showMessage)
                    await MessageService.ShowError("No mods folder selected.");
                return;
            }

            await LoadModsFromFolderAsync(selectedModPath);

            if (ModsDataGrid.ItemsSource is not List<ModInfo> mods || mods.Count == 0)
                return;

            var savedLinks = LoadSavedModLinks();
            var modsNeedingUpdate = new List<ModInfo>();
            var errors = new List<string>();
            int totalModsChecked = mods.Count;
            processedModsChecked = 0;

            string selectedVersion = (GameVersionDropdown.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "1.21.x";
            string selectedVersionPrefix = selectedVersion.Replace(".x", "");

            await MessageService.ShowProgress("Checking for mod updates", "Please wait...", async progress =>
            {
                using var client = new HttpClient();

                var tasks = mods.Select(async mod =>
                {
                    string zipPath = Path.Combine(ModsFolderTextBox.Text, mod.FileName);

                    try
                    {
                        using var archive = ZipFile.OpenRead(zipPath);
                        var entry = archive.GetEntry("modinfo.json")
                            ?? throw new InvalidDataException($"modinfo.json not found in {mod.FileName}");

                        using var reader = new StreamReader(entry.Open());
                        var json = await reader.ReadToEndAsync();
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

                        // Fetch API info
                        string apiUrl = $"https://mods.vintagestory.at/api/mod/{modId}";
                        string response = await client.GetStringAsync(apiUrl);
                        var apiData = JObject.Parse(response);

                        int assetId = apiData["mod"]?["assetid"]?.Value<int>() ?? 0;
                        if (assetId != 0)
                            mod.ModPageUrl = $"https://mods.vintagestory.at/show/mod/{assetId}";

                        var releases = apiData["mod"]?["releases"] as JArray;
                        if (releases != null && releases.Count > 0)
                        {
                            string? chosenVersion = null;
                            string? chosenDownloadUrl = null;

                            foreach (var release in releases)
                            {
                                var tags = release["tags"]?.Select(t => t.ToString()) ?? Enumerable.Empty<string>();

                                // Matches "1.21.x" style OR exact match like "1.21.1"
                                bool versionMatches = tags.Any(t =>
                                    t.Equals(selectedVersion, StringComparison.OrdinalIgnoreCase) ||
                                    t.StartsWith(selectedVersionPrefix, StringComparison.OrdinalIgnoreCase)
                                );

                                if (!versionMatches)
                                    continue;

                                string? releaseVersion = release["modversion"]?.ToString()?.TrimStart('v').Trim();
                                if (string.IsNullOrEmpty(releaseVersion))
                                    continue;

                                // Parse version safely
                                if (!NuGetVersion.TryParse(releaseVersion, out var parsedVersion))
                                    continue;

                                bool isPreRelease = parsedVersion.IsPrerelease;

                                // Prefer stable releases first
                                if (!isPreRelease)
                                {
                                    chosenVersion = parsedVersion.ToNormalizedString();
                                    chosenDownloadUrl = release["mainfile"]?.ToString();
                                    break; // stop at first stable match
                                }

                                // If no stable found yet, keep prerelease as fallback
                                if (chosenVersion == null)
                                {
                                    chosenVersion = parsedVersion.ToNormalizedString();
                                    chosenDownloadUrl = release["mainfile"]?.ToString();
                                }
                            }

                            if (chosenVersion != null && chosenDownloadUrl != null)
                            {
                                mod.LatestVersion = chosenVersion;
                                mod.ModDownloadUrl = chosenDownloadUrl;

                                lock (savedLinks)
                                {
                                    savedLinks[modId] = new ModLinkInfo
                                    {
                                        ModDownloadUrl = mod.ModDownloadUrl,
                                        ModPageUrl = mod.ModPageUrl
                                    };
                                }

                                if (IsVersionGreater(mod.LatestVersion, mod.Version))
                                    lock (modsNeedingUpdate) modsNeedingUpdate.Add(mod);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        lock (errors)
                        {
                            errors.Add($"Failed to check mod {mod.FileName}: {ex.Message}");
                        }
                    }
                    finally
                    {
                        int processed = Interlocked.Increment(ref processedModsChecked);
                        progress.Report((double)processed / totalModsChecked);
                    }
                });

                await Task.WhenAll(tasks);

                // Save updated links
                var sortedLinks = savedLinks.OrderBy(kvp => kvp.Key).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                SaveModLinks(sortedLinks);
                ModsDataGrid.Items.Refresh();

                if (showMessage)
                {
                    string message = modsNeedingUpdate.Count > 0
                        ? $"Total Mods: {totalModsChecked}\n\n{modsNeedingUpdate.Count} mod(s) need to be updated:\n\n- {string.Join("\n- ", modsNeedingUpdate.Select(m => m.Name))}"
                        : "All mods are up to date!";

                    await MessageService.ShowInfo("Update Check Complete", message);

                    if (errors.Count > 0)
                        await MessageService.ShowError(string.Join("\n", errors));
                }
            });
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