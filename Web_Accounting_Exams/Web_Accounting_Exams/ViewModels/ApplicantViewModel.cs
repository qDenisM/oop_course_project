using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using Web_Accounting_Exams.Data;
using Web_Accounting_Exams.Models;
using Web_Accounting_Exams.Services;

namespace Web_Accounting_Exams.ViewModels
{
    public partial class ApplicantViewModel : BaseViewModel
    {
        private readonly ApplicationDbContext _context;
        private readonly AuthenticationService _authService;
        private readonly UniversityMatchingService _matchingService;

        [ObservableProperty]
        private ObservableCollection<Exam> _availableExams = new();

        [ObservableProperty]
        private ObservableCollection<ExamSchedule> _availableSchedules = new();

        [ObservableProperty]
        private ObservableCollection<ExamApplication> _myApplications = new();

        [ObservableProperty]
        private ObservableCollection<ExamResult> _myResults = new();

        [ObservableProperty]
        private ObservableCollection<University> _universities = new();

        [ObservableProperty]
        private ObservableCollection<Certificate> _myCertificates = new();

        [ObservableProperty]
        private ObservableCollection<MatchedSpecialityInfo> _matchedSpecialities = new();

        [ObservableProperty]
        private ObservableCollection<UniversityApplication> _myUniversityApplications = new();

        [ObservableProperty]
        private Exam? _selectedExam;

        [ObservableProperty]
        private ExamSchedule? _selectedSchedule;

        [ObservableProperty]
        private Models.User? _currentUser;

        [ObservableProperty]
        private MatchedSpecialityInfo? _selectedMatchedSpeciality;

        public ApplicantViewModel(ApplicationDbContext context, AuthenticationService authService, UniversityMatchingService matchingService)
        {
            _context = context;
            _authService = authService;
            _matchingService = matchingService;
            CurrentUser = _authService.CurrentUser;
            LoadDataAsync();
        }

        private async void LoadDataAsync()
        {
            await LoadAvailableExamsAsync();
            await LoadMyApplicationsAsync();
            await LoadMyResultsAsync();
            await LoadUniversitiesAsync();
            await LoadMyCertificatesAsync();
            await LoadMyUniversityApplicationsAsync();
        }

        [RelayCommand]
        private async Task LoadAvailableExamsAsync()
        {
            var exams = await _context.Exams
                .Where(e => e.IsActive)
                .ToListAsync();
            AvailableExams = new ObservableCollection<Exam>(exams);
        }

        [RelayCommand]
        private async Task LoadAvailableSchedulesAsync()
        {
            if (SelectedExam == null) return;

            var schedules = await _context.ExamSchedules
                .Include(s => s.Exam)
                .Where(s => s.ExamId == SelectedExam.Id && s.IsActive && s.ExamDate >= DateTime.Today)
                .ToListAsync();
            AvailableSchedules = new ObservableCollection<ExamSchedule>(schedules);
        }

        [RelayCommand]
        private async Task LoadMyApplicationsAsync()
        {
            if (CurrentUser == null) return;

            var applications = await _context.ExamApplications
                .Include(a => a.Exam)
                .Include(a => a.ExamSchedule)
                .Where(a => a.UserId == CurrentUser.Id)
                .ToListAsync();
            MyApplications = new ObservableCollection<ExamApplication>(applications);
        }

        [RelayCommand]
        private async Task LoadMyResultsAsync()
        {
            if (CurrentUser == null) return;

            var results = await _context.ExamResults
                .Include(r => r.Exam)
                .Where(r => r.UserId == CurrentUser.Id)
                .OrderByDescending(r => r.ExamDate)
                .ToListAsync();
            MyResults = new ObservableCollection<ExamResult>(results);
        }

        [RelayCommand]
        private async Task LoadUniversitiesAsync()
        {
            var universities = await _context.Universities
                .Include(u => u.Specialities)
                .Where(u => u.IsActive)
                .ToListAsync();
            Universities = new ObservableCollection<University>(universities);
        }

        [RelayCommand]
        private async Task LoadMyCertificatesAsync()
        {
            if (CurrentUser == null) return;

            var certificates = await _context.Certificates
                .Where(c => c.UserId == CurrentUser.Id)
                .ToListAsync();
            MyCertificates = new ObservableCollection<Certificate>(certificates);
        }

        [RelayCommand]
        private async Task SubmitApplicationAsync()
        {
            if (CurrentUser == null || SelectedExam == null || SelectedSchedule == null)
            {
                MessageBox.Show("Пожалуйста, выберите экзамен и расписание.", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var existingApplication = await _context.ExamApplications
                    .FirstOrDefaultAsync(a => a.UserId == CurrentUser.Id && 
                                             a.ExamId == SelectedExam.Id &&
                                             a.Status != ApplicationStatus.Rejected);

                if (existingApplication != null)
                {
                    MessageBox.Show("Вы уже подали заявку на этот экзамен.", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (SelectedSchedule.CurrentParticipants >= SelectedSchedule.MaxParticipants)
                {
                    MessageBox.Show("Нет свободных мест на этот экзамен.", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var application = new ExamApplication
                {
                    UserId = CurrentUser.Id,
                    ExamId = SelectedExam.Id,
                    ExamScheduleId = SelectedSchedule.Id,
                    Status = ApplicationStatus.Pending,
                    AppliedAt = DateTime.Now
                };

                _context.ExamApplications.Add(application);
                SelectedSchedule.CurrentParticipants++;
                await _context.SaveChangesAsync();

                MessageBox.Show("Заявка успешно подана!", "Успех",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                await LoadMyApplicationsAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при подаче заявки: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        private async Task UploadCertificateAsync()
        {
            if (CurrentUser == null) return;

            var openFileDialog = new OpenFileDialog
            {
                Filter = "PDF files (*.pdf)|*.pdf|Image files (*.jpg;*.jpeg;*.png)|*.jpg;*.jpeg;*.png|All files (*.*)|*.*",
                Title = "Выберите файл сертификата"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    var fileName = Path.GetFileName(openFileDialog.FileName);
                    var uploadPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Certificates");
                    Directory.CreateDirectory(uploadPath);

                    var destinationPath = Path.Combine(uploadPath, $"{CurrentUser.Id}_{Guid.NewGuid()}_{fileName}");
                    File.Copy(openFileDialog.FileName, destinationPath, true);

                    var typeDialog = new Web_Accounting_Exams.Views.InputDialog("Тип сертификата (CT или Diploma):", "Тип сертификата", "CT");
                    if (typeDialog.ShowDialog() != true) return;
                    var certificateType = typeDialog.ResponseText;

                    var subjectDialog = new Web_Accounting_Exams.Views.InputDialog("Предмет:", "Предмет");
                    if (subjectDialog.ShowDialog() != true) return;
                    var subject = subjectDialog.ResponseText;

                    var scoreDialog = new Web_Accounting_Exams.Views.InputDialog("Баллы:", "Баллы");
                    if (scoreDialog.ShowDialog() != true) return;
                    var scoreStr = scoreDialog.ResponseText;

                    var certificateNumberDialog = new Web_Accounting_Exams.Views.InputDialog("Номер сертификата:", "Номер сертификата");
                    if (certificateNumberDialog.ShowDialog() != true) return;
                    var certificateNumber = certificateNumberDialog.ResponseText;

                    if (int.TryParse(scoreStr, out int score))
                    {
                        var certificate = new Certificate
                        {
                            UserId = CurrentUser.Id,
                            Type = certificateType.ToUpper() == "CT" ? CertificateType.CT : CertificateType.Diploma,
                            Subject = subject,
                            Score = score,
                            CertificateNumber = certificateNumber,
                            FilePath = destinationPath,
                            FileName = fileName,
                            UploadedAt = DateTime.Now,
                            Status = CertificateStatus.Pending
                        };

                        _context.Certificates.Add(certificate);
                        await _context.SaveChangesAsync();

                        MessageBox.Show("Сертификат успешно загружен!", "Успех",
                            MessageBoxButton.OK, MessageBoxImage.Information);

                        await LoadMyCertificatesAsync();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при загрузке сертификата: {ex.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        [RelayCommand]
        private async Task UpdateProfileAsync()
        {
            MessageBox.Show("Функция редактирования профиля будет реализована в диалоговом окне.",
                "Редактирование профиля", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        [RelayCommand]
        private async Task FindMatchingSpecialitiesAsync()
        {
            if (CurrentUser == null)
            {
                MessageBox.Show("Пользователь не найден.", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var approvedCertificates = await _context.Certificates
                    .Where(c => c.UserId == CurrentUser.Id && c.Status == CertificateStatus.Approved)
                    .ToListAsync();

                if (!approvedCertificates.Any())
                {
                    MessageBox.Show("У вас нет одобренных сертификатов. Пожалуйста, дождитесь рассмотрения ваших сертификатов администратором.",
                        "Нет сертификатов", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var matchedSpecialities = await _matchingService.FindMatchingUniversitiesAsync(
                    CurrentUser.Id, 
                    minTotalScore: null, 
                    showOnlyWithMatches: true);

                if (!matchedSpecialities.Any())
                {
                    MessageBox.Show("Не найдено подходящих специальностей по вашим сертификатам.",
                        "Результаты поиска", MessageBoxButton.OK, MessageBoxImage.Information);
                    MatchedSpecialities.Clear();
                    return;
                }

                var matchedInfoList = new List<MatchedSpecialityInfo>();

                foreach (var matched in matchedSpecialities)
                {
                    var budgetApplications = await _context.UniversityApplications
                        .Where(a => a.SpecialityId == matched.Speciality.Id && 
                                   a.Type == ApplicationType.Budget &&
                                   (a.Status == UniversityApplicationStatus.Pending || 
                                    a.Status == UniversityApplicationStatus.Accepted))
                        .CountAsync();

                    var paidApplications = await _context.UniversityApplications
                        .Where(a => a.SpecialityId == matched.Speciality.Id && 
                                   a.Type == ApplicationType.Paid &&
                                   (a.Status == UniversityApplicationStatus.Pending || 
                                    a.Status == UniversityApplicationStatus.Accepted))
                        .CountAsync();

                    var availableBudgetPlaces = (matched.Speciality.BudgetPlaces ?? 0) - budgetApplications;
                    var availablePaidPlaces = (matched.Speciality.PaidPlaces ?? 0) - paidApplications;

                    if (availableBudgetPlaces > 0 || availablePaidPlaces > 0)
                    {
                        matchedInfoList.Add(new MatchedSpecialityInfo
                        {
                            MatchedSpeciality = matched,
                            AvailableBudgetPlaces = availableBudgetPlaces,
                            AvailablePaidPlaces = availablePaidPlaces,
                            CanApplyBudget = availableBudgetPlaces > 0 && matched.AllRequirementsMet,
                            CanApplyPaid = availablePaidPlaces > 0 && matched.AllRequirementsMet,
                            CanApplyBoth = availableBudgetPlaces > 0 && availablePaidPlaces > 0 && matched.AllRequirementsMet
                        });
                    }
                }

                MatchedSpecialities = new ObservableCollection<MatchedSpecialityInfo>(matchedInfoList);

                if (!matchedInfoList.Any())
                {
                    MessageBox.Show("Не найдено специальностей с доступными местами.",
                        "Результаты поиска", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при поиске специальностей: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        private async Task SubmitUniversityApplicationAsync()
        {
            if (CurrentUser == null || SelectedMatchedSpeciality == null)
            {
                MessageBox.Show("Пожалуйста, выберите специальность.", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var matched = SelectedMatchedSpeciality.MatchedSpeciality;

            ApplicationType applicationType = ApplicationType.Budget;
            bool canChoose = SelectedMatchedSpeciality.CanApplyBoth;

            if (canChoose)
            {
                var choiceDialog = new Web_Accounting_Exams.Views.InputDialog(
                    "Выберите тип заявки:\n1 - Бюджет\n2 - Платное", 
                    "Выбор типа заявки", 
                    "1");
                
                if (choiceDialog.ShowDialog() != true) return;

                if (choiceDialog.ResponseText == "2")
                {
                    applicationType = ApplicationType.Paid;
                }
            }
            else if (SelectedMatchedSpeciality.CanApplyPaid && !SelectedMatchedSpeciality.CanApplyBudget)
            {
                applicationType = ApplicationType.Paid;
            }
            else if (!SelectedMatchedSpeciality.CanApplyBudget)
            {
                MessageBox.Show("Нет доступных мест на эту специальность.", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var existingApplication = await _context.UniversityApplications
                .FirstOrDefaultAsync(a => a.UserId == CurrentUser.Id && 
                                         a.SpecialityId == matched.Speciality.Id &&
                                         a.Status != UniversityApplicationStatus.Rejected);

            if (existingApplication != null)
            {
                MessageBox.Show("Вы уже подали заявку на эту специальность.", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var budgetApplications = await _context.UniversityApplications
                .Where(a => a.SpecialityId == matched.Speciality.Id && 
                           a.Type == ApplicationType.Budget &&
                           (a.Status == UniversityApplicationStatus.Pending || 
                            a.Status == UniversityApplicationStatus.Accepted))
                .CountAsync();

            var paidApplications = await _context.UniversityApplications
                .Where(a => a.SpecialityId == matched.Speciality.Id && 
                           a.Type == ApplicationType.Paid &&
                           (a.Status == UniversityApplicationStatus.Pending || 
                            a.Status == UniversityApplicationStatus.Accepted))
                .CountAsync();

            var availableBudgetPlaces = (matched.Speciality.BudgetPlaces ?? 0) - budgetApplications;
            var availablePaidPlaces = (matched.Speciality.PaidPlaces ?? 0) - paidApplications;

            if (applicationType == ApplicationType.Budget && availableBudgetPlaces <= 0)
            {
                MessageBox.Show("Нет доступных бюджетных мест на эту специальность.", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (applicationType == ApplicationType.Paid && availablePaidPlaces <= 0)
            {
                MessageBox.Show("Нет доступных платных мест на эту специальность.", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var application = new UniversityApplication
                {
                    UserId = CurrentUser.Id,
                    UniversityId = matched.University.Id,
                    SpecialityId = matched.Speciality.Id,
                    Type = applicationType,
                    Status = UniversityApplicationStatus.Pending,
                    AppliedAt = DateTime.Now
                };

                _context.UniversityApplications.Add(application);
                await _context.SaveChangesAsync();

                MessageBox.Show($"Заявка на специальность '{matched.Speciality.Name}' ({matched.University.Name}) успешно подана!",
                    "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

                await LoadMyUniversityApplicationsAsync();
                await FindMatchingSpecialitiesAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при подаче заявки: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        private async Task LoadMyUniversityApplicationsAsync()
        {
            if (CurrentUser == null) return;

            var applications = await _context.UniversityApplications
                .Include(a => a.University)
                .Include(a => a.Speciality)
                .Where(a => a.UserId == CurrentUser.Id)
                .OrderByDescending(a => a.AppliedAt)
                .ToListAsync();
            MyUniversityApplications = new ObservableCollection<UniversityApplication>(applications);
        }
    }

    public class MatchedSpecialityInfo
    {
        public Services.MatchedSpeciality MatchedSpeciality { get; set; } = null!;
        public int AvailableBudgetPlaces { get; set; }
        public int AvailablePaidPlaces { get; set; }
        public bool CanApplyBudget { get; set; }
        public bool CanApplyPaid { get; set; }
        public bool CanApplyBoth { get; set; }
    }
}

