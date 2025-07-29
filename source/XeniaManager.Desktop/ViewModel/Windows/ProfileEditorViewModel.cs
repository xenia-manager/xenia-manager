using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

// Imported Libraries
using XeniaManager.Core.Profile;

namespace XeniaManager.Desktop.ViewModel.Windows;
public class ProfileEditorViewModel : INotifyPropertyChanged
{
    private ProfileInfo _profileInfo = null!;
    public ProfileInfo ProfileInfo
    {
        get => _profileInfo;
        set
        {
            if (_profileInfo != value)
            {
                _profileInfo = value;
                OnPropertyChanged();
            }
        }
    }

    private string _profileGamertag = string.Empty;
    public string ProfileGamertag
    {
        get => _profileGamertag;
        set
        {
            if (_profileGamertag != value)
            {
                _profileGamertag = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsValid));
            }
        }
    }
    public bool IsValid => !string.IsNullOrWhiteSpace(ProfileGamertag) && ProfileGamertag.Length <= 16 && Regex.IsMatch(ProfileGamertag, @"^[a-zA-Z][a-zA-Z0-9]*$");
    public bool GamertagChanged = false;
    public string ProfileLocation = string.Empty;

    public ProfileEditorViewModel(ProfileInfo profileInfo, string profileLocation)
    {
        ProfileInfo = profileInfo;
        ProfileGamertag = profileInfo.Gamertag;
        ProfileLocation = profileLocation;
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}