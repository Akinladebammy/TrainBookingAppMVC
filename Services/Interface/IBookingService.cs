using TrainBookingAppMVC.DTOs.RequestModel;
using TrainBookingAppMVC.DTOs.ResponseModel;
using TrainBookingAppMVC.DTOs.Wrapper;

namespace TrainBookingAppMVC.Services.Interface
{
    public interface IBookingService
    {
        Task<ResponseWrapper<BookingListResponseModel>> GetAllBookingsAsync();
        Task<ResponseWrapper<BookingListResponseModel>> GetBookingsByUserIdAsync(Guid userId);
        Task<ResponseWrapper<BookingResponseModel>> GetBookingByIdAsync(Guid id);
        Task<ResponseWrapper<BookingCreationResponseModel>> CreateBookingAsync(CreateBookingRequestModel request);
        Task<ResponseWrapper<string>> CancelBookingAsync(Guid bookingId, CancelBookingRequestModel request);
    }

}
