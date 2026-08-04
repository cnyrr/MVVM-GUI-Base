using System;
using System.Collections.Generic;
using System.Text;

namespace Wpf.Shell.Services.Navigation.Contracts
{
    /// <summary>
    /// Contract identifying a ViewModel that serves as the root of a tab.
    ///
    /// Root ViewModels are singletons in the DI container — they live for the application's
    /// lifetime and preserve state across navigation context switches. They cannot be popped from
    /// the navigation history.
    ///
    /// Implementing this interface is the compile-time gate for tab registration:
    /// <see cref="Wpf.Shell.Services.Navigation.NavigationRegistration.AddTab{TRoot}"/> requires
    /// <c>TRoot : ViewModelBase, IRootViewModel</c>. Detail ViewModels (transient, pushed onto
    /// the history above a root) do not implement this interface.
    /// </summary>
    public interface IRootViewModel
    {
        /// <summary>
        /// Stable display label shown in the sidebar. Read once by the navigation service when
        /// building the tab list — roots set this in their constructor and do not change it.
        ///
        /// Independent of <see cref="Wpf.Shell.ViewModels.ViewModelBase.Name"/>. A root may
        /// implement <c>Label</c> as a literal expression-bodied property; it has no relationship
        /// to the observable <c>Name</c> unless the root chooses to wire them.
        /// </summary>
        string TabLabel { get; }
    }
}