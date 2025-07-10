using Microsoft.EntityFrameworkCore;
using TrainBookinAppWeb.Data;
using TrainBookingAppMVC.Models;
using TrainBookingAppMVC.Repository.Interfaces;

namespace TrainBookingAppMVC.Repository.Implementations
{
    public class UserRepository : IUserRepository
    {
        private readonly TrainAppContext _context;

        public UserRepository(TrainAppContext context)
        {
            _context = context;
        }

        public async Task<List<User>> GetAllUsersAsync()
        {
            return await _context.Users.OrderBy(u => u.FullName).ToListAsync();
        }

        public async Task<User> GetUserByIdAsync(Guid id)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.Id == id);
        }

        public async Task<User> GetUserByEmailAsync(string email)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());
        }

        public async Task<User> UpdateUserAsync(User user)
        {
            var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == user.Id);
            if (existingUser == null)
                return null;

            existingUser.FullName = user.FullName;
            existingUser.Email = user.Email;
            existingUser.Username = user.Username;
            existingUser.Role = user.Role;

            await _context.SaveChangesAsync();
            return existingUser;
        }

        public async Task<bool> DeleteUserAsync(Guid id)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id);
            if (user == null)
                return false;

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UserExistsByEmailAsync(string email)
        {
            return await _context.Users.AnyAsync(u => u.Email.ToLower() == email.ToLower());
        }

        public async Task<bool> UserExistsByUsernameAsync(string username, Guid? excludeUserId = null)
        {
            var query = _context.Users.Where(u => u.Username.ToLower() == username.ToLower());

            if (excludeUserId.HasValue)
            {
                query = query.Where(u => u.Id != excludeUserId.Value);
            }

            return await query.AnyAsync();
        }
    }
}