using System.ComponentModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MVVM_Base.Services.Navigation.Contracts;
using MVVM_Base.Services.Toasts.Internal;

namespace MVVM_Base.ViewModels
{
    /// <summary>
    /// Root ViewModel for the application shell. Projects navigation state from
    /// <see cref="INavigation"/> and owns shell-only UI state (sidebar expansion).
    /// Holds no navigation history of its own.
    /// </summary>
    public sealed partial class ShellViewModel : ObservableObject
    {
        private readonly INavigationFacade _nav;
        private readonly ToastHostViewModel _toastHost;

        public ShellViewModel(INavigationFacade nav, ToastHostViewModel toastHost)
        {
            _nav = nav;
            _nav.PropertyChanged += OnNavigationPropertyChanged;
            _toastHost = toastHost;
        }

        // ===== Projected from INavigationFacade (pure pass-through) =====

        /// <summary>
        /// The ViewModel currently displayed in the main content area — the top
        /// of the active tab's stack. Bound by the shell's <c>ContentControl</c>.
        /// </summary>
        public ObservableObject? CurrentViewModel => _nav.CurrentViewModel;

        /// <summary>
        /// The currently active tab. Bound by the sidebar to highlight the
        /// selected tab item.
        /// </summary>
        public TabKey ActiveTab => _nav.ActiveTab;

        // ===== Shell-only state =====

        /// <summary>
        /// Whether the sidebar is expanded. Defaults to collapsed; only the
        /// hamburger button is visible until the user opens it.
        /// </summary>
        [ObservableProperty]
        private bool _isSidebarExpanded;

        /// <summary>
        /// The toast host VM. The shell composes the toast host into its visual tree
        /// — bound by <c>ShellView.xaml</c>'s toast layer to render queued toasts.
        /// </summary>
        public ToastHostViewModel ToastHost => _toastHost;

        // ===== Commands =====

        /// <summary>
        /// Toggles <see cref="IsSidebarExpanded"/>. Bound by both the hamburger
        /// button and the scrim's click-outside-to-close behavior.
        /// </summary>
        [RelayCommand]
        private void ToggleSidebar() => IsSidebarExpanded = !IsSidebarExpanded;

        /// <summary>
        /// Switches the active tab and collapses the sidebar. Selecting a tab is
        /// always a "go there and let me see the content" action.
        /// </summary>
        [RelayCommand]
        private async Task SwitchTabAsync(TabKey key)
        {
            await _nav.SwitchTabAsync(key);
            IsSidebarExpanded = false;
        }

        // ===== Internals =====

        private void OnNavigationPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(INavigationFacade.CurrentViewModel):
                    OnPropertyChanged(nameof(CurrentViewModel));
                    break;
                case nameof(INavigationFacade.ActiveTab):
                    OnPropertyChanged(nameof(ActiveTab));
                    break;
            }
        }
    }
}
