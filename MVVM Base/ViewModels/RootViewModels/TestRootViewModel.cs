using CommunityToolkit.Mvvm.ComponentModel;
using MVVM_Base.Services.Navigation.Contracts;

namespace MVVM_Base.ViewModels.RootViewModels
{
    /// <summary>
    /// Minimal root ViewModel. Exists to verify the navigation framework wires up end-to-end.
    /// </summary>
    public sealed partial class TestRootViewModel : ObservableObject, IRootViewModel
    {
        [ObservableProperty]
        private string _message = "One VM to rule them all (some tbh).";
    }
}
