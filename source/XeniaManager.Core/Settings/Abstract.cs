﻿using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace XeniaManager.Core.Settings;
public abstract class AbstractSettings<T> where T : class, new()
{
    private readonly JsonSerializerOptions _jsonSerializerOptions;
    private readonly string _settingsPath;
    private T? _settings;

    protected virtual T DefaultSettings => new();
    public T Settings => _settings ??= LoadSettings();

    protected AbstractSettings(string fileName)
    {
        _jsonSerializerOptions = new JsonSerializerOptions
        {
            Converters = {new JsonStringEnumConverter()},
            WriteIndented = true
        };

        // Ensure the config directory exists
        if (!Directory.Exists(Constants.DirectoryPaths.Config))
        {
            Directory.CreateDirectory(Constants.DirectoryPaths.Config);
        }

        _settingsPath = Path.Combine(Constants.DirectoryPaths.Config, fileName);
    }

    /// <summary>
    /// Load the settings from the file. If the file does not exist or fails to load, the default settings are returned.
    /// </summary>
    public virtual T LoadSettings()
    {
        try
        {
            if (!File.Exists(_settingsPath))
            {
                // Save the default settings if the file is missing
                SaveSettings(DefaultSettings);
                return DefaultSettings;
            }

            string settingsSerialized = File.ReadAllText(_settingsPath);
            var settings = JsonSerializer.Deserialize<T>(settingsSerialized, _jsonSerializerOptions);
            return settings ?? DefaultSettings;
        }
        catch (Exception ex)
        {
            Logger.Error(ex, ex.Message);
            return DefaultSettings;
        }
    }

    /// <summary>
    /// Save the current settings into the file.
    /// </summary>
    public void SaveSettings() => SaveSettings(_settings ?? DefaultSettings);

    /// <summary>
    /// Save the provided settings instance into the file.
    /// </summary>
    public void SaveSettings(T settings)
    {
        try
        {
            string settingsSerialized = JsonSerializer.Serialize(settings, _jsonSerializerOptions);
            File.WriteAllText(_settingsPath, settingsSerialized);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, ex.Message);
        }
    }
}

[SupportedOSPlatform("windows")]
public class EncryptedStringJsonConverter : JsonConverter<string>
{
    private readonly byte[] _entropy = Encoding.UTF8.GetBytes("XeniaManager–Sęćrë†š💀☠️🧬");


    public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        string? encryptedBase64 = reader.GetString();
        if (string.IsNullOrEmpty(encryptedBase64))
        {
            return null;
        }

        byte[] encryptedBytes = Convert.FromBase64String(encryptedBase64);
        byte[] decrypted = ProtectedData.Unprotect(encryptedBytes, _entropy, DataProtectionScope.CurrentUser);

        return Encoding.UTF8.GetString(decrypted);
    }

    public override void Write(Utf8JsonWriter writer, string? value, JsonSerializerOptions options)
    {
        if (string.IsNullOrEmpty(value))
        {
            writer.WriteNullValue();
            return;
        }

        byte[] plaintextBytes = Encoding.UTF8.GetBytes(value);
        byte[] encryptedBytes = ProtectedData.Protect(plaintextBytes, _entropy, DataProtectionScope.CurrentUser);

        writer.WriteStringValue(Convert.ToBase64String(encryptedBytes));
    }
}