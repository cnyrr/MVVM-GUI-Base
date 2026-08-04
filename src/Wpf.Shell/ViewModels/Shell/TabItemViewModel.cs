using CommunityToolkit.Mvvm.ComponentModel;
using Wpf.Shell.Services.Navigation.Contracts;

namespace Wpf.Shell.ViewModels.Shell
{
    /// <summary>
    /// A single tab item shown in the sidebar. Wraps an <see cref="IRootViewModel"/> and carries
    /// the per-item denormalized <see cref="IsActive"/> flag.
    ///
    /// Derives directly from <see cref="ObservableObject"/> rather than <see cref="ViewModelBase"/>
    /// — pure-data leaf VMs are exempt from the universal-base rule per the project's design
    /// decision. There is no behavior to log; logging dependency would be busywork.
    /// </summary>
    public sealed partial class TabItemViewModel : ObservableObject
    {
        public TabItemViewModel(IRootViewModel root)
        {
            Root = root;
        }

        /// <summary>
        /// The underlying tab root. Passed to <see cref="INavigationFacade.SwitchTabAsync"/> when
        /// the user clicks this item. Stable for the lifetime of the wrapper (which is the
        /// lifetime of the app).
        /// </summary>
        public IRootViewModel Root { get; }

        /// <summary>
        /// Display label. Re-exposes <see cref="IRootViewModel.TabLabel"/> so the sidebar binds
        /// <c>{Binding Label}</c> directly against this VM.
        /// </summary>
        public string Label => Root.TabLabel;

        /// <summary>
        /// True when this tab is the active one. Updated by <see cref="SidebarViewModel"/> on
        /// every <see cref="INavigationFacade.ActiveTab"/> change. Bound via <c>DataTrigger</c> in
        /// the sidebar's item template to apply the selected-state visual treatment.
        /// </summary>
        [ObservableProperty]
        private bool _isActive;
    }
}