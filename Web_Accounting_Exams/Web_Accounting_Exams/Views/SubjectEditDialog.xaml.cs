using System.Windows;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Web_Accounting_Exams.Models;
using Web_Accounting_Exams.Data;

namespace Web_Accounting_Exams.Views
{
    public partial class SubjectEditDialog : Window
    {
        private readonly Speciality _speciality;
        private readonly SpecialitySubject? _subject;
        public SpecialitySubject? ResultSubject { get; private set; }

        public SubjectEditDialog(Speciality speciality, SpecialitySubject? subject = null)
        {
            InitializeComponent();
            _speciality = speciality;
            _subject = subject;

            LoadExamsAsync();

            if (_subject != null)
            {
                MinScoreTextBox.Text = _subject.MinScore?.ToString() ?? string.Empty;
                IsRequiredCheckBox.IsChecked = _subject.IsRequired;
            }
        }

        private async void LoadExamsAsync()
        {
            try
            {
                using (var context = new ApplicationDbContext())
                {
                    var exams = await context.Exams
                        .Where(e => e.IsActive)
                        .OrderBy(e => e.Name)
                        .ToListAsync();
                    
                    SubjectComboBox.ItemsSource = exams;
                    SubjectComboBox.DisplayMemberPath = "Name";
                    
                    if (_subject != null)
                    {
                        var exam = exams.FirstOrDefault(e => e.Name == _subject.SubjectName);
                        if (exam != null)
                        {
                            SubjectComboBox.SelectedItem = exam;
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки экзаменов: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (SubjectComboBox.SelectedItem is not Exam selectedExam)
            {
                MessageBox.Show("Пожалуйста, выберите предмет из списка.", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(MinScoreTextBox.Text))
            {
                MessageBox.Show("Пожалуйста, введите минимальный балл.", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                MinScoreTextBox.Focus();
                return;
            }

            if (!int.TryParse(MinScoreTextBox.Text, out int minScore) || minScore < 0 || minScore > 100)
            {
                MessageBox.Show("Пожалуйста, введите корректный минимальный балл (от 0 до 100).", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                MinScoreTextBox.Focus();
                return;
            }

            ResultSubject = _subject ?? new SpecialitySubject();
            ResultSubject.SpecialityId = _speciality.Id;
            ResultSubject.SubjectName = selectedExam.Name;
            ResultSubject.IsRequired = IsRequiredCheckBox.IsChecked ?? true;
            ResultSubject.MinScore = minScore;

            DialogResult = true;
            Close();
        }
    }
}

