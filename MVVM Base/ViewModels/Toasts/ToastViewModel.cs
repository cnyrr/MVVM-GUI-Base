using CommunityToolkit.Mvvm.ComponentModel;
using MVVM_Base.Services.Toasts.Contracts;
using MVVM_Base.Services.Toasts.Internal;

namespace MVVM_Base.ViewModels.Toasts
{
    /// <summary>
    /// A single toast. Pure data — no timer, no state machine. The host
    /// (<see cref="ToastHostViewModel"/>) owns expiration, dismissal, and pause.
    /// </summary>
    public sealed partial class ToastViewModel : ObservableObject
    {
        public ToastViewModel(string message, ToastSeverity severity, bool isSticky)
        {
            Message = message;
            Severity = severity;
            IsSticky = isSticky;
        }

        /// <summary>Displayed text.</summary>
        public string Message { get; }

        /// <summary>Determines accent color and icon.</summary>
        public ToastSeverity Severity { get; }

        /// <summary>
        /// True for severities that ignore auto-dismiss (today: errors).
        /// Sticky toasts are removable only via the X button.
        /// </summary>
        public bool IsSticky { get; }
    }
}
