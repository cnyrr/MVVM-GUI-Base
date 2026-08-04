using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace Wpf.Shell.Converters
{
    /// <summary>True → accent brush (selected), false → transparent. For active-state highlighting.</summary>
    public sealed class BoolToAccentBrushConverter : IValueConverter
    {
        public object Convert(object value, Type t, object p, CultureInfo c)
            => value is true
                ? Application.Current.TryFindResource("Brush.State.Selected") ?? Brushes.Transparent
                : Brushes.Transparent;

        public object ConvertBack(object value, Type t, object p, CultureInfo c)
            => throw new NotSupportedException();
    }
}