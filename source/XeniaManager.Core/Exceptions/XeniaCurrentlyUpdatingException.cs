using System;

namespace XeniaManager.Core.Exceptions;

public class XeniaCurrentlyUpdatingException : OperationCanceledException
{
    public XeniaCurrentlyUpdatingException() : base("Xenia is currently updating, please wait until the update is finished.") { }
}
