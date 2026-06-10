using Microsoft.Extensions.DependencyInjection;
using MVVM_Base.Services.Monitor.Contracts;
using MVVM_Base.Services.Monitor.Internal;

namespace MVVM_Base.Services.Monitor
{
    public static class MonitorRegistration
    {
        public static IServiceCollection AddMonitor(this IServiceCollection services)
        {
            services.AddSingleton<IMonitorSettings, MonitorSettings>();
            services.AddSingleton<ISnippetFactory, SnippetFactory>();
            services.AddSingleton<MonitorShellFactory>();
            return services;
        }
    }
}