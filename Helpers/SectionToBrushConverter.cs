using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using arasakagram.ViewModels;

namespace arasakagram.Helpers
{
    public class SectionToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null) return Brushes.Transparent;
            var selected = value.ToString();
            var param = parameter.ToString();
            return selected == param ? new SolidColorBrush(Color.FromRgb(227, 240, 255)) : Brushes.Transparent;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
} 