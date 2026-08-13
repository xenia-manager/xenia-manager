using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using XeniaManager.BigScreen.ViewModels;

namespace XeniaManager.BigScreen.Controls;

/// <summary>
/// Generic confirmation prompt: header, message and two controller-friendly
/// options (Left/Right selects, A activates the selection, B cancels).
/// </summary>
public partial class ConfirmationModal : UserControl
{
    public ConfirmationModal()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    /// <summary>
    /// Moves focus to the selected option so keyboard Enter activates it.
    /// </summary>
    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        Dispatcher.UIThread.Post(() =>
        {
            if (DataContext is ConfirmationModalViewModel vm)
            {
                (vm.IsOption1Selected ? BtnOption1 : BtnOption2).Focus();
            }
        });
    }

    private void OnOption1Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is ConfirmationModalViewModel vm)
        {
            vm.Confirm();
        }
    }

    private void OnOption2Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is ConfirmationModalViewModel vm)
        {
            vm.Cancel();
        }
    }
}