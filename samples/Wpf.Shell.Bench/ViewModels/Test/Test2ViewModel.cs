using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Wpf.Shell.Services.Navigation.Contracts;
using Wpf.Shell.ViewModels;
using System;
using System.Collections.Generic;
using System.Text;

namespace Wpf.Shell.Bench.ViewModels.Test
{
    /// <summary>
    /// Demonstrates <see cref="INavigationGuard"/>. The guard returns the value
    /// of <see cref="AllowLeaving"/> — when unchecked, any attempt to navigate
    /// away (back, forward, tab switch) is silently aborted per error policy E1.
    /// </summary>
    public sealed partial class Test2ViewModel
        : ViewModelBase,
          INavigationGuard
    {
        private readonly INavigationFacade _nav;

        public Test2ViewModel(INavigationFacade nav, ILogger<Test2ViewModel> logger) : base(logger)
        {
            _nav = nav;
        }

        /// <summary>
        /// When true, leaving navigations are permitted. When false, the guard
        /// blocks them. Default true so the user has to actively toggle the
        /// "lock" before noticing the effect.
        /// </summary>
        [ObservableProperty]
        private bool _allowLeaving = true;

        public Task<bool> CanNavigateAwayAsync()
        {
            return Task.FromResult(AllowLeaving);
        }

        [RelayCommand]
        private Task GoBackAsync() => _nav.GoBackAsync();
    }
}
