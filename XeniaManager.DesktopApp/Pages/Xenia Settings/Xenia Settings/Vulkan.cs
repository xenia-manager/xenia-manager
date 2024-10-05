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
        /// Loads the Vulkan Settings into the UI
        /// </summary>
        /// <param name="sectionTable">Portion of .toml file dedicated to Vulkan Settings</param>
        private void LoadVulkanSettings(TomlTable sectionTable)
        {
            // "vulkan_allow_present_mode_fifo_relaxed" setting
            if (sectionTable.ContainsKey("vulkan_allow_present_mode_fifo_relaxed"))
            {
                Log.Information($"vulkan_allow_present_mode_fifo_relaxed - {(bool)sectionTable["vulkan_allow_present_mode_fifo_relaxed"]}");
                chkVulkanPresentModeFIFORelaxed.IsChecked = (bool)sectionTable["vulkan_allow_present_mode_fifo_relaxed"];
            }

            // "vulkan_allow_present_mode_immediate" setting
            if (sectionTable.ContainsKey("vulkan_allow_present_mode_immediate"))
            {
                Log.Information($"vulkan_allow_present_mode_immediate - {(bool)sectionTable["vulkan_allow_present_mode_immediate"]}");
                chkVulkanPresentModeImmediate.IsChecked = (bool)sectionTable["vulkan_allow_present_mode_immediate"];
            }

            // "vulkan_allow_present_mode_mailbox" setting
            if (sectionTable.ContainsKey("vulkan_allow_present_mode_mailbox"))
            {
                Log.Information($"vulkan_allow_present_mode_mailbox - {(bool)sectionTable["vulkan_allow_present_mode_mailbox"]}");
                chkVulkanPresentModeMailbox.IsChecked = (bool)sectionTable["vulkan_allow_present_mode_mailbox"];
            }
        }

        /// <summary>
        /// Saves the Vulkan Settings into the configuration file
        /// </summary>
        /// <param name="sectionTable">Portion of .toml file dedicated to Vulkan Settings</param>
        private void SaveVulkanSettings(TomlTable sectionTable)
        {
            // "vulkan_allow_present_mode_fifo_relaxed" setting
            if (sectionTable.ContainsKey("vulkan_allow_present_mode_fifo_relaxed"))
            {
                Log.Information($"vulkan_allow_present_mode_fifo_relaxed - {chkVulkanPresentModeFIFORelaxed.IsChecked}");
                sectionTable["vulkan_allow_present_mode_fifo_relaxed"] = chkVulkanPresentModeFIFORelaxed.IsChecked;
            }

            // "vulkan_allow_present_mode_immediate" setting
            if (sectionTable.ContainsKey("vulkan_allow_present_mode_immediate"))
            {
                Log.Information($"vulkan_allow_present_mode_immediate - {chkVulkanPresentModeImmediate.IsChecked}");
                sectionTable["vulkan_allow_present_mode_immediate"] = chkVulkanPresentModeImmediate.IsChecked;
            }

            // "vulkan_allow_present_mode_mailbox" setting
            if (sectionTable.ContainsKey("vulkan_allow_present_mode_mailbox"))
            {
                Log.Information($"vulkan_allow_present_mode_mailbox - {chkVulkanPresentModeMailbox.IsChecked}");
                sectionTable["vulkan_allow_present_mode_mailbox"] = chkVulkanPresentModeMailbox.IsChecked;
            }
        }
    }
}