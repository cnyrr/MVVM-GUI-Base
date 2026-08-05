namespace Wpf.Shell.Services.Toasts.Contracts
{
    /// <summary>
    /// Toast display duration preset. The preset → wall-clock mapping is owned
    /// by the toast service and intended to become user-configurable via the
    /// settings subsystem.
    /// </summary>
    /// <remarks>
    /// Errors ignore this entirely; <see cref="ToastSeverity.Error"/> toasts are
    /// sticky until manually dismissed.
    /// </remarks>
    public enum ToastDuration
    {
        Short,
        Normal,
        Long,
    }
}
