using Microsoft.EntityFrameworkCore;
using Web_Accounting_Exams.Data;
using Web_Accounting_Exams.Models;

namespace Web_Accounting_Exams.Services
{
    public class AuthenticationService
    {
        private readonly ApplicationDbContext _context;
        private Models.User? _currentUser;

        public AuthenticationService(ApplicationDbContext context)
        {
            _context = context;
        }

        public Models.User? CurrentUser => _currentUser;

        public async Task<Models.User?> AuthenticateAsync(string email, string password)
        {
            var passwordHash = HashPassword(password);
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == email && u.PasswordHash == passwordHash);

            if (user != null)
            {
                _currentUser = user;
            }

            return user;
        }

        public async Task<bool> RegisterAsync(Models.User user)
        {
            if (await _context.Users.AnyAsync(u => u.Email == user.Email))
            {
                return false;
            }

            user.PasswordHash = HashPassword(user.PasswordHash);
            user.CreatedAt = DateTime.Now;
            user.Role = Models.UserRole.Applicant;

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            _currentUser = user;
            return true;
        }

        public void Logout()
        {
            _currentUser = null;
        }

        public void SetCurrentUser(Models.User user)
        {
            _currentUser = user;
        }

        private string HashPassword(string password)
        {
            return Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(password));
        }

        public bool IsAdmin()
        {
            return _currentUser?.Role == Models.UserRole.Admin;
        }

        public bool IsApplicant()
        {
            return _currentUser?.Role == Models.UserRole.Applicant;
        }
    }
}

