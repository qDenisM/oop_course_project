using System;
using System.Globalization;
using System.Windows.Data;
using Web_Accounting_Exams.Models;

namespace Web_Accounting_Exams.Converters
{
    public class ApplicationTypeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is ApplicationType type)
            {
                return type switch
                {
                    ApplicationType.Budget => "Бюджет",
                    ApplicationType.Paid => "Платно",
                    _ => "Неизвестно"
                };
            }
            return "Неизвестно";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

