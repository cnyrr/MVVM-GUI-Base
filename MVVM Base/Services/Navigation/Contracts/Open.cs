using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Text;

namespace MVVM_Base.Services.Navigation.Contracts
{
    /// <summary>
    /// Generic navigation intent for ViewModels that take no parameters.
    /// 
    /// Use this when a ViewModel doesn't need data passed at navigation time — typically pages
    /// or root-style views that load their own data without external input:
    /// 
    /// <code>
    /// // Call site:
    /// await _nav.NavigateAsync(new Open&lt;DashboardViewModel&gt;());
    /// </code>
    /// 
    /// For ViewModels that take parameters, define a parameter record that implements
    /// <see cref="INavigationIntent{TViewModel, TParameters}"/> directly instead of using this type.
    /// </summary>
    /// <typeparam name="TViewModel">The ViewModel to navigate to.</typeparam>
    public sealed record Open<TViewModel> : INavigationIntent<TViewModel, NoParameters>
        where TViewModel : ObservableObject
    {
        /// <summary>
        /// Returns <see cref="NoParameters.Instance"/>.
        /// </summary>
        public NoParameters ToParameters() => NoParameters.Instance;
    }
}
