using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ProbetaControl.Views
{
    public sealed class InverseBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => !(value is bool b && b);

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => !(value is bool b && b);
    }

    /// <summary>True → Collapsed, false → Visible.</summary>
    public sealed class InverseBoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
            value is bool b && b ? Visibility.Collapsed : Visibility.Visible;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotSupportedException();
    }

    /// <summary>Visible when the value is a non-empty string or any non-null object.</summary>
    public sealed class NotEmptyToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
            value == null || (value is string s && s.Length == 0) ? Visibility.Collapsed : Visibility.Visible;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotSupportedException();
    }
}
