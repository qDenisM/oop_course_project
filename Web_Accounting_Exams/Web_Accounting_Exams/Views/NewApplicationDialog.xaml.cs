using System.Windows;
using Microsoft.EntityFrameworkCore;
using Web_Accounting_Exams.Data;
using Web_Accounting_Exams.Models;

namespace Web_Accounting_Exams.Views
{
    public partial class NewApplicationDialog : Window
    {
        private readonly int _userId;
        public Exam? SelectedExam { get; private set; }
        public ExamSchedule? SelectedSchedule { get; private set; }
        public string? Notes { get; private set; }

        public NewApplicationDialog(ApplicationDbContext? context = null, int userId = 0)
        {
            InitializeComponent();
            _userId = userId;
            LoadExamsAsync();
        }

        private async void LoadExamsAsync()
        {
            try
            {
                using (var context = new ApplicationDbContext())
                {
                    var exams = await context.Exams
                        .Where(e => e.IsActive)
                        .ToListAsync();

                    ExamComboBox.ItemsSource = exams;
                    ExamComboBox.DisplayMemberPath = "Name";
                    ExamComboBox.SelectionChanged += ExamComboBox_SelectionChanged;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки экзаменов: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void ExamComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (ExamComboBox.SelectedItem is Exam exam)
            {
                await LoadSchedulesAsync(exam.Id);
            }
        }

        private async Task LoadSchedulesAsync(int examId)
        {
            try
            {
                using (var context = new ApplicationDbContext())
                {
                    var schedules = await context.ExamSchedules
                        .Where(s => s.ExamId == examId && s.IsActive && s.ExamDate >= DateTime.Today)
                        .OrderBy(s => s.ExamDate)
                        .ToListAsync();

                    ScheduleComboBox.ItemsSource = schedules;
                    ScheduleComboBox.DisplayMemberPath = "DisplayText";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки расписания: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SubmitButton_Click(object sender, RoutedEventArgs e)
        {
            if (ExamComboBox.SelectedItem is not Exam exam)
            {
                MessageBox.Show("Пожалуйста, выберите экзамен.", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (ScheduleComboBox.SelectedItem is not ExamSchedule schedule)
            {
                MessageBox.Show("Пожалуйста, выберите расписание.", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            SelectedExam = exam;
            SelectedSchedule = schedule;
            Notes = NotesTextBox.Text;
            
            this.DialogResult = true;
            Close();
        }
    }
}

