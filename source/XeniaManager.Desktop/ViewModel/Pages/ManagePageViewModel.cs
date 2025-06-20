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

    private bool _canaryInstalled;

    public bool CanaryInstalled
    {
        get=> _canaryInstalled;
        set
        {
            _canaryInstalled = value;
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

    private bool _mousehookInstalled;
    public bool MousehookInstalled
    {
        get => _mousehookInstalled;
        set
        {
            if (value == _mousehookInstalled)
            {
                return;
            }
            _mousehookInstalled = value;
            OnPropertyChanged();
        }
    }

    private string _mousehookVersionText;

    public string MousehookVersionText
    {
        get => _mousehookVersionText;
        set
        {
            _mousehookVersionText = value;
            OnPropertyChanged();
        }
    }

    private bool _mousehookInstall;

    public bool MousehookInstall
    {
        get => _mousehookInstall;
        set
        {
            _mousehookInstall = value;
            OnPropertyChanged();
        }
    }

    private bool _mousehookUninstall;

    public bool MousehookUninstall
    {
        get => _mousehookUninstall;
        set
        {
            _mousehookUninstall = value;
            OnPropertyChanged();
        }
    }

    private bool _mousehookUpdate;

    public bool MousehookUpdate
    {
        get => _mousehookUpdate;
        set
        {
            _mousehookUpdate = value;
            OnPropertyChanged();
        }
    }

    private bool _unifiedContentFolder = App.Settings.Emulator.Settings.UnifiedContentFolder;
    public bool UnifiedContentFolder
    {
        get => _unifiedContentFolder;
        set
        {
            // TODO: Remove true when releasing stable build
            if (value == null || value == _unifiedContentFolder || value != true)
            {
                return;
            }
            _unifiedContentFolder = value;
            App.Settings.Emulator.Settings.UnifiedContentFolder = value;
            App.AppSettings.SaveSettings();
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
        CanaryInstalled = App.Settings.Emulator.Canary?.Version != null;

        if (CanaryInstalled)
        {
            CanaryVersionText = App.Settings.Emulator.Canary.Version;
        }
        else
        {
            CanaryVersionText = LocalizationHelper.GetUiText("ManagePage_XeniaNotInstalled");
        }

        CanaryInstall = !CanaryInstalled;
        CanaryUninstall = CanaryInstalled;
        CanaryUpdate = CanaryInstalled && App.Settings.Emulator.Canary.UpdateAvailable;

        MousehookInstalled = App.Settings.Emulator.Mousehook?.Version != null;

        if (MousehookInstalled)
        {
            MousehookVersionText = App.Settings.Emulator.Mousehook.Version;
        }
        else
        {
            MousehookVersionText = LocalizationHelper.GetUiText("ManagePage_XeniaNotInstalled");
        }

        MousehookInstall = !MousehookInstalled;
        MousehookUninstall = MousehookInstalled;
        MousehookUpdate = MousehookInstalled && App.Settings.Emulator.Mousehook.UpdateAvailable;

        // TODO: Add updates for Mousehook and Netplay properties
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    #endregion
}