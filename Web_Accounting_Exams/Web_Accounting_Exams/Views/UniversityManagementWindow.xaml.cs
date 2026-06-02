using System.Windows;
using Microsoft.EntityFrameworkCore;
using Web_Accounting_Exams.Data;
using Web_Accounting_Exams.ViewModels;

namespace Web_Accounting_Exams.Views
{
    public partial class UniversityManagementWindow : Window
    {
        private readonly ApplicationDbContext _context;
        private readonly UniversityManagementViewModel _viewModel;

        public UniversityManagementWindow()
        {
            InitializeComponent();
            _context = new ApplicationDbContext();
            _viewModel = new UniversityManagementViewModel(_context);
            DataContext = _viewModel;
            LoadUniversities();
        }

        private class UniversityWithSelection
        {
            public Models.University University { get; set; } = null!;
            public bool IsSelected { get; set; }
        }

        private List<UniversityWithSelection> _universitiesWithSelection = new();

        private async void LoadUniversities()
        {
            try
            {
                await _viewModel.LoadUniversitiesAsync().ConfigureAwait(true);
                
                _universitiesWithSelection = _viewModel.Universities
                    .Select(u => new UniversityWithSelection { University = u, IsSelected = false })
                    .ToList();
                
                UniversitiesDataGrid.ItemsSource = null;
                UniversitiesDataGrid.ItemsSource = _universitiesWithSelection;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки университетов: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void AddButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new UniversityEditDialog();
            if (dialog.ShowDialog() == true && dialog.ResultUniversity != null)
            {
                try
                {
                    using (var context = new ApplicationDbContext())
                    {
                        context.Universities.Add(dialog.ResultUniversity);
                        await context.SaveChangesAsync().ConfigureAwait(true);
                    }
                    LoadUniversities();
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
            if (UniversitiesDataGrid.SelectedItem is UniversityWithSelection universityWrapper)
            {
                var university = universityWrapper.University;
                var dialog = new UniversityEditDialog(university);
                if (dialog.ShowDialog() == true && dialog.ResultUniversity != null)
                {
                    try
                    {
                        using (var context = new ApplicationDbContext())
                        {
                            context.Universities.Update(dialog.ResultUniversity);
                            await context.SaveChangesAsync().ConfigureAwait(true);
                        }
                        LoadUniversities();
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
                MessageBox.Show("Пожалуйста, выберите университет для редактирования.", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void ManageSpecialitiesButton_Click(object sender, RoutedEventArgs e)
        {
            if (UniversitiesDataGrid.SelectedItem is UniversityWithSelection universityWrapper)
            {
                var university = universityWrapper.University;
                var dialog = new SpecialityManagementWindow(_context, university);
                dialog.ShowDialog();
                LoadUniversities();
            }
            else
            {
                MessageBox.Show("Пожалуйста, выберите университет для управления специальностями.", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private async void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedUniversities = _universitiesWithSelection
                .Where(u => u.IsSelected)
                .Select(u => u.University)
                .ToList();

            if (selectedUniversities.Count == 0)
            {
                MessageBox.Show("Пожалуйста, выберите университет(ы) для удаления.", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var message = selectedUniversities.Count == 1
                ? $"Вы уверены, что хотите удалить университет '{selectedUniversities[0].Name}'?"
                : $"Вы уверены, что хотите удалить {selectedUniversities.Count} университет(ов)?";

            var result = MessageBox.Show(message, "Подтверждение удаления", 
                MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    foreach (var university in selectedUniversities)
                    {
                        _viewModel.SelectedUniversity = university;
                        await _viewModel.DeleteUniversityAsync();
                    }
                    LoadUniversities();
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

