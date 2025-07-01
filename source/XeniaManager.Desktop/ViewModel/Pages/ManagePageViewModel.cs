using System.ComponentModel;
using System.Runtime.CompilerServices;
using XeniaManager.Core;
using XeniaManager.Core.Game;
using XeniaManager.Core.Installation;
using XeniaManager.Desktop.Components;
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

    private bool _netplayInstalled;
    public bool NetplayInstalled
    {
        get => _netplayInstalled;
        set
        {
            if (value == _netplayInstalled)
            {
                return;
            }
            _netplayInstalled = value;
            OnPropertyChanged();
        }
    }

    private string _netplayVersionText;

    public string NetplayVersionText
    {
        get => _netplayVersionText;
        set
        {
            _netplayVersionText = value;
            OnPropertyChanged();
        }
    }

    private bool _netplayInstall;

    public bool NetplayInstall
    {
        get => _netplayInstall;
        set
        {
            _netplayInstall = value;
            OnPropertyChanged();
        }
    }

    private bool _netplayUninstall;

    public bool NetplayUninstall
    {
        get => _netplayUninstall;
        set
        {
            _netplayUninstall = value;
            OnPropertyChanged();
        }
    }

    private bool _netplayUpdate;

    public bool NetplayUpdate
    {
        get => _netplayUpdate;
        set
        {
            _netplayUpdate = value;
            OnPropertyChanged();
        }
    }

    private bool _netplayNightlyBuild = App.Settings.Emulator.Netplay?.UseNightlyBuild ?? false;
    public bool NetplayNightlyBuild
    {
        get => _netplayNightlyBuild;
        set
        {
            if (value == _netplayNightlyBuild || App.Settings.Emulator.Netplay == null)
            {
                return;
            }

            _netplayNightlyBuild = value;
            App.Settings.Emulator.Netplay.UseNightlyBuild = value;
            App.AppSettings.SaveSettings();
            OnPropertyChanged();
            UpdateEmulatorStatus();
        }
    }

    private bool _unifiedContentFolder = App.Settings.Emulator.Settings.UnifiedContentFolder;
    public bool UnifiedContentFolder
    {
        get => _unifiedContentFolder;
        set
        {
            if (value == null || value == _unifiedContentFolder)
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
        CanaryInstalled = App.Settings.Emulator.Canary != null;

        if (CanaryInstalled)
        {
            CanaryVersionText = App.Settings.Emulator.Canary?.CurrentVersion ?? string.Empty;
        }
        else
        {
            CanaryVersionText = LocalizationHelper.GetUiText("ManagePage_XeniaNotInstalled");
        }

        CanaryInstall = !CanaryInstalled;
        CanaryUninstall = CanaryInstalled;
        CanaryUpdate = CanaryInstalled && App.Settings.Emulator.Canary?.UpdateAvailable == true;

        MousehookInstalled = App.Settings.Emulator.Mousehook != null;

        if (MousehookInstalled)
        {
            MousehookVersionText = App.Settings.Emulator.Mousehook?.CurrentVersion ?? string.Empty;
        }
        else
        {
            MousehookVersionText = LocalizationHelper.GetUiText("ManagePage_XeniaNotInstalled");
        }

        MousehookInstall = !MousehookInstalled;
        MousehookUninstall = MousehookInstalled;
        MousehookUpdate = MousehookInstalled && App.Settings.Emulator.Mousehook?.UpdateAvailable == true;

        NetplayInstalled = App.Settings.Emulator.Netplay != null;

        if (NetplayInstalled)
        {
            NetplayVersionText = App.Settings.Emulator.Netplay?.CurrentVersion ?? string.Empty;
        }
        else
        {
            NetplayVersionText = LocalizationHelper.GetUiText("ManagePage_XeniaNotInstalled");
        }

        NetplayInstall = !NetplayInstalled;
        NetplayUninstall = NetplayInstalled;
        NetplayUpdate = NetplayInstalled && App.Settings.Emulator.Netplay?.UpdateAvailable == true;
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    #endregion
}