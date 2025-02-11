using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace RTL.Converters
{
    public class BoolToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isConnected)
            {
                // Возвращаем цвет в зависимости от состояния
                return isConnected ? new SolidColorBrush(Color.FromRgb(181, 230, 29)) // Цвет #B5E61D
                                   : new SolidColorBrush(Color.FromRgb(251, 5, 116)); // Цвет #FB0574
            }
            return Brushes.White;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
