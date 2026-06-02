using System.Windows;
using Web_Accounting_Exams.Models;

namespace Web_Accounting_Exams.Views
{
    public partial class ExamEditDialog : Window
    {
        private readonly Exam? _exam;
        public Exam? ResultExam { get; private set; }

        public ExamEditDialog(Exam? exam = null)
        {
            InitializeComponent();
            _exam = exam;

            if (_exam != null)
            {
                NameTextBox.Text = _exam.Name;
                SubjectTextBox.Text = _exam.Subject;
                DescriptionTextBox.Text = _exam.Description ?? string.Empty;
                DurationTextBox.Text = _exam.DurationMinutes.ToString();
                MaxScoreTextBox.Text = _exam.MaxScore.ToString();
                IsActiveCheckBox.IsChecked = _exam.IsActive;
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(NameTextBox.Text) || string.IsNullOrWhiteSpace(SubjectTextBox.Text))
            {
                MessageBox.Show("Пожалуйста, заполните все обязательные поля.", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!int.TryParse(DurationTextBox.Text, out int duration) || duration <= 0)
            {
                MessageBox.Show("Длительность должна быть положительным числом.", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!int.TryParse(MaxScoreTextBox.Text, out int maxScore) || maxScore <= 0)
            {
                MessageBox.Show("Максимальный балл должен быть положительным числом.", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            ResultExam = _exam ?? new Exam();
            ResultExam.Name = NameTextBox.Text;
            ResultExam.Subject = SubjectTextBox.Text;
            ResultExam.Description = DescriptionTextBox.Text;
            ResultExam.DurationMinutes = duration;
            ResultExam.MaxScore = maxScore;
            ResultExam.IsActive = IsActiveCheckBox.IsChecked ?? true;

            if (_exam == null)
            {
                ResultExam.CreatedAt = DateTime.Now;
            }

            DialogResult = true;
            Close();
        }
    }
}

