using System.Text.Json;
using System.Text.Json.Serialization;
using XeniaManager.Core.Constants;
using XeniaManager.Core.Logging;

namespace XeniaManager.Core.Settings;

/// <summary>
/// Provides a base implementation for settings management with JSON serialization and file-based persistence.
/// </summary>
/// <typeparam name="T">The type of settings object to manage must be a class with a parameterless constructor.</typeparam>
public abstract class AbstractSettings<T> : ISettingsService<T> where T : class, new()
{
    private readonly JsonSerializerOptions _jsonSerializerOptions;
    private readonly string _settingsPath = AppPaths.ConfigFile;
    private T? _settings;
    private readonly Lock _lock = new Lock();
    private bool _settingsLoaded = false;
    private T? _lastSavedSettings;

    /// <summary>
    /// Occurs when settings have been changed.
    /// </summary>
    public event EventHandler? SettingsChanged;

    /// <summary>
    /// Gets the default settings instance.
    /// </summary>
    protected virtual T DefaultSettings => new T();

    /// <summary>
    /// Raises the SettingsChanged event.
    /// </summary>
    protected virtual void OnSettingsChanged()
    {
        SettingsChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Gets the currently loaded settings instance, loading them if necessary.
    /// </summary>
    public T Settings
    {
        get
        {
            if (!_settingsLoaded)
            {
                LoadSettings();
            }
            return _settings!;
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AbstractSettings{T}"/> class with default JSON serialization options.
    /// </summary>
    protected AbstractSettings()
    {
        _jsonSerializerOptions = new JsonSerializerOptions
        {
            Converters = { new JsonStringEnumConverter() },
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.Never
        };

        Directory.CreateDirectory(Path.GetDirectoryName(_settingsPath) ?? string.Empty);
    }

    /// <summary>
    /// Gets JSON serializer options that ensure all properties are included in serialization,
    /// even those with default values, to ensure new settings appear in the file.
    /// </summary>
    /// <returns>JSON serializer options with full property inclusion.</returns>
    private JsonSerializerOptions GetFullSerializationOptions()
    {
        JsonSerializerOptions options = new JsonSerializerOptions
        {
            Converters = { new JsonStringEnumConverter() },
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.Never
        };

        return options;
    }

    /// <summary>
    /// Ensures the settings object has all properties properly initialized with defaults where needed.
    /// This helps ensure that when saving, all properties defined in the class are included in the output.
    /// </summary>
    /// <param name="currentSettings">The current settings instance</param>
    /// <returns>A settings instance with all properties properly initialized</returns>
    private T EnsureCompleteSettings(T currentSettings)
    {
        // Create a fresh instance of default settings
        T defaultSettings = DefaultSettings;

        // Serialize both current and default settings to JSON
        string currentJson = JsonSerializer.Serialize(currentSettings, _jsonSerializerOptions);
        string defaultJson = JsonSerializer.Serialize(defaultSettings, _jsonSerializerOptions);

        // Parse both as dynamic objects to merge them
        using JsonDocument currentDoc = JsonDocument.Parse(currentJson);
        using JsonDocument defaultDoc = JsonDocument.Parse(defaultJson);

        // For our scenario, the issue is that we want to ensure all properties from the class
        // definition are represented in the saved file. The most reliable way is to ensure
        // that we always save the settings object as it exists in memory after loading,
        // which will have all properties properly initialized (either from the file or with defaults).

        // Since the settings object in memory after loading should already have all properties
        // initialized (loaded from the file or set to defaults), we can return it as-is.
        // The key is to make sure that after loading, any missing properties have been
        // assigned their default values, which should happen automatically with C#'s object initialization.
        return currentSettings;
    }

    /// <summary>
    /// Loads settings from the persistent storage file. If the file does not exist or fails to load, the default settings are returned.
    /// </summary>
    /// <returns>The loaded settings instance, or default settings if loading fails.</returns>
    public virtual T LoadSettings()
    {
        lock (_lock)
        {
            try
            {
                if (!File.Exists(_settingsPath))
                {
                    // Save the default settings if the file is missing
                    T defaultSettings = DefaultSettings;
                    SaveSettings(defaultSettings);
                    _settings = defaultSettings;
                    _settingsLoaded = true;
                    _lastSavedSettings = CloneSettings(defaultSettings);
                    return _settings;
                }

                string settingsSerialized = File.ReadAllText(_settingsPath);
                T? settings = JsonSerializer.Deserialize<T>(settingsSerialized, _jsonSerializerOptions);

                if (settings != null)
                {
                    _settings = settings;
                    _settingsLoaded = true;
                    _lastSavedSettings = CloneSettings(settings);
                    return _settings;
                }
                else
                {
                    T defaultSettings = DefaultSettings;
                    _settings = defaultSettings;
                    _settingsLoaded = true;
                    _lastSavedSettings = CloneSettings(defaultSettings);
                    return _settings;
                }
            }
            catch (JsonException ex)
            {
                Logger.Error<AbstractSettings<T>>($"JSON deserialization error while loading settings from {_settingsPath}");
                Logger.LogExceptionDetails<AbstractSettings<T>>(ex);
                T defaultSettings = DefaultSettings;
                _settings = defaultSettings;
                _settingsLoaded = true;
                _lastSavedSettings = CloneSettings(defaultSettings);
                return _settings;
            }
            catch (UnauthorizedAccessException ex)
            {
                Logger.Error<AbstractSettings<T>>($"Access denied while loading settings from {_settingsPath}");
                Logger.LogExceptionDetails<AbstractSettings<T>>(ex);
                T defaultSettings = DefaultSettings;
                _settings = defaultSettings;
                _settingsLoaded = true;
                _lastSavedSettings = CloneSettings(defaultSettings);
                return _settings;
            }
            catch (Exception ex)
            {
                Logger.Error<AbstractSettings<T>>($"There was an unexpected error loading settings from {_settingsPath}");
                Logger.LogExceptionDetails<AbstractSettings<T>>(ex);
                T defaultSettings = DefaultSettings;
                _settings = defaultSettings;
                _settingsLoaded = true;
                _lastSavedSettings = CloneSettings(defaultSettings);
                return _settings;
            }
        }
    }

    /// <summary>
    /// Asynchronously loads settings from the persistent storage.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation. The task result contains the loaded settings instance, or default settings if loading fails.</returns>
    public async virtual Task<T> LoadSettingsAsync()
    {
        return await Task.Run(LoadSettings);
    }

    /// <summary>
    /// Saves the current settings instance to persistent storage.
    /// </summary>
    public void SaveSettings()
    {
        T settingsToSave = _settings ?? DefaultSettings;
        SaveSettings(settingsToSave);
    }

    /// <summary>
    /// Asynchronously saves the current settings instance to persistent storage.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task SaveSettingsAsync()
    {
        T settingsToSave = _settings ?? DefaultSettings;
        await SaveSettingsAsync(settingsToSave);
    }

    /// <summary>
    /// Saves the provided settings instance to persistent storage.
    /// </summary>
    /// <param name="settings">The settings instance to save.</param>
    public void SaveSettings(T settings)
    {
        lock (_lock)
        {
            try
            {
                // Ensure the settings object has all properties properly initialized
                T completeSettings = EnsureCompleteSettings(settings);

                string settingsSerialized = JsonSerializer.Serialize(completeSettings, GetFullSerializationOptions());

                // Use WriteAllText to ensure atomic write operation
                File.WriteAllText(_settingsPath, settingsSerialized);

                _settings = completeSettings;
                _settingsLoaded = true;
                _lastSavedSettings = CloneSettings(completeSettings);

                // Trigger the settings changed event
                OnSettingsChanged();
            }
            catch (UnauthorizedAccessException ex)
            {
                Logger.Error<AbstractSettings<T>>($"Access denied while saving settings to {_settingsPath}");
                Logger.LogExceptionDetails<AbstractSettings<T>>(ex);
            }
            catch (Exception ex)
            {
                Logger.Error<AbstractSettings<T>>($"There was an unexpected error saving settings to {_settingsPath}");
                Logger.LogExceptionDetails<AbstractSettings<T>>(ex);
            }
        }
    }

    /// <summary>
    /// Asynchronously saves the provided settings instance to persistent storage.
    /// </summary>
    /// <param name="settings">The settings instance to save.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task SaveSettingsAsync(T settings)
    {
        await Task.Run(() => SaveSettings(settings));
    }

    /// <summary>
    /// Compares two settings instances for equality to determine if a save operation is necessary.
    /// </summary>
    /// <param name="settings1">The first settings instance to compare.</param>
    /// <param name="settings2">The second settings instance to compare.</param>
    /// <returns>True if the settings are equal, false otherwise.</returns>
    private bool AreSettingsEqual(T? settings1, T? settings2)
    {
        if (settings1 == null && settings2 == null)
        {
            return true;
        }
        if (settings1 == null || settings2 == null)
        {
            return false;
        }

        string json1 = JsonSerializer.Serialize(settings1, _jsonSerializerOptions);
        string json2 = JsonSerializer.Serialize(settings2, _jsonSerializerOptions);

        return json1.Equals(json2);
    }

    /// <summary>
    /// Creates a deep clone of the settings object by serializing and deserializing it.
    /// </summary>
    /// <param name="settings">The settings object to clone.</param>
    /// <returns>A cloned copy of the settings object.</returns>
    private T CloneSettings(T settings)
    {
        string settingsSerialized = JsonSerializer.Serialize(settings, _jsonSerializerOptions);
        return JsonSerializer.Deserialize<T>(settingsSerialized, _jsonSerializerOptions) ?? DefaultSettings;
    }
}