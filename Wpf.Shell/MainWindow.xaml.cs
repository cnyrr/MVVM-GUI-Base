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

namespace Wpf.Shell
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