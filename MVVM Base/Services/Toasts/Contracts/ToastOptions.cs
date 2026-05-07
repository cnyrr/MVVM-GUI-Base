using System;
using System.Collections.Generic;
using System.Text;

namespace MVVM_Base.Services.Toasts.Contracts
{
    /// <summary>
    /// Optional parameters for <see cref="IToastService.Show"/>. Init-only so a
    /// caller constructs with the with-expression pattern; defaults yield a
    /// neutral, normal-duration informational toast.
    /// </summary>
    /// <remarks>
    /// New options are added as new init-only properties; existing call sites
    /// remain source-compatible.
    /// </remarks>
    public sealed record ToastOptions
    {
        public ToastSeverity Severity { get; init; } = ToastSeverity.Info;

        public ToastDuration Duration { get; init; } = ToastDuration.Normal;
    }
}
