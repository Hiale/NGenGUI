using System;
using System.Windows;
using System.Windows.Data;

namespace Hiale.NgenGui
{
    [ValueConversion(typeof(bool), typeof(Visibility))]
    public class BooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (parameter != null && (bool)parameter)
                value = !(bool)value;
            if ((bool)value)
                return Visibility.Visible;
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var v = (Visibility)value;
            return v != Visibility.Hidden && v != Visibility.Collapsed;
        }
    }
}
