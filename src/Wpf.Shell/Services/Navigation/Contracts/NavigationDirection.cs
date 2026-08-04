using System;
using System.Collections.Generic;
using System.Text;

namespace Wpf.Shell.Services.Navigation.Contracts
{
    /// <summary>
    /// Identifies why a ViewModel is being navigated away from or toward.
    /// 
    /// Used in <c>OnNavigatedFromAsync</c> to inform the leaving ViewModel about the cause of its
    /// departure, and in <see cref="NavigationContext"/> to inform the arriving ViewModel about how
    /// it became current.
    /// 
    /// Three of these values mean "the VM stays alive" (Forward, Backward, TabSwitch). One means
    /// "the VM is being destroyed" (Discarded).
    /// </summary>
    public enum NavigationDirection
    {
        /// <summary>
        /// A new frame is being pushed above the current one.
        /// The current VM stays alive in the history; the user can return to it via Back.
        /// </summary>
        Forward,

        /// <summary>
        /// The user moved the history pointer backward.
        /// The leaving VM stays alive in the history; the user can return to it via Forward.
        /// </summary>
        Backward,

        /// <summary>
        /// The user switched to a different tab.
        /// The leaving VM stays alive on its tab's history; switching back restores it.
        /// </summary>
        TabSwitch,

        /// <summary>
        /// The VM is being destroyed because the user branched from a non-tip position.
        /// All future frames beyond the branch point are discarded; their resources should be released.
        /// </summary>
        Discarded
    }
}
