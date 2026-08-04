using CommunityToolkit.Mvvm.ComponentModel;
using Wpf.Shell.Services.Monitor.Contracts;

namespace Wpf.Shell.Services.Monitor.Internal
{
    /// <summary>
    /// Default <see cref="ISnippetFactory"/>. Resolves snippet types through the app's
    /// <see cref="IViewModelFactory"/> — the single DI construction seam — using the same
    /// runtime-typed invocation pattern the navigation service uses for detail ViewModels. This is
    /// the one place snippet construction happens; the enrichment host calls here and nowhere else
    /// constructs snippets.
    /// </summary>
    internal sealed class SnippetFactory : ISnippetFactory
    {
        private readonly IViewModelFactory _factory;

        public SnippetFactory(IViewModelFactory factory) => _factory = factory;

        public ObservableObject Create(Type snippetType)
            => (ObservableObject)_factory.GetType()
                .GetMethod(nameof(IViewModelFactory.Create))!
                .MakeGenericMethod(snippetType)
                .Invoke(_factory, null)!;
    }
}