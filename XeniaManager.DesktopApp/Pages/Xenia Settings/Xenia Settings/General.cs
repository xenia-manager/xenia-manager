﻿using System.Windows;

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
                
                BrdAllowPluginsSetting.Visibility = Visibility.Visible;
                BrdAllowPluginsSetting.Tag = null;
            }
            else
            {
                Log.Warning("`allow_plugins` is missing from configuration file");
                BrdAllowPluginsSetting.Visibility = Visibility.Collapsed;
                BrdAllowPluginsSetting.Tag = "Ignore";
            }

            // "apply_patches" setting
            if (sectionTable.ContainsKey("apply_patches"))
            {
                Log.Information($"apply_patches - {(bool)sectionTable["apply_patches"]}");
                ChkApplyPatches.IsChecked = (bool)sectionTable["apply_patches"];
                
                BrdGamePatchesSetting.Visibility = Visibility.Visible;
                BrdGamePatchesSetting.Tag = null;
            }
            else
            {
                Log.Warning("`apply_patches` is missing from configuration file");
                BrdGamePatchesSetting.Visibility = Visibility.Collapsed;
                BrdGamePatchesSetting.Tag = "Ignore";
            }

            // "controller_hotkeys" setting
            if (sectionTable.ContainsKey("controller_hotkeys"))
            {
                Log.Information($"controller_hotkeys - {(bool)sectionTable["controller_hotkeys"]}");
                ChkControllerHotkeys.IsChecked = (bool)sectionTable["controller_hotkeys"];
                
                BrdControllerHotkeysSetting.Visibility = Visibility.Visible;
                BrdControllerHotkeysSetting.Tag = null;
            }
            else
            {
                Log.Warning("`controller_hotkeys` is missing from configuration file");
                BrdControllerHotkeysSetting.Visibility = Visibility.Collapsed;
                BrdControllerHotkeysSetting.Tag = "Ignore";
            }

            // "discord" setting
            if (sectionTable.ContainsKey("discord"))
            {
                Log.Information($"discord - {(bool)sectionTable["discord"]}");
                ChkDiscordRpc.IsChecked = (bool)sectionTable["discord"];
                
                BrdDiscordRpcSetting.Visibility = Visibility.Visible;
                BrdDiscordRpcSetting.Tag = null;
            }
            else
            {
                Log.Warning("`discord` is missing from configuration file");
                BrdDiscordRpcSetting.Visibility = Visibility.Collapsed;
                BrdDiscordRpcSetting.Tag = "Ignore";
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