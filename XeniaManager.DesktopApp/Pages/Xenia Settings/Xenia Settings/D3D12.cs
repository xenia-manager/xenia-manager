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
        /// Loads the D3D12 Settings into the UI
        /// </summary>
        /// <param name="sectionTable">Portion of .toml file dedicated to D3D12 Settings</param>
        private void LoadD3D12Settings(TomlTable sectionTable)
        {
            // "d3d12_allow_variable_refresh_rate_and_tearing" setting
            if (sectionTable.ContainsKey("d3d12_allow_variable_refresh_rate_and_tearing"))
            {
                Log.Information($"d3d12_allow_variable_refresh_rate_and_tearing - {(bool)sectionTable["d3d12_allow_variable_refresh_rate_and_tearing"]}");
                chkD3D12VariableRefreshRate.IsChecked = (bool)sectionTable["d3d12_allow_variable_refresh_rate_and_tearing"];
            }

            // "d3d12_readback_resolve" setting
            if (sectionTable.ContainsKey("d3d12_readback_resolve"))
            {
                Log.Information($"d3d12_readback_resolve - {(bool)sectionTable["d3d12_readback_resolve"]}");
                chkD3D12ReadbackResolve.IsChecked = (bool)sectionTable["d3d12_readback_resolve"];
            }

            // "d3d12_queue_priority" setting
            if (sectionTable.ContainsKey("d3d12_queue_priority"))
            {
                Log.Information($"d3d12_queue_priority - {int.Parse(sectionTable["d3d12_queue_priority"].ToString())}");
                cmbD3D12QueuePriority.SelectedIndex = int.Parse(sectionTable["d3d12_queue_priority"].ToString());
            }
        }

        /// <summary>
        /// Saves the D3D12 Settings into the configuration file
        /// </summary>
        /// <param name="sectionTable">Portion of .toml file dedicated to D3D12 Settings</param>
        private void SaveD3D12Settings(TomlTable sectionTable)
        {
            // "d3d12_allow_variable_refresh_rate_and_tearing" setting
            if (sectionTable.ContainsKey("d3d12_allow_variable_refresh_rate_and_tearing"))
            {
                Log.Information($"d3d12_allow_variable_refresh_rate_and_tearing - {chkD3D12VariableRefreshRate.IsChecked}");
                sectionTable["d3d12_allow_variable_refresh_rate_and_tearing"] = chkD3D12VariableRefreshRate.IsChecked;
            }

            // "d3d12_readback_resolve" setting
            if (sectionTable.ContainsKey("d3d12_readback_resolve"))
            {
                Log.Information($"d3d12_readback_resolve - {chkD3D12ReadbackResolve.IsChecked}");
                sectionTable["d3d12_readback_resolve"] = chkD3D12ReadbackResolve.IsChecked;
            }

            // "d3d12_queue_priority" setting
            if (sectionTable.ContainsKey("d3d12_queue_priority"))
            {
                Log.Information($"d3d12_queue_priority - {(cmbD3D12QueuePriority.SelectedItem as ComboBoxItem).Content}");
                sectionTable["d3d12_queue_priority"] = cmbD3D12QueuePriority.SelectedIndex;
            }
        }
    }
}