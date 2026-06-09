using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using MVVM_Base.Services.Navigation.Contracts;

namespace MVVM_Base.Services.Navigation.Internal
{
    /// <summary>
    /// Internal navigation primitive consumed by <see cref="INavigationFacade"/>.
    /// 
    /// Owns one navigation history per registered tab. Each history is a list with a current-index
    /// pointer; back and forward move the pointer, push truncates and appends, switch changes which
    /// tab's history is active. Roots are singletons that live for the application's lifetime;
    /// detail ViewModels are transient and may be discarded when the user branches past them.
    /// 
    /// Tabs are registered declaratively via
    /// <see cref="MVVM_Base.Services.Navigation.NavigationRegistration.AddTab{TRoot}"/>; the
    /// implementation resolves each registered root via <see cref="InitializeTabs"/>, which
    /// Bootstrap calls after the DI host is built. There is no other registration or
    /// initialization step on this interface — <see cref="SwitchTabAsync"/> is the single entry
    /// point for activating a tab, including the very first activation at startup.
    /// 
    /// This interface is not visible to ViewModels. ViewModels navigate through
    /// <see cref="INavigationFacade"/>, which translates intents into calls on this service and
    /// implements the recover-vs-branch decision.
    /// 
    /// All methods on this interface assume they are called on the WPF UI thread. In Debug builds,
    /// the implementation asserts dispatcher access; in Release builds, no check is performed.
    /// </summary>
    internal interface INavigationService : INotifyPropertyChanged
    {
        // ===== State (read-only, change-notified via INotifyPropertyChanged) =====

        /// <summary>
        /// The ViewModel at the current index of the active tab's history. Null only before the
        /// first <see cref="SwitchTabAsync"/> call.
        /// </summary>
        ObservableObject? CurrentViewModel { get; }

        /// <summary>
        /// The currently active tab's root ViewModel. Null only before the first
        /// <see cref="SwitchTabAsync"/> call.
        /// </summary>
        IRootViewModel? ActiveTab { get; }

        /// <summary>
        /// All registered tabs, in registration order. Empty until <see cref="InitializeTabs"/>
        /// runs; populated after, and never changes thereafter.
        /// </summary>
        IEnumerable<IRootViewModel> Tabs { get; }

        /// <summary>
        /// True if there is at least one frame behind the current index in the active tab's history.
        /// </summary>
        bool CanGoBack { get; }

        /// <summary>
        /// True if there is at least one frame ahead of the current index in the active tab's history.
        /// </summary>
        bool CanGoForward { get; }

        /// <summary>
        /// Raised when a frame's ViewModel is permanently discarded (after its full teardown sequence).
        /// Re-raised by the facade for external per-instance caches. Fired last in the discard sequence,
        /// so listeners observe a fully-dead instance.
        /// </summary>
        event EventHandler<ObservableObject>? ViewModelDiscarded;

        // ===== Setup =====

        /// <summary>
        /// Resolves every registered tab root and builds the per-tab history dictionary. Must be
        /// called once, by Bootstrap, after the DI host has been built and before any code touches
        /// <see cref="Tabs"/>, <see cref="SwitchTabAsync"/>, or any consumer of those.
        ///
        /// Deferred out of the constructor because roots typically take
        /// <see cref="INavigationFacade"/> as a dependency; resolving them during the service's
        /// own construction causes the container to re-enter the facade's construction graph,
        /// producing an undetected cycle with broken state. Calling this after BuildHostAsync
        /// returns sidesteps the cycle.
        ///
        /// Throws if called more than once.
        /// </summary>
        void InitializeTabs();

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
            where TViewModel : ObservableObject where TParameters : notnull, IEquatable<TParameters>;

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
        /// Switches the active tab to the one whose root is <paramref name="root"/>.
        /// 
        /// On first invocation (no active tab yet): activates <paramref name="root"/> as the
        /// initial tab. There is no leaving phase. Fires
        /// <see cref="INavigationAware{TParameters}.OnNavigatedToAsync"/> on the root with
        /// <see cref="NavigationContext.IsFirstNavigation"/> = true and
        /// <see cref="NavigationDirection.TabSwitch"/>.
        /// 
        /// On subsequent invocations: the leaving tab's current ViewModel receives
        /// <see cref="INavigationAware{TParameters}.OnNavigatedFromAsync"/> with
        /// <see cref="NavigationDirection.TabSwitch"/>; the entering tab's current ViewModel
        /// receives <see cref="INavigationAware{TParameters}.OnNavigatedToAsync"/> with the same
        /// direction.
        /// 
        /// No-op if <paramref name="root"/> is already the active tab. Throws if
        /// <paramref name="root"/> is not a registered tab root.
        /// </summary>
        Task SwitchTabAsync(IRootViewModel root);

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