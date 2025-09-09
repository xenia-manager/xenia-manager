using System;

namespace XeniaManager.Core.Exceptions;

/// <summary>
/// Exception thrown when no Xenia version is installed
/// </summary>
public class NoXeniaInstalledException : InvalidOperationException
{
    /// <summary>
    /// Initializes a new instance of the NoXeniaInstalledException class
    /// </summary>
    public NoXeniaInstalledException() : base("No Xenia version installed. Install Xenia before continuing.")
    {
    }

    /// <summary>
    /// Initializes a new instance of the NoXeniaInstalledException class with a custom message
    /// </summary>
    /// <param name="message">The error message</param>
    public NoXeniaInstalledException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the NoXeniaInstalledException class with a custom message and inner exception
    /// </summary>
    /// <param name="message">The error message</param>
    /// <param name="innerException">The inner exception</param>
    public NoXeniaInstalledException(string message, Exception innerException) : base(message, innerException)
    {
    }
}