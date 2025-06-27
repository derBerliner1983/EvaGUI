using NexusApp.Models;
using NexusApp.ViewModels;
using NexusApp.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using MahApps.Metro.IconPacks;

namespace NexusApp
{
    public partial class MainWindow : Window
    {
        private List<UserTask> _tasks;
        private string? _selectedTaskId; // Als "nullable" deklariert

        private readonly StatusIndicatorViewModel _statusViewModel;

        public MainWindow()
        {
            InitializeComponent();
            _statusViewModel = new StatusIndicatorViewModel();
            AdStatusIndicator.DataContext = _statusViewModel;
            _tasks = new List<UserTask>(); // Direkt initialisiert

            SetupConnectionCheckTimer();
            RenderAll();

            Loaded += async (s, e) => {
                await _statusViewModel.CheckAdConnectionAsync();
            };
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

        public void AddTasks(IEnumerable<UserTask> newTasks)
        {
            _tasks.AddRange(newTasks);
            if (string.IsNullOrEmpty(_selectedTaskId) && _tasks.Any())
            {
                _selectedTaskId = _tasks.First().Id;
            }
            RenderAll();
        }

        private void RenderAll()
        {
            if (!this.IsInitialized) return;
            RenderTaskList();
            RenderMainPanel();
        }

        #region UI Rendering
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
                // Die SetTaskType Methode kann bereits mit einem null-Wert umgehen.
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
            manualAddWindow.ShowDialog();
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
    }
}
