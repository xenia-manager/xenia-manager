using System.Windows;
using System.Windows.Controls;
using Tomlyn.Model;
using XeniaManager.Core;

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
            foreach (string item in CmbAudioSystem.Items)
            {
                if (item == audioSection["apu"].ToString())
                {
                    CmbAudioSystem.SelectedItem = item;
                    break;
                }
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
}