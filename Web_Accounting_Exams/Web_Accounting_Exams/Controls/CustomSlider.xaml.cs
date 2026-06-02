using System.Windows;
using System.Windows.Controls;

namespace Web_Accounting_Exams.Controls
{
    public partial class CustomSlider : UserControl
    {
        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register(nameof(Value), typeof(double), typeof(CustomSlider),
                new PropertyMetadata(0.0, OnValueChanged));

        public static readonly DependencyProperty MinimumProperty =
            DependencyProperty.Register(nameof(Minimum), typeof(double), typeof(CustomSlider),
                new PropertyMetadata(0.0));

        public static readonly DependencyProperty MaximumProperty =
            DependencyProperty.Register(nameof(Maximum), typeof(double), typeof(CustomSlider),
                new PropertyMetadata(100.0));

        public static readonly DependencyProperty TickFrequencyProperty =
            DependencyProperty.Register(nameof(TickFrequency), typeof(double), typeof(CustomSlider),
                new PropertyMetadata(1.0));

        public static readonly DependencyProperty LabelProperty =
            DependencyProperty.Register(nameof(Label), typeof(string), typeof(CustomSlider),
                new PropertyMetadata(null, OnLabelChanged));

        public double Value
        {
            get => (double)GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }

        public double Minimum
        {
            get => (double)GetValue(MinimumProperty);
            set => SetValue(MinimumProperty, value);
        }

        public double Maximum
        {
            get => (double)GetValue(MaximumProperty);
            set => SetValue(MaximumProperty, value);
        }

        public double TickFrequency
        {
            get => (double)GetValue(TickFrequencyProperty);
            set => SetValue(TickFrequencyProperty, value);
        }

        public string? Label
        {
            get => (string?)GetValue(LabelProperty);
            set => SetValue(LabelProperty, value);
        }

        public CustomSlider()
        {
            InitializeComponent();
            this.Loaded += CustomSlider_Loaded;
        }

        private void CustomSlider_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateLabelVisibility();
        }

        private void UpdateLabelVisibility()
        {
            if (LabelTextBlock != null)
            {
                LabelTextBlock.Visibility = string.IsNullOrWhiteSpace(Label) ? Visibility.Collapsed : Visibility.Visible;
            }
        }

        private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is CustomSlider slider)
            {
                slider.ValueChanged?.Invoke(slider, new RoutedPropertyChangedEventArgs<double>(
                    (double)e.OldValue, (double)e.NewValue));
            }
        }

        private static void OnLabelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is CustomSlider slider)
            {
                slider.UpdateLabelVisibility();
            }
        }

        private void SliderControl_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Value = e.NewValue;
        }

        public event RoutedPropertyChangedEventHandler<double>? ValueChanged;
    }
}

