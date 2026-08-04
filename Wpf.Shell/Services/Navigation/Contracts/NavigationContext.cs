using System;
using System.Collections.Generic;
using System.Text;

namespace Wpf.Shell.Services.Navigation.Contracts
{
    /// <summary>
    /// Contextual information passed to a ViewModel's <c>OnNavigatedToAsync</c> hook.
    /// 
    /// Allows a ViewModel to distinguish first-time initialization from subsequent re-entries,
    /// and to know which kind of navigation event caused it to become current.
    /// </summary>
    /// <param name="IsFirstNavigation">
    /// True when the ViewModel's <c>OnNavigatedToAsync</c> is being called for the first time
    /// on this instance. False on every subsequent call to the same instance (recovery via
    /// Forward, return via Backward, re-display via TabSwitch).
    /// 
    /// All VMs (singleton or transient) preserve their instance as long as they remain in the
    /// navigation history. A transient VM only sees <c>IsFirstNavigation: true</c> a second time
    /// if its previous instance was discarded (user branched past it) and a new instance was
    /// later constructed for a fresh navigation to the same type.
    /// </param>
    /// <param name="From">
    /// The kind of navigation event that caused the ViewModel to become current. Combined with
    /// <paramref name="IsFirstNavigation"/>, lets the VM decide between full load, refresh, or no-op.
    /// 
    /// Common patterns:
    /// <list type="bullet">
    ///   <item><c>(true, Forward)</c> — first activation; load all data.</item>
    ///   <item><c>(false, Backward)</c> — user returned via back; consider refreshing stale data.</item>
    ///   <item><c>(false, Forward)</c> — singleton re-entered after a discard above it; consider refresh.</item>
    ///   <item><c>(false, TabSwitch)</c> — user switched back to this tab; usually do nothing.</item>
    /// </list>
    /// </param>
    public sealed record NavigationContext(
        bool IsFirstNavigation,
        NavigationDirection From);
}
