using Microsoft.Extensions.DependencyInjection;
using MVVM_Base.Services.Display.Contracts;
using MVVM_Base.Services.Display.Internal;
using MVVM_Base.Services.Monitor.Contracts;
using MVVM_Base.Services.Monitor.Internal;

namespace MVVM_Base.Services.Monitor
{
    /// <summary>
    /// DI registration for the secondary-display subsystem. Called unconditionally from Bootstrap so
    /// the subsystem is compiled in; whether any monitor window is actually created is decided at
    /// bootstrap from <see cref="IDisplayService"/>. Not calling <c>AddMonitor()</c> at all is the
    /// switch for shipping a build without the subsystem.
    ///
    /// The mock display service is wired here for now; the real Win32 implementation swaps only this
    /// one line.
    /// </summary>
    public static class MonitorRegistration
    {
        public static IServiceCollection AddMonitor(this IServiceCollection services)
        {
            services.AddSingleton<IDisplayService, MockDisplayService>();
            services.AddSingleton<IMonitorSettings, MonitorSettings>();
            services.AddSingleton<MonitorShellFactory>();
            return services;
        }
    }
}