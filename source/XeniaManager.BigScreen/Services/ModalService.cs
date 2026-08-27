using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using XeniaManager.BigScreen.ViewModels.Modals;
using XeniaManager.Logging;

namespace XeniaManager.BigScreen.Services;

/// <summary>
/// Push/pop stack of full-screen modals. Each modal is instantiated per open,
/// popped with a result, and disposed - no state carries between opens and the
/// stack composes naturally (a modal can await another modal on top of itself).
/// </summary>
public class ModalService : IModalService
{
    /// <summary>
    /// The currently open modals, bottom to top.
    /// </summary>
    private readonly List<ModalViewModelBase> _stack = [];

    /// <inheritdoc />
    public bool IsOpen => _stack.Count > 0;

    /// <inheritdoc />
    public ModalViewModelBase? Top => _stack.Count > 0 ? _stack[^1] : null;

    /// <inheritdoc />
    public IReadOnlyList<ModalViewModelBase> Stack => _stack;

    /// <inheritdoc />
    public event Action? StackChanged;

    /// <summary>
    /// Marks only the top modal as the visible one, hiding the hint bars of
    /// the modals beneath it.
    /// </summary>
    private void UpdateTopModalState()
    {
        for (int i = 0; i < _stack.Count; i++)
        {
            _stack[i].SetIsTopModal(i == _stack.Count - 1);
        }
    }

    /// <summary>
    /// Pushes the modal and attaches the service. The modal completes its own
    /// close task (and delivers its result) when it closes.
    /// </summary>
    private Task ShowAsyncCore(ModalViewModelBase modal)
    {
        _stack.Add(modal);
        modal.Attach(this);
        UpdateTopModalState();
        StackChanged?.Invoke();
        Logger.Debug<ModalService>($"Modal pushed: {modal.GetType().Name} (stack: {_stack.Count})");
        return modal.WaitForClose();
    }

    /// <inheritdoc />
    public Task ShowAsync(ModalViewModelBase modal)
    {
        return ShowAsyncCore(modal);
    }

    /// <inheritdoc />
    public async Task<TResult?> ShowAsync<TResult>(ModalViewModelBase<TResult> modal)
    {
        await ShowAsyncCore(modal);
        return modal.Result;
    }

    /// <inheritdoc />
    public void Close(ModalViewModelBase modal)
    {
        if (_stack.Count == 0 || !ReferenceEquals(_stack[^1], modal))
        {
            Logger.Warning<ModalService>($"Ignoring close for '{modal.GetType().Name}' - not the top of the stack");
            return;
        }

        _stack.RemoveAt(_stack.Count - 1);
        modal.Dispose();
        UpdateTopModalState();
        StackChanged?.Invoke();
        Logger.Debug<ModalService>($"Modal popped: {modal.GetType().Name} (stack: {_stack.Count})");
    }
}