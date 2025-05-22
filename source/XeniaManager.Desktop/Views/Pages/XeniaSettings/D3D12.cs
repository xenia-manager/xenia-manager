using System.Windows;
using Tomlyn.Model;
using XeniaManager.Core;

namespace XeniaManager.Desktop.Views.Pages;

public partial class XeniaSettingsPage
{
    private void LoadD3D12Settings(TomlTable d3d12Section)
    {
        // d3d12_allow_variable_refresh_rate_and_tearing
        if (d3d12Section.ContainsKey("d3d12_allow_variable_refresh_rate_and_tearing"))
        {
            Logger.Info($"d3d12_allow_variable_refresh_rate_and_tearing - {d3d12Section["d3d12_allow_variable_refresh_rate_and_tearing"]}");
            BrdD3D12AllowVariableRefreshRateTearingSetting.Visibility = Visibility.Visible;
            ChkD3D12VariableRefreshRate.IsChecked = (bool)d3d12Section["d3d12_allow_variable_refresh_rate_and_tearing"];
        }
        else
        {
            Logger.Warning("`d3d12_allow_variable_refresh_rate_and_tearing` is missing from configuration file");
            BrdD3D12AllowVariableRefreshRateTearingSetting.Visibility = Visibility.Collapsed;
        }
        
        // d3d12_queue_priority
        if (d3d12Section.ContainsKey("d3d12_queue_priority"))
        {
            Logger.Info($"d3d12_queue_priority - {d3d12Section["d3d12_queue_priority"]}");
            try
            {
                CmbD3D12QueuePriority.SelectedIndex = int.Parse(d3d12Section["d3d12_queue_priority"].ToString());
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                CmbD3D12QueuePriority.SelectedIndex = 1;
            }
        }
        else
        {
            Logger.Warning("`d3d12_queue_priority` is missing from configuration file");
            BrdD3D12QueuePrioritySetting.Visibility = Visibility.Collapsed;
        }
    }

    private void SaveD3D12Settings(TomlTable d3d12Section)
    {
        // d3d12_allow_variable_refresh_rate_and_tearing
        if (d3d12Section.ContainsKey("d3d12_allow_variable_refresh_rate_and_tearing"))
        {
            Logger.Info($"d3d12_allow_variable_refresh_rate_and_tearing - {ChkD3D12VariableRefreshRate.IsChecked}");
            d3d12Section["d3d12_allow_variable_refresh_rate_and_tearing"] = ChkD3D12VariableRefreshRate.IsChecked;
        }
        
        // d3d12_queue_priority
        if (d3d12Section.ContainsKey("d3d12_queue_priority"))
        {
            Logger.Info($"d3d12_queue_priority - {CmbD3D12QueuePriority.SelectedIndex}");
            d3d12Section["d3d12_queue_priority"] = CmbD3D12QueuePriority.SelectedIndex;
        }
    }
}