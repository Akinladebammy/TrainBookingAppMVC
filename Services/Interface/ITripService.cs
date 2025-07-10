using TrainBookingAppMVC.DTOs.RequestModel;
using TrainBookingAppMVC.DTOs.ResponseModel;
using TrainBookingAppMVC.DTOs.Wrapper;

namespace TrainBookingAppMVC.Services.Interface
{
    public interface ITripService
    {
        Task<ResponseWrapper<IEnumerable<TripDto>>> GetAllTripsAsync();
        Task<ResponseWrapper<IEnumerable<TripDto>>> GetAvailableTripsAsync();
        Task<ResponseWrapper<TripDto>> GetTripByIdAsync(Guid id);
        Task<ResponseWrapper<TripDto>> CreateTripAsync(CreateTripDto createTripDto);
        Task<ResponseWrapper<TripDto>> UpdateTripAsync(Guid id, UpdateTripDto updateTripDto);
        Task<ResponseWrapper> DeleteTripAsync(Guid id);
    }
}