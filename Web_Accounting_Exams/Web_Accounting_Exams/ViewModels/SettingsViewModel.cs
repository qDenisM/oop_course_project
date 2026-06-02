using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Windows;
using Web_Accounting_Exams.Data;
using Web_Accounting_Exams.Models;
using Web_Accounting_Exams.Services;

namespace Web_Accounting_Exams.ViewModels
{
    public partial class SettingsViewModel : BaseViewModel
    {
        private readonly ApplicationDbContext _context;
        private readonly AuthenticationService _authService;

        [ObservableProperty]
        private Models.User? _currentUser;

        [ObservableProperty]
        private string _currentPassword = string.Empty;

        [ObservableProperty]
        private string _newPassword = string.Empty;

        [ObservableProperty]
        private string _confirmPassword = string.Empty;

        [ObservableProperty]
        private string _email = string.Empty;

        [ObservableProperty]
        private string _phone = string.Empty;

        [ObservableProperty]
        private bool _isLoading = false;

        public SettingsViewModel(ApplicationDbContext context, AuthenticationService authService)
        {
            _context = context;
            _authService = authService;
            CurrentUser = _authService.CurrentUser;
            LoadUserData();
        }

        private void LoadUserData()
        {
            if (CurrentUser != null)
            {
                Email = CurrentUser.Email;
                Phone = CurrentUser.Phone ?? string.Empty;
            }
        }

        [RelayCommand]
        private async Task UpdatePasswordAsync()
        {
            if (CurrentUser == null) return;

            if (string.IsNullOrWhiteSpace(CurrentPassword) || string.IsNullOrWhiteSpace(NewPassword))
            {
                MessageBox.Show("Пожалуйста, заполните все поля.", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (NewPassword.Length < 6)
            {
                MessageBox.Show("Новый пароль должен содержать не менее 6 символов.", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (NewPassword != ConfirmPassword)
            {
                MessageBox.Show("Пароли не совпадают.", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var passwordHash = HashPassword(CurrentPassword);
            if (CurrentUser.PasswordHash != passwordHash)
            {
                MessageBox.Show("Неверный текущий пароль.", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            IsLoading = true;

            try
            {
                var user = await _context.Users.FindAsync(CurrentUser.Id);
                if (user == null)
                {
                    MessageBox.Show("Пользователь не найден.", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                user.PasswordHash = HashPassword(NewPassword);
                await _context.SaveChangesAsync();

                MessageBox.Show("Пароль успешно изменен. Вы будете перенаправлены на страницу входа.", "Успех",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                _authService.Logout();

                var dashboardWindow = System.Windows.Application.Current.Windows
                    .OfType<Web_Accounting_Exams.DashboardWindow>()
                    .FirstOrDefault();

                if (dashboardWindow != null)
                {
                    var mainWindow = new Web_Accounting_Exams.MainWindow();
                    mainWindow.Show();
                    dashboardWindow.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при изменении пароля: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task UpdateEmailAsync()
        {
            if (CurrentUser == null) return;

            if (string.IsNullOrWhiteSpace(Email) || !IsValidEmail(Email))
            {
                MessageBox.Show("Пожалуйста, введите корректный email адрес.", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var emailExists = await _context.Users
                .AnyAsync(u => u.Email == Email && u.Id != CurrentUser.Id);

            if (emailExists)
            {
                MessageBox.Show("Этот email уже используется другим пользователем.", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            IsLoading = true;

            try
            {
                var user = await _context.Users.FindAsync(CurrentUser.Id);
                if (user == null)
                {
                    MessageBox.Show("Пользователь не найден.", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                user.Email = Email;
                await _context.SaveChangesAsync();

                _authService.SetCurrentUser(user);

                MessageBox.Show("Email успешно изменен.", "Успех",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при изменении email: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task UpdatePhoneAsync()
        {
            if (CurrentUser == null) return;

            if (!string.IsNullOrWhiteSpace(Phone) && !Helpers.InputValidationHelper.IsValidPhoneNumber(Phone))
            {
                MessageBox.Show("Пожалуйста, введите номер телефона в формате +375000000000 (12 цифр после +375).", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            IsLoading = true;

            try
            {
                var user = await _context.Users.FindAsync(CurrentUser.Id);
                if (user == null)
                {
                    MessageBox.Show("Пользователь не найден.", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                user.Phone = Phone;
                await _context.SaveChangesAsync();

                _authService.SetCurrentUser(user);

                MessageBox.Show("Телефон успешно изменен.", "Успех",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при изменении телефона: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private string HashPassword(string password)
        {
            return Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(password));
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

