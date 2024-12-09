// Imported
using Serilog;
using Tomlyn.Model;

namespace XeniaManager.DesktopApp.Pages
{
    public partial class XeniaSettings
    {
        // Functions for loading Settings into the UI
        /// <summary>
        /// Loads the General Settings into the UI
        /// </summary>
        /// <param name="sectionTable">Portion of .toml file dedicated to General Settings</param>
        private void LoadGeneralSettings(TomlTable sectionTable)
        {
            // "allow_plugins" setting
            if (sectionTable.ContainsKey("allow_plugins"))
            {
                Log.Information($"allow_plugins - {(bool)sectionTable["allow_plugins"]}");
                ChkAllowPlugins.IsChecked = (bool)sectionTable["allow_plugins"];
            }

            // "apply_patches" setting
            if (sectionTable.ContainsKey("apply_patches"))
            {
                Log.Information($"apply_patches - {(bool)sectionTable["apply_patches"]}");
                ChkApplyPatches.IsChecked = (bool)sectionTable["apply_patches"];
            }

            // "controller_hotkeys" setting
            if (sectionTable.ContainsKey("controller_hotkeys"))
            {
                Log.Information($"controller_hotkeys - {(bool)sectionTable["controller_hotkeys"]}");
                ChkControllerHotkeys.IsChecked = (bool)sectionTable["controller_hotkeys"];
            }

            // "discord" setting
            if (sectionTable.ContainsKey("discord"))
            {
                Log.Information($"discord - {(bool)sectionTable["discord"]}");
                ChkDiscordRpc.IsChecked = (bool)sectionTable["discord"];
            }
        }

        /// <summary>
        /// Saves the General Settings into the configuration file
        /// </summary>
        /// <param name="sectionTable">Portion of .toml file dedicated to General Settings</param>
        private void SaveGeneralSettings(TomlTable sectionTable)
        {
            // "allow_plugins" setting
            if (sectionTable.ContainsKey("allow_plugins"))
            {
                Log.Information($"allow_plugins - {ChkAllowPlugins.IsChecked}");
                sectionTable["allow_plugins"] = ChkAllowPlugins.IsChecked;
            }

            // "apply_patches" setting
            if (sectionTable.ContainsKey("apply_patches"))
            {
                Log.Information($"apply_patches - {ChkApplyPatches.IsChecked}");
                sectionTable["apply_patches"] = ChkApplyPatches.IsChecked;
            }

            // "controller_hotkeys" setting
            if (sectionTable.ContainsKey("controller_hotkeys"))
            {
                Log.Information($"controller_hotkeys - {ChkControllerHotkeys.IsChecked}");
                sectionTable["controller_hotkeys"] = ChkControllerHotkeys.IsChecked;
            }

            // "discord" setting
            if (sectionTable.ContainsKey("discord"))
            {
                Log.Information($"discord - {ChkDiscordRpc.IsChecked}");
                sectionTable["discord"] = ChkDiscordRpc.IsChecked;
            }
        }
    }
}