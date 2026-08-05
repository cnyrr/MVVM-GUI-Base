using Microsoft.Extensions.DependencyInjection;
using Wpf.Shell.Services.Theming.Contracts;
using Wpf.Shell.Services.Theming.Internal;

namespace Wpf.Shell.Services.Theming
{
    /// <summary>
    /// DI registration for the theming subsystem.
    /// </summary>
    internal static class ThemingRegistration
    {
        /// <summary>
        /// Registers <see cref="ITheming"/> as a singleton. The initial theme is
        /// NOT applied here — composition root calls <c>ITheming.Apply(...)</c>
        /// during startup, after the host is built and before the main window is
        /// shown.
        /// </summary>
        public static IServiceCollection AddTheming(this IServiceCollection services)
        {
            services.AddSingleton<ITheming, ThemingService>();
            return services;
        }
    }
}
