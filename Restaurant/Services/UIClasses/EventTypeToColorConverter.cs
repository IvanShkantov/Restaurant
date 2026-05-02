using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;

namespace Restaurant.Activity
{
    public class EventTypeToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var eventType = value as string;

            switch (eventType)
            {
                case "Добавление":
                    return new SolidColorBrush(Color.FromRgb(21, 87, 36));   
                case "Редактирование":
                    return new SolidColorBrush(Color.FromRgb(133, 100, 4)); 
                case "Удаление":
                    return new SolidColorBrush(Color.FromRgb(114, 28, 36));  
                case "Изменение статуса":
                    return new SolidColorBrush(Color.FromRgb(0, 70, 128));   
                default:
                    return Brushes.Black;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
