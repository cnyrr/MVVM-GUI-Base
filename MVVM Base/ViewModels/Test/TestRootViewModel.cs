using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MVVM_Base.Services.Navigation.Contracts;

namespace MVVM_Base.ViewModels.Test
{
    /// <summary>
    /// Minimal root ViewModel. Exists to verify the navigation framework wires up end-to-end.
    /// </summary>
    public sealed partial class TestRootViewModel : ObservableObject, IRootViewModel
    {
        [ObservableProperty]
        private string _message = "Font selection test.";

        private readonly INavigationFacade _nav;

        public TestRootViewModel(INavigationFacade nav)
        {
            _nav = nav;
        }

        [RelayCommand]
        private Task GoForwardToTest1Async() =>
        _nav.NavigateAsync(new Open<Test1ViewModel>());
    }
}
