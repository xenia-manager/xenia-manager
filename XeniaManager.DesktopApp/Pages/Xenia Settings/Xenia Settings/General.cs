using System;
using System.Windows;
using System.Windows.Controls;

// Imported
using Serilog;
using Tomlyn.Model;

namespace XeniaManager.DesktopApp.Pages
{
    public partial class XeniaSettings : Page
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
                chkAllowPlugins.IsChecked = (bool)sectionTable["allow_plugins"];
            }

            // "apply_patches" setting
            if (sectionTable.ContainsKey("apply_patches"))
            {
                Log.Information($"apply_patches - {(bool)sectionTable["apply_patches"]}");
                chkApplyPatches.IsChecked = (bool)sectionTable["apply_patches"];
            }

            // "controller_hotkeys" setting
            if (sectionTable.ContainsKey("controller_hotkeys"))
            {
                Log.Information($"controller_hotkeys - {(bool)sectionTable["controller_hotkeys"]}");
                chkControllerHotkeys.IsChecked = (bool)sectionTable["controller_hotkeys"];
            }

            // "discord" setting
            if (sectionTable.ContainsKey("discord"))
            {
                Log.Information($"discord - {(bool)sectionTable["discord"]}");
                chkDiscordRPC.IsChecked = (bool)sectionTable["discord"];
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
                Log.Information($"allow_plugins - {chkAllowPlugins.IsChecked}");
                sectionTable["allow_plugins"] = chkAllowPlugins.IsChecked;
            }

            // "apply_patches" setting
            if (sectionTable.ContainsKey("apply_patches"))
            {
                Log.Information($"apply_patches - {chkApplyPatches.IsChecked}");
                sectionTable["apply_patches"] = chkApplyPatches.IsChecked;
            }

            // "controller_hotkeys" setting
            if (sectionTable.ContainsKey("controller_hotkeys"))
            {
                Log.Information($"controller_hotkeys - {chkControllerHotkeys.IsChecked}");
                sectionTable["controller_hotkeys"] = chkControllerHotkeys.IsChecked;
            }

            // "discord" setting
            if (sectionTable.ContainsKey("discord"))
            {
                Log.Information($"discord - {chkDiscordRPC.IsChecked}");
                sectionTable["discord"] = chkDiscordRPC.IsChecked;
            }
        }
    }
}