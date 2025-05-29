using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace arasakagram.Helpers
{
    public class SectionToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null) return Visibility.Collapsed;
            var selected = value.ToString();
            var param = parameter.ToString();
            return selected == param ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
} 