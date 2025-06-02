using System.Windows;

// Imported Libraries
using Tomlyn.Model;
using XeniaManager.Core;

namespace XeniaManager.Desktop.Views.Pages;

public partial class XeniaSettingsPage
{
    private void LoadCpuSettings(TomlTable cpuSection)
    {
        // break_on_unimplemented_instructions
        if (cpuSection.ContainsKey("break_on_unimplemented_instructions"))
        {
            Logger.Info($"break_on_unimplemented_instructions - {cpuSection["break_on_unimplemented_instructions"]}");
            BrdBreakOnUnimplementedInstructionsSetting.Visibility = Visibility.Visible;
            ChkBreakOnUnimplementedInstructions.IsChecked = (bool)cpuSection["break_on_unimplemented_instructions"];
        }
        else
        {
            Logger.Warning("'break_on_unimplemented_instructions' is missing from the configuration file");
            BrdBreakOnUnimplementedInstructionsSetting.Visibility = Visibility.Collapsed;
        }

        // disable_context_promotion
        if (cpuSection.ContainsKey("disable_context_promotion"))
        {
            Logger.Info($"disable_context_promotion - {cpuSection["disable_context_promotion"]}");
            BrdDisableContextPromotionSetting.Visibility = Visibility.Visible;
            ChkDisableContextPromotion.IsChecked = (bool)cpuSection["disable_context_promotion"];
        }
        else
        {
            Logger.Warning("'disable_context_promotion' is missing from the configuration file");
            BrdDisableContextPromotionSetting.Visibility = Visibility.Collapsed;
        }

        // disassemble_functions
        if (cpuSection.ContainsKey("disassemble_functions"))
        {
            Logger.Info($"disassemble_functions - {cpuSection["disassemble_functions"]}");
            BrdDisassembleFunctionsSetting.Visibility = Visibility.Visible;
            ChkDisassembleFunctions.IsChecked = (bool)cpuSection["disassemble_functions"];
        }
        else
        {
            Logger.Warning("'disassemble_functions' is missing from the configuration file");
            BrdDisassembleFunctionsSetting.Visibility = Visibility.Collapsed;
        }
    }

    private void SaveCpuSettings(TomlTable cpuSection)
    {
        // break_on_unimplemented_instructions
        if (cpuSection.ContainsKey("break_on_unimplemented_instructions"))
        {
            Logger.Info($"break_on_unimplemented_instructions - {ChkBreakOnUnimplementedInstructions.IsChecked}");
            cpuSection["break_on_unimplemented_instructions"] = ChkBreakOnUnimplementedInstructions.IsChecked;
        }

        // disable_context_promotion
        if (cpuSection.ContainsKey("disable_context_promotion"))
        {
            Logger.Info($"disable_context_promotion - {ChkDisableContextPromotion.IsChecked}");
            cpuSection["disable_context_promotion"] = ChkDisableContextPromotion.IsChecked;
        }

        // disassemble_functions
        if (cpuSection.ContainsKey("disassemble_functions"))
        {
            Logger.Info($"disassemble_functions - {ChkDisassembleFunctions.IsChecked}");
            cpuSection["disassemble_functions"] = ChkDisassembleFunctions.IsChecked;
        }
    }
}