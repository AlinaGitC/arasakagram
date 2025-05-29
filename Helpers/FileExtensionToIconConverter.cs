using System;
using System.Globalization;
using System.Windows.Data;

namespace arasakagram.Helpers
{
    public class FileExtensionToIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string fileName)
            {
                var ext = System.IO.Path.GetExtension(fileName).ToLower();
                if (ext == ".jpg" || ext == ".jpeg" || ext == ".png" || ext == ".gif" || ext == ".bmp")
                    return "image";
                if (ext == ".pdf")
                    return "pdf";
                if (ext == ".doc" || ext == ".docx")
                    return "doc";
                if (ext == ".txt")
                    return "txt";
                // Добавьте другие типы по необходимости
            }
            return "file";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
} 