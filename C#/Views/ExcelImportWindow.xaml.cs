using Microsoft.Win32;
using NexusApp.Models;
using NexusApp.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;

namespace NexusApp.Views
{
    public partial class ExcelImportWindow : Window
    {
        // Das Feld ist jetzt als "nullable" deklariert, um die Compiler-Warnung zu beheben.
        private string? _selectedFilePath;
        private readonly ExcelImportService _importService;

        public List<UserTask> ImportedTasks { get; private set; }

        public ExcelImportWindow()
        {
            InitializeComponent();
            _importService = new ExcelImportService();
            ImportedTasks = new List<UserTask>();
        }

        private void SelectFileButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "Excel-Dateien (*.xlsx;*.xls)|*.xlsx;*.xls|CSV-Dateien (*.csv)|*.csv|Alle Dateien (*.*)|*.*",
                Title = "Datei für den Import auswählen"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                _selectedFilePath = openFileDialog.FileName;
                FileNameText.Text = Path.GetFileName(_selectedFilePath);
                FileIcon.Visibility = Visibility.Visible;
                FileIcon.SetResourceReference(ForegroundProperty, "Color.Text.Primary");
            }
        }

        private void ImportButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_selectedFilePath))
            {
                MessageBox.Show("Bitte wählen Sie zuerst eine Datei aus.", "Keine Datei ausgewählt", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                // Ruft den Service auf, um die Aufgaben aus der Datei zu lesen.
                ImportedTasks = _importService.ImportTasksFromFile(_selectedFilePath);

                // Prüft das Ergebnis.
                if (ImportedTasks.Any())
                {
                    // ERFOLG: Nachricht anzeigen, Erfolg signalisieren und Fenster schließen.
                    MessageBox.Show($"{ImportedTasks.Count} Aufgabe(n) erfolgreich importiert.", "Import erfolgreich", MessageBoxButton.OK, MessageBoxImage.Information);
                    this.DialogResult = true;
                    this.Close();
                }
                else
                {
                    // FEHLER (Keine Daten): Nachricht anzeigen, Fenster offen lassen.
                    MessageBox.Show("In der ausgewählten Datei konnten keine gültigen Aufgaben gefunden werden. Bitte überprüfen Sie das Format und stellen Sie sicher, dass ein Datum in Spalte B vorhanden ist.", "Keine Daten gefunden", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                // FEHLER (Ausnahme): Detaillierte Fehlermeldung anzeigen, Fenster offen lassen.
                MessageBox.Show($"Ein unerwarteter Fehler ist aufgetreten: {ex.Message}", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}
