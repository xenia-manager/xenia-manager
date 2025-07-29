using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media.Imaging;

// Imported Libraries
using XeniaManager.Core;
using XeniaManager.Core.Constants;
using XeniaManager.Core.Constants.Emulators;
using XeniaManager.Core.Enum;
using XeniaManager.Core.Game;
using XeniaManager.Core.Profile;
using static System.Runtime.InteropServices.JavaScript.JSType;
using Path = System.IO.Path;

namespace XeniaManager.Desktop.ViewModel.Windows;

public class FileItem
{
    #region Variables

    public string Name { get; set; }
    public string FullPath { get; set; }
    public bool IsDirectory { get; set; }
    public List<FileItem> Children { get; set; }

    #endregion

    #region Constructor

    public FileItem()
    {
        Children = new List<FileItem>();
    }

    #endregion
}

public class ContentViewerViewModel : INotifyPropertyChanged
{
    #region Variables
    private string _windowTitle;
    public string WindowTitle
    {
        get => _windowTitle;
        set
        {
            if (_windowTitle == value || value == null)
            {
                return;
            }
            _windowTitle = value;
            OnPropertyChanged();
        }
    }

    private BitmapImage _windowIcon;
    public BitmapImage WindowIcon
    {
        get => _windowIcon;
        set
        {
            if (_windowIcon == value || value == null)
            {
                return;
            }
            _windowIcon = value;
            OnPropertyChanged();
        }
    }

    private Game _game;

    public Game Game
    {
        get => _game;
        set
        {
            if (_game == value)
            {
                return;
            }
            _game = value;
            OnPropertyChanged();
        }
    }

    public Dictionary<string, string> ContentFolders { get; set; } = new Dictionary<string, string>
    {
        { "Saved Game", ContentType.SavedGame.ToHexString() },
        { "Downloadable Content", ContentType.DownloadableContent.ToHexString() },
        { "Installer", ContentType.Installer.ToHexString() }
    };

    private string _selectedContentType = ContentType.SavedGame.ToHexString();
    public string SelectedContentType
    {
        get => _selectedContentType;
        set
        {
            if (_selectedContentType == value || value == null)
            {
                return;
            }
            _selectedContentType = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(GamerProfilesVisibility));
        }
    }

    public string SelectedContentDisplayName
    {
        get => ContentFolders.FirstOrDefault(kvp => kvp.Value == _selectedContentType).Key ?? "Unknown";
    }

    public ObservableCollection<ProfileInfo> Profiles { get; set; } = [];

    private bool _profileSelected = false;
    public bool ProfileSelected
    {
        get => _profileSelected;
        set
        {
            if (value == null || value == _profileSelected)
            {
                return;
            }

            _profileSelected = value;
            OnPropertyChanged();
        }
    }
    public Visibility GamerProfilesVisibility
    {
        get => _selectedContentType == "00000001" ? Visibility.Visible : Visibility.Collapsed;
    }

    private ObservableCollection<FileItem> _files = [];

    public ObservableCollection<FileItem> Files
    {
        get => _files;
        set
        {
            if (_files == value) return;
            _files = value;
            OnPropertyChanged();
        }
    }

    #endregion

    #region Constructor
    public ContentViewerViewModel(Game game)
    {
        this.Game = game;
        _windowTitle = $"{Game.Title} Content";
        try
        {
            _windowIcon = ArtworkManager.CacheLoadArtwork(Path.Combine(DirectoryPaths.Base, Game.Artwork.Icon));
        }
        catch (Exception ex)
        {
            Logger.Error($"{ex.Message}\nFull Error:\n{ex}");
            try
            {
                _windowIcon = new BitmapImage(new Uri("pack://application:,,,/Assets/64.png", UriKind.Absolute));
            }
            catch (Exception)
            {
                _windowIcon = null;
            }
        }
        LoadProfiles(game.XeniaVersion);
    }

    #endregion

    public void LoadProfiles(XeniaVersion xeniaVersion)
    {
        string emulatorContentLocation = xeniaVersion switch
        {
            XeniaVersion.Canary => XeniaCanary.ContentFolderLocation,
            XeniaVersion.Mousehook => XeniaMousehook.ContentFolderLocation,
            XeniaVersion.Netplay => XeniaNetplay.ContentFolderLocation,
            _ => string.Empty
        };

        if (!Directory.Exists(emulatorContentLocation))
        {
            Logger.Error("Couldn't find emulator content folder");
            return;
        }

        string[] profileXuids = Directory.GetDirectories(emulatorContentLocation);
        foreach (string profileXuid in profileXuids)
        {
            string xuid = Path.GetFileName(profileXuid);
            if (xuid == "0000000000000000" || xuid.Length != 16)
            {
                continue;
            }
            if (File.Exists(Path.Combine(emulatorContentLocation, xuid, "FFFE07D1", "00010000", xuid, "Account")))
            {
                byte[] accountFile = File.ReadAllBytes(Path.Combine(emulatorContentLocation, xuid, "FFFE07D1", "00010000", xuid, "Account"));

                Logger.Debug($"Decrypting profile {xuid} with normal keys");
                ProfileInfo? profile = ProfileFile.Decrypt(accountFile, false);
                if (profile == null)
                {
                    Logger.Debug("Trying to decrypt profile with devkit keys");
                    profile = ProfileFile.Decrypt(accountFile, true);
                    if (profile == null)
                    {
                        Logger.Error("Couldn't decrypt profile. Skipping it");
                        continue;
                    }
                }
                profile.Xuid = Convert.ToUInt64(xuid, 16);
                Logger.Debug($"Profile has been decrypted: {profile.ToString()}");
                Profiles.Add(profile);

            }
        }
    }


    private List<FileItem> LoadChildrenDirectory(string mainDirectoryPath)
    {
        List<FileItem> items = new List<FileItem>();

        // Get all directories
        foreach (string directory in Directory.GetDirectories(mainDirectoryPath))
        {
            DirectoryInfo directoryInfo = new DirectoryInfo(directory);
            FileItem directoryItem = new FileItem
            {
                Name = directoryInfo.Name,
                FullPath = directoryInfo.FullName,
                IsDirectory = true,
                Children = LoadChildrenDirectory(directory) // Load subdirectories and files recursively
            };
            items.Add(directoryItem);
        }

        // Get all files
        foreach (string file in Directory.GetFiles(mainDirectoryPath))
        {
            FileInfo fileInfo = new FileInfo(file);
            FileItem fileItem = new FileItem
            {
                Name = fileInfo.Name,
                FullPath = fileInfo.FullName,
                IsDirectory = false
            };
            items.Add(fileItem);
        }
        return items;
    }

    public void LoadDirectory(string folderPath)
    {
        List<FileItem> children = LoadChildrenDirectory(folderPath);
        Files.Clear();
        foreach (FileItem child in children)
        {
            Files.Add(child);
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}