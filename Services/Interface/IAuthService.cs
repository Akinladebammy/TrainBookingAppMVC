using TrainBookinAppMVC.DTOs.ResponseModel;
using TrainBookingAppMVC.DTOs.RequestModel;
using TrainBookingAppMVC.DTOs.Wrapper;

namespace TrainBookingAppMVC.Services.Interface
{
    public interface IAuthService
    {
        Task<ResponseWrapper<AuthResponseModel>> LoginAsync(AuthRequestModel request);
        Task<ResponseWrapper<AuthResponseModel>> RegisterAsync(RegisterRequestModel request);
    }
}