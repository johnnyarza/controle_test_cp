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

    /// <summary>
    /// Responsive helper: Visible when the bound width (e.g. the window's ActualWidth) is at least
    /// ConverterParameter, otherwise Collapsed. Used to drop secondary details on narrow windows.
    /// </summary>
    public sealed class MinWidthToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var width = value is double d ? d : 0;
            var minimum = double.Parse(System.Convert.ToString(parameter, CultureInfo.InvariantCulture), CultureInfo.InvariantCulture);
            return width >= minimum ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotSupportedException();
    }

    /// <summary>Image file bytes (PNG / JPG) → an image for an Image control; null when missing or unreadable.</summary>
    public sealed class BytesToImageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is byte[] bytes) || bytes.Length == 0)
            {
                return null;
            }

            try
            {
                using (var stream = new System.IO.MemoryStream(bytes))
                {
                    var image = new System.Windows.Media.Imaging.BitmapImage();
                    image.BeginInit();
                    image.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                    image.StreamSource = stream;
                    image.EndInit();
                    image.Freeze();
                    return image;
                }
            }
            catch (Exception ex) when (ex is NotSupportedException || ex is System.IO.IOException || ex is ArgumentException
                                       || ex is InvalidOperationException || ex is System.Runtime.InteropServices.COMException)
            {
                return null;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotSupportedException();
    }

    /// <summary>A resource key (e.g. "Icon.User") → that resource, for icons chosen by the view model.</summary>
    public sealed class ResourceKeyConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
            value is string key ? Application.Current?.TryFindResource(key) : null;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotSupportedException();
    }

    /// <summary>
    /// Visible when the value is a non-empty string or any non-null object;
    /// ConverterParameter=Invert gives the opposite (e.g. a hint shown while nothing is selected).
    /// </summary>
    public sealed class NotEmptyToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var hasValue = !(value == null || (value is string s && s.Length == 0));
            if (string.Equals(parameter as string, "Invert", StringComparison.Ordinal))
            {
                hasValue = !hasValue;
            }

            return hasValue ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotSupportedException();
    }
}
