using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Windows;
using Wpf.Shell.Services.Navigation;
using Wpf.Shell.Services.Navigation.Contracts;
using Wpf.Shell.Services.Navigation.Internal;
using Wpf.Shell.Services.Theming;
using Wpf.Shell.Services.Toasts;
using Wpf.Shell.ViewModels;
using Wpf.Shell.ViewModels.Shell;

namespace Wpf.Shell
{
    /// <summary>
    /// The framework's composition entry point. Accepts a
    /// <see cref="HostApplicationBuilder"/> the consumer already owns, layers the shell's
    /// own registrations onto it, validates the result, and produces a
    /// <see cref="ShellApplication"/>.
    ///
    /// <para>
    /// The shell owns window construction and placement, navigation,
    /// theming, toasts, and the <see cref="IHost"/> lifecycle. The consuming application owns
    /// every ViewModel and View with domain meaning, all domain service registrations, which
    /// tabs exist, and which tab is initial. The shell never names a domain type — it receives
    /// type parameters and resolves through DI.
    /// </para>
    ///
    /// <code>
    /// var hostBuilder = Host.CreateApplicationBuilder(e.Args);
    /// hostBuilder.Services.AddMyDomainServices();
    ///
    /// var app = new ShellBuilder(hostBuilder)
    ///     .AddTab&lt;HomeRootViewModel&gt;()
    ///     .AddTab&lt;SettingsRootViewModel&gt;()
    ///     .WithInitialTab&lt;HomeRootViewModel&gt;()
    ///     .WithMainWindow&lt;MainWindow&gt;()
    ///     .Build();
    ///
    /// await app.StartAsync();
    /// </code>
    /// </summary>
    public sealed class ShellBuilder
    {
        private readonly HostApplicationBuilder _hostBuilder;

        /// <summary>
        /// The tab list, owned outright by this builder rather than accumulated in the DI
        /// container. Order of <see cref="AddTab{TRoot}"/> calls is the order of tabs in the
        /// sidebar, and is preserved by this list directly — not by any container behavior.
        /// </summary>
        private readonly List<TabRegistration> _tabs = [];

        private Type? _initialTabType;
        private Func<Window>? _mainWindowFactory;
        private bool _built;

        /// <summary>
        /// Creates a builder over an existing <see cref="HostApplicationBuilder"/>.
        /// </summary>
        /// 
        /// <param name="hostBuilder">
        /// A host builder, typically from <c>Host.CreateApplicationBuilder(args)</c>. The consumer
        /// should complete their own service registrations on it before calling <see cref="Build"/>
        /// — tab roots are resolved during <see cref="Build"/> and pull domain services out of the
        /// container as they construct.
        /// </param>
        public ShellBuilder(HostApplicationBuilder hostBuilder)
        {
            ArgumentNullException.ThrowIfNull(hostBuilder);
            _hostBuilder = hostBuilder;
        }

        // ===== Fluent configuration =====

        /// <summary>
        /// Registers <typeparamref name="TRoot"/> as a tab. Call order determines sidebar order.
        ///
        /// <para>
        /// The constraint chain enforces correctness at compile time: <typeparamref name="TRoot"/>
        /// must derive from <see cref="ViewModelBase"/> and
        /// implement <see cref="IRootViewModel"/>. Detail ViewModels cannot be passed here, and reaching for this method
        /// is itself the signal that a VM intends to be a tab.
        /// </para>
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// <typeparamref name="TRoot"/> has already been registered. Thrown at the offending
        /// registration line rather than deferred to <see cref="Build"/>, so the duplicate is
        /// reported where it was written.
        /// </exception>
        public ShellBuilder AddTab<TRoot>()
            where TRoot : ViewModelBase, IRootViewModel
        {
            ThrowIfBuilt();

            var type = typeof(TRoot);

            if (_tabs.Any(t => t.RootViewModelType == type))
                throw new InvalidOperationException(
                    $"Tab '{type.Name}' is already registered. Each tab root may be added once.");

            _tabs.Add(new TabRegistration(type));
            return this;
        }

        /// <summary>
        /// Declares which registered tab is active at startup.
        ///
        /// <para>
        /// This method names a tab; it does not register one. <see cref="AddTab{TRoot}"/> remains
        /// the sole registration verb, which keeps sidebar ordering entirely determined by
        /// <c>AddTab</c> call order and prevents this method's position in the chain from
        /// silently affecting layout.
        /// </para>
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// An initial tab has already been declared.
        /// </exception>
        public ShellBuilder WithInitialTab<TRoot>()
            where TRoot : ViewModelBase, IRootViewModel
        {
            ThrowIfBuilt();

            if (_initialTabType is not null)
                throw new InvalidOperationException(
                    $"An initial tab is already declared ('{_initialTabType.Name}'). " +
                    "WithInitialTab may be called once.");

            _initialTabType = typeof(TRoot);
            return this;
        }

        /// <summary>
        /// Declares the window type the framework constructs, places, and shows as the shell's
        /// main window.
        ///
        /// <para>
        /// A <b>type</b> rather than an instance: the framework constructs windows so it can also
        /// place them, which is what makes secondary-display support a framework concern rather
        /// than a consumer one. A window the consumer constructed would have to be handed over
        /// already-built, including for displays that may not be present at runtime.
        /// </para>
        ///
        /// <para>
        /// The window is expected to host <c>ShellView</c> and nothing else. Its
        /// <c>DataContext</c> is assigned by the framework; do not set one in XAML.
        /// </para>
        ///
        /// <para>
        /// <b>Optional today, required later.</b> Omitting this call falls back to a
        /// framework-supplied default window and logs a warning. That fallback is a runway, not a
        /// supported path — a future version will throw.
        /// </para>
        /// </summary>
        /// <exception cref="InvalidOperationException">A main window has already been declared.</exception>
        public ShellBuilder WithMainWindow<TWindow>()
            where TWindow : Window, new()
        {
            ThrowIfBuilt();

            if (_mainWindowFactory is not null)
                throw new InvalidOperationException(
                    "A main window is already declared. WithMainWindow may be called once.");

            _mainWindowFactory = static () => new TWindow();
            return this;
        }

        // ===== Build =====

        /// <summary>
        /// Composes the application: validates the configuration, registers the framework's
        /// services, builds the <see cref="IHost"/>, and initializes the tab set.
        ///
        /// <para>
        /// Synchronous by design. Everything here either composes an object graph or fails — no
        /// hosted services run, no navigation occurs, no window appears. Those belong to
        /// <see cref="ShellApplication.StartAsync"/>. Keeping this phase synchronous also means
        /// every validation failure arrives as a plain throw at the composition line rather than
        /// wrapped in task machinery.
        /// </para>
        ///
        /// <para>
        /// <b>Ordering note.</b> Tab roots are resolved here, before hosted services start.
        /// Anything a tab root needs must therefore be available at construction time, not at
        /// start time..
        /// </para>
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Called twice, no tabs registered, no initial tab declared, or the initial tab names a
        /// type that was never registered.
        /// </exception>
        public ShellApplication Build()
        {
            ThrowIfBuilt();
            _built = true;

            Validate();

            RegisterFrameworkServices();

            var host = _hostBuilder.Build();

            // Direct IServiceProvider access is forbidden throughout the framework — except here.
            // This class is the composition root, and the composition root is where the container
            // is legitimately addressed by hand.
            var navigation = host.Services.GetRequiredService<INavigationService>();
            navigation.InitializeTabs(_tabs);

            return new ShellApplication(host, _initialTabType!, _mainWindowFactory!);
        }

        // ===== Internals =====

        /// <summary>
        /// Every check that can be made from the builder's own state, run before anything is
        /// registered or constructed. Each condition here currently manifests at runtime as a
        /// blank window or a null reference.
        /// </summary>
        private void Validate()
        {
            if (_tabs.Count == 0)
                throw new InvalidOperationException(
                    "No tabs registered. Call AddTab<TRoot>() at least once before Build().");

            if (_initialTabType is null)
                throw new InvalidOperationException(
                    "No initial tab declared. Call WithInitialTab<TRoot>() before Build().");

            if (!_tabs.Any(t => t.RootViewModelType == _initialTabType))
                throw new InvalidOperationException(
                    $"Initial tab '{_initialTabType.Name}' was never registered. " +
                    $"Call AddTab<{_initialTabType.Name}>() as well as " +
                    $"WithInitialTab<{_initialTabType.Name}>().");

            if (_mainWindowFactory is null)
                throw new InvalidOperationException(
                    "No main window declared. Call WithMainWindow<TWindow>() before Build().");
        }

        /// <summary>
        /// Layers the framework's own registrations onto the consumer's service collection.
        ///
        /// <para>
        /// These subsystem registration methods are <c>internal</c> and this is their only caller.
        /// Consumers do not call <c>AddNavigation</c> / <c>AddTheming</c> / <c>AddToasts</c>
        /// themselves — the builder is the public initialization surface, and framework chrome
        /// registration is not a consumer concern.
        /// </para>
        ///
        /// <para>
        /// <see cref="HostOptions.ShutdownTimeout"/> is deliberately left alone. Framework
        /// registration lands after the consumer's, so anything configured here would override
        /// rather than default. The host's own value (30s) applies and remains overridable by the
        /// consumer through standard configuration.
        /// </para>
        /// </summary>
        private void RegisterFrameworkServices()
        {
            var services = _hostBuilder.Services;

            services.AddNavigation();
            services.AddTheming();
            services.AddToasts();

            // Shell cluster. A UI cluster, not a subsystem — no service interface, no contracts,
            // therefore no AddShell() extension. Registered directly by the composition root.
            services.AddSingleton<ShellViewModel>();
            services.AddSingleton<SidebarViewModel>();

            // Tab roots. Registered as singletons so IViewModelFactory returns the canonical
            // instance for each. The TabRegistration list itself is NOT registered — it is passed
            // to InitializeTabs directly, so the container never becomes a second source of truth
            // for the tab set.
            foreach (var tab in _tabs)
                services.AddSingleton(tab.RootViewModelType);
        }

        private void ThrowIfBuilt()
        {
            if (_built)
                throw new InvalidOperationException(
                    "This ShellBuilder has already been built. Create a new builder rather than " +
                    "reconfiguring or rebuilding this one.");
        }
    }
}