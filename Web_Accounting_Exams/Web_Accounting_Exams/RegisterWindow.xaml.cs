using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Web_Accounting_Exams.ViewModels;
using Web_Accounting_Exams.Models;
using Web_Accounting_Exams.Helpers;

namespace Web_Accounting_Exams
{
    public partial class RegisterWindow : Window
    {
        private readonly RegisterViewModel _viewModel;

        public RegisterWindow()
        {
            InitializeComponent();
            
            var context = new Data.ApplicationDbContext();
            var authService = new Services.AuthenticationService(context);
            _viewModel = new ViewModels.RegisterViewModel(authService);
            DataContext = _viewModel;
            
            FirstNameTextBox.PreviewTextInput += InputValidationHelper.LettersOnly_PreviewTextInput;
            LastNameTextBox.PreviewTextInput += InputValidationHelper.LettersOnly_PreviewTextInput;
            EmailTextBox.LostFocus += InputValidationHelper.Email_LostFocus;
            PhoneTextBox.PreviewTextInput += InputValidationHelper.Phone_PreviewTextInput;
            PhoneTextBox.LostFocus += InputValidationHelper.Phone_LostFocus;
            
            FirstNameTextBox.Focus();
        }

        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (sender is PasswordBox passwordBox)
            {
                _viewModel.Password = passwordBox.Password;
            }
        }

        private void ConfirmPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (sender is PasswordBox passwordBox)
            {
                _viewModel.ConfirmPassword = passwordBox.Password;
            }
        }

        private void Register_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.RegisterCommand.Execute(null);

        }

        private bool ValidateForm()
        {
            if (string.IsNullOrWhiteSpace(FirstNameTextBox.Text))
            {
                MessageBox.Show("Пожалуйста, введите ваше имя.", "Ошибка валидации",
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                FirstNameTextBox.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(LastNameTextBox.Text))
            {
                MessageBox.Show("Пожалуйста, введите вашу фамилию.", "Ошибка валидации",
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                LastNameTextBox.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(EmailTextBox.Text))
            {
                MessageBox.Show("Пожалуйста, введите email адрес.", "Ошибка валидации",
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                EmailTextBox.Focus();
                return false;
            }

            if (!IsValidEmail(EmailTextBox.Text))
            {
                MessageBox.Show("Пожалуйста, введите корректный email адрес.", "Ошибка валидации",
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                EmailTextBox.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(PhoneTextBox.Text))
            {
                MessageBox.Show("Пожалуйста, введите номер телефона.", "Ошибка валидации",
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                PhoneTextBox.Focus();
                return false;
            }

            if (!Helpers.InputValidationHelper.IsValidPhoneNumber(PhoneTextBox.Text))
            {
                MessageBox.Show("Пожалуйста, введите номер телефона в формате +375000000000 (12 цифр после +375).", "Ошибка валидации",
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                PhoneTextBox.Focus();
                return false;
            }

            if (!BirthDatePicker.SelectedDate.HasValue)
            {
                MessageBox.Show("Пожалуйста, выберите дату рождения.", "Ошибка валидации",
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                BirthDatePicker.Focus();
                return false;
            }

            var age = DateTime.Now.Year - BirthDatePicker.SelectedDate.Value.Year;
            if (age < 14)
            {
                MessageBox.Show("Возраст должен быть не менее 14 лет.", "Ошибка валидации",
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                BirthDatePicker.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(PasswordBox.Password))
            {
                MessageBox.Show("Пожалуйста, введите пароль.", "Ошибка валидации",
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                PasswordBox.Focus();
                return false;
            }

            if (PasswordBox.Password.Length < 6)
            {
                MessageBox.Show("Пароль должен содержать не менее 6 символов.", "Ошибка валидации",
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                PasswordBox.Focus();
                return false;
            }

            if (PasswordBox.Password != ConfirmPasswordBox.Password)
            {
                MessageBox.Show("Пароли не совпадают.", "Ошибка валидации",
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                ConfirmPasswordBox.Focus();
                return false;
            }

            if (!AgreementCheckBox.IsChecked.GetValueOrDefault())
            {
                MessageBox.Show("Для продолжения необходимо согласиться с пользовательским соглашением и политикой конфиденциальности.",
                              "Ошибка валидации", MessageBoxButton.OK, MessageBoxImage.Warning);
                AgreementCheckBox.Focus();
                return false;
            }

            return true;
        }

        private bool IsValidEmail(string email)
        {
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

        private void Login_Click(object sender, MouseButtonEventArgs e)
        {
            LoginWindow loginWindow = new LoginWindow();
            loginWindow.Show();
            this.Close();
        }

        private void Agreement_Click(object sender, MouseButtonEventArgs e)
        {
            MessageBox.Show("Здесь будет отображаться пользовательское соглашение.",
                          "Пользовательское соглашение", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void Privacy_Click(object sender, MouseButtonEventArgs e)
        {
            MessageBox.Show("Здесь будет отображаться политика конфиденциальности.",
                          "Политика конфиденциальности", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void GoogleRegister_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Регистрация через Google будет реализована в следующих версиях.",
                          "Google OAuth", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void FacebookRegister_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Регистрация через Facebook будет реализована в следующих версиях.",
                          "Facebook OAuth", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            MainWindow mainWindow = new MainWindow();
            mainWindow.Show();
            this.Close();
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.Enter && AgreementCheckBox.IsChecked.GetValueOrDefault())
            {
                Register_Click(this, new RoutedEventArgs());
            }
            base.OnKeyDown(e);
        }
    }
}