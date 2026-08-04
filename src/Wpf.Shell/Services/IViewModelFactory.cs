using CommunityToolkit.Mvvm.ComponentModel;

namespace Wpf.Shell.Services
{
    /// <summary>
    /// Resolves ViewModel instances from the dependency injection container.
    /// 
    /// Used by any component that needs to materialize ViewModels at runtime by type.
    /// 
    /// The factory is a thin wrapper around <see cref="System.IServiceProvider"/> that exists
    /// to keep consuming services' dependencies typed and explicit, and to provide a single
    /// seam for tests to substitute hand-rolled ViewModel instances.
    /// </summary>
    public interface IViewModelFactory
    {
        /// <summary>
        /// Creates or resolves an instance of <typeparamref name="TViewModel"/>.
        /// 
        /// Behavior depends on the ViewModel's DI registration:
        /// <list type="bullet">
        ///   <item>Singleton: returns the cached instance (created once per app lifetime).</item>
        ///   <item>Transient: returns a fresh instance with all dependencies resolved.</item>
        /// </list>
        /// 
        /// Throws if <typeparamref name="TViewModel"/> is not registered in DI. This is treated
        /// as a startup misconfiguration — the developer forgot to register the ViewModel.
        /// The framework does not catch this exception.
        /// </summary>
        TViewModel Create<TViewModel>() where TViewModel : ObservableObject;
    }
}
