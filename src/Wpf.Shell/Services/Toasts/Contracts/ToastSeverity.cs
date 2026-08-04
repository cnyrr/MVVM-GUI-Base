using System;
using System.Collections.Generic;
using System.Text;

namespace Wpf.Shell.Services.Toasts.Contracts
{
    /// <summary>
    /// Toast severity level. Determines accent color, default behavior
    /// (errors are sticky), and the icon.
    /// </summary>
    public enum ToastSeverity
    {
        Info,
        Success,
        Warning,
        Error,
    }
}
