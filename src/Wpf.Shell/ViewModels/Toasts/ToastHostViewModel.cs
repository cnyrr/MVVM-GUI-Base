using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;
using System.Windows.Threading;
using Wpf.Shell.Services.Toasts.Internal;

namespace Wpf.Shell.ViewModels.Toasts
{
    /// <summary>
    /// Owns the visible toast collection, the overflow queue, and all timing
    /// behavior (expiration, pause/resume on hover). A single dispatcher timer
    /// drives the whole subsystem; per-toast timers are deliberately avoided.
    /// </summary>
    /// <remarks>
    /// Threading: this class is UI-thread-only. <see cref="ToastService"/>
    /// marshals incoming requests before invoking <see cref="Enqueue"/>.
    /// </remarks>
    internal sealed partial class ToastHostViewModel : ViewModelBase
    {
        private const int VisibleCap = 3;
        private static readonly TimeSpan TickInterval = TimeSpan.FromMilliseconds(100);

        private readonly Queue<ToastViewModel> _queue = new();
        private readonly Dictionary<ToastViewModel, DateTime> _expiresAt = new();
        private readonly Dictionary<ToastViewModel, TimeSpan> _scheduledDuration = new();
        private readonly DispatcherTimer _timer;

        private DateTime? _pauseStartedAt;

        public ToastHostViewModel(ILogger<ToastHostViewModel> logger)
            : base(logger)
        {
            _timer = new DispatcherTimer(DispatcherPriority.Background)
            {
                Interval = TickInterval,
            };
            _timer.Tick += OnTick;
        }

        /// <summary>
        /// Toasts currently rendered in the host (newest at index 0). Capped at
        /// <see cref="VisibleCap"/>; further arrivals queue.
        /// </summary>
        public ObservableCollection<ToastViewModel> VisibleToasts { get; } = new();

        /// <summary>
        /// Adds a toast. If the visible cap is reached the toast is queued and
        /// surfaces when an existing toast dismisses.
        /// </summary>
        public void Enqueue(ToastViewModel toast, TimeSpan duration)
        {
            if (VisibleToasts.Count < VisibleCap)
            {
                Promote(toast, duration);
            }
            else
            {
                _queue.Enqueue(toast);
                _scheduledDuration[toast] = duration;
            }
        }

        // ===== Pause / resume =====
        // Bound by the host view's MouseEnter / MouseLeave on the toast region.
        // While paused, expirations don't fire; on resume, scheduled times are
        // shifted forward by the pause duration so toasts retain their full
        // remaining display time.

        [RelayCommand]
        private void Pause()
        {
            if (_pauseStartedAt is not null)
                return;

            _pauseStartedAt = DateTime.UtcNow;
        }

        [RelayCommand]
        private void Resume()
        {
            if (_pauseStartedAt is null)
                return;

            var pauseDuration = DateTime.UtcNow - _pauseStartedAt.Value;
            _pauseStartedAt = null;

            // Extend every visible toast's expiration by how long we were paused.
            // Sticky toasts have no entry in _expiresAt and are unaffected.
            foreach (var key in new List<ToastViewModel>(_expiresAt.Keys))
            {
                _expiresAt[key] = _expiresAt[key] + pauseDuration;
            }
        }

        // ===== Manual dismiss =====
        // Bound by the X button on each toast view. The host owns the command;
        // the toast itself stays pure data.

        [RelayCommand]
        private void DismissToast(ToastViewModel? toast)
        {
            if (toast is null)
                return;

            Remove(toast);
            DrainQueue();
        }

        // ===== Internals =====

        private void Promote(ToastViewModel toast, TimeSpan duration)
        {
            // Newest at index 0 — visual stacking is bottom-up from the corner,
            // i.e. the newest toast renders nearest the corner.
            VisibleToasts.Insert(0, toast);

            if (!toast.IsSticky)
            {
                _scheduledDuration[toast] = duration;
                _expiresAt[toast] = DateTime.UtcNow + duration;
                EnsureTimerRunning();
            }
        }

        private void Remove(ToastViewModel toast)
        {
            VisibleToasts.Remove(toast);
            _expiresAt.Remove(toast);
            _scheduledDuration.Remove(toast);

            if (_expiresAt.Count == 0)
                _timer.Stop();
        }

        private void DrainQueue()
        {
            while (VisibleToasts.Count < VisibleCap && _queue.Count > 0)
            {
                var next = _queue.Dequeue();
                var duration = _scheduledDuration.TryGetValue(next, out var d)
                    ? d
                    : TimeSpan.FromSeconds(3.5);  // defensive fallback
                _scheduledDuration.Remove(next);

                Promote(next, duration);
            }
        }

        private void EnsureTimerRunning()
        {
            if (!_timer.IsEnabled)
                _timer.Start();
        }

        private void OnTick(object? sender, EventArgs e)
        {
            // Paused: do nothing. Timer keeps ticking; resumes naturally.
            if (_pauseStartedAt is not null)
                return;

            var now = DateTime.UtcNow;

            // Snapshot to avoid mutating-while-iterating.
            List<ToastViewModel>? expired = null;
            foreach (var (toast, expiresAt) in _expiresAt)
            {
                if (expiresAt <= now)
                {
                    expired ??= new List<ToastViewModel>();
                    expired.Add(toast);
                }
            }

            if (expired is null)
                return;

            foreach (var toast in expired)
                Remove(toast);

            DrainQueue();
        }
    }
}