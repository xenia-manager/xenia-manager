namespace XeniaManager.BigScreen.ViewModels;

/// <summary>
/// Base class for modals that deliver a typed result to their caller
/// (e.g. confirmations). The result is set before closing and returned
/// by <see cref="IModalService.ShowAsync{TResult}"/>.
/// </summary>
public abstract class ModalViewModelBase<TResult> : ModalViewModelBase
{
    /// <summary>
    /// The result delivered to the awaiting caller when the modal closes.
    /// </summary>
    public TResult? Result { get; protected set; }

    /// <summary>
    /// Closes this modal, delivering the given result to the caller.
    /// </summary>
    protected void Close(TResult? result)
    {
        Result = result;
        Close();
    }
}