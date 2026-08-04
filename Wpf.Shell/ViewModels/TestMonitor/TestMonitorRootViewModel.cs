using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Wpf.Shell.Services.Navigation.Contracts;
using Wpf.Shell.ViewModels;

namespace Wpf.Shell.ViewModels.TestMonitor
{
    /// <summary>Tab root that offers three snippets to secondary displays.</summary>
    public sealed partial class TestMonitorRootViewModel : ViewModelBase, IRootViewModel, IMonitorAware
    {
        public string TabLabel => "Monitor Tests";

        public TestMonitorRootViewModel(ILogger<TestMonitorRootViewModel> logger) : base(logger) { }

        public IReadOnlyList<SnippetDefinition> SnippetCatalog { get; } = new[]
        {
            SnippetDefinition.For<StaticSnippetViewModel>("static", "Static"),
            SnippetDefinition.For<PollingSnippetViewModel>("polling", "Polling"),
            SnippetDefinition.For<ChartSnippetViewModel>("chart", "Chart"),
        };
    }
}