using System;

namespace XeniaManager.Core.Exceptions;

public class WindowsBuildAssetMissingException : Exception
{
    public WindowsBuildAssetMissingException() : base("Windows build asset missing in the release") { }
}
