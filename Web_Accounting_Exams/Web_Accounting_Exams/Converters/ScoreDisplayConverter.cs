using System.Globalization;
using System.Windows.Data;

namespace Web_Accounting_Exams.Converters
{
    public class ScoreDisplayConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value != null)
            {
                var type = value.GetType();
                var scoreProp = type.GetProperty("Score");
                var maxScoreProp = type.GetProperty("MaxScore");
                
                if (scoreProp != null)
                {
                    var score = scoreProp.GetValue(value);
                    var maxScore = maxScoreProp?.GetValue(value);
                    
                    if (maxScore != null && maxScore is int max && max > 0)
                    {
                        return $"{score} / {max}";
                    }
                    return score?.ToString() ?? string.Empty;
                }
            }
            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

