using Microsoft.Extensions.Logging;

namespace Wpf.Shell.ViewModels.TestMonitor
{
    /// <summary>Plain snippet, no lifecycle. Proves a non-IMonitorScreenAware catalog member works.</summary>
    public sealed partial class StaticSnippetViewModel : ViewModelBase
    {
        public StaticSnippetViewModel(ILogger<StaticSnippetViewModel> logger) : base(logger) { }

        public string Caption => "Static snippet — no live resource, nothing to start or stop.";
    }
}