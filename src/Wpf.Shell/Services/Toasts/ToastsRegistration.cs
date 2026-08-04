using Microsoft.Extensions.DependencyInjection;
using Wpf.Shell.Services.Toasts.Contracts;
using Wpf.Shell.Services.Toasts.Internal;
using System;
using System.Collections.Generic;
using System.Text;

namespace Wpf.Shell.Services.Toasts
{
    /// <summary>
    /// DI registration for the toast subsystem.
    /// </summary>
    public static class ToastsRegistration
    {
        /// <summary>
        /// Registers <see cref="IToastService"/> and the underlying
        /// <see cref="ToastHostViewModel"/> as singletons. The host VM is exposed
        /// to the shell via <see cref="ViewModels.ShellViewModel"/> as an
        /// <see cref="System.Windows.DependencyObject"/>-shaped binding target.
        /// </summary>
        public static IServiceCollection AddToasts(this IServiceCollection services)
        {
            services.AddSingleton<ToastHostViewModel>();
            services.AddSingleton<IToastService, ToastService>();
            return services;
        }
    }
}
