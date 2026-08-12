using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Windows;
using Wpf.Shell.Services.Navigation.Contracts;
using Wpf.Shell.Services.Theming.Contracts;
using Wpf.Shell.ViewModels.Shell;

namespace Wpf.Shell
{
    /// <summary>
    /// A composed shell application: the handle returned by <see cref="ShellBuilder.Build"/> and
    /// the consumer's sole point of contact with the framework for the rest of the process
    /// lifetime.
    ///
    /// <para>
    /// Composition is already complete by the time this object exists — the container is built,
    /// the tab set is initialized, and every registration has been validated. What remains is
    /// running: starting hosted services, applying the theme, activating the initial tab, and
    /// putting a window on screen. That is <see cref="StartAsync"/>. Shutdown is
    /// <see cref="DisposeAsync"/>.
    /// </para>
    ///
    /// <para>
    /// A typical consumer touches exactly two lines of this type:
    /// </para>
    ///
    /// <code>
    /// protected override async void OnStartup(StartupEventArgs e)
    /// {
    ///     base.OnStartup(e);
    ///     _app = new ShellBuilder(hostBuilder) /* … */ .Build();
    ///     await _app.StartAsync();
    /// }
    ///
    /// protected override async void OnExit(ExitEventArgs e)
    /// {
    ///     if (_app is not null)
    ///         await _app.DisposeAsync();
    ///     base.OnExit(e);
    /// }
    /// </code>
    ///
    /// <para>
    /// <b>Per-process, not per-window.</b> Everything this type owns — the host, the container,
    /// configuration, logging — exists once regardless of how many windows are on screen. Window
    /// count is deliberately absent from this surface: there is no <c>MainWindow</c> property,
    /// because a property is a promise of one. Windows are produced, not exposed.
    /// </para>
    /// </summary>
    public sealed class ShellApplication : IAsyncDisposable
    {
        private readonly IHost _host;
        private readonly Type _initialTabType;
        private readonly Func<Window> _mainWindowFactory;

        private bool _started;
        private bool _disposed;

        /// <summary>
        /// Constructed only by <see cref="ShellBuilder.Build"/>. Consumers never new this up —
        /// an instance that did not come from a validated builder would carry none of the
        /// guarantees the type documents.
        /// </summary>
        internal ShellApplication(IHost host, Type initialTabType, Func<Window> mainWindowFactory)
        {
            _host = host;
            _initialTabType = initialTabType;
            _mainWindowFactory = mainWindowFactory;
        }

        /// <summary>
        /// The application's service provider.
        ///
        /// <para>
        /// Present as an escape hatch, not as the intended way to reach framework capability —
        /// consumers get navigation, theming, and toasts by injecting
        /// <see cref="INavigationFacade"/>, <see cref="ITheming"/>, and
        /// <c>IToastService</c> into their own types. Resolving services by hand from here is
        /// service location, and every use of it should feel like one.
        /// </para>
        /// </summary>
        public IServiceProvider Services => _host.Services;

        // ===== Startup =====

        /// <summary>
        /// Runs the application: starts hosted services, applies the initial theme, activates the
        /// initial tab, then constructs and shows the main window.
        ///
        /// <para>
        /// <b>Order is load-bearing.</b> The theme is applied before any window exists so the
        /// first rendered frame is already themed rather than showing token placeholders. The
        /// initial tab is activated before the window is shown so the shell never appears with an
        /// empty content area. The window is last, and showing it is the final act of startup.
        /// </para>
        ///
        /// <para>
        /// <b>No cancellation token.</b> Startup is not a cancellable operation in any meaningful
        /// sense — if the application is booting, booting is what was wanted, and abandoning it
        /// halfway leaves a built container, started hosted services, and no window.
        /// </para>
        ///
        /// <para>
        /// Must be called on the UI thread. Theme application and navigation both assert it.
        /// </para>
        /// </summary>
        /// <exception cref="InvalidOperationException">Already started.</exception>
        public async Task StartAsync()
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            if (_started)
                throw new InvalidOperationException(
                    "This ShellApplication has already been started.");
            _started = true;

            await _host.StartAsync();

            ApplyInitialTheme();
            await ActivateInitialTabAsync();
            ShowMainWindow();
        }

        /// <summary>
        /// Applies the startup theme. Hardcoded to <see cref="Theme.Light"/> — theme persistence
        /// is deferred and lands together with the settings subsystem, at which point the read
        /// order becomes persisted choice, then OS preference, then this default.
        /// </summary>
        private void ApplyInitialTheme()
        {
            var theming = _host.Services.GetRequiredService<ITheming>();
            theming.Apply(Theme.Light);
        }

        /// <summary>
        /// Activates the tab declared via <c>WithInitialTab&lt;TRoot&gt;()</c>.
        ///
        /// <para>
        /// The root is resolved from the container by <see cref="Type"/> rather than through
        /// <c>IViewModelFactory</c>: the factory's generic surface needs a compile-time type
        /// argument, and the initial tab is only known as a <see cref="Type"/> here. Roots are
        /// registered as singletons, so this returns the same instance the navigation service
        /// already holds in its tab history — resolution, not construction.
        /// </para>
        ///
        /// <para>
        /// The builder has already verified this type was registered, so the resolution cannot
        /// fail for a configuration reason.
        /// </para>
        /// </summary>
        private async Task ActivateInitialTabAsync()
        {
            var facade = _host.Services.GetRequiredService<INavigationFacade>();
            var initial = (IRootViewModel)_host.Services.GetRequiredService(_initialTabType);

            await facade.SwitchTabAsync(initial);
        }

        /// <summary>
        /// Constructs the main window, binds it to the shell, registers it as the application's
        /// main window, and shows it.
        ///
        /// <para>
        /// <see cref="Application.MainWindow"/> is assigned explicitly rather than left to WPF's
        /// automatic behavior. WPF assigns it to the first window shown, which happens to be
        /// correct while there is exactly one — but once secondary displays exist, "first shown"
        /// and "primary" stop being the same thing, and the implicit version becomes an ordering
        /// dependency nobody wrote down.
        /// </para>
        ///
        /// <para>
        /// The window's <c>DataContext</c> is set here, by the framework, which is what allows
        /// <see cref="ShellViewModel"/> to remain internal. The consumer's window is expected to
        /// host <c>ShellView</c> and declare no DataContext of its own.
        /// </para>
        /// </summary>
        private void ShowMainWindow()
        {
            var shell = _host.Services.GetRequiredService<ShellViewModel>();

            var window = _mainWindowFactory();
            window.DataContext = shell;

            Application.Current.MainWindow = window;
            window.Show();
        }

        // ===== Shutdown =====

        /// <summary>
        /// Note: There is something fishy here I am too lazy to fix now. Let's see if it bites my ass in the future.
        /// Stops hosted services and disposes the host, in that order.
        ///
        /// <para>
        /// The sequence is framework knowledge and is encoded here so the consumer cannot get it
        /// wrong. <c>StopAsync</c> stops hosted services in reverse registration order and honors
        /// <c>HostOptions.ShutdownTimeout</c>; disposing the host then disposes every
        /// <see cref="IDisposable"/> singleton in the container — domain services included, which
        /// matters most for the ones the consumer registered and never wrote teardown for.
        /// </para>
        /// </summary>
        public async ValueTask DisposeAsync()
        {
            if (_disposed)
                return;
            _disposed = true;

            await _host.StopAsync();

            if (_host is IAsyncDisposable asyncDisposable)
                await asyncDisposable.DisposeAsync();
            else
                _host.Dispose();
        }
    }
}