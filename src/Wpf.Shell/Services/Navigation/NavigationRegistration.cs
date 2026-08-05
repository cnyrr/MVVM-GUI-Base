using Microsoft.Extensions.DependencyInjection;
using Wpf.Shell.Services.Navigation.Contracts;
using Wpf.Shell.Services.Navigation.Internal;
using Wpf.Shell.ViewModels;

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

        /// <summary>
        /// Registers <typeparamref name="TRoot"/> as a tab root.
        ///
        /// Performs two registrations in one call:
        /// <list type="bullet">
        ///   <item>Adds <typeparamref name="TRoot"/> itself as a singleton, so the navigation
        ///   service's <see cref="IViewModelFactory"/> can resolve it during eager construction.</item>
        ///   <item>Adds a <see cref="TabRegistration"/> singleton carrying
        ///   <c>typeof(TRoot)</c>. The DI container aggregates all such registrations into
        ///   <see cref="IEnumerable{TabRegistration}"/>, which the navigation service consumes
        ///   to build its tab list.</item>
        /// </list>
        ///
        /// Constraint chain enforces correctness at compile time: <typeparamref name="TRoot"/>
        /// must derive from <see cref="ViewModelBase"/> (every VM in the app does) and implement
        /// <see cref="IRootViewModel"/> (the load-bearing marker that a VM is suitable as a tab
        /// root). Detail ViewModels — which don't implement <see cref="IRootViewModel"/> — cannot
        /// be passed here, and reaching for this method is itself the signal that a VM intends to
        /// be a tab.
        ///
        /// Call once per tab during <see cref="Microsoft.Extensions.Hosting.IHost"/> service
        /// configuration:
        /// <code>
        /// services.AddNavigation();
        /// services.AddTab&lt;TestRootViewModel&gt;();
        /// services.AddTab&lt;CustomersRootViewModel&gt;();
        /// </code>
        /// </summary>
        public static IServiceCollection AddTab<TRoot>(this IServiceCollection services)
            where TRoot : ViewModelBase, IRootViewModel
        {
            services.AddSingleton<TRoot>();
            services.AddSingleton(new TabRegistration(typeof(TRoot)));
            return services;
        }
    }
}