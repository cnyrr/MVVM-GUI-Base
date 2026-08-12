using Microsoft.Extensions.DependencyInjection;
using Wpf.Shell.Services.Navigation.Contracts;
using Wpf.Shell.Services.Navigation.Internal;

namespace Wpf.Shell.Services.Navigation
{
    /// <summary>
    /// DI registration for the navigation framework. Call <see cref="AddNavigation"/> once
    /// during composition (typically in <c>Bootstrap.cs</c>) to register the navigation service,
    /// the facade, and the ViewModel factory. Then call <see cref="AddTab{TRoot}"/> once per tab
    /// to register its root ViewModel.
    /// </summary>
    internal static class NavigationRegistration
    {
        /// <summary>
        /// Registers the navigation framework's core services as singletons.
        /// </summary>
        public static IServiceCollection AddNavigation(this IServiceCollection services)
        {
            services.AddSingleton<IViewModelFactory, ViewModelFactory>();
            services.AddSingleton<INavigationService, NavigationService>();
            services.AddSingleton<INavigationFacade, NavigationFacade>();
            return services;
        }
    }
}