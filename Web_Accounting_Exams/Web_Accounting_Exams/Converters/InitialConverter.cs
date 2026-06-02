using System.Globalization;
using System.Windows.Data;

namespace Web_Accounting_Exams.Converters
{
    public class InitialConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string firstName && !string.IsNullOrEmpty(firstName))
            {
                return firstName[0].ToString().ToUpper();
            }
            return "П";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

