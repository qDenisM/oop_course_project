using System.Linq;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Data;
using Microsoft.EntityFrameworkCore;
using MaterialDesignThemes.Wpf;
using Web_Accounting_Exams.Data;
using Web_Accounting_Exams.Models;
using Web_Accounting_Exams.ViewModels;
using Web_Accounting_Exams.Views;
using Web_Accounting_Exams.Converters;
using Web_Accounting_Exams.Helpers;

namespace Web_Accounting_Exams
{
    public class ApplicationWithSelection
    {
        public Models.ExamApplication Application { get; set; } = null!;
        public bool IsSelected { get; set; }
    }

    public class CertificateWithSelection
    {
        public Models.Certificate Certificate { get; set; } = null!;
        public bool IsSelected { get; set; }
    }

    public class UniversityApplicationWithSelection
    {
        public Models.UniversityApplication Application { get; set; } = null!;
        public bool IsSelected { get; set; }
    }

    public class ResultItem
    {
        public string Type { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public int Score { get; set; }
        public int? MaxScore { get; set; }
        public string? Number { get; set; }
        public DateTime Date { get; set; }
        public string DateLabel { get; set; } = string.Empty;
        public object? OriginalItem { get; set; }
        public bool CanDelete { get; set; }
        public Models.CertificateStatus? Status { get; set; }
    }

    public partial class DashboardWindow : Window
    {
        private string _userName;
        private string _userType;
        private string _userInitial;

        private DashboardViewModel? _viewModel;
        private Services.AuthenticationService? _authService;
        private int _selectedTabIndex = 0;
        private TabControl? _adminTabControl;

        public DashboardWindow(Services.AuthenticationService? authService = null, string userName = "Пользователь", string userType = "Абитуриент")
        {
            InitializeComponent();

            if (authService == null)
            {
                var context = new Data.ApplicationDbContext();
                _authService = new Services.AuthenticationService(context);
            }
            else
            {
                _authService = authService;
            }
            
            _viewModel = new ViewModels.DashboardViewModel(_authService);
            DataContext = _viewModel;

            if (_viewModel.CurrentUser != null)
            {
                UpdateUserInfoFromViewModel();
                ApplyRoleBasedColors(_viewModel.IsAdmin);
            }
            else
            {
                _userName = userName;
                _userType = userType;
                _userInitial = string.IsNullOrEmpty(userName) ? "П" : userName[0].ToString().ToUpper();
                InitializeUserInfo();
            }

            LoadCTApplicationsContent();
        }

        private void UpdateUserInfoFromViewModel()
        {
            if (_viewModel?.CurrentUser != null)
            {
                UserNameTextBlock.Text = _viewModel.CurrentUser.FullName;
                UserTypeTextBlock.Text = _viewModel.IsAdmin ? "Администратор" : "Абитуриент";
                UserInitialTextBlock.Text = string.IsNullOrEmpty(_viewModel.CurrentUser.FirstName) 
                    ? "П" 
                    : _viewModel.CurrentUser.FirstName[0].ToString().ToUpper();
                SetAvatarColor(_viewModel.IsAdmin);
                ApplyRoleBasedColors(_viewModel.IsAdmin);
                
                UserNameTextBlock.GetBindingExpression(TextBlock.TextProperty)?.UpdateTarget();
                UserTypeTextBlock.GetBindingExpression(TextBlock.TextProperty)?.UpdateTarget();
            }
        }

        private void SetAvatarColor(bool isAdmin)
        {
            LinearGradientBrush gradient = new LinearGradientBrush();
            gradient.StartPoint = new Point(0, 0);
            gradient.EndPoint = new Point(1, 1);

            if (isAdmin)
            {
                gradient.GradientStops.Add(new GradientStop(Color.FromRgb(231, 76, 60), 0));
                gradient.GradientStops.Add(new GradientStop(Color.FromRgb(192, 57, 43), 1));
            }
            else
            {
                gradient.GradientStops.Add(new GradientStop(Color.FromRgb(102, 126, 234), 0));
                gradient.GradientStops.Add(new GradientStop(Color.FromRgb(118, 75, 162), 1));
            }

            ((Border)UserInitialTextBlock.Parent).Background = gradient;
        }

        private void ApplyRoleBasedColors(bool isAdmin)
        {
            var accentColor = isAdmin 
                ? new SolidColorBrush(Color.FromRgb(231, 76, 60))
                : new SolidColorBrush(Color.FromRgb(102, 126, 234));

            Resources["AccentColor"] = accentColor;
            Resources["AccentColorBrush"] = accentColor;
        }

        private Color GetAccentColor(bool isAdmin)
        {
            return isAdmin 
                ? Color.FromRgb(231, 76, 60)
                : Color.FromRgb(102, 126, 234);
        }

        private SolidColorBrush GetAccentBrush(bool isAdmin)
        {
            return new SolidColorBrush(GetAccentColor(isAdmin));
        }

        private void InitializeUserInfo()
        {
            UserNameTextBlock.Text = _userName;
            UserTypeTextBlock.Text = _userType;
            UserInitialTextBlock.Text = _userInitial;

            SetAvatarColor();
        }

        private void SetAvatarColor()
        {
            LinearGradientBrush gradient = new LinearGradientBrush();
            gradient.StartPoint = new Point(0, 0);
            gradient.EndPoint = new Point(1, 1);

            switch (_userType.ToLower())
            {
                case "абитуриент":
                    gradient.GradientStops.Add(new GradientStop(Color.FromRgb(102, 126, 234), 0));
                    gradient.GradientStops.Add(new GradientStop(Color.FromRgb(118, 75, 162), 1));
                    break;
                case "студент":
                    gradient.GradientStops.Add(new GradientStop(Color.FromRgb(0, 184, 148), 0));
                    gradient.GradientStops.Add(new GradientStop(Color.FromRgb(0, 206, 201), 1));
                    break;
                default:
                    gradient.GradientStops.Add(new GradientStop(Color.FromRgb(253, 121, 168), 0));
                    gradient.GradientStops.Add(new GradientStop(Color.FromRgb(253, 203, 110), 1));
                    break;
            }

            ((Border)UserInitialTextBlock.Parent).Background = gradient;
        }

        private void SetActiveButton(Button activeButton)
        {
            CTApplicationsButton.Style = (Style)FindResource("MenuButtonStyle");
            ExamsButton.Style = (Style)FindResource("MenuButtonStyle");
            ScheduleButton.Style = (Style)FindResource("MenuButtonStyle");
            UniversitiesButton.Style = (Style)FindResource("MenuButtonStyle");
            SettingsButton.Style = (Style)FindResource("MenuButtonStyle");

            activeButton.Style = (Style)FindResource("ActiveMenuButtonStyle");
        }

        private void CTApplications_Click(object sender, RoutedEventArgs e)
        {
            SetActiveButton(CTApplicationsButton);
            PageTitleTextBlock.Text = "Подача на ЦТ";
            LoadCTApplicationsContent();
        }

        private void Exams_Click(object sender, RoutedEventArgs e)
        {
            SetActiveButton(ExamsButton);
            PageTitleTextBlock.Text = "Экзамены";
            LoadExamsContent();
        }

        private async void Schedule_Click(object sender, RoutedEventArgs e)
        {
            SetActiveButton(ScheduleButton);
            PageTitleTextBlock.Text = _viewModel?.IsAdmin == true ? "Управление расписанием" : "Мое расписание экзаменов";
            var content = CreateScheduleContent();
            MainContentPresenter.Content = content;
        }

        private async void Universities_Click(object sender, RoutedEventArgs e)
        {
            SetActiveButton(UniversitiesButton);
            PageTitleTextBlock.Text = "Университеты";
            var content = await CreateUniversitiesContentAsync();
            MainContentPresenter.Content = content;
        }

        private void Settings_Click(object sender, RoutedEventArgs e)
        {
            SetActiveButton(SettingsButton);
            PageTitleTextBlock.Text = "Настройки";
            LoadSettingsContent();
        }

        private void Logout_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Вы уверены, что хотите выйти из системы?",
                                        "Подтверждение выхода",
                                        MessageBoxButton.YesNo,
                                        MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                MainWindow mainWindow = new MainWindow();
                mainWindow.Show();
                this.Close();
            }
        }

        private void LoadCTApplicationsContent()
        {
            var content = CreateCTApplicationsContent();
            MainContentPresenter.Content = content;
        }

        private async void LoadExamsContent()
        {
            var content = await CreateExamsContentAsync();
            MainContentPresenter.Content = content;
        }

        private void LoadScheduleContent()
        {
            var content = CreateScheduleContent();
            MainContentPresenter.Content = content;
        }

        private async void LoadUniversitiesContent()
        {
            var content = await CreateUniversitiesContentAsync();
            MainContentPresenter.Content = content;
        }

        private void LoadSettingsContent()
        {
            var content = CreateSettingsContent();
            MainContentPresenter.Content = content;
        }

        private FrameworkElement CreateCTApplicationsContent()
        {
            if (_viewModel is null) return new TextBlock { Text = "Ошибка загрузки данных" };

            var scrollViewer = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            var stackPanel = new StackPanel { Margin = new Thickness(30) };

            if (_viewModel.IsAdmin)
            {
                var titleText = new TextBlock
                {
                    Text = "Управление заявками и сертификатами",
                    FontSize = 24,
                    FontWeight = FontWeights.Bold,
                    Margin = new Thickness(0, 0, 0, 20)
                };
                stackPanel.Children.Add(titleText);

                _adminTabControl = new TabControl
                {
                    Margin = new Thickness(0, 0, 0, 20)
                };

                var examApplicationsTab = new TabItem
                {
                    Header = "Заявки на экзамены"
                };
                examApplicationsTab.Content = CreateExamApplicationsTabContent();
                _adminTabControl.Items.Add(examApplicationsTab);

                var certificatesTab = new TabItem
                {
                    Header = "Рассмотрение сертификатов"
                };
                certificatesTab.Content = CreateCertificatesReviewTabContent();
                _adminTabControl.Items.Add(certificatesTab);

                var universityApplicationsTab = new TabItem
                {
                    Header = "Заявки в университеты"
                };
                universityApplicationsTab.Content = CreateUniversityApplicationsReviewTabContent();
                _adminTabControl.Items.Add(universityApplicationsTab);

                if (_selectedTabIndex >= 0 && _selectedTabIndex < _adminTabControl.Items.Count)
                {
                    _adminTabControl.SelectedIndex = _selectedTabIndex;
                }

                _adminTabControl.SelectionChanged += (s, e) =>
                {
                    if (s is TabControl tc && tc.SelectedIndex >= 0)
                    {
                        _selectedTabIndex = tc.SelectedIndex;
                    }
                };

                stackPanel.Children.Add(_adminTabControl);
            }
            else
            {
                var statsGrid = new Grid();
                statsGrid.ColumnDefinitions.Add(new ColumnDefinition());
                statsGrid.ColumnDefinitions.Add(new ColumnDefinition());
                statsGrid.ColumnDefinitions.Add(new ColumnDefinition());
                statsGrid.Margin = new Thickness(0, 0, 0, 30);

                var userApps = _viewModel.ExamApplications
                    .Where(a => a.UserId == _viewModel.CurrentUser?.Id)
                    .OrderByDescending(a => a.AppliedAt)
                    .ToList();
                var pendingCount = userApps.Count(a => a.Status == Models.ApplicationStatus.Pending);
                var approvedCount = userApps.Count(a => a.Status == Models.ApplicationStatus.Approved);
                var totalCount = userApps.Count;

                var applicationsCard = CreateStatsCard("Поданные заявки", totalCount.ToString(), "#667eea", "FileDocumentEdit");
                Grid.SetColumn(applicationsCard, 0);
                statsGrid.Children.Add(applicationsCard);

                var processingCard = CreateStatsCard("В процессе", pendingCount.ToString(), "#00B894", "Clock");
                Grid.SetColumn(processingCard, 1);
                statsGrid.Children.Add(processingCard);

                var acceptedCard = CreateStatsCard("Принятые", approvedCount.ToString(), "#00CEC9", "CheckCircle");
                Grid.SetColumn(acceptedCard, 2);
                statsGrid.Children.Add(acceptedCard);

                stackPanel.Children.Add(statsGrid);

                var newApplicationButton = new Button();
                newApplicationButton.Content = "Подать новую заявку на ЦТ";
                newApplicationButton.Style = (Style)FindResource("MaterialDesignRaisedButton");
                newApplicationButton.Background = new SolidColorBrush(Color.FromRgb(102, 126, 234));
                newApplicationButton.Height = 50;
                newApplicationButton.FontSize = 16;
                newApplicationButton.FontWeight = FontWeights.SemiBold;
                newApplicationButton.Margin = new Thickness(0, 0, 0, 30);
                newApplicationButton.Click += (s, e) => OpenNewApplicationDialog();
                stackPanel.Children.Add(newApplicationButton);

                var applicationsListTextBlock = new TextBlock();
                applicationsListTextBlock.Text = "Ваши заявки";
                applicationsListTextBlock.FontSize = 18;
                applicationsListTextBlock.FontWeight = FontWeights.SemiBold;
                applicationsListTextBlock.Margin = new Thickness(0, 0, 0, 15);
                stackPanel.Children.Add(applicationsListTextBlock);

                var wrapPanel = new WrapPanel
                {
                    Orientation = Orientation.Horizontal,
                    Margin = new Thickness(0)
                };
                
                foreach (var app in userApps)
                {
                    var card = CreateApplicationCard(app);
                    card.Width = 400;
                    card.Margin = new Thickness(0, 0, 15, 15);
                    wrapPanel.Children.Add(card);
                }
                
                stackPanel.Children.Add(wrapPanel);
            }

            scrollViewer.Content = stackPanel;
            return scrollViewer;
        }

        private async void OpenNewApplicationDialog()
        {
            if (_viewModel?.CurrentUser == null) return;

            Exam? selectedExam = null;
            ExamSchedule? selectedSchedule = null;
            string? notes = null;

            try
            {
                var dialog = new Views.NewApplicationDialog(null, _viewModel.CurrentUser.Id);
                var dialogResult = dialog.ShowDialog();
                
                if (dialogResult == true && dialog.SelectedExam != null && dialog.SelectedSchedule != null)
                {
                    selectedExam = dialog.SelectedExam;
                    selectedSchedule = dialog.SelectedSchedule;
                    notes = dialog.Notes;
                }
                else
                {
                    return;
                }

                if (selectedExam != null && selectedSchedule != null)
                {
                    using (var saveContext = new Data.ApplicationDbContext())
                    {
                        var existingApplication = await saveContext.ExamApplications
                            .FirstOrDefaultAsync(a => a.UserId == _viewModel.CurrentUser.Id &&
                                                     a.ExamId == selectedExam.Id &&
                                                     a.Status != Models.ApplicationStatus.Rejected);

                        if (existingApplication != null)
                        {
                            MessageBox.Show("Вы уже подали заявку на этот экзамен.", "Ошибка",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }

                        var schedule = await saveContext.ExamSchedules
                            .FindAsync(selectedSchedule.Id);

                        if (schedule == null)
                        {
                            MessageBox.Show("Расписание не найдено.", "Ошибка",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }

                        var approvedCount = await saveContext.ExamApplications
                            .CountAsync(a => a.ExamScheduleId == schedule.Id && 
                                           a.Status == Models.ApplicationStatus.Approved);
                        
                        if (approvedCount >= schedule.MaxParticipants)
                        {
                            MessageBox.Show("Нет свободных мест на этот экзамен.", "Ошибка",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }

                        var application = new Models.ExamApplication
                        {
                            UserId = _viewModel.CurrentUser.Id,
                            ExamId = selectedExam.Id,
                            ExamScheduleId = schedule.Id,
                            Status = Models.ApplicationStatus.Pending,
                            Notes = notes,
                            AppliedAt = DateTime.Now
                        };

                        saveContext.ExamApplications.Add(application);
                        
                        try
                        {
                            var savedCount = await saveContext.SaveChangesAsync();
                            
                            if (savedCount > 0)
                            {
                                var savedApplication = await saveContext.ExamApplications
                                    .Include(a => a.Exam)
                                    .Include(a => a.ExamSchedule)
                                    .FirstOrDefaultAsync(a => a.Id == application.Id);
                                
                                if (savedApplication != null)
                                {
                                    if (_viewModel is not null)
                                    {
                                        await _viewModel.LoadDataAsync();
                                    }
                                    
                                    await Dispatcher.InvokeAsync(() =>
                                    {
                                        var newContent = CreateCTApplicationsContent();
                                        MainContentPresenter.Content = newContent;
                                        
                                        MessageBox.Show("Заявка успешно подана!", "Успех",
                                            MessageBoxButton.OK, MessageBoxImage.Information);
                                    });
                                }
                                else
                                {
                                    await Dispatcher.InvokeAsync(() =>
                                    {
                                        MessageBox.Show("Заявка не была найдена после сохранения. Попробуйте обновить список.", "Предупреждение",
                                            MessageBoxButton.OK, MessageBoxImage.Warning);
                                    });
                                }
                            }
                            else
                            {
                                await Dispatcher.InvokeAsync(() =>
                                {
                                    MessageBox.Show("Не удалось сохранить заявку. Попробуйте еще раз.", "Ошибка",
                                        MessageBoxButton.OK, MessageBoxImage.Warning);
                                });
                            }
                        }
                        catch (Exception saveEx)
                        {
                            await Dispatcher.InvokeAsync(() =>
                            {
                                var errorMessage = $"Ошибка при сохранении заявки: {saveEx.Message}";
                                if (saveEx.InnerException != null)
                                {
                                    errorMessage += $"\n\nДетали: {saveEx.InnerException.Message}";
                                }
                                if (saveEx.StackTrace != null)
                                {
                                    errorMessage += $"\n\nStack Trace: {saveEx.StackTrace}";
                                }
                                MessageBox.Show(errorMessage, "Ошибка",
                                    MessageBoxButton.OK, MessageBoxImage.Error);
                            });
                        }
                    }
                }
                else
                {
                    MessageBox.Show("Не выбраны экзамен или расписание.", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при подаче заявки: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private FrameworkElement CreateExamApplicationsTabContent()
        {
            if (_viewModel is null) return new TextBlock { Text = "Ошибка загрузки данных" };

            var scrollViewer = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            var stackPanel = new StackPanel { Margin = new Thickness(30) };

            var filterPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 0, 0, 20)
            };

            var filterLabel = new TextBlock 
            { 
                Text = "Фильтры:", 
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 10, 0)
            };
            filterPanel.Children.Add(filterLabel);
            
            var statusFilterComboBox = new ComboBox
            {
                Width = 180,
                Height = 35,
                Margin = new Thickness(0, 0, 10, 0)
            };
            statusFilterComboBox.Items.Add("Все статусы");
            statusFilterComboBox.Items.Add("Ожидают");
            statusFilterComboBox.Items.Add("Одобрены");
            statusFilterComboBox.Items.Add("Отклонены");
            statusFilterComboBox.SelectedIndex = 0;
            filterPanel.Children.Add(statusFilterComboBox);
            
            var applicantFilterTextBox = new TextBox
            {
                Width = 200,
                Height = 35,
                Margin = new Thickness(0, 0, 10, 0)
            };
            MaterialDesignThemes.Wpf.HintAssist.SetHint(applicantFilterTextBox, "Фильтр по абитуриенту...");
            filterPanel.Children.Add(applicantFilterTextBox);
            
            var examFilterTextBox = new TextBox
            {
                Width = 200,
                Height = 35,
                Margin = new Thickness(0, 0, 10, 0)
            };
            MaterialDesignThemes.Wpf.HintAssist.SetHint(examFilterTextBox, "Фильтр по экзамену...");
            filterPanel.Children.Add(examFilterTextBox);
            
            stackPanel.Children.Add(filterPanel);

            var applicationsDataGrid = new System.Windows.Controls.DataGrid
            {
                AutoGenerateColumns = false,
                CanUserAddRows = false,
                CanUserDeleteRows = false,
                SelectionMode = DataGridSelectionMode.Single,
                HeadersVisibility = DataGridHeadersVisibility.All,
                GridLinesVisibility = DataGridGridLinesVisibility.All,
                AlternatingRowBackground = new SolidColorBrush(Color.FromRgb(249, 250, 251)),
                RowBackground = Brushes.White,
                Background = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(229, 231, 235)),
                BorderThickness = new Thickness(1),
                Margin = new Thickness(0, 0, 0, 20),
                FontSize = 14,
                Height = 400,
                VerticalContentAlignment = VerticalAlignment.Center,
                RowHeight = 50
            };
            
            var applicationsCellStyle = new Style(typeof(System.Windows.Controls.DataGridCell));
            applicationsCellStyle.Setters.Add(new Setter(VerticalContentAlignmentProperty, VerticalAlignment.Center));
            applicationsCellStyle.Setters.Add(new Setter(HorizontalContentAlignmentProperty, HorizontalAlignment.Left));
            applicationsCellStyle.Setters.Add(new Setter(VerticalAlignmentProperty, VerticalAlignment.Stretch));
            applicationsCellStyle.Setters.Add(new Setter(HeightProperty, double.NaN));
            applicationsCellStyle.Setters.Add(new Setter(PaddingProperty, new Thickness(16, 12, 16, 12)));
            applicationsCellStyle.Setters.Add(new Setter(BorderThicknessProperty, new Thickness(1, 0, 1, 0)));
            applicationsCellStyle.Setters.Add(new Setter(BorderBrushProperty, new SolidColorBrush(Color.FromRgb(229, 231, 235))));
            
            var applicationsHoverTrigger = new Trigger { Property = UIElement.IsMouseOverProperty, Value = true };
            applicationsHoverTrigger.Setters.Add(new Setter(BackgroundProperty, new SolidColorBrush(Color.FromRgb(243, 244, 246))));
            applicationsCellStyle.Triggers.Add(applicationsHoverTrigger);
            
            var applicationsSelectedTrigger = new Trigger { Property = System.Windows.Controls.DataGridCell.IsSelectedProperty, Value = true };
            applicationsSelectedTrigger.Setters.Add(new Setter(BackgroundProperty, new SolidColorBrush(Color.FromRgb(239, 246, 255))));
            applicationsCellStyle.Triggers.Add(applicationsSelectedTrigger);
            
            applicationsDataGrid.CellStyle = applicationsCellStyle;
            
            var applicationsTextBlockStyle = new Style(typeof(TextBlock));
            applicationsTextBlockStyle.Setters.Add(new Setter(VerticalAlignmentProperty, VerticalAlignment.Center));
            applicationsTextBlockStyle.Setters.Add(new Setter(HorizontalAlignmentProperty, HorizontalAlignment.Left));
            applicationsTextBlockStyle.Setters.Add(new Setter(MarginProperty, new Thickness(0)));
            
            var applicationsHeaderStyle = new Style(typeof(System.Windows.Controls.Primitives.DataGridColumnHeader));
            applicationsHeaderStyle.Setters.Add(new Setter(BackgroundProperty, new SolidColorBrush(Color.FromRgb(249, 250, 251))));
            applicationsHeaderStyle.Setters.Add(new Setter(ForegroundProperty, new SolidColorBrush(Color.FromRgb(17, 24, 39))));
            applicationsHeaderStyle.Setters.Add(new Setter(FontWeightProperty, FontWeights.SemiBold));
            applicationsHeaderStyle.Setters.Add(new Setter(FontSizeProperty, 14.0));
            applicationsHeaderStyle.Setters.Add(new Setter(PaddingProperty, new Thickness(12, 10, 12, 10)));
            applicationsHeaderStyle.Setters.Add(new Setter(BorderBrushProperty, new SolidColorBrush(Color.FromRgb(229, 231, 235))));
            applicationsHeaderStyle.Setters.Add(new Setter(BorderThicknessProperty, new Thickness(0, 0, 1, 2)));
            applicationsHeaderStyle.Setters.Add(new Setter(HorizontalContentAlignmentProperty, HorizontalAlignment.Left));
            applicationsHeaderStyle.Setters.Add(new Setter(VerticalContentAlignmentProperty, VerticalAlignment.Center));
            applicationsDataGrid.ColumnHeaderStyle = applicationsHeaderStyle;

            var checkboxColumn = new System.Windows.Controls.DataGridCheckBoxColumn
            {
                Header = "Выбрать",
                Binding = new System.Windows.Data.Binding("IsSelected"),
                Width = new DataGridLength(80)
            };
            applicationsDataGrid.Columns.Add(checkboxColumn);

                applicationsDataGrid.Columns.Add(new System.Windows.Controls.DataGridTextColumn
                {
                    Header = "Абитуриент",
                    Binding = new System.Windows.Data.Binding("Application.User.FullName")
                    {
                        Mode = BindingMode.OneWay
                    },
                    Width = new DataGridLength(2, DataGridLengthUnitType.Star),
                    ElementStyle = applicationsTextBlockStyle
                });

            applicationsDataGrid.Columns.Add(new System.Windows.Controls.DataGridTextColumn
            {
                Header = "Экзамен",
                Binding = new System.Windows.Data.Binding("Application.Exam.Name"),
                Width = new DataGridLength(2, DataGridLengthUnitType.Star),
                ElementStyle = applicationsTextBlockStyle
            });

            applicationsDataGrid.Columns.Add(new System.Windows.Controls.DataGridTextColumn
            {
                Header = "Дата подачи",
                Binding = new System.Windows.Data.Binding("Application.AppliedAt") { StringFormat = "dd.MM.yyyy HH:mm" },
                Width = new DataGridLength(150),
                ElementStyle = applicationsTextBlockStyle
            });

            var statusColumn = new System.Windows.Controls.DataGridTextColumn
            {
                Header = "Статус",
                Binding = new System.Windows.Data.Binding("Application.Status")
                {
                    Converter = new ApplicationStatusConverter()
                },
                Width = new DataGridLength(120),
                ElementStyle = applicationsTextBlockStyle
            };
            applicationsDataGrid.Columns.Add(statusColumn);

            applicationsDataGrid.CanUserSortColumns = true;
            applicationsDataGrid.Sorting += (s, e) =>
            {
            };

            Func<List<Models.ExamApplication>, List<Models.ExamApplication>> filterApplications = (apps) =>
            {
                var filtered = apps;
                
                if (statusFilterComboBox.SelectedIndex > 0)
                {
                    var status = statusFilterComboBox.SelectedIndex switch
                    {
                        1 => Models.ApplicationStatus.Pending,
                        2 => Models.ApplicationStatus.Approved,
                        3 => Models.ApplicationStatus.Rejected,
                        _ => (Models.ApplicationStatus?)null
                    };
                    if (status.HasValue)
                    {
                        filtered = filtered.Where(a => a.Status == status.Value).ToList();
                    }
                }
                
                if (!string.IsNullOrWhiteSpace(applicantFilterTextBox.Text))
                {
                    var searchLower = applicantFilterTextBox.Text.ToLower();
                    filtered = filtered.Where(a => 
                        (a.User?.FirstName?.ToLower().Contains(searchLower) ?? false) ||
                        (a.User?.LastName?.ToLower().Contains(searchLower) ?? false) ||
                        (a.User?.FullName?.ToLower().Contains(searchLower) ?? false)
                    ).ToList();
                }
                
                if (!string.IsNullOrWhiteSpace(examFilterTextBox.Text))
                {
                    var searchLower = examFilterTextBox.Text.ToLower();
                    filtered = filtered.Where(a => 
                        (a.Exam?.Name?.ToLower().Contains(searchLower) ?? false) ||
                        (a.Exam?.Subject?.ToLower().Contains(searchLower) ?? false)
                    ).ToList();
                }
                
                return filtered.OrderByDescending(a => a.AppliedAt).ToList();
            };

            var filteredApplications = filterApplications(_viewModel.ExamApplications.ToList());
            var applicationsWithSelection = filteredApplications
                .Select(a => new ApplicationWithSelection { Application = a, IsSelected = false })
                .ToList();

            applicationsDataGrid.ItemsSource = applicationsWithSelection;
            stackPanel.Children.Add(applicationsDataGrid);
            
            Action updateApplicationsList = () =>
            {
                var filtered = filterApplications(_viewModel.ExamApplications.ToList());
                var updated = filtered
                    .Select(a => new ApplicationWithSelection { Application = a, IsSelected = false })
                    .ToList();
                applicationsDataGrid.ItemsSource = null;
                applicationsDataGrid.ItemsSource = updated;
                for (int i = 0; i < updated.Count && i < applicationsWithSelection.Count; i++)
                {
                    updated[i].IsSelected = applicationsWithSelection[i].IsSelected;
                }
                applicationsWithSelection = updated;
            };
            
            statusFilterComboBox.SelectionChanged += (s, e) => updateApplicationsList();
            applicantFilterTextBox.TextChanged += (s, e) => updateApplicationsList();
            examFilterTextBox.TextChanged += (s, e) => updateApplicationsList();

            var actionsStack = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 20, 0, 0)
            };

            var approveButton = new Button
            {
                Content = "Одобрить выбранные",
                Style = (Style)FindResource("MaterialDesignRaisedButton"),
                Background = new SolidColorBrush(Color.FromRgb(0, 184, 148)),
                Height = 40,
                Margin = new Thickness(0, 0, 10, 0)
            };
            approveButton.Click += async (s, e) => 
            {
                var selectedApps = applicationsWithSelection
                    .Where(a => a.IsSelected && a.Application.Status == Models.ApplicationStatus.Pending)
                    .Select(a => a.Application)
                    .ToList();
                
                if (selectedApps.Count == 0)
                {
                    MessageBox.Show("Пожалуйста, выберите заявки для одобрения.", "Информация",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }
                
                foreach (var app in selectedApps)
                {
                    await ApproveApplication(app);
                }
                
                LoadCTApplicationsContent();
            };
            actionsStack.Children.Add(approveButton);

            var rejectButton = new Button
            {
                Content = "Отклонить выбранные",
                Style = (Style)FindResource("MaterialDesignRaisedButton"),
                Background = new SolidColorBrush(Color.FromRgb(231, 76, 60)),
                Height = 40
            };
            rejectButton.Click += async (s, e) => 
            {
                var selectedApps = applicationsWithSelection
                    .Where(a => a.IsSelected && (a.Application.Status == Models.ApplicationStatus.Pending || a.Application.Status == Models.ApplicationStatus.Approved))
                    .Select(a => a.Application)
                    .ToList();
                
                if (selectedApps.Count == 0)
                {
                    MessageBox.Show("Пожалуйста, выберите заявки для отклонения.", "Информация",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }
                
                foreach (var app in selectedApps)
                {
                    await RejectApplication(app);
                }
                
                LoadCTApplicationsContent();
            };
            actionsStack.Children.Add(rejectButton);

            stackPanel.Children.Add(actionsStack);
            scrollViewer.Content = stackPanel;
            return scrollViewer;
        }

        private FrameworkElement CreateCertificatesReviewTabContent()
        {
            if (_viewModel is null) return new TextBlock { Text = "Ошибка загрузки данных" };

            var scrollViewer = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            var stackPanel = new StackPanel { Margin = new Thickness(30) };

            var filtersPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 0, 0, 20)
            };

            var statusLabel = new TextBlock
            {
                Text = "Статус:",
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 10, 0),
                FontWeight = FontWeights.SemiBold
            };
            filtersPanel.Children.Add(statusLabel);

            var statusComboBox = new ComboBox
            {
                Width = 150,
                Margin = new Thickness(0, 0, 20, 0)
            };
            statusComboBox.Items.Add("Все");
            statusComboBox.Items.Add("Ожидает");
            statusComboBox.Items.Add("Принята");
            statusComboBox.Items.Add("Отклонена");
            
            bool isUpdatingSelection = false;
            
            SelectionChangedEventHandler statusComboBoxSelectionChanged = null;
            statusComboBoxSelectionChanged = async (s, e) =>
            {
                if (isUpdatingSelection) return;
                
                var oldFilter = _viewModel.SelectedCertificateStatusFilter;
                
                if (statusComboBox.SelectedIndex == 0)
                    _viewModel.SelectedCertificateStatusFilter = null;
                else if (statusComboBox.SelectedIndex == 1)
                    _viewModel.SelectedCertificateStatusFilter = Models.CertificateStatus.Pending;
                else if (statusComboBox.SelectedIndex == 2)
                    _viewModel.SelectedCertificateStatusFilter = Models.CertificateStatus.Approved;
                else if (statusComboBox.SelectedIndex == 3)
                    _viewModel.SelectedCertificateStatusFilter = Models.CertificateStatus.Rejected;
                
                if (oldFilter != _viewModel.SelectedCertificateStatusFilter)
                {
                    await _viewModel.FilterCertificatesAsync();
                    if (_adminTabControl != null && _adminTabControl.Items.Count > 1)
                    {
                        var certificatesTab = _adminTabControl.Items[1] as TabItem;
                        if (certificatesTab != null)
                        {
                            certificatesTab.Content = CreateCertificatesReviewTabContent();
                        }
                    }
                }
            };
            
            statusComboBox.SelectionChanged += statusComboBoxSelectionChanged;
            
            isUpdatingSelection = true;
            try
            {
                if (!_viewModel.SelectedCertificateStatusFilter.HasValue)
                    statusComboBox.SelectedIndex = 0;
                else if (_viewModel.SelectedCertificateStatusFilter.Value == Models.CertificateStatus.Pending)
                    statusComboBox.SelectedIndex = 1;
                else if (_viewModel.SelectedCertificateStatusFilter.Value == Models.CertificateStatus.Approved)
                    statusComboBox.SelectedIndex = 2;
                else if (_viewModel.SelectedCertificateStatusFilter.Value == Models.CertificateStatus.Rejected)
                    statusComboBox.SelectedIndex = 3;
                else
                    statusComboBox.SelectedIndex = 0;
            }
            finally
            {
                isUpdatingSelection = false;
            }
            filtersPanel.Children.Add(statusComboBox);

            var userLabel = new TextBlock
            {
                Text = "Абитуриент:",
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 10, 0),
                FontWeight = FontWeights.SemiBold
            };
            filtersPanel.Children.Add(userLabel);

            var userComboBox = new ComboBox
            {
                Width = 200,
                DisplayMemberPath = "FullName"
            };
            userComboBox.Items.Add("Все");
            foreach (var user in _viewModel.AllUsers)
            {
                userComboBox.Items.Add(user);
            }
            userComboBox.SelectedIndex = 0;
            userComboBox.SelectionChanged += async (s, e) =>
            {
                if (userComboBox.SelectedIndex == 0 || userComboBox.SelectedItem == null)
                    _viewModel.SelectedCertificateUserIdFilter = null;
                else if (userComboBox.SelectedItem is Models.User selectedUser)
                    _viewModel.SelectedCertificateUserIdFilter = selectedUser.Id;
                
                await _viewModel.FilterCertificatesAsync();
                if (_adminTabControl != null && _adminTabControl.Items.Count > 1)
                {
                    var certificatesTab = _adminTabControl.Items[1] as TabItem;
                    if (certificatesTab != null)
                    {
                        certificatesTab.Content = CreateCertificatesReviewTabContent();
                    }
                }
            };
            filtersPanel.Children.Add(userComboBox);

            stackPanel.Children.Add(filtersPanel);

            var allCertificates = _viewModel.AllCertificates ?? new System.Collections.ObjectModel.ObservableCollection<Models.Certificate>();

            if (allCertificates.Count == 0)
            {
                var noDataText = new TextBlock
                {
                    Text = "Нет сертификатов",
                    FontSize = 16,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 50, 0, 0)
                };
                stackPanel.Children.Add(noDataText);
            }
            else
            {
                var dataGrid = new System.Windows.Controls.DataGrid
                {
                    AutoGenerateColumns = false,
                    CanUserAddRows = false,
                    CanUserDeleteRows = false,
                    SelectionMode = DataGridSelectionMode.Single,
                    HeadersVisibility = DataGridHeadersVisibility.All,
                    GridLinesVisibility = DataGridGridLinesVisibility.All,
                    AlternatingRowBackground = new SolidColorBrush(Color.FromRgb(249, 250, 251)),
                    RowBackground = Brushes.White,
                    Background = Brushes.White,
                    BorderBrush = new SolidColorBrush(Color.FromRgb(229, 231, 235)),
                    BorderThickness = new Thickness(1),
                    Margin = new Thickness(0, 0, 0, 20),
                    FontSize = 14,
                    Height = 400,
                    VerticalContentAlignment = VerticalAlignment.Center,
                    RowHeight = 50
                };

                var cellStyle = new Style(typeof(System.Windows.Controls.DataGridCell));
                cellStyle.Setters.Add(new Setter(VerticalContentAlignmentProperty, VerticalAlignment.Center));
                cellStyle.Setters.Add(new Setter(HorizontalContentAlignmentProperty, HorizontalAlignment.Left));
                cellStyle.Setters.Add(new Setter(VerticalAlignmentProperty, VerticalAlignment.Stretch));
                cellStyle.Setters.Add(new Setter(HeightProperty, double.NaN));
                cellStyle.Setters.Add(new Setter(PaddingProperty, new Thickness(16, 12, 16, 12)));
                cellStyle.Setters.Add(new Setter(BorderThicknessProperty, new Thickness(1, 0, 1, 0)));
                cellStyle.Setters.Add(new Setter(BorderBrushProperty, new SolidColorBrush(Color.FromRgb(229, 231, 235))));
                
                var hoverTrigger = new Trigger { Property = UIElement.IsMouseOverProperty, Value = true };
                hoverTrigger.Setters.Add(new Setter(BackgroundProperty, new SolidColorBrush(Color.FromRgb(243, 244, 246))));
                cellStyle.Triggers.Add(hoverTrigger);
                
                var selectedTrigger = new Trigger { Property = System.Windows.Controls.DataGridCell.IsSelectedProperty, Value = true };
                selectedTrigger.Setters.Add(new Setter(BackgroundProperty, new SolidColorBrush(Color.FromRgb(239, 246, 255))));
                cellStyle.Triggers.Add(selectedTrigger);
                
                dataGrid.CellStyle = cellStyle;
                
                var textBlockStyle = new Style(typeof(TextBlock));
                textBlockStyle.Setters.Add(new Setter(VerticalAlignmentProperty, VerticalAlignment.Center));
                textBlockStyle.Setters.Add(new Setter(HorizontalAlignmentProperty, HorizontalAlignment.Left));
                textBlockStyle.Setters.Add(new Setter(MarginProperty, new Thickness(0)));
                
                var headerStyle = new Style(typeof(System.Windows.Controls.Primitives.DataGridColumnHeader));
                headerStyle.Setters.Add(new Setter(BackgroundProperty, new SolidColorBrush(Color.FromRgb(249, 250, 251))));
                headerStyle.Setters.Add(new Setter(ForegroundProperty, new SolidColorBrush(Color.FromRgb(17, 24, 39))));
                headerStyle.Setters.Add(new Setter(FontWeightProperty, FontWeights.SemiBold));
                headerStyle.Setters.Add(new Setter(FontSizeProperty, 14.0));
                headerStyle.Setters.Add(new Setter(PaddingProperty, new Thickness(12, 10, 12, 10)));
                headerStyle.Setters.Add(new Setter(BorderBrushProperty, new SolidColorBrush(Color.FromRgb(229, 231, 235))));
                headerStyle.Setters.Add(new Setter(BorderThicknessProperty, new Thickness(0, 0, 1, 2)));
                headerStyle.Setters.Add(new Setter(HorizontalContentAlignmentProperty, HorizontalAlignment.Left));
                headerStyle.Setters.Add(new Setter(VerticalContentAlignmentProperty, VerticalAlignment.Center));
                dataGrid.ColumnHeaderStyle = headerStyle;

                var checkboxColumn = new System.Windows.Controls.DataGridCheckBoxColumn
                {
                    Header = "Выбрать",
                    Binding = new System.Windows.Data.Binding("IsSelected"),
                    Width = new DataGridLength(80)
                };
                dataGrid.Columns.Add(checkboxColumn);

                dataGrid.Columns.Add(new System.Windows.Controls.DataGridTextColumn
                {
                    Header = "Пользователь",
                    Binding = new System.Windows.Data.Binding("Certificate.User.FullName") 
                    { 
                        Mode = BindingMode.OneWay 
                    },
                    Width = new DataGridLength(200),
                    ElementStyle = textBlockStyle
                });

                dataGrid.Columns.Add(new System.Windows.Controls.DataGridTextColumn
                {
                    Header = "Предмет",
                    Binding = new System.Windows.Data.Binding("Certificate.Subject"),
                    Width = new DataGridLength(150),
                    ElementStyle = textBlockStyle
                });

                dataGrid.Columns.Add(new System.Windows.Controls.DataGridTextColumn
                {
                    Header = "Тип",
                    Binding = new System.Windows.Data.Binding("Certificate.Type")
                    {
                        Converter = new Converters.CertificateTypeConverter()
                    },
                    Width = new DataGridLength(150),
                    ElementStyle = textBlockStyle
                });

                dataGrid.Columns.Add(new System.Windows.Controls.DataGridTextColumn
                {
                    Header = "Номер",
                    Binding = new System.Windows.Data.Binding("Certificate.CertificateNumber"),
                    Width = new DataGridLength(150),
                    ElementStyle = textBlockStyle
                });

                dataGrid.Columns.Add(new System.Windows.Controls.DataGridTextColumn
                {
                    Header = "Баллы",
                    Binding = new System.Windows.Data.Binding("Certificate.Score"),
                    Width = new DataGridLength(100),
                    ElementStyle = textBlockStyle
                });

                dataGrid.Columns.Add(new System.Windows.Controls.DataGridTextColumn
                {
                    Header = "Дата загрузки",
                    Binding = new System.Windows.Data.Binding("Certificate.UploadedAt") { StringFormat = "dd.MM.yyyy" },
                    Width = new DataGridLength(150),
                    ElementStyle = textBlockStyle
                });

                dataGrid.Columns.Add(new System.Windows.Controls.DataGridTextColumn
                {
                    Header = "Статус",
                    Binding = new System.Windows.Data.Binding("Certificate.Status")
                    {
                        Converter = new Converters.CertificateStatusConverter()
                    },
                    Width = new DataGridLength(120),
                    ElementStyle = textBlockStyle
                });

                var certificatesWithSelection = allCertificates
                    .Select(c => new CertificateWithSelection { Certificate = c, IsSelected = false })
                    .ToList();

                dataGrid.ItemsSource = certificatesWithSelection;
                stackPanel.Children.Add(dataGrid);

                var actionsPanel = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Margin = new Thickness(0, 10, 0, 0)
                };

                var approveBtn = new Button
                {
                    Content = "Одобрить выбранные",
                    Style = (Style)FindResource("MaterialDesignRaisedButton"),
                    Background = new SolidColorBrush(Color.FromRgb(0, 184, 148)),
                    Height = 40,
                    Margin = new Thickness(0, 0, 10, 0)
                };
                approveBtn.Click += async (s, e) =>
                {
                    var selectedCerts = certificatesWithSelection
                        .Where(c => c.IsSelected)
                        .Select(c => c.Certificate)
                        .ToList();
                    
                    if (selectedCerts.Count == 0)
                    {
                        MessageBox.Show("Пожалуйста, выберите сертификаты для одобрения.", "Информация",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }
                    
                    foreach (var cert in selectedCerts)
                    {
                        await _viewModel.ApproveCertificateAsync(cert);
                    }
                    
                    LoadCTApplicationsContent();
                };
                actionsPanel.Children.Add(approveBtn);

                var rejectBtn = new Button
                {
                    Content = "Отклонить выбранные",
                    Style = (Style)FindResource("MaterialDesignRaisedButton"),
                    Background = new SolidColorBrush(Color.FromRgb(231, 76, 60)),
                    Height = 40
                };
                rejectBtn.Click += async (s, e) =>
                {
                    var selectedCerts = certificatesWithSelection
                        .Where(c => c.IsSelected)
                        .Select(c => c.Certificate)
                        .ToList();
                    
                    if (selectedCerts.Count == 0)
                    {
                        MessageBox.Show("Пожалуйста, выберите сертификаты для отклонения.", "Информация",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }
                    
                    var dialog = new Views.InputDialog("Введите причину отклонения сертификатов:", "Причина отклонения");
                    if (dialog.ShowDialog() == true && !string.IsNullOrWhiteSpace(dialog.ResponseText))
                    {
                        var reason = dialog.ResponseText;
                        foreach (var cert in selectedCerts)
                        {
                            await _viewModel.RejectCertificateAsync(cert, reason);
                        }
                        LoadCTApplicationsContent();
                    }
                };
                actionsPanel.Children.Add(rejectBtn);

                stackPanel.Children.Add(actionsPanel);
            }

            scrollViewer.Content = stackPanel;
            return scrollViewer;
        }

        private FrameworkElement CreateUniversityApplicationsReviewTabContent()
        {
            if (_viewModel is null) return new TextBlock { Text = "Ошибка загрузки данных" };

            var scrollViewer = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            var stackPanel = new StackPanel { Margin = new Thickness(30) };

            var filtersPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 0, 0, 20)
            };

            var statusLabel = new TextBlock
            {
                Text = "Статус:",
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 10, 0),
                FontWeight = FontWeights.SemiBold
            };
            filtersPanel.Children.Add(statusLabel);

            var statusComboBox = new ComboBox
            {
                Width = 150,
                Margin = new Thickness(0, 0, 20, 0)
            };
            statusComboBox.Items.Add("Все");
            statusComboBox.Items.Add("Ожидает");
            statusComboBox.Items.Add("Принята");
            statusComboBox.Items.Add("Отклонена");
            statusComboBox.Items.Add("В листе ожидания");
            
            bool isUpdatingSelection = false;
            
            SelectionChangedEventHandler statusComboBoxSelectionChanged = null;
            statusComboBoxSelectionChanged = async (s, e) =>
            {
                if (isUpdatingSelection) return;
                
                var oldFilter = _viewModel.SelectedUniversityApplicationStatusFilter;
                
                if (statusComboBox.SelectedIndex == 0)
                    _viewModel.SelectedUniversityApplicationStatusFilter = null;
                else if (statusComboBox.SelectedIndex == 1)
                    _viewModel.SelectedUniversityApplicationStatusFilter = Models.UniversityApplicationStatus.Pending;
                else if (statusComboBox.SelectedIndex == 2)
                    _viewModel.SelectedUniversityApplicationStatusFilter = Models.UniversityApplicationStatus.Accepted;
                else if (statusComboBox.SelectedIndex == 3)
                    _viewModel.SelectedUniversityApplicationStatusFilter = Models.UniversityApplicationStatus.Rejected;
                else if (statusComboBox.SelectedIndex == 4)
                    _viewModel.SelectedUniversityApplicationStatusFilter = Models.UniversityApplicationStatus.Waitlisted;
                
                if (oldFilter != _viewModel.SelectedUniversityApplicationStatusFilter)
                {
                    await _viewModel.FilterUniversityApplicationsAsync();
                    if (_adminTabControl != null && _adminTabControl.Items.Count > 2)
                    {
                        var universityApplicationsTab = _adminTabControl.Items[2] as TabItem;
                        if (universityApplicationsTab != null)
                        {
                            universityApplicationsTab.Content = CreateUniversityApplicationsReviewTabContent();
                        }
                    }
                }
            };
            
            statusComboBox.SelectionChanged += statusComboBoxSelectionChanged;
            
            isUpdatingSelection = true;
            try
            {
                if (!_viewModel.SelectedUniversityApplicationStatusFilter.HasValue)
                    statusComboBox.SelectedIndex = 0;
                else if (_viewModel.SelectedUniversityApplicationStatusFilter.Value == Models.UniversityApplicationStatus.Pending)
                    statusComboBox.SelectedIndex = 1;
                else if (_viewModel.SelectedUniversityApplicationStatusFilter.Value == Models.UniversityApplicationStatus.Accepted)
                    statusComboBox.SelectedIndex = 2;
                else if (_viewModel.SelectedUniversityApplicationStatusFilter.Value == Models.UniversityApplicationStatus.Rejected)
                    statusComboBox.SelectedIndex = 3;
                else if (_viewModel.SelectedUniversityApplicationStatusFilter.Value == Models.UniversityApplicationStatus.Waitlisted)
                    statusComboBox.SelectedIndex = 4;
                else
                    statusComboBox.SelectedIndex = 0;
            }
            finally
            {
                isUpdatingSelection = false;
            }
            filtersPanel.Children.Add(statusComboBox);

            var userLabel = new TextBlock
            {
                Text = "Абитуриент:",
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 10, 0),
                FontWeight = FontWeights.SemiBold
            };
            filtersPanel.Children.Add(userLabel);

            var userComboBox = new ComboBox
            {
                Width = 200,
                DisplayMemberPath = "FullName"
            };
            userComboBox.Items.Add("Все");
            foreach (var user in _viewModel.AllUsers)
            {
                userComboBox.Items.Add(user);
            }
            userComboBox.SelectedIndex = 0;
            userComboBox.SelectionChanged += async (s, e) =>
            {
                if (userComboBox.SelectedIndex == 0 || userComboBox.SelectedItem == null)
                    _viewModel.SelectedUniversityApplicationUserIdFilter = null;
                else if (userComboBox.SelectedItem is Models.User selectedUser)
                    _viewModel.SelectedUniversityApplicationUserIdFilter = selectedUser.Id;
                
                await _viewModel.FilterUniversityApplicationsAsync();
                if (_adminTabControl != null && _adminTabControl.Items.Count > 2)
                {
                    var universityApplicationsTab = _adminTabControl.Items[2] as TabItem;
                    if (universityApplicationsTab != null)
                    {
                        universityApplicationsTab.Content = CreateUniversityApplicationsReviewTabContent();
                    }
                }
            };
            filtersPanel.Children.Add(userComboBox);

            stackPanel.Children.Add(filtersPanel);

            var allApplications = _viewModel.AllUniversityApplications ?? new System.Collections.ObjectModel.ObservableCollection<Models.UniversityApplication>();

            if (allApplications.Count == 0)
            {
                var noDataText = new TextBlock
                {
                    Text = "Нет заявок в университеты",
                    FontSize = 16,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 50, 0, 0)
                };
                stackPanel.Children.Add(noDataText);
            }
            else
            {
                var applicationsWithSelection = allApplications.Select(app => new UniversityApplicationWithSelection
                {
                    Application = app,
                    IsSelected = false
                }).ToList();

                var dataGrid = new System.Windows.Controls.DataGrid
                {
                    AutoGenerateColumns = false,
                    CanUserAddRows = false,
                    ItemsSource = applicationsWithSelection,
                    Margin = new Thickness(0, 0, 0, 20)
                };

                var checkboxColumn = new System.Windows.Controls.DataGridCheckBoxColumn
                {
                    Header = "Выбрать",
                    Binding = new System.Windows.Data.Binding("IsSelected"),
                    Width = new DataGridLength(80)
                };
                dataGrid.Columns.Add(checkboxColumn);

                dataGrid.Columns.Add(new System.Windows.Controls.DataGridTextColumn
                {
                    Header = "Абитуриент",
                    Binding = new System.Windows.Data.Binding("Application.User.FullName")
                    {
                        Mode = BindingMode.OneWay
                    },
                    Width = new DataGridLength(200)
                });

                dataGrid.Columns.Add(new System.Windows.Controls.DataGridTextColumn
                {
                    Header = "Университет",
                    Binding = new System.Windows.Data.Binding("Application.University.Name"),
                    Width = new DataGridLength(200)
                });

                dataGrid.Columns.Add(new System.Windows.Controls.DataGridTextColumn
                {
                    Header = "Специальность",
                    Binding = new System.Windows.Data.Binding("Application.Speciality.Name"),
                    Width = new DataGridLength(200)
                });

                dataGrid.Columns.Add(new System.Windows.Controls.DataGridTextColumn
                {
                    Header = "Тип",
                    Binding = new System.Windows.Data.Binding("Application.Type")
                    {
                        Converter = new Converters.ApplicationTypeConverter()
                    },
                    Width = new DataGridLength(100)
                });

                dataGrid.Columns.Add(new System.Windows.Controls.DataGridTextColumn
                {
                    Header = "Статус",
                    Binding = new System.Windows.Data.Binding("Application.Status")
                    {
                        Converter = new Converters.UniversityApplicationStatusConverter()
                    },
                    Width = new DataGridLength(150)
                });

                dataGrid.Columns.Add(new System.Windows.Controls.DataGridTextColumn
                {
                    Header = "Дата подачи",
                    Binding = new System.Windows.Data.Binding("Application.AppliedAt") { StringFormat = "dd.MM.yyyy" },
                    Width = new DataGridLength(150)
                });

                stackPanel.Children.Add(dataGrid);

                var actionsPanel = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Margin = new Thickness(0, 10, 0, 0)
                };

                var approveBtn = new Button
                {
                    Content = "Одобрить выбранные",
                    Style = (Style)FindResource("MaterialDesignRaisedButton"),
                    Background = new SolidColorBrush(Color.FromRgb(0, 184, 148)),
                    Height = 40,
                    Margin = new Thickness(0, 0, 10, 0)
                };
                approveBtn.Click += async (s, e) =>
                {
                    var selectedApps = applicationsWithSelection
                        .Where(w => w.IsSelected)
                        .Select(w => w.Application)
                        .ToList();

                    if (selectedApps.Count == 0)
                    {
                        MessageBox.Show("Пожалуйста, выберите заявки для одобрения.", "Информация",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }

                    foreach (var app in selectedApps)
                    {
                        await _viewModel.ApproveUniversityApplicationAsync(app);
                    }
                    LoadCTApplicationsContent();
                };
                actionsPanel.Children.Add(approveBtn);

                var rejectBtn = new Button
                {
                    Content = "Отклонить выбранные",
                    Style = (Style)FindResource("MaterialDesignRaisedButton"),
                    Background = new SolidColorBrush(Color.FromRgb(231, 76, 60)),
                    Height = 40
                };
                rejectBtn.Click += async (s, e) =>
                {
                    var selectedApps = applicationsWithSelection
                        .Where(w => w.IsSelected)
                        .Select(w => w.Application)
                        .ToList();

                    if (selectedApps.Count == 0)
                    {
                        MessageBox.Show("Пожалуйста, выберите заявки для отклонения.", "Информация",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }

                    var dialog = new Views.InputDialog("Введите причину отклонения заявок:", "Причина отклонения");
                    if (dialog.ShowDialog() == true && !string.IsNullOrWhiteSpace(dialog.ResponseText))
                    {
                        var reason = dialog.ResponseText;
                        foreach (var app in selectedApps)
                        {
                            await _viewModel.RejectUniversityApplicationAsync(app, reason);
                        }
                        LoadCTApplicationsContent();
                    }
                };
                actionsPanel.Children.Add(rejectBtn);

                stackPanel.Children.Add(actionsPanel);
            }

            scrollViewer.Content = stackPanel;
            return scrollViewer;
        }

        private FrameworkElement CreateAdminApplicationItem(Models.ExamApplication app)
        {
            var border = new Border
            {
                Background = Brushes.White,
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(15),
                Margin = new Thickness(0, 0, 0, 10),
                BorderBrush = new SolidColorBrush(GetStatusColor(app.Status)),
                BorderThickness = new Thickness(2, 0, 0, 0),
                Tag = app
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var userText = new TextBlock 
            { 
                Text = app.User?.FullName ?? "Неизвестно", 
                FontWeight = FontWeights.SemiBold,
                FontSize = 14
            };
            Grid.SetColumn(userText, 0);

            var examText = new TextBlock 
            { 
                Text = app.Exam?.Name ?? "Неизвестно", 
                Foreground = Brushes.Gray,
                FontSize = 14
            };
            Grid.SetColumn(examText, 1);

            var dateText = new TextBlock 
            { 
                Text = app.AppliedAt.ToString("dd.MM.yyyy"), 
                Foreground = Brushes.Gray,
                FontSize = 14
            };
            Grid.SetColumn(dateText, 2);

            var statusText = new TextBlock
            {
                Text = GetStatusText(app.Status),
                Foreground = new SolidColorBrush(GetStatusColor(app.Status)),
                FontWeight = FontWeights.SemiBold,
                FontSize = 14
            };
            Grid.SetColumn(statusText, 3);

            grid.Children.Add(userText);
            grid.Children.Add(examText);
            grid.Children.Add(dateText);
            grid.Children.Add(statusText);

            border.Child = grid;
            return border;
        }


        private async Task ApproveApplication(Models.ExamApplication app)
        {
            if (app == null)
            {
                MessageBox.Show("Пожалуйста, выберите заявку для одобрения.", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (app.Status != Models.ApplicationStatus.Pending)
            {
                MessageBox.Show("Можно одобрять только заявки со статусом 'Ожидает'.", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show(
                $"Вы уверены, что хотите одобрить заявку от {app.User?.FullName} на экзамен '{app.Exam?.Name}'?",
                "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    using (var context = new Data.ApplicationDbContext())
                    {
                        var appToUpdate = await context.ExamApplications
                            .Include(a => a.ExamSchedule)
                            .FirstOrDefaultAsync(a => a.Id == app.Id);
                        
                        if (appToUpdate != null)
                        {
                            var schedule = await context.ExamSchedules.FindAsync(appToUpdate.ExamScheduleId);
                            
                            if (schedule != null)
                            {
                                if (schedule.CurrentParticipants >= schedule.MaxParticipants)
                                {
                                    MessageBox.Show("Нет свободных мест на этот экзамен. Невозможно одобрить заявку.", "Ошибка",
                                        MessageBoxButton.OK, MessageBoxImage.Warning);
                                    return;
                                }
                                
                                schedule.CurrentParticipants++;
                            }
                            
                            appToUpdate.Status = Models.ApplicationStatus.Approved;
                            appToUpdate.ReviewedAt = DateTime.Now;
                            await context.SaveChangesAsync();
                            
                            var scheduleToSync = await context.ExamSchedules.FindAsync(appToUpdate.ExamScheduleId);
                            if (scheduleToSync != null)
                            {
                                var approvedCount = await context.ExamApplications
                                    .CountAsync(a => a.ExamScheduleId == scheduleToSync.Id && 
                                                   a.Status == Models.ApplicationStatus.Approved);
                                scheduleToSync.CurrentParticipants = approvedCount;
                                await context.SaveChangesAsync();
                            }

                            var emailService = new Services.EmailService();
                            if (app.User != null)
                            {
                                await emailService.SendEmailAsync(
                                    app.User.Email,
                                    "Заявка на экзамен одобрена",
                                    $"Ваша заявка на экзамен '{app.Exam?.Name}' была одобрена."
                                );
                            }

                            MessageBox.Show("Заявка одобрена.", "Успех",
                                MessageBoxButton.OK, MessageBoxImage.Information);

                            if (_viewModel is not null)
                            {
                                await _viewModel.LoadDataAsync();
                                LoadCTApplicationsContent();
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async Task RejectApplication(Models.ExamApplication app)
        {
            if (app.Status == Models.ApplicationStatus.Rejected || app.Status == Models.ApplicationStatus.Completed)
            {
                MessageBox.Show("Нельзя изменить статус уже отклоненной или завершенной заявки.", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var reasonDialog = new Views.InputDialog("Укажите причину отклонения:", "Отклонение заявки");
            if (reasonDialog.ShowDialog() != true || string.IsNullOrWhiteSpace(reasonDialog.ResponseText))
                return;

            var reason = reasonDialog.ResponseText;

            try
            {
                using (var context = new Data.ApplicationDbContext())
                {
                    var appToUpdate = await context.ExamApplications
                        .Include(a => a.ExamSchedule)
                        .FirstOrDefaultAsync(a => a.Id == app.Id);
                    
                    if (appToUpdate != null)
                    {
                        appToUpdate.Status = Models.ApplicationStatus.Rejected;
                        appToUpdate.RejectionReason = reason;
                        appToUpdate.ReviewedAt = DateTime.Now;
                        await context.SaveChangesAsync();
                        
                        var scheduleToSync = await context.ExamSchedules.FindAsync(appToUpdate.ExamScheduleId);
                        if (scheduleToSync != null)
                        {
                            var approvedCount = await context.ExamApplications
                                .CountAsync(a => a.ExamScheduleId == scheduleToSync.Id && 
                                               a.Status == Models.ApplicationStatus.Approved);
                            scheduleToSync.CurrentParticipants = approvedCount;
                            await context.SaveChangesAsync();
                        }

                        var emailService = new Services.EmailService();
                        if (app.User != null)
                        {
                            await emailService.SendEmailAsync(
                                app.User.Email,
                                "Заявка на экзамен отклонена",
                                $"Ваша заявка на экзамен '{app.Exam?.Name}' была отклонена. Причина: {reason}"
                            );
                        }

                        MessageBox.Show("Заявка отклонена.", "Успех",
                            MessageBoxButton.OK, MessageBoxImage.Information);

                        if (_viewModel is not null)
                        {
                            await _viewModel.LoadDataAsync();
                            LoadCTApplicationsContent();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task DeleteExamApplicationAsync(Models.ExamApplication app)
        {
            if (_viewModel?.CurrentUser == null)
            {
                MessageBox.Show("Ошибка: пользователь не авторизован.", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (app.UserId != _viewModel.CurrentUser.Id)
            {
                MessageBox.Show("Вы можете удалять только свои заявки.", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (app.Status != Models.ApplicationStatus.Pending && app.Status != Models.ApplicationStatus.Rejected)
            {
                MessageBox.Show("Можно удалять только заявки со статусом 'Ожидает' или 'Отклонена'.", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using (var context = new Data.ApplicationDbContext())
                {
                    var appToDelete = await context.ExamApplications
                        .Include(a => a.ExamSchedule)
                        .FirstOrDefaultAsync(a => a.Id == app.Id);

                    if (appToDelete == null)
                    {
                        MessageBox.Show("Заявка не найдена.", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    var scheduleId = appToDelete.ExamScheduleId;
                    var wasApproved = appToDelete.Status == Models.ApplicationStatus.Approved;

                    context.ExamApplications.Remove(appToDelete);
                    await context.SaveChangesAsync();

                    if (wasApproved && scheduleId > 0)
                    {
                        var schedule = await context.ExamSchedules.FindAsync(scheduleId);
                        if (schedule != null)
                        {
                            var approvedCount = await context.ExamApplications
                                .CountAsync(a => a.ExamScheduleId == scheduleId &&
                                               a.Status == Models.ApplicationStatus.Approved);
                            schedule.CurrentParticipants = approvedCount;
                            await context.SaveChangesAsync();
                        }
                    }

                    MessageBox.Show("Заявка успешно удалена.", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);

                    if (_viewModel is not null)
                    {
                        await _viewModel.LoadDataAsync();
                        LoadCTApplicationsContent();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при удалении заявки: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task DeleteExamResultAsync(Models.ExamResult examResult)
        {
            if (_viewModel?.CurrentUser == null)
            {
                MessageBox.Show("Ошибка: пользователь не авторизован.", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (examResult.UserId != _viewModel.CurrentUser.Id)
            {
                MessageBox.Show("Вы можете удалять только свои результаты экзаменов.", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using (var context = new Data.ApplicationDbContext())
                {
                    var resultToDelete = await context.ExamResults.FindAsync(examResult.Id);

                    if (resultToDelete == null)
                    {
                        MessageBox.Show("Результат экзамена не найден.", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    context.ExamResults.Remove(resultToDelete);
                    await context.SaveChangesAsync();

                    MessageBox.Show("Результат экзамена успешно удален.", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);

                    if (_viewModel != null)
                    {
                        await _viewModel.LoadDataAsync();
                    }

                    var newContent = await CreateExamsContentAsync();
                    MainContentPresenter.Content = newContent;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при удалении результата экзамена: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private FrameworkElement CreateApplicationItem(Models.ExamApplication app)
        {
            return CreateApplicationCard(app);
        }

        private FrameworkElement CreateApplicationCard(Models.ExamApplication app)
        {
            var card = new Border
            {
                Background = Brushes.White,
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(20),
                Margin = new Thickness(0, 0, 0, 15),
                BorderBrush = new SolidColorBrush(GetStatusColor(app.Status)),
                BorderThickness = new Thickness(0, 0, 0, 4),
                Effect = new System.Windows.Media.Effects.DropShadowEffect
                {
                    Color = Colors.Gray,
                    Direction = 270,
                    ShadowDepth = 3,
                    Opacity = 0.3,
                    BlurRadius = 5
                }
            };

            var stackPanel = new StackPanel();

            var headerGrid = new Grid();
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var examNameText = new TextBlock
            {
                Text = app.Exam?.Name ?? "Неизвестный экзамен",
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(51, 51, 51))
            };
            Grid.SetColumn(examNameText, 0);
            headerGrid.Children.Add(examNameText);

            var statusBadge = new Border
            {
                Background = new SolidColorBrush(GetStatusColor(app.Status)),
                CornerRadius = new CornerRadius(15),
                Padding = new Thickness(12, 6, 12, 6),
                Child = new TextBlock
                {
                    Text = GetStatusText(app.Status),
                    Foreground = Brushes.White,
                    FontWeight = FontWeights.SemiBold,
                    FontSize = 12
                }
            };
            Grid.SetColumn(statusBadge, 1);
            headerGrid.Children.Add(statusBadge);

            stackPanel.Children.Add(headerGrid);

            if (app.ExamSchedule != null)
            {
                var scheduleText = new TextBlock
                {
                    Text = $"📅 {app.ExamSchedule.ExamDate:dd.MM.yyyy} в {app.ExamSchedule.StartTime:hh\\:mm}",
                    FontSize = 14,
                    Foreground = new SolidColorBrush(Color.FromRgb(102, 102, 102)),
                    Margin = new Thickness(0, 8, 0, 0)
                };
                stackPanel.Children.Add(scheduleText);

                var locationText = new TextBlock
                {
                    Text = $"📍 {app.ExamSchedule.Location}",
                    FontSize = 14,
                    Foreground = new SolidColorBrush(Color.FromRgb(102, 102, 102)),
                    Margin = new Thickness(0, 4, 0, 0)
                };
                stackPanel.Children.Add(locationText);
            }

            var dateText = new TextBlock
            {
                Text = $"Подано: {app.AppliedAt:dd.MM.yyyy HH:mm}",
                FontSize = 12,
                Foreground = new SolidColorBrush(Color.FromRgb(153, 153, 153)),
                Margin = new Thickness(0, 12, 0, 0)
            };
            stackPanel.Children.Add(dateText);

            if (app.Status == Models.ApplicationStatus.Rejected && !string.IsNullOrEmpty(app.RejectionReason))
            {
                var rejectionText = new TextBlock
                {
                    Text = $"❌ Причина отклонения: {app.RejectionReason}",
                    FontSize = 12,
                    Foreground = new SolidColorBrush(Color.FromRgb(231, 76, 60)),
                    Margin = new Thickness(0, 8, 0, 0),
                    TextWrapping = TextWrapping.Wrap
                };
                stackPanel.Children.Add(rejectionText);
            }

            if (app.Status == Models.ApplicationStatus.Pending || app.Status == Models.ApplicationStatus.Rejected)
            {
                var deleteButton = new Button
                {
                    Content = "Удалить заявку",
                    Style = (Style)FindResource("MaterialDesignOutlinedButton"),
                    Background = Brushes.Transparent,
                    Foreground = new SolidColorBrush(Color.FromRgb(231, 76, 60)),
                    BorderBrush = new SolidColorBrush(Color.FromRgb(231, 76, 60)),
                    BorderThickness = new Thickness(1),
                    Height = 35,
                    FontSize = 12,
                    Margin = new Thickness(0, 12, 0, 0),
                    HorizontalAlignment = HorizontalAlignment.Left,
                    Padding = new Thickness(12, 0, 12, 0)
                };
                deleteButton.Click += async (s, e) =>
                {
                    var result = MessageBox.Show(
                        $"Вы уверены, что хотите удалить заявку на экзамен '{app.Exam?.Name}'?",
                        "Подтверждение удаления",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    if (result == MessageBoxResult.Yes)
                    {
                        await DeleteExamApplicationAsync(app);
                    }
                };
                stackPanel.Children.Add(deleteButton);
            }

            card.Child = stackPanel;
            return card;
        }

        private FrameworkElement CreateAdminApplicationCard(Models.ExamApplication app)
        {
            var card = new Border
            {
                Background = Brushes.White,
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(20),
                Margin = new Thickness(0, 0, 0, 15),
                BorderBrush = new SolidColorBrush(GetStatusColor(app.Status)),
                BorderThickness = new Thickness(0, 0, 0, 4),
                Effect = new System.Windows.Media.Effects.DropShadowEffect
                {
                    Color = Colors.Gray,
                    Direction = 270,
                    ShadowDepth = 3,
                    Opacity = 0.3,
                    BlurRadius = 5
                },
                Tag = app
            };

            var stackPanel = new StackPanel();

            var headerGrid = new Grid();
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var userText = new TextBlock
            {
                Text = $"👤 {app.User?.FullName ?? "Неизвестный пользователь"}",
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(51, 51, 51))
            };
            Grid.SetColumn(userText, 0);
            headerGrid.Children.Add(userText);

            var statusBadge = new Border
            {
                Background = new SolidColorBrush(GetStatusColor(app.Status)),
                CornerRadius = new CornerRadius(15),
                Padding = new Thickness(12, 6, 12, 6),
                Child = new TextBlock
                {
                    Text = GetStatusText(app.Status),
                    Foreground = Brushes.White,
                    FontWeight = FontWeights.SemiBold,
                    FontSize = 12
                }
            };
            Grid.SetColumn(statusBadge, 1);
            headerGrid.Children.Add(statusBadge);

            stackPanel.Children.Add(headerGrid);

            var examText = new TextBlock
            {
                Text = $"📝 Экзамен: {app.Exam?.Name ?? "Неизвестно"}",
                FontSize = 14,
                Foreground = new SolidColorBrush(Color.FromRgb(102, 102, 102)),
                Margin = new Thickness(0, 8, 0, 0)
            };
            stackPanel.Children.Add(examText);

            if (app.ExamSchedule != null)
            {
                var scheduleText = new TextBlock
                {
                    Text = $"📅 {app.ExamSchedule.ExamDate:dd.MM.yyyy} в {app.ExamSchedule.StartTime:hh\\:mm}",
                    FontSize = 14,
                    Foreground = new SolidColorBrush(Color.FromRgb(102, 102, 102)),
                    Margin = new Thickness(0, 4, 0, 0)
                };
                stackPanel.Children.Add(scheduleText);

                var locationText = new TextBlock
                {
                    Text = $"📍 {app.ExamSchedule.Location}",
                    FontSize = 14,
                    Foreground = new SolidColorBrush(Color.FromRgb(102, 102, 102)),
                    Margin = new Thickness(0, 4, 0, 0)
                };
                stackPanel.Children.Add(locationText);
            }

            var dateText = new TextBlock
            {
                Text = $"Подано: {app.AppliedAt:dd.MM.yyyy HH:mm}",
                FontSize = 12,
                Foreground = new SolidColorBrush(Color.FromRgb(153, 153, 153)),
                Margin = new Thickness(0, 12, 0, 0)
            };
            stackPanel.Children.Add(dateText);

            if (app.Status == Models.ApplicationStatus.Rejected && !string.IsNullOrEmpty(app.RejectionReason))
            {
                var rejectionText = new TextBlock
                {
                    Text = $"❌ Причина: {app.RejectionReason}",
                    FontSize = 12,
                    Foreground = new SolidColorBrush(Color.FromRgb(231, 76, 60)),
                    Margin = new Thickness(0, 8, 0, 0),
                    TextWrapping = TextWrapping.Wrap
                };
                stackPanel.Children.Add(rejectionText);
            }

            card.Child = stackPanel;
            return card;
        }

        private string GetStatusText(Models.ApplicationStatus status)
        {
            return status switch
            {
                Models.ApplicationStatus.Pending => "Ожидает",
                Models.ApplicationStatus.Approved => "Одобрена",
                Models.ApplicationStatus.Rejected => "Отклонена",
                Models.ApplicationStatus.Completed => "Завершена",
                _ => "Неизвестно"
            };
        }

        private Color GetStatusColor(Models.ApplicationStatus status)
        {
            return status switch
            {
                Models.ApplicationStatus.Pending => Color.FromRgb(255, 165, 0),
                Models.ApplicationStatus.Approved => Color.FromRgb(0, 184, 148),
                Models.ApplicationStatus.Rejected => Color.FromRgb(231, 76, 60),
                Models.ApplicationStatus.Completed => Color.FromRgb(0, 206, 201),
                _ => Colors.Gray
            };
        }

        private FrameworkElement CreateStatsCard(string title, string value, string color, string iconKind)
        {
            var card = new Border();
            card.Background = Brushes.White;
            card.CornerRadius = new CornerRadius(8);
            card.Margin = new Thickness(0, 0, 15, 0);
            card.Padding = new Thickness(20);

            var stackPanel = new StackPanel();

            var iconStackPanel = new StackPanel();
            iconStackPanel.Orientation = Orientation.Horizontal;
            iconStackPanel.Margin = new Thickness(0, 0, 0, 10);

            var icon = new TextBlock();
            icon.Text = "📊";
            icon.FontSize = 24;
            icon.Margin = new Thickness(0, 0, 10, 0);

            var titleTextBlock = new TextBlock();
            titleTextBlock.Text = title;
            titleTextBlock.FontSize = 14;
            titleTextBlock.Foreground = new SolidColorBrush(Color.FromRgb(113, 128, 150));

            iconStackPanel.Children.Add(icon);
            iconStackPanel.Children.Add(titleTextBlock);

            var valueTextBlock = new TextBlock();
            valueTextBlock.Text = value;
            valueTextBlock.FontSize = 32;
            valueTextBlock.FontWeight = FontWeights.Bold;
            valueTextBlock.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color));

            stackPanel.Children.Add(iconStackPanel);
            stackPanel.Children.Add(valueTextBlock);

            card.Child = stackPanel;
            return card;
        }

        private FrameworkElement CreateApplicationsList()
        {
            var listView = new ListView();
            listView.Height = 300;

            return listView;
        }

        private object CreateDemoApplicationItem(string subject, string date, string status, string statusColor)
        {
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.Margin = new Thickness(10);

            var subjectTextBlock = new TextBlock { Text = subject, FontWeight = FontWeights.SemiBold };
            Grid.SetColumn(subjectTextBlock, 0);

            var dateTextBlock = new TextBlock { Text = date, Foreground = Brushes.Gray };
            Grid.SetColumn(dateTextBlock, 1);

            var statusTextBlock = new TextBlock
            {
                Text = status,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(statusColor)),
                FontWeight = FontWeights.SemiBold
            };
            Grid.SetColumn(statusTextBlock, 2);

            grid.Children.Add(subjectTextBlock);
            grid.Children.Add(dateTextBlock);
            grid.Children.Add(statusTextBlock);

            return grid;
        }

        private async Task<FrameworkElement> CreateExamsContentAsync()
        {
            if (_viewModel is null) return new TextBlock { Text = "Ошибка загрузки данных" };

            var scrollViewer = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            var stackPanel = new StackPanel { Margin = new Thickness(30) };

            var titleText = new TextBlock
            {
                Text = _viewModel.IsAdmin ? "Управление экзаменами" : "Мои экзамены и результаты",
                FontSize = 24,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 20)
            };
            stackPanel.Children.Add(titleText);

            if (!_viewModel.IsAdmin)
            {
                var resultsTitle = new TextBlock
                {
                    Text = "Мои сертификаты и результаты экзаменов",
                    FontSize = 18,
                    FontWeight = FontWeights.SemiBold,
                    Margin = new Thickness(0, 0, 0, 15)
                };
                stackPanel.Children.Add(resultsTitle);

                var combinedResults = new List<ResultItem>();

                foreach (var cert in _viewModel.Certificates)
                {
                    combinedResults.Add(new ResultItem
                    {
                        Type = cert.Type == Models.CertificateType.CT ? "Сертификат ЦТ" : "Аттестат",
                        Subject = cert.Subject,
                        Score = cert.Score,
                        MaxScore = null,
                        Number = cert.CertificateNumber,
                        Date = cert.UploadedAt,
                        DateLabel = "Дата загрузки",
                        OriginalItem = cert,
                        CanDelete = true,
                        Status = cert.Status
                    });
                }

                foreach (var result in _viewModel.ExamResults)
                {
                    combinedResults.Add(new ResultItem
                    {
                        Type = "Экзамен",
                        Subject = result.Exam?.Name ?? "Неизвестно",
                        Score = result.Score,
                        MaxScore = result.MaxScore,
                        Number = null,
                        Date = result.ExamDate,
                        DateLabel = "Дата экзамена",
                        OriginalItem = result,
                        CanDelete = true,
                        Status = null
                    });
                }

                combinedResults = combinedResults.OrderByDescending(r => r.Date).ToList();

                var combinedDataGrid = new System.Windows.Controls.DataGrid
                {
                    AutoGenerateColumns = false,
                    CanUserAddRows = false,
                    CanUserDeleteRows = false,
                    IsReadOnly = true,
                    HeadersVisibility = DataGridHeadersVisibility.All,
                    GridLinesVisibility = DataGridGridLinesVisibility.All,
                    AlternatingRowBackground = new SolidColorBrush(Color.FromRgb(249, 250, 251)),
                    RowBackground = Brushes.White,
                    Background = Brushes.White,
                    BorderBrush = new SolidColorBrush(Color.FromRgb(229, 231, 235)),
                    BorderThickness = new Thickness(1),
                    Margin = new Thickness(0, 0, 0, 20),
                    FontSize = 14,
                    Height = 400,
                    VerticalContentAlignment = VerticalAlignment.Center,
                    RowHeight = 50,
                    SelectionMode = DataGridSelectionMode.Single,
                    SelectionUnit = DataGridSelectionUnit.Cell
                };
                
                var cellStyle = new Style(typeof(System.Windows.Controls.DataGridCell));
                cellStyle.Setters.Add(new Setter(VerticalContentAlignmentProperty, VerticalAlignment.Center));
                cellStyle.Setters.Add(new Setter(HorizontalContentAlignmentProperty, HorizontalAlignment.Left));
                cellStyle.Setters.Add(new Setter(VerticalAlignmentProperty, VerticalAlignment.Stretch));
                cellStyle.Setters.Add(new Setter(HeightProperty, double.NaN));
                cellStyle.Setters.Add(new Setter(PaddingProperty, new Thickness(16, 12, 16, 12)));
                cellStyle.Setters.Add(new Setter(BorderThicknessProperty, new Thickness(1, 0, 1, 0)));
                cellStyle.Setters.Add(new Setter(BorderBrushProperty, new SolidColorBrush(Color.FromRgb(229, 231, 235))));
                
                var hoverTrigger = new Trigger { Property = UIElement.IsMouseOverProperty, Value = true };
                hoverTrigger.Setters.Add(new Setter(BackgroundProperty, new SolidColorBrush(Color.FromRgb(243, 244, 246))));
                cellStyle.Triggers.Add(hoverTrigger);
                
                var selectedTrigger = new Trigger { Property = System.Windows.Controls.DataGridCell.IsSelectedProperty, Value = true };
                selectedTrigger.Setters.Add(new Setter(BackgroundProperty, new SolidColorBrush(Color.FromRgb(239, 246, 255))));
                cellStyle.Triggers.Add(selectedTrigger);
                
                combinedDataGrid.CellStyle = cellStyle;
                
                var headerStyle = new Style(typeof(System.Windows.Controls.Primitives.DataGridColumnHeader));
                headerStyle.Setters.Add(new Setter(BackgroundProperty, new SolidColorBrush(Color.FromRgb(249, 250, 251))));
                headerStyle.Setters.Add(new Setter(ForegroundProperty, new SolidColorBrush(Color.FromRgb(17, 24, 39))));
                headerStyle.Setters.Add(new Setter(FontWeightProperty, FontWeights.SemiBold));
                headerStyle.Setters.Add(new Setter(FontSizeProperty, 14.0));
                headerStyle.Setters.Add(new Setter(PaddingProperty, new Thickness(12, 10, 12, 10)));
                headerStyle.Setters.Add(new Setter(BorderBrushProperty, new SolidColorBrush(Color.FromRgb(229, 231, 235))));
                headerStyle.Setters.Add(new Setter(BorderThicknessProperty, new Thickness(1, 0, 1, 0)));
                headerStyle.Setters.Add(new Setter(HorizontalContentAlignmentProperty, HorizontalAlignment.Left));
                headerStyle.Setters.Add(new Setter(VerticalContentAlignmentProperty, VerticalAlignment.Center));
                combinedDataGrid.ColumnHeaderStyle = headerStyle;

                var textBlockStyle = new Style(typeof(TextBlock));
                textBlockStyle.Setters.Add(new Setter(VerticalAlignmentProperty, VerticalAlignment.Center));
                textBlockStyle.Setters.Add(new Setter(HorizontalAlignmentProperty, HorizontalAlignment.Left));
                textBlockStyle.Setters.Add(new Setter(MarginProperty, new Thickness(0)));

                var typeColumn = new System.Windows.Controls.DataGridTextColumn
                {
                    Header = "Тип",
                    Binding = new System.Windows.Data.Binding("Type"),
                    Width = new DataGridLength(150),
                    ElementStyle = textBlockStyle
                };
                combinedDataGrid.Columns.Add(typeColumn);

                var subjectColumn = new System.Windows.Controls.DataGridTextColumn
                {
                    Header = "Предмет",
                    Binding = new System.Windows.Data.Binding("Subject"),
                    Width = new DataGridLength(2, DataGridLengthUnitType.Star),
                    ElementStyle = textBlockStyle
                };
                combinedDataGrid.Columns.Add(subjectColumn);

                var scoreColumn = new System.Windows.Controls.DataGridTemplateColumn
                {
                    Header = "Балл",
                    Width = new DataGridLength(120)
                };

                var scoreFactory = new System.Windows.FrameworkElementFactory(typeof(TextBlock));
                scoreFactory.SetBinding(TextBlock.TextProperty, new Binding(".")
                {
                    Converter = new ScoreDisplayConverter()
                });
                scoreFactory.SetValue(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center);
                scoreFactory.SetValue(TextBlock.HorizontalAlignmentProperty, HorizontalAlignment.Left);
                scoreFactory.SetValue(TextBlock.FontWeightProperty, FontWeights.SemiBold);
                scoreFactory.SetValue(TextBlock.ForegroundProperty, new SolidColorBrush(Color.FromRgb(34, 197, 94)));
                scoreFactory.SetValue(TextBlock.FontSizeProperty, 14.0);
                scoreFactory.SetValue(TextBlock.MarginProperty, new Thickness(0));

                var scoreTemplate = new DataTemplate();
                scoreTemplate.VisualTree = scoreFactory;
                scoreColumn.CellTemplate = scoreTemplate;
                combinedDataGrid.Columns.Add(scoreColumn);

                var numberColumn = new System.Windows.Controls.DataGridTextColumn
                {
                    Header = "Номер",
                    Binding = new System.Windows.Data.Binding("Number"),
                    Width = new DataGridLength(1.5, DataGridLengthUnitType.Star),
                    ElementStyle = textBlockStyle
                };
                combinedDataGrid.Columns.Add(numberColumn);

                var dateColumn = new System.Windows.Controls.DataGridTemplateColumn
                {
                    Header = "Дата",
                    Width = new DataGridLength(150)
                };

                var dateFactory = new System.Windows.FrameworkElementFactory(typeof(TextBlock));
                dateFactory.SetBinding(TextBlock.TextProperty, new Binding("Date")
                {
                    StringFormat = "dd.MM.yyyy"
                });
                dateFactory.SetValue(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center);
                dateFactory.SetValue(TextBlock.HorizontalAlignmentProperty, HorizontalAlignment.Left);
                dateFactory.SetValue(TextBlock.MarginProperty, new Thickness(0));

                var dateTemplate = new DataTemplate();
                dateTemplate.VisualTree = dateFactory;
                dateColumn.CellTemplate = dateTemplate;
                combinedDataGrid.Columns.Add(dateColumn);

                var certificateStatusColumn = new System.Windows.Controls.DataGridTemplateColumn
                {
                    Header = "Статус",
                    Width = new DataGridLength(120)
                };

                var certificateStatusFactory = new System.Windows.FrameworkElementFactory(typeof(TextBlock));
                certificateStatusFactory.SetBinding(TextBlock.TextProperty, new Binding("Status")
                {
                    Converter = new Converters.CertificateStatusConverter()
                });
                certificateStatusFactory.SetValue(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center);
                certificateStatusFactory.SetValue(TextBlock.HorizontalAlignmentProperty, HorizontalAlignment.Left);
                certificateStatusFactory.SetValue(TextBlock.MarginProperty, new Thickness(0));

                var certificateStatusTemplate = new DataTemplate();
                certificateStatusTemplate.VisualTree = certificateStatusFactory;
                certificateStatusColumn.CellTemplate = certificateStatusTemplate;
                combinedDataGrid.Columns.Add(certificateStatusColumn);

                var deleteColumn = new System.Windows.Controls.DataGridTemplateColumn
                {
                    Header = "Действия",
                    Width = new DataGridLength(100)
                };

                var deleteButtonFactory = new System.Windows.FrameworkElementFactory(typeof(Button));
                deleteButtonFactory.SetValue(Button.ContentProperty, "Удалить");
                deleteButtonFactory.SetBinding(Button.VisibilityProperty, new Binding("CanDelete")
                {
                    Converter = new System.Windows.Controls.BooleanToVisibilityConverter()
                });
                
                var buttonStyle = new Style(typeof(Button));
                buttonStyle.Setters.Add(new Setter(Button.BackgroundProperty, new SolidColorBrush(Colors.Transparent)));
                buttonStyle.Setters.Add(new Setter(Button.BorderBrushProperty, new SolidColorBrush(Color.FromRgb(239, 68, 68))));
                buttonStyle.Setters.Add(new Setter(Button.BorderThicknessProperty, new Thickness(1.5)));
                buttonStyle.Setters.Add(new Setter(Button.ForegroundProperty, new SolidColorBrush(Color.FromRgb(239, 68, 68))));
                buttonStyle.Setters.Add(new Setter(Button.PaddingProperty, new Thickness(12, 6, 12, 6)));
                buttonStyle.Setters.Add(new Setter(Button.CursorProperty, Cursors.Hand));
                buttonStyle.Setters.Add(new Setter(Button.FontSizeProperty, 12.0));
                buttonStyle.Setters.Add(new Setter(Button.FontWeightProperty, FontWeights.Medium));
                buttonStyle.Setters.Add(new Setter(Button.HeightProperty, 32.0));
                
                var buttonHoverTrigger = new Trigger { Property = UIElement.IsMouseOverProperty, Value = true };
                buttonHoverTrigger.Setters.Add(new Setter(Button.BackgroundProperty, new SolidColorBrush(Color.FromRgb(239, 68, 68))));
                buttonHoverTrigger.Setters.Add(new Setter(Button.ForegroundProperty, Brushes.White));
                buttonStyle.Triggers.Add(buttonHoverTrigger);
                
                deleteButtonFactory.SetValue(Button.StyleProperty, buttonStyle);
                deleteButtonFactory.SetValue(Button.MarginProperty, new Thickness(8, 0, 8, 0));
                deleteButtonFactory.SetValue(Button.VerticalAlignmentProperty, VerticalAlignment.Center);
                deleteButtonFactory.AddHandler(Button.ClickEvent, new RoutedEventHandler(async (s, e) =>
                {
                    if (s is Button btn && btn.DataContext is ResultItem resultItem)
                    {
                        if (resultItem.OriginalItem is Models.Certificate cert)
                        {
                            var documentType = cert.Type == Models.CertificateType.CT ? "сертификат ЦТ" : "аттестат";
                            var result = MessageBox.Show(
                                $"Вы уверены, что хотите удалить {documentType}?\n\nТип: {(cert.Type == Models.CertificateType.CT ? "ЦТ" : "Аттестат")}\nПредмет: {cert.Subject}\nБалл: {cert.Score}",
                                "Подтверждение удаления",
                                MessageBoxButton.YesNo,
                                MessageBoxImage.Question);

                            if (result == MessageBoxResult.Yes)
                            {
                                try
                                {
                                    using (var context = new Data.ApplicationDbContext())
                                    {
                                        var certificateToDelete = await context.Certificates.FindAsync(cert.Id);
                                        if (certificateToDelete != null)
                                        {
                                            if (_viewModel?.CurrentUser != null && certificateToDelete.UserId != _viewModel.CurrentUser.Id)
                                            {
                                                MessageBox.Show("Вы можете удалять только свои сертификаты.", "Ошибка",
                                                    MessageBoxButton.OK, MessageBoxImage.Warning);
                                                return;
                                            }

                                            if (!string.IsNullOrEmpty(certificateToDelete.FilePath) && File.Exists(certificateToDelete.FilePath))
                                            {
                                                try
                                                {
                                                    File.Delete(certificateToDelete.FilePath);
                                                }
                                                catch
                                                {
                                                }
                                            }

                                            context.Certificates.Remove(certificateToDelete);
                                            await context.SaveChangesAsync();

                                            if (_viewModel != null)
                                            {
                                                await _viewModel.LoadDataAsync();
                                            }

                                            var newContent = await CreateExamsContentAsync();
                                            MainContentPresenter.Content = newContent;

                                            var deletedDocumentType = cert.Type == Models.CertificateType.CT ? "Сертификат ЦТ" : "Аттестат";
                                            MessageBox.Show($"{deletedDocumentType} успешно удален!", "Успех",
                                                MessageBoxButton.OK, MessageBoxImage.Information);
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    var errorDocumentType = cert.Type == Models.CertificateType.CT ? "сертификата ЦТ" : "аттестата";
                                    MessageBox.Show($"Ошибка при удалении {errorDocumentType}: {ex.Message}", "Ошибка",
                                        MessageBoxButton.OK, MessageBoxImage.Error);
                                }
                            }
                        }
                        else if (resultItem.OriginalItem is Models.ExamResult examResult)
                        {
                            var maxScoreText = resultItem.MaxScore.HasValue ? $"/{resultItem.MaxScore.Value}" : "";
                            var result = MessageBox.Show(
                                $"Вы уверены, что хотите удалить результат экзамена?\n\nЭкзамен: {examResult.Exam?.Name ?? "Неизвестно"}\nБалл: {examResult.Score}{maxScoreText}\nДата экзамена: {examResult.ExamDate:dd.MM.yyyy}",
                                "Подтверждение удаления",
                                MessageBoxButton.YesNo,
                                MessageBoxImage.Question);

                            if (result == MessageBoxResult.Yes)
                            {
                                await DeleteExamResultAsync(examResult);
                            }
                        }
                    }
                }));

                var deleteCellTemplate = new DataTemplate();
                deleteCellTemplate.VisualTree = deleteButtonFactory;
                deleteColumn.CellTemplate = deleteCellTemplate;
                combinedDataGrid.Columns.Add(deleteColumn);

                combinedDataGrid.ItemsSource = combinedResults;
                stackPanel.Children.Add(combinedDataGrid);

                var totalScore = _viewModel.Certificates?
                    .Where(c => c.Status == Models.CertificateStatus.Approved)
                    .Sum(c => c.Score) ?? 0;
                var totalScoreBorder = new Border
                {
                    Background = new SolidColorBrush(Color.FromRgb(240, 248, 255)),
                    BorderBrush = new SolidColorBrush(Color.FromRgb(59, 130, 246)),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(4),
                    Padding = new Thickness(12, 8, 12, 8),
                    Margin = new Thickness(0, 0, 0, 20),
                    HorizontalAlignment = HorizontalAlignment.Stretch
                };

                var totalScorePanel = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Right
                };

                var totalScoreLabel = new TextBlock
                {
                    Text = "Итого баллов:",
                    FontSize = 16,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = new SolidColorBrush(Color.FromRgb(30, 58, 138)),
                    Margin = new Thickness(0, 0, 10, 0),
                    VerticalAlignment = VerticalAlignment.Center
                };

                var totalScoreValue = new TextBlock
                {
                    Text = totalScore.ToString(),
                    FontSize = 18,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(Color.FromRgb(30, 58, 138)),
                    VerticalAlignment = VerticalAlignment.Center
                };

                totalScorePanel.Children.Add(totalScoreLabel);
                totalScorePanel.Children.Add(totalScoreValue);
                totalScoreBorder.Child = totalScorePanel;
                stackPanel.Children.Add(totalScoreBorder);

               var uploadButton = new Button
               {
                   Content = "Загрузить сертификат",
                   Style = (Style)FindResource("MaterialDesignRaisedButton"),
                   Background = GetAccentBrush(_viewModel.IsAdmin),
                   Height = 50,
                   FontSize = 16,
                   FontWeight = FontWeights.SemiBold,
                   Margin = new Thickness(0, 0, 0, 20)
               };
                uploadButton.Click += async (s, e) => 
                {
                    await UploadCertificate();
                    var newContent = await CreateExamsContentAsync();
                    MainContentPresenter.Content = newContent;
                };
                stackPanel.Children.Add(uploadButton);

                var universitiesTitle = new TextBlock
                {
                    Text = "Подходящие университеты",
                    FontSize = 18,
                    FontWeight = FontWeights.SemiBold,
                    Margin = new Thickness(0, 30, 0, 15)
                };
                stackPanel.Children.Add(universitiesTitle);

                await _viewModel.LoadMatchedUniversitiesAsync();

                if (_viewModel.MatchedSpecialities.Any())
                {
                    var universitiesDataGrid = new System.Windows.Controls.DataGrid
                    {
                        AutoGenerateColumns = false,
                        CanUserAddRows = false,
                        CanUserDeleteRows = false,
                        IsReadOnly = true,
                        HeadersVisibility = DataGridHeadersVisibility.All,
                        GridLinesVisibility = DataGridGridLinesVisibility.All,
                        AlternatingRowBackground = new SolidColorBrush(Color.FromRgb(249, 250, 251)),
                        RowBackground = Brushes.White,
                        Background = Brushes.White,
                        BorderBrush = new SolidColorBrush(Color.FromRgb(229, 231, 235)),
                        BorderThickness = new Thickness(1),
                        Margin = new Thickness(0, 0, 0, 20),
                        FontSize = 14,
                        MaxHeight = 400,
                        VerticalContentAlignment = VerticalAlignment.Center,
                        RowHeight = 50,
                        VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                        HorizontalScrollBarVisibility = ScrollBarVisibility.Auto
                    };
                    
                    var universitiesCellStyle = new Style(typeof(System.Windows.Controls.DataGridCell));
                    universitiesCellStyle.Setters.Add(new Setter(VerticalContentAlignmentProperty, VerticalAlignment.Center));
                    universitiesCellStyle.Setters.Add(new Setter(HorizontalContentAlignmentProperty, HorizontalAlignment.Left));
                    universitiesCellStyle.Setters.Add(new Setter(VerticalAlignmentProperty, VerticalAlignment.Stretch));
                    universitiesCellStyle.Setters.Add(new Setter(HeightProperty, double.NaN));
                    universitiesCellStyle.Setters.Add(new Setter(PaddingProperty, new Thickness(16, 12, 16, 12)));
                    universitiesCellStyle.Setters.Add(new Setter(BorderThicknessProperty, new Thickness(1, 0, 1, 0)));
                    universitiesCellStyle.Setters.Add(new Setter(BorderBrushProperty, new SolidColorBrush(Color.FromRgb(229, 231, 235))));
                    
                    var universitiesHoverTrigger = new Trigger { Property = UIElement.IsMouseOverProperty, Value = true };
                    universitiesHoverTrigger.Setters.Add(new Setter(BackgroundProperty, new SolidColorBrush(Color.FromRgb(243, 244, 246))));
                    universitiesCellStyle.Triggers.Add(universitiesHoverTrigger);
                    
                    var universitiesSelectedTrigger = new Trigger { Property = System.Windows.Controls.DataGridCell.IsSelectedProperty, Value = true };
                    universitiesSelectedTrigger.Setters.Add(new Setter(BackgroundProperty, new SolidColorBrush(Color.FromRgb(239, 246, 255))));
                    universitiesCellStyle.Triggers.Add(universitiesSelectedTrigger);
                    
                    universitiesDataGrid.CellStyle = universitiesCellStyle;
                    
                    var universitiesTextBlockStyle = new Style(typeof(TextBlock));
                    universitiesTextBlockStyle.Setters.Add(new Setter(VerticalAlignmentProperty, VerticalAlignment.Center));
                    universitiesTextBlockStyle.Setters.Add(new Setter(HorizontalAlignmentProperty, HorizontalAlignment.Left));
                    universitiesTextBlockStyle.Setters.Add(new Setter(MarginProperty, new Thickness(0)));
                    
                    var universitiesHeaderStyle = new Style(typeof(System.Windows.Controls.Primitives.DataGridColumnHeader));
                    universitiesHeaderStyle.Setters.Add(new Setter(BackgroundProperty, new SolidColorBrush(Color.FromRgb(249, 250, 251))));
                    universitiesHeaderStyle.Setters.Add(new Setter(ForegroundProperty, new SolidColorBrush(Color.FromRgb(17, 24, 39))));
                    universitiesHeaderStyle.Setters.Add(new Setter(FontWeightProperty, FontWeights.SemiBold));
                    universitiesHeaderStyle.Setters.Add(new Setter(FontSizeProperty, 14.0));
                    universitiesHeaderStyle.Setters.Add(new Setter(PaddingProperty, new Thickness(12, 10, 12, 10)));
                    universitiesHeaderStyle.Setters.Add(new Setter(BorderBrushProperty, new SolidColorBrush(Color.FromRgb(229, 231, 235))));
                    universitiesHeaderStyle.Setters.Add(new Setter(BorderThicknessProperty, new Thickness(0, 0, 1, 2)));
                    universitiesHeaderStyle.Setters.Add(new Setter(HorizontalContentAlignmentProperty, HorizontalAlignment.Left));
                    universitiesHeaderStyle.Setters.Add(new Setter(VerticalContentAlignmentProperty, VerticalAlignment.Center));
                    universitiesDataGrid.ColumnHeaderStyle = universitiesHeaderStyle;

                    universitiesDataGrid.Columns.Add(new System.Windows.Controls.DataGridTextColumn
                    {
                        Header = "Университет",
                        Binding = new System.Windows.Data.Binding("University.Name"),
                        Width = new DataGridLength(2, DataGridLengthUnitType.Star),
                        ElementStyle = universitiesTextBlockStyle
                    });

                    universitiesDataGrid.Columns.Add(new System.Windows.Controls.DataGridTextColumn
                    {
                        Header = "Специальность",
                        Binding = new System.Windows.Data.Binding("Speciality.Name"),
                        Width = new DataGridLength(2, DataGridLengthUnitType.Star),
                        ElementStyle = universitiesTextBlockStyle
                    });

                    universitiesDataGrid.Columns.Add(new System.Windows.Controls.DataGridTextColumn
                    {
                        Header = "Код",
                        Binding = new System.Windows.Data.Binding("Speciality.Code"),
                        Width = new DataGridLength(100),
                        ElementStyle = universitiesTextBlockStyle
                    });

                    var statusColumn = new System.Windows.Controls.DataGridTemplateColumn
                    {
                        Header = "Статус",
                        Width = new DataGridLength(150)
                    };

                    var statusFactory = new System.Windows.FrameworkElementFactory(typeof(TextBlock));
                    statusFactory.SetBinding(TextBlock.TextProperty, new Binding(".")
                    {
                        Converter = new BooleanToStatusConverter()
                    });
                    statusFactory.SetBinding(TextBlock.ForegroundProperty, new Binding(".")
                    {
                        Converter = new BooleanToColorConverter()
                    });
                    statusFactory.SetValue(TextBlock.FontWeightProperty, FontWeights.SemiBold);
                    statusFactory.SetValue(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center);
                    statusFactory.SetValue(TextBlock.HorizontalAlignmentProperty, HorizontalAlignment.Center);
                    statusFactory.SetValue(TextBlock.MarginProperty, new Thickness(0));

                    var statusTemplate = new DataTemplate();
                    statusTemplate.VisualTree = statusFactory;
                    statusColumn.CellTemplate = statusTemplate;
                    universitiesDataGrid.Columns.Add(statusColumn);

                    var subjectsColumn = new System.Windows.Controls.DataGridTemplateColumn
                    {
                        Header = "Предметы",
                        Width = new DataGridLength(2, DataGridLengthUnitType.Star)
                    };

                    var subjectsFactory = new System.Windows.FrameworkElementFactory(typeof(TextBlock));
                    subjectsFactory.SetBinding(TextBlock.TextProperty, new Binding(".")
                    {
                        Converter = new MatchedSubjectsConverter()
                    });
                    subjectsFactory.SetValue(TextBlock.TextWrappingProperty, TextWrapping.Wrap);
                    subjectsFactory.SetValue(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center);
                    subjectsFactory.SetValue(TextBlock.HorizontalAlignmentProperty, HorizontalAlignment.Left);
                    subjectsFactory.SetValue(TextBlock.MarginProperty, new Thickness(0));

                    var subjectsTemplate = new DataTemplate();
                    subjectsTemplate.VisualTree = subjectsFactory;
                    subjectsColumn.CellTemplate = subjectsTemplate;
                    universitiesDataGrid.Columns.Add(subjectsColumn);

                    universitiesDataGrid.Columns.Add(new System.Windows.Controls.DataGridTextColumn
                    {
                        Header = "Общий балл",
                        Binding = new System.Windows.Data.Binding("TotalScore"),
                        Width = new DataGridLength(100),
                        ElementStyle = universitiesTextBlockStyle
                    });

                    universitiesDataGrid.Columns.Add(new System.Windows.Controls.DataGridTextColumn
                    {
                        Header = "Бюджет",
                        Binding = new System.Windows.Data.Binding("Speciality.BudgetPlaces"),
                        Width = new DataGridLength(100),
                        ElementStyle = universitiesTextBlockStyle
                    });

                    universitiesDataGrid.Columns.Add(new System.Windows.Controls.DataGridTextColumn
                    {
                        Header = "Платно",
                        Binding = new System.Windows.Data.Binding("Speciality.PaidPlaces"),
                        Width = new DataGridLength(100),
                        ElementStyle = universitiesTextBlockStyle
                    });

                    universitiesDataGrid.ItemsSource = _viewModel.MatchedSpecialities;
                    stackPanel.Children.Add(universitiesDataGrid);
                }
                else
                {
                    var noUniversitiesText = new TextBlock
                    {
                        Text = "Подходящие университеты не найдены. Убедитесь, что у вас загружены все необходимые сертификаты.",
                        FontSize = 14,
                        Foreground = new SolidColorBrush(Color.FromRgb(107, 114, 128)),
                        Margin = new Thickness(0, 0, 0, 20),
                        TextWrapping = TextWrapping.Wrap
                    };
                    stackPanel.Children.Add(noUniversitiesText);
                }
            }
            else
            {
                var manageButton = new Button
                {
                    Content = "Управление экзаменами",
                    Style = (Style)FindResource("MaterialDesignRaisedButton"),
                    Background = GetAccentBrush(_viewModel.IsAdmin),
                    Height = 50,
                    FontSize = 16,
                    FontWeight = FontWeights.SemiBold,
                    Margin = new Thickness(0, 0, 0, 20)
                };
                manageButton.Click += (s, e) => OpenExamManagement();
                stackPanel.Children.Add(manageButton);

                var examsDataGrid = new System.Windows.Controls.DataGrid
                {
                    AutoGenerateColumns = false,
                    CanUserAddRows = false,
                    CanUserDeleteRows = false,
                    IsReadOnly = true,
                    HeadersVisibility = DataGridHeadersVisibility.All,
                    GridLinesVisibility = DataGridGridLinesVisibility.All,
                    AlternatingRowBackground = new SolidColorBrush(Color.FromRgb(245, 247, 250)),
                    RowBackground = Brushes.White,
                    Background = Brushes.White,
                    BorderBrush = new SolidColorBrush(Color.FromRgb(226, 232, 240)),
                    BorderThickness = new Thickness(1),
                    Margin = new Thickness(0, 0, 0, 20),
                    FontSize = 14,
                    Height = 400,
                    VerticalContentAlignment = VerticalAlignment.Center,
                    RowHeight = 40,
                    VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                    HorizontalScrollBarVisibility = ScrollBarVisibility.Auto
                };
                
                var examsCellStyle = new Style(typeof(System.Windows.Controls.DataGridCell));
                examsCellStyle.Setters.Add(new Setter(VerticalContentAlignmentProperty, VerticalAlignment.Center));
                examsCellStyle.Setters.Add(new Setter(HorizontalContentAlignmentProperty, HorizontalAlignment.Left));
                examsCellStyle.Setters.Add(new Setter(VerticalAlignmentProperty, VerticalAlignment.Stretch));
                examsCellStyle.Setters.Add(new Setter(HeightProperty, double.NaN));
                examsCellStyle.Setters.Add(new Setter(PaddingProperty, new Thickness(16, 12, 16, 12)));
                examsCellStyle.Setters.Add(new Setter(BorderThicknessProperty, new Thickness(1, 0, 1, 0)));
                examsCellStyle.Setters.Add(new Setter(BorderBrushProperty, new SolidColorBrush(Color.FromRgb(229, 231, 235))));
                
                var examsHoverTrigger = new Trigger { Property = UIElement.IsMouseOverProperty, Value = true };
                examsHoverTrigger.Setters.Add(new Setter(BackgroundProperty, new SolidColorBrush(Color.FromRgb(243, 244, 246))));
                examsCellStyle.Triggers.Add(examsHoverTrigger);
                
                var examsSelectedTrigger = new Trigger { Property = System.Windows.Controls.DataGridCell.IsSelectedProperty, Value = true };
                examsSelectedTrigger.Setters.Add(new Setter(BackgroundProperty, new SolidColorBrush(Color.FromRgb(239, 246, 255))));
                examsCellStyle.Triggers.Add(examsSelectedTrigger);
                
                examsDataGrid.CellStyle = examsCellStyle;
                
                var examsTextBlockStyle = new Style(typeof(TextBlock));
                examsTextBlockStyle.Setters.Add(new Setter(VerticalAlignmentProperty, VerticalAlignment.Center));
                examsTextBlockStyle.Setters.Add(new Setter(HorizontalAlignmentProperty, HorizontalAlignment.Left));
                examsTextBlockStyle.Setters.Add(new Setter(MarginProperty, new Thickness(0)));

                examsDataGrid.Columns.Add(new System.Windows.Controls.DataGridTextColumn
                {
                    Header = "Название",
                    Binding = new System.Windows.Data.Binding("Name"),
                    Width = new DataGridLength(2, DataGridLengthUnitType.Star),
                    ElementStyle = examsTextBlockStyle
                });

                examsDataGrid.Columns.Add(new System.Windows.Controls.DataGridTextColumn
                {
                    Header = "Предмет",
                    Binding = new System.Windows.Data.Binding("Subject"),
                    Width = new DataGridLength(1.5, DataGridLengthUnitType.Star),
                    ElementStyle = examsTextBlockStyle
                });

                examsDataGrid.Columns.Add(new System.Windows.Controls.DataGridTextColumn
                {
                    Header = "Описание",
                    Binding = new System.Windows.Data.Binding("Description"),
                    Width = new DataGridLength(2, DataGridLengthUnitType.Star),
                    ElementStyle = examsTextBlockStyle
                });

                examsDataGrid.Columns.Add(new System.Windows.Controls.DataGridTextColumn
                {
                    Header = "Длительность (мин)",
                    Binding = new System.Windows.Data.Binding("DurationMinutes"),
                    Width = new DataGridLength(150),
                    ElementStyle = examsTextBlockStyle
                });

                examsDataGrid.Columns.Add(new System.Windows.Controls.DataGridTextColumn
                {
                    Header = "Макс. балл",
                    Binding = new System.Windows.Data.Binding("MaxScore"),
                    Width = new DataGridLength(100),
                    ElementStyle = examsTextBlockStyle
                });

                examsDataGrid.Columns.Add(new System.Windows.Controls.DataGridCheckBoxColumn
                {
                    Header = "Активен",
                    Binding = new System.Windows.Data.Binding("IsActive"),
                    Width = new DataGridLength(100)
                });

                examsDataGrid.Columns.Add(new System.Windows.Controls.DataGridTextColumn
                {
                    Header = "Дата создания",
                    Binding = new System.Windows.Data.Binding("CreatedAt") { StringFormat = "dd.MM.yyyy" },
                    Width = new DataGridLength(120),
                    ElementStyle = examsTextBlockStyle
                });

                List<Models.Exam> exams;
                using (var context = new Data.ApplicationDbContext())
                {
                    exams = await context.Exams.OrderBy(e => e.Name).ToListAsync();
                }
                examsDataGrid.ItemsSource = exams;
                stackPanel.Children.Add(examsDataGrid);
            }

            scrollViewer.Content = stackPanel;
            return scrollViewer;
        }

        private FrameworkElement CreateResultItem(ExamResult result)
        {
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.Margin = new Thickness(10);

            var examText = new TextBlock
            {
                Text = result.Exam?.Name ?? "Неизвестно",
                FontWeight = FontWeights.SemiBold
            };
            Grid.SetColumn(examText, 0);

            var scoreText = new TextBlock
            {
                Text = $"{result.Score} / {result.MaxScore}",
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(0, 184, 148))
            };
            Grid.SetColumn(scoreText, 1);

            var dateText = new TextBlock
            {
                Text = result.ExamDate.ToString("dd.MM.yyyy"),
                Foreground = Brushes.Gray
            };
            Grid.SetColumn(dateText, 2);

            grid.Children.Add(examText);
            grid.Children.Add(scoreText);
            grid.Children.Add(dateText);

            return grid;
        }

        private async Task UploadCertificate()
        {
            if (_viewModel?.CurrentUser == null) return;

            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Image files (*.jpg;*.jpeg;*.png;*.bmp)|*.jpg;*.jpeg;*.png;*.bmp|PDF files (*.pdf)|*.pdf|All files (*.*)|*.*",
                Title = "Выберите файл сертификата"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    var fileName = Path.GetFileName(openFileDialog.FileName);
                    var uploadPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Certificates");
                    Directory.CreateDirectory(uploadPath);

                    var destinationPath = Path.Combine(uploadPath, $"{_viewModel.CurrentUser.Id}_{Guid.NewGuid()}_{fileName}");
                    File.Copy(openFileDialog.FileName, destinationPath, true);

                    Dictionary<string, string> ocrData = new Dictionary<string, string>();
                    try
                    {
                    var ocrService = new Services.OCRService();
                        ocrData = await ocrService.ExtractCertificateDataAsync(openFileDialog.FileName);
                        
                        if (ocrData.Count > 0)
                        {
                            System.Diagnostics.Debug.WriteLine($"OCR распознано полей: {ocrData.Count}");
                            foreach (var kvp in ocrData)
                            {
                                System.Diagnostics.Debug.WriteLine($"  {kvp.Key}: {kvp.Value}");
                            }
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine("OCR не распознал данные. Показываем пустой диалог.");
                        }
                    }
                    catch (Exception ocrEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"Ошибка OCR: {ocrEx.Message}");
                        System.Diagnostics.Debug.WriteLine($"StackTrace: {ocrEx.StackTrace}");
                            MessageBox.Show($"Ошибка при распознавании текста: {ocrEx.Message}\n\nВы можете ввести данные вручную.", 
                            "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }

                    using (var checkContext = new Data.ApplicationDbContext())
                    {
                        var existingCertificates = await checkContext.Certificates
                            .Where(c => c.UserId == _viewModel.CurrentUser.Id)
                            .ToListAsync();

                        var certificateTypeFromOCR = Models.CertificateType.CT;
                        if (ocrData.ContainsKey("Type") && ocrData["Type"] == "Diploma")
                        {
                            certificateTypeFromOCR = Models.CertificateType.Diploma;
                        }

                        if (certificateTypeFromOCR == Models.CertificateType.Diploma)
                        {
                            var diplomaCount = existingCertificates.Count(c => c.Type == Models.CertificateType.Diploma);
                            if (diplomaCount >= 1)
                            {
                                MessageBox.Show("Вы уже загрузили максимальное количество аттестатов (1).\n\nУдалите существующий аттестат перед загрузкой нового.",
                                    "Ограничение", MessageBoxButton.OK, MessageBoxImage.Warning);
                                return;
                            }
                        }
                        else
                        {
                            var ctCount = existingCertificates.Count(c => c.Type == Models.CertificateType.CT);
                            if (ctCount >= 3)
                            {
                                MessageBox.Show("Вы уже загрузили максимальное количество сертификатов ЦТ (3).\n\nУдалите существующие сертификаты перед загрузкой новых.",
                                    "Ограничение", MessageBoxButton.OK, MessageBoxImage.Warning);
                                return;
                            }
                        }
                    }

                    var dialog = new Views.CertificateUploadDialog(ocrData);
                    if (dialog.ShowDialog() == true)
                    {
                        using (var checkContext2 = new Data.ApplicationDbContext())
                        {
                            var existingCertificates = await checkContext2.Certificates
                                .Where(c => c.UserId == _viewModel.CurrentUser.Id)
                                .ToListAsync();

                            if (dialog.CertificateType == Models.CertificateType.Diploma)
                            {
                                var diplomaCount = existingCertificates.Count(c => c.Type == Models.CertificateType.Diploma);
                                if (diplomaCount >= 1)
                                {
                                    MessageBox.Show("Вы уже загрузили максимальное количество аттестатов (1).\n\nУдалите существующий аттестат перед загрузкой нового.",
                                        "Ограничение", MessageBoxButton.OK, MessageBoxImage.Warning);
                                    return;
                                }
                            }
                            else
                            {
                                var ctCount = existingCertificates.Count(c => c.Type == Models.CertificateType.CT);
                                if (ctCount >= 3)
                                {
                                    MessageBox.Show("Вы уже загрузили максимальное количество сертификатов ЦТ (3).\n\nУдалите существующие сертификаты перед загрузкой новых.",
                                        "Ограничение", MessageBoxButton.OK, MessageBoxImage.Warning);
                                    return;
                                }
                            }
                        }

                        if (!string.IsNullOrWhiteSpace(dialog.CertificateNumber))
                        {
                            using (var checkContext = new Data.ApplicationDbContext())
                            {
                                var existingCertificate = await checkContext.Certificates
                                    .FirstOrDefaultAsync(c => c.CertificateNumber == dialog.CertificateNumber.Trim());

                                if (existingCertificate != null)
                                {
                                    var documentType = dialog.CertificateType == Models.CertificateType.CT ? "сертификат ЦТ" : "аттестат";
                                    MessageBox.Show($"Сертификат с номером {dialog.CertificateNumber} уже существует в системе.\n\nНомер сертификата должен быть уникальным.",
                                        "Дублирование", MessageBoxButton.OK, MessageBoxImage.Warning);
                                    return;
                                }
                            }
                        }

                        var context = new Data.ApplicationDbContext();
                        var certificate = new Models.Certificate
                        {
                            UserId = _viewModel.CurrentUser.Id,
                            Type = dialog.CertificateType,
                            Subject = dialog.Subject,
                            Score = dialog.Score,
                            CertificateNumber = dialog.CertificateNumber ?? string.Empty,
                            FilePath = destinationPath,
                            FileName = fileName,
                            UploadedAt = DateTime.Now
                        };

                        context.Certificates.Add(certificate);
                        await context.SaveChangesAsync();

                        if (_viewModel != null)
                        {
                            await _viewModel.LoadDataAsync();
                        }

                        var uploadedDocumentType = dialog.CertificateType == Models.CertificateType.CT ? "Сертификат ЦТ" : "Аттестат";
                        MessageBox.Show($"{uploadedDocumentType} успешно загружен!", "Успех",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при загрузке документа: {ex.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async void OpenExamManagement()
        {
            var window = new Views.ExamManagementWindow();
            window.ShowDialog();
            if (_viewModel is not null)
            {
                await _viewModel.LoadDataAsync();
                if (PageTitleTextBlock.Text == "Экзамены")
                {
                    var newContent = await CreateExamsContentAsync();
                    MainContentPresenter.Content = newContent;
                }
            }
        }

        private FrameworkElement CreateScheduleContent()
        {
            if (_viewModel is null) return new TextBlock { Text = "Ошибка загрузки данных" };

            var scrollViewer = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            var stackPanel = new StackPanel { Margin = new Thickness(30) };

            var titleText = new TextBlock
            {
                Text = _viewModel.IsAdmin ? "Управление расписанием" : "Мое расписание экзаменов",
                FontSize = 24,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 20)
            };
            stackPanel.Children.Add(titleText);

            if (_viewModel.IsAdmin)
            {
                var manageButton = new Button
                {
                    Content = "Управление расписанием",
                    Style = (Style)FindResource("MaterialDesignRaisedButton"),
                    Background = GetAccentBrush(_viewModel.IsAdmin),
                    Height = 50,
                    FontSize = 16,
                    FontWeight = FontWeights.SemiBold,
                    Margin = new Thickness(0, 0, 0, 20)
                };
                manageButton.Click += (s, e) => OpenScheduleManagement();
                stackPanel.Children.Add(manageButton);
            }

            var dataGrid = new DataGrid
            {
                AutoGenerateColumns = false,
                CanUserAddRows = false,
                CanUserDeleteRows = false,
                IsReadOnly = true,
                HeadersVisibility = DataGridHeadersVisibility.All,
                GridLinesVisibility = DataGridGridLinesVisibility.All,
                AlternatingRowBackground = new SolidColorBrush(Color.FromRgb(245, 247, 250)),
                RowBackground = Brushes.White,
                Background = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(226, 232, 240)),
                BorderThickness = new Thickness(1),
                Margin = new Thickness(0, 0, 0, 20),
                FontSize = 14,
                VerticalContentAlignment = VerticalAlignment.Center,
                RowHeight = 40
            };
            
            var scheduleCellStyle = new Style(typeof(System.Windows.Controls.DataGridCell));
            scheduleCellStyle.Setters.Add(new Setter(VerticalContentAlignmentProperty, VerticalAlignment.Center));
            scheduleCellStyle.Setters.Add(new Setter(HorizontalContentAlignmentProperty, HorizontalAlignment.Left));
            scheduleCellStyle.Setters.Add(new Setter(VerticalAlignmentProperty, VerticalAlignment.Stretch));
            scheduleCellStyle.Setters.Add(new Setter(HeightProperty, double.NaN));
            scheduleCellStyle.Setters.Add(new Setter(PaddingProperty, new Thickness(16, 12, 16, 12)));
            scheduleCellStyle.Setters.Add(new Setter(BorderThicknessProperty, new Thickness(1, 0, 1, 0)));
            scheduleCellStyle.Setters.Add(new Setter(BorderBrushProperty, new SolidColorBrush(Color.FromRgb(229, 231, 235))));
            
            var scheduleHoverTrigger = new Trigger { Property = UIElement.IsMouseOverProperty, Value = true };
            scheduleHoverTrigger.Setters.Add(new Setter(BackgroundProperty, new SolidColorBrush(Color.FromRgb(243, 244, 246))));
            scheduleCellStyle.Triggers.Add(scheduleHoverTrigger);
            
            var scheduleSelectedTrigger = new Trigger { Property = System.Windows.Controls.DataGridCell.IsSelectedProperty, Value = true };
            scheduleSelectedTrigger.Setters.Add(new Setter(BackgroundProperty, new SolidColorBrush(Color.FromRgb(239, 246, 255))));
            scheduleCellStyle.Triggers.Add(scheduleSelectedTrigger);
            
            dataGrid.CellStyle = scheduleCellStyle;
            
            var scheduleTextBlockStyle = new Style(typeof(TextBlock));
            scheduleTextBlockStyle.Setters.Add(new Setter(VerticalAlignmentProperty, VerticalAlignment.Center));
            scheduleTextBlockStyle.Setters.Add(new Setter(HorizontalAlignmentProperty, HorizontalAlignment.Left));
            scheduleTextBlockStyle.Setters.Add(new Setter(MarginProperty, new Thickness(0)));

            dataGrid.Columns.Add(new System.Windows.Controls.DataGridTextColumn
            {
                Header = "Экзамен",
                Binding = new System.Windows.Data.Binding("Exam.Name"),
                Width = new DataGridLength(2, DataGridLengthUnitType.Star),
                ElementStyle = scheduleTextBlockStyle
            });

            dataGrid.Columns.Add(new System.Windows.Controls.DataGridTextColumn
            {
                Header = "Дата",
                Binding = new System.Windows.Data.Binding("ExamDate") { StringFormat = "dd.MM.yyyy" },
                Width = new DataGridLength(120),
                ElementStyle = scheduleTextBlockStyle
            });

            dataGrid.Columns.Add(new System.Windows.Controls.DataGridTextColumn
            {
                Header = "Время",
                Binding = new System.Windows.Data.Binding("StartTime") { StringFormat = "hh\\:mm" },
                Width = new DataGridLength(100),
                ElementStyle = scheduleTextBlockStyle
            });

            dataGrid.Columns.Add(new System.Windows.Controls.DataGridTextColumn
            {
                Header = "Место проведения",
                Binding = new System.Windows.Data.Binding("Location"),
                Width = new DataGridLength(2, DataGridLengthUnitType.Star),
                ElementStyle = scheduleTextBlockStyle
            });

            var freePlacesColumn = new System.Windows.Controls.DataGridTextColumn
            {
                Header = "Свободно мест",
                Width = new DataGridLength(150),
                ElementStyle = scheduleTextBlockStyle
            };
            freePlacesColumn.Binding = new Binding
            {
                Path = new PropertyPath("."),
                Converter = new FreePlacesConverter()
            };
            dataGrid.Columns.Add(freePlacesColumn);

            dataGrid.ItemsSource = _viewModel.ExamSchedules;
            dataGrid.Items.Refresh();
            stackPanel.Children.Add(dataGrid);

            scrollViewer.Content = stackPanel;
            return scrollViewer;
        }

        private FrameworkElement CreateScheduleItem(ExamSchedule schedule)
        {
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.Margin = new Thickness(10);

            var examText = new TextBlock
            {
                Text = schedule.Exam?.Name ?? "Неизвестно",
                FontWeight = FontWeights.SemiBold
            };
            Grid.SetColumn(examText, 0);

            var dateText = new TextBlock
            {
                Text = schedule.ExamDate.ToString("dd.MM.yyyy"),
                Foreground = Brushes.Gray
            };
            Grid.SetColumn(dateText, 1);

            var timeText = new TextBlock
            {
                Text = schedule.StartTime.ToString(@"hh\:mm"),
                Foreground = Brushes.Gray
            };
            Grid.SetColumn(timeText, 2);

            var locationText = new TextBlock
            {
                Text = schedule.Location,
                Foreground = Brushes.Gray
            };
            Grid.SetColumn(locationText, 3);

            grid.Children.Add(examText);
            grid.Children.Add(dateText);
            grid.Children.Add(timeText);
            grid.Children.Add(locationText);

            return grid;
        }

        private async void OpenScheduleManagement()
        {
            var window = new Views.ScheduleManagementWindow();
            window.ShowDialog();
            if (_viewModel is not null)
            {
                await _viewModel.LoadDataAsync();
                if (PageTitleTextBlock.Text == "Расписание экзаменов" || PageTitleTextBlock.Text == "Мое расписание экзаменов")
                {
                    var newContent = CreateScheduleContent();
                    MainContentPresenter.Content = newContent;
                }
            }
        }

        private async Task<FrameworkElement> CreateUniversitiesContentAsync()
        {
            if (_viewModel is null) return new TextBlock { Text = "Ошибка загрузки данных" };

            await _viewModel.LoadDataAsync();

            var scrollViewer = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            var stackPanel = new StackPanel { Margin = new Thickness(30) };

            var titleText = new TextBlock
            {
                Text = _viewModel.IsAdmin ? "Управление университетами" : "Университеты",
                FontSize = 24,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 20)
            };
            stackPanel.Children.Add(titleText);

            var filterCard = new Border
            {
                Background = Brushes.White,
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(25),
                Margin = new Thickness(0, 0, 0, 25),
                BorderBrush = new SolidColorBrush(Color.FromRgb(226, 232, 240)),
                BorderThickness = new Thickness(1),
                Effect = new System.Windows.Media.Effects.DropShadowEffect
                {
                    Color = Colors.Gray,
                    Direction = 270,
                    ShadowDepth = 2,
                    Opacity = 0.15,
                    BlurRadius = 8
                }
            };

            var filterCardContent = new StackPanel();

            var filterHeader = new StackPanel 
            { 
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 0, 0, 20)
            };
            
            var filterIcon = new MaterialDesignThemes.Wpf.PackIcon
            {
                Kind = MaterialDesignThemes.Wpf.PackIconKind.Filter,
                Width = 24,
                Height = 24,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 10, 0),
                Foreground = GetAccentBrush(_viewModel.IsAdmin)
            };
            filterHeader.Children.Add(filterIcon);

            var filterTitle = new TextBlock
            {
                Text = "Фильтры поиска",
                FontSize = 20,
                FontWeight = FontWeights.Bold,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = new SolidColorBrush(Color.FromRgb(51, 51, 51))
            };
            filterHeader.Children.Add(filterTitle);
            filterCardContent.Children.Add(filterHeader);

            var searchCard = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(248, 250, 252)),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(15),
                Margin = new Thickness(0, 0, 0, 20)
            };
            
            var searchGrid = new Grid();
            searchGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            searchGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            searchGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            
            var searchIcon = new MaterialDesignThemes.Wpf.PackIcon
            {
                Kind = MaterialDesignThemes.Wpf.PackIconKind.Magnify,
                Width = 20,
                Height = 20,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Left,
                Margin = new Thickness(0, 0, 12, 0),
                Foreground = new SolidColorBrush(Color.FromRgb(100, 116, 139))
            };
            Grid.SetColumn(searchIcon, 0);
            searchGrid.Children.Add(searchIcon);
            
            var searchTextBox = new TextBox
            {
                Height = 55,
                FontSize = 14,
                VerticalAlignment = VerticalAlignment.Center,
                Style = (Style)FindResource("MaterialDesignOutlinedTextBox")
            };
            MaterialDesignThemes.Wpf.HintAssist.SetHint(searchTextBox, "Поиск по названию, адресу, описанию...");
            MaterialDesignThemes.Wpf.HintAssist.SetHelperText(searchTextBox, "Введите текст для поиска");
            Grid.SetColumn(searchTextBox, 1);
            searchGrid.Children.Add(searchTextBox);

            var clearFiltersButton = new Button
            {
                Content = "Очистить",
                Width = 120,
                Height = 55,
                Margin = new Thickness(12, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center,
                Style = (Style)FindResource("MaterialDesignOutlinedButton"),
                FontSize = 14
            };
            Grid.SetColumn(clearFiltersButton, 2);
            searchGrid.Children.Add(clearFiltersButton);
            
            searchCard.Child = searchGrid;
            filterCardContent.Children.Add(searchCard);

            var cityCard = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(248, 250, 252)),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(15),
                Margin = new Thickness(0, 0, 0, 15)
            };
            
            var cityCardContent = new StackPanel();
            
            var cityHeader = new StackPanel 
            { 
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 0, 0, 12)
            };
            
            var cityIcon = new MaterialDesignThemes.Wpf.PackIcon
            {
                Kind = MaterialDesignThemes.Wpf.PackIconKind.City,
                Width = 20,
                Height = 20,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 8, 0),
                Foreground = new SolidColorBrush(Color.FromRgb(59, 130, 246))
            };
            cityHeader.Children.Add(cityIcon);
            
            var cityLabel = new TextBlock 
            { 
                Text = "Город", 
                FontSize = 16,
                FontWeight = FontWeights.SemiBold,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = new SolidColorBrush(Color.FromRgb(51, 51, 51))
            };
            cityHeader.Children.Add(cityLabel);
            cityCardContent.Children.Add(cityHeader);
            
            var cityCheckBoxesPanel = new WrapPanel 
            { 
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 5, 0, 0)
            };
            var cities = new[] { "Минск", "Брест", "Могилев", "Витебск", "Гродно", "Гомель" };
            var cityCheckBoxes = new Dictionary<string, CheckBox>();
            
            foreach (var city in cities)
            {
                var cityCheckBox = new CheckBox
                {
                    Content = city,
                    Margin = new Thickness(0, 0, 20, 10),
                    FontSize = 14,
                    Style = (Style)FindResource("MaterialDesignCheckBox")
                };
                cityCheckBoxes[city] = cityCheckBox;
                cityCheckBoxesPanel.Children.Add(cityCheckBox);
            }
            cityCardContent.Children.Add(cityCheckBoxesPanel);
            cityCard.Child = cityCardContent;
            filterCardContent.Children.Add(cityCard);

            var subjectCard = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(248, 250, 252)),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(15),
                Margin = new Thickness(0, 0, 0, 0)
            };
            
            var subjectCardContent = new StackPanel();
            
            var subjectHeader = new StackPanel 
            { 
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 0, 0, 12)
            };
            
            var subjectIcon = new MaterialDesignThemes.Wpf.PackIcon
            {
                Kind = MaterialDesignThemes.Wpf.PackIconKind.BookOpen,
                Width = 20,
                Height = 20,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 8, 0),
                Foreground = new SolidColorBrush(Color.FromRgb(16, 185, 129))
            };
            subjectHeader.Children.Add(subjectIcon);
            
            var subjectLabel = new TextBlock 
            { 
                Text = "Предметы для сдачи", 
                FontSize = 16,
                FontWeight = FontWeights.SemiBold,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = new SolidColorBrush(Color.FromRgb(51, 51, 51))
            };
            subjectHeader.Children.Add(subjectLabel);
            subjectCardContent.Children.Add(subjectHeader);
            
            var subjectCheckBoxesPanel = new WrapPanel 
            { 
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 5, 0, 0)
            };
            var subjectCheckBoxes = new Dictionary<string, CheckBox>();
            
            List<string> allSubjects = new List<string>();
            using (var context = new Data.ApplicationDbContext())
            {
                allSubjects = await context.SpecialitySubjects
                    .Select(s => s.SubjectName)
                    .Distinct()
                    .OrderBy(s => s)
                    .ToListAsync();
            }
            
            foreach (var subject in allSubjects)
            {
                var subjectCheckBox = new CheckBox
                {
                    Content = subject,
                    Margin = new Thickness(0, 0, 20, 10),
                    FontSize = 14,
                    Style = (Style)FindResource("MaterialDesignCheckBox")
                };
                subjectCheckBoxes[subject] = subjectCheckBox;
                subjectCheckBoxesPanel.Children.Add(subjectCheckBox);
            }
            subjectCardContent.Children.Add(subjectCheckBoxesPanel);
            subjectCard.Child = subjectCardContent;
            filterCardContent.Children.Add(subjectCard);

            filterCard.Child = filterCardContent;
            stackPanel.Children.Add(filterCard);

            if (!_viewModel.IsAdmin)
            {
                var applicantSection = new StackPanel { Margin = new Thickness(0, 0, 0, 30) };
                
                var applicantTitle = new TextBlock
                {
                    Text = "Подача заявки в университет",
                    FontSize = 20,
                    FontWeight = FontWeights.Bold,
                    Margin = new Thickness(0, 0, 0, 15)
                };
                applicantSection.Children.Add(applicantTitle);

                var findButton = new Button
                {
                    Content = "Найти подходящие специальности",
                    Style = (Style)FindResource("MaterialDesignRaisedButton"),
                    Background = GetAccentBrush(_viewModel.IsAdmin),
                    Height = 50,
                    FontSize = 16,
                    FontWeight = FontWeights.SemiBold,
                    Margin = new Thickness(0, 0, 0, 20),
                    Padding = new Thickness(20, 0, 20, 0)
                };
                
                var matchedSpecialitiesPanel = new StackPanel { Margin = new Thickness(0, 0, 0, 30) };
                
                findButton.Click += async (s, e) =>
                {
                    try
                    {
                        await _viewModel.FindMatchingSpecialitiesForApplicantAsync();
                        
                        matchedSpecialitiesPanel.Children.Clear();
                        
                        if (_viewModel.MatchedSpecialities.Any())
                        {
                            var resultsTitle = new TextBlock
                            {
                                Text = "Найденные подходящие специальности:",
                                FontSize = 16,
                                FontWeight = FontWeights.SemiBold,
                                Margin = new Thickness(0, 0, 0, 15)
                            };
                            matchedSpecialitiesPanel.Children.Add(resultsTitle);

                            foreach (var matched in _viewModel.MatchedSpecialities)
                            {
                                var specialityCard = await CreateMatchedSpecialityCardAsync(matched);
                                matchedSpecialitiesPanel.Children.Add(specialityCard);
                            }
                        }
                        else
                        {
                            var noResultsText = new TextBlock
                            {
                                Text = "Не найдено подходящих специальностей. Убедитесь, что у вас есть одобренные сертификаты.",
                                Foreground = Brushes.Gray,
                                Margin = new Thickness(0, 10, 0, 0),
                                TextWrapping = TextWrapping.Wrap
                            };
                            matchedSpecialitiesPanel.Children.Add(noResultsText);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка при поиске специальностей: {ex.Message}", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                };
                
                applicantSection.Children.Add(findButton);
                applicantSection.Children.Add(matchedSpecialitiesPanel);

                var myApplicationsTitle = new TextBlock
                {
                    Text = "Мои заявки в университеты",
                    FontSize = 20,
                    FontWeight = FontWeights.Bold,
                    Margin = new Thickness(0, 20, 0, 15)
                };
                applicantSection.Children.Add(myApplicationsTitle);

                var myApplicationsDataGrid = new System.Windows.Controls.DataGrid
                {
                    AutoGenerateColumns = false,
                    CanUserAddRows = false,
                    CanUserDeleteRows = false,
                    IsReadOnly = true,
                    HeadersVisibility = DataGridHeadersVisibility.All,
                    GridLinesVisibility = DataGridGridLinesVisibility.All,
                    AlternatingRowBackground = new SolidColorBrush(Color.FromRgb(249, 250, 251)),
                    RowBackground = Brushes.White,
                    Background = Brushes.White,
                    BorderBrush = new SolidColorBrush(Color.FromRgb(229, 231, 235)),
                    BorderThickness = new Thickness(1),
                    Margin = new Thickness(0, 0, 0, 20),
                    FontSize = 14,
                    Height = 300,
                    VerticalContentAlignment = VerticalAlignment.Center,
                    RowHeight = 50
                };

                myApplicationsDataGrid.Columns.Add(new System.Windows.Controls.DataGridTextColumn
                {
                    Header = "Университет",
                    Binding = new Binding("University.Name"),
                    Width = new DataGridLength(2, DataGridLengthUnitType.Star)
                });

                myApplicationsDataGrid.Columns.Add(new System.Windows.Controls.DataGridTextColumn
                {
                    Header = "Специальность",
                    Binding = new Binding("Speciality.Name"),
                    Width = new DataGridLength(2, DataGridLengthUnitType.Star)
                });

                myApplicationsDataGrid.Columns.Add(new System.Windows.Controls.DataGridTextColumn
                {
                    Header = "Тип",
                    Binding = new Binding("Type")
                    {
                        Converter = new Converters.ApplicationTypeConverter()
                    },
                    Width = new DataGridLength(100)
                });

                myApplicationsDataGrid.Columns.Add(new System.Windows.Controls.DataGridTextColumn
                {
                    Header = "Статус",
                    Binding = new Binding("Status")
                    {
                        Converter = new Converters.UniversityApplicationStatusConverter()
                    },
                    Width = new DataGridLength(150)
                });

                myApplicationsDataGrid.Columns.Add(new System.Windows.Controls.DataGridTextColumn
                {
                    Header = "Дата подачи",
                    Binding = new Binding("AppliedAt") { StringFormat = "dd.MM.yyyy HH:mm" },
                    Width = new DataGridLength(150)
                });

                myApplicationsDataGrid.ItemsSource = _viewModel.UniversityApplications;
                applicantSection.Children.Add(myApplicationsDataGrid);

                stackPanel.Children.Add(applicantSection);
            }

            if (_viewModel.IsAdmin)
            {
                var manageButton = new Button
                {
                    Content = "Управление университетами",
                    Style = (Style)FindResource("MaterialDesignRaisedButton"),
                    Background = GetAccentBrush(_viewModel.IsAdmin),
                    Height = 50,
                    FontSize = 16,
                    FontWeight = FontWeights.SemiBold,
                    Margin = new Thickness(0, 0, 0, 20),
                    Padding = new Thickness(20, 0, 20, 0)
                };
                manageButton.Click += (s, e) => OpenUniversityManagement();
                stackPanel.Children.Add(manageButton);
            }

            var universitiesPanel = new StackPanel();

            Func<List<University>, List<University>> filterUniversities = (universities) =>
            {
                var filtered = universities;
                
                if (!string.IsNullOrWhiteSpace(searchTextBox.Text))
                {
                    var searchLower = searchTextBox.Text.ToLower();
                    filtered = filtered.Where(u => 
                        (u.Name?.ToLower().Contains(searchLower) ?? false) ||
                        (u.Address?.ToLower().Contains(searchLower) ?? false) ||
                        (u.Description?.ToLower().Contains(searchLower) ?? false)
                    ).ToList();
                }
                
                var selectedCities = cityCheckBoxes
                    .Where(kvp => kvp.Value.IsChecked == true)
                    .Select(kvp => kvp.Key)
                    .ToList();
                
                if (selectedCities.Any())
                {
                    filtered = filtered.Where(u =>
                    {
                        if (string.IsNullOrWhiteSpace(u.Address)) return false;
                        var addressLower = u.Address.ToLower();
                        return selectedCities.Any(city => addressLower.Contains(city.ToLower()));
                    }).ToList();
                }
                
                var selectedSubjects = subjectCheckBoxes
                    .Where(kvp => kvp.Value.IsChecked == true)
                    .Select(kvp => kvp.Key)
                    .ToList();
                
                if (selectedSubjects.Any())
                {
                    filtered = filtered.Where(u =>
                    {
                        if (u.Specialities == null || !u.Specialities.Any()) return false;
                        
                        return u.Specialities.Any(spec =>
                        {
                            if (spec.Subjects == null || !spec.Subjects.Any()) return false;
                            var specSubjects = spec.Subjects.Select(s => s.SubjectName).ToList();
                            return selectedSubjects.All(subj => specSubjects.Contains(subj));
                        });
                    }).ToList();
                }
                
                return filtered.OrderBy(u => u.Name).ToList();
            };

            Func<Task> updateUniversitiesList = async () =>
            {
                try
                {
                    List<University> allUniversities;
                    using (var context = new Data.ApplicationDbContext())
                    {
                        allUniversities = await context.Universities
                            .Include(u => u.Specialities)
                                .ThenInclude(s => s.Subjects)
                            .ToListAsync();
                    }
                    
                    var filtered = filterUniversities(allUniversities);
                    universitiesPanel.Children.Clear();
                    foreach (var university in filtered)
                    {
                        var universityCard = await CreateUniversityCardWithDetailsAsync(university);
                        universitiesPanel.Children.Add(universityCard);
                    }
                }
                catch (Exception ex)
                {
                    universitiesPanel.Children.Clear();
                    universitiesPanel.Children.Add(new TextBlock 
                    { 
                        Text = $"Ошибка загрузки университетов: {ex.Message}",
                        Foreground = Brushes.Red,
                        Margin = new Thickness(10)
                    });
                }
            };

            searchTextBox.TextChanged += async (s, e) =>
            {
                await updateUniversitiesList();
            };

            foreach (var cityCheckBox in cityCheckBoxes.Values)
            {
                cityCheckBox.Checked += async (s, e) => await updateUniversitiesList();
                cityCheckBox.Unchecked += async (s, e) => await updateUniversitiesList();
            }

            foreach (var subjectCheckBox in subjectCheckBoxes.Values)
            {
                subjectCheckBox.Checked += async (s, e) => await updateUniversitiesList();
                subjectCheckBox.Unchecked += async (s, e) => await updateUniversitiesList();
            }

            clearFiltersButton.Click += async (s, e) =>
            {
                searchTextBox.Text = "";
                foreach (var cityCheckBox in cityCheckBoxes.Values)
                {
                    cityCheckBox.IsChecked = false;
                }
                foreach (var subjectCheckBox in subjectCheckBoxes.Values)
                {
                    subjectCheckBox.IsChecked = false;
                }
                await updateUniversitiesList();
            };

            stackPanel.Children.Add(universitiesPanel);

            await updateUniversitiesList();

            scrollViewer.Content = stackPanel;
            return scrollViewer;
        }

        private async Task<FrameworkElement> CreateUniversityCardWithDetailsAsync(University university)
        {
            var border = new Border
            {
                Background = Brushes.White,
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(20),
                Margin = new Thickness(0, 0, 0, 15),
                BorderBrush = new SolidColorBrush(Color.FromRgb(226, 232, 240)),
                BorderThickness = new Thickness(1)
            };

            var stackPanel = new StackPanel();

            var nameText = new TextBlock
            {
                Text = university.Name,
                FontSize = 20,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 10)
            };
            stackPanel.Children.Add(nameText);

            if (!string.IsNullOrEmpty(university.Address))
            {
                var addressText = new TextBlock
                {
                    Text = $"📍 {university.Address}",
                    FontSize = 14,
                    Foreground = Brushes.Gray,
                    Margin = new Thickness(0, 0, 0, 5)
                };
                stackPanel.Children.Add(addressText);
            }

            var contactsStack = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 15) };
            if (!string.IsNullOrEmpty(university.Phone))
            {
                var phoneText = new TextBlock
                {
                    Text = $"📞 {university.Phone}",
                    FontSize = 14,
                    Foreground = Brushes.Gray,
                    Margin = new Thickness(0, 0, 15, 0)
                };
                contactsStack.Children.Add(phoneText);
            }
            if (!string.IsNullOrEmpty(university.Email))
            {
                var emailText = new TextBlock
                {
                    Text = $"✉ {university.Email}",
                    FontSize = 14,
                    Foreground = Brushes.Gray
                };
                contactsStack.Children.Add(emailText);
            }
            if (contactsStack.Children.Count > 0)
            {
                stackPanel.Children.Add(contactsStack);
            }

            List<Models.Speciality> specialities = null;
            try
            {
                using (var context = new Data.ApplicationDbContext())
                {
                    specialities = await context.Specialities
                        .Include(s => s.Subjects)
                        .Where(s => s.UniversityId == university.Id && s.IsActive)
                        .ToListAsync();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка загрузки специальностей: {ex.Message}");
            }

            if (specialities != null && specialities.Any())
            {
                var specialitiesTitle = new TextBlock
                {
                    Text = "Специальности:",
                    FontSize = 16,
                    FontWeight = FontWeights.SemiBold,
                    Margin = new Thickness(0, 10, 0, 10)
                };
                stackPanel.Children.Add(specialitiesTitle);

                foreach (var speciality in specialities)
                {
                    var specialityBorder = new Border
                    {
                        Background = new SolidColorBrush(Color.FromRgb(245, 247, 250)),
                        CornerRadius = new CornerRadius(5),
                        Padding = new Thickness(15),
                        Margin = new Thickness(0, 0, 0, 10)
                    };

                    var specialityStack = new StackPanel();

                    var specialityNameText = new TextBlock
                    {
                        Text = $"• {speciality.Name}" + (string.IsNullOrEmpty(speciality.Code) ? "" : $" ({speciality.Code})"),
                        FontSize = 15,
                        FontWeight = FontWeights.SemiBold,
                        Margin = new Thickness(0, 0, 0, 8)
                    };
                    specialityStack.Children.Add(specialityNameText);

                    var placesText = new TextBlock
                    {
                        Text = $"Места: Бюджет - {speciality.BudgetPlaces ?? 0}, Платное - {speciality.PaidPlaces ?? 0}",
                        FontSize = 13,
                        Foreground = new SolidColorBrush(Color.FromRgb(102, 126, 234)),
                        Margin = new Thickness(0, 0, 0, 5)
                    };
                    specialityStack.Children.Add(placesText);

                    var passingScoresText = new TextBlock
                    {
                        FontSize = 13,
                        Foreground = new SolidColorBrush(Color.FromRgb(34, 197, 94)),
                        Margin = new Thickness(0, 0, 0, 8)
                    };
                    
                    var passingScoresParts = new List<string>();
                    if (speciality.PassingScoreBudget.HasValue)
                    {
                        passingScoresParts.Add($"Бюджет: {speciality.PassingScoreBudget.Value}");
                    }
                    if (speciality.PassingScorePaid.HasValue)
                    {
                        passingScoresParts.Add($"Платное: {speciality.PassingScorePaid.Value}");
                    }
                    
                    if (passingScoresParts.Any())
                    {
                        passingScoresText.Text = $"Проходные баллы: {string.Join(", ", passingScoresParts)}";
                        specialityStack.Children.Add(passingScoresText);
                    }

                    if (speciality.Subjects != null && speciality.Subjects.Any())
                    {
                        var subjectsTitle = new TextBlock
                        {
                            Text = "Предметы для сдачи:",
                            FontSize = 13,
                            FontWeight = FontWeights.Medium,
                            Margin = new Thickness(0, 0, 0, 5)
                        };
                        specialityStack.Children.Add(subjectsTitle);

                        var subjectsList = new StackPanel { Margin = new Thickness(15, 0, 0, 0) };
                        foreach (var subject in speciality.Subjects)
                        {
                            var subjectText = new TextBlock
                            {
                                Text = $"  - {subject.SubjectName}" + 
                                       (subject.MinScore.HasValue ? $" (мин. балл: {subject.MinScore})" : "") +
                                       (subject.IsRequired ? " *" : ""),
                                FontSize = 12,
                                Foreground = Brushes.Gray,
                                Margin = new Thickness(0, 0, 0, 3)
                            };
                            subjectsList.Children.Add(subjectText);
                        }
                        specialityStack.Children.Add(subjectsList);
                    }

                    specialityBorder.Child = specialityStack;
                    stackPanel.Children.Add(specialityBorder);
                }
            }
            else
            {
                var noSpecialitiesText = new TextBlock
                {
                    Text = "Специальности не указаны",
                    FontSize = 14,
                    Foreground = Brushes.Gray,
                    FontStyle = FontStyles.Italic,
                    Margin = new Thickness(0, 10, 0, 0)
                };
                stackPanel.Children.Add(noSpecialitiesText);
            }

            border.Child = stackPanel;
            return border;
        }

        private async Task<FrameworkElement> CreateMatchedSpecialityCardAsync(Services.MatchedSpeciality matched)
        {
            var border = new Border
            {
                Background = Brushes.White,
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(20),
                Margin = new Thickness(0, 0, 0, 15),
                BorderBrush = new SolidColorBrush(Color.FromRgb(226, 232, 240)),
                BorderThickness = new Thickness(1)
            };

            var stackPanel = new StackPanel();

            var headerText = new TextBlock
            {
                Text = $"{matched.University.Name} - {matched.Speciality.Name}",
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 10)
            };
            stackPanel.Children.Add(headerText);

            var matchInfo = new TextBlock
            {
                Text = $"Совпало предметов: {matched.MatchedSubjectsCount} из {matched.RequiredSubjectsCount}",
                FontSize = 14,
                Foreground = matched.AllRequirementsMet ? new SolidColorBrush(Colors.Green) : new SolidColorBrush(Colors.Orange),
                Margin = new Thickness(0, 0, 0, 10)
            };
            stackPanel.Children.Add(matchInfo);

            using (var context = new Data.ApplicationDbContext())
            {
                var budgetApplications = await context.UniversityApplications
                    .Where(a => a.SpecialityId == matched.Speciality.Id && 
                               a.Type == ApplicationType.Budget &&
                               (a.Status == UniversityApplicationStatus.Pending || 
                                a.Status == UniversityApplicationStatus.Accepted))
                    .CountAsync();

                var paidApplications = await context.UniversityApplications
                    .Where(a => a.SpecialityId == matched.Speciality.Id && 
                               a.Type == ApplicationType.Paid &&
                               (a.Status == UniversityApplicationStatus.Pending || 
                                a.Status == UniversityApplicationStatus.Accepted))
                    .CountAsync();

                var availableBudgetPlaces = (matched.Speciality.BudgetPlaces ?? 0) - budgetApplications;
                var availablePaidPlaces = (matched.Speciality.PaidPlaces ?? 0) - paidApplications;

                var placesText = new TextBlock
                {
                    Text = $"Доступные места: Бюджет - {availableBudgetPlaces}, Платное - {availablePaidPlaces}",
                    FontSize = 14,
                    Foreground = Brushes.Gray,
                    Margin = new Thickness(0, 0, 0, 5)
                };
                stackPanel.Children.Add(placesText);

                var passingScoresParts = new List<string>();
                if (matched.Speciality.PassingScoreBudget.HasValue)
                {
                    passingScoresParts.Add($"Бюджет: {matched.Speciality.PassingScoreBudget.Value}");
                }
                if (matched.Speciality.PassingScorePaid.HasValue)
                {
                    passingScoresParts.Add($"Платное: {matched.Speciality.PassingScorePaid.Value}");
                }
                
                if (passingScoresParts.Any())
                {
                    var passingScoresText = new TextBlock
                    {
                        Text = $"Проходные баллы: {string.Join(", ", passingScoresParts)}",
                        FontSize = 14,
                        Foreground = new SolidColorBrush(Color.FromRgb(34, 197, 94)),
                        Margin = new Thickness(0, 0, 0, 15)
                    };
                    stackPanel.Children.Add(passingScoresText);
                }

                if (matched.AllRequirementsMet && (availableBudgetPlaces > 0 || availablePaidPlaces > 0))
                {
                    var applyButton = new Button
                    {
                        Content = availableBudgetPlaces > 0 && availablePaidPlaces > 0 
                            ? "Подать заявку (выбрать тип)" 
                            : availableBudgetPlaces > 0 
                                ? "Подать заявку (бюджет)" 
                                : "Подать заявку (платное)",
                        Style = (Style)FindResource("MaterialDesignRaisedButton"),
                        Background = GetAccentBrush(false),
                        Height = 40,
                        FontSize = 14,
                        FontWeight = FontWeights.SemiBold,
                        Margin = new Thickness(0, 0, 0, 10)
                    };

                    applyButton.Click += async (s, e) =>
                    {
                        ApplicationType applicationType = ApplicationType.Budget;
                        
                        if (availableBudgetPlaces > 0 && availablePaidPlaces > 0)
                        {
                            var choiceDialog = new Views.InputDialog(
                                "Выберите тип заявки:\n1 - Бюджет\n2 - Платное", 
                                "Выбор типа заявки", 
                                "1");
                            
                            if (choiceDialog.ShowDialog() != true) return;

                            if (choiceDialog.ResponseText == "2")
                            {
                                applicationType = ApplicationType.Paid;
                            }
                        }
                        else if (availablePaidPlaces > 0)
                        {
                            applicationType = ApplicationType.Paid;
                        }

                        await _viewModel.SubmitUniversityApplicationAsync(matched, applicationType);
                        await _viewModel.LoadDataAsync();
                        
                        var newContent = await CreateUniversitiesContentAsync();
                        MainContentPresenter.Content = newContent;
                    };

                    stackPanel.Children.Add(applyButton);
                }
                else
                {
                    var noPlacesText = new TextBlock
                    {
                        Text = "Нет доступных мест или не выполнены все требования",
                        FontSize = 14,
                        Foreground = Brushes.Red,
                        FontStyle = FontStyles.Italic
                    };
                    stackPanel.Children.Add(noPlacesText);
                }
            }

            border.Child = stackPanel;
            return border;
        }

        private FrameworkElement CreateUniversityItem(University university)
        {
            var border = new Border
            {
                Background = Brushes.White,
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(15),
                Margin = new Thickness(0, 0, 0, 10),
                BorderBrush = new SolidColorBrush(Color.FromRgb(226, 232, 240)),
                BorderThickness = new Thickness(1)
            };

            var stackPanel = new StackPanel();

            var nameText = new TextBlock
            {
                Text = university.Name,
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 10)
            };
            stackPanel.Children.Add(nameText);

            if (!string.IsNullOrEmpty(university.Address))
            {
                var addressText = new TextBlock
                {
                    Text = $"📍 {university.Address}",
                    FontSize = 14,
                    Foreground = Brushes.Gray,
                    Margin = new Thickness(0, 0, 0, 5)
                };
                stackPanel.Children.Add(addressText);
            }

            if (!string.IsNullOrEmpty(university.Phone))
            {
                var phoneText = new TextBlock
                {
                    Text = $"📞 {university.Phone}",
                    FontSize = 14,
                    Foreground = Brushes.Gray,
                    Margin = new Thickness(0, 0, 0, 5)
                };
                stackPanel.Children.Add(phoneText);
            }

            border.Child = stackPanel;
            return border;
        }

        private void OpenUniversityManagement()
        {
            var window = new Views.UniversityManagementWindow();
            window.ShowDialog();
            if (_viewModel is not null)
            {
                _viewModel.LoadDataAsync();
            }
        }

        private FrameworkElement CreateSettingsContent()
        {
            if (_viewModel?.CurrentUser == null)
            {
                try
                {
                    var context = new Data.ApplicationDbContext();
                    var authService = new Services.AuthenticationService(context);
                    _viewModel = new ViewModels.DashboardViewModel(authService);
                    if (_viewModel?.CurrentUser == null)
                        return new TextBlock { Text = "Ошибка загрузки данных" };
                }
                catch
                {
                    return new TextBlock { Text = "Ошибка загрузки данных" };
                }
            }

            var scrollViewer = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            var stackPanel = new StackPanel { Margin = new Thickness(30) };

            var titleText = new TextBlock
            {
                Text = "Настройки профиля",
                FontSize = 24,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 30)
            };
            stackPanel.Children.Add(titleText);

            var passwordCard = new Border
            {
                Background = Brushes.White,
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(20),
                Margin = new Thickness(0, 0, 0, 20)
            };

            var passwordStack = new StackPanel();
            var passwordTitle = new TextBlock
            {
                Text = "Изменение пароля",
                FontSize = 18,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 0, 15)
            };
            passwordStack.Children.Add(passwordTitle);

            var currentPasswordBox = new PasswordBox
            {
                Margin = new Thickness(0, 0, 0, 10),
                Height = 40
            };
            HintAssist.SetHint(currentPasswordBox, "Текущий пароль");
            passwordStack.Children.Add(currentPasswordBox);

            var newPasswordBox = new PasswordBox
            {
                Margin = new Thickness(0, 0, 0, 10),
                Height = 40
            };
            HintAssist.SetHint(newPasswordBox, "Новый пароль");
            passwordStack.Children.Add(newPasswordBox);

            var confirmPasswordBox = new PasswordBox
            {
                Margin = new Thickness(0, 0, 0, 15),
                Height = 40
            };
            HintAssist.SetHint(confirmPasswordBox, "Подтвердите новый пароль");
            passwordStack.Children.Add(confirmPasswordBox);

            var updatePasswordButton = new Button
            {
                Content = "Изменить пароль",
                Style = (Style)FindResource("MaterialDesignRaisedButton"),
                Background = GetAccentBrush(_viewModel?.IsAdmin ?? false),
                Height = 40
            };
            updatePasswordButton.Click += async (s, e) =>
            {
                try
                {
                    var context = new Data.ApplicationDbContext();
                    
                    var authService = _authService;
                    if (authService == null)
                    {
                        authService = new Services.AuthenticationService(context);
                        if (_viewModel?.CurrentUser != null)
                        {
                            var user = await context.Users.FindAsync(_viewModel.CurrentUser.Id);
                            if (user != null)
                            {
                                authService.SetCurrentUser(user);
                            }
                        }
                    }
                    
                    var settingsVm = new ViewModels.SettingsViewModel(context, authService);
                    settingsVm.CurrentPassword = currentPasswordBox.Password;
                    settingsVm.NewPassword = newPasswordBox.Password;
                    settingsVm.ConfirmPassword = confirmPasswordBox.Password;
                    await settingsVm.UpdatePasswordCommand.ExecuteAsync(null);
                    
                    if (settingsVm.CurrentUser != null)
                    {
                        currentPasswordBox.Password = string.Empty;
                        newPasswordBox.Password = string.Empty;
                        confirmPasswordBox.Password = string.Empty;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            };
            passwordStack.Children.Add(updatePasswordButton);

            passwordCard.Child = passwordStack;
            stackPanel.Children.Add(passwordCard);

            var emailCard = new Border
            {
                Background = Brushes.White,
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(20),
                Margin = new Thickness(0, 0, 0, 20)
            };

            var emailStack = new StackPanel();
            var emailTitle = new TextBlock
            {
                Text = "Изменение email (для получения результатов)",
                FontSize = 18,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 0, 15)
            };
            emailStack.Children.Add(emailTitle);

            var emailTextBox = new TextBox
            {
                Text = string.Empty,
                Margin = new Thickness(0, 0, 0, 15),
                Height = 40
            };
            HintAssist.SetHint(emailTextBox, "Email адрес");
            emailTextBox.LostFocus += InputValidationHelper.Email_LostFocus;
            emailStack.Children.Add(emailTextBox);

            var updateEmailButton = new Button
            {
                Content = "Изменить email",
                Style = (Style)FindResource("MaterialDesignRaisedButton"),
                Background = GetAccentBrush(_viewModel?.IsAdmin ?? false),
                Height = 40
            };
            updateEmailButton.Click += async (s, e) =>
            {
                try
                {
                    var context = new Data.ApplicationDbContext();
                    
                    var authService = _authService;
                    if (authService == null)
                    {
                        authService = new Services.AuthenticationService(context);
                        if (_viewModel?.CurrentUser != null)
                        {
                            var user = await context.Users.FindAsync(_viewModel.CurrentUser.Id);
                            if (user != null)
                            {
                                authService.SetCurrentUser(user);
                            }
                        }
                    }
                    
                    var settingsVm = new ViewModels.SettingsViewModel(context, authService);
                    settingsVm.Email = emailTextBox.Text;
                    await settingsVm.UpdateEmailCommand.ExecuteAsync(null);
                    
                    if (settingsVm.CurrentUser != null && _viewModel != null)
                    {
                        _viewModel.CurrentUser = settingsVm.CurrentUser;
                        emailTextBox.Text = settingsVm.CurrentUser.Email;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            };
            emailStack.Children.Add(updateEmailButton);

            emailCard.Child = emailStack;
            stackPanel.Children.Add(emailCard);

            scrollViewer.Content = stackPanel;
            return scrollViewer;
        }

        public class FreePlacesConverter : IValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
            {
                if (value is Models.ExamSchedule schedule)
                {
                    return $"{schedule.MaxParticipants - schedule.CurrentParticipants} / {schedule.MaxParticipants}";
                }
                return string.Empty;
            }

            public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
            {
                throw new NotImplementedException();
            }
        }
    }
}