using CommunityToolkit.Mvvm.ComponentModel;
using System;

namespace MVVM_Base.Services.Navigation.Contracts
{
    /// <summary>
    /// One entry in an <see cref="IMonitorAware.SnippetCatalog"/>. Pure declaration: a stable id,
    /// the dock's label, and the snippet ViewModel's <see cref="Type"/> — identity only, never a
    /// factory delegate and never an instance.
    ///
    /// The catalog is a marker: it names which ViewModels a screen offers for enrichment. It carries
    /// no construction means — birth is the monitor subsystem's concern, performed by
    /// <see cref="MVVM_Base.Services.Monitor.Contracts.ISnippetFactory"/>, which resolves the type
    /// from DI. A ViewModel declaring a catalog neither constructs nor owns the snippets; it only
    /// declares that they relate to it.
    ///
    /// Prefer <see cref="For{TSnippet}"/> for compile-time type safety over the raw constructor.
    /// </summary>
    /// <param name="Id">
    /// Stable identifier. The per-parent cache key and the dock's selection handle. Unique within a
    /// single catalog.
    /// </param>
    /// <param name="DockLabel">Text the dock shows for this snippet's selector button.</param>
    /// <param name="SnippetType">
    /// Concrete snippet ViewModel type, resolved from DI by the snippet factory. Must be registered
    /// (transient) and have a corresponding DataTemplate.
    /// </param>
    public sealed record SnippetDefinition(
        string Id,
        string DockLabel,
        Type SnippetType)
    {
        /// <summary>Builds a definition for snippet type <typeparamref name="TSnippet"/>.</summary>
        public static SnippetDefinition For<TSnippet>(string id, string dockLabel)
            where TSnippet : ObservableObject
            => new(id, dockLabel, typeof(TSnippet));
    }
}