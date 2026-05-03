using System;
using System.Collections.Generic;
using System.Text;

namespace MVVM_Base.Services.Navigation.Contracts
{
    /// <summary>
    /// Optional interface for ViewModels that need to control whether the user is allowed to
    /// navigate away from them.
    /// 
    /// The framework queries <see cref="CanNavigateAwayAsync"/> before mutating any navigation
    /// state. If the method returns false, the navigation is silently aborted — the user remains
    /// on the current ViewModel as if they hadn't initiated the navigation.
    /// 
    /// Typical use case: a form with unsaved changes shows a confirmation dialog inside this
    /// method and returns true only if the user confirms discarding the changes.
    /// 
    /// Implementing this interface is a deliberate choice. Most ViewModels do not need it.
    /// 
    /// Convention: this method should not throw. A throw is treated as "cannot navigate away" by
    /// the framework — safer to keep the user where they are than to risk an inconsistent state.
    /// </summary>
    public interface INavigationGuard
    {
        /// <summary>
        /// Returns true if the user may navigate away from this ViewModel, false otherwise.
        /// 
        /// Called before any navigation operation (push, back, forward, tab switch) that would
        /// change the current ViewModel. The framework awaits the result before proceeding,
        /// so this method may show dialogs, await user input, or perform async checks.
        /// </summary>
        Task<bool> CanNavigateAwayAsync();
    }
}
