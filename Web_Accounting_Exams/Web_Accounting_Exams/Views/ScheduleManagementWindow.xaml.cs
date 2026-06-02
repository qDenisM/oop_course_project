using System.Windows;
using Microsoft.EntityFrameworkCore;
using Web_Accounting_Exams.Data;
using Web_Accounting_Exams.ViewModels;

namespace Web_Accounting_Exams.Views
{
    public partial class ScheduleManagementWindow : Window
    {
        private readonly ApplicationDbContext _context;
        private readonly ScheduleManagementViewModel _viewModel;

        public ScheduleManagementWindow()
        {
            InitializeComponent();
            _context = new ApplicationDbContext();
            _viewModel = new ScheduleManagementViewModel(_context);
            DataContext = _viewModel;
            LoadSchedules();
        }

        private class ScheduleWithSelection
        {
            public Models.ExamSchedule Schedule { get; set; } = null!;
            public bool IsSelected { get; set; }
            public int FreePlaces => Schedule.MaxParticipants - Schedule.CurrentParticipants;
        }

        private List<ScheduleWithSelection> _schedulesWithSelection = new();

        private async void LoadSchedules()
        {
            try
            {
                await _viewModel.LoadSchedulesAsync().ConfigureAwait(true);
                
                _schedulesWithSelection = _viewModel.Schedules
                    .Select(s => new ScheduleWithSelection { Schedule = s, IsSelected = false })
                    .ToList();
                
                ScheduleDataGrid.ItemsSource = null;
                ScheduleDataGrid.ItemsSource = _schedulesWithSelection;
                ScheduleDataGrid.Items.Refresh();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки расписания: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void AddButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new ScheduleEditDialog();
            if (dialog.ShowDialog() == true && dialog.ResultSchedule != null)
            {
                try
                {
                    using (var context = new ApplicationDbContext())
                    {
                        context.ExamSchedules.Add(dialog.ResultSchedule);
                        await context.SaveChangesAsync().ConfigureAwait(true);
                    }
                    LoadSchedules();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка сохранения: {ex.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async void EditButton_Click(object sender, RoutedEventArgs e)
        {
            if (ScheduleDataGrid.SelectedItem is ScheduleWithSelection scheduleWrapper)
            {
                var schedule = scheduleWrapper.Schedule;
                var scheduleCopy = new Models.ExamSchedule
                {
                    Id = schedule.Id,
                    ExamId = schedule.ExamId,
                    ExamDate = schedule.ExamDate,
                    StartTime = schedule.StartTime,
                    Location = schedule.Location,
                    MaxParticipants = schedule.MaxParticipants,
                    CurrentParticipants = schedule.CurrentParticipants,
                    IsActive = schedule.IsActive,
                    CreatedAt = schedule.CreatedAt
                };
                
                var dialog = new ScheduleEditDialog(null, scheduleCopy);
                if (dialog.ShowDialog() == true && dialog.ResultSchedule != null)
                {
                    try
                    {
                        using (var context = new ApplicationDbContext())
                        {
                            var scheduleToUpdate = await context.ExamSchedules
                                .Include(s => s.Exam)
                                .FirstOrDefaultAsync(s => s.Id == schedule.Id);
                            
                            if (scheduleToUpdate != null)
                            {
                                scheduleToUpdate.ExamId = dialog.ResultSchedule.ExamId;
                                scheduleToUpdate.ExamDate = dialog.ResultSchedule.ExamDate;
                                scheduleToUpdate.StartTime = dialog.ResultSchedule.StartTime;
                                scheduleToUpdate.Location = dialog.ResultSchedule.Location;
                                scheduleToUpdate.MaxParticipants = dialog.ResultSchedule.MaxParticipants;
                                scheduleToUpdate.IsActive = dialog.ResultSchedule.IsActive;
                                
                                context.ExamSchedules.Update(scheduleToUpdate);
                                await context.SaveChangesAsync().ConfigureAwait(true);
                            }
                        }
                        LoadSchedules();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка сохранения: {ex.Message}", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            else
            {
                MessageBox.Show("Пожалуйста, выберите расписание для редактирования.", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private async void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedSchedules = _schedulesWithSelection
                .Where(s => s.IsSelected)
                .Select(s => s.Schedule)
                .ToList();

            if (selectedSchedules.Count == 0)
            {
                MessageBox.Show("Пожалуйста, выберите расписание(я) для удаления.", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var message = selectedSchedules.Count == 1
                ? "Вы уверены, что хотите удалить это расписание?"
                : $"Вы уверены, что хотите удалить {selectedSchedules.Count} расписаний?";

            var result = MessageBox.Show(message, "Подтверждение удаления", 
                MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    foreach (var schedule in selectedSchedules)
                    {
                        _viewModel.SelectedSchedule = schedule;
                        await _viewModel.DeleteScheduleAsync();
                    }
                    LoadSchedules();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка удаления: {ex.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}

