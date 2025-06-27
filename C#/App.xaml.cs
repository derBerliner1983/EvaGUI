using System;
using System.Linq;
using System.Windows;

namespace NexusApp
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static void SwitchTheme(string themeName)
        {
            if (themeName == "System")
            {
                // Hier könnte Logik stehen, die das System-Theme erkennt.
                // Zur Vereinfachung verwenden wir 'Light'.
                themeName = "Light";
            }

            try
            {
                var appResources = Current.Resources;

                // Altes Theme-Dictionary entfernen
                var oldTheme = appResources.MergedDictionaries
                    .FirstOrDefault(d => d.Source != null && d.Source.OriginalString.Contains("Theme.xaml"));

                if (oldTheme != null)
                {
                    appResources.MergedDictionaries.Remove(oldTheme);
                }

                // Neues Theme-Dictionary hinzufügen
                var uri = new Uri($"Themes/{themeName}Theme.xaml", UriKind.Relative);
                ResourceDictionary theme = new ResourceDictionary { Source = uri };
                appResources.MergedDictionaries.Add(theme);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler beim Laden des Themes '{themeName}Theme.xaml': {ex.Message}");
            }
        }
    }
}
