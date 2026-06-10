using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using MVVM_Base.Services.Monitor.Contracts;
using MVVM_Base.Services.Navigation.Contracts;

namespace MVVM_Base.Services.Monitor.Internal
{
    /// <summary>
    /// Per-display enrichment machinery. Owns the lifecycle of snippet instances for one secondary
    /// display: the per-parent cache, birth (via <see cref="ISnippetFactory"/>), death (via the
    /// facade's <see cref="INavigationFacade.ViewModelDiscarded"/>), and visibility hooks
    /// (<see cref="IMonitorScreenAware"/>).
    ///
    /// The shell (a projection layer) drives this through <see cref="Project"/>, pushing the complete
    /// current enrichment fact each time it recomputes. The host is passive about "what's current" —
    /// it is told. It subscribes only to <see cref="INavigationFacade.ViewModelDiscarded"/> directly,
    /// because death is its concern and a discard can fire for a parent that isn't current.
    ///
    /// One instance per secondary display, so two displays enriching the same parent hold independent
    /// snippet instances (independent selection, independent live state).
    /// </summary>
    internal sealed class MonitorEnrichmentHost : IDisposable
    {
        private readonly ISnippetFactory _factory;
        private readonly INavigationFacade _nav;

        private readonly Dictionary<ObservableObject, ParentEntry> _cache = new();

        // The instance currently shown (OnShown) on this display, or null. The at-most-one-active
        // invariant; the only mutable liveness state.
        private ObservableObject? _liveInstance;

        // Held solely so the discard handler can tell whether a discarded VM was the current parent.
        private ObservableObject? _currentParent;

        private bool _disposed;

        public MonitorEnrichmentHost(ISnippetFactory factory, INavigationFacade nav)
        {
            _factory = factory;
            _nav = nav;
            _nav.ViewModelDiscarded += OnViewModelDiscarded;
        }

        /// <summary>
        /// Pushes the complete current enrichment fact and returns the instance to render (or null).
        /// Resolves/caches the selected snippet, runs visibility transitions when the live instance
        /// changes, and returns it. Safe to call on every shell recompute — unchanged successive
        /// calls produce no liveness side effects (guarded in <see cref="UpdateLiveness"/>).
        /// </summary>
        /// <param name="parent">The current ViewModel, or null. Cache key for its snippet instances.</param>
        /// <param name="selected">
        /// The selected snippet definition for this parent (id for caching + type for birth), or null
        /// when there is no enrichment content to show (no catalog, no selection, or the rendered
        /// surface is not an enrichment snippet — Logo / Mirror / degraded fallback).
        /// </param>
        public object? Project(ObservableObject? parent, SnippetDefinition? selected)
        {
            _currentParent = parent;

            var instance = (parent is not null && selected is not null)
                ? GetOrCreate(parent, selected)
                : null;

            UpdateLiveness(instance);   // selected==null → instance==null → hides current; else shows it

            return instance;
        }

        /// <summary>
        /// The last-selected snippet id for <paramref name="parent"/>, or null if this parent has never
        /// been enriched on this display. Memory lives as long as the parent's cache entry — i.e. as long
        /// as the parent is reachable in history — and is evicted with it on permanent discard.
        /// </summary>
        public string? LastSelectedId(ObservableObject parent)
            => _cache.TryGetValue(parent, out var entry) ? entry.SelectedId : null;

        // ===== Cache / birth =====

        private ObservableObject GetOrCreate(ObservableObject parent, SnippetDefinition def)
        {
            if (!_cache.TryGetValue(parent, out var entry))
            {
                entry = new ParentEntry();
                _cache[parent] = entry;
            }
            entry.SelectedId = def.Id;

            if (!entry.Instances.TryGetValue(def.Id, out var instance))
            {
                instance = _factory.Create(def.SnippetType);
                entry.Instances[def.Id] = instance;
            }
            return instance;
        }

        // ===== Liveness =====

        private void UpdateLiveness(ObservableObject? shouldBeLive)
        {
            if (ReferenceEquals(_liveInstance, shouldBeLive))
                return;

            (_liveInstance as IMonitorScreenAware)?.OnHidden();
            _liveInstance = shouldBeLive;
            (_liveInstance as IMonitorScreenAware)?.OnShown();
        }

        // ===== Death =====

        private void OnViewModelDiscarded(object? sender, ObservableObject vm)
        {
            if (!_cache.TryGetValue(vm, out var entry))
                return;

            foreach (var instance in entry.Instances.Values)
            {
                if (ReferenceEquals(instance, _liveInstance))
                {
                    (instance as IMonitorScreenAware)?.OnHidden();
                    _liveInstance = null;
                }
                (instance as IDisposable)?.Dispose();
            }

            _cache.Remove(vm);

            if (ReferenceEquals(vm, _currentParent))
                _currentParent = null;
        }

        // ===== Teardown =====

        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;

            _nav.ViewModelDiscarded -= OnViewModelDiscarded;

            (_liveInstance as IMonitorScreenAware)?.OnHidden();
            _liveInstance = null;

            foreach (var entry in _cache.Values)
                foreach (var instance in entry.Instances.Values)
                    (instance as IDisposable)?.Dispose();
            _cache.Clear();
        }

        private sealed class ParentEntry
        {
            public string? SelectedId;
            public readonly Dictionary<string, ObservableObject> Instances = new();
        }
    }
}