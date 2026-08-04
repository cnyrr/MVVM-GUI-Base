using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Wpf.Shell.Services.Navigation.Contracts;

namespace Wpf.Shell.ViewModels.Shell
{
    /// <summary>
    /// Sidebar VM. Wraps each registered tab root in a <see cref="TabItemViewModel"/> for binding,
    /// listens to <see cref="INavigationFacade"/> for active-tab changes, and exposes the tab
    /// switch command.
    ///
    /// <see cref="Tabs"/> is materialized once at construction. The facade has already eagerly
    /// resolved all roots by the time this VM is constructed, so iterating <c>nav.Tabs</c> is
    /// stable and complete.
    ///
    /// Active-tab denormalization: navigation owns the single source of truth
    /// (<see cref="INavigationFacade.ActiveTab"/>). This VM mirrors it as per-item
    /// <see cref="TabItemViewModel.IsActive"/> booleans, because XAML <c>DataTrigger</c> cannot
    /// cleanly compare two bindings — the converter pattern was rejected as bloat. The
    /// denormalization is N flags for N tabs, refreshed on every active-tab change.
    /// </summary>
    public sealed partial class SidebarViewModel : ViewModelBase
    {
        private readonly INavigationFacade _nav;

        public SidebarViewModel(INavigationFacade nav, ILogger<SidebarViewModel> logger)
            : base(logger)
        {
            _nav = nav;

            // Eager wrapper construction. The facade's Tabs is already populated (roots are
            // resolved in NavigationService's constructor, which runs before this VM is built).
            var items = new List<TabItemViewModel>();
            foreach (var root in _nav.Tabs)
                items.Add(new TabItemViewModel(root));
            Tabs = items;

            _nav.PropertyChanged += OnNavigationPropertyChanged;

            // Initial sync — if a tab is already active when this VM is constructed (it isn't
            // today, but the cost is one pass over a tiny list), reflect it in the wrappers.
            UpdateActive();
        }

        /// <summary>
        /// Tab items in registration order. Bound by the sidebar view's <c>ItemsControl</c>.
        /// Set once at construction; never changes.
        /// </summary>
        public IReadOnlyList<TabItemViewModel> Tabs { get; }

        /// <summary>
        /// Switches to the tab represented by <paramref name="item"/>. The shell observes the
        /// resulting <see cref="INavigationFacade.ActiveTab"/> change and collapses itself.
        /// </summary>
        [RelayCommand]
        private Task SwitchTabAsync(TabItemViewModel? item)
        {
            if (item is null)
                return Task.CompletedTask;
            return _nav.SwitchTabAsync(item.Root);
        }

        // ===== Internals =====

        private void OnNavigationPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(INavigationFacade.ActiveTab))
                UpdateActive();
        }

        private void UpdateActive()
        {
            var active = _nav.ActiveTab;
            foreach (var item in Tabs)
                item.IsActive = ReferenceEquals(item.Root, active);
        }
    }
}