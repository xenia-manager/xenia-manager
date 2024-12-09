using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

namespace XeniaManager.DesktopApp.CustomControls
{
    public partial class GameButton
    {
        /// <summary>
        /// Creates a tooltip for the button
        /// </summary>
        private TextBlock CreateTooltip(Game game)
        {
            // Create TextBlock for tooltip
            TextBlock tooltip = new TextBlock { TextAlignment = TextAlignment.Center };
            tooltip.Inlines.Add(new Run(game.Title + "\n")
                { FontWeight = FontWeights.Bold }); // Adding game title to tooltip

            // Adding compatibility rating to the tooltip
            tooltip.Inlines.Add(new Run($"{game.CompatibilityRating}")
                { FontWeight = FontWeights.Bold, TextDecorations = TextDecorations.Underline });
            switch (game.CompatibilityRating)
            {
                case CompatibilityRating.Unplayable:
                    tooltip.Inlines.Add(new Run(" (The game either doesn't start or it crashes a lot)"));
                    break;
                case CompatibilityRating.Loads:
                    tooltip.Inlines.Add(new Run(" (The game loads, but crashes in the title screen or main menu)"));
                    break;
                case CompatibilityRating.Gameplay:
                    tooltip.Inlines.Add(new Run(" (Gameplay loads, but it may be unplayable)"));
                    break;
                case CompatibilityRating.Playable:
                    tooltip.Inlines.Add(new Run(
                        " (The game can be reasonably played from start to finish with little to no issues)"));
                    break;
                default:
                    break;
            }

            // Adding playtime to the tooltip
            if (game.Playtime != null)
            {
                string formattedPlaytime = "";
                if (game.Playtime == 0)
                {
                    formattedPlaytime = "Never played";
                }
                else if (game.Playtime < 60)
                {
                    formattedPlaytime = $"{game.Playtime:N0} minutes";
                }
                else
                {
                    formattedPlaytime = $"{(game.Playtime / 60):N1} hours";
                }

                tooltip.Inlines.Add(new Run("\n" + "Time played:")
                    { FontWeight = FontWeights.Bold, TextDecorations = TextDecorations.Underline });
                tooltip.Inlines.Add(new Run($" {formattedPlaytime}"));
            }
            else
            {
                tooltip.Inlines.Add(new Run("\n" + "Time played:")
                    { FontWeight = FontWeights.Bold, TextDecorations = TextDecorations.Underline });
                tooltip.Inlines.Add(new Run(" Never played"));
            }

            // Return the created tooltip
            return tooltip;
        }
    }
}