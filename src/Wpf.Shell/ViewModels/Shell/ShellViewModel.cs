using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Wpf.Shell.Services.Navigation.Contracts;
using Wpf.Shell.Services.Toasts.Internal;

namespace Wpf.Shell.ViewModels.Shell
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
        private readonly ToastHostViewModel _toastHost;

        public ShellViewModel(
            INavigationFacade nav,
            SidebarViewModel sidebar,
            ToastHostViewModel toastHost,
            ILogger<ShellViewModel> logger)
            : base(logger)
        {
            _nav = nav;
            _sidebar = sidebar;
            _toastHost = toastHost;

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

        /// <summary>   
        /// Toast host VM. Owns the toast queue and the toast display logic. Bound by the shell view's
        /// toast layer.
        /// </summary>
        public ToastHostViewModel ToastHost => _toastHost;

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