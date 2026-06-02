using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;
using System.Windows;
using Web_Accounting_Exams.Data;
using Web_Accounting_Exams.Models;
using Web_Accounting_Exams.Services;

namespace Web_Accounting_Exams.ViewModels
{
    public partial class ExamResultManagementViewModel : BaseViewModel
    {
        private readonly ApplicationDbContext _context;
        private readonly EmailService _emailService;

        [ObservableProperty]
        private ObservableCollection<ExamApplication> _completedApplications = new();

        [ObservableProperty]
        private ObservableCollection<ExamResult> _examResults = new();

        [ObservableProperty]
        private ExamApplication? _selectedApplication;

        [ObservableProperty]
        private int _score;

        [ObservableProperty]
        private int _maxScore = 100;

        [ObservableProperty]
        private string _comments = string.Empty;

        public ExamResultManagementViewModel(ApplicationDbContext context, EmailService emailService)
        {
            _context = context;
            _emailService = emailService;
            LoadDataAsync();
        }

        private async void LoadDataAsync()
        {
            await LoadCompletedApplicationsAsync();
            await LoadExamResultsAsync();
        }

        [RelayCommand]
        private async Task LoadCompletedApplicationsAsync()
        {
            var applications = await _context.ExamApplications
                .Include(a => a.User)
                .Include(a => a.Exam)
                .Include(a => a.ExamSchedule)
                .Where(a => a.Status == ApplicationStatus.Approved && 
                           a.ExamSchedule.ExamDate < DateTime.Today)
                .ToListAsync();
            CompletedApplications = new ObservableCollection<ExamApplication>(applications);
        }

        [RelayCommand]
        private async Task LoadExamResultsAsync()
        {
            var results = await _context.ExamResults
                .Include(r => r.User)
                .Include(r => r.Exam)
                .OrderByDescending(r => r.ExamDate)
                .ToListAsync();
            ExamResults = new ObservableCollection<ExamResult>(results);
        }

        [RelayCommand]
        private async Task AddExamResultAsync()
        {
            if (SelectedApplication == null)
            {
                MessageBox.Show("Пожалуйста, выберите заявку.", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (Score < 0 || Score > MaxScore)
            {
                MessageBox.Show($"Баллы должны быть в диапазоне от 0 до {MaxScore}.", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var existingResult = await _context.ExamResults
                    .FirstOrDefaultAsync(r => r.ExamApplicationId == SelectedApplication.Id);

                if (existingResult != null)
                {
                    MessageBox.Show("Результат для этой заявки уже добавлен.", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var result = new ExamResult
                {
                    UserId = SelectedApplication.UserId,
                    ExamId = SelectedApplication.ExamId,
                    ExamApplicationId = SelectedApplication.Id,
                    Score = Score,
                    MaxScore = MaxScore,
                    Comments = Comments,
                    ExamDate = SelectedApplication.ExamSchedule.ExamDate,
                    ResultDate = DateTime.Now,
                    IsNotified = false
                };

                _context.ExamResults.Add(result);
                SelectedApplication.Status = ApplicationStatus.Completed;
                await _context.SaveChangesAsync();

                bool emailSent = false;
                string emailMessage = "Результат успешно добавлен.";

                if (SelectedApplication.User != null)
                {
                    emailSent = await _emailService.SendExamResultNotificationAsync(
                        SelectedApplication.User.Email,
                        SelectedApplication.User.FullName,
                        SelectedApplication.Exam?.Name ?? "Экзамен",
                        Score,
                        MaxScore);

                    if (emailSent)
                    {
                        result.IsNotified = true;
                        await _context.SaveChangesAsync();
                        emailMessage += " Email успешно отправлен.";
                    }
                    else
                    {
                        var errorMsg = _emailService.LastErrorMessage;
                        emailMessage += $"\n\nОшибка отправки email: {errorMsg}";
                    }
                }

                MessageBox.Show(emailMessage, emailSent ? "Успех" : "Предупреждение",
                    MessageBoxButton.OK, emailSent ? MessageBoxImage.Information : MessageBoxImage.Warning);

                Score = 0;
                Comments = string.Empty;
                SelectedApplication = null;
                await LoadCompletedApplicationsAsync();
                await LoadExamResultsAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при добавлении результата: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        private async Task ResendNotificationAsync(ExamResult result)
        {
            if (result == null) return;

            try
            {
                var application = await _context.ExamApplications
                    .Include(a => a.User)
                    .Include(a => a.Exam)
                    .FirstOrDefaultAsync(a => a.Id == result.ExamApplicationId);

                if (application?.User != null)
                {
                    var emailSent = await _emailService.SendExamResultNotificationAsync(
                        application.User.Email,
                        application.User.FullName,
                        application.Exam?.Name ?? "Экзамен",
                        result.Score,
                        result.MaxScore);

                    if (emailSent)
                    {
                        result.IsNotified = true;
                        await _context.SaveChangesAsync();
                        MessageBox.Show("Уведомление успешно отправлено.", "Успех",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        var errorMsg = _emailService.LastErrorMessage;
                        MessageBox.Show($"Ошибка при отправке уведомления.\n\n{errorMsg}", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}

