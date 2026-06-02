using System.Globalization;
using System.Linq;
using System.Windows.Data;
using Web_Accounting_Exams.Services;

namespace Web_Accounting_Exams.Converters
{
    public class MatchedSubjectsConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is MatchedSpeciality match)
            {
                var parts = new List<string>();

                if (match.MatchedSubjects.Any())
                {
                    parts.Add($"✓ {string.Join(", ", match.MatchedSubjects)}");
                }

                if (match.MissingSubjects.Any())
                {
                    parts.Add($"✗ Отсутствуют: {string.Join(", ", match.MissingSubjects)}");
                }

                if (!parts.Any())
                {
                    return "Нет обязательных предметов";
                }

                return string.Join("\n", parts);
            }
            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

