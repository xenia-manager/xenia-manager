// Imported Libraries
using NvAPIWrapper.DRS;
using NvAPIWrapper.Native.Exceptions;

namespace XeniaManager.Core.GPU.NVIDIA;

public static class NVAPI
{
    private static readonly string[] _xeniaExecutableNames = ["xenia.exe", "xenia_canary.exe", "xenia_canary_mousehook.exe", "xenia_canary_netplay.exe"];
    private const string _profileName = "Xenia";
    private static DriverSettingsSession _session { get; set; }
    private static DriverSettingsProfile _profile { get; set; }
    private static ProfileApplication _application { get; set; }

    public static bool Initialize()
    {
        try
        {
            Logger.Info("Initializing NVIDIA API");
            NvAPIWrapper.NVIDIA.Initialize();
            _session = DriverSettingsSession.CreateAndLoad();
            Logger.Info("NVIDIA API successfully initialized");
            return true;
        }
        catch (Exception ex)
        {
            Logger.Error($"{ex.Message}\nFull Error:\n{ex}");
            return false;
        }
    }

    public static void FindAppProfile()
    {
        try
        {
            if (_session == null)
            {
                Logger.Error("Session not initialized");
                if (!Initialize())
                {
                    return;
                }
            }

            Logger.Debug("Trying to find Xenia profile");
            _profile = _session.FindProfileByName(_profileName);
            Logger.Info($"Found Xenia profile: {_profile.Name}");
            foreach (string xeniaExecutableName in _xeniaExecutableNames)
            {
                ProfileApplication includedExecutable = _session.FindApplication(xeniaExecutableName);
                if (includedExecutable != null && includedExecutable.Profile.Name == _profileName)
                {
                    continue;
                }
                Logger.Warning($"{xeniaExecutableName} is not in the NVIDIA Xenia driver profile");
                _application = ProfileApplication.CreateApplication(_profile, xeniaExecutableName);
            }
        }
        catch (NVIDIAApiException)
        {
            try
            {
                Logger.Warning("Profile not found");
                Logger.Info("Creating new profile for Xenia");
                _profile = DriverSettingsProfile.CreateProfile(_session, _profileName);
                foreach (string xeniaExecutableName in _xeniaExecutableNames)
                {
                    Logger.Debug($"Adding {xeniaExecutableName} executable to Xenia profile");
                    _application = ProfileApplication.CreateApplication(_profile, xeniaExecutableName);
                }
            }
            catch (NVIDIANotSupportedException nvidiaNotSupportedException)
            {
                Logger.Error($"NVIDIA API not supported: {nvidiaNotSupportedException}");
            }
            catch (Exception ex)
            {
                Logger.Error($"{ex.Message}\nFull Error:\n{ex}");
            }
        }
        finally
        {
            if (_session != null)
            {
                try
                {
                    Logger.Info("Saving changes to the session");
                    _session.Save();
                }
                catch (NVIDIAApiException ex)
                {
                    Logger.Error($"Failed to save NVIDIA profile changes: {ex.Message}\nFull Error:\n{ex}");
                    Logger.Info("Continuing in read-only mode. Some GPU settings may not apply.");
                }
            }
        }
    }
    
    public static ProfileSetting GetSetting(NVAPI_SETTINGS setting)
    {
        try
        {
            if (_profile == null) 
            {
                Logger.Error("Profile not initialized");
                FindAppProfile();
            };
            return _profile.GetSetting((uint)setting);;
        }
        catch (NVIDIAApiException ex)
        {
            Logger.Error($"{ex.Message}\nFull Error:\n{ex}");
            return null;
        }
    }
    
    public static void SetSettingValue(NVAPI_SETTINGS setting, uint value)
    {
        try
        {
            if (_profile == null) 
            {
                Logger.Error("Profile not initialized");
                FindAppProfile();
            };
            _profile.SetSetting((uint)setting, value);
            _session.Save();
        }
        catch (NVIDIAApiException ex)
        {
            Logger.Error($"{ex.Message}\nFull Error:\n{ex}");
        }
    }
}