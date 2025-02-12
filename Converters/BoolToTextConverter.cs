using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace RTL.Converters
{
    public class BoolToTextConverter : IValueConverter
    {
        // Метод для преобразования значений
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isConnected)
            {
                return isConnected ? "Отключить" : "Подключить"; // Текст для отображения
            }
            return "Подключить"; // Если значение не булево, возвращаем "Не подключено"
        }

        // Метод для обратного преобразования (не используется в данном случае, так как мы не будем изменять текст обратно в bool)
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return false; // Не требуется
        }
    }
}