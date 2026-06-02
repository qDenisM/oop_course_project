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
    public partial class DashboardViewModel : BaseViewModel
    {
        private readonly AuthenticationService _authService;
        private readonly ApplicationDbContext _context;
        private readonly EmailService _emailService;

        [ObservableProperty]
        private Models.User? _currentUser;

        [ObservableProperty]
        private ObservableCollection<ExamApplication> _examApplications = new();

        [ObservableProperty]
        private ObservableCollection<ExamResult> _examResults = new();

        [ObservableProperty]
        private ObservableCollection<ExamSchedule> _examSchedules = new();

        [ObservableProperty]
        private ObservableCollection<University> _universities = new();

        [ObservableProperty]
        private ObservableCollection<Certificate> _certificates = new();

        [ObservableProperty]
        private ObservableCollection<Services.MatchedSpeciality> _matchedSpecialities = new();

        [ObservableProperty]
        private ObservableCollection<UniversityApplication> _universityApplications = new();

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
        private string _currentView = "Applications";

        [ObservableProperty]
        private bool _isAdmin;

        public DashboardViewModel(AuthenticationService authService)
        {
            _authService = authService;
            _context = new ApplicationDbContext();
            _emailService = new EmailService();

            CurrentUser = _authService.CurrentUser;
            IsAdmin = _authService.IsAdmin();

            LoadDataAsync();
        }

        public async Task LoadDataAsync()
        {
            if (CurrentUser == null) return;

            try
            {
                if (IsAdmin)
                {
                    await LoadAdminDataAsync();
                }
                else
                {
                    await LoadApplicantDataAsync();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void LoadDataAsyncInternal()
        {
            await LoadDataAsync();
        }

        private async Task LoadAdminDataAsync()
        {
            using (var context = new Data.ApplicationDbContext())
            {
                var applications = await context.ExamApplications
                    .Include(a => a.User)
                    .Include(a => a.Exam)
                    .Include(a => a.ExamSchedule)
                    .OrderByDescending(a => a.AppliedAt)
                    .ToListAsync();

                var schedules = await context.ExamSchedules
                    .Include(s => s.Exam)
                    .Where(s => s.IsActive)
                    .ToListAsync();

                var universities = await context.Universities
                    .Include(u => u.Specialities)
                    .ToListAsync();

                var certificates = await context.Certificates
                    .Include(c => c.User)
                    .ToListAsync();

                var pendingCertificates = await context.Certificates
                    .Include(c => c.User)
                    .Where(c => c.Status == CertificateStatus.Pending)
                    .OrderBy(c => c.UploadedAt)
                    .ToListAsync();

                var universityApplications = await context.UniversityApplications
                    .Include(a => a.User)
                    .Include(a => a.University)
                    .Include(a => a.Speciality)
                    .OrderByDescending(a => a.AppliedAt)
                    .ToListAsync();

                var pendingUniversityApplications = await context.UniversityApplications
                    .Include(a => a.User)
                    .Include(a => a.University)
                    .Include(a => a.Speciality)
                    .Where(a => a.Status == UniversityApplicationStatus.Pending)
                    .OrderBy(a => a.AppliedAt)
                    .ToListAsync();

                var allUsers = await context.Users
                    .Where(u => u.Role == UserRole.Applicant)
                    .OrderBy(u => u.LastName)
                    .ThenBy(u => u.FirstName)
                    .ToListAsync();

                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    ExamApplications.Clear();
                    foreach (var app in applications)
                    {
                        ExamApplications.Add(app);
                    }
                    
                    ExamSchedules.Clear();
                    foreach (var sched in schedules)
                    {
                        ExamSchedules.Add(sched);
                    }
                    
                    Universities.Clear();
                    foreach (var univ in universities)
                    {
                        Universities.Add(univ);
                    }

                    Certificates.Clear();
                    foreach (var cert in certificates)
                    {
                        Certificates.Add(cert);
                    }

                    PendingCertificates.Clear();
                    foreach (var cert in pendingCertificates)
                    {
                        PendingCertificates.Add(cert);
                    }

                    AllCertificates.Clear();
                    foreach (var cert in certificates)
                    {
                        AllCertificates.Add(cert);
                    }

                    UniversityApplications.Clear();
                    foreach (var app in universityApplications)
                    {
                        UniversityApplications.Add(app);
                    }

                    PendingUniversityApplications.Clear();
                    foreach (var app in pendingUniversityApplications)
                    {
                        PendingUniversityApplications.Add(app);
                    }

                    AllUniversityApplications.Clear();
                    foreach (var app in universityApplications)
                    {
                        AllUniversityApplications.Add(app);
                    }

                    AllUsers.Clear();
                    foreach (var user in allUsers)
                    {
                        AllUsers.Add(user);
                    }

                });
            }

            await FilterCertificatesAsync();
            await FilterUniversityApplicationsAsync();
        }

        private async Task LoadApplicantDataAsync()
        {
            if (CurrentUser == null) return;

            using (var context = new Data.ApplicationDbContext())
            {
                var applications = await context.ExamApplications
                    .Include(a => a.Exam)
                    .Include(a => a.ExamSchedule)
                    .Where(a => a.UserId == CurrentUser.Id)
                    .OrderByDescending(a => a.AppliedAt)
                    .ToListAsync();

                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    ExamApplications.Clear();
                    foreach (var app in applications)
                    {
                        ExamApplications.Add(app);
                    }
                });

                var results = await context.ExamResults
                    .Include(r => r.Exam)
                    .Where(r => r.UserId == CurrentUser.Id)
                    .ToListAsync();

                var userApplicationScheduleIds = applications
                    .Where(a => a.ExamScheduleId > 0 && a.Status == Models.ApplicationStatus.Approved)
                    .Select(a => a.ExamScheduleId)
                    .Distinct()
                    .ToList();

                var schedules = await context.ExamSchedules
                    .Include(s => s.Exam)
                    .Where(s => s.IsActive && 
                               s.ExamDate >= DateTime.Today &&
                               userApplicationScheduleIds.Contains(s.Id))
                    .OrderBy(s => s.ExamDate)
                    .ThenBy(s => s.StartTime)
                    .ToListAsync();

                var universities = await context.Universities
                    .Include(u => u.Specialities)
                    .ToListAsync();

                var certificates = await context.Certificates
                    .Where(c => c.UserId == CurrentUser.Id)
                    .ToListAsync();

                var universityApplications = await context.UniversityApplications
                    .Include(a => a.University)
                    .Include(a => a.Speciality)
                    .Where(a => a.UserId == CurrentUser.Id)
                    .OrderByDescending(a => a.AppliedAt)
                    .ToListAsync();

                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    ExamResults.Clear();
                    foreach (var res in results)
                    {
                        ExamResults.Add(res);
                    }
                    
                    ExamSchedules.Clear();
                    foreach (var sched in schedules)
                    {
                        ExamSchedules.Add(sched);
                    }
                    
                    Universities.Clear();
                    foreach (var univ in universities)
                    {
                        Universities.Add(univ);
                    }

                    Certificates.Clear();
                    foreach (var cert in certificates)
                    {
                        Certificates.Add(cert);
                    }

                    UniversityApplications.Clear();
                    foreach (var app in universityApplications)
                    {
                        UniversityApplications.Add(app);
                    }
                });
            }
        }

        [RelayCommand]
        private void NavigateToView(string viewName)
        {
            CurrentView = viewName;
        }

        public async Task LoadMatchedUniversitiesAsync()
        {
            if (CurrentUser == null || IsAdmin) return;

            try
            {
                using (var context = new ApplicationDbContext())
                {
                    var matchingService = new UniversityMatchingService(context);
                    var matched = await matchingService.FindMatchingUniversitiesAsync(CurrentUser.Id);

                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        MatchedSpecialities.Clear();
                        foreach (var match in matched)
                        {
                            MatchedSpecialities.Add(match);
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при поиске подходящих университетов: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        private void Logout()
        {
            var result = MessageBox.Show("Вы уверены, что хотите выйти из системы?",
                "Подтверждение выхода", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                _authService.Logout();
                var mainWindow = new MainWindow();
                mainWindow.Show();
                Application.Current.Windows.OfType<DashboardWindow>().FirstOrDefault()?.Close();
            }
        }


        public async Task FindMatchingSpecialitiesForApplicantAsync()
        {
            if (CurrentUser == null || IsAdmin) return;

            try
            {
                using (var context = new ApplicationDbContext())
                {
                    var matchingService = new UniversityMatchingService(context);
                    var matched = await matchingService.FindMatchingUniversitiesAsync(CurrentUser.Id, null, true);

                    var matchedInfoList = new List<MatchedSpecialityInfo>();

                    foreach (var match in matched)
                    {
                        var budgetApplications = await context.UniversityApplications
                            .Where(a => a.SpecialityId == match.Speciality.Id && 
                                       a.Type == ApplicationType.Budget &&
                                       (a.Status == UniversityApplicationStatus.Pending || 
                                        a.Status == UniversityApplicationStatus.Accepted))
                            .CountAsync();

                        var paidApplications = await context.UniversityApplications
                            .Where(a => a.SpecialityId == match.Speciality.Id && 
                                       a.Type == ApplicationType.Paid &&
                                       (a.Status == UniversityApplicationStatus.Pending || 
                                        a.Status == UniversityApplicationStatus.Accepted))
                            .CountAsync();

                        var availableBudgetPlaces = (match.Speciality.BudgetPlaces ?? 0) - budgetApplications;
                        var availablePaidPlaces = (match.Speciality.PaidPlaces ?? 0) - paidApplications;

                        if (availableBudgetPlaces > 0 || availablePaidPlaces > 0)
                        {
                            matchedInfoList.Add(new MatchedSpecialityInfo
                            {
                                MatchedSpeciality = match,
                                AvailableBudgetPlaces = availableBudgetPlaces,
                                AvailablePaidPlaces = availablePaidPlaces,
                                CanApplyBudget = availableBudgetPlaces > 0 && match.AllRequirementsMet,
                                CanApplyPaid = availablePaidPlaces > 0 && match.AllRequirementsMet,
                                CanApplyBoth = availableBudgetPlaces > 0 && availablePaidPlaces > 0 && match.AllRequirementsMet
                            });
                        }
                    }

                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        MatchedSpecialities.Clear();
                        foreach (var info in matchedInfoList)
                        {
                            MatchedSpecialities.Add(info.MatchedSpeciality);
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при поиске специальностей: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public async Task SubmitUniversityApplicationAsync(Services.MatchedSpeciality matched, ApplicationType applicationType)
        {
            if (CurrentUser == null) return;

            try
            {
                using (var context = new ApplicationDbContext())
                {
                    var existingApplication = await context.UniversityApplications
                        .FirstOrDefaultAsync(a => a.UserId == CurrentUser.Id && 
                                                 a.SpecialityId == matched.Speciality.Id &&
                                                 a.Status != UniversityApplicationStatus.Rejected);

                    if (existingApplication != null)
                    {
                        MessageBox.Show("Вы уже подали заявку на эту специальность.", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    var application = new UniversityApplication
                    {
                        UserId = CurrentUser.Id,
                        UniversityId = matched.University.Id,
                        SpecialityId = matched.Speciality.Id,
                        Type = applicationType,
                        Status = UniversityApplicationStatus.Pending,
                        AppliedAt = DateTime.Now
                    };

                    context.UniversityApplications.Add(application);
                    await context.SaveChangesAsync();

                    MessageBox.Show($"Заявка на специальность '{matched.Speciality.Name}' ({matched.University.Name}) успешно подана!",
                        "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

                    await LoadDataAsync();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при подаче заявки: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public async Task ApproveCertificateAsync(Certificate certificate)
        {
            if (CurrentUser == null || !IsAdmin) return;

            try
            {
                using (var context = new ApplicationDbContext())
                {
                    var cert = await context.Certificates.FindAsync(certificate.Id);
                    if (cert == null) return;

                    cert.Status = CertificateStatus.Approved;
                    cert.ReviewedAt = DateTime.Now;
                    cert.ReviewedByUserId = CurrentUser.Id;
                    await context.SaveChangesAsync();

                    await LoadDataAsync();
                    await FilterCertificatesAsync();
                    MessageBox.Show("Сертификат одобрен.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public async Task RejectCertificateAsync(Certificate certificate, string reason)
        {
            if (CurrentUser == null || !IsAdmin) return;

            try
            {
                using (var context = new ApplicationDbContext())
                {
                    var cert = await context.Certificates.FindAsync(certificate.Id);
                    if (cert == null) return;

                    cert.Status = CertificateStatus.Rejected;
                    cert.RejectionReason = reason;
                    cert.ReviewedAt = DateTime.Now;
                    cert.ReviewedByUserId = CurrentUser.Id;
                    await context.SaveChangesAsync();

                    await LoadDataAsync();
                    await FilterCertificatesAsync();
                    MessageBox.Show("Сертификат отклонен.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public async Task ApproveUniversityApplicationAsync(UniversityApplication application)
        {
            if (CurrentUser == null || !IsAdmin) return;

            try
            {
                using (var context = new ApplicationDbContext())
                {
                    var app = await context.UniversityApplications
                        .Include(a => a.Speciality)
                        .FirstOrDefaultAsync(a => a.Id == application.Id);
                    if (app == null) return;

                    var speciality = app.Speciality;
                    var applicationType = app.Type;

                    var acceptedApplications = await context.UniversityApplications
                        .Where(a => a.SpecialityId == speciality.Id && 
                                   a.Type == applicationType &&
                                   a.Status == UniversityApplicationStatus.Accepted)
                        .CountAsync();

                    int? maxPlaces = applicationType == ApplicationType.Budget 
                        ? speciality.BudgetPlaces 
                        : speciality.PaidPlaces;

                    if (maxPlaces.HasValue && acceptedApplications >= maxPlaces.Value)
                    {
                        app.Status = UniversityApplicationStatus.Waitlisted;
                        MessageBox.Show("Все места заняты. Заявка переведена в лист ожидания.", 
                            "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        app.Status = UniversityApplicationStatus.Accepted;
                        MessageBox.Show("Заявка одобрена.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    }

                    app.ReviewedAt = DateTime.Now;
                    app.ReviewedByUserId = CurrentUser.Id;
                    await context.SaveChangesAsync();

                    await LoadDataAsync();
                    await FilterUniversityApplicationsAsync();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public async Task RejectUniversityApplicationAsync(UniversityApplication application, string reason)
        {
            if (CurrentUser == null || !IsAdmin) return;

            try
            {
                using (var context = new ApplicationDbContext())
                {
                    var app = await context.UniversityApplications.FindAsync(application.Id);
                    if (app == null) return;

                    app.Status = UniversityApplicationStatus.Rejected;
                    app.RejectionReason = reason;
                    app.ReviewedAt = DateTime.Now;
                    app.ReviewedByUserId = CurrentUser.Id;
                    await context.SaveChangesAsync();

                    await LoadDataAsync();
                    await FilterUniversityApplicationsAsync();
                    MessageBox.Show("Заявка отклонена.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        public async Task FilterCertificatesAsync()
        {
            using (var context = new ApplicationDbContext())
            {
                var query = context.Certificates
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

                var filtered = await query
                    .OrderByDescending(c => c.UploadedAt)
                    .ToListAsync();

                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    AllCertificates.Clear();
                    foreach (var cert in filtered)
                    {
                        AllCertificates.Add(cert);
                    }
                });
            }
        }

        [RelayCommand]
        public async Task FilterUniversityApplicationsAsync()
        {
            using (var context = new ApplicationDbContext())
            {
                var query = context.UniversityApplications
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

                var filtered = await query
                    .OrderByDescending(a => a.AppliedAt)
                    .ToListAsync();

                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    AllUniversityApplications.Clear();
                    foreach (var app in filtered)
                    {
                        AllUniversityApplications.Add(app);
                    }
                });
            }
        }
    }
}

