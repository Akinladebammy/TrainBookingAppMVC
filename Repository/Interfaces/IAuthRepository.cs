using TrainBookingAppMVC.Models;

namespace TrainBookingAppMVC.Repository.Interfaces
{
    public interface IAuthRepository
    {
        Task<User> GetUserByUsernameAsync(string username);
        Task<User> CreateUserAsync(User user);
        Task<bool> UserExistsAsync(string username);


    }
}
