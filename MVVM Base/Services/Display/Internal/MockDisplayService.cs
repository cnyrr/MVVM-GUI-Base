using System.Collections.Generic;
using System.Windows;
using MVVM_Base.Services.Display.Contracts;

namespace MVVM_Base.Services.Display.Internal
{
    /// <summary>
    /// Development <see cref="IDisplayService"/>: fabricates one primary plus three windowed
    /// secondaries, laid out on a single physical screen as a top row of three monitors with the
    /// main screen filling the area below. Bounds are derived from the real primary work area so the
    /// layout fits whatever machine runs it. The real Win32 implementation replaces only this class.
    /// </summary>
    internal sealed class MockDisplayService : IDisplayService
    {
        public IReadOnlyList<DisplayInfo> Displays { get; }

        public MockDisplayService()
        {
            // Real usable screen area (excludes taskbar). SystemParameters is available app-wide.
            var areaW = SystemParameters.WorkArea.Width;
            var areaH = SystemParameters.WorkArea.Height;
            var originX = SystemParameters.WorkArea.Left;
            var originY = SystemParameters.WorkArea.Top;

            // Top row: three equal-width secondaries occupying the top third.
            var rowH = areaH / 3.0;
            var colW = areaW / 3.0;

            // Main: the remaining lower two-thirds, full width.
            var mainY = originY + rowH;
            var mainH = areaH - rowH;

            Displays = new[]
            {
                // Primary (touchscreen / main) — below the row. Not windowed; fills its area.
                new DisplayInfo(0, originX, mainY, areaW, mainH, IsPrimary: true, Windowed: true),

                // Three secondaries across the top, left → right.
                new DisplayInfo(1, originX + 0 * colW, originY, colW, rowH, IsPrimary: false, Windowed: true),
                new DisplayInfo(2, originX + 1 * colW, originY, colW, rowH, IsPrimary: false, Windowed: true),
                new DisplayInfo(3, originX + 2 * colW, originY, colW, rowH, IsPrimary: false, Windowed: true),
            };
        }
    }
}