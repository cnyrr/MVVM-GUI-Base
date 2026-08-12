namespace Wpf.Shell.Services.Navigation.Internal
{
    /// <summary>
    /// Metadata seed for a single tab. One <see cref="TabRegistration"/> per
    /// <c>ShellBuilder.AddTab&lt;TRoot&gt;()</c> call.
    ///
    /// The builder owns the list and passes it to
    /// <see cref="INavigationService.InitializeTabs"/> directly. It is never registered in the DI
    /// container — the container holds the roots themselves, but the tab *set* has exactly one
    /// owner, and list order is the sidebar order with no dependency on container behavior.
    /// </summary>
    /// <param name="RootViewModelType">
    /// The concrete type of the root ViewModel, resolved via
    /// <see cref="Wpf.Shell.Services.IViewModelFactory"/> during
    /// <see cref="INavigationService.InitializeTabs"/>.
    /// </param>
    internal sealed record TabRegistration(Type RootViewModelType);
}