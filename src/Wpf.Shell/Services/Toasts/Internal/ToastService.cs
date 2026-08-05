using System.Windows;
using Wpf.Shell.Services.Toasts.Contracts;
using Wpf.Shell.ViewModels.Toasts;


namespace Wpf.Shell.Services.Toasts.Internal
{
    /// <summary>
    /// <see cref="IToastService"/> implementation. Auto-marshals to the UI thread
    /// (toast surfacing must be ergonomic from background services). All real
    /// work is delegated to <see cref="ToastHostViewModel"/>.
    /// </summary>
    internal sealed class ToastService : IToastService
    {
        private readonly ToastHostViewModel _host;

        public ToastService(ToastHostViewModel host)
        {
            _host = host;
        }

        public void Show(string message, ToastOptions? options = null)
        {
            var opts = options ?? new ToastOptions();
            var dispatcher = Application.Current.Dispatcher;

            if (!dispatcher.CheckAccess())
            {
                dispatcher.BeginInvoke(new Action(() => Show(message, opts)));
                return;
            }

            var isSticky = opts.Severity == ToastSeverity.Error;
            var duration = DurationToTimeSpan(opts.Duration);
            var toast = new ToastViewModel(message, opts.Severity, isSticky);

            _host.Enqueue(toast, duration);
        }

        public void Info(string message, ToastDuration duration = ToastDuration.Normal) =>
            Show(message, new ToastOptions { Severity = ToastSeverity.Info, Duration = duration });

        public void Success(string message, ToastDuration duration = ToastDuration.Normal) =>
            Show(message, new ToastOptions { Severity = ToastSeverity.Success, Duration = duration });

        public void Warning(string message, ToastDuration duration = ToastDuration.Normal) =>
            Show(message, new ToastOptions { Severity = ToastSeverity.Warning, Duration = duration });

        public void Error(string message) =>
            Show(message, new ToastOptions { Severity = ToastSeverity.Error });

        /// <summary>
        /// Maps the <see cref="ToastDuration"/> preset to a wall-clock value.
        /// Hardcoded today; intended to read from settings once that subsystem
        /// lands.
        /// </summary>
        private static TimeSpan DurationToTimeSpan(ToastDuration d) => d switch
        {
            ToastDuration.Short => TimeSpan.FromSeconds(2),
            ToastDuration.Normal => TimeSpan.FromSeconds(3.5),
            ToastDuration.Long => TimeSpan.FromSeconds(6),
            _ => TimeSpan.FromSeconds(3.5),
        };
    }
}
