namespace MVVM_Base.Services.Monitor.Contracts
{
    /// <summary>
    /// What a secondary display is set to. Sticky operator intent — only the dock changes it; never
    /// navigation. The rendered surface derives from this (see the monitor shell): Logo paints the
    /// dock over a dark/blank content area, Mirror reflects the touchscreen's current view, Enrich
    /// shows the selected snippet (falling back to the pre-Enrich surface when the current ViewModel
    /// offers no catalog).
    /// </summary>
    public enum MonitorMode
    {
        /// <summary>Dark/blank content, themed dock only, toasts suppressed. Power-off-like idle.</summary>
        Logo,

        /// <summary>Reflects the touchscreen's current view; may forward input to the shared
        /// navigation context when not input-locked.</summary>
        Mirror,

        /// <summary>Shows the current ViewModel's selected snippet; never touches navigation.</summary>
        Enrich,
    }
}