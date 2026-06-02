using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Web_Accounting_Exams
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            this.Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            StartAnimations();
        }

        private void StartAnimations()
        {
            var slideAnimation = (Storyboard)this.Resources["SlideInAnimation"];
            if (slideAnimation != null)
            {
                Storyboard.SetTarget(slideAnimation.Children[0], LeftContent);
                slideAnimation.Begin();
            }

            var fadeAnimation = (Storyboard)this.Resources["FadeInAnimation"];
            if (fadeAnimation != null)
            {
                Storyboard.SetTarget(fadeAnimation.Children[0], RightContent);
                fadeAnimation.Begin();
            }
        }

        private void StartRegistration_Click(object sender, RoutedEventArgs e)
        {
            RegisterWindow registerWindow = new RegisterWindow();
            registerWindow.Show();
            this.Close();
        }

        private void LearnMore_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Здесь будет подробная информация о сервисе.",
                          "Узнать больше", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void Contact_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Контактная информация:\nEmail: info@electronic-report.ru\nТелефон: +7 (800) 123-45-67",
                          "Связаться с нами", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void Login_Click(object sender, RoutedEventArgs e)
        {
            LoginWindow loginWindow = new LoginWindow();
            loginWindow.Show();
            this.Close();
        }

        private void StartNow_Click(object sender, RoutedEventArgs e)
        {
            RegisterWindow registerWindow = new RegisterWindow();
            registerWindow.Show();
            this.Close();
        }
    }
}