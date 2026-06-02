using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using System;
using MailKit;

namespace Web_Accounting_Exams.Services
{
    public class EmailService
    {
        private readonly string _smtpServer = "smtp.gmail.com";
        private readonly int _smtpPort = 587;
        private readonly string _smtpUsername = "";
        private readonly string _smtpPassword = "";

        public string LastErrorMessage { get; private set; } = string.Empty;

        public async Task<bool> SendEmailAsync(string toEmail, string subject, string body)
        {
            LastErrorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(_smtpUsername) || 
                string.IsNullOrWhiteSpace(_smtpPassword))
            {
                LastErrorMessage = "SMTP учетные данные не настроены. Пожалуйста, настройте _smtpUsername и _smtpPassword в EmailService.";
                System.Diagnostics.Debug.WriteLine($"Ошибка: {LastErrorMessage}");
                return false;
            }

            if (string.IsNullOrWhiteSpace(toEmail) || !toEmail.Contains("@"))
            {
                LastErrorMessage = $"Некорректный email адрес получателя: {toEmail}";
                System.Diagnostics.Debug.WriteLine($"Ошибка: {LastErrorMessage}");
                return false;
            }

            try
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress("Система учета экзаменов", _smtpUsername));
                message.To.Add(new MailboxAddress("", toEmail));
                message.Subject = subject;
                message.Body = new TextPart("html")
                {
                    Text = body
                };

                using (var client = new SmtpClient())
                {
                    System.Diagnostics.Debug.WriteLine($"Подключение к SMTP серверу {_smtpServer}:{_smtpPort}...");
                    await client.ConnectAsync(_smtpServer, _smtpPort, SecureSocketOptions.StartTls);
                    
                    System.Diagnostics.Debug.WriteLine("Аутентификация...");
                    await client.AuthenticateAsync(_smtpUsername, _smtpPassword);
                    
                    System.Diagnostics.Debug.WriteLine($"Отправка email на {toEmail}...");
                    await client.SendAsync(message);
                    
                    System.Diagnostics.Debug.WriteLine("Email успешно отправлен!");
                    await client.DisconnectAsync(true);
                }

                return true;
            }
            catch (AuthenticationException ex)
            {
                LastErrorMessage = $"Ошибка аутентификации SMTP: {ex.Message}. Проверьте правильность email и пароля приложения.";
                System.Diagnostics.Debug.WriteLine($"Ошибка аутентификации: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Полная ошибка: {ex}");
                return false;
            }
            catch (SmtpCommandException ex)
            {
                LastErrorMessage = $"Ошибка SMTP команды: {ex.Message}. Код ошибки: {ex.StatusCode}";
                System.Diagnostics.Debug.WriteLine($"Ошибка SMTP команды: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Полная ошибка: {ex}");
                return false;
            }
            catch (SmtpProtocolException ex)
            {
                LastErrorMessage = $"Ошибка SMTP протокола: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"Ошибка SMTP протокола: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Полная ошибка: {ex}");
                return false;
            }
            catch (Exception ex)
            {
                LastErrorMessage = $"Ошибка отправки email: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"Ошибка отправки email: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Тип ошибки: {ex.GetType().Name}");
                System.Diagnostics.Debug.WriteLine($"Полная ошибка: {ex}");
                return false;
            }
        }

        public async Task<bool> TestEmailConnectionAsync(string testEmail)
        {
            var subject = "Тестовое письмо";
            var body = $@"
                <html>
                <body>
                    <h2>Тестовое письмо</h2>
                    <p>Это тестовое письмо для проверки работы системы отправки email.</p>
                    <p>Время отправки: {DateTime.Now:yyyy-MM-dd HH:mm:ss}</p>
                </body>
                </html>";

            return await SendEmailAsync(testEmail, subject, body);
        }

        public async Task<bool> SendExamResultNotificationAsync(string toEmail, string studentName, string examName, int score, int maxScore)
        {
            var subject = "Результаты экзамена";
            var body = $@"
                <html>
                <body>
                    <h2>Уважаемый(ая) {studentName}!</h2>
                    <p>Ваши результаты экзамена <strong>{examName}</strong>:</p>
                    <p><strong>Баллы: {score} из {maxScore}</strong></p>
                    <p>Спасибо за участие!</p>
                </body>
                </html>";

            return await SendEmailAsync(toEmail, subject, body);
        }
    }
}

