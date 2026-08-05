namespace Wpf.Shell.Services.Theming.Contracts
{
    /// <summary>
    /// Identifies a theme. Each value corresponds to one resource dictionary under
    /// <c>Themes/</c> (e.g., <see cref="Light"/> → <c>Themes/Light.xaml</c>).
    /// New themes are added by extending this enum and providing a matching dictionary.
    /// </summary>
    public enum Theme
    {
        Light,
        Dark,
    }
}
