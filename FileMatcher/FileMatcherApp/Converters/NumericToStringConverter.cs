using System;
using System.Globalization;
using System.Windows.Data;

namespace FileMatcherApp.Converters
{
    public class NumericToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var sval = (string)value;
            if (targetType == typeof(long))
            {
                long res;
                if (!long.TryParse(sval, out res))
                {
                    res = 0;
                }
                return res;
            }
            if (targetType == typeof(int))
            {
                int res;
                if (!int.TryParse(sval, out res))
                {
                    res = 0;
                }
                return res;
            }
            if (targetType == typeof(double))
            {
                double res;
                if (!double.TryParse(sval, out res))
                {
                    res = 0;
                }
                return res;
            }
            throw new NotSupportedException(string.Format("Target type '{0}' not supported", targetType.Name));
        }
    }
}
