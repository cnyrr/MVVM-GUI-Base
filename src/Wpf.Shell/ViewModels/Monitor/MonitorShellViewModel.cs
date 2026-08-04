using System;
using System.ComponentModel;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Wpf.Shell.Services.Monitor.Contracts;
using Wpf.Shell.Services.Monitor.Internal;
using Wpf.Shell.Services.Navigation.Contracts;

namespace Wpf.Shell.ViewModels.Monitor
{
    /// <summary>
    /// Per-display projection layer for one secondary display. Drives the view: derives the rendered
    /// surface from (mode, current VM, pre-Enrich mode), owns the dock's mode/lock/selection state,
    /// and projects the current ViewModel from <see cref="INavigationFacade"/>.
    ///
    /// It does NOT own enrichment machinery. Snippet birth, caching, death, and visibility hooks live
    /// in <see cref="MonitorEnrichmentHost"/> (one per display). The shell computes the current
    /// enrichment fact and pushes it to the host via <see cref="MonitorEnrichmentHost.Project"/>,
    /// rendering whatever the host returns. The shell is a projection that drives a view; the host is
    /// the machinery.
    /// </summary>
    internal sealed partial class MonitorShellViewModel : ViewModelBase, IDisposable
    {
        private readonly int _displayIndex;
        private readonly INavigationFacade _nav;
        private readonly MonitorEnrichmentHost _enrichment;

        private MonitorMode _modeBeforeEnrich = MonitorMode.Logo;
        private bool _disposed;

        public MonitorShellViewModel(
            int displayIndex,
            INavigationFacade nav,
            MonitorEnrichmentHost enrichment,
            IMonitorSettings settings,
            ILogger<MonitorShellViewModel> logger)
            : base(logger)
        {
            _displayIndex = displayIndex;
            _nav = nav;
            _enrichment = enrichment;

            _mode = settings.InitialMode(displayIndex);
            _isInputLocked = settings.InitialInputLocked(displayIndex);
            if (_mode == MonitorMode.Enrich)
                _modeBeforeEnrich = MonitorMode.Logo;

            _nav.PropertyChanged += OnNavigationPropertyChanged;

            Recompute();
        }

        public string DisplayLabel => $"Screen #{_displayIndex}";

        // ===== Sticky operator state =====

        [ObservableProperty]
        private MonitorMode _mode;

        [ObservableProperty]
        private bool _isInputLocked;

        // ===== Derived, bindable outputs =====

        [ObservableProperty]
        private object? _content;

        [ObservableProperty]
        private IReadOnlyList<SnippetDefinition> _dockSnippets = Array.Empty<SnippetDefinition>();

        public string? SelectedSnippetId { get; private set; }

        public bool CanEnrich => _nav.CurrentViewModel is IMonitorAware;

        public MonitorMode EffectiveMode =>
            Mode == MonitorMode.Enrich && _nav.CurrentViewModel is not IMonitorAware
                ? _modeBeforeEnrich
                : Mode;

        public bool IsLogoActive => EffectiveMode == MonitorMode.Logo;
        public bool IsMirrorActive => EffectiveMode == MonitorMode.Mirror;
        public bool IsEnrichActive => EffectiveMode == MonitorMode.Enrich;

        public bool ContentInteractive => !IsInputLocked;
        public bool ToastsVisible => EffectiveMode != MonitorMode.Logo;

        partial void OnIsInputLockedChanged(bool value)
            => OnPropertyChanged(nameof(ContentInteractive));

        // ===== Dock commands =====

        [RelayCommand]
        private void SetMode(MonitorMode mode)
        {
            if (mode == Mode)
                return;
            if (mode == MonitorMode.Enrich && Mode != MonitorMode.Enrich)
                _modeBeforeEnrich = Mode;
            Mode = mode;
            Recompute();
        }

        [RelayCommand] private void SetLogo() => SetMode(MonitorMode.Logo);
        [RelayCommand] private void SetMirror() => SetMode(MonitorMode.Mirror);
        [RelayCommand] private void SetEnrich() => SetMode(MonitorMode.Enrich);

        [RelayCommand]
        private void SelectSnippet(string? id)
        {
            if (id is null || id == SelectedSnippetId)
                return;
            SelectedSnippetId = id;
            Recompute();
        }

        // ===== Navigation reaction =====

        private void OnNavigationPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(INavigationFacade.CurrentViewModel))
            {
                // Restore this parent's last selection if the host remembers it; else Recompute auto-selects.
                SelectedSnippetId = _nav.CurrentViewModel is { } vm
                    ? _enrichment.LastSelectedId(vm)
                    : null;
                Recompute();
            }
        }

        // ===== The single decision point =====

        private void Recompute()
        {
            var current = _nav.CurrentViewModel;
            var catalog = (current as IMonitorAware)?.SnippetCatalog;

            DockSnippets = catalog ?? Array.Empty<SnippetDefinition>();

            // Auto-select the first snippet when entering an enrichable parent with no selection.
            if (catalog is { Count: > 0 } && SelectedSnippetId is null)
                SelectedSnippetId = catalog[0].Id;

            // Determine whether we are actually rendering an Enrich snippet, and which one.
            SnippetDefinition? selected = null;
            if (Mode == MonitorMode.Enrich && catalog is { Count: > 0 })
                selected = FindDefinition(catalog, SelectedSnippetId) ?? catalog[0];

            // Push the enrichment fact to the host; it returns the instance to render (or null).
            var enrichInstance = _enrichment.Project(current, selected);

            // Derive the rendered surface.
            Content = Mode switch
            {
                MonitorMode.Logo => null,
                MonitorMode.Mirror => current,
                MonitorMode.Enrich => selected is not null
                                        ? enrichInstance
                                        // degraded: no catalog — fall back to pre-Enrich surface
                                        : _modeBeforeEnrich == MonitorMode.Mirror ? current : null,
                _ => null,
            };

            // Notify derived members that depend on current VM / mode.
            OnPropertyChanged(nameof(SelectedSnippetId));
            OnPropertyChanged(nameof(CanEnrich));
            OnPropertyChanged(nameof(EffectiveMode));
            OnPropertyChanged(nameof(IsLogoActive));
            OnPropertyChanged(nameof(IsMirrorActive));
            OnPropertyChanged(nameof(IsEnrichActive));
            OnPropertyChanged(nameof(ToastsVisible));
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

        // ===== Teardown =====

        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;

            _nav.PropertyChanged -= OnNavigationPropertyChanged;
            _enrichment.Dispose();
        }
    }
}