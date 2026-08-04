using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Text;

namespace Wpf.Shell.Services.Toasts.Contracts
{
    /// <summary>
    /// Application-level service for surfacing transient notifications (toasts).
    /// Auto-marshals to the UI thread; safe to call from any thread.
    /// </summary>
    public interface IToastService
    {
        /// <summary>
        /// Surfaces a toast. <paramref name="options"/> may be <c>null</c> to
        /// accept all defaults (Info severity, Normal duration).
        /// </summary>
        void Show(string message, ToastOptions? options = null);

        /// <summary>Surfaces an informational toast.</summary>
        void Info(string message, ToastDuration duration = ToastDuration.Normal);

        /// <summary>Surfaces a success toast.</summary>
        void Success(string message, ToastDuration duration = ToastDuration.Normal);

        /// <summary>Surfaces a warning toast.</summary>
        void Warning(string message, ToastDuration duration = ToastDuration.Normal);

        /// <summary>
        /// Surfaces an error toast. Errors are sticky and do not auto-dismiss.
        /// </summary>
        void Error(string message);
    }
}
