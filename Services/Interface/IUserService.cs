using TrainBookingAppMVC.DTOs.RequestModel;
using TrainBookingAppMVC.DTOs.ResponseModel;
using TrainBookingAppMVC.DTOs.Wrapper;

namespace TrainBookingAppMVC.Services.Interface
{
    public interface IUserService
    {
        Task<ResponseWrapper<UserListResponseModel>> GetAllUsersAsync();
        Task<ResponseWrapper<UserResponseModel>> GetUserByIdAsync(Guid id);
        Task<ResponseWrapper<UserResponseModel>> GetUserByEmailAsync(string email);
        Task<ResponseWrapper<UserResponseModel>> UpdateUserAsync(Guid id, UpdateUserRequestModel request);
        Task<ResponseWrapper> DeleteUserAsync(Guid id);
    }
}