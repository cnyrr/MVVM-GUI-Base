using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MVVM_Base.Services.Navigation.Contracts;
using System;
using System.Collections.Generic;
using System.Text;

namespace MVVM_Base.ViewModels.Test
{
    /// <summary>
    /// Demonstrates <see cref="INavigationAware{TParameters}"/>. Counts every
    /// arrival via <see cref="OnNavigatedToAsync"/> — including back/forward
    /// returns. Transient lifetime: the counter resets if this VM's frame is
    /// ever truncated.
    /// </summary>
    public sealed partial class Test1ViewModel
        : ObservableObject,
          INavigationAware<NoParameters>
    {
        private readonly INavigationFacade _nav;

        public Test1ViewModel(INavigationFacade nav)
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
