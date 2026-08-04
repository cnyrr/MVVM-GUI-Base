using System;
using System.Collections.Generic;
using System.Text;

namespace Wpf.Shell.Services.Theming.Contracts
{
    /// <summary>
    /// Application theming service. Owns the currently active <see cref="Theme"/>
    /// and swaps the application's theme resource dictionary on demand. All themed
    /// brushes/styles must bind via <c>{DynamicResource}</c> so swaps repaint
    /// without rebuilding visuals.
    /// </summary>
    public interface ITheming
    {
        /// <summary>The currently active theme.</summary>
        Theme Current { get; }

        /// <summary>
        /// Applies <paramref name="theme"/> as the active theme. No-op if it is
        /// already current. Must be called on the UI thread.
        /// </summary>
        void Apply(Theme theme);

        /// <summary>
        /// Raised after a theme swap completes, on the UI thread. The argument is
        /// the new <see cref="Current"/> value.
        /// </summary>
        event EventHandler<Theme>? Changed;
    }
}
