namespace MVVM_Base.Services.Display.Contracts
{
    /// <summary>
    /// A single display in the system topology. Plain data — produced by <see cref="IDisplayService"/>,
    /// consumed by bootstrap to create and place monitor windows.
    ///
    /// Bounds are in device-independent pixels. <see cref="IsPrimary"/> marks the operator's
    /// touchscreen (hosts the main shell); all others are secondary projection surfaces.
    ///
    /// <see cref="Windowed"/> is the dev-vs-production placement hint: when true, the monitor window
    /// renders as a normal movable window (single-screen development); when false, borderless and
    /// positioned to fill the target display (production). The window applies chrome from this flag,
    /// so there is no dev-only window code to strip later — only the display service differs.
    /// </summary>
    public sealed record DisplayInfo(
        int Index,
        double X,
        double Y,
        double Width,
        double Height,
        bool IsPrimary,
        bool Windowed);
}