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
    /// Owns one <see cref="TabHistory"/> per registered tab. Active tab is identified by
    /// <see cref="ActiveTab"/>; the current view is the frame at the active tab's current index.
    /// Property change notifications are raised through <see cref="ObservableObject"/>.
    /// </summary>
    internal sealed class NavigationService : ObservableObject, INavigationService
    {
        private readonly IViewModelFactory _factory;
        private readonly Dictionary<TabKey, Type> _registeredRoots = new();
        private readonly Dictionary<TabKey, TabHistory> _tabs = new();

        private TabKey? _activeTab;
        private bool _bootstrapped;

        public NavigationService(IViewModelFactory factory)
        {
            _factory = factory;
        }

        // ===== State =====

        public ObservableObject? CurrentViewModel
            => _activeTab is { } tab ? _tabs[tab].Current.ViewModel : null;

        public TabKey ActiveTab
            => _activeTab ?? throw new InvalidOperationException(
                "ActiveTab accessed before SetInitialTabAsync was called.");

        public bool CanGoBack
            => _activeTab is { } tab && _tabs[tab].CanGoBack;

        public bool CanGoForward
            => _activeTab is { } tab && _tabs[tab].CanGoForward;

        // ===== Setup =====

        public void RegisterTab<TRootVM>(TabKey key)
            where TRootVM : ObservableObject, IRootViewModel
        {
            AssertUiThread();

            if (_bootstrapped)
                throw new InvalidOperationException(
                    "Cannot register tabs after SetInitialTabAsync has been called.");

            if (_registeredRoots.ContainsKey(key))
                throw new InvalidOperationException(
                    $"Tab {key} is already registered.");

            _registeredRoots[key] = typeof(TRootVM);
        }

        public async Task SetInitialTabAsync(TabKey key)
        {
            AssertUiThread();

            if (_bootstrapped)
                throw new InvalidOperationException(
                    "SetInitialTabAsync has already been called.");

            if (!_registeredRoots.TryGetValue(key, out var rootType))
                throw new InvalidOperationException(
                    $"Tab {key} is not registered.");

            _bootstrapped = true;

            // Construct the initial tab's root and activate it.
            var rootFrame = ConstructRootFrame(rootType);
            _tabs[key] = new TabHistory(rootFrame);
            _tabs[key].MarkActivated();
            _activeTab = key;

            // Notify property changes before lifecycle so the View can render the loading state
            // while OnNavigatedToAsync runs.
            OnPropertyChanged(nameof(CurrentViewModel));
            OnPropertyChanged(nameof(ActiveTab));
            OnPropertyChanged(nameof(CanGoBack));
            OnPropertyChanged(nameof(CanGoForward));

            await DispatchOnNavigatedToAsync(
                rootFrame,
                new NavigationContext(IsFirstNavigation: true, From: NavigationDirection.Forward));
        }

        // ===== Helpers =====

        private Frame ConstructRootFrame(Type rootType)
        {
            var vm = (ObservableObject)_factory.GetType()
                .GetMethod(nameof(IViewModelFactory.Create))!
                .MakeGenericMethod(rootType)
                .Invoke(_factory, null)!;

            return new Frame(vm, NoParameters.Instance, new CancellationTokenSource());
        }

        private static void AssertUiThread()
        {
            Debug.Assert(
                Application.Current.Dispatcher.CheckAccess(),
                "Navigation service methods must be called on the UI thread.");
        }

        // ===== Operations =====

        public async Task NavigateToAsync<TViewModel, TParameters>(TParameters parameters)
            where TViewModel : ObservableObject
        {
            AssertUiThread();
            EnsureBootstrapped();

            var tab = _tabs[_activeTab!.Value];
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

            var tab = _tabs[_activeTab!.Value];

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

            var tab = _tabs[_activeTab!.Value];

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

        public async Task SwitchTabAsync(TabKey key)
        {
            AssertUiThread();
            EnsureBootstrapped();

            if (key == _activeTab)
                return;

            if (!_registeredRoots.TryGetValue(key, out var rootType))
                throw new InvalidOperationException(
                    $"Tab {key} is not registered.");

            var leavingTab = _tabs[_activeTab!.Value];
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

            // Lazy-construct the entering tab if this is its first activation.
            if (!_tabs.TryGetValue(key, out var enteringTab))
            {
                var rootFrame = ConstructRootFrame(rootType);
                enteringTab = new TabHistory(rootFrame);
                _tabs[key] = enteringTab;
            }

            var arrived = enteringTab.Current;
            var isFirstActivation = !enteringTab.HasBeenActivated;
            enteringTab.MarkActivated();

            // Phase 4 — Switch the active pointer.
            _activeTab = key;

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
                        IsFirstNavigation: isFirstActivation,
                        From: NavigationDirection.TabSwitch));
            }
            catch (OperationCanceledException) { }
            catch { /* E5 */ }
        }

        public ForwardPeek? PeekForward()
        {
            AssertUiThread();
            EnsureBootstrapped();

            var tab = _tabs[_activeTab!.Value];
            if (!tab.CanGoForward)
                return null;

            var next = tab.PeekForward();
            return new ForwardPeek(next.ViewModel.GetType(), next.Parameters);
        }

        private void EnsureBootstrapped()
        {
            if (!_bootstrapped)
                throw new InvalidOperationException(
                    "Navigation service has not been bootstrapped. Call SetInitialTabAsync first.");
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

            public TabHistory(Frame root)
            {
                _frames.Add(root);
                _currentIndex = 0;
            }

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
