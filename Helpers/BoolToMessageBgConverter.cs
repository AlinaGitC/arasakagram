using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace arasakagram.Helpers
{
    public class BoolToMessageBgConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b && b)
                return Brushes.LightBlue; // Сообщение пользователя
            return Brushes.White; // Сообщение собеседника
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
} 