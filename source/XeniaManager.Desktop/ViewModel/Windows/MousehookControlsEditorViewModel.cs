using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using XeniaManager.Core;
using XeniaManager.Core.Constants;
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
            try
            {
                _windowIcon = new BitmapImage(new Uri("pack://application:,,,/Assets/64.png", UriKind.Absolute));
            }
            catch (Exception)
            {
                _windowIcon = null;
            }
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
                foreach (var kvp in gameKeyMapping.KeyBindings)
                {
                    foreach (var binding in kvp.Value)
                    {
                        Logger.Debug($"Keybinding: {kvp.Key} - {binding}");
                        KeyBindings.Add(new KeyBindingItem
                        {
                            Key = kvp.Key,
                            Value = binding
                        });
                    }
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
                // Clear all bindings for this mode
                keyMapping.KeyBindings.Clear();

                // Rebuild from KeyBindings collection
                foreach (var item in KeyBindings)
                {
                    keyMapping.AddKeyBinding(item.Key, item.Value);
                }
                break;
            }
        }
    }

    /// <summary>
    /// Helper method to add a new binding to a specific key
    /// </summary>
    /// <param name="key">The Xbox 360 key</param>
    /// <param name="binding">The keyboard/mouse binding to add</param>
    public void AddKeyBinding(string key, string binding)
    {
        // Check if the binding already exists for this key in the current mode
        foreach (GameKeyMapping keyMapping in GameKeyMappings)
        {
            if (keyMapping.Mode == SelectedKeybindingMode)
            {
                if (keyMapping.KeyBindings.TryGetValue(key, out var bindings))
                {
                    if (bindings.Contains(binding, StringComparer.OrdinalIgnoreCase))
                    {
                        Logger.Info($"Binding '{binding}' already exists for key '{key}'. Not adding duplicate.");
                        return; // Block duplicate
                    }
                }
                keyMapping.AddKeyBinding(key, binding);
                break;
            }
        }
        ReloadKeyBindings(); // Refresh the UI
    }

    /// <summary>
    /// Helper method to remove a specific binding from a key
    /// </summary>
    /// <param name="key">The Xbox 360 key</param>
    /// <param name="binding">The keyboard/mouse binding to remove</param>
    public void RemoveKeyBinding(string key, string binding)
    {
        foreach (GameKeyMapping keyMapping in GameKeyMappings)
        {
            if (keyMapping.Mode == SelectedKeybindingMode)
            {
                keyMapping.RemoveKeyBinding(key, binding);
                break;
            }
        }
        ReloadKeyBindings(); // Refresh the UI
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}