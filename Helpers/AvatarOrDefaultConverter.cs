using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace arasakagram.Helpers
{
    public class AvatarOrDefaultConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var avatar = value as BitmapImage;
            if (avatar != null)
                return avatar;
            return new BitmapImage(new Uri("pack://application:,,,/Resources/user.png"));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
} 