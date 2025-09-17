using System;

namespace XeniaManager.Core.Exceptions;

public class XeniaSelectionCanceledException : OperationCanceledException
{
    public XeniaSelectionCanceledException() : base("Xenia version selection was canceled by the user.") { }
}
