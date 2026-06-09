using MVVM_Base.Services.Monitor.Contracts;

namespace MVVM_Base.Services.Monitor.Internal
{
    /// <summary>
    /// Stub <see cref="IMonitorSettings"/> with hardcoded defaults. Dev defaults deliberately span
    /// the three modes so the subsystem's independence is visible on first run; replaced by the
    /// settings panel later.
    /// </summary>
    internal sealed class MonitorSettings : IMonitorSettings
    {
        public MonitorMode InitialMode(int displayIndex) => displayIndex switch
        {
            1 => MonitorMode.Mirror,
            2 => MonitorMode.Enrich,
            _ => MonitorMode.Logo,
        };

        public bool InitialInputLocked(int displayIndex) => false;
    }
}