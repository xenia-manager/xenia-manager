using System.Windows;
using System.Windows.Controls;

// Imported
using Serilog;
using Tomlyn.Model;

namespace XeniaManager.DesktopApp.Pages
{
    public partial class XeniaSettings
    {
        // Functions for loading Settings into the UI
        /// <summary>
        /// Loads the Video Settings into the UI
        /// </summary>
        /// <param name="sectionTable">Portion of .toml file dedicated to Video Settings</param>
        private void LoadVideoSettings(TomlTable sectionTable)
        {
            // "internal_display_resolution" setting
            if (sectionTable.ContainsKey("internal_display_resolution"))
            {
                Log.Information(
                    $"internal_display_resolution - {int.Parse(sectionTable["internal_display_resolution"].ToString())}");
                CmbInternalDisplayResolution.SelectedIndex =
                    int.Parse(sectionTable["internal_display_resolution"].ToString());
                if (CmbInternalDisplayResolution.SelectedIndex == 17)
                {
                    BrdCustomInternalResolutionWidthSetting.Visibility = Visibility.Visible;
                    BrdCustomInternalResolutionWidthSetting.Tag = null;

                    BrdCustomInternalResolutionHeightSetting.Visibility = Visibility.Visible;
                    BrdCustomInternalResolutionHeightSetting.Tag = null;
                }
                else
                {
                    BrdCustomInternalResolutionWidthSetting.Visibility = Visibility.Collapsed;
                    BrdCustomInternalResolutionWidthSetting.Tag = "Ignore";

                    BrdCustomInternalResolutionHeightSetting.Visibility = Visibility.Collapsed;
                    BrdCustomInternalResolutionHeightSetting.Tag = "Ignore";
                }
                
                BrdInternalDisplayResolutionSetting.Visibility = Visibility.Visible;
                BrdInternalDisplayResolutionSetting.Tag = null;
            }
            else
            {
                Log.Warning("`internal_display_resolution` is missing from configuration file");
                BrdInternalDisplayResolutionSetting.Visibility = Visibility.Collapsed;
                BrdInternalDisplayResolutionSetting.Tag = "Ignore";
            }

            // "internal_display_resolution_x" setting
            if (sectionTable.ContainsKey("internal_display_resolution_x"))
            {
                Log.Information($"internal_display_resolution_x - {sectionTable["internal_display_resolution_x"]}");
                TxtCustomInternalResolutionWidth.Text = sectionTable["internal_display_resolution_x"].ToString();
                
                BrdCustomInternalResolutionWidthSetting.Visibility = Visibility.Visible;
                BrdCustomInternalResolutionWidthSetting.Tag = null;
            }
            else
            {
                Log.Warning("`internal_display_resolution_x` is missing from configuration file");
                BrdCustomInternalResolutionWidthSetting.Visibility = Visibility.Collapsed;
                BrdCustomInternalResolutionWidthSetting.Tag = "Ignore";
            }

            // "internal_display_resolution_y" setting
            if (sectionTable.ContainsKey("internal_display_resolution_y"))
            {
                Log.Information($"internal_display_resolution_y - {sectionTable["internal_display_resolution_y"]}");
                TxtCustomInternalResolutionHeight.Text = sectionTable["internal_display_resolution_y"].ToString();
                
                BrdCustomInternalResolutionHeightSetting.Visibility = Visibility.Visible;
                BrdCustomInternalResolutionHeightSetting.Tag = null;
            }
            else
            {
                Log.Warning("`internal_display_resolution_y` is missing from configuration file");
                BrdCustomInternalResolutionHeightSetting.Visibility = Visibility.Collapsed;
                BrdCustomInternalResolutionHeightSetting.Tag = "Ignore";
            }

            // "widescreen" setting
            if (sectionTable.ContainsKey("widescreen"))
            {
                Log.Information($"widescreen - {(bool)sectionTable["widescreen"]}");
                ChkWidescreen.IsChecked = (bool)sectionTable["widescreen"];
                
                BrdWidescreenSetting.Visibility = Visibility.Visible;
                BrdWidescreenSetting.Tag = null;
            }
            else
            {
                Log.Warning("`widescreen` is missing from configuration file");
                BrdWidescreenSetting.Visibility = Visibility.Collapsed;
                BrdWidescreenSetting.Tag = "Ignore";
            }
        }

        /// <summary>
        /// Saves the Video Settings into the configuration file
        /// </summary>
        /// <param name="sectionTable">Portion of .toml file dedicated to Video Settings</param>
        private void SaveVideoSettings(TomlTable sectionTable)
        {
            // "internal_display_resolution" setting
            if (sectionTable.ContainsKey("internal_display_resolution"))
            {
                Log.Information(
                    $"internal_display_resolution - {(CmbInternalDisplayResolution.SelectedItem as ComboBoxItem).Content}");
                sectionTable["internal_display_resolution"] = CmbInternalDisplayResolution.SelectedIndex;
            }

            // "internal_display_resolution_x" setting
            if (sectionTable.ContainsKey("internal_display_resolution_x"))
            {
                int resolutionX;
                try
                {
                    resolutionX = int.Parse(TxtCustomInternalResolutionWidth.Text);
                }
                catch (Exception)
                {
                    Log.Error(
                        "Invalid input for custom internal display resolution width. Setting it to default value (1280)");
                    MessageBox.Show(
                        "Invalid input for custom internal display resolution width.\nSetting it to default value (1280)");
                    resolutionX = 1280;
                }

                if (resolutionX > 1920)
                {
                    Log.Warning(
                        "Custom internal display resolution width is too big. Setting it to maximum allowed (1920)");
                    MessageBox.Show(
                        "Custom internal display resolution width is too big.\nSetting it to maximum allowed (1920)");
                    resolutionX = 1920;
                }
                else if (resolutionX < 1)
                {
                    Log.Warning(
                        "Custom internal display resolution width is too small. Setting it to minimal allowed (1)");
                    MessageBox.Show(
                        "Custom internal display resolution width is too small.\nSetting it to minimal allowed (1)");
                    resolutionX = 1;
                }

                Log.Information($"internal_display_resolution_x - {resolutionX}");
                sectionTable["internal_display_resolution_x"] = resolutionX;
                TxtCustomInternalResolutionWidth.Text = resolutionX.ToString();
            }

            // "internal_display_resolution_y" setting
            if (sectionTable.ContainsKey("internal_display_resolution_y"))
            {
                int resolutionY;
                try
                {
                    resolutionY = int.Parse(TxtCustomInternalResolutionHeight.Text);
                }
                catch (Exception)
                {
                    Log.Error("Invalid input for custom internal display resolution height. Setting it to default");
                    MessageBox.Show(
                        "Invalid input for custom internal display resolution height.\nSetting it to default value (720)");
                    resolutionY = 720;
                }

                if (resolutionY > 1080)
                {
                    Log.Warning(
                        "Custom internal display resolution height is too big. Setting it to maximum allowed (1080)");
                    MessageBox.Show(
                        "Custom internal display resolution height is too big.\nSetting it to maximum allowed (1080)");
                    resolutionY = 1080;
                }
                else if (resolutionY < 1)
                {
                    Log.Warning(
                        "Custom internal display resolution width is too small. Setting it to minimal allowed (1)");
                    MessageBox.Show(
                        "Custom internal display resolution width is too small.\nSetting it to minimal allowed (1)");
                    resolutionY = 1;
                }

                Log.Information($"internal_display_resolution_y - {resolutionY}");
                sectionTable["internal_display_resolution_y"] = resolutionY;
                TxtCustomInternalResolutionHeight.Text = resolutionY.ToString();
            }

            // "widescreen" setting
            if (sectionTable.ContainsKey("widescreen"))
            {
                Log.Information($"widescreen - {ChkWidescreen.IsChecked}");
                sectionTable["widescreen"] = ChkWidescreen.IsChecked;
            }
        }
    }
}