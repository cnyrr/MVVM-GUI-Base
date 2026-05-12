using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace MVVM_Base.Services.Navigation.Contracts
{
    /// <summary>
    /// The public navigation API consumed by ViewModels.
    /// 
    /// Translates typed navigation intents into operations on the underlying navigation primitive,
    /// applying the recover-vs-branch policy uniformly: if the next forward frame matches the
    /// requested intent (same ViewModel type and equal parameters by record value), the framework
    /// goes forward and recovers the existing frame; otherwise it pushes a new frame and discards
    /// any preserved future.
    /// 
    /// Provides direct back/forward/tab-switch operations for explicit user navigation, plus a
    /// batching mechanism for compound transitions that should appear as a single visual change.
    /// 
    /// Tabs are registered declaratively via
    /// <see cref="MVVM_Base.Services.Navigation.NavigationRegistration.AddTab{TRoot}"/>; root
    /// ViewModels are constructed eagerly at app startup and exposed via <see cref="Tabs"/>.
    /// The first <see cref="SwitchTabAsync"/> call activates the initial tab — there is no
    /// separate initialization step on this surface.
    /// 
    /// All methods assume they are called on the WPF UI thread. Background callers must marshal
    /// to the dispatcher before invoking.
    /// </summary>
    public interface INavigationFacade : INotifyPropertyChanged
    {
        // ===== State (read-only, change-notified via INotifyPropertyChanged) =====

        /// <summary>
        /// The ViewModel currently displayed. Null only at startup before the first
        /// <see cref="SwitchTabAsync"/> call.
        /// </summary>
        ObservableObject? CurrentViewModel { get; }

        /// <summary>
        /// The currently active tab's root ViewModel. Null only at startup before the first
        /// <see cref="SwitchTabAsync"/> call.
        /// </summary>
        IRootViewModel? ActiveTab { get; }

        /// <summary>
        /// All registered tabs, in registration order. Set once at construction; never changes.
        /// Bound by the sidebar's <c>ItemsControl</c>.
        /// </summary>
        IEnumerable<IRootViewModel> Tabs { get; }

        /// <summary>
        /// True if the active tab's history has at least one frame behind the current position.
        /// View authors typically bind a back button's IsEnabled to this property.
        /// </summary>
        bool CanGoBack { get; }

        /// <summary>
        /// True if the active tab's history has at least one preserved future frame.
        /// View authors typically bind a forward button's IsEnabled to this property.
        /// </summary>
        bool CanGoForward { get; }

        // ===== Operations =====

        /// <summary>
        /// Dispatches a typed navigation intent.
        /// 
        /// The framework inspects the intent's type arguments and the active tab's history to
        /// decide between recovery and branching:
        /// <list type="bullet">
        ///   <item>If the next forward frame's ViewModel type matches the intent's target and its
        ///   parameters are equal (by record value) to those produced by the intent, the framework
        ///   recovers the existing frame via forward navigation.</item>
        ///   <item>Otherwise, the framework pushes a fresh frame, discarding any preserved future.</item>
        /// </list>
        /// </summary>
        Task NavigateAsync<TViewModel, TParameters>(
            INavigationIntent<TViewModel, TParameters> intent)
            where TViewModel : ObservableObject where TParameters : notnull, IEquatable<TParameters>;

        /// <summary>
        /// Moves the active tab's history pointer back by one. Throws if <see cref="CanGoBack"/>
        /// is false.
        /// </summary>
        Task GoBackAsync();

        /// <summary>
        /// Moves the active tab's history pointer forward by one, recovering the preserved frame
        /// at that position. Throws if <see cref="CanGoForward"/> is false.
        /// </summary>
        Task GoForwardAsync();

        /// <summary>
        /// Switches the active tab to the one whose root is <paramref name="root"/>.
        /// 
        /// The leaving tab's history is preserved; switching back later restores its current
        /// position. No-op if <paramref name="root"/> is already the active tab. Throws if
        /// <paramref name="root"/> is not a registered tab root.
        /// 
        /// At app startup, the first call to this method also serves as the initial tab
        /// activation — there is no leaving phase to skip because no tab is active yet.
        /// </summary>
        Task SwitchTabAsync(IRootViewModel root);

        // ===== Batching =====

        /// <summary>
        /// Suppresses <see cref="CurrentViewModel"/> change notifications until the returned scope
        /// is disposed. Useful for compound navigation sequences (deep-link handling, session
        /// restore, multi-step programmatic navigation) where the View should render only the
        /// final state, not each intermediate one.
        /// 
        /// Lifecycle hooks fire normally during the batch. History mutations apply normally.
        /// ViewModels pushed during the batch are real frames in the history and will be
        /// recoverable via Back/Forward as usual. Only the View binding update is deferred.
        /// 
        /// Nested batches are supported; the View updates only when the outermost scope disposes.
        /// </summary>
        IDisposable BeginBatch();
    }
}