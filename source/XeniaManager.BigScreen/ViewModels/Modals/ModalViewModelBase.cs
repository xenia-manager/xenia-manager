using System.Threading.Tasks;
using XeniaManager.BigScreen.Models;
using XeniaManager.BigScreen.Services;

namespace XeniaManager.BigScreen.ViewModels.Modals;

/// <summary>
/// Base class for full-screen modal overlays hosted by the modal service.
/// Modals are created fresh per open, closed via <see cref="Close"/> and
/// disposed when popped off the stack.
/// </summary>
public abstract class ModalViewModelBase : ViewModelBase
{
    /// <summary>
    /// The modal service that hosts this modal, attached when pushed.
    /// </summary>
    private IModalService? _modalService;

    /// <summary>
    /// Completed when the modal closes, releasing awaiting callers.
    /// </summary>
    private readonly TaskCompletionSource _closed = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>
    /// Awaits the modal's close (returns after it is popped).
    /// </summary>
    public Task WaitForClose() => _closed.Task;

    /// <summary>
    /// Whether this modal is the top of the stack (drives which hint bar shows).
    /// </summary>
    public bool IsTopModal { get; private set; }

    /// <summary>
    /// Whether this modal's hint bar is visible - only the top modal shows one,
    /// so stacked modals never show competing hints.
    /// </summary>
    public bool IsHintBarVisible => IsTopModal;

    /// <summary>
    /// Handles a navigation command while this modal is the top of the stack.
    /// The base implementation closes on Back and ignores everything else.
    /// </summary>
    /// <returns>True when the command was consumed.</returns>
    public virtual bool HandleInput(NavigationCommand command)
    {
        if (command == NavigationCommand.Back)
        {
            Close();
            return true;
        }

        return false;
    }

    /// <summary>
    /// Updates whether this modal is the top of the stack (called by the modal
    /// service when the stack changes).
    /// </summary>
    internal void SetIsTopModal(bool isTop)
    {
        IsTopModal = isTop;
        OnPropertyChanged(nameof(IsHintBarVisible));
    }

    /// <summary>
    /// Releases resources held by the modal (event subscriptions, images).
    /// Called once when the modal is popped off the stack.
    /// </summary>
    public virtual void Dispose()
    {
    }

    /// <summary>
    /// Closes this modal and pops it off the stack.
    /// </summary>
    protected void Close()
    {
        _closed.TrySetResult();
        _modalService?.Close(this);
    }

    /// <summary>
    /// Attaches the modal service that hosts this modal. Called by the modal
    /// service when the modal is pushed.
    /// </summary>
    internal void Attach(IModalService modalService)
    {
        _modalService = modalService;
    }
}