using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MVVM_Base.Services.Monitor.Contracts;
using MVVM_Base.Services.Navigation.Contracts;
using MVVM_Base.ViewModels.Monitor;

namespace MVVM_Base.Services.Monitor
{
    /// <summary>Builds one MonitorShellViewModel per display index, pulling collaborators from DI.</summary>
    internal sealed class MonitorShellFactory
    {
        private readonly IServiceProvider _sp;
        public MonitorShellFactory(IServiceProvider sp) => _sp = sp;

        public MonitorShellViewModel Create(int displayIndex) =>
            new(displayIndex,
                _sp.GetRequiredService<INavigationFacade>(),
                _sp.GetRequiredService<IViewModelFactory>(),
                _sp.GetRequiredService<IMonitorSettings>(),
                _sp.GetRequiredService<ILogger<MonitorShellViewModel>>());
    }
}