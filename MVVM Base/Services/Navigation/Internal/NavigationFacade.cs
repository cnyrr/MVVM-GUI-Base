using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using MVVM_Base.Services.Navigation.Contracts;

namespace MVVM_Base.Services.Navigation.Internal
{
    /// <summary>
    /// Default implementation of <see cref="INavigationFacade"/>.
    /// 
    /// Wraps the internal <see cref="INavigationService"/> and adds two responsibilities:
    /// <list type="bullet">
    ///   <item>Recover-vs-branch policy: inspects the active tab's next forward frame and
    ///   decides between <c>GoForwardAsync</c> (recovery) and <c>NavigateToAsync</c> (branch)
    ///   based on VM type and parameter equality.</item>
    ///   <item>Batch-aware notification re-raising: subscribes to the service's
    ///   <see cref="INotifyPropertyChanged"/> and re-raises events for consumers, suppressing
    ///   all property notifications during a <see cref="BeginBatch"/> scope so that compound
    ///   navigation sequences appear as a single atomic transition.</item>
    /// </list>
    /// </summary>
    internal sealed class NavigationFacade : ObservableObject, INavigationFacade
    {
        private readonly INavigationService _service;

        private int _batchDepth;
        private readonly HashSet<string> _suppressedProperties = new();

        public NavigationFacade(INavigationService service)
        {
            _service = service;
            _service.PropertyChanged += OnServicePropertyChanged;
        }

        // ===== State (forwarded from service) =====

        public ObservableObject? CurrentViewModel => _service.CurrentViewModel;
        public IRootViewModel? ActiveTab => _service.ActiveTab;
        public IEnumerable<IRootViewModel> Tabs => _service.Tabs;
        public bool CanGoBack => _service.CanGoBack;
        public bool CanGoForward => _service.CanGoForward;

        // ===== Operations =====

        public Task NavigateAsync<TViewModel, TParameters>(
            INavigationIntent<TViewModel, TParameters> intent)
            where TViewModel : ObservableObject where TParameters : notnull, IEquatable<TParameters>
        {
            AssertUiThread();
            ArgumentNullException.ThrowIfNull(intent);

            var parameters = intent.ToParameters();

            // Recover-vs-branch decision.
            if (TryRecoverForward<TViewModel, TParameters>(parameters))
                return _service.GoForwardAsync();

            return _service.NavigateToAsync<TViewModel, TParameters>(parameters);
        }

        public Task GoBackAsync()
        {
            AssertUiThread();
            return _service.GoBackAsync();
        }

        public Task GoForwardAsync()
        {
            AssertUiThread();
            return _service.GoForwardAsync();
        }

        public Task SwitchTabAsync(IRootViewModel root)
        {
            AssertUiThread();
            return _service.SwitchTabAsync(root);
        }

        // ===== Batching =====

        public IDisposable BeginBatch()
        {
            AssertUiThread();
            _batchDepth++;
            return new BatchScope(this);
        }

        private void EndBatch()
        {
            _batchDepth--;
            if (_batchDepth > 0)
                return;

            // Outermost batch closed. Fire each suppressed property change exactly once,
            // in deterministic order, then clear.
            var suppressed = _suppressedProperties.ToArray();
            _suppressedProperties.Clear();

            foreach (var name in suppressed)
                OnPropertyChanged(name);
        }

        private sealed class BatchScope : IDisposable
        {
            private readonly NavigationFacade _facade;
            private bool _disposed;

            public BatchScope(NavigationFacade facade) => _facade = facade;

            public void Dispose()
            {
                if (_disposed)
                    return;
                _disposed = true;
                _facade.EndBatch();
            }
        }

        // ===== Internals =====

        /// <summary>
        /// Re-raises property change notifications from the service. During an active batch,
        /// records the property name for deferred firing instead of raising immediately.
        /// </summary>
        private void OnServicePropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName is null)
                return;

            if (_batchDepth > 0)
            {
                _suppressedProperties.Add(e.PropertyName);
                return;
            }

            OnPropertyChanged(e.PropertyName);
        }

        /// <summary>
        /// Inspects the active tab's next forward frame. Returns true if it represents the same
        /// navigation as the requested intent (matching VM type and parameters that compare equal),
        /// indicating the framework should recover via <c>GoForwardAsync</c> rather than push.
        /// </summary>
        private bool TryRecoverForward<TViewModel, TParameters>(TParameters parameters)
            where TViewModel : ObservableObject
        {
            var peek = _service.PeekForward();
            if (peek is null)
                return false;

            if (peek.ViewModelType != typeof(TViewModel))
                return false;

            return peek.Parameters is TParameters stored
                && Equals(stored, parameters);
        }

        private static void AssertUiThread()
        {
            Debug.Assert(
                Application.Current.Dispatcher.CheckAccess(),
                "Navigation methods must be called on the UI thread.");
        }
    }
}