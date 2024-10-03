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
    }
}