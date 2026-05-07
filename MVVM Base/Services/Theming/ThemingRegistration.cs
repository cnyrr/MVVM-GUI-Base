using Microsoft.Extensions.DependencyInjection;
using MVVM_Base.Services.Theming.Contracts;
using MVVM_Base.Services.Theming.Internal;

namespace MVVM_Base.Services.Theming
{
    /// <summary>
    /// DI registration for the theming subsystem.
    /// </summary>
    public static class ThemingRegistration
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
