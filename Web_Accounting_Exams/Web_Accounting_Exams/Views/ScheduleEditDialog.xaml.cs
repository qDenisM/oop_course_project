using System;
using System.Linq;
using System.Windows;
using Microsoft.EntityFrameworkCore;
using Web_Accounting_Exams.Data;
using Web_Accounting_Exams.Models;

namespace Web_Accounting_Exams.Views
{
    public partial class ScheduleEditDialog : Window
    {
        private readonly ExamSchedule? _schedule;
        public ExamSchedule? ResultSchedule { get; private set; }

        public ScheduleEditDialog(ApplicationDbContext? context = null, ExamSchedule? schedule = null)
        {
            InitializeComponent();
            _schedule = schedule;
            
            ExamDatePicker.DisplayDateStart = DateTime.Today;
            if (DateTime.Today > DateTime.MinValue)
            {
                ExamDatePicker.BlackoutDates.Add(
                    new System.Windows.Controls.CalendarDateRange(DateTime.MinValue, DateTime.Today.AddDays(-1)));
            }
            
            LoadExamsAsync();
            LoadUniversitiesAsync();

            if (_schedule != null)
            {
                ExamDatePicker.SelectedDate = _schedule.ExamDate;
                TimeTextBox.Text = _schedule.StartTime.ToString(@"hh\:mm");
                MaxParticipantsTextBox.Text = _schedule.MaxParticipants.ToString();
                IsActiveCheckBox.IsChecked = _schedule.IsActive;
            }
            else
            {
                ExamDatePicker.SelectedDate = DateTime.Today;
            }
        }

        private async void LoadExamsAsync()
        {
            try
            {
                using (var context = new ApplicationDbContext())
                {
                    var exams = await context.Exams.Where(e => e.IsActive).ToListAsync();
                    ExamComboBox.ItemsSource = exams;
                    
                    if (_schedule != null)
                    {
                        var exam = exams.FirstOrDefault(e => e.Id == _schedule.ExamId);
                        if (exam != null)
                        {
                            ExamComboBox.SelectedItem = exam;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки экзаменов: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void LoadUniversitiesAsync()
        {
            try
            {
                using (var context = new ApplicationDbContext())
                {
                    var universities = await context.Universities
                        .Where(u => u.IsActive)
                        .ToListAsync();
                    UniversityComboBox.ItemsSource = universities;
                    
                    if (_schedule != null)
                    {
                        var university = universities.FirstOrDefault(u => u.Name == _schedule.Location);
                        if (university != null)
                        {
                            UniversityComboBox.SelectedItem = university;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки университетов: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (ExamComboBox.SelectedItem is not Exam exam)
            {
                MessageBox.Show("Пожалуйста, выберите экзамен.", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!ExamDatePicker.SelectedDate.HasValue)
            {
                MessageBox.Show("Пожалуйста, выберите дату экзамена.", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!TimeSpan.TryParse(TimeTextBox.Text, out TimeSpan startTime))
            {
                MessageBox.Show("Пожалуйста, введите корректное время в формате HH:mm.", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }


            var minTime = new TimeSpan(7, 0, 0);
            var maxTime = new TimeSpan(16, 0, 0);
            if (startTime < minTime || startTime > maxTime)
            {
                MessageBox.Show("Время проведения экзамена должно быть в диапазоне от 7:00 до 16:00.", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (UniversityComboBox.SelectedItem is not University university)
            {
                MessageBox.Show("Пожалуйста, выберите университет.", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!int.TryParse(MaxParticipantsTextBox.Text, out int maxParticipants) || maxParticipants <= 0)
            {
                MessageBox.Show("Максимальное количество участников должно быть положительным числом.", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                MaxParticipantsTextBox.Focus();
                return;
            }

            if (maxParticipants > 1000)
            {
                MessageBox.Show("Максимальное количество участников не может превышать 1000.", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                MaxParticipantsTextBox.Focus();
                return;
            }

            if (_schedule == null)
            {
                using (var context = new ApplicationDbContext())
                {
                    var existingSchedules = context.ExamSchedules
                        .Where(s => s.ExamId == exam.Id && 
                                   s.ExamDate.Date == ExamDatePicker.SelectedDate.Value.Date &&
                                   s.IsActive)
                        .ToList();

                    if (existingSchedules.Any(s => s.Location == university.Name))
                    {
                        MessageBox.Show($"Для экзамена '{exam.Name}' на дату {ExamDatePicker.SelectedDate.Value:dd.MM.yyyy} уже существует расписание в месте проведения '{university.Name}'. Пожалуйста, выберите другое место проведения.", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                        UniversityComboBox.Focus();
                        return;
                    }
                }
            }

            ResultSchedule = _schedule ?? new ExamSchedule();
            ResultSchedule.ExamId = exam.Id;
            ResultSchedule.ExamDate = ExamDatePicker.SelectedDate.Value;
            ResultSchedule.StartTime = startTime;
            ResultSchedule.Location = university.Name;
            ResultSchedule.MaxParticipants = maxParticipants;
            ResultSchedule.IsActive = IsActiveCheckBox.IsChecked ?? true;

            if (_schedule == null)
            {
                ResultSchedule.CurrentParticipants = 0;
                ResultSchedule.CreatedAt = DateTime.Now;
            }

            DialogResult = true;
            Close();
        }
    }
}

