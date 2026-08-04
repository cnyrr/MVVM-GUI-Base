using System;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using Wpf.Shell.Services.Navigation.Contracts;

namespace Wpf.Shell.ViewModels.TestMonitor
{
    /// <summary>
    /// Demonstrates <see cref="IMonitorScreenAware"/>. A timer increments <see cref="Ticks"/> ONLY
    /// while this snippet is the visible content of a live Enrich display. Watch <see cref="Status"/>
    /// flip and <see cref="Ticks"/> freeze/resume as you navigate, switch snippets, or change mode —
    /// and note the count is preserved across hide/show because the instance is cached.
    /// </summary>
    public sealed partial class PollingSnippetViewModel : ViewModelBase, IMonitorScreenAware, IDisposable
    {
        private readonly DispatcherTimer _timer;

        public PollingSnippetViewModel(ILogger<PollingSnippetViewModel> logger) : base(logger)
        {
            _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
            _timer.Tick += (_, _) => Ticks++;
        }

        /// <summary>Number of timer ticks accrued while visible. Frozen while hidden.</summary>
        [ObservableProperty]
        private int _ticks;

        /// <summary>"POLLING" while shown, "stopped" while hidden. Bound large in the view.</summary>
        [ObservableProperty]
        private string _status = "stopped";

        public void OnShown() { Status = "POLLING"; _timer.Start(); }
        public void OnHidden() { Status = "stopped"; _timer.Stop(); }

        public void Dispose() => _timer.Stop();
    }
}
