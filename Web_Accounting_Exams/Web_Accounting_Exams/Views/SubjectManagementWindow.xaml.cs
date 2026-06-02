using System.Windows;
using Microsoft.EntityFrameworkCore;
using Web_Accounting_Exams.Data;
using Web_Accounting_Exams.Models;

namespace Web_Accounting_Exams.Views
{
    public partial class SubjectManagementWindow : Window
    {
        private readonly ApplicationDbContext _context;
        private readonly Speciality _speciality;

        public SubjectManagementWindow(ApplicationDbContext context, Speciality speciality)
        {
            InitializeComponent();
            _context = context;
            _speciality = speciality;
            TitleTextBlock.Text = $"Управление предметами - {speciality.Name}";
            LoadSubjects();
        }

        private class SubjectWithSelection
        {
            public SpecialitySubject Subject { get; set; } = null!;
            public bool IsSelected { get; set; }
        }

        private List<SubjectWithSelection> _subjectsWithSelection = new();

        private async void LoadSubjects()
        {
            try
            {
                var subjects = await _context.SpecialitySubjects
                    .Where(s => s.SpecialityId == _speciality.Id)
                    .ToListAsync();
                
                _subjectsWithSelection = subjects
                    .Select(s => new SubjectWithSelection { Subject = s, IsSelected = false })
                    .ToList();
                
                SubjectsDataGrid.ItemsSource = null;
                SubjectsDataGrid.ItemsSource = _subjectsWithSelection;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки предметов: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new SubjectEditDialog(_speciality);
            if (dialog.ShowDialog() == true && dialog.ResultSubject != null)
            {
                SaveSubject(dialog.ResultSubject);
            }
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            if (SubjectsDataGrid.SelectedItem is SubjectWithSelection subjectWrapper)
            {
                var subject = subjectWrapper.Subject;
                var dialog = new SubjectEditDialog(_speciality, subject);
                if (dialog.ShowDialog() == true && dialog.ResultSubject != null)
                {
                    SaveSubject(dialog.ResultSubject);
                }
            }
            else
            {
                MessageBox.Show("Пожалуйста, выберите предмет для редактирования.", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private async void SaveSubject(SpecialitySubject subject)
        {
            try
            {
                if (subject.Id == 0)
                {
                    _context.SpecialitySubjects.Add(subject);
                }
                else
                {
                    _context.SpecialitySubjects.Update(subject);
                }
                await _context.SaveChangesAsync();
                LoadSubjects();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedSubjects = _subjectsWithSelection
                .Where(s => s.IsSelected)
                .Select(s => s.Subject)
                .ToList();

            if (selectedSubjects.Count == 0)
            {
                MessageBox.Show("Пожалуйста, выберите предмет(ы) для удаления.", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var message = selectedSubjects.Count == 1
                ? $"Вы уверены, что хотите удалить предмет '{selectedSubjects[0].SubjectName}'?"
                : $"Вы уверены, что хотите удалить {selectedSubjects.Count} предметов?";

            var result = MessageBox.Show(message, "Подтверждение удаления", 
                MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    foreach (var subject in selectedSubjects)
                    {
                        _context.SpecialitySubjects.Remove(subject);
                    }
                    await _context.SaveChangesAsync();
                    LoadSubjects();
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

