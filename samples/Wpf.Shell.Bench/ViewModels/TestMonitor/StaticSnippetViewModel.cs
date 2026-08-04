using Microsoft.Extensions.Logging;
using Wpf.Shell.ViewModels;

namespace Wpf.Shell.Bench.ViewModels.TestMonitor
{
    /// <summary>Plain snippet, no lifecycle. Proves a non-IMonitorScreenAware catalog member works.</summary>
    public sealed partial class StaticSnippetViewModel : ViewModelBase
    {
        public StaticSnippetViewModel(ILogger<StaticSnippetViewModel> logger) : base(logger) { }

        public string Caption => "Static snippet — no live resource, nothing to start or stop.";
    }
}