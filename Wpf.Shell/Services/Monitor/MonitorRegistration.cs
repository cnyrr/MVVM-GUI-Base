using Microsoft.Extensions.DependencyInjection;
using Wpf.Shell.Services.Monitor.Contracts;
using Wpf.Shell.Services.Monitor.Internal;

namespace Wpf.Shell.Services.Monitor
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