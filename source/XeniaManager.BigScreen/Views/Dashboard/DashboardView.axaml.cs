using Avalonia.Controls;
using XeniaManager.BigScreen.Constants;
using TweenAvalonia;

namespace XeniaManager.BigScreen.Views.Dashboard;

/// <summary>
/// Dashboard content: recent game cards and option cards.
/// </summary>
public partial class DashboardView : UserControl
{
    /// <summary>
    /// The in-flight game-row reveal fade; always completes to full opacity.
    /// </summary>
    private Tween _gamesRowFade;

    /// <summary>
    /// The in-flight option-row reveal fade; always completes to full opacity.
    /// </summary>
    private Tween _optionsRowFade;

    public DashboardView()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Fades the dashboard elements (game cards + option cards) in from 0 to
    /// full opacity. Called by the shell when the splash is about to close.
    /// </summary>
    public void BeginReveal()
    {
        _gamesRowFade.Stop();
        _optionsRowFade.Stop();
        GamesRow.Opacity = 0;
        OptionsRow.Opacity = 0;
        _gamesRowFade = Tween.Opacity(GamesRow, 1, TimingConstants.LaunchFadeDuration);
        _optionsRowFade = Tween.Opacity(OptionsRow, 1, TimingConstants.LaunchFadeDuration);
    }
}