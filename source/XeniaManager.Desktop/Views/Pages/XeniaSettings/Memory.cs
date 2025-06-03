using System.Windows;

// Imported Libraries
using Tomlyn.Model;
using XeniaManager.Core;

namespace XeniaManager.Desktop.Views.Pages;

public partial class XeniaSettingsPage
{
    private void LoadMemorySettings(TomlTable memorySection)
    {
        // protect_zero
        if (memorySection.ContainsKey("protect_zero"))
        {
            Logger.Info($"protect_zero - {memorySection["protect_zero"]}");
            BrdProtectZeroPageSetting.Visibility = Visibility.Visible;
            ChkProtectZero.IsChecked = (bool)memorySection["protect_zero"];
        }
        else
        {
            Logger.Warning("'protect_zero' is missing from the configuration file");
            BrdProtectZeroPageSetting.Visibility = Visibility.Collapsed;
        }
        
        // scribble_heap
        if (memorySection.ContainsKey("scribble_heap"))
        {
            Logger.Info($"scribble_heap - {memorySection["scribble_heap"]}");
            BrdScribbleHeapSetting.Visibility = Visibility.Visible;
            ChkScribbleHeap.IsChecked = (bool)memorySection["scribble_heap"];
        }
        else
        {
            Logger.Warning("'scribble_heap' is missing from the configuration file");
            BrdScribbleHeapSetting.Visibility = Visibility.Collapsed;
        }
    }

    private void SaveMemorySettings(TomlTable memorySection)
    {
        // protect_zero
        if (memorySection.ContainsKey("protect_zero"))
        {
            Logger.Info($"protect_zero - {ChkProtectZero.IsChecked}");
            memorySection["protect_zero"] = ChkProtectZero.IsChecked;
        }
        
        // scribble_heap
        if (memorySection.ContainsKey("scribble_heap"))
        {
            Logger.Info($"scribble_heap - {ChkScribbleHeap.IsChecked}");
            memorySection["scribble_heap"] = ChkScribbleHeap.IsChecked;
        }
    }
}