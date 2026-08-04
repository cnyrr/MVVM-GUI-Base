using Microsoft.Extensions.Hosting;
using System.Windows;

namespace Wpf.Shell.Bench
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


            MainWindow = Bootstrap.CreateMainWindow(_host.Services);
            MainWindow.Show();
        }

        protected override async void OnExit(ExitEventArgs e)
        {
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

