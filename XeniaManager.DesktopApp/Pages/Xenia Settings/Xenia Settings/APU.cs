using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;

// Imported
using Serilog;
using Tomlyn.Model;

namespace XeniaManager.DesktopApp.Pages
{
    public partial class XeniaSettings
    {
        /// <summary>
        /// Loads the Audio Settings into the UI
        /// </summary>
        /// <param name="sectionTable">Portion of .toml file dedicated to Audio Settings</param>
        private void LoadAudioSettings(TomlTable sectionTable)
        {
            // "apu" setting
            if (sectionTable.ContainsKey("apu"))
            {
                Log.Information($"apu - {sectionTable["apu"]}");
                foreach (var item in CmbAudioSystem.Items)
                {
                    if (item is ComboBoxItem comboBoxItem &&
                        comboBoxItem.Content.ToString() == sectionTable["apu"].ToString())
                    {
                        CmbAudioSystem.SelectedItem = comboBoxItem;
                    }
                }
                
                BrdAudioSystemSetting.Visibility = Visibility.Visible;
                BrdAudioSystemSetting.Tag = null;
            }
            else
            {
                BrdAudioSystemSetting.Visibility = Visibility.Collapsed;
                BrdAudioSystemSetting.Tag = "Ignore";
            }

            // "apu_max_queued_frames" setting
            if (sectionTable.ContainsKey("apu_max_queued_frames"))
            {
                Log.Information($"apu_max_queued_frames - {sectionTable["apu_max_queued_frames"]}");
                TxtAudioMaxQueuedFrames.Text = sectionTable["apu_max_queued_frames"].ToString() ?? string.Empty;
                
                BrdAudioMaxQueuedFramesSetting.Visibility = Visibility.Visible;
                BrdAudioMaxQueuedFramesSetting.Tag = null;
            }
            else
            {
                BrdAudioMaxQueuedFramesSetting.Visibility = Visibility.Collapsed;
                BrdAudioMaxQueuedFramesSetting.Tag = "Ignore";
            }
            
            // "enable_xmp" setting
            if (sectionTable.ContainsKey("enable_xmp"))
            {
                Log.Information($"enable_xmp - {sectionTable["enable_xmp"]}");
                ChkXmp.IsChecked = (bool)sectionTable["enable_xmp"];
                if (ChkXmp.IsChecked == true)
                {
                    BrdXmpVolumeSetting.Visibility = Visibility.Visible;
                    BrdXmpVolumeSetting.Tag = null;
                }
                else
                {
                    BrdXmpVolumeSetting.Visibility = Visibility.Collapsed;
                    BrdXmpVolumeSetting.Tag = "Ignore";
                }
                
                BrdEnableXmpSetting.Visibility = Visibility.Visible;
                BrdEnableXmpSetting.Tag = null;
            }
            else
            {
                BrdEnableXmpSetting.Visibility = Visibility.Collapsed;
                BrdEnableXmpSetting.Tag = "Ignore";
            }

            // "mute" setting
            if (sectionTable.ContainsKey("mute"))
            {
                Log.Information($"mute - {(bool)sectionTable["mute"]}");
                ChkAudioMute.IsChecked = (bool)sectionTable["mute"];
                
                BrdAudioMuteSetting.Visibility = Visibility.Visible;
                BrdAudioMuteSetting.Tag = null;
            }
            else
            {
                BrdAudioMuteSetting.Visibility = Visibility.Collapsed;
                BrdAudioMuteSetting.Tag = "Ignore";
            }

            // "use_dedicated_xma_thread" setting
            if (sectionTable.ContainsKey("use_dedicated_xma_thread"))
            {
                Log.Information($"use_dedicated_xma_thread - {(bool)sectionTable["use_dedicated_xma_thread"]}");
                ChkDedicatedXmaThread.IsChecked = (bool)sectionTable["use_dedicated_xma_thread"];
                
                BrdAudioDedicatedXmaThreadSetting.Visibility = Visibility.Visible;
                BrdAudioDedicatedXmaThreadSetting.Tag = null;
            }
            else
            {
                BrdAudioDedicatedXmaThreadSetting.Visibility = Visibility.Collapsed;
                BrdAudioDedicatedXmaThreadSetting.Tag = "Ignore";
            }

            // "use_new_decoder" setting
            if (sectionTable.ContainsKey("use_new_decoder"))
            {
                Log.Information($"use_new_decoder - {(bool)sectionTable["use_new_decoder"]}");
                ChkXmaAudioDecoder.IsChecked = (bool)sectionTable["use_new_decoder"];
                
                BrdAudioXmaDecoderSetting.Visibility = Visibility.Visible;
                BrdAudioXmaDecoderSetting.Tag = null;
            }
            else
            {
                BrdAudioXmaDecoderSetting.Visibility = Visibility.Collapsed;
                BrdAudioXmaDecoderSetting.Tag = "Ignore";
            }
            
            // "xmp_default_volume" setting
            if (sectionTable.ContainsKey("xmp_default_volume"))
            {
                Log.Information($"xmp_default_volume - {sectionTable["xmp_default_volume"]}");
                SldXmpVolume.Value = int.Parse(sectionTable["xmp_default_volume"].ToString());
                AutomationProperties.SetName(SldXmpVolume,
                    $"XMP Default Volume: {SldXmpVolume.Value}");
                
                BrdXmpVolumeSetting.Visibility = Visibility.Visible;
                BrdXmpVolumeSetting.Tag = null;
            }
            else
            {
                BrdXmpVolumeSetting.Visibility = Visibility.Collapsed;
                BrdXmpVolumeSetting.Tag = "Ignore";
            }
        }

        /// <summary>
        /// Saves the Audio Settings into the configuration file
        /// </summary>
        /// <param name="sectionTable">Portion of .toml file dedicated to Audio Settings</param>
        private void SaveAudioSettings(TomlTable sectionTable)
        {
            // "apu" setting
            if (sectionTable.ContainsKey("apu"))
            {
                ComboBoxItem selectedAudioSystem = CmbAudioSystem.Items[CmbAudioSystem.SelectedIndex] as ComboBoxItem;
                Log.Information($"apu - {selectedAudioSystem.Content}");
                sectionTable["apu"] = selectedAudioSystem.Content;
            }

            // "apu_max_queued_frames" setting
            if (sectionTable.ContainsKey("apu_max_queued_frames"))
            {
                try
                {
                    int apuInt = int.Parse(TxtAudioMaxQueuedFrames.Text);
                    if (apuInt < 4)
                    {
                        MessageBox.Show("apu_max_queued_frames minimal value is 4");
                        apuInt = 4;
                    }
                    else if (apuInt > 64)
                    {
                        MessageBox.Show("apu_max_queued_frames maximum value is 64");
                        apuInt = 64;
                    }

                    TxtAudioMaxQueuedFrames.Text = apuInt.ToString();
                    Log.Information($"apu_max_queued_frames - {apuInt.ToString()}");
                    sectionTable["apu_max_queued_frames"] = apuInt;
                }
                catch (Exception ex)
                {
                    // If the input is incorrect, do the default
                    Log.Error(ex.Message + "\nFull Error:\n" + ex);
                    sectionTable["apu_max_queued_frames"] = 8;
                    TxtAudioMaxQueuedFrames.Text = "8";
                    MessageBox.Show(
                        "Invalid input: apu_max_queued_frames must be a number.\nSetting the default value of 8.");
                }
            }
            
            // "enable_xmp" setting
            if (sectionTable.ContainsKey("enable_xmp"))
            {
                Log.Information($"enable_xmp - {ChkXmp.IsChecked}");
                sectionTable["enable_xmp"] = ChkXmp.IsChecked;
            }

            // "mute" setting
            if (sectionTable.ContainsKey("mute"))
            {
                Log.Information($"mute - {ChkAudioMute.IsChecked}");
                sectionTable["mute"] = ChkAudioMute.IsChecked;
            }

            // "use_dedicated_xma_thread" setting
            if (sectionTable.ContainsKey("use_dedicated_xma_thread"))
            {
                Log.Information($"use_dedicated_xma_thread - {ChkDedicatedXmaThread.IsChecked}");
                sectionTable["use_dedicated_xma_thread"] = ChkDedicatedXmaThread.IsChecked;
            }

            // "use_new_decoder" setting
            if (sectionTable.ContainsKey("use_new_decoder"))
            {
                Log.Information($"use_new_decoder - {ChkXmaAudioDecoder.IsChecked}");
                sectionTable["use_new_decoder"] = ChkXmaAudioDecoder.IsChecked;
            }
            
            // "xmp_default_volume" setting
            if (sectionTable.ContainsKey("xmp_default_volume"))
            {
                Log.Information($"xmp_default_volume - {SldXmpVolume.Value}");
                sectionTable["xmp_default_volume"] = (int)SldXmpVolume.Value;
            }
        }
    }
}