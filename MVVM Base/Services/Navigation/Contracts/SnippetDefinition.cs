using CommunityToolkit.Mvvm.ComponentModel;
using System;

namespace MVVM_Base.Services.Navigation.Contracts
{
    /// <summary>
    /// One entry in an <see cref="IMonitorAware.SnippetCatalog"/>. Pure declaration: identifies a
    /// snippet ViewModel type, the label the dock shows for it, and a stable id used as the
    /// per-parent cache key and the dock's selection handle.
    ///
    /// Carries a <see cref="Type"/>, not a factory delegate and not an instance. The monitor shell
    /// resolves the type through the DI ViewModel factory
    /// (<see cref="MVVM_Base.Services.IViewModelFactory"/>) — the single construction seam used
    /// everywhere in the app — so snippet construction is DI's responsibility, identical to how tab
    /// roots and detail ViewModels are built. Snippet ViewModels register transient, so each
    /// display's shell resolves its own independent instance.
    ///
    /// Prefer <see cref="For{TSnippet}"/> over the raw constructor for compile-time type safety.
    /// </summary>
    /// <param name="Id">
    /// Stable identifier. Cache key under which a display preserves this snippet's instance across
    /// navigation, and the dock's selection handle. Unique within a single catalog.
    /// </param>
    /// <param name="DockLabel">Text the dock shows for this snippet's selector button.</param>
    /// <param name="SnippetViewModelType">
    /// Concrete snippet ViewModel type, resolved via the DI ViewModel factory. Must be registered
    /// (transient) in DI and must have a corresponding DataTemplate.
    /// </param>
    public sealed record SnippetDefinition(
        string Id,
        string DockLabel,
        Type SnippetViewModelType)
    {
        /// <summary>
        /// Builds a definition for snippet type <typeparamref name="TSnippet"/> with compile-time
        /// enforcement that the type is a ViewModel. Preferred over the raw constructor.
        /// </summary>
        public static SnippetDefinition For<TSnippet>(string id, string dockLabel)
            where TSnippet : ObservableObject
            => new(id, dockLabel, typeof(TSnippet));
    }
}