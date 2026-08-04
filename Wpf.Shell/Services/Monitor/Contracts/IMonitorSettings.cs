namespace Wpf.Shell.Services.Monitor.Contracts
{
    /// <summary>
    /// Per-display configuration the monitor shell reads at startup: initial mode and initial input
    /// lock, keyed by the display index from <see cref="Display.Contracts.DisplayInfo.Index"/>.
    ///
    /// A stub today (hardcoded defaults); the future settings panel backs this without the shell
    /// changing. Read-only here — runtime changes flow through the dock onto the shell's own state,
    /// not back through this surface. Persistence is a later concern.
    /// </summary>
    public interface IMonitorSettings
    {
        /// <summary>Initial mode for the display at <paramref name="displayIndex"/>.</summary>
        MonitorMode InitialMode(int displayIndex);

        /// <summary>Initial input-lock for the display at <paramref name="displayIndex"/>.</summary>
        bool InitialInputLocked(int displayIndex);
    }
}