using System.Globalization;
using System.Windows.Data;
using Web_Accounting_Exams.Models;

namespace Web_Accounting_Exams.Converters
{
    public class CertificateTypeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is CertificateType type)
            {
                return type == CertificateType.CT ? "ЦТ" : "Аттестат";
            }
            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string str)
            {
                return str == "ЦТ" ? CertificateType.CT : CertificateType.Diploma;
            }
            return CertificateType.CT;
        }
    }
}

