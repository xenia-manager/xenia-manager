using System;

namespace XeniaManager.Core.Exceptions;

public class MissingImplementationForButtonException : NotImplementedException
{
    public MissingImplementationForButtonException() : base("Missing implementation for this button.") { }
}
