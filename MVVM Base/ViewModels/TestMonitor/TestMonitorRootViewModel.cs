using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using MVVM_Base.Services.Navigation.Contracts;
using MVVM_Base.ViewModels;

namespace MVVM_Base.ViewModels.TestMonitor
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