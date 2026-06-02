using System.Text.RegularExpressions;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace Web_Accounting_Exams.Helpers
{
    public static class InputValidationHelper
    {
        private static bool _isShowingEmailMessage = false;
        private static bool _isShowingPhoneMessage = false;

       
        public static bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            try
            {
                var regex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");
                return regex.IsMatch(email);
            }
            catch
            {
                return false;
            }
        }

        public static bool IsValidPhoneNumber(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone))
                return false;

            var regex = new Regex(@"^\+375\d{9}$");
            return regex.IsMatch(phone);
        }

        public static bool IsValidUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return false;

            try
            {
                var urlPattern = @"^(https?:\/\/)?([\da-z\.-]+)\.([a-z\.]{2,6})([\/\w \.-]*)*\/?$";
                var regex = new Regex(urlPattern, RegexOptions.IgnoreCase);
                return regex.IsMatch(url);
            }
            catch
            {
                return false;
            }
        }

        [Obsolete("Используйте IsValidPhoneNumber для валидации")]
        private static string FormatPhoneNumber(string phone, string? previousPhone = null)
        {
            if (string.IsNullOrWhiteSpace(phone))
                return string.Empty;

            var digits = Regex.Replace(phone, @"[^\d+]", "");
            
            bool wasCountryCodeRemoved = false;
            bool hadCountryCodeBefore = false;
            if (!string.IsNullOrEmpty(previousPhone))
            {
                var previousDigits = Regex.Replace(previousPhone, @"[^\d+]", "");
                hadCountryCodeBefore = previousDigits.StartsWith("375") || previousDigits.StartsWith("+375");
                
                if (hadCountryCodeBefore)
                {
                    bool hasCountryCodeNow = digits.StartsWith("375") || digits.StartsWith("+375");
                    if (!hasCountryCodeNow || (previousDigits.Length >= 5 && digits.Length < 5))
                    {
                        wasCountryCodeRemoved = true;
                    }
                }
            }
            
            if (digits.StartsWith("+375"))
            {
                digits = digits.Substring(1);
            }
            else if (digits.StartsWith("375"))
            {
            }
            else if (!wasCountryCodeRemoved && digits.Length > 0 && !hadCountryCodeBefore)
            {
                digits = "375" + digits;
            }
            else if (wasCountryCodeRemoved || digits.Length == 0)
            {
                if (digits.Length == 0)
                    return string.Empty;
                return phone;
            }

            digits = Regex.Replace(digits, @"\D", "");

            if (digits.Length >= 12)
            {
                var code = digits.Substring(0, 3);
                var operatorCode = digits.Substring(3, 2);
                var part1 = digits.Substring(5, 3);
                var part2 = digits.Substring(8, 2);
                var part3 = digits.Substring(10, 2);
                return $"+{code} ({operatorCode}) {part1}-{part2}-{part3}";
            }
            else if (digits.Length >= 10)
            {
                var code = digits.Substring(0, 3);
                var operatorCode = digits.Substring(3, 2);
                var part1 = digits.Substring(5, 3);
                var part2 = digits.Substring(8, Math.Min(2, digits.Length - 8));
                return $"+{code} ({operatorCode}) {part1}-{part2}";
            }
            else if (digits.Length >= 8)
            {
                var code = digits.Substring(0, 3);
                var operatorCode = digits.Substring(3, 2);
                var part1 = digits.Substring(5, 3);
                if (digits.Length >= 9)
                {
                    var part2 = digits.Substring(8, Math.Min(2, digits.Length - 8));
                    return $"+{code} ({operatorCode}) {part1}-{part2}";
                }
                return $"+{code} ({operatorCode}) {part1}";
            }
            else if (digits.Length >= 5)
            {
                var code = digits.Substring(0, 3);
                var operatorCode = digits.Substring(3, Math.Min(2, digits.Length - 3));
                return $"+{code} ({operatorCode}";
            }
            else if (digits.Length >= 3)
            {
                if (digits == "375" || digits.StartsWith("375"))
                {
                    return $"+{digits}";
                }
                else
                {
                    return phone;
                }
            }
            else if (digits.Length > 0)
            {
                return phone;
            }

            return phone;
        }

        public static string FormatSpecialtyCode(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
                return string.Empty;

            var digits = Regex.Replace(code, @"\D", "");

            if (digits.Length == 0)
                return string.Empty;
            if (digits.Length >= 10)
            {
                return $"{digits.Substring(0, 1)}-{digits.Substring(1, 2)}-{digits.Substring(3, 4)}-{digits.Substring(7, 2)}";
            }
            else if (digits.Length >= 7)
            {
                return $"{digits.Substring(0, 1)}-{digits.Substring(1, 2)}-{digits.Substring(3, 4)}-{digits.Substring(7)}";
            }
            else if (digits.Length >= 3)
            {
                return $"{digits.Substring(0, 1)}-{digits.Substring(1, 2)}-{digits.Substring(3)}";
            }
            else if (digits.Length >= 1)
            {
                return $"{digits.Substring(0, 1)}-{digits.Substring(1)}";
            }

            return code;
        }

        public static void NumericOnly_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !char.IsDigit(e.Text, 0);
        }

        public static void LettersOnly_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            var text = e.Text;
            e.Handled = !Regex.IsMatch(text, @"^[\p{L}\s\-']+$", RegexOptions.Compiled);
        }

        public static void Phone_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !(char.IsDigit(e.Text, 0) || e.Text == "+");
        }

        public static void Phone_LostFocus(object sender, System.Windows.RoutedEventArgs e)
        {
            if (sender is TextBox textBox && !_isShowingPhoneMessage)
            {
                if (!string.IsNullOrWhiteSpace(textBox.Text) && !IsValidPhoneNumber(textBox.Text))
                {
                    _isShowingPhoneMessage = true;
                    
                    textBox.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new System.Action(() =>
                    {
                        try
                        {
                            System.Windows.MessageBox.Show("Пожалуйста, введите номер телефона в формате +375000000000 (12 цифр после +375).", "Ошибка валидации",
                                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                            
                            textBox.Dispatcher.BeginInvoke(DispatcherPriority.Input, new System.Action(() =>
                            {
                                textBox.Focus();
                                _isShowingPhoneMessage = false;
                            }));
                        }
                        catch
                        {
                            _isShowingPhoneMessage = false;
                        }
                    }));
                }
            }
        }

        public static void SpecialtyCode_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                var cursorPosition = textBox.CaretIndex;
                var text = textBox.Text;
                var formatted = FormatSpecialtyCode(text);
                
                if (formatted != text)
                {
                    textBox.Text = formatted;
                    textBox.CaretIndex = Math.Min(cursorPosition + (formatted.Length - text.Length), formatted.Length);
                }
            }
        }

        public static void Email_LostFocus(object sender, System.Windows.RoutedEventArgs e)
        {
            if (sender is TextBox textBox && !_isShowingEmailMessage)
            {
                if (!string.IsNullOrWhiteSpace(textBox.Text) && !IsValidEmail(textBox.Text))
                {
                    _isShowingEmailMessage = true;
                    
                    textBox.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new System.Action(() =>
                    {
                        try
                        {
                            System.Windows.MessageBox.Show("Пожалуйста, введите корректный email адрес.", "Ошибка валидации",
                                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                            
                            textBox.Dispatcher.BeginInvoke(DispatcherPriority.Input, new System.Action(() =>
                            {
                                textBox.Focus();
                                _isShowingEmailMessage = false;
                            }));
                        }
                        catch
                        {
                            _isShowingEmailMessage = false;
                        }
                    }));
                }
            }
        }
    }
}

