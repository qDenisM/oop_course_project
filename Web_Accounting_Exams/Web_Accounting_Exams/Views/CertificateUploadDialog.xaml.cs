using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using Web_Accounting_Exams.Models;
using Web_Accounting_Exams.Helpers;

namespace Web_Accounting_Exams.Views
{
    public partial class CertificateUploadDialog : Window
    {
        public CertificateType CertificateType { get; private set; }
        public string Subject { get; private set; } = string.Empty;
        public int Score { get; private set; }
        public string CertificateNumber { get; private set; } = string.Empty;

        public CertificateUploadDialog(Dictionary<string, string>? ocrData = null)
        {
            InitializeComponent();
            TypeComboBox.SelectedIndex = 0;

            if (ScoreTextBox != null)
            {
                ScoreTextBox.PreviewTextInput += InputValidationHelper.NumericOnly_PreviewTextInput;
            }

            if (ocrData != null && ocrData.Count > 0)
            {
                if (ocrData.ContainsKey("Type"))
                {
                    var type = ocrData["Type"];
                    if (type == "Diploma")
                    {
                        TypeComboBox.SelectedIndex = 1;
                    }
                    else
                    {
                        TypeComboBox.SelectedIndex = 0;
                    }
                }

                if (ocrData.ContainsKey("Score"))
                {
                    ScoreTextBox.Text = ocrData["Score"];
                }
                if (ocrData.ContainsKey("Subject"))
                {
                    SubjectTextBox.Text = ocrData["Subject"];
                }
                if (ocrData.ContainsKey("CertificateNumber"))
                {
                    CertificateNumberTextBox.Text = ocrData["CertificateNumber"];
                }

                if (ocrData.Count > 0)
                {
                    var recognizedFields = new List<string>();
                    if (ocrData.ContainsKey("Type")) recognizedFields.Add("тип");
                    if (ocrData.ContainsKey("Subject")) recognizedFields.Add("предмет");
                    if (ocrData.ContainsKey("Score")) recognizedFields.Add("балл");
                    if (ocrData.ContainsKey("CertificateNumber")) recognizedFields.Add("номер сертификата");
                    
                    var fieldsText = string.Join(", ", recognizedFields);
                    OCRResultTextBlock.Text = $"Распознано: {fieldsText}. Проверьте и при необходимости исправьте.";
                    OCRResultTextBlock.Visibility = Visibility.Visible;
                }
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(SubjectTextBox.Text))
            {
                MessageBox.Show("Пожалуйста, введите предмет.", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!int.TryParse(ScoreTextBox.Text, out int score) || score < 0)
            {
                MessageBox.Show("Пожалуйста, введите корректное количество баллов.", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(CertificateNumberTextBox.Text))
            {
                MessageBox.Show("Пожалуйста, введите номер сертификата. Это обязательное поле.", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (TypeComboBox.SelectedItem is not System.Windows.Controls.ComboBoxItem selectedItem)
            {
                MessageBox.Show("Пожалуйста, выберите тип сертификата.", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            CertificateType = selectedItem.Tag?.ToString() == "CT" ? CertificateType.CT : CertificateType.Diploma;
            Subject = SubjectTextBox.Text;
            Score = score;
            CertificateNumber = CertificateNumberTextBox.Text.Trim();

            DialogResult = true;
            Close();
        }
    }
}

