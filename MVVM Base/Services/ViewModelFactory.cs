using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;

namespace MVVM_Base.Services
{
    /// <summary>
    /// Default implementation of <see cref="IViewModelFactory"/>.
    /// 
    /// Thin wrapper around <see cref="IServiceProvider"/> that resolves ViewModel instances
    /// according to their DI lifetime registration. Exists to keep consuming services'
    /// dependencies typed and to provide a substitution seam for tests.
    /// </summary>
    internal sealed class ViewModelFactory : IViewModelFactory
    {
        private readonly IServiceProvider _provider;

        public ViewModelFactory(IServiceProvider provider)
        {
            _provider = provider;
        }

        public TViewModel Create<TViewModel>() where TViewModel : ObservableObject
            => _provider.GetRequiredService<TViewModel>();
    }
}
