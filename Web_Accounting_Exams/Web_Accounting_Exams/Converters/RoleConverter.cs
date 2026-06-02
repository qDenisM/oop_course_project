using System.Globalization;
using System.Windows.Data;

namespace Web_Accounting_Exams.Converters
{
    public class RoleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isAdmin)
            {
                return isAdmin ? "Администратор" : "Абитуриент";
            }
            return "Абитуриент";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string role)
            {
                return role == "Администратор";
            }
            return false;
        }
    }
}

