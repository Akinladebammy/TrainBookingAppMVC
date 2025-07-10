using Microsoft.EntityFrameworkCore;
using TrainBookinAppMVC.Models;
using TrainBookinAppWeb.Data;
using TrainBookingAppMVC.Models.Enum;
using TrainBookingAppMVC.Repository.Interfaces;

namespace TrainBookingAppMVC.Repository.Implementations
{
    public class BookingRepository : IBookingRepository
    {
        private readonly TrainAppContext _context;

        public BookingRepository(TrainAppContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Booking>> GetAllBookingsAsync()
        {
            return await _context.Bookings
                .Include(b => b.Trip)
                    .ThenInclude(t => t.Train)
                .Include(b => b.User)
                .OrderByDescending(b => b.BookingDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<Booking>> GetBookingsByUserIdAsync(Guid userId)
        {
            return await _context.Bookings
                .Include(b => b.Trip)
                    .ThenInclude(t => t.Train)
                .Include(b => b.User)
                .Where(b => b.UserId == userId)
                .OrderByDescending(b => b.BookingDate)
                .ToListAsync();
        }

        public async Task<Booking?> GetBookingByIdAsync(Guid id)
        {
            return await _context.Bookings
                .Include(b => b.Trip)
                    .ThenInclude(t => t.Train)
                .Include(b => b.User)
                .FirstOrDefaultAsync(b => b.Id == id);
        }

        public async Task<Booking?> GetBookingByIdAndUserIdAsync(Guid id, Guid userId)
        {
            return await _context.Bookings
                .Include(b => b.Trip)
                    .ThenInclude(t => t.Train)
                .Include(b => b.User)
                .FirstOrDefaultAsync(b => b.Id == id && b.UserId == userId);
        }

        public async Task<Booking> CreateBookingAsync(Booking booking)
        {
            _context.Bookings.Add(booking);
            await _context.SaveChangesAsync();
            return booking;
        }

        public async Task<bool> DeleteBookingAsync(Guid id)
        {
            var booking = await _context.Bookings.FindAsync(id);
            if (booking == null) return false;

            _context.Bookings.Remove(booking);
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<bool> HasActiveBookingForTripAsync(Guid tripId, Guid userId)
        {
            return await _context.Bookings
                .AnyAsync(b => b.TripId == tripId && b.UserId == userId && !b.IsCancelled);
        }

        public async Task<int> GetTotalBookedSeatsForTripAsync(Guid tripId)
        {
            return await _context.Bookings
                .Where(b => b.TripId == tripId && !b.IsCancelled)
                .SumAsync(b => b.NumberOfSeats);
        }

        public async Task<bool> UpdateTripAvailableSeatsAsync(Guid tripId, int seatChange)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var trip = await _context.Trips
                    .Include(t => t.TripPricings)
                    .FirstOrDefaultAsync(t => t.Id == tripId);

                if (trip == null) return false;

                // For now, we'll update the first pricing entry
                // In a real scenario, you'd need to specify which ticket class
                var pricing = trip.TripPricings.FirstOrDefault();
                if (pricing != null)
                {
                    pricing.AvailableSeats += seatChange; // Add seats back for cancellation
                    _context.TripPricings.Update(pricing);
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return true;
            }
            catch
            {
                await transaction.RollbackAsync();
                return false;
            }
        }

        public async Task<(bool success, string message)> ProcessBookingTransactionAsync(
            Guid tripId,
            Guid userId,
            int numberOfSeats,
            decimal totalPrice,
            string ticketClass)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // Get trip with lock
                var trip = await _context.Trips
                    .Include(t => t.TripPricings)
                    .Include(t => t.Train)
                    .FirstOrDefaultAsync(t => t.Id == tripId);

                if (trip == null)
                {
                    return (false, "Trip not found.");
                }

                // Final check for expiration
                if (trip.IsExpired || trip.DepartureTime <= DateTime.UtcNow)
                {
                    return (false, "Sorry, this trip has expired while processing your booking.");
                }

                // Parse ticket class
                if (!Enum.TryParse<TicketClass>(ticketClass, out var ticketClassEnum))
                {
                    return (false, "Invalid ticket class.");
                }

                // Get specific pricing
                var pricing = trip.TripPricings.FirstOrDefault(p => p.TicketClass == ticketClassEnum);
                if (pricing == null)
                {
                    return (false, $"No pricing found for {ticketClass} class.");
                }

                // Final seat availability check
                if (pricing.AvailableSeats < numberOfSeats)
                {
                    return (false, $"Sorry, only {pricing.AvailableSeats} seats are now available in {ticketClass} class. Please try again with a lower number.");
                }

                // Create booking
                var booking = new Booking
                {
                    Id = Guid.NewGuid(),
                    TripId = tripId,
                    UserId = userId,
                    TicketClass = ticketClassEnum,
                    NumberOfSeats = numberOfSeats,
                    TotalPrice = totalPrice,
                    BookingDate = DateTime.UtcNow,
                    IsCancelled = false
                };

                _context.Bookings.Add(booking);

                // Update available seats
                pricing.AvailableSeats -= numberOfSeats;
                _context.TripPricings.Update(pricing);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return (true, $"Booking successful! Your booking ID is: {booking.Id}");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return (false, $"Booking failed: {ex.Message}");
            }
        }
    }
}
