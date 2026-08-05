using System.Windows;

namespace Wpf.Shell.Bench
{
    /// <summary>
    /// Application top-level window. Hosts <see cref="Views.ShellView"/>. The
    /// window is intentionally thin — no logic, no code-behind beyond the
    /// generated InitializeComponent call. DataContext is set in App.xaml.cs.
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }
    }
}