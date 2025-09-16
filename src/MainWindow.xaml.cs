using MahApps.Metro.Controls;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Windows;
using System.Windows.Input;
using VSModUpdater.Resources.Functions.Services;
using HtmlDocument = HtmlAgilityPack.HtmlDocument;

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
        private void ModsFolderTextBox_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                this.DragMove();
        }

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

        // Model for JSON storage
        private class ModLinkInfo
        {
            public string UserLink { get; set; }
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

            await MessageService.ShowProgress("Checking for mod updates", "Please wait...", async progress =>
            {
                var savedLinks = LoadSavedModLinks();
                using var client = new HttpClient();

                int totalModsChecked = mods.Count;
                int processedModsChecked = 0;

                // List to keep track of mods that need updates
                var modsNeedingUpdate = new List<ModInfo>();

                foreach (var mod in mods)
                {
                    // Read modId from modName.zip
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
                            modId = root.Properties().FirstOrDefault(p => string.Equals(p.Name, "modId", StringComparison.OrdinalIgnoreCase))?.Value?.ToString();
                        }
                        else
                        {
                            await MessageService.ShowError($"No modinfo.json found in {mod.FileName}, skipping this mod.");
                        }
                    }
                    catch (InvalidDataException ex)
                    {
                        await MessageService.ShowError($"Corrupt ZIP file: {mod.FileName}\n{ex.Message}");
                    }
                    catch (JsonException ex)
                    {
                        await MessageService.ShowError($"Invalid JSON in modinfo.json for {mod.FileName}\n{ex.Message}");
                    }
                    catch (Exception ex)
                    {
                        await MessageService.ShowError($"Unexpected error reading {mod.FileName}\n{ex.Message}");
                    }

                    if (!string.IsNullOrEmpty(modId))
                    {
                        string modLink = null;

                        // If the ModLinks.json contains a link, use it.
                        if (savedLinks.ContainsKey(modId))
                        {
                            modLink = savedLinks[modId].UserLink;
                        }
                        else
                        {
                            // Keep prompting until valid link entered or user skips
                            while (true)
                            {
                                modLink = await MessageService.ShowInput("Enter Mod Link", $"Please enter the mod link for \"{mod.Name}\" " + $"\n\n(e.g. https://mods.vintagestory.at/combatoverhaul\n Or \n https://mods.vintagestory.at/show/mod/463):");

                                if (!string.IsNullOrWhiteSpace(modLink))
                                {
                                    savedLinks[modId] = new ModLinkInfo { UserLink = modLink };
                                    SaveModLinks(savedLinks);
                                    break;
                                }

                                // Ask if user wants to skip this mod
                                bool skip = await MessageService.ShowYesNo("Skip Mod?", $"You did not enter a link for \"{mod.Name}\".\nDo you want to skip this mod?");

                                if (skip)
                                {
                                    modLink = null;
                                    break;
                                }
                            }
                        }

                        if (!string.IsNullOrWhiteSpace(modLink))
                        {
                            // Ensure it has #tab-files suffix, this is likely not needed since the html is exposed from the main link
                            if (!modLink.Contains("#tab-files"))
                                modLink += "#tab-files";

                            try
                            {
                                // Fetch mod page
                                string modPageHtml = await client.GetStringAsync(modLink);
                                var modDoc = new HtmlDocument();
                                modDoc.LoadHtml(modPageHtml);

                                // Grab release table rows
                                var fileRows = modDoc.DocumentNode.SelectNodes("//div[contains(@class,'tab-content files')]//table[contains(@class,'release-table')]//tr");

                                if (fileRows != null)
                                {
                                    foreach (var row in fileRows)
                                    {
                                        // Skip header row
                                        if (row.SelectNodes(".//th") != null)
                                            continue;

                                        var versionTd = row.SelectSingleNode(".//td[1]");
                                        string latestVersion = versionTd?.InnerText.Trim() ?? "";

                                        var downloadNode = row.SelectSingleNode(".//a[contains(@class,'mod-dl')]");
                                        string downloadLink = downloadNode != null ? "https://mods.vintagestory.at" + downloadNode.GetAttributeValue("href", "") : "";

                                        if (!string.IsNullOrEmpty(latestVersion) && !string.IsNullOrEmpty(downloadLink))
                                        {
                                            // Update ModInfo model
                                            mod.LatestVersion = latestVersion;
                                            mod.DownloadLink = downloadLink;

                                            // Save download link into JSON
                                            if (savedLinks.ContainsKey(modId))
                                                savedLinks[modId].DownloadLink = downloadLink;
                                            else
                                                savedLinks[modId] = new ModLinkInfo { UserLink = modLink, DownloadLink = downloadLink };

                                            SaveModLinks(savedLinks);
                                            break;
                                        }
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                await MessageService.ShowError($"Failed to parse mod page for {mod.Name}: {ex.Message}");
                            }
                        }
                    }

                    // Track mods that need updates
                    if (mod.HasUpdate)
                        modsNeedingUpdate.Add(mod);

                    // Update progress after each mod
                    processedModsChecked++;
                    progress.Report((double)processedModsChecked / totalModsChecked);
                }

                // Refresh DataGrid
                ModsDataGrid.Items.Refresh();

                // Build summary message
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

                // Inform user the operation is complete
                await MessageService.ShowInfo("Update Check Complete", message);
            });
        }

        //////////////////////////////
        // UPDATE ALL MODS SECTION  //
        //////////////////////////////

        // Update All Mods
        private async void UpdateAllModsButton_Click(object sender, RoutedEventArgs e)
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
                    string tempFile = Path.Combine(Path.GetTempPath(), Path.GetFileName(downloadUrl));

                    try
                    {
                        // Download the updated mod
                        using var response = await client.GetAsync(downloadUrl);
                        response.EnsureSuccessStatusCode();

                        using var fs = new FileStream(tempFile, FileMode.Create, FileAccess.Write, FileShare.None);
                        await response.Content.CopyToAsync(fs);
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

                        // Move downloaded file to mods folder
                        string destPath = Path.Combine(ModsFolderTextBox.Text, Path.GetFileName(tempFile));
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
            string tempFile = Path.Combine(Path.GetTempPath(), Path.GetFileName(downloadUrl));

            try
            {
                // Download the mod
                using var response = await client.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead);
                response.EnsureSuccessStatusCode();

                using var fs = new FileStream(tempFile, FileMode.Create, FileAccess.Write, FileShare.None);
                await response.Content.CopyToAsync(fs);
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

                // Move downloaded file
                string destPath = Path.Combine(ModsFolderTextBox.Text, Path.GetFileName(tempFile));
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