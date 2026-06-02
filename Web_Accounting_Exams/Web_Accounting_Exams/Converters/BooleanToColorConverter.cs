using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using Web_Accounting_Exams.Services;

namespace Web_Accounting_Exams.Converters
{
    public class BooleanToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is MatchedSpeciality match)
            {
                if (match.AllRequirementsMet)
                {
                    return new SolidColorBrush(Color.FromRgb(34, 197, 94));
                }
                else if (match.MatchedSubjectsCount > 0)
                {
                    return new SolidColorBrush(Color.FromRgb(234, 179, 8));
                }
                else
                {
                    return new SolidColorBrush(Color.FromRgb(239, 68, 68));
                }
            }
            
            if (value is bool isMet)
            {
                return isMet 
                    ? new SolidColorBrush(Color.FromRgb(34, 197, 94))
                    : new SolidColorBrush(Color.FromRgb(234, 179, 8));
            }
            return Brushes.Gray;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

