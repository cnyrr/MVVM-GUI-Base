using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using MVVM_Base.Services.Navigation.Contracts;

namespace MVVM_Base.ViewModels.Shell
{
    /// <summary>
    /// Root ViewModel for the application shell.
    ///
    /// Composes companion VMs (<see cref="Sidebar"/>, <see cref="ToastHost"/>) into one binding
    /// surface for <c>ShellView</c>, projects <see cref="CurrentViewModel"/> from the navigation
    /// facade, and owns shell-only UI state (<see cref="IsSidebarExpanded"/>).
    ///
    /// Sidebar collapse is triggered by <see cref="INavigationFacade.ActiveTab"/> change rather
    /// than paired with a click: any tab switch — whether initiated by the sidebar or
    /// programmatically — leaves the user looking at the new content unobstructed.
    /// </summary>
    public sealed partial class ShellViewModel : ViewModelBase
    {
        private readonly INavigationFacade _nav;
        private readonly SidebarViewModel _sidebar;

        public ShellViewModel(
            INavigationFacade nav,
            SidebarViewModel sidebar,
            ILogger<ShellViewModel> logger)
            : base(logger)
        {
            _nav = nav;
            _sidebar = sidebar;

            _nav.PropertyChanged += OnNavigationPropertyChanged;
        }

        // ===== Projected from INavigationFacade =====

        /// <summary>
        /// The ViewModel currently displayed in the main content area. Bound by the shell's
        /// <c>ContentControl</c>.
        /// </summary>
        public ObservableObject? CurrentViewModel => _nav.CurrentViewModel;

        // ===== Composed companion VMs =====

        /// <summary>
        /// Sidebar VM. Owns the tab list, active-tab tracking, and the switch command. Bound by
        /// the shell view's sidebar layer.
        /// </summary>
        public SidebarViewModel Sidebar => _sidebar;

        // ===== Shell-only state =====

        /// <summary>
        /// Whether the sidebar is expanded. Defaults to collapsed; only the hamburger button is
        /// visible until the user opens it. Collapsed automatically when the active tab changes.
        /// </summary>
        [ObservableProperty]
        private bool _isSidebarExpanded;

        // ===== Commands =====

        /// <summary>
        /// Toggles <see cref="IsSidebarExpanded"/>. Bound by both the hamburger button and the
        /// scrim's click-outside-to-close behavior.
        /// </summary>
        [RelayCommand]
        private void ToggleSidebar() => IsSidebarExpanded = !IsSidebarExpanded;

        // ===== Internals =====

        private void OnNavigationPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(INavigationFacade.CurrentViewModel):
                    OnPropertyChanged(nameof(CurrentViewModel));
                    break;
                case nameof(INavigationFacade.ActiveTab):
                    // Collapse the sidebar on any tab switch — sidebar-initiated or programmatic.
                    // The collapse fires between phase 5 (view notification) and phase 6 (arrival
                    // lifecycle) of the navigation, so the user sees content swap and sidebar
                    // collapse on the same frame.
                    IsSidebarExpanded = false;
                    break;
            }
        }
    }
}