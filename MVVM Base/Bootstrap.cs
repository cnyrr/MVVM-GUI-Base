using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MVVM_Base.Services.Navigation;
using MVVM_Base.Services.Navigation.Contracts;
using MVVM_Base.Services.Navigation.Internal;
using MVVM_Base.Services.Theming.Contracts;
using MVVM_Base.Services.Theming;
using MVVM_Base.ViewModels;
using MVVM_Base.ViewModels.Test;
using System.Windows;

namespace MVVM_Base
{
    /// <summary>
    /// Application bootstrap. Encapsulates the discrete startup phases — service
    /// registration, host build, theme application, navigation registration,
    /// main-window construction — so <see cref="App"/> orchestrates them rather
    /// than performing them directly.
    ///
    /// Each method does one thing. Order matters and is enforced by the calling
    /// orchestrator in <see cref="App.OnStartup"/>.
    /// </summary>
    internal static class Bootstrap
    {
        /// <summary>
        /// Builds the host: registers all services, then constructs the
        /// <see cref="IHost"/> and starts it. The returned host is owned by the
        /// caller; dispose it in <c>OnExit</c>.
        /// </summary>
        public static async Task<IHost> BuildHostAsync(string[] args)
        {
            var builder = Host.CreateApplicationBuilder(args);
            ConfigureServices(builder.Services);

            var host = builder.Build();
            await host.StartAsync();
            return host;
        }

        /// <summary>
        /// Applies the initial theme. Today: hardcoded <see cref="Theme.Light"/>.
        /// Future: read from persisted settings — see deferred-decisions.md
        /// ("Persisted Theme Selection"). Must run before any window is shown so
        /// the first render uses themed brushes.
        /// </summary>
        public static void ApplyInitialTheme(IServiceProvider services)
        {
            var theming = services.GetRequiredService<ITheming>();
            theming.Apply(Theme.Light);
        }

        /// <summary>
        /// Registers tab roots with the navigation service and sets the initial
        /// tab. After this returns, <see cref="INavigationFacade.CurrentViewModel"/>
        /// resolves to the initial tab's root VM.
        /// </summary>
        public static async Task ConfigureNavigationAsync(IServiceProvider services)
        {
            var nav = services.GetRequiredService<INavigationService>();

            nav.RegisterTab<TestRootViewModel>(TabKey.Test);
            // Additional RegisterTab<...>(...) calls go here as tabs are added.

            await nav.SetInitialTabAsync(TabKey.Test);
        }

        /// <summary>
        /// Constructs the main window with its DataContext set to
        /// <see cref="ShellViewModel"/>. Caller is responsible for assigning
        /// <see cref="Application.MainWindow"/> and calling <c>Show()</c>.
        /// </summary>
        public static Window CreateMainWindow(IServiceProvider services)
        {
            var shellVm = services.GetRequiredService<ShellViewModel>();
            return new MainWindow { DataContext = shellVm };
        }

        // ----- private helpers -----

        private static void ConfigureServices(IServiceCollection services)
        {
            // Navigation framework: registers INavigationService, INavigationFacade,
            // IViewModelFactory, and any internal collaborators.
            services.AddNavigation();

            // Theming: registers ITheming. Theme is applied after host build.
            services.AddTheming();

            // ---- ViewModels ----
            // Tab root VMs are singletons (per the architecture).
            services.AddSingleton<TestRootViewModel>();
            // Detail VMs (transient) get registered alongside their root as the app grows:
            //   services.AddTransient<SomeDetailViewModel>();

            // ---- Shell ----
            services.AddSingleton<ShellViewModel>();

            // ---- Application services (logging, config, settings, dialogs, etc.) ----
            // Add as they come online. Deferred for this iteration.
        }
    }
}
