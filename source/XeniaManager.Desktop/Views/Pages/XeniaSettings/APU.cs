using System.Windows;

// Imported Libraries
using Tomlyn.Model;
using XeniaManager.Core;
using XeniaManager.Desktop.Components;

namespace XeniaManager.Desktop.Views.Pages;

public partial class XeniaSettingsPage
{
    private void LoadAudioSettings(TomlTable audioSection)
    {
        // apu
        if (audioSection.ContainsKey("apu"))
        {
            Logger.Info($"apu - {audioSection["apu"]}");
            BrdAudioSystemSetting.Visibility = Visibility.Visible;
            CmbAudioSystem.SelectedValue = audioSection["apu"].ToString();
            if (CmbAudioSystem.SelectedIndex < 0)
            {
                CmbAudioSystem.SelectedIndex = 0;
            }
        }
        else
        {
            Logger.Warning("'apu' is missing from the configuration file");
            BrdAudioSystemSetting.Visibility = Visibility.Collapsed;
        }
        
        // apu_max_queued_frames
        if (audioSection.ContainsKey("apu_max_queued_frames"))
        {
            Logger.Info($"apu_max_queued_frames - {audioSection["apu_max_queued_frames"]}");
            BrdEnableXmpSetting.Visibility = Visibility.Visible;
            TxtAudioMaxQueuedFrames.Text = audioSection["apu_max_queued_frames"].ToString();
        }
        else
        {
            Logger.Warning("'apu_max_queued_frames' is missing from the configuration file");
            BrdAudioMaxQueuedFramesSetting.Visibility = Visibility.Collapsed;
        }
        
        // enable_xmp
        if (audioSection.ContainsKey("enable_xmp"))
        {
            Logger.Info($"enable_xmp - {audioSection["enable_xmp"]}");
            BrdEnableXmpSetting.Visibility = Visibility.Visible;
            ChkXmp.IsChecked = (bool)audioSection["enable_xmp"];
            if (ChkXmp.IsChecked == true)
            {
                BrdXmpVolumeSetting.Visibility = Visibility.Visible;
            }
            else
            {
                BrdXmpVolumeSetting.Visibility = Visibility.Collapsed;
            }
        }
        else
        {
            Logger.Warning("'enable_xmp' is missing from the configuration file");
            BrdEnableXmpSetting.Visibility = Visibility.Collapsed;
        }
        
        // mute
        if (audioSection.ContainsKey("mute"))
        {
            Logger.Info($"mute - {audioSection["mute"]}");
            BrdAudioMuteSetting.Visibility = Visibility.Visible;
            ChkAudioMute.IsChecked = (bool)audioSection["mute"];
        }
        else
        {
            Logger.Warning("'mute' is missing from the configuration file");
            BrdAudioMuteSetting.Visibility = Visibility.Collapsed;
        }
        
        // use_dedicated_xma_thread
        if (audioSection.ContainsKey("use_dedicated_xma_thread"))
        {
            Logger.Info($"use_dedicated_xma_thread - {audioSection["use_dedicated_xma_thread"]}");
            BrdAudioDedicatedXmaThreadSetting.Visibility = Visibility.Visible;
            ChkDedicatedXmaThread.IsChecked = (bool)audioSection["use_dedicated_xma_thread"];
        }
        else
        {
            Logger.Warning("'use_dedicated_xma_thread' is missing from the configuration file");
            BrdAudioDedicatedXmaThreadSetting.Visibility = Visibility.Collapsed;
        }
        
        // use_new_decoder
        if (audioSection.ContainsKey("use_new_decoder"))
        {
            Logger.Info($"use_new_decoder - {audioSection["use_new_decoder"]}");
            BrdAudioXmaDecoderSetting.Visibility = Visibility.Visible;
            ChkXmaAudioDecoder.IsChecked = (bool)audioSection["use_new_decoder"];
        }
        else
        {
            Logger.Warning("'use_new_decoder' is missing from the configuration file");
            BrdAudioMuteSetting.Visibility = Visibility.Collapsed;
        }
        
        // xmp_default_volume
        if (audioSection.ContainsKey("xmp_default_volume"))
        {
            Logger.Info($"xmp_default_volume - {audioSection["xmp_default_volume"]}");
            if (ChkXmp.IsChecked == true)
            {
                BrdXmpVolumeSetting.Visibility = Visibility.Visible;
            }
            else
            {
                BrdXmpVolumeSetting.Visibility = Visibility.Collapsed;
            }
            SldXmpVolume.Value = int.Parse(audioSection["xmp_default_volume"].ToString());
        }
        else
        {
            Logger.Warning("'xmp_default_volume' is missing from the configuration file");
            BrdXmpVolumeSetting.Visibility = Visibility.Collapsed;
        }
    }

    private void SaveAudioSettings(TomlTable audioSection)
    {
        // apu
        if (audioSection.ContainsKey("apu"))
        {
            Logger.Info($"apu - {CmbAudioSystem.SelectedItem}");
            audioSection["apu"] = CmbAudioSystem.SelectedValue;
        }
        
        // apu_max_queued_frames
        if (audioSection.ContainsKey("apu_max_queued_frames"))
        {
            try
            {
                int apuInt = int.Parse(TxtAudioMaxQueuedFrames.Text);
                if (apuInt < 4)
                {
                    apuInt = 4;
                }
                else if (apuInt > 64)
                {
                    apuInt = 64;
                }
                TxtAudioMaxQueuedFrames.Text = apuInt.ToString();
                Logger.Info($"apu_max_queued_frames - {TxtAudioMaxQueuedFrames.Text}");
                audioSection["apu_max_queued_frames"] = apuInt;
            }
            catch (Exception ex)
            {
                Logger.Error(ex.Message + "\nFull Error:\n" + ex);
                audioSection["apu_max_queued_frames"] = 8;
                TxtAudioMaxQueuedFrames.Text = "8";
                CustomMessageBox.Show("Invalid Input (Audio Max Queued Frames)", "Audio Max Queued Frames must be a number between 4 and 64.\nSetting the default value of 8.");
            }
        }
        
        // enable_xmp
        if (audioSection.ContainsKey("enable_xmp"))
        {
            Logger.Info($"enable_xmp - {ChkXmp.IsChecked}");
            audioSection["enable_xmp"] = ChkXmp.IsChecked;
        }
        
        // mute
        if (audioSection.ContainsKey("mute"))
        {
            Logger.Info($"mute - {ChkAudioMute.IsChecked}");
            audioSection["mute"] = ChkAudioMute.IsChecked;
        }
        
        // use_dedicated_xma_thread
        if (audioSection.ContainsKey("use_dedicated_xma_thread"))
        {
            Logger.Info($"use_dedicated_xma_thread - {ChkDedicatedXmaThread.IsChecked}");
            audioSection["use_dedicated_xma_thread"] = ChkDedicatedXmaThread.IsChecked;
        }
        
        // use_new_decoder
        if (audioSection.ContainsKey("use_new_decoder"))
        {
            Logger.Info($"use_new_decoder - {ChkXmaAudioDecoder.IsChecked}");
            audioSection["use_new_decoder"] = ChkXmaAudioDecoder.IsChecked;
        }
        
        // xmp_default_volume
        if (audioSection.ContainsKey("xmp_default_volume"))
        {
            Logger.Info($"xmp_default_volume - {SldXmpVolume.Value}");
            audioSection["xmp_default_volume"] = (int)SldXmpVolume.Value;
        }
    }
}