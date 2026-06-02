using System.Windows;
using System.Windows.Media;
using Web_Accounting_Exams.Data;
using Web_Accounting_Exams.Services;
using Web_Accounting_Exams.Helpers;

namespace Web_Accounting_Exams.Views
{
    public partial class ForgotPasswordWindow : Window
    {
        private enum ResetStep
        {
            EmailInput,
            CodeInput,
            NewPasswordInput
        }

        private ResetStep _currentStep = ResetStep.EmailInput;
        private readonly PasswordResetService _passwordResetService;
        private string _userEmail = string.Empty;
        private string _verifiedCode = string.Empty;

        public ForgotPasswordWindow()
        {
            InitializeComponent();
            var context = new ApplicationDbContext();
            var emailService = new EmailService();
            _passwordResetService = new PasswordResetService(context, emailService);
            EmailTextBox.Focus();
        }

        private async void ActionButton_Click(object sender, RoutedEventArgs e)
        {
            switch (_currentStep)
            {
                case ResetStep.EmailInput:
                    await HandleEmailSubmissionAsync();
                    break;
                case ResetStep.CodeInput:
                    await HandleCodeVerificationAsync();
                    break;
                case ResetStep.NewPasswordInput:
                    await HandlePasswordResetAsync();
                    break;
            }
        }

        private async Task HandleEmailSubmissionAsync()
        {
            var email = EmailTextBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(email))
            {
                ShowStatus("Пожалуйста, введите email адрес.", isError: true);
                return;
            }

            if (!InputValidationHelper.IsValidEmail(email))
            {
                ShowStatus("Пожалуйста, введите корректный email адрес.", isError: true);
                EmailTextBox.Focus();
                return;
            }

            ActionButton.IsEnabled = false;
            ActionButton.Content = "Отправка...";
            ShowStatus("Отправка кода на email...", isError: false);

            var success = await _passwordResetService.RequestPasswordResetAsync(email);

            ActionButton.IsEnabled = true;
            ActionButton.Content = "Отправить код";

            if (success)
            {
                _userEmail = email;
                _currentStep = ResetStep.CodeInput;
                ShowStatus("Код отправлен на ваш email. Проверьте почту.", isError: false);
                
                EmailTextBox.IsEnabled = false;
                CodeTextBox.Visibility = Visibility.Visible;
                CodeTextBox.Focus();
                ActionButton.Content = "Подтвердить код";
            }
            else
            {
                var errorMessage = _passwordResetService.LastErrorMessage ?? "Ошибка отправки email. Проверьте настройки SMTP или попробуйте позже.";
                ShowStatus(errorMessage, isError: true);
                EmailTextBox.Focus();
            }
        }

        private async Task HandleCodeVerificationAsync()
        {
            var code = CodeTextBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(code) || code.Length != 6)
            {
                ShowStatus("Пожалуйста, введите 6-значный код.", isError: true);
                CodeTextBox.Focus();
                return;
            }

            var isValid = await _passwordResetService.VerifyResetCodeAsync(_userEmail, code);

            if (isValid)
            {
                _currentStep = ResetStep.NewPasswordInput;
                ShowStatus("Код подтвержден. Введите новый пароль.", isError: false);
                
                _verifiedCode = code;
                
                CodeTextBox.IsEnabled = false;
                NewPasswordBox.Visibility = Visibility.Visible;
                ConfirmPasswordBox.Visibility = Visibility.Visible;
                NewPasswordBox.Focus();
                ActionButton.Content = "Сбросить пароль";
            }
            else
            {
                var errorMessage = _passwordResetService.LastErrorMessage ?? "Неверный или истекший код. Попробуйте еще раз.";
                ShowStatus(errorMessage, isError: true);
                CodeTextBox.Text = string.Empty;
                CodeTextBox.Focus();
            }
        }

        private async Task HandlePasswordResetAsync()
        {
            var newPassword = NewPasswordBox.Password;
            var confirmPassword = ConfirmPasswordBox.Password;

            if (string.IsNullOrWhiteSpace(newPassword))
            {
                ShowStatus("Пожалуйста, введите новый пароль.", isError: true);
                NewPasswordBox.Focus();
                return;
            }

            if (newPassword.Length < 6)
            {
                ShowStatus("Пароль должен содержать не менее 6 символов.", isError: true);
                NewPasswordBox.Focus();
                return;
            }

            if (newPassword != confirmPassword)
            {
                ShowStatus("Пароли не совпадают.", isError: true);
                ConfirmPasswordBox.Focus();
                return;
            }

            ActionButton.IsEnabled = false;
            ActionButton.Content = "Сброс пароля...";

            var success = await _passwordResetService.ResetPasswordAsync(_userEmail, _verifiedCode, newPassword);

            ActionButton.IsEnabled = true;
            ActionButton.Content = "Сбросить пароль";

            if (success)
            {
                MessageBox.Show("Пароль успешно изменен! Теперь вы можете войти с новым паролем.", 
                    "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                DialogResult = true;
                Close();
            }
            else
            {
                var errorMessage = _passwordResetService.LastErrorMessage ?? "Ошибка при сбросе пароля. Код мог истечь. Попробуйте начать заново.";
                ShowStatus(errorMessage, isError: true);
            }
        }

        private void ShowStatus(string message, bool isError)
        {
            StatusTextBlock.Text = message;
            StatusTextBlock.Foreground = new SolidColorBrush(isError ? Color.FromRgb(231, 76, 60) : Color.FromRgb(0, 184, 148));
            StatusTextBlock.Visibility = Visibility.Visible;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}

