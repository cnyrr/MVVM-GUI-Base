using System.Collections.Generic;

namespace Wpf.Shell.Services.Navigation.Contracts
{
    /// <summary>
    /// Optional role interface for ViewModels that offer enrichment content to secondary
    /// displays. A ViewModel implementing this declares a catalog of snippets it relates to;
    /// the monitor subsystem consumes the catalog to populate each display's dock and to
    /// construct snippet instances per display.
    ///
    /// Declaration only — the catalog carries snippet *types* and labels, never instances.
    /// Construction is delegated entirely to the monitor subsystem via the DI ViewModel factory,
    /// so a ViewModel implementing this never instantiates, owns, or disposes a snippet, and
    /// never knows how many displays exist or whether any do.
    ///
    /// Lives beside <see cref="INavigationAware{TParameters}"/> deliberately: a feature ViewModel
    /// implements it without taking any dependency on the monitor subsystem. The dependency arrow
    /// points one way — features produce the catalog, the monitor subsystem consumes it.
    /// </summary>
    public interface IMonitorAware
    {
        /// <summary>
        /// The ordered set of snippets this ViewModel offers, in dock display order. Read by each
        /// display's shell when this ViewModel becomes current. Stable for the ViewModel's
        /// lifetime; built once (typically in the constructor) and not changed.
        /// </summary>
        IReadOnlyList<SnippetDefinition> SnippetCatalog { get; }
    }
}