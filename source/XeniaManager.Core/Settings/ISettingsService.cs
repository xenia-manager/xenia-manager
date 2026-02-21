namespace XeniaManager.Core.Settings;

/// <summary>
/// Defines a contract for settings management services that handle loading, saving, and accessing application settings.
/// </summary>
/// <typeparam name="T">The type of settings object to manage, must be a class with a parameterless constructor.</typeparam>
public interface ISettingsService<T> where T : class, new()
{
    /// <summary>
    /// Gets the currently loaded settings instance.
    /// </summary>
    T Settings { get; }

    /// <summary>
    /// Loads settings from the persistent storage.
    /// </summary>
    /// <returns>The loaded settings instance, or default settings if loading fails.</returns>
    T LoadSettings();

    /// <summary>
    /// Asynchronously loads settings from the persistent storage.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation. The task result contains the loaded settings instance, or default settings if loading fails.</returns>
    Task<T> LoadSettingsAsync();

    /// <summary>
    /// Saves the current settings instance to persistent storage.
    /// </summary>
    void SaveSettings();

    /// <summary>
    /// Asynchronously saves the current settings instance to persistent storage.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task SaveSettingsAsync();

    /// <summary>
    /// Saves the provided settings instance to persistent storage.
    /// </summary>
    /// <param name="settings">The settings instance to save.</param>
    void SaveSettings(T settings);

    /// <summary>
    /// Asynchronously saves the provided settings instance to persistent storage.
    /// </summary>
    /// <param name="settings">The settings instance to save.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task SaveSettingsAsync(T settings);
}