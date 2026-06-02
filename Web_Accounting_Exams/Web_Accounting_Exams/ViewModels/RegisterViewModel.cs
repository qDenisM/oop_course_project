using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Windows;
using Web_Accounting_Exams.Models;
using Web_Accounting_Exams.Services;

namespace Web_Accounting_Exams.ViewModels
{
    public partial class RegisterViewModel : BaseViewModel
    {
        private readonly AuthenticationService _authService;

        [ObservableProperty]
        private string _firstName = string.Empty;

        [ObservableProperty]
        private string _lastName = string.Empty;

        [ObservableProperty]
        private string _email = string.Empty;

        [ObservableProperty]
        private string _phone = string.Empty;

        [ObservableProperty]
        private DateTime? _birthDate;

        [ObservableProperty]
        private string _password = string.Empty;

        [ObservableProperty]
        private string _confirmPassword = string.Empty;

        [ObservableProperty]
        private bool _agreementAccepted = false;

        [ObservableProperty]
        private bool _isLoading = false;

        public RegisterViewModel(AuthenticationService authService)
        {
            _authService = authService;
        }

        [RelayCommand]
        private async Task RegisterAsync()
        {
            if (!ValidateForm())
                return;

            IsLoading = true;

            try
            {
                var newUser = new Models.User
                {
                    Email = Email,
                    PasswordHash = Password,
                    FirstName = FirstName,
                    LastName = LastName,
                    Phone = Phone,
                    BirthDate = BirthDate,
                    Role = Models.UserRole.Applicant
                };

                var success = await _authService.RegisterAsync(newUser);

                if (success)
                {
                    MessageBox.Show($"Регистрация прошла успешно!\n\nДобро пожаловать, {newUser.FullName}!",
                        "Успешная регистрация", MessageBoxButton.OK, MessageBoxImage.Information);

                    var dashboardWindow = new DashboardWindow(_authService);
                    dashboardWindow.Show();

                    Application.Current.Windows.OfType<RegisterWindow>().FirstOrDefault()?.Close();
                }
                else
                {
                    MessageBox.Show("Пользователь с таким email уже существует.", "Ошибка регистрации",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при регистрации: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private void NavigateToLogin()
        {
            var loginWindow = new LoginWindow();
            loginWindow.Show();
            Application.Current.Windows.OfType<RegisterWindow>().FirstOrDefault()?.Close();
        }

        private bool ValidateForm()
        {
            if (string.IsNullOrWhiteSpace(FirstName))
            {
                MessageBox.Show("Пожалуйста, введите ваше имя.", "Ошибка валидации",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (string.IsNullOrWhiteSpace(LastName))
            {
                MessageBox.Show("Пожалуйста, введите вашу фамилию.", "Ошибка валидации",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (string.IsNullOrWhiteSpace(Email) || !IsValidEmail(Email))
            {
                MessageBox.Show("Пожалуйста, введите корректный email адрес.", "Ошибка валидации",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (string.IsNullOrWhiteSpace(Phone))
            {
                MessageBox.Show("Пожалуйста, введите номер телефона.", "Ошибка валидации",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (!Helpers.InputValidationHelper.IsValidPhoneNumber(Phone))
            {
                MessageBox.Show("Пожалуйста, введите номер телефона в формате +375000000000 (12 цифр после +375).", "Ошибка валидации",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (!BirthDate.HasValue)
            {
                MessageBox.Show("Пожалуйста, выберите дату рождения.", "Ошибка валидации",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            var age = DateTime.Now.Year - BirthDate.Value.Year;
            if (age < 14)
            {
                MessageBox.Show("Возраст должен быть не менее 14 лет.", "Ошибка валидации",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (string.IsNullOrWhiteSpace(Password) || Password.Length < 6)
            {
                MessageBox.Show("Пароль должен содержать не менее 6 символов.", "Ошибка валидации",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (Password != ConfirmPassword)
            {
                MessageBox.Show("Пароли не совпадают.", "Ошибка валидации",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (!AgreementAccepted)
            {
                MessageBox.Show("Для продолжения необходимо согласиться с пользовательским соглашением.",
                    "Ошибка валидации", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            return true;
        }

        private bool IsValidEmail(string email)
        {
            try
            {
                var regex = new System.Text.RegularExpressions.Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");
                return regex.IsMatch(email);
            }
            catch
            {
                return false;
            }
        }
    }
}

