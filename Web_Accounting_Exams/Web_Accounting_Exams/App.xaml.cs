using System.Configuration;
using System.Data;
using System.Windows;
using Web_Accounting_Exams.Data;
using Web_Accounting_Exams.Services;

namespace Web_Accounting_Exams
{
    public partial class App : Application
    {
        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            try
            {
                var context = new ApplicationDbContext();
                var dbService = new DatabaseService(context);
                var success = await dbService.EnsureDatabaseCreatedAsync();
                
                if (success)
                {
                    System.Diagnostics.Debug.WriteLine("База данных успешно инициализирована.");
                    System.Diagnostics.Debug.WriteLine("Тестовые аккаунты:");
                    System.Diagnostics.Debug.WriteLine("  Администратор: admin@test.com / admin");
                    System.Diagnostics.Debug.WriteLine("  Абитуриент: student@test.com / student123");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка инициализации базы данных: {ex.Message}\n\nУбедитесь, что SQL Server установлен и запущен.\n\nДетали: {ex.InnerException?.Message ?? ex.Message}",
                    "Ошибка базы данных", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
