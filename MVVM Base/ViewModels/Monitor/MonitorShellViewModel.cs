using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using MVVM_Base.Services;
using MVVM_Base.Services.Monitor.Contracts;
using MVVM_Base.Services.Navigation.Contracts;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;

namespace MVVM_Base.ViewModels.Monitor
{
    /// <summary>
    /// Per-display projection layer for one secondary display. One instance exists per secondary
    /// display, each independent in mode, input lock, and snippet selection. Sibling to
    /// <see cref="Shell.ShellViewModel"/>: both project the single navigation context from
    /// <see cref="INavigationFacade"/>; neither owns navigation truth and neither knows the other
    /// exists.
    ///
    /// Read-only toward navigation in Enrich and Logo (pure projection). In Mirror, the view may
    /// forward input to the same shared facade when not input-locked — acting as an additional input
    /// device onto the one navigation context, never a second context. The shell exposes
    /// <see cref="IsInputLocked"/>; the view wires the actual forwarding and toast hit-testing.
    ///
    /// State model:
    /// <list type="bullet">
    ///   <item><see cref="Mode"/> — sticky operator intent; only the dock changes it.</item>
    ///   <item><see cref="_modeBeforeEnrich"/> — the surface to fall back to when Enrich has no
    ///   catalog to show; recorded on entering Enrich, defaults <see cref="MonitorMode.Logo"/>.</item>
    ///   <item><see cref="_cache"/> — per-parent-instance snippet instances + selection, preserving
    ///   state across back/forward and evicted on permanent discard.</item>
    ///   <item><see cref="_liveSnippet"/> — the single snippet currently <c>OnShown</c> on THIS
    ///   display; the at-most-one-active invariant.</item>
    /// </list>
    ///
    /// Every state change routes through <see cref="Recompute"/>, the single function that derives
    /// the rendered surface and drives <see cref="IMonitorScreenAware"/> visibility transitions.
    /// </summary>
    public sealed partial class MonitorShellViewModel : ViewModelBase, IDisposable
    {
        private readonly int _displayIndex;
        private readonly INavigationFacade _nav;
        private readonly IViewModelFactory _factory;

        private readonly Dictionary<ObservableObject, ParentEntry> _cache = new();

        private MonitorMode _modeBeforeEnrich = MonitorMode.Logo;
        private ObservableObject? _liveSnippet;
        private bool _disposed;

        public bool ToastsVisible => EffectiveMode != MonitorMode.Logo;

        /// <summary>
        /// Whether the content region (rendered surface + toast layer) accepts input. False when the
        /// display is input-locked. The dock is a separate layer and is never gated by this — it stays
        /// interactive in every mode so the lock can always be released.
        /// </summary>
        public bool ContentInteractive => !IsInputLocked;

        // <summary>
        /// The mode actually being rendered, as opposed to the operator's sticky <see cref="Mode"/>.
        /// Differs from <see cref="Mode"/> only in the degraded case: Enrich selected but the current
        /// ViewModel offers no catalog, so the pre-Enrich surface (Logo/Mirror) is what paints. The dock
        /// highlights this, not raw Mode, so the active key matches what's on screen.
        /// </summary>
        public MonitorMode EffectiveMode =>
            Mode == MonitorMode.Enrich && _nav.CurrentViewModel is not IMonitorAware
                ? _modeBeforeEnrich
                : Mode;

        public bool IsLogoActive => EffectiveMode == MonitorMode.Logo;
        public bool IsMirrorActive => EffectiveMode == MonitorMode.Mirror;
        public bool IsEnrichActive => EffectiveMode == MonitorMode.Enrich;

        /// <summary>
        /// True when the current ViewModel offers a snippet catalog — i.e. Enrich is usable. Drives the
        /// dock's Enrich button enabled-state. Changes with the current ViewModel.
        /// </summary>
        public bool CanEnrich => _nav.CurrentViewModel is IMonitorAware;

        partial void OnIsInputLockedChanged(bool value)
            => OnPropertyChanged(nameof(ContentInteractive));

        public MonitorShellViewModel(
            int displayIndex,
            INavigationFacade nav,
            IViewModelFactory factory,
            IMonitorSettings settings,
            ILogger<MonitorShellViewModel> logger)
            : base(logger)
        {
            _displayIndex = displayIndex;
            _nav = nav;
            _factory = factory;

            _mode = settings.InitialMode(displayIndex);
            _isInputLocked = settings.InitialInputLocked(displayIndex);
            if (_mode == MonitorMode.Enrich)
                _modeBeforeEnrich = MonitorMode.Logo; // no prior manual mode at cold start

            _nav.PropertyChanged += OnNavigationPropertyChanged;
            _nav.ViewModelDiscarded += OnViewModelDiscarded;

            Recompute();
        }

        /// <summary>Label for the dock header. "Screen #1" for the first secondary, etc.</summary>
        public string DisplayLabel => $"Screen #{_displayIndex}";

        // ===== Sticky operator state =====

        /// <summary>Operator-chosen mode. Changed only via <see cref="SetModeCommand"/>.</summary>
        [ObservableProperty]
        private MonitorMode _mode;

        /// <summary>
        /// When true, this display cannot drive the shared navigation (mirror forwarding) and its
        /// toast region is non-interactive. Render-only. The dock itself stays interactive so the
        /// lock can always be released.
        /// </summary>
        [ObservableProperty]
        private bool _isInputLocked;

        // ===== Derived, bindable outputs =====

        /// <summary>
        /// What the window paints below the dock. Null in Logo (dark/blank area). In Mirror, the
        /// facade's current ViewModel (same instance, second materialized view). In Enrich, the
        /// selected snippet instance — or the pre-Enrich surface when the current VM offers no
        /// catalog.
        /// </summary>
        [ObservableProperty]
        private object? _content;

        /// <summary>
        /// The current VM's snippet catalog if it is <see cref="IMonitorAware"/>, else empty. Drives
        /// the dock's dynamic snippet selector — present only on enrichable screens.
        /// </summary>
        [ObservableProperty]
        private IReadOnlyList<SnippetDefinition> _dockSnippets = Array.Empty<SnippetDefinition>();

        /// <summary>The selected snippet's id for the current parent, or null. Highlights the dock.</summary>
        public string? SelectedSnippetId =>
            _nav.CurrentViewModel is { } vm && _cache.TryGetValue(vm, out var e) ? e.SelectedId : null;

        // ===== Dock commands =====

        /// <summary>Sets the operator mode. Records the pre-Enrich surface when entering Enrich.</summary>
        [RelayCommand]
        private void SetMode(MonitorMode mode)
        {
            if (mode == Mode)
                return;

            if (mode == MonitorMode.Enrich && Mode != MonitorMode.Enrich)
                _modeBeforeEnrich = Mode; // remember Logo/Mirror to fall back to during gaps

            Mode = mode;
            Recompute();
        }

        /// <summary>Selects a snippet by id for the current parent (Enrich only).</summary>
        [RelayCommand]
        private void SelectSnippet(string? id)
        {
            if (id is null || _nav.CurrentViewModel is not { } vm)
                return;
            if (!_cache.TryGetValue(vm, out var entry) || entry.SelectedId == id)
                return;

            entry.SelectedId = id;
            Recompute();
        }

        /// <summary>Dock convenience commands — XAML binds these rather than passing enum literals.</summary>
        [RelayCommand] private void SetLogo() => SetMode(MonitorMode.Logo);
        [RelayCommand] private void SetMirror() => SetMode(MonitorMode.Mirror);
        [RelayCommand] private void SetEnrich() => SetMode(MonitorMode.Enrich);

        // ===== Navigation reactions =====

        private void OnNavigationPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(INavigationFacade.CurrentViewModel))
                Recompute();
        }

        private void OnViewModelDiscarded(object? sender, ObservableObject vm)
        {
            if (!_cache.TryGetValue(vm, out var entry))
                return;

            // Hide-then-dispose. If this parent's live snippet is the one currently shown on this
            // display, hide it first so any resource stops cleanly (covers the E5 path where a
            // discarded VM was momentarily current).
            foreach (var instance in entry.Instances.Values)
            {
                if (ReferenceEquals(instance, _liveSnippet))
                {
                    (instance as IMonitorScreenAware)?.OnHidden();
                    _liveSnippet = null;
                }
                (instance as IDisposable)?.Dispose();
            }

            _cache.Remove(vm);
            Recompute();
        }

        // ===== The single decision point =====

        /// <summary>
        /// Derives the rendered surface from (mode, current VM, pre-Enrich mode) and drives snippet
        /// visibility. Idempotent: every state change calls this; it figures out the correct
        /// <see cref="Content"/>, <see cref="DockSnippets"/>, and which snippet should be live, then
        /// applies exactly the transitions needed.
        /// </summary>
        private void Recompute()
        {
            var current = _nav.CurrentViewModel;
            var catalog = (current as IMonitorAware)?.SnippetCatalog;

            DockSnippets = catalog ?? Array.Empty<SnippetDefinition>();

            object? content;
            ObservableObject? shouldBeLive = null;

            switch (Mode)
            {
                case MonitorMode.Logo:
                    content = null;
                    break;

                case MonitorMode.Mirror:
                    content = current; // same instance; window materializes its own view
                    break;

                case MonitorMode.Enrich:
                    if (current is not null && catalog is { Count: > 0 })
                    {
                        var snippet = ResolveSelectedSnippet(current, catalog);
                        content = snippet;
                        shouldBeLive = snippet;
                    }
                    else
                    {
                        // No catalog to enrich — fall back to the surface in effect before Enrich.
                        content = _modeBeforeEnrich == MonitorMode.Mirror ? current : null;
                    }
                    break;

                default:
                    content = null;
                    break;
            }

            UpdateLiveness(shouldBeLive);

            Content = content;
            OnPropertyChanged(nameof(SelectedSnippetId));
            OnPropertyChanged(nameof(CanEnrich));
            OnPropertyChanged(nameof(EffectiveMode));
            OnPropertyChanged(nameof(IsLogoActive));
            OnPropertyChanged(nameof(IsMirrorActive));
            OnPropertyChanged(nameof(IsEnrichActive));
        }

        /// <summary>
        /// Returns the selected snippet instance for <paramref name="parent"/>, building the cache
        /// entry (auto-selecting the first catalog member) and lazily constructing the instance via
        /// the DI factory on first selection. Unselected catalog members are never built — keeping a
        /// performance-heavy snippet inert until actually shown.
        /// </summary>
        private ObservableObject ResolveSelectedSnippet(
            ObservableObject parent,
            IReadOnlyList<SnippetDefinition> catalog)
        {
            if (!_cache.TryGetValue(parent, out var entry))
            {
                entry = new ParentEntry { SelectedId = catalog[0].Id };
                _cache[parent] = entry;
            }

            var def = FindDefinition(catalog, entry.SelectedId) ?? catalog[0];
            entry.SelectedId = def.Id;

            if (!entry.Instances.TryGetValue(def.Id, out var instance))
            {
                instance = CreateSnippet(def.SnippetViewModelType);
                entry.Instances[def.Id] = instance;
            }
            return instance;
        }

        /// <summary>
        /// Applies the single visibility transition: hides the current live snippet if it differs
        /// from <paramref name="shouldBeLive"/>, then shows the new one. Enforces at-most-one-active
        /// on this display and the "inactive the moment not visible" guarantee — uniformly across
        /// navigation, mode change, selection change, and eviction.
        /// </summary>
        private void UpdateLiveness(ObservableObject? shouldBeLive)
        {
            if (ReferenceEquals(_liveSnippet, shouldBeLive))
                return;

            (_liveSnippet as IMonitorScreenAware)?.OnHidden();
            _liveSnippet = shouldBeLive;
            (_liveSnippet as IMonitorScreenAware)?.OnShown();
        }

        private static SnippetDefinition? FindDefinition(
            IReadOnlyList<SnippetDefinition> catalog, string? id)
        {
            if (id is null)
                return null;
            foreach (var d in catalog)
                if (d.Id == id)
                    return d;
            return null;
        }

        /// <summary>
        /// Resolves a snippet ViewModel by runtime type through <see cref="IViewModelFactory"/> —
        /// the same construction seam tabs and detail VMs use. Mirrors the reflection pattern in
        /// <c>NavigationService.InitializeTabs</c> so construction stays consistent and DI-owned.
        /// </summary>
        private ObservableObject CreateSnippet(Type snippetType)
            => (ObservableObject)_factory.GetType()
                .GetMethod(nameof(IViewModelFactory.Create))!
                .MakeGenericMethod(snippetType)
                .Invoke(_factory, null)!;

        // ===== Teardown =====

        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;

            _nav.PropertyChanged -= OnNavigationPropertyChanged;
            _nav.ViewModelDiscarded -= OnViewModelDiscarded;

            (_liveSnippet as IMonitorScreenAware)?.OnHidden();
            _liveSnippet = null;

            foreach (var entry in _cache.Values)
                foreach (var instance in entry.Instances.Values)
                    (instance as IDisposable)?.Dispose();
            _cache.Clear();
        }

        /// <summary>Per-parent cache: which snippet is selected and the instances built so far.</summary>
        private sealed class ParentEntry
        {
            public string? SelectedId;
            public readonly Dictionary<string, ObservableObject> Instances = new();
        }
    }
}