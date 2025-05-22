using System.Windows;
using Tomlyn.Model;
using XeniaManager.Core;

namespace XeniaManager.Desktop.Views.Pages;

public partial class XeniaSettingsPage
{
    private void LoadStorageSettings(TomlTable storageSection)
    {
        // mount_cache
        if (storageSection.ContainsKey("mount_cache"))
        {
            Logger.Info($"mount_cache - {storageSection["mount_cache"]}");
            BrdMountCacheSetting.Visibility = Visibility.Visible;
            ChkMountCache.IsChecked = (bool)storageSection["mount_cache"];
        }
        else
        {
            Logger.Warning("'mount_cache' is missing from the configuration file");
            BrdMountCacheSetting.Visibility = Visibility.Collapsed;
        }
        
        // mount_scratch
        if (storageSection.ContainsKey("mount_scratch"))
        {
            Logger.Info($"mount_scratch - {storageSection["mount_scratch"]}");
            BrdMountScratchSetting.Visibility = Visibility.Visible;
            ChkMountScratch.IsChecked = (bool)storageSection["mount_scratch"];
        }
        else
        {
            Logger.Warning("'mount_scratch' is missing from the configuration file");
            BrdMountScratchSetting.Visibility = Visibility.Collapsed;
        }
    }

    private void SaveStorageSettings(TomlTable storageSection)
    { 
        // mount_cache
        if (storageSection.ContainsKey("mount_cache"))
        {
            Logger.Info($"mount_cache - {ChkMountCache.IsChecked}");
            storageSection["mount_cache"] = ChkMountCache.IsChecked;
        }
        
        // mount_scratch
        if (storageSection.ContainsKey("mount_scratch"))
        {
            Logger.Info($"mount_scratch - {ChkMountScratch.IsChecked}");
            storageSection["mount_scratch"] = ChkMountScratch.IsChecked;
        }
    }
}