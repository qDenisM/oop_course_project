using System.Windows;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using Web_Accounting_Exams.Data;
using Web_Accounting_Exams.Models;
using Web_Accounting_Exams.ViewModels;
using Web_Accounting_Exams.Services;
using Web_Accounting_Exams.Helpers;

namespace Web_Accounting_Exams.Views
{
    public partial class ExamManagementWindow : Window
    {
        private readonly ApplicationDbContext _context;
        private readonly ExamManagementViewModel _viewModel;
        private readonly EmailService _emailService;

        public ExamManagementWindow()
        {
            InitializeComponent();
            _context = new ApplicationDbContext();
            _emailService = new EmailService();
            _viewModel = new ExamManagementViewModel(_context, _emailService);
            DataContext = _viewModel;
            
            if (ScoreTextBox != null)
            {
                ScoreTextBox.PreviewTextInput += InputValidationHelper.NumericOnly_PreviewTextInput;
            }
            
            LoadExams();
        }

        private class ExamWithSelection
        {
            public Models.Exam Exam { get; set; } = null!;
            public bool IsSelected { get; set; }
        }

        private List<ExamWithSelection> _examsWithSelection = new();

        private async void LoadExams()
        {
            try
            {
                await _viewModel.LoadExamsAsync().ConfigureAwait(true);
                
                _examsWithSelection = _viewModel.Exams
                    .Select(e => new ExamWithSelection { Exam = e, IsSelected = false })
                    .ToList();
                
                ExamsDataGrid.ItemsSource = null;
                ExamsDataGrid.ItemsSource = _examsWithSelection;
                ExamsDataGrid.Items.Refresh();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки экзаменов: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void AddButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new ExamEditDialog();
            if (dialog.ShowDialog() == true && dialog.ResultExam != null)
            {
                try
                {
                    using (var context = new ApplicationDbContext())
                    {
                        context.Exams.Add(dialog.ResultExam);
                        await context.SaveChangesAsync().ConfigureAwait(true);
                    }
                    LoadExams();
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
            if (ExamsDataGrid.SelectedItem is ExamWithSelection examWrapper)
            {
                var exam = examWrapper.Exam;
                var examCopy = new Exam
                {
                    Id = exam.Id,
                    Name = exam.Name,
                    Subject = exam.Subject,
                    Description = exam.Description,
                    DurationMinutes = exam.DurationMinutes,
                    MaxScore = exam.MaxScore,
                    IsActive = exam.IsActive,
                    CreatedAt = exam.CreatedAt
                };
                
                var dialog = new ExamEditDialog(examCopy);
                if (dialog.ShowDialog() == true && dialog.ResultExam != null)
                {
                    try
                    {
                        using (var context = new ApplicationDbContext())
                        {
                            var examToUpdate = await context.Exams.FindAsync(exam.Id);
                            if (examToUpdate != null)
                            {
                                examToUpdate.Name = dialog.ResultExam.Name;
                                examToUpdate.Subject = dialog.ResultExam.Subject;
                                examToUpdate.Description = dialog.ResultExam.Description;
                                examToUpdate.DurationMinutes = dialog.ResultExam.DurationMinutes;
                                examToUpdate.MaxScore = dialog.ResultExam.MaxScore;
                                examToUpdate.IsActive = dialog.ResultExam.IsActive;
                                context.Exams.Update(examToUpdate);
                                await context.SaveChangesAsync().ConfigureAwait(true);
                            }
                        }
                        LoadExams();
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
                MessageBox.Show("Пожалуйста, выберите экзамен для редактирования.", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private async void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedExams = _examsWithSelection
                .Where(e => e.IsSelected)
                .Select(e => e.Exam)
                .ToList();

            if (selectedExams.Count == 0)
            {
                MessageBox.Show("Пожалуйста, выберите экзамен(ы) для удаления.", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var message = selectedExams.Count == 1
                ? $"Вы уверены, что хотите удалить экзамен '{selectedExams[0].Name}'?"
                : $"Вы уверены, что хотите удалить {selectedExams.Count} экзамен(ов)?";

            var result = MessageBox.Show(message, "Подтверждение удаления", 
                MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    foreach (var exam in selectedExams)
                    {
                        _viewModel.SelectedExam = exam;
                        await _viewModel.DeleteExamAsync();
                    }
                    LoadExams();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка удаления: {ex.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ExamsDataGrid_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (ExamsDataGrid.SelectedItem is ExamWithSelection examWrapper)
            {
                _viewModel.SelectedExamForResult = examWrapper.Exam;
                ExamComboBox.SelectedItem = examWrapper.Exam;
            }
            else
            {
                _viewModel.SelectedExamForResult = null;
                ExamComboBox.SelectedItem = null;
            }
        }

        private void ExamComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (ExamComboBox.SelectedItem is Exam exam)
            {
                _viewModel.SelectedExamForResult = exam;
            }
        }
    }
}

