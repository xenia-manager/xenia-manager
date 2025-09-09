using System;

namespace XeniaManager.Core.Exceptions;

public class SymbolicLinkCreationFailedException : Exception
{
    public SymbolicLinkCreationFailedException() : base("Symbolic Link creation process failed.") { }
}
