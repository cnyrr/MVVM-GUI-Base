using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MVVM_Base.Services.Monitor.Contracts;
using MVVM_Base.Services.Monitor.Internal;
using MVVM_Base.Services.Navigation.Contracts;
using MVVM_Base.ViewModels.Monitor;

namespace MVVM_Base.Services.Monitor
{
    /// <summary>
    /// Builds one MonitorShellViewModel per display index, each with its own per-display
    /// MonitorEnrichmentHost. The host is constructed here (not a DI singleton) because each display
    /// needs an independent cache/liveness; the shared collaborators it needs (ISnippetFactory,
    /// INavigationFacade) are pulled from DI.
    /// </summary>
    internal sealed class MonitorShellFactory
    {
        private readonly IServiceProvider _sp;
        public MonitorShellFactory(IServiceProvider sp) => _sp = sp;

        public MonitorShellViewModel Create(int displayIndex)
        {
            var host = new MonitorEnrichmentHost(
                _sp.GetRequiredService<ISnippetFactory>(),
                _sp.GetRequiredService<INavigationFacade>());

            return new MonitorShellViewModel(
                displayIndex,
                _sp.GetRequiredService<INavigationFacade>(),
                host,
                _sp.GetRequiredService<IMonitorSettings>(),
                _sp.GetRequiredService<ILogger<MonitorShellViewModel>>());
        }
    }
}