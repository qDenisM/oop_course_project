using System.Windows;
using System.Windows.Input;

namespace Web_Accounting_Exams.Views
{
    public partial class InputDialog : Window
    {
        public string ResponseText { get; set; } = string.Empty;

        public InputDialog(string prompt, string title, string defaultValue = "")
        {
            InitializeComponent();
            Title = title;
            PromptTextBlock.Text = prompt;
            ResponseText = defaultValue;
            InputTextBox.Text = defaultValue;
            InputTextBox.Focus();
            InputTextBox.SelectAll();
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            ResponseText = InputTextBox.Text;
            DialogResult = true;
        }

        private void InputTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                OkButton_Click(sender, e);
            }
        }
    }
}

