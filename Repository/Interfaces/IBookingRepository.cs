using TrainBookinAppMVC.Models;
using TrainBookingAppMVC.Models;
using TrainBookingAppMVC.Models.Enum;

namespace TrainBookingAppMVC.Repository.Interfaces
{
    public interface IBookingRepository
    {
        Task<IEnumerable<Booking>> GetAllBookingsAsync();
        Task<IEnumerable<Booking>> GetBookingsByUserIdAsync(Guid userId);
        Task<Booking?> GetBookingByIdAsync(Guid id);
        Task<Booking?> GetBookingByIdAndUserIdAsync(Guid id, Guid userId);
        Task<Booking> CreateBookingAsync(Booking booking);
        Task<bool> DeleteBookingAsync(Guid id);
        Task<bool> HasActiveBookingForTripAsync(Guid tripId, Guid userId);
        Task<int> GetTotalBookedSeatsForTripAsync(Guid tripId);
        Task<bool> UpdateTripAvailableSeatsAsync(Guid tripId, int seatChange, string ticketClass = null);
        Task<(bool success, string message, Guid bookingId)> ProcessBookingTransactionAsync(Guid tripId, Guid userId, int numberOfSeats, decimal totalPrice, string ticketClass, List<string> seatNumbers, string transactionReference = null);
        Task<List<string>> GetAvailableSeatsAsync(Guid tripId, TicketClass ticketClass);
    }
}