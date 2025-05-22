using System.Windows;
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
    }
}