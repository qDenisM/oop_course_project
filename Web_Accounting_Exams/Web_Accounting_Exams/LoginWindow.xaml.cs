using System.Windows;
using System.Windows.Input;
using Web_Accounting_Exams.Data;
using Web_Accounting_Exams.Services;
using Web_Accounting_Exams.ViewModels;

namespace Web_Accounting_Exams
{
    public partial class LoginWindow : Window
    {
        private readonly LoginViewModel _viewModel;

        public LoginWindow()
        {
            InitializeComponent();
            
            var context = new ApplicationDbContext();
            var authService = new AuthenticationService(context);
            _viewModel = new LoginViewModel(authService, Application.Current);
            DataContext = _viewModel;
            
            EmailTextBox.Focus();
        }

        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.PasswordBox passwordBox)
            {
                _viewModel.Password = passwordBox.Password;
            }
        }

        private void ForgotPassword_Click(object sender, MouseButtonEventArgs e)
        {
            var forgotPasswordWindow = new Views.ForgotPasswordWindow
            {
                Owner = this
            };
            forgotPasswordWindow.ShowDialog();
        }

        private void GoogleLogin_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Вход через Google будет реализован в следующих версиях.",
                          "Google OAuth", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void FacebookLogin_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Вход через Facebook будет реализован в следующих версиях.",
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
            if (e.Key == Key.Enter)
            {
                _viewModel.LoginCommand.Execute(null);
            }
            base.OnKeyDown(e);
        }
    }
}