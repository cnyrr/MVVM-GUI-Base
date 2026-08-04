using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Wpf.Shell.Services.Display.Contracts;
using Wpf.Shell.Services.Display.Internal;
using Wpf.Shell.Services.Monitor;
using Wpf.Shell.Services.Navigation;
using Wpf.Shell.Services.Navigation.Contracts;
using Wpf.Shell.Services.Navigation.Internal;
using Wpf.Shell.Services.Theming;
using Wpf.Shell.Services.Theming.Contracts;
using Wpf.Shell.Services.Toasts;
using Wpf.Shell.Services.Toasts.Internal;
using Wpf.Shell.ViewModels.Shell;
using Wpf.Shell.Bench.ViewModels.Test;
using Wpf.Shell.Bench.ViewModels.TestMonitor;
using Wpf.Shell.Bench.ViewModels.TestScaling;
using Wpf.Shell.Bench.ViewModels.TestToasts;
using Wpf.Shell.Views.Monitor;
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
        /// Dev display topology. The enum value IS the secondary-monitor count: SingleScreen = 0 (Case A,
        /// no secondaries), FourScreens = 3 (Case B, three windowed dev monitors). Cast to int for the count.
        /// The real DisplayService, when it exists, ignores this and reads actual hardware.
        /// </summary>
        private enum DevDisplayTopology { SingleScreen = 0, FourScreens = 3 }

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
            var primary = services.GetRequiredService<IDisplayService>().Primary();

            var window = new MainWindow { DataContext = shellVm };

            if (primary.Windowed)
            {
                window.WindowState = WindowState.Normal;
                window.Left = primary.X;
                window.Top = primary.Y;
                window.Width = primary.Width;
                window.Height = primary.Height;
            }
            else
            {
                window.WindowState = WindowState.Maximized; // production: fullscreen on the touchscreen
            }

            return window;
        }

        private static readonly List<Window> _monitorWindows = new();

        /// <summary>
        /// Creates one MonitorWindow per secondary display. Case A (no secondary) → no-op. Each window gets
        /// its own MonitorShellViewModel. Windows are tracked for disposal on exit.
        /// </summary>
        public static void ConfigureMonitors(IServiceProvider services)
        {
            var displays = services.GetRequiredService<IDisplayService>();
            if (!displays.HasSecondary())
                return;

            var factory = services.GetRequiredService<MonitorShellFactory>();
            var toastHost = services.GetRequiredService<ToastHostViewModel>();

            foreach (var d in displays.Secondaries())
            {
                var shell = factory.Create(d.Index);
                var window = new MonitorWindow
                {
                    DataContext = shell,
                    Left = d.X,
                    Top = d.Y,
                    Width = d.Width,
                    Height = d.Height,
                };
                // Toast layer inside MonitorShellView binds the shared ToastHostViewModel; supply it.
                // (Wired via the view's ToastHostView DataContext — see note below.)
                _monitorWindows.Add(window);
                window.Show();
            }
        }

        public static void DisposeMonitors()
        {
            foreach (var w in _monitorWindows)
            {
                (w.DataContext as IDisposable)?.Dispose();
                w.Close();
            }
            _monitorWindows.Clear();
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

            // Monitors: registers IMonitorService and any related VMs.

            // Display topology: dev mock for now. Enum value is the secondary count.
            services.AddSingleton<IDisplayService>(new MockDisplayService((int)DevDisplayTopology.FourScreens));

            services.AddMonitor();

            // ---- Tab roots ----
            // Each AddTab<T>() registers T as a singleton AND emits a TabRegistration
            // metadata record. The navigation service consumes IEnumerable<TabRegistration>
            // when Bootstrap calls InitializeTabs() and resolves every root via
            // IViewModelFactory. Registration order = sidebar order.
            services.AddTab<TestRootViewModel>();
            services.AddTab<TestToastsRootViewModel>();
            services.AddTab<TestScalingRootViewModel>();
            services.AddTab<TestMonitorRootViewModel>();
            // Additional AddTab<...>() calls go here as tabs are added.

            // ---- Detail VMs ----
            // Transient — fresh instance per navigation.
            services.AddTransient<Test1ViewModel>();
            services.AddTransient<Test2ViewModel>();
            services.AddTransient<TestStressViewModel>();
            services.AddTransient<StaticSnippetViewModel>();
            services.AddTransient<PollingSnippetViewModel>();
            services.AddTransient<ChartSnippetViewModel>();

            // ---- Shell cluster ----
            services.AddSingleton<ShellViewModel>();
            services.AddSingleton<SidebarViewModel>();

            // ---- Application services (logging, config, settings, dialogs, etc.) ----
            // Add as they come online. Deferred for this iteration.
        }
    }
}