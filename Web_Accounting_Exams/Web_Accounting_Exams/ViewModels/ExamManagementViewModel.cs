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
    public partial class ExamManagementViewModel : BaseViewModel
    {
        private readonly ApplicationDbContext _context;
        private readonly EmailService _emailService;

        [ObservableProperty]
        private ObservableCollection<Exam> _exams = new();

        [ObservableProperty]
        private Exam? _selectedExam;

        [ObservableProperty]
        private string _examName = string.Empty;

        [ObservableProperty]
        private string _examSubject = string.Empty;

        [ObservableProperty]
        private string _examDescription = string.Empty;

        [ObservableProperty]
        private int _durationMinutes = 180;

        [ObservableProperty]
        private int _maxScore = 100;

        [ObservableProperty]
        private bool _isActive = true;

        [ObservableProperty]
        private bool _isEditMode = false;

        [ObservableProperty]
        private ObservableCollection<ExamApplicationWrapper> _completedApplications = new();

        [ObservableProperty]
        private Exam? _selectedExamForResult;

        private ExamApplicationWrapper? _selectedApplicationForResult;
        
        public ExamApplicationWrapper? SelectedApplicationForResult
        {
            get => _selectedApplicationForResult;
            set
            {
                if (SetProperty(ref _selectedApplicationForResult, value))
                {
                    OnSelectedApplicationForResultChanged(value?.Application);
                }
            }
        }

        [ObservableProperty]
        private int _resultScore;

        [ObservableProperty]
        private int _resultMaxScore = 100;

        [ObservableProperty]
        private string _resultComments = string.Empty;

        [ObservableProperty]
        private bool _isExamSelected = false;

        public bool IsApplicationEnabled => SelectedExamForResult != null;

        public bool CanAddResult => SelectedApplicationForResult != null && 
                                    ResultScore >= 0 && 
                                    ResultScore <= ResultMaxScore;

        public ExamManagementViewModel(ApplicationDbContext context, EmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        [RelayCommand]
        public async Task LoadExamsAsync()
        {
            try
            {
                List<Exam> exams;
                using (var context = new ApplicationDbContext())
                {
                    exams = await context.Exams
                        .OrderBy(e => e.Name)
                        .ToListAsync()
                        .ConfigureAwait(false);
                }
                
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    Exams.Clear();
                    foreach (var exam in exams)
                    {
                        Exams.Add(exam);
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка загрузки экзаменов: {ex.Message}");
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    System.Windows.MessageBox.Show($"Ошибка загрузки экзаменов: {ex.Message}", "Ошибка",
                        System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                });
            }
        }

        [RelayCommand]
        private void SelectExam(Exam exam)
        {
            SelectedExam = exam;
            ExamName = exam.Name;
            ExamSubject = exam.Subject;
            ExamDescription = exam.Description ?? string.Empty;
            DurationMinutes = exam.DurationMinutes;
            MaxScore = exam.MaxScore;
            IsActive = exam.IsActive;
            IsEditMode = true;
        }

        [RelayCommand]
        private void ClearSelection()
        {
            SelectedExam = null;
            ExamName = string.Empty;
            ExamSubject = string.Empty;
            ExamDescription = string.Empty;
            DurationMinutes = 180;
            MaxScore = 100;
            IsActive = true;
            IsEditMode = false;
        }

        [RelayCommand]
        private async Task SaveExamAsync()
        {
            if (string.IsNullOrWhiteSpace(ExamName) || string.IsNullOrWhiteSpace(ExamSubject))
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    MessageBox.Show("Пожалуйста, заполните все обязательные поля.", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                });
                return;
            }

            try
            {
                using (var context = new ApplicationDbContext())
                {
                    if (IsEditMode && SelectedExam != null)
                    {
                        var examToUpdate = await context.Exams.FindAsync(SelectedExam.Id);
                        if (examToUpdate != null)
                        {
                            examToUpdate.Name = ExamName;
                            examToUpdate.Subject = ExamSubject;
                            examToUpdate.Description = ExamDescription;
                            examToUpdate.DurationMinutes = DurationMinutes;
                            examToUpdate.MaxScore = MaxScore;
                            examToUpdate.IsActive = IsActive;
                            context.Exams.Update(examToUpdate);
                        }
                    }
                    else
                    {
                        var newExam = new Exam
                        {
                            Name = ExamName,
                            Subject = ExamSubject,
                            Description = ExamDescription,
                            DurationMinutes = DurationMinutes,
                            MaxScore = MaxScore,
                            IsActive = IsActive,
                            CreatedAt = DateTime.Now
                        };
                        context.Exams.Add(newExam);
                    }

                    await context.SaveChangesAsync().ConfigureAwait(false);
                }
                
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    MessageBox.Show("Экзамен успешно сохранен.", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    ClearSelection();
                });

                await LoadExamsAsync();
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
        public async Task DeleteExamAsync()
        {
            if (SelectedExam == null) return;

            var result = System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                return MessageBox.Show(
                    $"Вы уверены, что хотите удалить экзамен '{SelectedExam.Name}'?",
                    "Подтверждение удаления", MessageBoxButton.YesNo, MessageBoxImage.Question);
            });

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    using (var context = new ApplicationDbContext())
                    {
                        var examToDelete = await context.Exams.FindAsync(SelectedExam.Id);
                        if (examToDelete != null)
                        {
                            context.Exams.Remove(examToDelete);
                            await context.SaveChangesAsync().ConfigureAwait(false);
                        }
                    }
                    
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        MessageBox.Show("Экзамен удален.", "Успех",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                        ClearSelection();
                    });
                    
                    await LoadExamsAsync();
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

        partial void OnSelectedExamForResultChanged(Exam? value)
        {
            IsExamSelected = value != null;
            LoadCompletedApplicationsAsync();
            OnPropertyChanged(nameof(IsApplicationEnabled));
            OnPropertyChanged(nameof(CanAddResult));
        }

        private void OnSelectedApplicationForResultChanged(ExamApplication? value)
        {
            if (value != null && SelectedExamForResult != null)
            {
                ResultMaxScore = SelectedExamForResult.MaxScore;
            }
            OnPropertyChanged(nameof(CanAddResult));
        }

        partial void OnResultScoreChanged(int value)
        {
            OnPropertyChanged(nameof(CanAddResult));
        }

        [RelayCommand]
        private async Task LoadCompletedApplicationsAsync()
        {
            if (SelectedExamForResult == null)
            {
                CompletedApplications.Clear();
                return;
            }

            try
            {
                using (var context = new ApplicationDbContext())
                {
                    var existingResultApplicationIds = await context.ExamResults
                        .Select(r => r.ExamApplicationId)
                        .ToListAsync()
                        .ConfigureAwait(false);

                    var allApplications = await context.ExamApplications
                        .Include(a => a.User)
                        .Include(a => a.Exam)
                        .Include(a => a.ExamSchedule)
                        .Where(a => a.ExamId == SelectedExamForResult.Id)
                        .ToListAsync()
                        .ConfigureAwait(false);

                    var today = DateTime.Today;
                    var applications = allApplications
                        .Where(a => a.Status == ApplicationStatus.Approved &&
                                   a.ExamSchedule != null &&
                                   a.ExamSchedule.ExamDate.Date <= today &&
                                   !existingResultApplicationIds.Contains(a.Id))
                        .ToList();

                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        CompletedApplications.Clear();
                        foreach (var app in applications)
                        {
                            CompletedApplications.Add(new ExamApplicationWrapper(app));
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка загрузки заявок: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    MessageBox.Show($"Ошибка загрузки заявок: {ex.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                });
            }
        }

        [RelayCommand]
        private async Task AddExamResultAsync()
        {
            if (SelectedApplicationForResult == null)
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    MessageBox.Show("Пожалуйста, выберите абитуриента.", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                });
                return;
            }

            if (ResultScore < 0 || ResultScore > ResultMaxScore)
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    MessageBox.Show($"Баллы должны быть в диапазоне от 0 до {ResultMaxScore}.", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                });
                return;
            }

            try
            {
                using (var context = new ApplicationDbContext())
                {
                    var existingResult = await context.ExamResults
                        .FirstOrDefaultAsync(r => r.ExamApplicationId == SelectedApplicationForResult.Application.Id)
                        .ConfigureAwait(false);

                    if (existingResult != null)
                    {
                        System.Windows.Application.Current.Dispatcher.Invoke(() =>
                        {
                            MessageBox.Show("Результат для этой заявки уже добавлен.", "Ошибка",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                        });
                        return;
                    }

                    var application = await context.ExamApplications
                        .Include(a => a.User)
                        .Include(a => a.Exam)
                        .Include(a => a.ExamSchedule)
                        .FirstOrDefaultAsync(a => a.Id == SelectedApplicationForResult.Application.Id)
                        .ConfigureAwait(false);

                    if (application == null)
                    {
                        System.Windows.Application.Current.Dispatcher.Invoke(() =>
                        {
                            MessageBox.Show("Заявка не найдена.", "Ошибка",
                                MessageBoxButton.OK, MessageBoxImage.Error);
                        });
                        return;
                    }

                    var result = new ExamResult
                    {
                        UserId = application.UserId,
                        ExamId = application.ExamId,
                        ExamApplicationId = application.Id,
                        Score = ResultScore,
                        MaxScore = ResultMaxScore,
                        Comments = ResultComments,
                        ExamDate = application.ExamSchedule.ExamDate,
                        ResultDate = DateTime.Now,
                        IsNotified = false
                    };

                    context.ExamResults.Add(result);
                    application.Status = ApplicationStatus.Completed;
                    await context.SaveChangesAsync().ConfigureAwait(false);

                    bool emailSent = false;
                    string emailMessage = "Результат успешно добавлен.";

                    if (application.User != null)
                    {
                        emailSent = await _emailService.SendExamResultNotificationAsync(
                            application.User.Email,
                            application.User.FullName,
                            application.Exam?.Name ?? "Экзамен",
                            ResultScore,
                            ResultMaxScore).ConfigureAwait(false);

                        if (emailSent)
                        {
                            result.IsNotified = true;
                            await context.SaveChangesAsync().ConfigureAwait(false);
                            emailMessage += " Email успешно отправлен.";
                        }
                        else
                        {
                            var errorMsg = _emailService.LastErrorMessage;
                            emailMessage += $"\n\nОшибка отправки email: {errorMsg}";
                        }
                    }

                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        MessageBox.Show(emailMessage, emailSent ? "Успех" : "Предупреждение",
                            MessageBoxButton.OK, emailSent ? MessageBoxImage.Information : MessageBoxImage.Warning);
                    });

                    ResultScore = 0;
                    ResultComments = string.Empty;
                    SelectedApplicationForResult = null;
                    
                    await LoadCompletedApplicationsAsync();
                }
            }
            catch (Exception ex)
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    MessageBox.Show($"Ошибка при добавлении результата: {ex.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                });
            }
        }
    }

    public class ExamApplicationWrapper
    {
        public ExamApplication Application { get; set; }
        
        public string DisplayText => $"{Application.User?.FullName ?? "Неизвестно"} - {Application.ExamSchedule?.ExamDate:dd.MM.yyyy}";

        public ExamApplicationWrapper(ExamApplication application)
        {
            Application = application;
        }
    }
}

