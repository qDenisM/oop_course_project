using System;
using System.Globalization;
using System.Windows.Data;
using Web_Accounting_Exams.Models;

namespace Web_Accounting_Exams.Converters
{
    public class ApplicationStatusConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is ApplicationStatus status)
            {
                return status switch
                {
                    ApplicationStatus.Pending => "Ожидает",
                    ApplicationStatus.Approved => "Одобрена",
                    ApplicationStatus.Rejected => "Отклонена",
                    ApplicationStatus.Completed => "Завершена",
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

