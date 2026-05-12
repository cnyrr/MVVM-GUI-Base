using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using MVVM_Base.Services;
using MVVM_Base.Services.Navigation.Contracts;

namespace MVVM_Base.Services.Navigation.Internal
{
    /// <summary>
    /// Default implementation of <see cref="INavigationService"/>.
    /// 
    /// Owns one <see cref="TabHistory"/> per registered tab, keyed by the root ViewModel's
    /// concrete <see cref="Type"/>. Tabs are constructed eagerly at service construction: each
    /// <see cref="TabRegistration"/> in DI is resolved via <see cref="IViewModelFactory"/> and
    /// stored as a root frame in its own history. The first <see cref="SwitchTabAsync"/> call
    /// activates one of those existing tabs; subsequent calls switch between them.
    /// 
    /// Active tab is identified by <see cref="ActiveTab"/>; the current view is the frame at the
    /// active tab's current index. Property change notifications are raised through
    /// <see cref="ObservableObject"/>.
    /// </summary>
    internal sealed class NavigationService : ObservableObject, INavigationService
    {
        private readonly IViewModelFactory _factory;
        private readonly Dictionary<Type, TabHistory> _tabs = new();
        private readonly IEnumerable<TabRegistration> _registrations;

        private IRootViewModel? _activeTab;
        private bool _initialized;

        public NavigationService(
            IViewModelFactory factory,
            IEnumerable<TabRegistration> registrations)
        {
            _factory = factory;
            _registrations = registrations;
            // Root construction is deferred to InitializeTabs(). Resolving roots here would
            // recurse into DI mid-construction of INavigationFacade — roots take INavigationFacade
            // as a dependency, and INavigationFacade is currently being built (it's why this
            // ctor is running). The container handles that recursion by spinning up additional
            // facade instances rather than failing fast, which manifests as repeated root ctor
            // calls and an eventually-broken graph. Bootstrap calls InitializeTabs() after
            // BuildHostAsync returns — past the point where the construction cycle can happen.
        }

        public void InitializeTabs()
        {
            if (_initialized)
                throw new InvalidOperationException(
                    "InitializeTabs() has already been called.");
            _initialized = true;

            // One pass through the registrations in DI order; resolve each root via the factory
            // and wrap it in a TabHistory. The factory is the single seam to DI — direct
            // IServiceProvider access is forbidden in the navigation framework. Roots are
            // singletons in DI, so the factory returns the canonical instances.
            //
            // Tab order: Microsoft.Extensions.DependencyInjection preserves singleton registration
            // order for the same service type, and Dictionary<,> preserves insertion order in
            // modern .NET. The sidebar reads Tabs.Values directly. If a future runtime ever
            // breaks dictionary insertion order, a separate ordered list becomes warranted.
            foreach (var registration in _registrations)
            {
                if (_tabs.ContainsKey(registration.RootViewModelType))
                    throw new InvalidOperationException(
                        $"Tab {registration.RootViewModelType.Name} is registered more than once.");

                // Same runtime-typed factory invocation pattern used by Phase 4 of NavigateToAsync.
                var rootObservable = (ObservableObject)_factory.GetType()
                    .GetMethod(nameof(IViewModelFactory.Create))!
                    .MakeGenericMethod(registration.RootViewModelType)
                    .Invoke(_factory, null)!;

                var root = (IRootViewModel)rootObservable;

                var rootFrame = new Frame(rootObservable, NoParameters.Instance, new CancellationTokenSource());
                _tabs[registration.RootViewModelType] = new TabHistory(rootFrame, root);
            }
        }

        // ===== State =====

        public ObservableObject? CurrentViewModel
            => _activeTab is null ? null : _tabs[_activeTab.GetType()].Current.ViewModel;

        public IRootViewModel? ActiveTab => _activeTab;

        public IEnumerable<IRootViewModel> Tabs => _tabs.Values.Select(h => h.Root);

        public bool CanGoBack
            => _activeTab is not null && _tabs[_activeTab.GetType()].CanGoBack;

        public bool CanGoForward
            => _activeTab is not null && _tabs[_activeTab.GetType()].CanGoForward;

        // ===== Operations =====

        public async Task NavigateToAsync<TViewModel, TParameters>(TParameters parameters)
            where TViewModel : ObservableObject where TParameters : notnull, IEquatable<TParameters>
        {
            AssertUiThread();
            EnsureBootstrapped();

            var tab = _tabs[_activeTab!.GetType()];
            var leaving = tab.Current;

            // Phase 1 — Guard check.
            if (leaving.ViewModel is INavigationGuard guard)
            {
                bool canLeave;
                try
                {
                    canLeave = await guard.CanNavigateAwayAsync();
                }
                catch
                {
                    // E1: a thrown guard is treated as "cannot leave."
                    // Logging deferred until the notification service exists.
                    return;
                }
                if (!canLeave)
                    return;
            }

            // Phase 2 — Leaving lifecycle.
            try
            {
                await DispatchOnNavigatedFromAsync(leaving, NavigationDirection.Forward);
            }
            catch
            {
                // E2: leaving lifecycle throw is a VM bug. Continue with navigation.
            }

            // Phase 3 — Truncate future, discarding any preserved frames.
            await TruncateFutureAsync(tab);

            // Phase 4 — Construct new frame, append, advance pointer.
            var newVm = (ObservableObject)_factory.GetType()
                .GetMethod(nameof(IViewModelFactory.Create))!
                .MakeGenericMethod(typeof(TViewModel))
                .Invoke(_factory, null)!;

            var newFrame = new Frame(newVm, parameters!, new CancellationTokenSource());
            tab.Push(newFrame);

            // Phase 5 — Notify View. The new VM renders in its loading state.
            OnPropertyChanged(nameof(CurrentViewModel));
            OnPropertyChanged(nameof(CanGoBack));
            OnPropertyChanged(nameof(CanGoForward));

            // Phase 6 — Arrive. The new VM loads its data.
            try
            {
                await DispatchOnNavigatedToAsync(
                    newFrame,
                    new NavigationContext(IsFirstNavigation: true, From: NavigationDirection.Forward));
            }
            catch (OperationCanceledException)
            {
                // Expected — the VM was discarded mid-load.
            }
            catch
            {
                // E5: VM bug. Discard the broken frame and revert to previous.
                await DiscardFrameAsync(newFrame);
                tab.Pop();
                OnPropertyChanged(nameof(CurrentViewModel));
                OnPropertyChanged(nameof(CanGoBack));
                OnPropertyChanged(nameof(CanGoForward));
            }
        }

        public async Task GoBackAsync()
        {
            AssertUiThread();
            EnsureBootstrapped();

            var tab = _tabs[_activeTab!.GetType()];

            if (!tab.CanGoBack)
                throw new InvalidOperationException(
                    "Cannot go back: already at the root of the active tab.");

            var leaving = tab.Current;

            // Phase 1 — Guard check.
            if (leaving.ViewModel is INavigationGuard guard)
            {
                bool canLeave;
                try
                { canLeave = await guard.CanNavigateAwayAsync(); }
                catch { return; }
                if (!canLeave)
                    return;
            }

            // Phase 2 — Leaving lifecycle.
            try
            {
                await DispatchOnNavigatedFromAsync(leaving, NavigationDirection.Backward);
            }
            catch { /* E2 */ }

            // Move the pointer back. No discard — preserved frame stays alive.
            tab.MoveBack();

            // Notify View.
            OnPropertyChanged(nameof(CurrentViewModel));
            OnPropertyChanged(nameof(CanGoBack));
            OnPropertyChanged(nameof(CanGoForward));

            // Arrive on the recovered frame.
            var arrived = tab.Current;
            try
            {
                await DispatchOnNavigatedToAsync(
                    arrived,
                    new NavigationContext(IsFirstNavigation: false, From: NavigationDirection.Backward));
            }
            catch (OperationCanceledException) { }
            catch { /* E5: leaving the user on a broken recovered frame. Logged when notification service exists. */ }
        }

        public async Task GoForwardAsync()
        {
            AssertUiThread();
            EnsureBootstrapped();

            var tab = _tabs[_activeTab!.GetType()];

            if (!tab.CanGoForward)
                throw new InvalidOperationException(
                    "Cannot go forward: already at the tip of the active tab.");

            var leaving = tab.Current;

            // Phase 1 — Guard check.
            if (leaving.ViewModel is INavigationGuard guard)
            {
                bool canLeave;
                try
                { canLeave = await guard.CanNavigateAwayAsync(); }
                catch { return; }
                if (!canLeave)
                    return;
            }

            // Phase 2 — Leaving lifecycle.
            try
            {
                await DispatchOnNavigatedFromAsync(leaving, NavigationDirection.Forward);
            }
            catch { /* E2 */ }

            // Move the pointer forward. No construction — recovered frame stays alive.
            tab.MoveForward();

            // Notify View.
            OnPropertyChanged(nameof(CurrentViewModel));
            OnPropertyChanged(nameof(CanGoBack));
            OnPropertyChanged(nameof(CanGoForward));

            // Arrive on the recovered frame.
            var arrived = tab.Current;
            try
            {
                await DispatchOnNavigatedToAsync(
                    arrived,
                    new NavigationContext(IsFirstNavigation: false, From: NavigationDirection.Forward));
            }
            catch (OperationCanceledException) { }
            catch { /* E5 */ }
        }

        public async Task SwitchTabAsync(IRootViewModel root)
        {
            AssertUiThread();
            ArgumentNullException.ThrowIfNull(root);

            var enteringType = root.GetType();
            if (!_tabs.TryGetValue(enteringType, out var enteringTab))
                throw new InvalidOperationException(
                    $"{enteringType.Name} is not a registered tab root.");

            // Already active: no-op.
            if (ReferenceEquals(root, _activeTab))
                return;

            // First activation: no leaving phase. The entering tab is fresh; its root frame
            // is the one constructed eagerly at service construction and has never been arrived on.
            var isInitialActivation = _activeTab is null;

            if (!isInitialActivation)
            {
                var leavingTab = _tabs[_activeTab!.GetType()];
                var leaving = leavingTab.Current;

                // Phase 1 — Guard check on the leaving VM.
                if (leaving.ViewModel is INavigationGuard guard)
                {
                    bool canLeave;
                    try
                    { canLeave = await guard.CanNavigateAwayAsync(); }
                    catch { return; }
                    if (!canLeave)
                        return;
                }

                // Phase 2 — Leaving lifecycle.
                try
                {
                    await DispatchOnNavigatedFromAsync(leaving, NavigationDirection.TabSwitch);
                }
                catch { /* E2 */ }
            }

            var arrived = enteringTab.Current;
            var isFirstNavigationOnArrived = !enteringTab.HasBeenActivated;
            enteringTab.MarkActivated();

            // Phase 4 — Switch the active pointer.
            _activeTab = root;

            // Phase 5 — Notify View.
            OnPropertyChanged(nameof(CurrentViewModel));
            OnPropertyChanged(nameof(ActiveTab));
            OnPropertyChanged(nameof(CanGoBack));
            OnPropertyChanged(nameof(CanGoForward));

            // Phase 6 — Arrive.
            try
            {
                await DispatchOnNavigatedToAsync(
                    arrived,
                    new NavigationContext(
                        IsFirstNavigation: isFirstNavigationOnArrived,
                        From: NavigationDirection.TabSwitch));
            }
            catch (OperationCanceledException) { }
            catch { /* E5 */ }
        }

        public ForwardPeek? PeekForward()
        {
            AssertUiThread();
            EnsureBootstrapped();

            var tab = _tabs[_activeTab!.GetType()];
            if (!tab.CanGoForward)
                return null;

            var next = tab.PeekForward();
            return new ForwardPeek(next.ViewModel.GetType(), next.Parameters);
        }

        private void EnsureBootstrapped()
        {
            if (_activeTab is null)
                throw new InvalidOperationException(
                    "Navigation service has no active tab. Call SwitchTabAsync first.");
        }

        private static void AssertUiThread()
        {
            Debug.Assert(
                Application.Current.Dispatcher.CheckAccess(),
                "Navigation service methods must be called on the UI thread.");
        }

        // ===== Lifecycle dispatch =====

        /// <summary>
        /// Calls OnNavigatedToAsync on the frame's VM if it implements INavigationAware for the
        /// frame's parameter type. The implementation check is at runtime against the stored
        /// parameters' concrete type.
        /// </summary>
        private static async Task DispatchOnNavigatedToAsync(Frame frame, NavigationContext context)
        {
            var vm = frame.ViewModel;
            var parameters = frame.Parameters;
            var parametersType = parameters.GetType();

            var awareType = typeof(INavigationAware<>).MakeGenericType(parametersType);

            if (!awareType.IsInstanceOfType(vm))
                return;

            var method = awareType.GetMethod(nameof(INavigationAware<object>.OnNavigatedToAsync))!;
            var task = (Task)method.Invoke(vm, new[] { parameters, context, (object)frame.Cts.Token })!;
            await task;
        }

        /// <summary>
        /// Calls OnNavigatedFromAsync on the frame's VM if it implements INavigationAware for the
        /// frame's parameter type.
        /// </summary>
        private static async Task DispatchOnNavigatedFromAsync(Frame frame, NavigationDirection direction)
        {
            var vm = frame.ViewModel;
            var parametersType = frame.Parameters.GetType();
            var awareType = typeof(INavigationAware<>).MakeGenericType(parametersType);

            if (!awareType.IsInstanceOfType(vm))
                return;

            var method = awareType.GetMethod(nameof(INavigationAware<object>.OnNavigatedFromAsync))!;
            var task = (Task)method.Invoke(vm, new object[] { direction })!;
            await task;
        }

        // ===== Discard cleanup =====

        /// <summary>
        /// Cancels the future frames in the active tab and runs the discard sequence on each.
        /// Called as Phase 3 of NavigateToAsync. Per E3, all discards run to completion regardless
        /// of individual failures, then navigation continues.
        /// </summary>
        private async Task TruncateFutureAsync(TabHistory tab)
        {
            var discarded = tab.TruncateFuture();
            foreach (var frame in discarded)
                await DiscardFrameAsync(frame);
        }

        /// <summary>
        /// Runs the discard sequence on a single frame:
        /// cancel CTS → fire OnNavigatedFromAsync(Discarded) → Dispose if implemented.
        /// Per E3, exceptions in any step are caught and ignored; the next step runs anyway.
        /// </summary>
        private static async Task DiscardFrameAsync(Frame frame)
        {
            try
            { frame.Cts.Cancel(); }
            catch { /* defensive */ }

            try
            {
                await DispatchOnNavigatedFromAsync(frame, NavigationDirection.Discarded);
            }
            catch { /* VM bug, continue cleanup */ }

            if (frame.ViewModel is IDisposable disposable)
            {
                try
                { disposable.Dispose(); }
                catch { /* VM bug */ }
            }

            try
            { frame.Cts.Dispose(); }
            catch { /* defensive */ }
        }

        // ===== Per-tab history =====

        /// <summary>
        /// One tab's navigation history. Encapsulates the list of frames and the current-index
        /// pointer. Exposes operations that maintain invariants.
        /// </summary>
        private sealed class TabHistory
        {
            private readonly List<Frame> _frames = new();
            private int _currentIndex;
            private bool _hasBeenActivated;

            public TabHistory(Frame rootFrame, IRootViewModel root)
            {
                _frames.Add(rootFrame);
                _currentIndex = 0;
                Root = root;
            }

            /// <summary>
            /// The tab's root ViewModel. Stable for the tab's lifetime; the same instance the
            /// dictionary keys to by type. Exposed so the navigation service's <c>Tabs</c>
            /// projection can read root references without re-casting frame ViewModels.
            /// </summary>
            public IRootViewModel Root { get; }

            public Frame Current => _frames[_currentIndex];
            public bool CanGoBack => _currentIndex > 0;
            public bool CanGoForward => _currentIndex < _frames.Count - 1;
            public bool HasBeenActivated => _hasBeenActivated;

            public void MarkActivated() => _hasBeenActivated = true;

            public void Push(Frame frame)
            {
                _frames.Add(frame);
                _currentIndex = _frames.Count - 1;
            }

            public void Pop()
            {
                if (_frames.Count <= 1)
                    throw new InvalidOperationException("Cannot pop the root frame.");
                _frames.RemoveAt(_frames.Count - 1);
                _currentIndex = _frames.Count - 1;
            }

            public void MoveBack()
            {
                if (!CanGoBack)
                    throw new InvalidOperationException("Cannot move back: at root.");
                _currentIndex--;
            }

            public void MoveForward()
            {
                if (!CanGoForward)
                    throw new InvalidOperationException("Cannot move forward: at tip.");
                _currentIndex++;
            }

            public Frame PeekForward()
            {
                if (!CanGoForward)
                    throw new InvalidOperationException("No forward frame to peek.");
                return _frames[_currentIndex + 1];
            }

            /// <summary>
            /// Removes all frames after the current index and returns them in order.
            /// Caller is responsible for running the discard sequence on each.
            /// </summary>
            public List<Frame> TruncateFuture()
            {
                if (_currentIndex == _frames.Count - 1)
                    return new List<Frame>();

                var future = _frames.GetRange(_currentIndex + 1, _frames.Count - _currentIndex - 1);
                _frames.RemoveRange(_currentIndex + 1, _frames.Count - _currentIndex - 1);
                return future;
            }
        }
    }
}