using System;
using System.Globalization;
using System.Windows.Data;
using Web_Accounting_Exams.Models;

namespace Web_Accounting_Exams.Converters
{
    public class CertificateStatusConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is CertificateStatus status)
            {
                return status switch
                {
                    CertificateStatus.Pending => "Ожидает",
                    CertificateStatus.Approved => "Принята",
                    CertificateStatus.Rejected => "Отклонена",
                    _ => "Неизвестно"
                };
            }
            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

