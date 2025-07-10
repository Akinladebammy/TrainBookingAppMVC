using TrainBookingAppMVC.DTOs.RequestModel;
using TrainBookingAppMVC.DTOs.ResponseModel;
using TrainBookingAppMVC.DTOs.Wrapper;

namespace TrainBookingAppMVC.Services.Interface
{
    public interface ITrainService
    {
        Task<ResponseWrapper<TrainResponseModel>> CreateTrainAsync(CreateTrainRequestModel request);
        Task<ResponseWrapper<TrainListResponseModel>> GetAllTrainsAsync();
        Task<ResponseWrapper<TrainResponseModel>> GetTrainByIdAsync(Guid id);
        Task<ResponseWrapper<TrainResponseModel>> GetTrainByTrainNumberAsync(string trainNumber);
        Task<ResponseWrapper<TrainResponseModel>> GetTrainByNameAsync(string name);
        Task<ResponseWrapper<TrainResponseModel>> UpdateTrainAsync(Guid id, UpdateTrainRequestModel request);
        Task<ResponseWrapper> DeleteTrainAsync(Guid id);
        Task<ResponseWrapper> EnsureSampleTrainsExistAsync();
    }
}
