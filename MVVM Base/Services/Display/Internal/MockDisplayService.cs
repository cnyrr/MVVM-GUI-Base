using System.Collections.Generic;
using System.Windows;
using MVVM_Base.Services.Display.Contracts;

namespace MVVM_Base.Services.Display.Internal
{
    /// <summary>
    /// Development <see cref="IDisplayService"/>: fabricates a topology on a single physical screen.
    /// <c>secondaryCount</c> = 0 yields Case A (one maximized primary, no secondaries); a positive
    /// count yields Case B (primary in the lower area, that many secondaries tiled across the top).
    /// The real Win32 implementation replaces this class entirely.
    /// </summary>
    internal sealed class MockDisplayService : IDisplayService
    {
        public IReadOnlyList<DisplayInfo> Displays { get; }

        public MockDisplayService(int secondaryCount)
        {
            var area = SystemParameters.WorkArea;
            var list = new List<DisplayInfo>();

            if (secondaryCount <= 0)
            {
                // Case A: single primary, maximized (Windowed:false), full screen.
                list.Add(new DisplayInfo(
                    0, area.Left, area.Top, area.Width, area.Height,
                    IsPrimary: true, Windowed: false));
            }
            else
            {
                // Case B (dev): primary fills the lower area; secondaries tile across the top.
                var rowH = area.Height / 3.0;
                var colW = area.Width / secondaryCount;

                list.Add(new DisplayInfo(
                    0, area.Left, area.Top + rowH, area.Width, area.Height - rowH,
                    IsPrimary: true, Windowed: true));

                for (int i = 0; i < secondaryCount; i++)
                    list.Add(new DisplayInfo(
                        i + 1, area.Left + i * colW, area.Top, colW, rowH,
                        IsPrimary: false, Windowed: true));
            }

            Displays = list;
        }
    }
}