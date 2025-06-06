using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace XeniaManager.Desktop.ViewModel.Pages
{
    public class AboutPageViewModel : INotifyPropertyChanged
    {
        #region Variables

        private bool _isDownloading;

        public bool IsDownloading
        {
            get => _isDownloading;
            set
            {
                _isDownloading = value;
                OnPropertyChanged();
            }
        }
        
        private string _applicationVersion = $"v{App.AppSettings.Settings.GetInformationalVersion()}";

        public string ApplicationVersion
        {
            get => _applicationVersion;
            set
            {
                _applicationVersion = value;
                OnPropertyChanged();
            }
        }

        private bool _checkForUpdatesButtonVisible;

        public bool CheckForUpdatesButtonVisible
        {
            get => _checkForUpdatesButtonVisible;
            set
            {
                _checkForUpdatesButtonVisible = value;
                OnPropertyChanged();
            }
        }
        
        private bool _updateManagerButtonVisible;

        public bool UpdateManagerButtonVisible
        {
            get => _updateManagerButtonVisible;
            set
            {
                _updateManagerButtonVisible = value;
                OnPropertyChanged();
            }
        }
        
        #endregion

        #region Constructors

        public AboutPageViewModel()
        {
            CheckForUpdatesButtonVisible = !App.Settings.Notification.ManagerUpdateAvailable;
            UpdateManagerButtonVisible = App.Settings.Notification.ManagerUpdateAvailable;
        }
        #endregion

        #region Functions
        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion
    }
}
