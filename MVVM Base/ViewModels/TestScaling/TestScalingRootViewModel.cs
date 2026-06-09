using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using MVVM_Base.Services.Navigation.Contracts;
using MVVM_Base.ViewModels.TestScaling;

namespace MVVM_Base.ViewModels.TestScaling
{
    /// <summary>
    /// Tab root for eyeballing layout scaling. Now also navigates to the stress-test page, which
    /// pushes onto this tab's history.
    ///
    /// <c>partial</c> for the source-generated command.
    /// </summary>
    public sealed partial class TestScalingRootViewModel : ViewModelBase, IRootViewModel
    {
        public string TabLabel => "Scale Test";

        private readonly INavigationFacade _nav;

        public TestScalingRootViewModel(INavigationFacade nav, ILogger<TestScalingRootViewModel> logger)
            : base(logger)
        {
            _nav = nav;
        }

        [RelayCommand]
        private Task GoToStressTestAsync() => _nav.NavigateAsync(new Open<TestStressViewModel>());
    }
}