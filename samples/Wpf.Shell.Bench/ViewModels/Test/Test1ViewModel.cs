using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Wpf.Shell.Services.Navigation.Contracts;
using System;
using System.Collections.Generic;
using System.Text;

namespace Wpf.Shell.ViewModels.Test
{
    /// <summary>
    /// Demonstrates <see cref="INavigationAware{TParameters}"/>. Counts every
    /// arrival via <see cref="OnNavigatedToAsync"/> — including back/forward
    /// returns. Transient lifetime: the counter resets if this VM's frame is
    /// ever truncated.
    /// </summary>
    public sealed partial class Test1ViewModel
        : ViewModelBase,
          INavigationAware<NoParameters>
    {
        private readonly INavigationFacade _nav;

        public Test1ViewModel(INavigationFacade nav, ILogger<Test1ViewModel> logger) : base(logger)
        {
            _nav = nav;
        }

        /// <summary>Number of times this VM has been navigated to.</summary>
        [ObservableProperty]
        private int _visitCount;

        public Task OnNavigatedToAsync(
            NoParameters parameters,
            NavigationContext context,
            CancellationToken cancellationToken)
        {
            VisitCount++;
            return Task.CompletedTask;
        }

        [RelayCommand]
        private Task GoBackAsync() => _nav.GoBackAsync();

        [RelayCommand]
        private Task GoForwardToTest2Async() =>
            _nav.NavigateAsync(new Open<Test2ViewModel>());
    }
}
