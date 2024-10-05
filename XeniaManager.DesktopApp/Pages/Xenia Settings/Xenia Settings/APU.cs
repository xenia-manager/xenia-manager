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
        /// <summary>
        /// Loads the Audio Settings into the UI
        /// </summary>
        /// <param name="sectionTable">Portion of .toml file dedicated to Audio Settings</param>
        private void LoadAudioSettings(TomlTable sectionTable)
        {
            // "apu" setting
            if (sectionTable.ContainsKey("apu"))
            {
                Log.Information($"apu - {sectionTable["apu"].ToString()}");
                foreach (var item in cmbAudioSystem.Items)
                {
                    if (item is ComboBoxItem comboBoxItem && comboBoxItem.Content.ToString() == sectionTable["apu"].ToString())
                    {
                        cmbAudioSystem.SelectedItem = comboBoxItem;
                        continue;
                    }
                }
            }

            // "apu_max_queued_frames" setting
            if (sectionTable.ContainsKey("apu_max_queued_frames"))
            {
                Log.Information($"apu_max_queued_frames - {sectionTable["apu_max_queued_frames"].ToString()}");
                txtAudioMaxQueuedFrames.Text = sectionTable["apu_max_queued_frames"].ToString();
            }

            // "mute" setting
            if (sectionTable.ContainsKey("mute"))
            {
                Log.Information($"mute - {(bool)sectionTable["mute"]}");
                chkMute.IsChecked = (bool)sectionTable["mute"];
            }

            // "use_dedicated_xma_thread" setting
            if (sectionTable.ContainsKey("use_dedicated_xma_thread"))
            {
                Log.Information($"use_dedicated_xma_thread - {(bool)sectionTable["use_dedicated_xma_thread"]}");
                chkDedicatedXMAThread.IsChecked = (bool)sectionTable["use_dedicated_xma_thread"];
            }

            // "use_new_decoder" setting
            if (sectionTable.ContainsKey("use_new_decoder"))
            {
                Log.Information($"use_new_decoder - {(bool)sectionTable["use_new_decoder"]}");
                chkXmaAudioDecoder.IsChecked = (bool)sectionTable["use_new_decoder"];
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
                ComboBoxItem selectedAudioSystem = cmbAudioSystem.Items[cmbAudioSystem.SelectedIndex] as ComboBoxItem;
                Log.Information($"apu - {selectedAudioSystem.Content}");
                sectionTable["apu"] = selectedAudioSystem.Content;
            }

            // "apu_max_queued_frames" setting
            if (sectionTable.ContainsKey("apu_max_queued_frames"))
            {
                try
                {
                    int apuInt = int.Parse(txtAudioMaxQueuedFrames.Text);
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
                    txtAudioMaxQueuedFrames.Text = apuInt.ToString();
                    Log.Information($"apu_max_queued_frames - {apuInt.ToString()}");
                    sectionTable["apu_max_queued_frames"] = apuInt;
                }
                catch (Exception ex)
                {
                    // If the input is incorrect, do the default
                    Log.Error(ex.Message + "\nFull Error:\n" + ex);
                    sectionTable["apu_max_queued_frames"] = 8;
                    txtAudioMaxQueuedFrames.Text = "8";
                    MessageBox.Show("Invalid input: apu_max_queued_frames must be a number.\nSetting the default value of 8.");
                }
            }

            // "mute" setting
            if (sectionTable.ContainsKey("mute"))
            {
                Log.Information($"mute - {chkMute.IsChecked}");
                sectionTable["mute"] = chkMute.IsChecked;
            }

            // "use_dedicated_xma_thread" setting
            if (sectionTable.ContainsKey("use_dedicated_xma_thread"))
            {
                Log.Information($"use_dedicated_xma_thread - {chkDedicatedXMAThread.IsChecked}");
                sectionTable["use_dedicated_xma_thread"] = chkDedicatedXMAThread.IsChecked;
            }

            // "use_new_decoder" setting
            if (sectionTable.ContainsKey("use_new_decoder"))
            {
                Log.Information($"use_new_decoder - {chkXmaAudioDecoder.IsChecked}");
                sectionTable["use_new_decoder"] = chkXmaAudioDecoder.IsChecked;
            }
        }
    }
}