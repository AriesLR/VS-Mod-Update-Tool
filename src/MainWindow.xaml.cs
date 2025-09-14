using MahApps.Metro.Controls;
using System.IO;
using System.IO.Compression;
using System.Text.Json;
using System.Windows;
using VSModUpdater.Resources.Functions.Services;

namespace VSModUpdater
{
    public partial class MainWindow : MetroWindow
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        // Model for the table data
        public class ModInfo
        {
            public string FileName { get; set; }
            public string Name { get; set; }
            public string Version { get; set; }
            public string Game { get; set; }
            public string Description { get; set; }
        }

        // Open Github Repo
        private void LaunchBrowserGitHubVSModUpdater(object sender, RoutedEventArgs e)
        {
            UrlService.OpenUrlAsync("https://github.com/AriesLR/VSModUpdater");
        }

        // Check For Updates
        private async void CheckForUpdates(object sender, RoutedEventArgs e)
        {
            await UpdateService.CheckForUpdatesAsync("https://raw.githubusercontent.com/AriesLR/VSModUpdater/refs/heads/main/docs/version/update.json");
        }

        // Browse for mods folder
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
                }
            }
        }

        // Load mods from folder
        private async void LoadModsFromFolder(string folderPath)
        {
            var mods = new List<ModInfo>();

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

                                using var doc = JsonDocument.Parse(json);
                                var root = doc.RootElement;

                                var mod = new ModInfo
                                {
                                    FileName = Path.GetFileName(zipFile),
                                    Name = root.TryGetProperty("name", out var name) ? name.GetString() : "",
                                    Version = root.TryGetProperty("version", out var version) ? version.GetString() : "",
                                    Game = root.TryGetProperty("dependencies", out var deps) && deps.TryGetProperty("game", out var game)
                                        ? game.GetString()
                                        : "",
                                    Description = root.TryGetProperty("description", out var desc) ? desc.GetString() : ""
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
    }
}