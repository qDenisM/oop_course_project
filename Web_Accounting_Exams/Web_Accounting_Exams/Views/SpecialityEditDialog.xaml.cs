using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Web_Accounting_Exams.Models;
using Web_Accounting_Exams.Helpers;

namespace Web_Accounting_Exams.Views
{
    public partial class SpecialityEditDialog : Window
    {
        private readonly University _university;
        private readonly Speciality? _speciality;
        public Speciality? ResultSpeciality { get; private set; }

        public SpecialityEditDialog(University university, Speciality? speciality = null)
        {
            InitializeComponent();
            _university = university;
            _speciality = speciality;

            NameTextBox.PreviewTextInput += InputValidationHelper.LettersOnly_PreviewTextInput;
            CodeTextBox.PreviewTextInput += CodeTextBox_PreviewTextInput;
            CodeTextBox.TextChanged += InputValidationHelper.SpecialtyCode_TextChanged;
            BudgetPlacesTextBox.PreviewTextInput += InputValidationHelper.NumericOnly_PreviewTextInput;
            PaidPlacesTextBox.PreviewTextInput += InputValidationHelper.NumericOnly_PreviewTextInput;
            PassingScoreBudgetTextBox.PreviewTextInput += InputValidationHelper.NumericOnly_PreviewTextInput;
            PassingScorePaidTextBox.PreviewTextInput += InputValidationHelper.NumericOnly_PreviewTextInput;

            if (_speciality != null)
            {
                NameTextBox.Text = _speciality.Name;
                CodeTextBox.Text = _speciality.Code ?? string.Empty;
                DescriptionTextBox.Text = _speciality.Description ?? string.Empty;
                BudgetPlacesTextBox.Text = _speciality.BudgetPlaces?.ToString() ?? string.Empty;
                PaidPlacesTextBox.Text = _speciality.PaidPlaces?.ToString() ?? string.Empty;
                PassingScoreBudgetTextBox.Text = _speciality.PassingScoreBudget?.ToString() ?? string.Empty;
                PassingScorePaidTextBox.Text = _speciality.PassingScorePaid?.ToString() ?? string.Empty;
                IsActiveCheckBox.IsChecked = _speciality.IsActive;
            }
        }

        private void CodeTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !System.Text.RegularExpressions.Regex.IsMatch(e.Text, @"^[\d\-]+$");
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(NameTextBox.Text))
            {
                MessageBox.Show("Пожалуйста, введите название специальности.", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                NameTextBox.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(CodeTextBox.Text))
            {
                MessageBox.Show("Пожалуйста, введите код специальности.", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                CodeTextBox.Focus();
                return;
            }

            if (!System.Text.RegularExpressions.Regex.IsMatch(CodeTextBox.Text, @"^\d-\d{2}-\d{4}-\d{2}$"))
            {
                MessageBox.Show("Пожалуйста, введите код специальности в формате 0-00-0000-00.", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                CodeTextBox.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(BudgetPlacesTextBox.Text) || !int.TryParse(BudgetPlacesTextBox.Text, out int budgetPlaces) || budgetPlaces <= 0)
            {
                MessageBox.Show("Пожалуйста, введите корректное количество бюджетных мест (положительное число).", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                BudgetPlacesTextBox.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(PaidPlacesTextBox.Text) || !int.TryParse(PaidPlacesTextBox.Text, out int paidPlaces) || paidPlaces <= 0)
            {
                MessageBox.Show("Пожалуйста, введите корректное количество платных мест (положительное число).", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                PaidPlacesTextBox.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(PassingScoreBudgetTextBox.Text) || !int.TryParse(PassingScoreBudgetTextBox.Text, out int passingScoreBudget) || passingScoreBudget < 0)
            {
                MessageBox.Show("Пожалуйста, введите корректный проходной балл для бюджета (неотрицательное число).", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                PassingScoreBudgetTextBox.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(PassingScorePaidTextBox.Text) || !int.TryParse(PassingScorePaidTextBox.Text, out int passingScorePaid) || passingScorePaid < 0)
            {
                MessageBox.Show("Пожалуйста, введите корректный проходной балл для платного обучения (неотрицательное число).", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                PassingScorePaidTextBox.Focus();
                return;
            }

            ResultSpeciality = _speciality ?? new Speciality();
            ResultSpeciality.UniversityId = _university.Id;
            ResultSpeciality.Name = NameTextBox.Text;
            ResultSpeciality.Code = CodeTextBox.Text;
            ResultSpeciality.Description = DescriptionTextBox.Text;
            ResultSpeciality.IsActive = IsActiveCheckBox.IsChecked ?? true;
            ResultSpeciality.BudgetPlaces = budgetPlaces;
            ResultSpeciality.PaidPlaces = paidPlaces;
            ResultSpeciality.PassingScoreBudget = passingScoreBudget;
            ResultSpeciality.PassingScorePaid = passingScorePaid;

            if (_speciality == null)
            {
                ResultSpeciality.CreatedAt = DateTime.Now;
            }

            DialogResult = true;
            Close();
        }
    }
}

