using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using Web_Accounting_Exams.Data;
using Web_Accounting_Exams.Models;

namespace Web_Accounting_Exams.Services
{
    public class PasswordResetService
    {
        private readonly ApplicationDbContext _context;
        private readonly EmailService _emailService;

        public string? LastErrorMessage { get; private set; }

        public PasswordResetService(ApplicationDbContext context, EmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        public async Task<bool> RequestPasswordResetAsync(string email)
        {
            LastErrorMessage = null;
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
            {
                LastErrorMessage = "Пользователь с таким email адресом не найден.";
                return false;
            }

            var code = GenerateResetCode();

            var oldTokens = await _context.PasswordResetTokens
                .Where(t => t.UserId == user.Id && !t.IsUsed && t.ExpiresAt > DateTime.Now)
                .ToListAsync();
            _context.PasswordResetTokens.RemoveRange(oldTokens);

            var token = new PasswordResetToken
            {
                UserId = user.Id,
                Code = code,
                CreatedAt = DateTime.Now,
                ExpiresAt = DateTime.Now.AddMinutes(15),
                IsUsed = false
            };

            _context.PasswordResetTokens.Add(token);
            await _context.SaveChangesAsync();
            var subject = "Восстановление пароля";
            var body = $@"
                <html>
                <body style='font-family: Arial, sans-serif;'>
                    <h2>Восстановление пароля</h2>
                    <p>Здравствуйте, {user.FirstName}!</p>
                    <p>Вы запросили восстановление пароля для вашего аккаунта.</p>
                    <p><strong>Код восстановления: <span style='font-size: 24px; color: #6C5CE7;'>{code}</span></strong></p>
                    <p>Этот код действителен в течение 15 минут.</p>
                    <p>Если вы не запрашивали восстановление пароля, проигнорируйте это письмо.</p>
                    <hr>
                    <p style='color: #757575; font-size: 12px;'>Это автоматическое сообщение, пожалуйста, не отвечайте на него.</p>
                </body>
                </html>";

            var emailSent = await _emailService.SendEmailAsync(email, subject, body);
            if (!emailSent)
            {
                LastErrorMessage = "Ошибка отправки email. Проверьте настройки SMTP или попробуйте позже.";
            }
            return emailSent;
        }

        public async Task<bool> VerifyResetCodeAsync(string email, string code)
        {
            LastErrorMessage = null;
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
            {
                LastErrorMessage = "Пользователь с таким email адресом не найден.";
                return false;
            }

            var token = await _context.PasswordResetTokens
                .Where(t => t.UserId == user.Id && 
                           t.Code == code && 
                           !t.IsUsed && 
                           t.ExpiresAt > DateTime.Now)
                .OrderByDescending(t => t.CreatedAt)
                .FirstOrDefaultAsync();

            if (token == null)
            {
                LastErrorMessage = "Неверный или истекший код восстановления. Проверьте код и попробуйте еще раз.";
                return false;
            }

            return true;
        }

        public async Task<bool> ResetPasswordAsync(string email, string code, string newPassword)
        {
            LastErrorMessage = null;
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
            {
                LastErrorMessage = "Пользователь с таким email адресом не найден.";
                return false;
            }

            var token = await _context.PasswordResetTokens
                .Where(t => t.UserId == user.Id && 
                           t.Code == code && 
                           !t.IsUsed && 
                           t.ExpiresAt > DateTime.Now)
                .OrderByDescending(t => t.CreatedAt)
                .FirstOrDefaultAsync();

            if (token == null)
            {
                LastErrorMessage = "Неверный или истекший код восстановления. Пожалуйста, начните процесс заново.";
                return false;
            }

            token.IsUsed = true;

            var passwordHash = HashPassword(newPassword);
            user.PasswordHash = passwordHash;

            await _context.SaveChangesAsync();

            return true;
        }

        private string GenerateResetCode()
        {
            var random = new Random();
            return random.Next(100000, 999999).ToString();
        }

        private string HashPassword(string password)
        {
            return Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(password));
        }
    }
}

