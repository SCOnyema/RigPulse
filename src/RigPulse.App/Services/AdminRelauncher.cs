using System.ComponentModel;
using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace RigPulse.App.Services;

/// <summary>
/// The outcome of <see cref="IAdminRelauncher.RelaunchElevated"/>.
/// </summary>
public enum RelaunchResult
{
    /// <summary>The elevated instance started; this one should exit.</summary>
    Started,

    /// <summary>The user declined the UAC prompt; keep running as is.</summary>
    Cancelled,

    /// <summary>The elevated instance could not be started; keep running as is.</summary>
    Failed,
}

/// <summary>
/// Starts a new, elevated instance of the app.
/// </summary>
public interface IAdminRelauncher
{
    /// <summary>
    /// Starts this executable again with the "runas" verb, which shows the UAC prompt. Never throws.
    /// </summary>
    RelaunchResult RelaunchElevated();
}

internal sealed partial class AdminRelauncher(ILogger<AdminRelauncher> logger) : IAdminRelauncher
{
    /// <summary>Win32 ERROR_CANCELLED: the user chose "No" on the UAC prompt.</summary>
    private const int ErrorCancelled = 1223;

    public RelaunchResult RelaunchElevated()
    {
        if (Environment.ProcessPath is not { } path)
        {
            LogFailed(null);
            return RelaunchResult.Failed;
        }

        var startInfo = new ProcessStartInfo(path)
        {
            UseShellExecute = true, // required for the "runas" verb
            Verb = "runas",
            WorkingDirectory = Environment.CurrentDirectory,
        };

        foreach (var arg in Environment.GetCommandLineArgs().Skip(1))
        {
            startInfo.ArgumentList.Add(arg);
        }

        try
        {
            using var process = Process.Start(startInfo);
            LogStarted();
            return RelaunchResult.Started;
        }
        catch (Win32Exception ex) when (ex.NativeErrorCode == ErrorCancelled)
        {
            LogCancelled();
            return RelaunchResult.Cancelled;
        }
        catch (Exception ex) when (ex is Win32Exception or InvalidOperationException or PlatformNotSupportedException)
        {
            LogFailed(ex);
            return RelaunchResult.Failed;
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Started an elevated instance; exiting this one.")]
    private partial void LogStarted();

    [LoggerMessage(Level = LogLevel.Information, Message = "Restart as administrator was cancelled at the UAC prompt.")]
    private partial void LogCancelled();

    [LoggerMessage(Level = LogLevel.Warning, Message = "Could not restart as administrator.")]
    private partial void LogFailed(Exception? exception);
}
