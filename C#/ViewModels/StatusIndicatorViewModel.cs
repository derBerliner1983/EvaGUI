using NexusApp.Services;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Media;

namespace NexusApp.ViewModels
{
    /// <summary>
    /// ViewModel, das den Status der AD-Verbindung verwaltet und
    /// für die UI bereitstellt.
    /// Implementiert INotifyPropertyChanged, damit die UI auf Änderungen reagieren kann.
    /// </summary>
    public class StatusIndicatorViewModel : INotifyPropertyChanged
    {
        private readonly ConnectionService _connectionService;
        private bool _isAdConnected;

        /// <summary>
        /// Gibt an, ob die Verbindung zum Active Directory besteht.
        /// </summary>
        public bool IsAdConnected
        {
            get => _isAdConnected;
            private set
            {
                _isAdConnected = value;
                // Benachrichtigt die UI, dass sich diese Eigenschaft geändert hat.
                OnPropertyChanged();
                // Benachrichtigt die UI auch, dass sich die davon abhängigen Eigenschaften geändert haben.
                OnPropertyChanged(nameof(ConnectionStatusBrush));
                OnPropertyChanged(nameof(ConnectionStatusText));
            }
        }

        /// <summary>
        /// Gibt die Füllfarbe für die Status-LED zurück.
        /// Grün bei Erfolg, Rot bei Fehlschlag.
        /// </summary>
        public Brush ConnectionStatusBrush => IsAdConnected
            ? new SolidColorBrush(Color.FromRgb(34, 197, 94)) // Green
            : new SolidColorBrush(Color.FromRgb(239, 68, 68)); // Red

        /// <summary>
        /// Gibt den Text für die Statusanzeige zurück.
        /// </summary>
        public string ConnectionStatusText => IsAdConnected ? "AD erreichbar" : "AD nicht erreichbar";

        public event PropertyChangedEventHandler? PropertyChanged;

        public StatusIndicatorViewModel()
        {
            _connectionService = new ConnectionService();
        }

        /// <summary>
        /// Führt eine asynchrone Überprüfung der AD-Verbindung durch und aktualisiert den Status.
        /// </summary>
        public async Task CheckAdConnectionAsync()
        {
            IsAdConnected = await _connectionService.CheckADConnectionAsync();
        }

        /// <summary>
        /// Wird aufgerufen, um die Benutzeroberfläche über eine Eigenschaftsänderung zu informieren.
        /// </summary>
        /// <param name="propertyName">Der Name der Eigenschaft, die sich geändert hat.</param>
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
