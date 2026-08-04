#if DEBUG
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace Wpf.Shell.Diagnostics
{
    /// <summary>
    /// DEBUG-only resolution / aspect-ratio test harness. Attaches to a window and drops it out
    /// of its maximized kiosk state so the shell can be inspected at a curated set of effective
    /// DIP extents.
    ///
    /// WPF lays out against device-independent pixels, not raw resolution: a 1920x1080 @100%
    /// monitor and a 3840x2160 @200% monitor both present 1920x1080 DIPs and lay out identically.
    /// The matrix below is therefore expressed in DIP extents -- the only quantity that actually
    /// drives Grid star-sizing, wrapping, and the MaxWidth clamps in Sizing.xaml.
    ///
    /// Hotkeys (while the window is focused):
    ///   F2  -- cycle to the next test size
    ///   F3  -- restore the kiosk maximized state
    ///   F4  -- render the current view at every size and save PNGs to
    ///          &lt;bin&gt;/Debug-Screenshots/&lt;ViewName&gt;/{W}x{H}.png, then open the folder
    /// </summary>
    public sealed class ResolutionTester
    {
        // (label, DIP width, DIP height). The mainstream block covers the ratios Sizing.xaml
        // claims to support; the extreme block stress-tests ultrawide, portrait, square, and
        // small-embedded layouts. Portrait / ultrawide sizes exceed a typical dev monitor and
        // render off-screen for F2, but capture correctly with F4.
        private static readonly (string Label, double W, double H)[] Sizes =
        {
            // ----- Mainstream landscape -----
            ("1280x720   16:9",  1280,  720),
            ("1366x768   16:9",  1366,  768),
            ("1920x1080  16:9",  1920, 1080),
            ("2560x1440  16:9",  2560, 1440),
            ("1280x800   16:10", 1280,  800),
            ("1920x1200  16:10", 1920, 1200),
            ("2560x1600  16:10", 2560, 1600),
            ("1280x1024  5:4",   1280, 1024),
            ("1024x768   4:3",   1024,  768),
            ("1600x1200  4:3",   1600, 1200),

            // ----- Ultrawide -----
            ("2560x1080  21:9",  2560, 1080),
            ("3440x1440  21:9",  3440, 1440),
            ("3840x1080  32:9",  3840, 1080),
            ("5120x1440  32:9",  5120, 1440),

            // ----- Portrait (rotated / vertical kiosk) -----
            ("720x1280   9:16",   720, 1280),
            ("1080x1920  9:16",  1080, 1920),
            ("1440x2560  9:16",  1440, 2560),
            ("1200x1920  10:16", 1200, 1920),

            // ----- Square & small-embedded -----
            ("1080x1080  1:1",   1080, 1080),
            ("800x480    5:3",    800,  480),
        };

        private readonly Window _window;
        private readonly TextBlock _readout;
        private int _index = -1;
        private bool _capturing;

        private ResolutionTester(Window window)
        {
            _window = window;

            _readout = new TextBlock
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Bottom,
                Margin = new Thickness(0, 0, 0, 8),
                Padding = new Thickness(10, 4, 10, 4),
                Background = new SolidColorBrush(Color.FromArgb(0xC0, 0, 0, 0)),
                Foreground = Brushes.White,
                FontFamily = new FontFamily("Consolas"),
                FontSize = 12,
                IsHitTestVisible = false,
                Visibility = Visibility.Collapsed,
            };

            InjectReadout();

            _window.PreviewKeyDown += OnKeyDown;
            _window.SizeChanged += (_, _) => UpdateReadout();
        }

        /// <summary>Attaches a tester to <paramref name="window"/>. No-op outside DEBUG builds.</summary>
        public static void Attach(Window window) => new ResolutionTester(window);

        private async void OnKeyDown(object? sender, KeyEventArgs e)
        {
            if (_capturing)
                return;

            switch (e.Key)
            {
                case Key.F2:
                    Next();
                    e.Handled = true;
                    break;
                case Key.F3:
                    Restore();
                    e.Handled = true;
                    break;
                case Key.F4:
                    e.Handled = true;
                    await CaptureAllAsync();
                    break;
            }
        }

        private void Next()
        {
            _index = (_index + 1) % Sizes.Length;
            var s = Sizes[_index];

            _window.WindowState = WindowState.Normal;
            _window.Width = s.W;
            _window.Height = s.H;

            var area = SystemParameters.WorkArea;
            _window.Left = area.Left + Math.Max(0, (area.Width - s.W) / 2);
            _window.Top = area.Top + Math.Max(0, (area.Height - s.H) / 2);

            _readout.Visibility = Visibility.Visible;
            UpdateReadout();
        }

        private void Restore()
        {
            _index = -1;
            _readout.Visibility = Visibility.Collapsed;
            _window.WindowState = WindowState.Maximized;
        }

        // ===== Screenshot capture (F4) =====

        private async Task CaptureAllAsync()
        {
            // The shell's main content host is the only plain ContentControl in the tree; the
            // current view is its first descendant UserControl. Capturing the host yields the
            // view region alone -- overlays (hamburger, scrim, toast, this readout) are siblings
            // of the host, not its children, so they're excluded.
            var host = FindContentHost(_window);
            if (host is null)
                return;

            var view = FindDescendant<UserControl>(host);
            var viewName = view?.GetType().Name ?? "UnknownView";

            var dir = Path.Combine(AppContext.BaseDirectory, "Debug-Screenshots", viewName);
            Directory.CreateDirectory(dir);

            // Snapshot the current window state so it can be put back exactly.
            var prevState = _window.WindowState;
            var prevW = _window.Width;
            var prevH = _window.Height;
            var prevLeft = _window.Left;
            var prevTop = _window.Top;
            var prevReadout = _readout.Visibility;

            _capturing = true;
            _readout.Visibility = Visibility.Collapsed;

            try
            {
                _window.WindowState = WindowState.Normal;

                foreach (var s in Sizes)
                {
                    _window.Width = s.W;
                    _window.Height = s.H;
                    _window.UpdateLayout();

                    // Let measure / arrange / render flush before snapshotting. RenderTargetBitmap
                    // renders from the visual's own data, so this works even if the window is
                    // larger than the screen and partly off it.
                    await _window.Dispatcher.InvokeAsync(() => { }, DispatcherPriority.ContextIdle);

                    CaptureVisual(host, Path.Combine(dir, $"{s.W:0}x{s.H:0}.png"));
                }
            }
            finally
            {
                _window.WindowState = prevState;
                if (prevState == WindowState.Normal)
                {
                    _window.Width = prevW;
                    _window.Height = prevH;
                    _window.Left = prevLeft;
                    _window.Top = prevTop;
                }
                _readout.Visibility = prevReadout;
                _capturing = false;
            }

            try
            {
                Process.Start(new ProcessStartInfo { FileName = dir, UseShellExecute = true });
            }
            catch { /* best effort -- folder still written */ }
        }

        private static void CaptureVisual(FrameworkElement element, string path)
        {
            var w = (int)Math.Ceiling(element.ActualWidth);
            var h = (int)Math.Ceiling(element.ActualHeight);
            if (w <= 0 || h <= 0)
                return;

            // 96 DPI -> 1 DIP maps to 1 pixel, so the file is exactly w x h pixels.
            var rtb = new RenderTargetBitmap(w, h, 96, 96, PixelFormats.Pbgra32);
            rtb.Render(element);

            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(rtb));

            using var fs = File.Create(path);
            encoder.Save(fs);
        }

        // ===== Visual-tree helpers =====

        private static ContentControl? FindContentHost(DependencyObject root)
        {
            // Exact type match: UserControl derives from ContentControl, so an `is` check would
            // wrongly match ShellView. The content host is a plain ContentControl.
            foreach (var d in Descendants(root))
                if (d.GetType() == typeof(ContentControl))
                    return (ContentControl)d;
            return null;
        }

        private static T? FindDescendant<T>(DependencyObject root) where T : DependencyObject
        {
            foreach (var d in Descendants(root))
                if (d is T match)
                    return match;
            return null;
        }

        private static IEnumerable<DependencyObject> Descendants(DependencyObject root)
        {
            var count = VisualTreeHelper.GetChildrenCount(root);
            for (var i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(root, i);
                yield return child;
                foreach (var sub in Descendants(child))
                    yield return sub;
            }
        }

        // ===== Readout =====

        private void UpdateReadout()
        {
            if (_readout.Visibility != Visibility.Visible)
                return;

            var w = _window.ActualWidth;
            var h = _window.ActualHeight;
            var ratio = h > 0 ? w / h : 0;
            var target = _index >= 0 ? Sizes[_index].Label : "\u2014";
            _readout.Text = $"target {target}   actual {w:0}x{h:0} DIP   ratio {ratio:0.000}";
        }

        private void InjectReadout()
        {
            if (_window.Content is UIElement existing)
            {
                _window.Content = null;
                var overlay = new Grid();
                overlay.Children.Add(existing);
                overlay.Children.Add(_readout);
                _window.Content = overlay;
            }
        }
    }
}
#endif