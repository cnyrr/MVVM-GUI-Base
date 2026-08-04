using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Wpf.Shell.Services.Navigation.Contracts;
using Wpf.Shell.Services.Toasts.Contracts;

namespace Wpf.Shell.ViewModels.TestToasts
{
    /// <summary>
    /// Test harness for the toast subsystem. Exposes one button per severity, plus
    /// a "fire N at once" control to exercise the host's queue and cap behavior.
    /// </summary>
    public sealed partial class TestToastsRootViewModel : ViewModelBase, IRootViewModel
    {
        public string TabLabel => "Toast Tests";

        private readonly IToastService _toast;

        public TestToastsRootViewModel(IToastService toast, ILogger<TestToastsRootViewModel> logger) : base(logger)
        {
            _toast = toast;
        }

        /// <summary>
        /// How many toasts a "Fire N" press will queue at once. Defaults to a value
        /// above the visible cap so queue behavior is exercised by default.
        /// </summary>
        [ObservableProperty]
        private int _fireCount = 5;

        [RelayCommand]
        private void ShowInfo() =>
            _toast.Info("Info toast — neutral severity, normal duration.");

        [RelayCommand]
        private void ShowSuccess() =>
            _toast.Success("Success toast — accent green, normal duration.");

        [RelayCommand]
        private void ShowWarning() =>
            _toast.Warning("Warning toast — accent amber, normal duration.");

        [RelayCommand]
        private void ShowError() =>
            _toast.Error("Error toast — sticky, must be dismissed via X.");

        [RelayCommand]
        private void ShowShort() =>
            _toast.Info("Short duration toast — ~2s before auto-dismiss.",
                ToastDuration.Short);

        [RelayCommand]
        private void ShowLong() =>
            _toast.Info("Long duration toast — ~6s before auto-dismiss.",
                ToastDuration.Long);

        [RelayCommand]
        private void FireBatch()
        {
            var n = FireCount < 1 ? 1 : FireCount;
            for (var i = 1; i <= n; i++)
            {
                _toast.Info($"Batch toast {i} of {n} — exercises queue when n > visible cap (3).");
            }
        }
    }
}
