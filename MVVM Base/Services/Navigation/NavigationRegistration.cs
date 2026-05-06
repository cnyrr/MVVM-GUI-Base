using Microsoft.Extensions.DependencyInjection;
using MVVM_Base.Services;
using MVVM_Base.Services.Navigation.Contracts;
using MVVM_Base.Services.Navigation.Internal;

namespace MVVM_Base.Services.Navigation
{
    /// <summary>
    /// DI registration for the navigation framework. Call <see cref="AddNavigation"/> once
    /// during composition (typically in <c>App.xaml.cs</c>) to register the navigation service,
    /// the facade, and the ViewModel factory.
    /// </summary>
    public static class NavigationRegistration
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
