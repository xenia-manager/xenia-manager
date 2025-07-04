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

        private string _applicationVersion = $"{App.AppSettings.Settings.GetManagerVersion()}";

        public string ApplicationVersion
        {
            get => _applicationVersion;
            set
            {
                _applicationVersion = value;
                OnPropertyChanged();
            }
        }

        private bool _useExperimentalBuilds = App.Settings.UpdateCheckChecks.UseExperimentalBuild;

        public bool UseExperimentalBuilds
        {
            get => _useExperimentalBuilds;
            set
            {
                // TODO: Remove !value when releasing stable build
                if (value == null || !value)
                {
                    return;
                }
                _useExperimentalBuilds = value;
                OnPropertyChanged();
                App.Settings.UpdateCheckChecks.UseExperimentalBuild = value;
                App.AppSettings.SaveSettings();
            }
        }

        private bool _checkForUpdatesButtonVisible = !App.Settings.Notification.ManagerUpdateAvailable;

        public bool CheckForUpdatesButtonVisible
        {
            get => _checkForUpdatesButtonVisible;
            set
            {
                _checkForUpdatesButtonVisible = value;
                OnPropertyChanged();
            }
        }

        private bool _updateManagerButtonVisible = App.Settings.Notification.ManagerUpdateAvailable;

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