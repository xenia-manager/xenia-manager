using System;
using System.IO;
using System.Windows;

// Imported
using NvAPIWrapper;
using NvAPIWrapper.DRS;
using NvAPIWrapper.Native.Exceptions;
using Serilog;

namespace Xenia_Manager.Classes
{
    public class NVAPI
    {
        /// <summary>
        /// Holds the current driver session
        /// </summary>
        private DriverSettingsSession session;

        /// <summary>
        /// Holds the driver profile for the specific app
        /// </summary>
        private DriverSettingsProfile profile;

        private ProfileApplication application;

        /// <summary>
        /// Initializes a session for grabbing and modifying settings
        /// </summary>
        public bool Initialize()
        {
            try
            {
                NVIDIA.Initialize();
                session = DriverSettingsSession.CreateAndLoad();
                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message + "\nFull Error:\n" + ex);
                return false;
            }
        }

        /// <summary>
        /// Finds the NVIDIA profile for Xenia
        /// </summary>
        public void FindAppProfile()
        {
            try
            {
                if (session == null)
                {
                    Log.Error("Session not initialized");
                    return;
                }
                this.profile = session.FindProfileByName("Xenia");
                // Check if every xenia version is in the Xenia profile
                string[] xeniaExecutableNames = new string[3] { "xenia.exe", "xenia_canary.exe", "xenia_canary_netplay.exe" };

                foreach (string executableName in xeniaExecutableNames)
                {
                    ProfileApplication test = session.FindApplication(executableName);
                    if (test.Profile.Name != "Xenia")
                    {
                        Log.Information($"{executableName} is not in the NVIDIA Xenia profile");
                        Log.Information("Adding it now");
                        this.application = ProfileApplication.CreateApplication(this.profile, executableName);
                    }
                }
                session.Save();
            }
            catch (NVIDIAApiException)
            {
                Log.Information("Profile not found");
                Log.Information($"Creating new profile for Xenia");
                this.profile = DriverSettingsProfile.CreateProfile(this.session, "Xenia");
                this.application = ProfileApplication.CreateApplication(this.profile, "xenia.exe");
                this.application = ProfileApplication.CreateApplication(this.profile, "xenia_canary.exe");
                this.application = ProfileApplication.CreateApplication(this.profile, "xenia_canary_netplay.exe");
                session.Save();
            }
        }

        /// <summary>
        /// Grabs the current value of the specified setting
        /// </summary>
        /// <param name="settingID"></param>
        public ProfileSetting GetSetting(object settingID)
        {
            try
            {
                if (profile == null) 
                {
                    Log.Error("Profile not initialized");
                    return null;
                };
                ProfileSetting setting = null;
                if (settingID is uint)
                {
                    setting = (ProfileSetting)this.profile.GetSetting((uint)settingID);
                }
                else if (settingID is KnownSettingId)
                {
                    setting = (ProfileSetting)this.profile.GetSetting((KnownSettingId)settingID);
                }
                return setting;
            }
            catch (NVIDIAApiException ex)
            {
                Log.Error(ex.Message + "\nFull Error:\n" + ex);
                return null;
            }
        }

        /// <summary>
        /// Applies a change to a setting
        /// </summary>
        /// <param name="settingID">ID of the setting</param>
        /// <param name="settingValue">The value we want to apply</param>
        public void SetSettingValue(object settingID, uint settingValue)
        {
            try
            {
                if (profile == null)
                {
                    Log.Error("Profile not initialized");
                    return;
                };
                if (settingID is uint)
                {
                    this.profile.SetSetting((uint)settingID, settingValue);
                }
                else if (settingID is KnownSettingId)
                {
                    this.profile.SetSetting((KnownSettingId)settingID, settingValue);
                }
                session.Save();
            }
            catch (NVIDIAApiException ex)
            {
                Log.Error(ex.Message + "\nFull Error:\n" + ex);
            }
        }
    }
}
