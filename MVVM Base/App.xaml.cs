using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MVVM_Base.Services.Navigation;
using MVVM_Base.Services.Navigation.Contracts;
using MVVM_Base.Services.Navigation.Internal;
using MVVM_Base.ViewModels;
using MVVM_Base.ViewModels.Test;
using MVVM_Base.Views;
using System;
using System.Threading.Tasks;
using System.Windows;

namespace MVVM_Base
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

