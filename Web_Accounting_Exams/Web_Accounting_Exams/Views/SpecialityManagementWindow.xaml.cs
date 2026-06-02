using System.Windows;
using Microsoft.EntityFrameworkCore;
using Web_Accounting_Exams.Data;
using Web_Accounting_Exams.Models;

namespace Web_Accounting_Exams.Views
{
    public partial class SpecialityManagementWindow : Window
    {
        private readonly ApplicationDbContext _context;
        private readonly University _university;

        public SpecialityManagementWindow(ApplicationDbContext context, University university)
        {
            InitializeComponent();
            _context = context;
            _university = university;
            TitleTextBlock.Text = $"Управление специальностями - {university.Name}";
            LoadSpecialities();
        }

        private class SpecialityWithSelection
        {
            public Speciality Speciality { get; set; } = null!;
            public bool IsSelected { get; set; }
        }

        private List<SpecialityWithSelection> _specialitiesWithSelection = new();

        private async void LoadSpecialities()
        {
            try
            {
                var specialities = await _context.Specialities
                    .Where(s => s.UniversityId == _university.Id)
                    .ToListAsync();
                
                _specialitiesWithSelection = specialities
                    .Select(s => new SpecialityWithSelection { Speciality = s, IsSelected = false })
                    .ToList();
                
                SpecialitiesDataGrid.ItemsSource = null;
                SpecialitiesDataGrid.ItemsSource = _specialitiesWithSelection;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки специальностей: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new SpecialityEditDialog(_university);
            if (dialog.ShowDialog() == true && dialog.ResultSpeciality != null)
            {
                SaveSpeciality(dialog.ResultSpeciality);
            }
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            if (SpecialitiesDataGrid.SelectedItem is SpecialityWithSelection specialityWrapper)
            {
                var speciality = specialityWrapper.Speciality;
                var dialog = new SpecialityEditDialog(_university, speciality);
                if (dialog.ShowDialog() == true && dialog.ResultSpeciality != null)
                {
                    SaveSpeciality(dialog.ResultSpeciality);
                }
            }
            else
            {
                MessageBox.Show("Пожалуйста, выберите специальность для редактирования.", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private async void SaveSpeciality(Speciality speciality)
        {
            try
            {
                if (speciality.Id == 0)
                {
                    _context.Specialities.Add(speciality);
                }
                else
                {
                    _context.Specialities.Update(speciality);
                }
                await _context.SaveChangesAsync();
                LoadSpecialities();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ManageSubjectsButton_Click(object sender, RoutedEventArgs e)
        {
            if (SpecialitiesDataGrid.SelectedItem is SpecialityWithSelection specialityWrapper)
            {
                var speciality = specialityWrapper.Speciality;
                var dialog = new SubjectManagementWindow(_context, speciality);
                dialog.ShowDialog();
                LoadSpecialities();
            }
            else
            {
                MessageBox.Show("Пожалуйста, выберите специальность для управления предметами.", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private async void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedSpecialities = _specialitiesWithSelection
                .Where(s => s.IsSelected)
                .Select(s => s.Speciality)
                .ToList();

            if (selectedSpecialities.Count == 0)
            {
                MessageBox.Show("Пожалуйста, выберите специальность(и) для удаления.", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var message = selectedSpecialities.Count == 1
                ? $"Вы уверены, что хотите удалить специальность '{selectedSpecialities[0].Name}'?"
                : $"Вы уверены, что хотите удалить {selectedSpecialities.Count} специальностей?";

            var result = MessageBox.Show(message, "Подтверждение удаления", 
                MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    foreach (var speciality in selectedSpecialities)
                    {
                        _context.Specialities.Remove(speciality);
                    }
                    await _context.SaveChangesAsync();
                    LoadSpecialities();
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

