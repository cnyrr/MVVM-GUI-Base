using System;
using System.Collections.Generic;
using System.Text;

namespace Wpf.Shell.Services.Navigation.Contracts
{
    /// <summary>
    /// Sentinel parameter type for ViewModels that do not require navigation parameters.
    /// 
    /// ViewModels declare <c>INavigationAware&lt;NoParameters&gt;</c> when they need lifecycle hooks
    /// but no input data. The framework passes <see cref="Instance"/> to satisfy the type contract.
    /// 
    /// Use the parameterless overload of <c>INavigationService.NavigateToAsync&lt;TVM&gt;()</c> to
    /// navigate to such ViewModels — the framework supplies the instance.
    /// </summary>
    public sealed record NoParameters
    {
        /// <summary>
        /// The single shared instance. Use this everywhere a <see cref="NoParameters"/> value is needed.
        /// </summary>
        public static readonly NoParameters Instance = new();

        private NoParameters() { }
    }
}
