using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Text.RegularExpressions;
using Web_Accounting_Exams.Models;
using Web_Accounting_Exams.Helpers;

namespace Web_Accounting_Exams.Views
{
    public partial class UniversityEditDialog : Window
    {
        private readonly University? _university;
        public University? ResultUniversity { get; private set; }

        public UniversityEditDialog(University? university = null)
        {
            InitializeComponent();
            _university = university;

            NameTextBox.PreviewTextInput += InputValidationHelper.LettersOnly_PreviewTextInput;
            EmailTextBox.LostFocus += InputValidationHelper.Email_LostFocus;
            PhoneTextBox.PreviewTextInput += InputValidationHelper.Phone_PreviewTextInput;
            PhoneTextBox.LostFocus += InputValidationHelper.Phone_LostFocus;

            if (_university != null)
            {
                NameTextBox.Text = _university.Name;
                AddressTextBox.Text = _university.Address ?? string.Empty;
                PhoneTextBox.Text = _university.Phone ?? string.Empty;
                EmailTextBox.Text = _university.Email ?? string.Empty;
                WebsiteTextBox.Text = _university.Website ?? string.Empty;
                DescriptionTextBox.Text = _university.Description ?? string.Empty;
                IsActiveCheckBox.IsChecked = _university.IsActive;
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (_university == null)
            {
                if (string.IsNullOrWhiteSpace(NameTextBox.Text))
                {
                    MessageBox.Show("Пожалуйста, введите название университета.", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    NameTextBox.Focus();
                    return;
                }

                if (string.IsNullOrWhiteSpace(AddressTextBox.Text))
                {
                    MessageBox.Show("Пожалуйста, введите адрес университета.", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    AddressTextBox.Focus();
                    return;
                }

                if (string.IsNullOrWhiteSpace(PhoneTextBox.Text))
                {
                    MessageBox.Show("Пожалуйста, введите номер телефона университета.", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    PhoneTextBox.Focus();
                    return;
                }

                if (string.IsNullOrWhiteSpace(EmailTextBox.Text))
                {
                    MessageBox.Show("Пожалуйста, введите email адрес университета.", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    EmailTextBox.Focus();
                    return;
                }
            }
            else
            {
                if (string.IsNullOrWhiteSpace(NameTextBox.Text))
                {
                    MessageBox.Show("Пожалуйста, введите название университета.", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    NameTextBox.Focus();
                    return;
                }
            }

            if (!string.IsNullOrWhiteSpace(EmailTextBox.Text) && !InputValidationHelper.IsValidEmail(EmailTextBox.Text))
            {
                MessageBox.Show("Пожалуйста, введите корректный email адрес.", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                EmailTextBox.Focus();
                return;
            }

            if (!string.IsNullOrWhiteSpace(PhoneTextBox.Text) && !InputValidationHelper.IsValidPhoneNumber(PhoneTextBox.Text))
            {
                MessageBox.Show("Пожалуйста, введите номер телефона в формате +375000000000 (12 цифр после +375).", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                PhoneTextBox.Focus();
                return;
            }

            if (!string.IsNullOrWhiteSpace(WebsiteTextBox.Text) && !InputValidationHelper.IsValidUrl(WebsiteTextBox.Text))
            {
                MessageBox.Show("Пожалуйста, введите корректный URL сайта (например, example.com или https://example.com).", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                WebsiteTextBox.Focus();
                return;
            }

            ResultUniversity = _university ?? new University();
            ResultUniversity.Name = NameTextBox.Text;
            ResultUniversity.Address = AddressTextBox.Text;
            ResultUniversity.Phone = PhoneTextBox.Text;
            ResultUniversity.Email = EmailTextBox.Text;
            ResultUniversity.Website = WebsiteTextBox.Text;
            ResultUniversity.Description = DescriptionTextBox.Text;
            ResultUniversity.IsActive = IsActiveCheckBox.IsChecked ?? true;

            if (_university == null)
            {
                ResultUniversity.CreatedAt = DateTime.Now;
            }

            DialogResult = true;
            Close();
        }
    }
}

