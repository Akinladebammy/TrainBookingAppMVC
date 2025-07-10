using TrainBookingAppMVC.Models;

namespace TrainBookingAppMVC.Repository.Interfaces
{
    public interface IUserRepository
    {
        Task<List<User>> GetAllUsersAsync();
        Task<User> GetUserByIdAsync(Guid id);
        Task<User> GetUserByEmailAsync(string email);
        Task<User> UpdateUserAsync(User user);
        Task<bool> DeleteUserAsync(Guid id);
        Task<bool> UserExistsByEmailAsync(string email);
        Task<bool> UserExistsByUsernameAsync(string username, Guid? excludeUserId = null);
    }
}
