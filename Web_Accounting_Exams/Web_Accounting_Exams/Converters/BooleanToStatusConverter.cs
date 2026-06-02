using System.Globalization;
using System.Windows.Data;
using Web_Accounting_Exams.Services;

namespace Web_Accounting_Exams.Converters
{
    public class BooleanToStatusConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is MatchedSpeciality match)
            {
                if (match.AllRequirementsMet)
                {
                    return "✓ Подходит";
                }
                else if (match.MatchedSubjectsCount > 0)
                {
                    return $"⚠ Частично ({match.MatchedSubjectsCount}/{match.RequiredSubjectsCount})";
                }
                else
                {
                    return "✗ Не подходит";
                }
            }
            
            if (value is bool isMet)
            {
                return isMet ? "✓ Подходит" : "⚠ Частично";
            }
            return "—";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

