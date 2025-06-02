using System.ComponentModel;
using System.Runtime.CompilerServices;
using XeniaManager.Desktop.Utilities;

namespace XeniaManager.Desktop.ViewModel.Pages;

public class ManagePageViewModel : INotifyPropertyChanged
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

    private string _canaryVersionText;

    public string CanaryVersionText
    {
        get => _canaryVersionText;
        set
        {
            _canaryVersionText = value;
            OnPropertyChanged();
        }
    }

    private bool _canaryInstall;

    public bool CanaryInstall
    {
        get => _canaryInstall;
        set
        {
            _canaryInstall = value;
            OnPropertyChanged();
        }
    }

    private bool _canaryUninstall;

    public bool CanaryUninstall
    {
        get => _canaryUninstall;
        set
        {
            _canaryUninstall = value;
            OnPropertyChanged();
        }
    }

    private bool _canaryUpdate;

    public bool CanaryUpdate
    {
        get => _canaryUpdate;
        set
        {
            _canaryUpdate = value;
            OnPropertyChanged();
        }
    }

    #endregion

    #region Constructor

    public ManagePageViewModel()
    {
        UpdateEmulatorStatus();
    }

    #endregion

    #region Functions

    public void UpdateEmulatorStatus()
    {
        bool canaryInstalled = App.Settings.Emulator.Canary?.Version != null;

        if (canaryInstalled)
        {
            CanaryVersionText = App.Settings.Emulator.Canary.Version;
        }
        else
        {
            CanaryVersionText = LocalizationHelper.GetUiText("ManagePage_XeniaNotInstalled");
        }

        CanaryInstall = !canaryInstalled;
        CanaryUninstall = canaryInstalled;
        CanaryUpdate = canaryInstalled && App.Settings.Emulator.Canary.UpdateAvailable;

        // TODO: Add updates for Mousehook and Netplay properties
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    #endregion
}