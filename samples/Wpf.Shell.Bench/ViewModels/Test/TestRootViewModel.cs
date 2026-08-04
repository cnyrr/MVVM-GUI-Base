using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Wpf.Shell.Services.Navigation.Contracts;
using Wpf.Shell.ViewModels;

namespace Wpf.Shell.Bench.ViewModels.Test
{
    /// <summary>
    /// Minimal root ViewModel. Exists to verify the navigation framework wires up end-to-end.
    /// </summary>
    public sealed partial class TestRootViewModel : ViewModelBase, IRootViewModel
    {
        public string TabLabel => "Navigation Tests";

        [ObservableProperty]
        private string _message = "Tab root test.";

        private readonly INavigationFacade _nav;

        public TestRootViewModel(INavigationFacade nav, ILogger<TestRootViewModel> logger) : base(logger)
        {
            _nav = nav;
        }

        [RelayCommand]
        private Task GoForwardToTest1Async() =>
        _nav.NavigateAsync(new Open<Test1ViewModel>());
    }
}
