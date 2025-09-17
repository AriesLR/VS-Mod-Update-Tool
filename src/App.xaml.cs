using ControlzEx.Theming;
using MahApps.Metro.Theming;
using System.Windows;
using Application = System.Windows.Application;

namespace VSModUpdater
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var theme = ThemeManager.Current.AddLibraryTheme(new LibraryTheme(new Uri("pack://application:,,,/VS-Mod-Update-Tool;component/Resources/Themes/Dark.DarkOliveGreen.xaml"), MahAppsLibraryThemeProvider.DefaultInstance));

            ThemeManager.Current.ChangeTheme(this, theme);
        }
    }
}