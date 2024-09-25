using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media.Animation;
using XeniaManager.DesktopApp.Windows;

namespace XeniaManager.DesktopApp.Components.CustomControls
{
    /// <summary>
    /// Custom Button that has the ability to store game title and titleid
    /// </summary>
    public class GameButton : Button
    {
        // Custom properties
        public string GameTitle { get; set; } // Game Title
        public string GameId { get; set; } // TitleID

        // Constructor
        public GameButton(Game game)
        {
            GameTitle = game.Title;
            GameId = game.GameId;
            this.Cursor = Cursors.Hand;
            this.Style = (Style)FindResource("GameCoverButtons");
            this.ToolTip = CreateTooltip(game);
        }

        /// <summary>
        /// Creates a tooltip for the button
        /// </summary>
        private TextBlock CreateTooltip(Game game)
        {
            // Create TextBlock for tooltip
            TextBlock tooltip = new TextBlock { TextAlignment = TextAlignment.Center };
            tooltip.Inlines.Add(new Run(game.Title + "\n") { FontWeight = FontWeights.Bold }); // Adding game title to tooltip

            // Adding compatibility rating to the tooltip
            tooltip.Inlines.Add(new Run($"{game.CompatibilityRating}") { FontWeight = FontWeights.Bold, TextDecorations = TextDecorations.Underline });
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
                    tooltip.Inlines.Add(new Run(" (The game can be reasonably played from start to finish with little to no issues)"));
                    break;
                default:
                    break;
            }

            // Adding playtime to the tooltip
            if (game.Playtime != null)
            {
                string FormattedPlaytime = "";
                if (game.Playtime == 0)
                {
                    FormattedPlaytime = "Never played";
                }
                else if (game.Playtime < 60)
                {
                    FormattedPlaytime = $"{game.Playtime:N0} minutes";
                }
                else
                {
                    FormattedPlaytime = $"{(game.Playtime / 60):N1} hours";
                }
                tooltip.Inlines.Add(new Run("\n" + "Time played:") { FontWeight = FontWeights.Bold, TextDecorations = TextDecorations.Underline });
                tooltip.Inlines.Add(new Run($" {FormattedPlaytime}"));
            }
            else
            {
                tooltip.Inlines.Add(new Run("\n" + "Time played:") { FontWeight = FontWeights.Bold, TextDecorations = TextDecorations.Underline });
                tooltip.Inlines.Add(new Run(" Never played"));
            }

            // Return the created tooltip
            return tooltip;
        }
    }
}
