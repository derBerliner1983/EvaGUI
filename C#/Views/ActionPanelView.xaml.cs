using NexusApp.Models;
using System.Windows;
using System.Windows.Controls;

namespace NexusApp.Views
{
    public partial class ActionPanelView : UserControl
    {
        public ActionPanelView()
        {
            InitializeComponent();
        }

        // KORRIGIERT: Die Methode akzeptiert jetzt einen 'nullable' UserTask (UserTask?),
        // um die Compiler-Warnung zu beheben.
        public void SetTaskType(UserTask? task)
        {
            if (task == null)
            {
                OnboardingPanelContent.Visibility = Visibility.Collapsed;
                TransferPanelContent.Visibility = Visibility.Collapsed;
                OffboardingPanelContent.Visibility = Visibility.Collapsed;
                return;
            }

            OnboardingPanelContent.Visibility = Visibility.Collapsed;
            TransferPanelContent.Visibility = Visibility.Collapsed;
            OffboardingPanelContent.Visibility = Visibility.Collapsed;

            switch (task.Type)
            {
                case TaskProcessType.Einstellung:
                    OnboardingPanelContent.Visibility = Visibility.Visible;
                    OnboardingTitle.Text = $"Onboarding: {task.Name}";
                    break;
                case TaskProcessType.Versetzung:
                    TransferPanelContent.Visibility = Visibility.Visible;
                    TransferTitle.Text = $"Versetzung: {task.Name}";
                    break;
                case TaskProcessType.Austritt:
                    OffboardingPanelContent.Visibility = Visibility.Visible;
                    OffboardingTitle.Text = $"Austritt: {task.Name}";
                    break;
            }
        }
    }
}
