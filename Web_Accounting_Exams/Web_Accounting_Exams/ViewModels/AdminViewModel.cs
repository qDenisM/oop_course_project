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
    public partial class AdminViewModel : BaseViewModel
    {
        private readonly ApplicationDbContext _context;
        private readonly EmailService _emailService;

        [ObservableProperty]
        private ObservableCollection<Exam> _exams = new();

        [ObservableProperty]
        private ObservableCollection<ExamSchedule> _examSchedules = new();

        [ObservableProperty]
        private ObservableCollection<University> _universities = new();

        [ObservableProperty]
        private ObservableCollection<ExamApplication> _pendingApplications = new();

        [ObservableProperty]
        private ObservableCollection<Certificate> _pendingCertificates = new();

        [ObservableProperty]
        private ObservableCollection<Certificate> _allCertificates = new();

        [ObservableProperty]
        private ObservableCollection<UniversityApplication> _pendingUniversityApplications = new();

        [ObservableProperty]
        private ObservableCollection<UniversityApplication> _allUniversityApplications = new();

        [ObservableProperty]
        private CertificateStatus? _selectedCertificateStatusFilter;

        [ObservableProperty]
        private int? _selectedCertificateUserIdFilter;

        [ObservableProperty]
        private UniversityApplicationStatus? _selectedUniversityApplicationStatusFilter;

        [ObservableProperty]
        private int? _selectedUniversityApplicationUserIdFilter;

        [ObservableProperty]
        private ObservableCollection<Models.User> _allUsers = new();

        [ObservableProperty]
        private Exam? _selectedExam;

        [ObservableProperty]
        private ExamSchedule? _selectedSchedule;

        [ObservableProperty]
        private University? _selectedUniversity;

        [ObservableProperty]
        private ExamApplication? _selectedApplication;

        [ObservableProperty]
        private Certificate? _selectedCertificate;

        [ObservableProperty]
        private UniversityApplication? _selectedUniversityApplication;

        [ObservableProperty]
        private Models.User? _currentUser;

        public AdminViewModel(ApplicationDbContext context, EmailService emailService, AuthenticationService authService)
        {
            _context = context;
            _emailService = emailService;
            CurrentUser = authService.CurrentUser;
            LoadDataAsync();
        }

        private async void LoadDataAsync()
        {
            await LoadExamsAsync();
            await LoadSchedulesAsync();
            await LoadUniversitiesAsync();
            await LoadPendingApplicationsAsync();
            await LoadAllCertificatesAsync();
            await LoadAllUniversityApplicationsAsync();
            await LoadUsersAsync();
        }

        [RelayCommand]
        private async Task LoadUsersAsync()
        {
            var users = await _context.Users
                .Where(u => u.Role == UserRole.Applicant)
                .OrderBy(u => u.LastName)
                .ThenBy(u => u.FirstName)
                .ToListAsync();
            AllUsers = new ObservableCollection<Models.User>(users);
        }

        [RelayCommand]
        private async Task LoadExamsAsync()
        {
            var exams = await _context.Exams.ToListAsync();
            Exams = new ObservableCollection<Exam>(exams);
        }

        [RelayCommand]
        private async Task LoadSchedulesAsync()
        {
            var schedules = await _context.ExamSchedules
                .Include(s => s.Exam)
                .ToListAsync();
            ExamSchedules = new ObservableCollection<ExamSchedule>(schedules);
        }

        [RelayCommand]
        private async Task LoadUniversitiesAsync()
        {
            var universities = await _context.Universities
                .Include(u => u.Specialities)
                .ToListAsync();
            Universities = new ObservableCollection<University>(universities);
        }

        [RelayCommand]
        private async Task LoadPendingApplicationsAsync()
        {
            var applications = await _context.ExamApplications
                .Include(a => a.User)
                .Include(a => a.Exam)
                .Include(a => a.ExamSchedule)
                .Where(a => a.Status == ApplicationStatus.Pending)
                .ToListAsync();
            PendingApplications = new ObservableCollection<ExamApplication>(applications);
        }

        [RelayCommand]
        private async Task ApproveApplicationAsync()
        {
            if (SelectedApplication == null) return;

            try
            {
                SelectedApplication.Status = ApplicationStatus.Approved;
                SelectedApplication.ReviewedAt = DateTime.Now;
                await _context.SaveChangesAsync();

                if (SelectedApplication.User != null)
                {
                    await _emailService.SendEmailAsync(
                        SelectedApplication.User.Email,
                        "Заявка на экзамен одобрена",
                        $"Ваша заявка на экзамен '{SelectedApplication.Exam?.Name}' была одобрена."
                    );
                }

                MessageBox.Show("Заявка одобрена.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                await LoadPendingApplicationsAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        private async Task RejectApplicationAsync()
        {
            if (SelectedApplication == null) return;

            var inputDialog = new Web_Accounting_Exams.Views.InputDialog("Укажите причину отклонения:", "Отклонение заявки");
            if (inputDialog.ShowDialog() != true || string.IsNullOrWhiteSpace(inputDialog.ResponseText))
                return;

            var reason = inputDialog.ResponseText;

            try
            {
                SelectedApplication.Status = ApplicationStatus.Rejected;
                SelectedApplication.RejectionReason = reason;
                SelectedApplication.ReviewedAt = DateTime.Now;
                await _context.SaveChangesAsync();

                if (SelectedApplication.User != null)
                {
                    await _emailService.SendEmailAsync(
                        SelectedApplication.User.Email,
                        "Заявка на экзамен отклонена",
                        $"Ваша заявка на экзамен '{SelectedApplication.Exam?.Name}' была отклонена. Причина: {reason}"
                    );
                }

                MessageBox.Show("Заявка отклонена.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                await LoadPendingApplicationsAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        private async Task SendExamResultAsync()
        {
            MessageBox.Show("Функция отправки результатов будет реализована в диалоговом окне.", 
                "Отправка результатов", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        [RelayCommand]
        private async Task LoadPendingCertificatesAsync()
        {
            var certificates = await _context.Certificates
                .Include(c => c.User)
                .Where(c => c.Status == CertificateStatus.Pending)
                .OrderBy(c => c.UploadedAt)
                .ToListAsync();
            PendingCertificates = new ObservableCollection<Certificate>(certificates);
        }

        [RelayCommand]
        private async Task LoadAllCertificatesAsync()
        {
            var query = _context.Certificates
                .Include(c => c.User)
                .AsQueryable();

            if (SelectedCertificateStatusFilter.HasValue)
            {
                query = query.Where(c => c.Status == SelectedCertificateStatusFilter.Value);
            }

            if (SelectedCertificateUserIdFilter.HasValue)
            {
                query = query.Where(c => c.UserId == SelectedCertificateUserIdFilter.Value);
            }

            var certificates = await query
                .OrderByDescending(c => c.UploadedAt)
                .ToListAsync();
            
            AllCertificates = new ObservableCollection<Certificate>(certificates);
        }

        [RelayCommand]
        private async Task ApproveCertificateAsync()
        {
            if (SelectedCertificate == null || CurrentUser == null) return;

            try
            {
                SelectedCertificate.Status = CertificateStatus.Approved;
                SelectedCertificate.ReviewedAt = DateTime.Now;
                SelectedCertificate.ReviewedByUserId = CurrentUser.Id;
                await _context.SaveChangesAsync();

                if (SelectedCertificate.User != null)
                {
                    await _emailService.SendEmailAsync(
                        SelectedCertificate.User.Email,
                        "Сертификат одобрен",
                        $"Ваш сертификат по предмету '{SelectedCertificate.Subject}' был одобрен администратором."
                    );
                }

                MessageBox.Show("Сертификат одобрен.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                await LoadAllCertificatesAsync();
                await LoadPendingCertificatesAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        private async Task RejectCertificateAsync()
        {
            if (SelectedCertificate == null || CurrentUser == null) return;

            var inputDialog = new Web_Accounting_Exams.Views.InputDialog("Укажите причину отклонения:", "Отклонение сертификата");
            if (inputDialog.ShowDialog() != true || string.IsNullOrWhiteSpace(inputDialog.ResponseText))
                return;

            var reason = inputDialog.ResponseText;

            try
            {
                SelectedCertificate.Status = CertificateStatus.Rejected;
                SelectedCertificate.RejectionReason = reason;
                SelectedCertificate.ReviewedAt = DateTime.Now;
                SelectedCertificate.ReviewedByUserId = CurrentUser.Id;
                await _context.SaveChangesAsync();

                if (SelectedCertificate.User != null)
                {
                    await _emailService.SendEmailAsync(
                        SelectedCertificate.User.Email,
                        "Сертификат отклонен",
                        $"Ваш сертификат по предмету '{SelectedCertificate.Subject}' был отклонен. Причина: {reason}"
                    );
                }

                MessageBox.Show("Сертификат отклонен.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                await LoadAllCertificatesAsync();
                await LoadPendingCertificatesAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        private async Task LoadPendingUniversityApplicationsAsync()
        {
            var applications = await _context.UniversityApplications
                .Include(a => a.User)
                .Include(a => a.University)
                .Include(a => a.Speciality)
                .Where(a => a.Status == UniversityApplicationStatus.Pending)
                .OrderBy(a => a.AppliedAt)
                .ToListAsync();
            PendingUniversityApplications = new ObservableCollection<UniversityApplication>(applications);
        }

        [RelayCommand]
        private async Task LoadAllUniversityApplicationsAsync()
        {
            var query = _context.UniversityApplications
                .Include(a => a.User)
                .Include(a => a.University)
                .Include(a => a.Speciality)
                .AsQueryable();

            if (SelectedUniversityApplicationStatusFilter.HasValue)
            {
                query = query.Where(a => a.Status == SelectedUniversityApplicationStatusFilter.Value);
            }

            if (SelectedUniversityApplicationUserIdFilter.HasValue)
            {
                query = query.Where(a => a.UserId == SelectedUniversityApplicationUserIdFilter.Value);
            }

            var applications = await query
                .OrderByDescending(a => a.AppliedAt)
                .ToListAsync();
            
            AllUniversityApplications = new ObservableCollection<UniversityApplication>(applications);
        }

        [RelayCommand]
        private async Task ApproveUniversityApplicationAsync()
        {
            if (SelectedUniversityApplication == null || CurrentUser == null) return;

            try
            {
                var speciality = SelectedUniversityApplication.Speciality;
                var applicationType = SelectedUniversityApplication.Type;

                var acceptedApplications = await _context.UniversityApplications
                    .Where(a => a.SpecialityId == speciality.Id && 
                               a.Type == applicationType &&
                               a.Status == UniversityApplicationStatus.Accepted)
                    .CountAsync();

                int? maxPlaces = applicationType == ApplicationType.Budget 
                    ? speciality.BudgetPlaces 
                    : speciality.PaidPlaces;

                if (maxPlaces.HasValue && acceptedApplications >= maxPlaces.Value)
                {
                    SelectedUniversityApplication.Status = UniversityApplicationStatus.Waitlisted;
                    MessageBox.Show("Все места заняты. Заявка переведена в лист ожидания.", 
                        "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    SelectedUniversityApplication.Status = UniversityApplicationStatus.Accepted;
                    MessageBox.Show("Заявка одобрена.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }

                SelectedUniversityApplication.ReviewedAt = DateTime.Now;
                SelectedUniversityApplication.ReviewedByUserId = CurrentUser.Id;
                await _context.SaveChangesAsync();

                if (SelectedUniversityApplication.User != null)
                {
                    var statusText = SelectedUniversityApplication.Status == UniversityApplicationStatus.Accepted 
                        ? "одобрена" 
                        : "переведена в лист ожидания";
                    
                    await _emailService.SendEmailAsync(
                        SelectedUniversityApplication.User.Email,
                        "Заявка в университет рассмотрена",
                        $"Ваша заявка на специальность '{speciality.Name}' ({SelectedUniversityApplication.University.Name}) была {statusText}."
                    );
                }

                await LoadAllUniversityApplicationsAsync();
                await LoadPendingUniversityApplicationsAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        private async Task RejectUniversityApplicationAsync()
        {
            if (SelectedUniversityApplication == null || CurrentUser == null) return;

            var inputDialog = new Web_Accounting_Exams.Views.InputDialog("Укажите причину отклонения:", "Отклонение заявки");
            if (inputDialog.ShowDialog() != true || string.IsNullOrWhiteSpace(inputDialog.ResponseText))
                return;

            var reason = inputDialog.ResponseText;

            try
            {
                SelectedUniversityApplication.Status = UniversityApplicationStatus.Rejected;
                SelectedUniversityApplication.RejectionReason = reason;
                SelectedUniversityApplication.ReviewedAt = DateTime.Now;
                SelectedUniversityApplication.ReviewedByUserId = CurrentUser.Id;
                await _context.SaveChangesAsync();

                if (SelectedUniversityApplication.User != null)
                {
                    await _emailService.SendEmailAsync(
                        SelectedUniversityApplication.User.Email,
                        "Заявка в университет отклонена",
                        $"Ваша заявка на специальность '{SelectedUniversityApplication.Speciality.Name}' ({SelectedUniversityApplication.University.Name}) была отклонена. Причина: {reason}"
                    );
                }

                MessageBox.Show("Заявка отклонена.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                await LoadAllUniversityApplicationsAsync();
                await LoadPendingUniversityApplicationsAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}

