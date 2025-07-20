using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using XeniaManager.Core;
using XeniaManager.Core.Constants;





// Imported Libraries
using XeniaManager.Core.Game;
using XeniaManager.Core.Mousehook;
using Logger = XeniaManager.Core.Logger;

namespace XeniaManager.Desktop.ViewModel.Windows;

public class KeyBindingItem : INotifyPropertyChanged
{
    public string Key { get; set; } = string.Empty;

    private string _value = string.Empty;

    public string Value
    {
        get => _value;
        set
        {
            if (_value != value)
            {
                _value = value;
                OnPropertyChanged(nameof(Value)); // Notify the UI of the change
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public class MousehookControlsEditorViewModel : INotifyPropertyChanged
{
    public string Title { get; set; } = "Mousehook Controls";
    private ImageSource _windowIcon;
    public ImageSource WindowIcon
    {
        get => _windowIcon;
        set
        {
            _windowIcon = value;
            OnPropertyChanged(nameof(WindowIcon));
        }
    }
    public List<GameKeyMapping> GameKeyMappings { get; set; } = new List<GameKeyMapping>();

    private int _keybindingModeIndex = 0;
    public int KeybindingModeIndex
    {
        get => _keybindingModeIndex;
        set
        {
            if (_keybindingModeIndex == value)
            {
                return;
            }

            SaveKeyBindingChanges();
            _keybindingModeIndex = value;
            OnPropertyChanged(nameof(KeybindingModeIndex));
            ReloadKeyBindings();
        }
    }
    public ObservableCollection<string> KeybindingMode { get; set; } = new ObservableCollection<string>();
    public string SelectedKeybindingMode => KeybindingMode.Count > KeybindingModeIndex ? KeybindingMode[KeybindingModeIndex] : string.Empty;
    public ObservableCollection<KeyBindingItem> KeyBindings { get; set; } = new ObservableCollection<KeyBindingItem>();

    public MousehookControlsEditorViewModel(Game game, List<GameKeyMapping> gameKeyMappings)
    {
        Title = $"{game.Title} Controls";
        try
        {
            _windowIcon = ArtworkManager.CacheLoadArtwork(Path.Combine(DirectoryPaths.Base, game.Artwork.Icon));
        }
        catch (Exception ex)
        {
            Logger.Error($"{ex.Message}\nFull Error:\n{ex}");
            _windowIcon = new BitmapImage(new Uri("pack://application:,,,/Assets/64.png", UriKind.Absolute));
        }
        GameKeyMappings = gameKeyMappings;
        foreach (GameKeyMapping gameKeyMapping in GameKeyMappings)
        {
            Logger.Debug($"Found keybinding mode: {gameKeyMapping.Mode}");
            KeybindingMode.Add(gameKeyMapping.Mode);
        }
        ReloadKeyBindings();
    }

    public void ReloadKeyBindings()
    {
        Logger.Info($"Loading keybindings for {SelectedKeybindingMode}");
        KeyBindings.Clear();
        foreach (GameKeyMapping gameKeyMapping in GameKeyMappings)
        {
            if (gameKeyMapping.Mode == SelectedKeybindingMode)
            {
                foreach (string key in gameKeyMapping.KeyBindings.Keys)
                {
                    Logger.Debug($"Keybinding: {key} - {gameKeyMapping.KeyBindings[key]}");
                    KeyBindings.Add(new KeyBindingItem
                    {
                        Key = key,
                        Value = gameKeyMapping.KeyBindings[key]
                    });
                }
            }
        }
    }

    public void SaveKeyBindingChanges()
    {
        if (KeyBindings.Count <= 0)
        {
            return;
        }

        Logger.Info("Saving keybinding changes");
        foreach (GameKeyMapping keyMapping in GameKeyMappings)
        {
            if (keyMapping.Mode == SelectedKeybindingMode)
            {
                foreach (string key in keyMapping.KeyBindings.Keys)
                {
                    string? updatedBinding = KeyBindings.FirstOrDefault(keyBindingItem => keyBindingItem.Key == key)?.Value;
                    if (updatedBinding != null)
                    {
                        Logger.Debug($"{key}: {keyMapping.KeyBindings[key]} -> {updatedBinding}");
                        keyMapping.KeyBindings[key] = updatedBinding;
                    }
                }
                break;
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}