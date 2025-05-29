using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace arasakagram.Helpers
{
    public class ChatTypeToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null)
                return Visibility.Collapsed;
            int chatType = System.Convert.ToInt32(value);
            int requiredType = System.Convert.ToInt32(parameter);
            return chatType == requiredType ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
} 