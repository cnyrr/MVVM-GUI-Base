using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Wpf.Shell.Services.Navigation;
using Wpf.Shell.Services.Navigation.Contracts;
using Wpf.Shell.Services.Navigation.Internal;
using Wpf.Shell.ViewModels;
using Wpf.Shell.ViewModels.Test;
using Wpf.Shell.Views;
using System;
using System.Threading.Tasks;
using System.Windows;

namespace Wpf.Shell
{
    /// <summary>
    /// Application composition root. Builds the host, registers services, registers
    /// tabs, sets the initial tab, and shows <see cref="MainWindow"/>.
    /// </summary>
    public partial class App : Application
    {
        private IHost? _host;

        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            _host = await Bootstrap.BuildHostAsync(e.Args);

            Bootstrap.ApplyInitialTheme(_host.Services);

            await Bootstrap.ConfigureNavigationAsync(_host.Services);

            Bootstrap.PublishSharedResources(_host.Services);

            Bootstrap.ConfigureMonitors(_host.Services);

            MainWindow = Bootstrap.CreateMainWindow(_host.Services);
            MainWindow.Show();
        }

        protected override async void OnExit(ExitEventArgs e)
        {
            Bootstrap.DisposeMonitors();

            if (_host is not null)
            {
                await _host.StopAsync(TimeSpan.FromSeconds(5));
                _host.Dispose();
                _host = null;
            }

            base.OnExit(e);
        }
    }
}

