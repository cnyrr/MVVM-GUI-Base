using System;
using System.Collections.Generic;
using System.Text;

namespace Wpf.Shell.Services.Navigation.Contracts
{
    /// <summary>
    /// Optional lifecycle interface for ViewModels that participate in navigation.
    /// 
    /// ViewModels implement this interface to receive notifications when they become current
    /// (<see cref="OnNavigatedToAsync"/>) or are navigated away from (<see cref="OnNavigatedFromAsync"/>).
    /// 
    /// Both methods have default no-op implementations; ViewModels override only the hooks they need.
    /// 
    /// ViewModels that require no navigation parameters should declare
    /// <c>INavigationAware&lt;NoParameters&gt;</c>.
    /// </summary>
    /// <typeparam name="TParameters">
    /// The strongly-typed parameter contract for this ViewModel. Define a record per VM
    /// (e.g. <c>CustomerDetailParameters</c>) or use <see cref="NoParameters"/> for none.
    /// </typeparam>
    public interface INavigationAware<TParameters>
    {
        /// <summary>
        /// Called when this ViewModel becomes the current view.
        /// 
        /// Fires on first activation (push), on recovery (Forward after Back), on return (Back),
        /// and on tab re-display (TabSwitch). Use <paramref name="context"/> to distinguish.
        /// 
        /// Convention: this method should not throw. Validate parameters at entry and represent
        /// failures as ViewModel state (e.g. a LoadState property) rather than exceptions.
        /// Throws are treated as VM bugs by the framework — the broken VM is discarded and the
        /// previous frame becomes current.
        /// 
        /// <see cref="System.OperationCanceledException"/> is the exception: it propagates normally
        /// and indicates cooperative cancellation, not failure.
        /// </summary>
        /// <param name="parameters">
        /// The parameters supplied at navigation time. For <see cref="NoParameters"/>, this is
        /// always <see cref="NoParameters.Instance"/>.
        /// </param>
        /// <param name="context">
        /// Information about why this VM is becoming current
        /// (first navigation vs. subsequent, and the direction of the navigation event).
        /// </param>
        /// <param name="cancellationToken">
        /// Cancelled if the ViewModel is discarded while this method is in flight.
        /// Threaded through async calls; checked after each <c>await</c> before mutating state.
        /// </param>
        Task OnNavigatedToAsync(
            TParameters parameters,
            NavigationContext context,
            CancellationToken cancellationToken)
            => Task.CompletedTask;

        /// <summary>
        /// Called when this ViewModel is being navigated away from.
        /// 
        /// The <paramref name="direction"/> indicates whether the VM stays alive (Forward, Backward,
        /// TabSwitch) or is being destroyed (Discarded). VMs typically use this to save drafts on
        /// Forward, discard drafts on Backward, do nothing on TabSwitch, and release subscriptions
        /// on Discarded.
        /// </summary>
        /// <param name="direction">
        /// The kind of navigation event causing this VM to leave current view.
        /// </param>
        Task OnNavigatedFromAsync(NavigationDirection direction)
            => Task.CompletedTask;
    }
}
