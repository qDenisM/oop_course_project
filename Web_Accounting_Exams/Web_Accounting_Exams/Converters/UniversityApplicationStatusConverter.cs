using System;
using System.Globalization;
using System.Windows.Data;
using Web_Accounting_Exams.Models;

namespace Web_Accounting_Exams.Converters
{
    public class UniversityApplicationStatusConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is UniversityApplicationStatus status)
            {
                return status switch
                {
                    UniversityApplicationStatus.Pending => "Ожидает",
                    UniversityApplicationStatus.Accepted => "Принята",
                    UniversityApplicationStatus.Rejected => "Отклонена",
                    UniversityApplicationStatus.Waitlisted => "В листе ожидания",
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

