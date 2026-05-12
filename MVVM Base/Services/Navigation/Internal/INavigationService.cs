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
    /// implementation eagerly resolves each registered root in its constructor and builds the
    /// immutable <see cref="Tabs"/> list. There is no separate registration or initialization
    /// step on this interface — <see cref="SwitchTabAsync"/> is the single entry point for
    /// activating a tab, including the very first activation at startup.
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
        /// All registered tabs, in registration order. Set once at construction; never changes.
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