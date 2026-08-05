using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Wpf.Shell.Services.Navigation;
using Wpf.Shell.Services.Navigation.Contracts;
using Wpf.Shell.Services.Navigation.Internal;
using Wpf.Shell.Services.Theming;
using Wpf.Shell.Services.Theming.Contracts;
using Wpf.Shell.Services.Toasts;
using Wpf.Shell.Services.Toasts.Internal;
using Wpf.Shell.ViewModels.Shell;
using Wpf.Shell.Bench.ViewModels.Test;
using Wpf.Shell.Bench.ViewModels.TestScaling;
using Wpf.Shell.Bench.ViewModels.TestToasts;
using System.Windows;

namespace Wpf.Shell.Bench
{
    /// <summary>
    /// Application bootstrap. Encapsulates the discrete startup phases — service
    /// registration, host build, theme application, initial navigation,
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
        /// Initializes tab roots and activates the initial tab. Tabs themselves were registered
        /// in <see cref="ConfigureServices"/> via <c>AddTab&lt;TRoot&gt;()</c>; this phase
        /// resolves each root (now safe — the DI graph has been built) and chooses which tab is
        /// current at startup.
        ///
        /// After this returns, <see cref="INavigationFacade.CurrentViewModel"/> resolves to the
        /// initial tab's root VM and <see cref="INavigationFacade.ActiveTab"/> is set.
        ///
        /// Three service lookups here — internal navigation service (to initialize tabs), facade,
        /// and initial root — are the composition-root cost for keeping
        /// <c>INavigationFacade</c>'s public surface tight (one switch method, takes an instance)
        /// while breaking the construction-time DI cycle that eager resolution would create.
        /// </summary>
        public static async Task ConfigureNavigationAsync(IServiceProvider services)
        {
            var nav = services.GetRequiredService<INavigationService>();
            nav.InitializeTabs();

            var facade = services.GetRequiredService<INavigationFacade>();
            var initial = services.GetRequiredService<TestRootViewModel>();

            await facade.SwitchTabAsync(initial);
        }

        /// <summary>
        /// Constructs the main window with its DataContext set to
        /// <see cref="ShellViewModel"/>. Caller is responsible for assigning
        /// <see cref="Application.MainWindow"/> and calling <c>Show()</c>.
        /// </summary>
        public static Window CreateMainWindow(IServiceProvider services)
        {
            var shellVm = services.GetRequiredService<ShellViewModel>();

            var window = new MainWindow { DataContext = shellVm };

            return window;
        }

        public static void PublishSharedResources(IServiceProvider services)
        {
            Application.Current.Resources["SharedToastHost"] =
                services.GetRequiredService<ToastHostViewModel>();
        }

        // ----- private helpers -----

        private static void ConfigureServices(IServiceCollection services)
        {
            // Navigation framework: registers INavigationService, INavigationFacade,
            // IViewModelFactory, and any internal collaborators.
            services.AddNavigation();

            // Theming: registers ITheming. Theme is applied after host build.
            services.AddTheming();

            // Toasts: registers IToastService and ToastHostViewModel.
            services.AddToasts();

            // ---- Tab roots ----
            // Each AddTab<T>() registers T as a singleton AND emits a TabRegistration
            // metadata record. The navigation service consumes IEnumerable<TabRegistration>
            // when Bootstrap calls InitializeTabs() and resolves every root via
            // IViewModelFactory. Registration order = sidebar order.
            services.AddTab<TestRootViewModel>();
            services.AddTab<TestToastsRootViewModel>();
            services.AddTab<TestScalingRootViewModel>();
            // Additional AddTab<...>() calls go here as tabs are added.

            // ---- Detail VMs ----
            // Transient — fresh instance per navigation.
            services.AddTransient<Test1ViewModel>();
            services.AddTransient<Test2ViewModel>();
            services.AddTransient<TestStressViewModel>();

            // ---- Shell cluster ----
            services.AddSingleton<ShellViewModel>();
            services.AddSingleton<SidebarViewModel>();

            // ---- Application services (logging, config, settings, dialogs, etc.) ----
            // Add as they come online. Deferred for this iteration.
        }
    }
}