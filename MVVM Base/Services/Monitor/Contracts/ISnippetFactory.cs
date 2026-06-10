using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace MVVM_Base.Services.Monitor.Contracts
{
    /// <summary>
    /// Constructs snippet ViewModel instances by type. The single, isolatable seam for snippet
    /// birth: the enrichment host calls it to materialize a snippet; tests substitute a fake that
    /// returns spy instances, isolating creation from the host's cache/lifecycle logic.
    ///
    /// Distinct from the general <see cref="MVVM_Base.Services.IViewModelFactory"/> so snippet
    /// creation can be mocked without affecting (or depending on) all-VM creation.
    /// </summary>
    public interface ISnippetFactory
    {
        /// <summary>
        /// Resolves a fresh instance of <paramref name="snippetType"/> from DI. The type is expected
        /// to be registered transient, so each call yields an independent instance — which is what
        /// lets two displays enrich the same parent with independent snippet state.
        /// </summary>
        ObservableObject Create(Type snippetType);
    }
}