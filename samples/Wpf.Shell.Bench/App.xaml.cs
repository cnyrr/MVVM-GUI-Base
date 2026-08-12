using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Windows;
using Wpf.Shell.Bench.ViewModels.Test;
using Wpf.Shell.Bench.ViewModels.TestScaling;
using Wpf.Shell.Bench.ViewModels.TestToasts;

namespace Wpf.Shell.Bench
{
    /// <summary>
    /// Application entry point and composition root.
    ///
    /// <para>
    /// The startup phases that used to live in <c>Bootstrap.cs</c> — host build, theme
    /// application, tab initialization, initial navigation, main-window construction — are now
    /// the framework's. What remains here is what the application actually owns: its own service
    /// registrations, which tabs exist, which is initial, and which window hosts the shell.
    /// </para>
    /// </summary>
    public partial class App : Application
    {
        private ShellApplication? _app;

        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var hostBuilder = Host.CreateApplicationBuilder(e.Args);

            // ---- Application services ----
            // Configuration, logging, and domain services are configured here, on the host
            // builder, before it is handed over. Everything registered by the time Build() runs
            // is visible to tab roots as they construct.

            // ---- Detail VMs ----
            // Transient — a fresh instance per navigation. Tab roots are registered by the
            // builder's AddTab and must not be registered here.
            hostBuilder.Services.AddTransient<Test1ViewModel>();
            hostBuilder.Services.AddTransient<Test2ViewModel>();
            hostBuilder.Services.AddTransient<TestStressViewModel>();

            // ---- Shell composition ----
            // AddTab order is sidebar order. Build() validates and throws on any incomplete
            // configuration; nothing starts until it returns.
            _app = new ShellBuilder(hostBuilder)
                .AddTab<TestRootViewModel>()
                .AddTab<TestToastsRootViewModel>()
                .AddTab<TestScalingRootViewModel>()
                .WithInitialTab<TestRootViewModel>()
                .WithMainWindow<MainWindow>()
                .Build();

            await _app.StartAsync();
        }

        protected override async void OnExit(ExitEventArgs e)
        {
            if (_app is not null)
            {
                await _app.DisposeAsync();
                _app = null;
            }

            base.OnExit(e);
        }
    }
}
