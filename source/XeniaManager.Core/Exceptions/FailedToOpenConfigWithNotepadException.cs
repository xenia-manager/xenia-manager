using System;

namespace XeniaManager.Core.Exceptions;

public class FailedToOpenConfigWithNotepadException : Exception
{
    public FailedToOpenConfigWithNotepadException() : base("Failed to open the configuration file with notepad.") { }
}
