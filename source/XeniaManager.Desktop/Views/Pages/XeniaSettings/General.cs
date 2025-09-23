using System.Windows;

// Imported Libraries
using Tomlyn.Model;
using XeniaManager.Core;

namespace XeniaManager.Desktop.Views.Pages;

public partial class XeniaSettingsPage
{
    private void LoadGeneralSettings(TomlTable generalSection)
    {
        // allow_plugins
        if (generalSection.ContainsKey("allow_plugins"))
        {
            Logger.Info($"allow_plugins - {generalSection["allow_plugins"]}");
            BrdAllowPluginsSetting.Visibility = Visibility.Visible;
            ChkAllowPlugins.IsChecked = (bool)generalSection["allow_plugins"];
        }
        else
        {
            Logger.Warning("'allow_plugins' is missing from the configuration file");
            BrdAllowPluginsSetting.Visibility = Visibility.Collapsed;
        }

        // discord
        if (generalSection.ContainsKey("discord"))
        {
            Logger.Info($"discord - {generalSection["discord"]}");
            BrdDiscordRpcSetting.Visibility = Visibility.Visible;
            ChkDiscordRpc.IsChecked = (bool)generalSection["discord"];
        }
        else
        {
            Logger.Warning("'discord' is missing from the configuration file");
            BrdDiscordRpcSetting.Visibility = Visibility.Collapsed;
        }

        // apply_patches
        if (generalSection.ContainsKey("apply_patches"))
        {
            Logger.Info($"apply_patches - {generalSection["apply_patches"]}");
            BrdGamePatchesSetting.Visibility = Visibility.Visible;
            ChkApplyPatches.IsChecked = (bool)generalSection["apply_patches"];
        }
        else
        {
            Logger.Warning("'apply_patches' is missing from the configuration file");
            BrdGamePatchesSetting.Visibility = Visibility.Collapsed;
        }
        
        // controller_hotkeys
        if (generalSection.ContainsKey("controller_hotkeys"))
        {
            Logger.Info($"controller_hotkeys - {generalSection["controller_hotkeys"]}");
            BrdControllerHotkeysSetting.Visibility = Visibility.Visible;
            ChkControllerHotkeys.IsChecked = (bool)generalSection["controller_hotkeys"];
        }
        else
        {
            Logger.Warning("'controller_hotkeys' is missing from the configuration file");
            BrdControllerHotkeysSetting.Visibility = Visibility.Collapsed;
        }

        // notification_sound_path
        if (generalSection.ContainsKey("notification_sound_path"))
        {
            Logger.Info($"notification_sound_path - {generalSection["notification_sound_path"]}");
            BrdNotificationSoundPathSetting.Visibility = Visibility.Visible;
            BtnChangeNotificationSoundPath.ToolTip = generalSection["notification_sound_path"].ToString();
        }
        else
        {
            Logger.Warning("'notification_sound_path' is missing from the configuration file");
            BrdNotificationSoundPathSetting.Visibility = Visibility.Collapsed;
        }
    }

    private void SaveGeneralSettings(TomlTable generalSection)
    {
        // allow_plugins
        if (generalSection.ContainsKey("allow_plugins"))
        {
            Logger.Info($"allow_plugins - {ChkAllowPlugins.IsChecked}");
            generalSection["allow_plugins"] = ChkAllowPlugins.IsChecked;
        }

        // discord
        if (generalSection.ContainsKey("discord"))
        {
            Logger.Info($"discord - {ChkDiscordRpc.IsChecked}");
            generalSection["discord"] = ChkDiscordRpc.IsChecked;
        }

        // apply_patches
        if (generalSection.ContainsKey("apply_patches"))
        {
            Logger.Info($"apply_patches - {ChkApplyPatches.IsChecked}");
            generalSection["apply_patches"] = ChkApplyPatches.IsChecked;
        }
        
        // controller_hotkeys
        if (generalSection.ContainsKey("controller_hotkeys"))
        {
            Logger.Info($"controller_hotkeys - {ChkControllerHotkeys.IsChecked}");
            generalSection["controller_hotkeys"] = ChkControllerHotkeys.IsChecked;
        }

        // notification_sound_path
        if (generalSection.ContainsKey("notification_sound_path"))
        {
            string path = BtnChangeNotificationSoundPath.ToolTip?.ToString() ?? string.Empty;
            Logger.Info($"notification_sound_path - {path}");
            generalSection["notification_sound_path"] = path;
        }
    }
}