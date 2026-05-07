using System;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using MVVM_Base.Services.Theming.Contracts;

namespace MVVM_Base.Services.Theming.Internal
{
    /// <summary>
    /// Default <see cref="ITheming"/> implementation. Manages a single "theme slot"
    /// in <see cref="Application.Resources"/>'s merged dictionaries — replacing it
    /// on each <see cref="Apply"/> call. Tokens.xaml and component dictionaries are
    /// untouched; only the theme dictionary is swapped.
    /// </summary>
    internal sealed class ThemingService : ITheming
    {
        // Source URIs must match the actual file locations under Themes/.
        // Pack URIs are used so the dictionaries resolve identically whether the
        // app is running loose or as a single packaged executable.
        private static readonly Uri LightUri =
            new("pack://application:,,,/Themes/Light.xaml", UriKind.Absolute);

        private static readonly Uri DarkUri =
            new("pack://application:,,,/Themes/Dark.xaml", UriKind.Absolute);

        private ResourceDictionary? _activeThemeDictionary;

        public Theme Current { get; private set; }

        public event EventHandler<Theme>? Changed;

        public void Apply(Theme theme)
        {
            Debug.Assert(
                Application.Current.Dispatcher.CheckAccess(),
                "ITheming.Apply must be called on the UI thread.");

            if (_activeThemeDictionary is not null && Current == theme)
                return;

            var next = new ResourceDictionary { Source = UriFor(theme) };
            var merged = Application.Current.Resources.MergedDictionaries;

            if (_activeThemeDictionary is not null)
            {
                var index = merged.IndexOf(_activeThemeDictionary);
                if (index >= 0)
                    merged[index] = next;
                else
                    merged.Add(next);  // active reference lost — append defensively
            }
            else
            {
                merged.Add(next);
            }

            _activeThemeDictionary = next;
            Current = theme;

            Changed?.Invoke(this, theme);
        }

        private static Uri UriFor(Theme theme) => theme switch
        {
            Theme.Light => LightUri,
            Theme.Dark => DarkUri,
            _ => throw new ArgumentOutOfRangeException(nameof(theme), theme, "Unknown theme."),
        };
    }
}
