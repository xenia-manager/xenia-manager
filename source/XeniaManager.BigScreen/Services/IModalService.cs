using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using XeniaManager.BigScreen.ViewModels.Modals;

namespace XeniaManager.BigScreen.Services;

/// <summary>
/// Hosts the full-screen modal overlay stack: pushing a modal places it above
/// everything else, popping it disposes it and returns its result to the caller.
/// </summary>
public interface IModalService
{
    /// <summary>
    /// Whether any modal is currently open.
    /// </summary>
    bool IsOpen { get; }

    /// <summary>
    /// The top of the modal stack (receives input), or null when empty.
    /// </summary>
    ModalViewModelBase? Top { get; }

    /// <summary>
    /// The open modals, bottom to top.
    /// </summary>
    IReadOnlyList<ModalViewModelBase> Stack { get; }

    /// <summary>
    /// Raised after the stack changes (push or pop), so the host can re-render.
    /// </summary>
    event Action? StackChanged;

    /// <summary>
    /// Pushes a modal that delivers no result and awaits its close.
    /// </summary>
    Task ShowAsync(ModalViewModelBase modal);

    /// <summary>
    /// Pushes a modal and awaits its close, returning the result it delivers.
    /// </summary>
    Task<TResult?> ShowAsync<TResult>(ModalViewModelBase<TResult> modal);

    /// <summary>
    /// Pops the given modal off the stack, disposes it and completes its awaiters.
    /// </summary>
    void Close(ModalViewModelBase modal);
}