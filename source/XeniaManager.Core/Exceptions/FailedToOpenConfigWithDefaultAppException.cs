using System;

namespace XeniaManager.Core.Exceptions;

public class FailedToOpenConfigWithDefaultAppException : Exception
{
    public FailedToOpenConfigWithDefaultAppException() : base("Failed to open the configuration file with default app.") { }
}
