using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using MVVM_Base.Services.Navigation.Contracts;

namespace MVVM_Base.Services.Navigation.Internal
{
    /// <summary>
    /// Internal navigation primitive consumed by <see cref="INavigation"/>.
    /// 
    /// Owns one navigation history per registered tab. Each history is a list with a current-index
    /// pointer; back and forward move the pointer, push truncates and appends, switch changes which
    /// tab's history is active. Roots are singletons that live for the application's lifetime;
    /// detail ViewModels are transient and may be discarded when the user branches past them.
    /// 
    /// This interface is not visible to ViewModels. ViewModels navigate through
    /// <see cref="INavigation"/>, which translates intents into calls on this service and
    /// implements the recover-vs-branch decision.
    /// 
    /// All methods on this interface assume they are called on the WPF UI thread. In Debug builds,
    /// the implementation asserts dispatcher access; in Release builds, no check is performed.
    /// </summary>
    internal interface INavigationService : INotifyPropertyChanged
    {
        // ===== State (read-only, change-notified via INotifyPropertyChanged) =====

        /// <summary>
        /// The ViewModel at the current index of the active tab's history. Null only before
        /// <see cref="SetInitialTabAsync"/> has been called.
        /// </summary>
        ObservableObject? CurrentViewModel { get; }

        /// <summary>
        /// The currently active tab. Throws if accessed before bootstrap.
        /// </summary>
        TabKey ActiveTab { get; }

        /// <summary>
        /// True if there is at least one frame behind the current index in the active tab's history.
        /// </summary>
        bool CanGoBack { get; }

        /// <summary>
        /// True if there is at least one frame ahead of the current index in the active tab's history.
        /// </summary>
        bool CanGoForward { get; }

        // ===== Setup (called once at startup) =====

        /// <summary>
        /// Registers a ViewModel type as the root of the given tab. The root is constructed lazily
        /// on first activation. Each tab must be registered before <see cref="SetInitialTabAsync"/>
        /// is called.
        /// 
        /// Throws if the tab is already registered.
        /// </summary>
        void RegisterTab<TRootVM>(TabKey key)
            where TRootVM : ObservableObject, IRootViewModel;

        /// <summary>
        /// Bootstraps the navigation service by activating the given tab and constructing its root.
        /// Fires the root's <see cref="INavigationAware{TParameters}.OnNavigatedToAsync"/> if it
        /// implements the interface, with <see cref="NavigationContext.IsFirstNavigation"/> = true.
        /// 
        /// Must be called exactly once after all tabs are registered. Throws if called more than
        /// once or if the tab is not registered.
        /// </summary>
        Task SetInitialTabAsync(TabKey key);

        // ===== Operations =====

        /// <summary>
        /// Pushes a new ViewModel onto the active tab's history.
        /// 
        /// If the current index is not at the tip, the future frames are truncated and discarded.
        /// Each discarded frame's ViewModel receives
        /// <see cref="INavigationAware{TParameters}.OnNavigatedFromAsync"/> with
        /// <see cref="NavigationDirection.Discarded"/> if it implements the interface, then
        /// <see cref="System.IDisposable.Dispose"/> is called if implemented.
        /// 
        /// The leaving ViewModel's <see cref="INavigationGuard.CanNavigateAwayAsync"/> is queried
        /// first if implemented; a false result silently aborts the navigation.
        /// </summary>
        Task NavigateToAsync<TViewModel, TParameters>(TParameters parameters)
            where TViewModel : ObservableObject where TParameters : notnull;

        /// <summary>
        /// Moves the active tab's current index back by one. The previous frame's ViewModel becomes
        /// current and receives <see cref="INavigationAware{TParameters}.OnNavigatedToAsync"/> if
        /// it implements the interface, with <see cref="NavigationDirection.Backward"/>.
        /// 
        /// Throws if <see cref="CanGoBack"/> is false.
        /// </summary>
        Task GoBackAsync();

        /// <summary>
        /// Moves the active tab's current index forward by one. The recovered frame's ViewModel
        /// becomes current and receives <see cref="INavigationAware{TParameters}.OnNavigatedToAsync"/>
        /// if it implements the interface, with <see cref="NavigationDirection.Forward"/>.
        /// 
        /// Throws if <see cref="CanGoForward"/> is false.
        /// </summary>
        Task GoForwardAsync();

        /// <summary>
        /// Switches the active tab. The leaving tab's current ViewModel receives
        /// <see cref="INavigationAware{TParameters}.OnNavigatedFromAsync"/> with
        /// <see cref="NavigationDirection.TabSwitch"/> if it implements the interface; the entering
        /// tab's current ViewModel receives <see cref="INavigationAware{TParameters}.OnNavigatedToAsync"/>
        /// with the same direction.
        /// 
        /// No-op if the requested tab is already active. Throws if the tab is not registered.
        /// </summary>
        Task SwitchTabAsync(TabKey key);

        /// <summary>
        /// Returns a snapshot of the next forward frame in the active tab's history, or null if
        /// no forward frame exists.
        /// 
        /// Used by the facade to decide between recovery (matching VM type and parameters) and
        /// branching (push, discarding the future). Does not modify state.
        /// </summary>
        ForwardPeek? PeekForward();
    }
}
