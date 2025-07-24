using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using XeniaManager.Desktop.Utilities;

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

        private bool _useExperimentalBuilds = App.Settings.UpdateChecks.UseExperimentalBuild;

        public bool UseExperimentalBuilds
        {
            get => _useExperimentalBuilds;
            set
            {
                if (value == _useExperimentalBuilds)
                {
                    return;
                }
                _useExperimentalBuilds = value;
                App.Settings.UpdateChecks.UseExperimentalBuild = value;
                App.Settings.Notification.ManagerUpdateAvailable = false;
                CheckForUpdatesButtonVisible = !App.Settings.Notification.ManagerUpdateAvailable;
                UpdateManagerButtonVisible = App.Settings.Notification.ManagerUpdateAvailable;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ToggleText));
                OnPropertyChanged(nameof(CheckForUpdatesButtonVisible));
                OnPropertyChanged(nameof(UpdateManagerButtonVisible));
                App.AppSettings.SaveSettings();
                ApplicationVersion = $"{App.AppSettings.Settings.GetManagerVersion()}";
            }
        }

        public string ToggleText
        {
            get => UseExperimentalBuilds ? LocalizationHelper.GetUiText("ToggleVersionSwitch_Experimental") : LocalizationHelper.GetUiText("ToggleVersionSwitch_Stable");
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

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
}