using System;
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