using System.Windows;

// Imported Libraries
using Tomlyn.Model;
using XeniaManager.Core;

namespace XeniaManager.Desktop.Views.Windows;

public partial class GameSettingsEditor
{
    private void LoadVulkanSettings(TomlTable vulkanSection)
    {
        // vulkan_allow_present_mode_immediate
        if (vulkanSection.ContainsKey("vulkan_allow_present_mode_immediate"))
        {
            Logger.Info($"vulkan_allow_present_mode_immediate - {vulkanSection["vulkan_allow_present_mode_immediate"]}");
            BrdVulkanAllowPresentModeImmediateSetting.Visibility = Visibility.Visible;
            ChkVulkanPresentModeImmediate.IsChecked = (bool)vulkanSection["vulkan_allow_present_mode_immediate"];
        }
        else
        {
            Logger.Warning("`vulkan_allow_present_mode_immediate` is missing from configuration file");
            BrdVulkanAllowPresentModeImmediateSetting.Visibility = Visibility.Collapsed;
        }
        
        // vulkan_allow_present_mode_mailbox
        if (vulkanSection.ContainsKey("vulkan_allow_present_mode_mailbox"))
        {
            Logger.Info($"vulkan_allow_present_mode_mailbox - {vulkanSection["vulkan_allow_present_mode_mailbox"]}");
            BrdVulkanAllowPresentModeMailboxSetting.Visibility = Visibility.Visible;
            ChkVulkanPresentModeMailbox.IsChecked = (bool)vulkanSection["vulkan_allow_present_mode_mailbox"];
        }
        else
        {
            Logger.Warning("`vulkan_allow_present_mode_mailbox` is missing from configuration file");
            BrdVulkanAllowPresentModeMailboxSetting.Visibility = Visibility.Collapsed;
        }
        
        // vulkan_allow_present_mode_fifo_relaxed
        if (vulkanSection.ContainsKey("vulkan_allow_present_mode_fifo_relaxed"))
        {
            Logger.Info($"vulkan_allow_present_mode_fifo_relaxed - {vulkanSection["vulkan_allow_present_mode_fifo_relaxed"]}");
            BrdVulkanAllowPresentModeFifoRelaxedSetting.Visibility = Visibility.Visible;
            ChkVulkanPresentModeFifoRelaxed.IsChecked = (bool)vulkanSection["vulkan_allow_present_mode_fifo_relaxed"];
        }
        else
        {
            Logger.Warning("`vulkan_allow_present_mode_fifo_relaxed` is missing from configuration file");
            BrdVulkanAllowPresentModeFifoRelaxedSetting.Visibility = Visibility.Collapsed;
        }
    }

    private void SaveVulkanSettings(TomlTable vulkanSection)
    {
        // vulkan_allow_present_mode_immediate
        if (vulkanSection.ContainsKey("vulkan_allow_present_mode_immediate"))
        {
            Logger.Info($"vulkan_allow_present_mode_immediate - {ChkVulkanPresentModeImmediate.IsChecked}");
            vulkanSection["vulkan_allow_present_mode_immediate"] = ChkVulkanPresentModeImmediate.IsChecked;
        }
        
        // vulkan_allow_present_mode_mailbox
        if (vulkanSection.ContainsKey("vulkan_allow_present_mode_mailbox"))
        {
            Logger.Info($"vulkan_allow_present_mode_mailbox - {ChkVulkanPresentModeMailbox.IsChecked}");
            vulkanSection["vulkan_allow_present_mode_mailbox"] = ChkVulkanPresentModeMailbox.IsChecked;
        }
        
        // vulkan_allow_present_mode_fifo_relaxed
        if (vulkanSection.ContainsKey("vulkan_allow_present_mode_fifo_relaxed"))
        {
            Logger.Info($"vulkan_allow_present_mode_fifo_relaxed - {ChkVulkanPresentModeFifoRelaxed.IsChecked}");
            vulkanSection["vulkan_allow_present_mode_fifo_relaxed"] = ChkVulkanPresentModeFifoRelaxed.IsChecked;
        }
    }
}