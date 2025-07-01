using System.Windows;

// Imported Libraries
using Tomlyn.Model;
using XeniaManager.Core;

namespace XeniaManager.Desktop.Views.Windows;

public partial class GameSettingsEditor
{
    private void LoadLiveSettings(TomlTable liveSection)
    {
        _viewModel.NetplaySettingsVisibility = Visibility.Visible;

        // api_list
        if (liveSection.ContainsKey("api_list"))
        {
            Logger.Info($"api_list - {liveSection["api_list"]}");
            CmbApiAddress.Items.Clear();
            string[] split = liveSection["api_list"].ToString()?.Split(',') ?? Array.Empty<string>();
            foreach (string address in split)
            {
                if (!string.IsNullOrEmpty(address))
                {
                    CmbApiAddress.Items.Add(address.Trim());
                }
            }

            BrdNetplayApiAddressSetting.Visibility = Visibility.Visible;
        }
        else
        {
            Logger.Warning("'api_list' is missing from the configuration file");
            BrdNetplayApiAddressSetting.Visibility = Visibility.Collapsed;
        }

        // api_address
        if (liveSection.ContainsKey("api_address"))
        {
            Logger.Info($"api_address - {liveSection["api_address"]}");
            if (!CmbApiAddress.Items.Contains(liveSection["api_address"].ToString()))
            {
                CmbApiAddress.Items.Add(liveSection["api_address"].ToString());
            }
            CmbApiAddress.SelectedItem = liveSection["api_address"].ToString();
        }
        else
        {
            Logger.Warning("'api_address' is missing from the configuration file");
        }

        // network_mode
        if (liveSection.ContainsKey("network_mode"))
        {
            string? networkMode = liveSection["network_mode"]?.ToString();
            if (!string.IsNullOrEmpty(networkMode) && int.TryParse(networkMode, out int selectedIndex))
            {
                Logger.Info($"network_mode - {networkMode}");
                CmbNetworkMode.SelectedIndex = selectedIndex;
                BrdNetplayNetworkModeSetting.Visibility = Visibility.Visible;
            }
            else
            {
                Logger.Warning("'network_mode' is invalid or missing from the configuration file");
                BrdNetplayNetworkModeSetting.Visibility = Visibility.Collapsed;
            }
        }
        else
        {
            Logger.Warning("'network_mode' is missing from the configuration file");
            BrdNetplayNetworkModeSetting.Visibility = Visibility.Collapsed;
        }

        // upnp
        if (liveSection.ContainsKey("upnp"))
        {
            Logger.Info($"upnp - {(bool)liveSection["upnp"]}");
            ChkNetplayUPnP.IsChecked = (bool)liveSection["upnp"];

            BrdNetplayUPnPSetting.Visibility = Visibility.Visible;
        }
        else
        {
            Logger.Warning("'upnp' is missing from the configuration file");
            BrdNetplayUPnPSetting.Visibility = Visibility.Collapsed;
        }

        // xlink_kai_systemlink_hack
        if (liveSection.ContainsKey("xlink_kai_systemlink_hack"))
        {
            Logger.Info($"xlink_kai_systemlink_hack - {(bool)liveSection["xlink_kai_systemlink_hack"]}");
            ChkNetplayXLinkKaiSystemLinkHacks.IsChecked = (bool)liveSection["xlink_kai_systemlink_hack"];

            BrdNetplayXLinkKaiSystemLinkHacksSetting.Visibility = Visibility.Visible;
        }
        else
        {
            Logger.Warning("'xlink_kai_systemlink_hack' is missing from the configuration file");
            BrdNetplayXLinkKaiSystemLinkHacksSetting.Visibility = Visibility.Collapsed;
        }
    }

    private void SaveLiveSettings(TomlTable liveSection)
    {
        // api_address
        if (liveSection.ContainsKey("api_address"))
        {
            string selectedAddress = CmbApiAddress.SelectedItem?.ToString() ?? CmbApiAddress.Text;
            Logger.Info($"api_address - {selectedAddress}");
            liveSection["api_address"] = selectedAddress;
        }

        // network_mode
        if (liveSection.ContainsKey("network_mode"))
        {
            Logger.Info($"network_mode - {CmbNetworkMode.SelectedIndex}");
            liveSection["network_mode"] = CmbNetworkMode.SelectedIndex;
        }

        // upnp
        if (liveSection.ContainsKey("upnp"))
        {
            Logger.Info($"upnp - {ChkNetplayUPnP.IsChecked}");
            liveSection["upnp"] = ChkNetplayUPnP.IsChecked ?? false;
        }

        // xlink_kai_systemlink_hack
        if (liveSection.ContainsKey("xlink_kai_systemlink_hack"))
        {
            Logger.Info($"xlink_kai_systemlink_hack - {ChkNetplayXLinkKaiSystemLinkHacks.IsChecked}");
            liveSection["xlink_kai_systemlink_hack"] = ChkNetplayXLinkKaiSystemLinkHacks.IsChecked ?? false;
        }
    }
}