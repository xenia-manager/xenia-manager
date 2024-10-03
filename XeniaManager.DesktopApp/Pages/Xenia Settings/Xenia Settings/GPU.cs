using System;
using System.Windows;
using System.Windows.Automation;
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
        /// Loads the GPU Settings into the UI
        /// </summary>
        /// <param name="sectionTable">Portion of .toml file dedicated to GPU Settings</param>
        private void LoadGPUSettings(TomlTable sectionTable)
        {
            // "framerate_limit" setting
            if (sectionTable.ContainsKey("framerate_limit"))
            {
                Log.Information($"framerate_limit - {sectionTable["framerate_limit"].ToString()}");
                sldXeniaFramerate.Value = int.Parse(sectionTable["framerate_limit"].ToString());
                AutomationProperties.SetName(sldXeniaFramerate, $"Xenia Framerate Limiter: {sldXeniaFramerate.Value} FPS");
            }

            // "vsync" setting
            if (sectionTable.ContainsKey("vsync"))
            {
                Log.Information($"vsync - {sectionTable["vsync"]}");
                chkVSync.IsChecked = (bool)sectionTable["vsync"];
            }
        }
    }
}