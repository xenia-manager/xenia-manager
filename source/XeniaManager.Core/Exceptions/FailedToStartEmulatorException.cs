using System;

namespace XeniaManager.Core.Exceptions;

public class FailedToStartEmulatorException : Exception
{
    public FailedToStartEmulatorException() : base("Failed to start emulator.") { }
}
