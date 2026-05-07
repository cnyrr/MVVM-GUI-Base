using MVVM_Base.ViewModels;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace MVVM_Base.Views
{
    /// <summary>
    /// Interaction logic for ShellView.xaml
    /// </summary>
    public partial class ShellView : UserControl
    {
        private ShellViewModel? _vm;

        public ShellView()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;
            Unloaded += OnUnloaded;
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (_vm is not null)
                _vm.PropertyChanged -= OnVmPropertyChanged;

            _vm = e.NewValue as ShellViewModel;

            if (_vm is not null)
                _vm.PropertyChanged += OnVmPropertyChanged;
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            if (_vm is not null)
            {
                _vm.PropertyChanged -= OnVmPropertyChanged;
                _vm = null;
            }
        }

        private void OnVmPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(ShellViewModel.IsSidebarExpanded))
                return;

            if (_vm is null || !_vm.IsSidebarExpanded)
                return;

            // Sidebar just opened: move focus into it so keyboard users can navigate
            // the tab list. Defer to the dispatcher so the panel is realized + visible
            // before MoveFocus runs.
            Dispatcher.BeginInvoke(
                new Action(() =>
                    SidebarPanel.MoveFocus(new TraversalRequest(FocusNavigationDirection.First))),
                DispatcherPriority.Input);
        }
    }
}
