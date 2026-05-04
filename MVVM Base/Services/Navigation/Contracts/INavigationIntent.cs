using CommunityToolkit.Mvvm.ComponentModel;

namespace MVVM_Base.Services.Navigation.Contracts
{
    /// <summary>
    /// A typed navigation request. Implementations identify both the target ViewModel and the
    /// parameters to pass to it.
    /// 
    /// In typical use, the parameter record itself implements this interface — the intent and
    /// the parameters are the same value:
    /// 
    /// <code>
    /// public sealed record CustomerDetailParameters(Guid CustomerId)
    ///     : INavigationIntent&lt;CustomerDetailViewModel, CustomerDetailParameters&gt;
    /// {
    ///     public CustomerDetailParameters ToParameters() =&gt; this;
    /// }
    /// </code>
    /// 
    /// The target ViewModel is not required to implement
    /// <see cref="INavigationAware{TParameters}"/>. ViewModels that implement it receive lifecycle
    /// notifications with the typed parameters; ViewModels that don't implement it are still
    /// navigable but receive no lifecycle calls (parameters are ignored).
    /// 
    /// The framework dispatches navigation by reading the intent's type arguments and calling
    /// <see cref="ToParameters"/> at navigation time.
    /// </summary>
    /// <typeparam name="TViewModel">The ViewModel this intent navigates to.</typeparam>
    /// <typeparam name="TParameters">
    /// The parameter type carried by this navigation. Used to deliver to
    /// <see cref="INavigationAware{TParameters}.OnNavigatedToAsync"/> on ViewModels that implement
    /// it; ignored otherwise.
    /// </typeparam>
    public interface INavigationIntent<TViewModel, TParameters>
        where TViewModel : ObservableObject where TParameters : notnull
    {
        /// <summary>
        /// Produces the parameter value carried by this navigation.
        /// 
        /// Called once per navigation, at the moment the navigation is dispatched.
        /// </summary>
        TParameters ToParameters();
    }
}
