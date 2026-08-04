using Microsoft.Extensions.Logging;
using Wpf.Shell.ViewModels;

namespace Wpf.Shell.Bench.ViewModels.TestMonitor
{
    /// <summary>Second plain snippet. Selecting between this and Polling on two displays proves
    /// independent per-screen selection and per-screen instances.</summary>
    public sealed partial class ChartSnippetViewModel : ViewModelBase
    {
        public ChartSnippetViewModel(ILogger<ChartSnippetViewModel> logger) : base(logger) { }

        public string Caption => "Chart snippet — placeholder content, independently selectable per screen.";
    }
}