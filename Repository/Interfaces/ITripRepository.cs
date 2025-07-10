using TrainBookingAppMVC.Models;
using TrainBookingAppMVC.Models.Enum;

namespace TrainBookingAppMVC.Repository.Interfaces
{
    public interface ITripRepository
    {
        Task<IEnumerable<Trip>> GetAllTripsAsync();
        Task<IEnumerable<Trip>> GetAvailableTripsAsync();
        Task<Trip?> GetTripByIdAsync(Guid id);
        Task<Trip?> GetTripWithDetailsAsync(Guid id);
        Task<Trip> CreateTripAsync(Trip trip);
        Task<Trip> UpdateTripAsync(Trip trip);
        Task<bool> DeleteTripAsync(Guid id);
        Task<bool> TripExistsAsync(Guid id);
        Task<bool> HasActiveBookingsAsync(Guid tripId);
        Task MarkExpiredTripsAsync();
        Task<bool> UpdateSeatAvailabilityAsync(Guid tripId, TicketClass ticketClass, int seatsToBook);
    }
}
