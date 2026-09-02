using System;
using System.Threading.Tasks;
using XeniaManager.Logging;

namespace XeniaManager.BigScreen.Utilities;

/// <summary>
/// Safe fire-and-forget task execution: exceptions are logged, never swallowed.
/// </summary>
public static class TaskUtilities
{
    /// <summary>
    /// Runs the given work without awaiting it, logging any exception (including
    /// synchronous ones thrown while the work is being constructed).
    /// </summary>
    /// <typeparam name="T">The caller type, used for log prefixes.</typeparam>
    public static void RunSafely<T>(Func<Task> work, string operation) => _ = RunCore<T>(work, operation);

    /// <summary>
    /// Awaits the work and logs any exception that escapes it.
    /// </summary>
    private static async Task RunCore<T>(Func<Task> work, string operation)
    {
        try
        {
            await work();
        }
        catch (Exception ex)
        {
            Logger.Error<T>($"Unhandled exception in '{operation}'");
            Logger.LogExceptionDetails<T>(ex);
        }
    }
}