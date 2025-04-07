using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;


namespace RTL.Converters
{

    public class UShortToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is ushort testStatus)
            {
                return testStatus switch
                {
                    1 => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1562FE")), // еще не проводили голубой 
                    2 => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#B5E61D")), // Зелёный
                    3 => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FB0574")), // Розовый
                    _ => Brushes.Black
                };
            }
            return Brushes.Black;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}