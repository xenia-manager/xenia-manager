using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using Path = System.IO.Path;
using System.Runtime.CompilerServices;

// Imported Libraries
using XeniaManager.Core;
using XeniaManager.Core.Game;

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
        { "Saved Game", "00000001" },
        { "Downloadable Content", "00000002" },
        //{"Publisher", "00000003"}, // Content published by a third party
        //{"Xbox360 Title", "00001000"}, // Xbox 360 title
        //{"Installed Game", "00040000"}, // 0x0004000
        //{"Xbox Original Game", "00050000"}, // 0x0005000
        //{"Xbox Title", "00050000"}, // Xbox Title, also used for Xbox Original games
        //{"Game On Demand", "00070000"}, // 0x0007000
        //{"Avatar Item", "00090000"}, // 0x0009000
        //{"Profile", "00100000"}, // 0x0010000
        //{"Gamer Picture", "00200000"}, // 0x0020000
        //{"Theme", "00300000"}, // 0x0030000
        //{"Storage Download", "00500000"}, // 0x0050000
        //{"Xbox Saved Game", "00600000"}, // 0x0060000
        //{"Xbox Download", "00700000"}, // 0x0070000
        //{"Game Demo", "00800000"}, // 0x0080000
        //{"Game Title", "00A00000"}, // 0x00A0000
        //{ "Installer", "00B00000" }, // 0x00B0000
        //{"Arcade Title", "00D00000"} // 0x00D0000
    };

    public ObservableCollection<GamerProfile> Profiles { get; set; } = [];

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
        LoadProfiles(game.XeniaVersion);
    }

    #endregion

    private void LoadProfiles(XeniaVersion xeniaVersion)
    {
        string emulatorContentLocation = xeniaVersion switch
        {
            XeniaVersion.Canary => Constants.Xenia.Canary.ContentFolderLocation,
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
            GamerProfile profile = new GamerProfile
            {
                Xuid = xuid
            };
            if (File.Exists(Path.Combine(emulatorContentLocation, xuid, "FFFE07D1", "00010000", xuid, "Account")))
            {
                byte[] accountFile = File.ReadAllBytes(Path.Combine(emulatorContentLocation, xuid, "FFFE07D1", "00010000", xuid, "Account"));
                if (!XboxProfileManager.TryDecryptAccountFile(accountFile, ref profile))
                {
                    Logger.Error($"Failed to decrypt account file {xuid}");
                    profile.Name = string.Empty;
                }
            }
            Logger.Debug($"Detected profile: {profile.Name} ({profile.Xuid})");
            Profiles.Add(profile);
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