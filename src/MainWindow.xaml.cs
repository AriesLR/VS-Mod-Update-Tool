using MahApps.Metro.Controls;
using Newtonsoft.Json.Linq;
using System.IO;
using System.IO.Compression;
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

            string GetValue(JObject obj, string key)
            {
                return obj.Properties()
                          .FirstOrDefault(p => string.Equals(p.Name, key, StringComparison.OrdinalIgnoreCase))
                          ?.Value?.ToString() ?? "";
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
                                var dependenciesToken = root.Properties()
                                                            .FirstOrDefault(p => string.Equals(p.Name, "dependencies", StringComparison.OrdinalIgnoreCase))
                                                            ?.Value as JObject;
                                if (dependenciesToken != null)
                                {
                                    gameVersion = dependenciesToken.Properties()
                                                                   .FirstOrDefault(p => string.Equals(p.Name, "game", StringComparison.OrdinalIgnoreCase))
                                                                   ?.Value?.ToString() ?? "";
                                }

                                var mod = new ModInfo
                                {
                                    FileName = Path.GetFileName(zipFile),
                                    Name = GetValue(root, "name"),
                                    Version = GetValue(root, "version"),
                                    Game = gameVersion,
                                    Description = GetValue(root, "description")
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