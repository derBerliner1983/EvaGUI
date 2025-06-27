using NexusApp.Models;
using NexusApp.ViewModels;
using NexusApp.Views;
using NexusApp.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using MahApps.Metro.IconPacks;
using System.IO;

namespace NexusApp
{
    public partial class MainWindow : Window
    {
        private List<UserTask> _tasks;
        private string? _selectedTaskId;
        private readonly StatusIndicatorViewModel _statusViewModel;

        // HINZUGEFÜGT: Encrypted Storage Service
        private readonly EncryptedStorageService _storageService;

        public MainWindow()
        {
            InitializeComponent();

            // Services initialisieren
            _statusViewModel = new StatusIndicatorViewModel();
            _storageService = new EncryptedStorageService();

            AdStatusIndicator.DataContext = _statusViewModel;
            _tasks = new List<UserTask>();

            SetupConnectionCheckTimer();

            // HINZUGEFÜGT: Lade Tasks aus der Datenbank beim Start
            LoadTasksFromDatabaseAsync();

            RenderAll();

            Loaded += async (s, e) => {
                await _statusViewModel.CheckAdConnectionAsync();
            };
        }

        // HINZUGEFÜGT: Lade Tasks aus dem Storage
        private async Task LoadTasksFromDatabaseAsync()
        {
            try
            {
                var tasksFromStorage = await _storageService.LoadAllUserTasksAsync();
                _tasks.Clear();
                _tasks.AddRange(tasksFromStorage);

                if (string.IsNullOrEmpty(_selectedTaskId) && _tasks.Any())
                {
                    _selectedTaskId = _tasks.First().Id;
                }

                RenderAll();

                System.Diagnostics.Debug.WriteLine($"Loaded {_tasks.Count} tasks from storage");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler beim Laden der Aufgaben: {ex.Message}",
                    "Ladefehler", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void SetupConnectionCheckTimer()
        {
            DispatcherTimer timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(5)
            };
            timer.Tick += async (sender, e) => await _statusViewModel.CheckAdConnectionAsync();
            timer.Start();
        }

        // GEÄNDERT: AddTasks speichert jetzt auch in den Storage
        public async void AddTasks(IEnumerable<UserTask> newTasks)
        {
            try
            {
                // Speichere neue Tasks in den Storage
                var success = await _storageService.SaveUserTasksAsync(newTasks);

                if (success)
                {
                    _tasks.AddRange(newTasks);
                    if (string.IsNullOrEmpty(_selectedTaskId) && _tasks.Any())
                    {
                        _selectedTaskId = _tasks.First().Id;
                    }
                    RenderAll();

                    System.Diagnostics.Debug.WriteLine($"Added and saved {newTasks.Count()} tasks to storage");
                }
                else
                {
                    MessageBox.Show("Fehler beim Speichern der neuen Aufgaben.",
                        "Speicherfehler", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler beim Hinzufügen der Aufgaben: {ex.Message}",
                    "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // HINZUGEFÜGT: Einzelne Task hinzufügen (für manuellen Import)
        public async void AddTask(UserTask newTask)
        {
            try
            {
                var success = await _storageService.SaveUserTaskAsync(newTask);

                if (success)
                {
                    _tasks.Add(newTask);
                    if (string.IsNullOrEmpty(_selectedTaskId))
                    {
                        _selectedTaskId = newTask.Id;
                    }
                    RenderAll();

                    System.Diagnostics.Debug.WriteLine($"Added and saved task '{newTask.Name}' to storage");
                }
                else
                {
                    MessageBox.Show("Fehler beim Speichern der neuen Aufgabe.",
                        "Speicherfehler", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler beim Hinzufügen der Aufgabe: {ex.Message}",
                    "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // HINZUGEFÜGT: Task als abgeschlossen markieren
        public async void CompleteTask(string taskId, string completionNotes = "")
        {
            try
            {
                var success = await _storageService.CompleteUserTaskAsync(taskId, completionNotes);

                if (success)
                {
                    var task = _tasks.FirstOrDefault(t => t.Id == taskId);
                    if (task != null)
                    {
                        _tasks.Remove(task);

                        // Wähle nächste verfügbare Task aus
                        if (_selectedTaskId == taskId)
                        {
                            _selectedTaskId = _tasks.Any() ? _tasks.First().Id : null;
                        }

                        RenderAll();

                        MessageBox.Show($"Aufgabe '{task.Name}' wurde erfolgreich abgeschlossen.",
                            "Aufgabe abgeschlossen", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                else
                {
                    MessageBox.Show("Fehler beim Abschließen der Aufgabe.",
                        "Fehler", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler beim Abschließen der Aufgabe: {ex.Message}",
                    "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // HINZUGEFÜGT: Task löschen
        public async void DeleteTask(string taskId)
        {
            var task = _tasks.FirstOrDefault(t => t.Id == taskId);
            if (task == null) return;

            var result = MessageBox.Show($"Möchten Sie die Aufgabe '{task.Name}' wirklich löschen?",
                "Aufgabe löschen", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    var success = await _storageService.DeleteUserTaskAsync(taskId);

                    if (success)
                    {
                        _tasks.Remove(task);

                        if (_selectedTaskId == taskId)
                        {
                            _selectedTaskId = _tasks.Any() ? _tasks.First().Id : null;
                        }

                        RenderAll();
                    }
                    else
                    {
                        MessageBox.Show("Fehler beim Löschen der Aufgabe.",
                            "Fehler", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Fehler beim Löschen der Aufgabe: {ex.Message}",
                        "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void RenderAll()
        {
            if (!this.IsInitialized) return;
            RenderTaskList();
            RenderMainPanel();
        }

        #region UI Rendering (bleibt unverändert)
        private void RenderTaskList()
        {
            TaskList.Children.Clear();
            TaskListTitle.Text = $"Anstehende Aufgaben ({_tasks.Count})";

            foreach (var task in _tasks)
            {
                var border = new Border
                {
                    Style = (Style)FindResource(task.Id == _selectedTaskId ? "ActiveTaskItemStyle" : "TaskItemStyle"),
                    Tag = task.Id
                };
                border.MouseLeftButtonDown += (s, e) => {
                    if (s is FrameworkElement element && element.Tag is string tag)
                    {
                        _selectedTaskId = tag;
                        RenderAll();
                    }
                };

                var grid = new Grid();
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                var nameBlock = new TextBlock
                {
                    Text = task.Name,
                    FontWeight = FontWeights.SemiBold,
                    VerticalAlignment = VerticalAlignment.Center,
                };
                nameBlock.SetResourceReference(ForegroundProperty, task.Id == _selectedTaskId ? "Color.Accent.Blue.Text" : "Color.Text.Primary");
                Grid.SetColumn(nameBlock, 0);

                var badge = CreateBadge(task.Type);
                Grid.SetColumn(badge, 1);

                grid.Children.Add(nameBlock);
                grid.Children.Add(badge);
                border.Child = grid;
                TaskList.Children.Add(border);
            }
        }

        private void RenderMainPanel()
        {
            var task = _tasks.FirstOrDefault(t => t.Id == _selectedTaskId);

            if (MainActionPanel != null)
            {
                MainActionPanel.SetTaskType(task);
            }
        }

        private Border CreateBadge(TaskProcessType type)
        {
            string text;
            string backgroundKey, foregroundKey;

            switch (type)
            {
                case TaskProcessType.Versetzung:
                    text = "Versetzung";
                    backgroundKey = "Color.Badge.Yellow.Background";
                    foregroundKey = "Color.Badge.Yellow.Foreground";
                    break;
                case TaskProcessType.Austritt:
                    text = "Austritt";
                    backgroundKey = "Color.Badge.Red.Background";
                    foregroundKey = "Color.Badge.Red.Foreground";
                    break;
                case TaskProcessType.Einstellung:
                default:
                    text = "Einstellung";
                    backgroundKey = "Color.Badge.Green.Background";
                    foregroundKey = "Color.Badge.Green.Foreground";
                    break;
            }

            var textBlock = new TextBlock { Text = text, FontSize = 12, FontWeight = FontWeights.Medium, TextAlignment = TextAlignment.Center };
            textBlock.SetResourceReference(ForegroundProperty, foregroundKey);

            var border = new Border
            {
                Style = (Style)FindResource("StatusBadgeStyle"),
                Child = textBlock
            };
            border.SetResourceReference(BackgroundProperty, backgroundKey);

            return border;
        }
        #endregion

        #region Theme Logic
        private void ApplyTheme(string themeName)
        {
            App.SwitchTheme(themeName);

            if (ThemeIcon != null)
            {
                ThemeIcon.Kind = (themeName.ToLower() == "dark") ? PackIconMaterialKind.WeatherNight : PackIconMaterialKind.WhiteBalanceSunny;
            }

            RenderAll();
        }
        #endregion

        #region Event Handlers
        private void ImportExcelButton_Click(object sender, RoutedEventArgs e)
        {
            var importWindow = new ExcelImportWindow();
            importWindow.Owner = this;

            if (importWindow.ShowDialog() == true)
            {
                if (importWindow.ImportedTasks.Any())
                {
                    AddTasks(importWindow.ImportedTasks);
                }
            }
        }

        private void AddManualButton_Click(object sender, RoutedEventArgs e)
        {
            var manualAddWindow = new ManualAddWindow();
            manualAddWindow.Owner = this;

            if (manualAddWindow.ShowDialog() == true)
            {
                // Hier können Sie die neue Task aus dem Dialog holen
                // Das müssten Sie in ManualAddWindow implementieren
            }
        }

        private void ThemeToggleButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.ContextMenu != null)
            {
                button.ContextMenu.IsOpen = true;
            }
        }

        private void Theme_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem && menuItem.Tag is string themeName)
            {
                ApplyTheme(themeName);
            }
        }
        #endregion

        #region Enhanced Security Methods

        // Sicherheitsstatus anzeigen
        public async void ShowSecurityStatus()
        {
            try
            {
                var status = await _storageService.GetSecurityStatusAsync();
                var info = $@"
SICHERHEITSSTATUS:
{_storageService.GetSecurityInfo()}

Statistiken:
- Aktive Tasks: {status.TaskCount}
- Audit-Einträge: {status.AuditLogCount}
- Letzter Zugriff: {status.LastAccess:dd.MM.yyyy HH:mm}
- Secure Memory: {(status.SecureMemoryEnabled ? "Aktiviert" : "Deaktiviert")}
- Backup verfügbar: {(status.BackupAvailable ? "Ja" : "Nein")}
";

                MessageBox.Show(info, "Sicherheitsstatus", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler beim Laden des Sicherheitsstatus: {ex.Message}",
                    "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Sicheres Backup erstellen
        public async void CreateSecureBackup()
        {
            try
            {
                var dialog = new Microsoft.Win32.SaveFileDialog
                {
                    Title = "Sicheres Backup erstellen",
                    Filter = "Backup-Ordner|*.folder",
                    FileName = $"NexusBackup_{DateTime.Now:yyyyMMdd_HHmmss}"
                };

                if (dialog.ShowDialog() == true)
                {
                    var selectedPath = dialog.FileName;
                    if (string.IsNullOrEmpty(selectedPath))
                    {
                        MessageBox.Show("Ungültiger Pfad ausgewählt.", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    var backupPath = Path.GetDirectoryName(selectedPath);
                    if (string.IsNullOrEmpty(backupPath))
                    {
                        MessageBox.Show("Ungültiger Backup-Pfad.", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    var fileName = Path.GetFileNameWithoutExtension(selectedPath);
                    if (string.IsNullOrEmpty(fileName))
                    {
                        fileName = $"NexusBackup_{DateTime.Now:yyyyMMdd_HHmmss}";
                    }

                    var backupFolder = Path.Combine(backupPath, fileName);

                    var success = await _storageService.CreateSecureBackupAsync(backupFolder);

                    if (success)
                    {
                        MessageBox.Show($"Sicheres Backup erfolgreich erstellt:\n{backupFolder}",
                            "Backup erstellt", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show("Fehler beim Erstellen des Backups.",
                            "Backup-Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler beim Erstellen des Backups: {ex.Message}",
                    "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Alte Daten bereinigen (DSGVO)
        public async void CleanupOldData()
        {
            var result = MessageBox.Show(
                "Möchten Sie abgeschlossene Tasks älter als 1 Jahr löschen?\n\n" +
                "Dies dient der DSGVO-konformen Datenminimierung.",
                "Datenbereinigung", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    var deletedCount = await _storageService.CleanupOldDataAsync(TimeSpan.FromDays(365));

                    MessageBox.Show($"{deletedCount} alte Tasks wurden gelöscht.",
                        "Bereinigung abgeschlossen", MessageBoxButton.OK, MessageBoxImage.Information);

                    // Aktualisiere die UI nach der Bereinigung
                    await LoadTasksFromDatabaseAsync();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Fehler bei der Datenbereinigung: {ex.Message}",
                        "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        #endregion

        // HINZUGEFÜGT: Cleanup beim Schließen
        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            // Hier können Sie zusätzliche Cleanup-Aktionen durchführen
        }
    }
}