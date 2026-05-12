using System;
using System.Collections.Generic;
using System.Text;

namespace MVVM_Base.Services.Navigation.Internal
{
    /// <summary>
    /// Metadata seed for a single tab. One <see cref="TabRegistration"/> is added to the DI
    /// container per <see cref="MVVM_Base.Services.Navigation.NavigationRegistration.AddTab{TRoot}"/>
    /// call. The DI container aggregates them into <see cref="IEnumerable{TabRegistration}"/>,
    /// which <see cref="NavigationService"/> consumes at construction time to build its tab list.
    ///
    /// Registration order is preserved by Microsoft.Extensions.DependencyInjection for singletons
    /// of the same service type, so the order of <c>AddTab</c> calls becomes the order of tabs
    /// in the sidebar.
    ///
    /// Internal because nothing outside the navigation service should consume it directly —
    /// consumers see the resolved <see cref="MVVM_Base.Services.Navigation.Contracts.IRootViewModel"/>
    /// instances via <see cref="MVVM_Base.Services.Navigation.Contracts.INavigationFacade.Tabs"/>.
    /// </summary>
    /// <param name="RootViewModelType">
    /// The concrete type of the root ViewModel. <see cref="NavigationService"/> resolves this
    /// via <see cref="MVVM_Base.Services.IViewModelFactory"/> eagerly in its constructor.
    /// </param>
    internal sealed record TabRegistration(Type RootViewModelType);
}