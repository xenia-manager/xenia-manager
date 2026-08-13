using CommunityToolkit.Mvvm.ComponentModel;
using XeniaManager.BigScreen.Models;

namespace XeniaManager.BigScreen.ViewModels;

/// <summary>
/// A generic, reusable confirmation prompt: a header, a message and two
/// controller-friendly options (Left/Right to pick, A to activate, B cancels).
/// Resolves <c>true</c> for the first option, <c>false</c> for the second and
/// <c>null</c> when dismissed with B (callers decide what "cancel" means).
/// </summary>
public partial class ConfirmationModalViewModel : ModalViewModelBase<bool?>
{
    /// <summary>
    /// The prompt header.
    /// </summary>
    public string Title { get; }

    /// <summary>
    /// The prompt message body.
    /// </summary>
    public string Message { get; }

    /// <summary>
    /// Text of the left (primary) option.
    /// </summary>
    public string Option1Text { get; }

    /// <summary>
    /// Text of the right (cancel) option.
    /// </summary>
    public string Option2Text { get; }

    /// <summary>
    /// Whether the left (first) option is selected.
    /// </summary>
    [ObservableProperty] private bool _isOption1Selected = true;

    /// <summary>
    /// Whether the right (second) option is selected.
    /// </summary>
    public bool IsOption2Selected => !IsOption1Selected;

    /// <summary>
    /// Text of the currently selected option (drives the A hint).
    /// </summary>
    public string ActiveOptionText => IsOption1Selected ? Option1Text : Option2Text;

    public ConfirmationModalViewModel(string title, string message, string option1Text, string option2Text)
    {
        Title = title;
        Message = message;
        Option1Text = option1Text;
        Option2Text = option2Text;
    }

    partial void OnIsOption1SelectedChanged(bool value)
    {
        OnPropertyChanged(nameof(IsOption2Selected));
        OnPropertyChanged(nameof(ActiveOptionText));
    }

    /// <inheritdoc />
    public override bool HandleInput(NavigationCommand command)
    {
        switch (command)
        {
            case NavigationCommand.MoveLeft:
                IsOption1Selected = true;
                return true;
            case NavigationCommand.MoveRight:
                IsOption1Selected = false;
                return true;
            case NavigationCommand.Activate:
                Close(IsOption1Selected);
                return true;
            case NavigationCommand.Back:
                // B cancels the prompt entirely (null result)
                Close(null);
                return true;
            default:
                return false;
        }
    }

    /// <summary>
    /// Activates the left (primary) option with <c>true</c>.
    /// </summary>
    public void Confirm() => Close(true);

    /// <summary>
    /// Activates the right (cancel) option with <c>false</c>.
    /// </summary>
    public void Cancel() => Close(false);
}