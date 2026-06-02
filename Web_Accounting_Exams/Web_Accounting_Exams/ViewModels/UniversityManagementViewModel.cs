using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;
using System.Windows;
using Web_Accounting_Exams.Data;
using Web_Accounting_Exams.Models;

namespace Web_Accounting_Exams.ViewModels
{
    public partial class UniversityManagementViewModel : BaseViewModel
    {
        private readonly ApplicationDbContext _context;

        [ObservableProperty]
        private ObservableCollection<University> _universities = new();

        [ObservableProperty]
        private ObservableCollection<Speciality> _specialities = new();

        [ObservableProperty]
        private University? _selectedUniversity;

        [ObservableProperty]
        private Speciality? _selectedSpeciality;

        [ObservableProperty]
        private string _universityName = string.Empty;

        [ObservableProperty]
        private string _universityAddress = string.Empty;

        [ObservableProperty]
        private string _universityPhone = string.Empty;

        [ObservableProperty]
        private string _universityEmail = string.Empty;

        [ObservableProperty]
        private string _universityWebsite = string.Empty;

        [ObservableProperty]
        private string _universityDescription = string.Empty;

        [ObservableProperty]
        private string _specialityName = string.Empty;

        [ObservableProperty]
        private string _specialityCode = string.Empty;

        [ObservableProperty]
        private string _specialityDescription = string.Empty;

        [ObservableProperty]
        private int? _budgetPlaces;

        [ObservableProperty]
        private int? _paidPlaces;

        [ObservableProperty]
        private int? _passingScoreBudget;

        [ObservableProperty]
        private int? _passingScorePaid;

        [ObservableProperty]
        private bool _isActive = true;

        [ObservableProperty]
        private bool _isEditMode = false;

        [ObservableProperty]
        private bool _isSpecialityEditMode = false;

        public UniversityManagementViewModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [RelayCommand]
        public async Task LoadUniversitiesAsync()
        {
            try
            {
                List<University> universities;
                using (var context = new ApplicationDbContext())
                {
                    universities = await context.Universities
                        .Include(u => u.Specialities)
                        .ToListAsync()
                        .ConfigureAwait(false);
                }
                
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    Universities = new ObservableCollection<University>(universities);
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка загрузки университетов: {ex.Message}");
            }
        }

        [RelayCommand]
        private async Task LoadSpecialitiesAsync()
        {
            if (SelectedUniversity == null) return;

            var specialities = await _context.Specialities
                .Include(s => s.Subjects)
                .Where(s => s.UniversityId == SelectedUniversity.Id)
                .ToListAsync();
            Specialities = new ObservableCollection<Speciality>(specialities);
        }

        [RelayCommand]
        private void SelectUniversity(University university)
        {
            SelectedUniversity = university;
            UniversityName = university.Name;
            UniversityAddress = university.Address ?? string.Empty;
            UniversityPhone = university.Phone ?? string.Empty;
            UniversityEmail = university.Email ?? string.Empty;
            UniversityWebsite = university.Website ?? string.Empty;
            UniversityDescription = university.Description ?? string.Empty;
            IsActive = university.IsActive;
            IsEditMode = true;
            LoadSpecialitiesAsync();
        }

        [RelayCommand]
        private void ClearSelection()
        {
            SelectedUniversity = null;
            SelectedSpeciality = null;
            UniversityName = string.Empty;
            UniversityAddress = string.Empty;
            UniversityPhone = string.Empty;
            UniversityEmail = string.Empty;
            UniversityWebsite = string.Empty;
            UniversityDescription = string.Empty;
            IsActive = true;
            IsEditMode = false;
            IsSpecialityEditMode = false;
            Specialities.Clear();
        }

        [RelayCommand]
        private async Task SaveUniversityAsync()
        {
            if (string.IsNullOrWhiteSpace(UniversityName))
            {
                MessageBox.Show("Пожалуйста, введите название университета.", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using (var context = new ApplicationDbContext())
                {
                    if (IsEditMode && SelectedUniversity != null)
                    {
                        var universityToUpdate = await context.Universities.FindAsync(SelectedUniversity.Id);
                        if (universityToUpdate != null)
                        {
                            universityToUpdate.Name = UniversityName;
                            universityToUpdate.Address = UniversityAddress;
                            universityToUpdate.Phone = UniversityPhone;
                            universityToUpdate.Email = UniversityEmail;
                            universityToUpdate.Website = UniversityWebsite;
                            universityToUpdate.Description = UniversityDescription;
                            universityToUpdate.IsActive = IsActive;
                            context.Universities.Update(universityToUpdate);
                        }
                    }
                    else
                    {
                        var newUniversity = new University
                        {
                            Name = UniversityName,
                            Address = UniversityAddress,
                            Phone = UniversityPhone,
                            Email = UniversityEmail,
                            Website = UniversityWebsite,
                            Description = UniversityDescription,
                            IsActive = IsActive,
                            CreatedAt = DateTime.Now
                        };
                        context.Universities.Add(newUniversity);
                    }

                    await context.SaveChangesAsync().ConfigureAwait(false);
                }
                
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    MessageBox.Show("Университет успешно сохранен.", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    ClearSelection();
                });

                await LoadUniversitiesAsync();
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
        public async Task DeleteUniversityAsync()
        {
            if (SelectedUniversity == null) return;

            var result = MessageBox.Show(
                $"Вы уверены, что хотите удалить университет '{SelectedUniversity.Name}'?",
                "Подтверждение удаления", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    using (var context = new ApplicationDbContext())
                    {
                        var universityToDelete = await context.Universities.FindAsync(SelectedUniversity.Id);
                        if (universityToDelete != null)
                        {
                            context.Universities.Remove(universityToDelete);
                            await context.SaveChangesAsync().ConfigureAwait(false);
                        }
                    }
                    
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        MessageBox.Show("Университет удален.", "Успех",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                        ClearSelection();
                    });
                    
                    await LoadUniversitiesAsync();
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

        [RelayCommand]
        private void SelectSpeciality(Speciality speciality)
        {
            SelectedSpeciality = speciality;
            SpecialityName = speciality.Name;
            SpecialityCode = speciality.Code ?? string.Empty;
            SpecialityDescription = speciality.Description ?? string.Empty;
            BudgetPlaces = speciality.BudgetPlaces;
            PaidPlaces = speciality.PaidPlaces;
            PassingScoreBudget = speciality.PassingScoreBudget;
            PassingScorePaid = speciality.PassingScorePaid;
            IsSpecialityEditMode = true;
        }

        [RelayCommand]
        private async Task SaveSpecialityAsync()
        {
            if (SelectedUniversity == null || string.IsNullOrWhiteSpace(SpecialityName))
            {
                MessageBox.Show("Пожалуйста, выберите университет и введите название специальности.", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                if (IsSpecialityEditMode && SelectedSpeciality != null)
                {
                    SelectedSpeciality.Name = SpecialityName;
                    SelectedSpeciality.Code = SpecialityCode;
                    SelectedSpeciality.Description = SpecialityDescription;
                    SelectedSpeciality.BudgetPlaces = BudgetPlaces;
                    SelectedSpeciality.PaidPlaces = PaidPlaces;
                    SelectedSpeciality.PassingScoreBudget = PassingScoreBudget;
                    SelectedSpeciality.PassingScorePaid = PassingScorePaid;
                }
                else
                {
                    var newSpeciality = new Speciality
                    {
                        UniversityId = SelectedUniversity.Id,
                        Name = SpecialityName,
                        Code = SpecialityCode,
                        Description = SpecialityDescription,
                        BudgetPlaces = BudgetPlaces,
                        PaidPlaces = PaidPlaces,
                        PassingScoreBudget = PassingScoreBudget,
                        PassingScorePaid = PassingScorePaid,
                        IsActive = true,
                        CreatedAt = DateTime.Now
                    };
                    _context.Specialities.Add(newSpeciality);
                }

                await _context.SaveChangesAsync();
                MessageBox.Show("Специальность успешно сохранена.", "Успех",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                SelectedSpeciality = null;
                SpecialityName = string.Empty;
                SpecialityCode = string.Empty;
                SpecialityDescription = string.Empty;
                BudgetPlaces = null;
                PaidPlaces = null;
                PassingScoreBudget = null;
                PassingScorePaid = null;
                IsSpecialityEditMode = false;
                await LoadSpecialitiesAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        private async Task DeleteSpecialityAsync()
        {
            if (SelectedSpeciality == null) return;

            var result = MessageBox.Show(
                $"Вы уверены, что хотите удалить специальность '{SelectedSpeciality.Name}'?",
                "Подтверждение удаления", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    _context.Specialities.Remove(SelectedSpeciality);
                    await _context.SaveChangesAsync();
                    MessageBox.Show("Специальность удалена.", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    SelectedSpeciality = null;
                    await LoadSpecialitiesAsync();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при удалении: {ex.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}

