﻿using System.Windows;

// Imported
using Serilog;
using Tomlyn.Model;

namespace XeniaManager.DesktopApp.Pages
{
    public partial class XeniaSettings
    {
        // Functions for loading Settings into the UI
        /// <summary>
        /// Loads the CPU Settings into the UI
        /// </summary>
        /// <param name="sectionTable">Portion of .toml file dedicated to CPU Settings</param>
        private void LoadCpuSettings(TomlTable sectionTable)
        {
            // "break_on_unimplemented_instructions" setting
            if (sectionTable.ContainsKey("break_on_unimplemented_instructions"))
            {
                Log.Information(
                    $"break_on_unimplemented_instructions - {(bool)sectionTable["break_on_unimplemented_instructions"]}");
                ChkBreakOnUnimplementedInstructions.IsChecked =
                    (bool)sectionTable["break_on_unimplemented_instructions"];
                
                BrdBreakOnUnimplementedInstructionsSetting.Visibility = Visibility.Visible;
                BrdBreakOnUnimplementedInstructionsSetting.Tag = null;
            }
            else
            {
                Log.Warning("`break_on_unimplemented_instructions` is missing from the configuration file");
                BrdBreakOnUnimplementedInstructionsSetting.Visibility = Visibility.Collapsed;
                BrdBreakOnUnimplementedInstructionsSetting.Tag = "Ignore";
            }

            // "disable_context_promotion" setting
            if (sectionTable.ContainsKey("disable_context_promotion"))
            {
                Log.Information(
                    $"disable_context_promotion - {(bool)sectionTable["disable_context_promotion"]}");
                ChkDisableContextPromotion.IsChecked =
                    (bool)sectionTable["disable_context_promotion"];
                
                BrdDisableContextPromotionSetting.Visibility = Visibility.Visible;
                BrdDisableContextPromotionSetting.Tag = "Ignore";
            }
            else
            {
                Log.Warning("`disable_context_promotion` is missing from the configuration file");
                BrdDisableContextPromotionSetting.Visibility = Visibility.Collapsed;
                BrdDisableContextPromotionSetting.Tag = "Ignore";
            }

            // "disassemble_functions" setting
            if (sectionTable.ContainsKey("disassemble_functions"))
            {
                Log.Information(
                    $"disassemble_functions - {(bool)sectionTable["disassemble_functions"]}");
                ChkDisassembleFunctions.IsChecked =
                    (bool)sectionTable["disassemble_functions"];
                
                BrdDisassembleFunctionsSetting.Visibility = Visibility.Visible;
                BrdDisassembleFunctionsSetting.Tag = null;
            }
            else
            {
                Log.Warning("`disassemble_functions` is missing from the configuration file");
                BrdDisassembleFunctionsSetting.Visibility = Visibility.Collapsed;
                BrdDisassembleFunctionsSetting.Tag = "Ignore";
            }
        }

        /// <summary>
        /// Saves the CPU Settings into the configuration file
        /// </summary>
        /// <param name="sectionTable">Portion of .toml file dedicated to CPU Settings</param>
        private void SaveCpuSettings(TomlTable sectionTable)
        {
            // "break_on_unimplemented_instructions" setting
            if (sectionTable.ContainsKey("break_on_unimplemented_instructions"))
            {
                Log.Information(
                    $"break_on_unimplemented_instructions - {ChkBreakOnUnimplementedInstructions.IsChecked}");
                sectionTable["break_on_unimplemented_instructions"] = ChkBreakOnUnimplementedInstructions.IsChecked;
            }

            // "disable_context_promotion" setting
            if (sectionTable.ContainsKey("disable_context_promotion"))
            {
                Log.Information(
                    $"disable_context_promotion - {ChkDisableContextPromotion.IsChecked}");
                sectionTable["disable_context_promotion"] = ChkDisableContextPromotion.IsChecked;
            }

            // "disassemble_functions" setting
            if (sectionTable.ContainsKey("disassemble_functions"))
            {
                Log.Information(
                    $"disassemble_functions - {ChkDisassembleFunctions.IsChecked}");
                sectionTable["disassemble_functions"] = ChkDisassembleFunctions.IsChecked;
            }
        }
    }
}