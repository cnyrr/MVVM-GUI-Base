using System.Collections.Generic;
using System.Linq;

namespace MVVM_Base.Services.Display.Contracts
{
    /// <summary>
    /// Reports the display topology and supplies placement data for monitor windows. The single
    /// seam between the monitor subsystem and physical hardware: every layer above consumes this
    /// interface, so swapping the implementation (mock single-screen ↔ real multi-monitor) changes
    /// nothing above it.
    /// </summary>
    public interface IDisplayService
    {
        /// <summary>All displays, primary first. Enumerated once at startup (hot-plug deferred).</summary>
        IReadOnlyList<DisplayInfo> Displays { get; }
    }

    /// <summary>Convenience accessors over <see cref="IDisplayService.Displays"/>.</summary>
    public static class DisplayServiceExtensions
    {
        public static DisplayInfo Primary(this IDisplayService s) => s.Displays.First(d => d.IsPrimary);
        public static IEnumerable<DisplayInfo> Secondaries(this IDisplayService s) => s.Displays.Where(d => !d.IsPrimary);
        public static bool HasSecondary(this IDisplayService s) => s.Displays.Any(d => !d.IsPrimary);
    }
}