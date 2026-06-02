using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;
using System.Windows;
using Web_Accounting_Exams.Data;
using Web_Accounting_Exams.Models;

namespace Web_Accounting_Exams.ViewModels
{
    public partial class ScheduleManagementViewModel : BaseViewModel
    {
        private readonly ApplicationDbContext _context;

        [ObservableProperty]
        private ObservableCollection<ExamSchedule> _schedules = new();

        [ObservableProperty]
        private ObservableCollection<Exam> _exams = new();

        [ObservableProperty]
        private ExamSchedule? _selectedSchedule;

        [ObservableProperty]
        private Exam? _selectedExam;

        [ObservableProperty]
        private DateTime _examDate = DateTime.Today;

        [ObservableProperty]
        private TimeSpan _startTime = new TimeSpan(10, 0, 0);

        [ObservableProperty]
        private string _location = string.Empty;

        [ObservableProperty]
        private int _maxParticipants = 50;

        [ObservableProperty]
        private bool _isActive = true;

        [ObservableProperty]
        private bool _isEditMode = false;

        public ScheduleManagementViewModel(ApplicationDbContext context)
        {
            _context = context;
        }

        private async void LoadDataAsync()
        {
            await LoadSchedulesAsync();
            await LoadExamsAsync();
        }

        [RelayCommand]
        public async Task LoadSchedulesAsync()
        {
            try
            {
                List<ExamSchedule> schedules;
                using (var context = new ApplicationDbContext())
                {
                    schedules = await context.ExamSchedules
                        .Include(s => s.Exam)
                        .OrderBy(s => s.ExamDate)
                        .ThenBy(s => s.StartTime)
                        .ToListAsync()
                        .ConfigureAwait(false);
                }
                
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    Schedules.Clear();
                    foreach (var schedule in schedules)
                    {
                        Schedules.Add(schedule);
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка загрузки расписания: {ex.Message}");
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    System.Windows.MessageBox.Show($"Ошибка загрузки расписания: {ex.Message}", "Ошибка",
                        System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                });
            }
        }

        [RelayCommand]
        private async Task LoadExamsAsync()
        {
            var exams = await _context.Exams.Where(e => e.IsActive).ToListAsync();
            Exams = new ObservableCollection<Exam>(exams);
        }

        [RelayCommand]
        private void SelectSchedule(ExamSchedule schedule)
        {
            SelectedSchedule = schedule;
            SelectedExam = schedule.Exam;
            ExamDate = schedule.ExamDate;
            StartTime = schedule.StartTime;
            Location = schedule.Location;
            MaxParticipants = schedule.MaxParticipants;
            IsActive = schedule.IsActive;
            IsEditMode = true;
        }

        [RelayCommand]
        private void ClearSelection()
        {
            SelectedSchedule = null;
            SelectedExam = null;
            ExamDate = DateTime.Today;
            StartTime = new TimeSpan(10, 0, 0);
            Location = string.Empty;
            MaxParticipants = 50;
            IsActive = true;
            IsEditMode = false;
        }

        [RelayCommand]
        private async Task SaveScheduleAsync()
        {
            if (SelectedExam == null || string.IsNullOrWhiteSpace(Location))
            {
                MessageBox.Show("Пожалуйста, заполните все обязательные поля.", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var minTime = new TimeSpan(7, 0, 0);
            var maxTime = new TimeSpan(16, 0, 0);
            if (StartTime < minTime || StartTime > maxTime)
            {
                MessageBox.Show("Время проведения экзамена должно быть в диапазоне от 7:00 до 16:00.", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (MaxParticipants <= 0)
            {
                MessageBox.Show("Максимальное количество участников должно быть положительным числом.", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (MaxParticipants > 1000)
            {
                MessageBox.Show("Максимальное количество участников не может превышать 1000.", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using (var context = new ApplicationDbContext())
                {
                    if (IsEditMode && SelectedSchedule != null)
                    {
                        var scheduleToUpdate = await context.ExamSchedules.FindAsync(SelectedSchedule.Id);
                        if (scheduleToUpdate != null)
                        {
                            scheduleToUpdate.ExamId = SelectedExam.Id;
                            scheduleToUpdate.ExamDate = ExamDate;
                            scheduleToUpdate.StartTime = StartTime;
                            scheduleToUpdate.Location = Location;
                            scheduleToUpdate.MaxParticipants = MaxParticipants;
                            scheduleToUpdate.IsActive = IsActive;
                            context.ExamSchedules.Update(scheduleToUpdate);
                        }
                    }
                    else
                    {
                        var newSchedule = new ExamSchedule
                        {
                            ExamId = SelectedExam.Id,
                            ExamDate = ExamDate,
                            StartTime = StartTime,
                            Location = Location,
                            MaxParticipants = MaxParticipants,
                            CurrentParticipants = 0,
                            IsActive = IsActive,
                            CreatedAt = DateTime.Now
                        };
                        context.ExamSchedules.Add(newSchedule);
                    }

                    await context.SaveChangesAsync().ConfigureAwait(false);
                }
                
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    MessageBox.Show("Расписание успешно сохранено.", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    ClearSelection();
                });

                await LoadSchedulesAsync();
            }
            catch (Exception ex)
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    MessageBox.Show($"Ошибка при сохранении: {ex.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                });
            }
        }

        [RelayCommand]
        public async Task DeleteScheduleAsync()
        {
            if (SelectedSchedule == null) return;

            var result = MessageBox.Show(
                $"Вы уверены, что хотите удалить это расписание?",
                "Подтверждение удаления", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    using (var context = new ApplicationDbContext())
                    {
                        var scheduleToDelete = await context.ExamSchedules.FindAsync(SelectedSchedule.Id);
                        if (scheduleToDelete != null)
                        {
                            context.ExamSchedules.Remove(scheduleToDelete);
                            await context.SaveChangesAsync().ConfigureAwait(false);
                        }
                    }
                    
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        MessageBox.Show("Расписание удалено.", "Успех",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                        ClearSelection();
                    });
                    
                    await LoadSchedulesAsync();
                }
                catch (Exception ex)
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        MessageBox.Show($"Ошибка при удалении: {ex.Message}", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    });
                }
            }
        }
    }
}

