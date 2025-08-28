using Microsoft.EntityFrameworkCore;
using TrainBookinAppMVC.Models;
using TrainBookinAppWeb.Data;
using TrainBookingAppMVC.Models;
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

        public async Task<bool> UpdateTripAvailableSeatsAsync(Guid tripId, int seatChange, string ticketClass = null)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var trip = await _context.Trips
                    .Include(t => t.TripPricings)
                    .FirstOrDefaultAsync(t => t.Id == tripId);

                if (trip == null) return false;

                var pricing = ticketClass != null && Enum.TryParse<TicketClass>(ticketClass, out var ticketClassEnum)
                    ? trip.TripPricings.FirstOrDefault(p => p.TicketClass == ticketClassEnum)
                    : trip.TripPricings.FirstOrDefault();

                if (pricing != null)
                {
                    pricing.AvailableSeats += seatChange;
                    if (pricing.AvailableSeats < 0)
                    {
                        throw new InvalidOperationException($"Cannot reduce available seats below 0 for {pricing.TicketClass}.");
                    }
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

        public async Task<(bool success, string message, Guid bookingId)> ProcessBookingTransactionAsync(
               Guid tripId,
               Guid userId,
               int numberOfSeats,
               decimal totalPrice,
               string ticketClass,
               List<string> seatNumbers,
               string transactionReference = null)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var trip = await _context.Trips
                    .Include(t => t.TripPricings)
                    .Include(t => t.Train)
                    .Include(t => t.Bookings)
                    .FirstOrDefaultAsync(t => t.Id == tripId);

                if (trip == null)
                {
                    return (false, "Trip not found.", Guid.Empty);
                }

                if (trip.IsExpired || trip.DepartureTime <= DateTime.UtcNow)
                {
                    return (false, "Sorry, this trip has expired while processing your booking.", Guid.Empty);
                }

                if (!Enum.TryParse<TicketClass>(ticketClass, out var ticketClassEnum))
                {
                    return (false, "Invalid ticket class.", Guid.Empty);
                }

                var pricing = trip.TripPricings.FirstOrDefault(p => p.TicketClass == ticketClassEnum);
                if (pricing == null)
                {
                    return (false, $"No pricing found for {ticketClass} class.", Guid.Empty);
                }

                if (pricing.AvailableSeats < seatNumbers.Count)
                {
                    return (false, $"Sorry, only {pricing.AvailableSeats} seats are now available in {ticketClass} class. Please try again with a lower number.", Guid.Empty);
                }

                foreach (var seatNumber in seatNumbers)
                {
                    if (string.IsNullOrEmpty(seatNumber))
                    {
                        return (false, "Seat number cannot be empty.", Guid.Empty);
                    }

                    var seatTaken = trip.Bookings.Any(b => !b.IsCancelled && b.SeatNumbers.Contains(seatNumber) && b.TicketClass == ticketClassEnum);
                    if (seatTaken)
                    {
                        return (false, $"Sorry, seat {seatNumber} is already taken. Please select another seat.", Guid.Empty);
                    }

                    var seatRegex = new System.Text.RegularExpressions.Regex($@"^{ticketClass[0]}\d+$");
                    if (!seatRegex.IsMatch(seatNumber))
                    {
                        return (false, $"Invalid seat number: {seatNumber}. Must be in format like '{ticketClass[0]}1'.", Guid.Empty);
                    }
                }

                var booking = new Booking
                {
                    Id = Guid.NewGuid(),
                    TripId = tripId,
                    UserId = userId,
                    TicketClass = ticketClassEnum,
                    SeatNumbers = seatNumbers,
                    NumberOfSeats = seatNumbers.Count,
                    TotalPrice = totalPrice,
                    BookingDate = DateTime.UtcNow,
                    IsCancelled = false,
                    TransactionReference = transactionReference
                };

                _context.Bookings.Add(booking);

                pricing.AvailableSeats -= seatNumbers.Count;
                if (pricing.AvailableSeats < 0)
                {
                    throw new InvalidOperationException($"Cannot reduce available seats below 0 for {ticketClass}.");
                }
                _context.TripPricings.Update(pricing);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return (true, $"Booking successful! {seatNumbers.Count} seats booked.", booking.Id);
            }
            catch (DbUpdateException dbEx)
            {
                // Log inner exception
                Console.WriteLine($"DbUpdateException: {dbEx.InnerException?.Message ?? dbEx.Message}");
                return (false, $"Booking failed: {dbEx.InnerException?.Message ?? dbEx.Message}", Guid.Empty);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"General Exception: {ex.Message}, Inner: {ex.InnerException?.Message}");
                return (false, $"Booking failed: {ex.Message}", Guid.Empty);
            }
        }

        public async Task<List<string>> GetAvailableSeatsAsync(Guid tripId, TicketClass ticketClass)
        {
            var trip = await _context.Trips
                .Include(t => t.Train)
                .Include(t => t.TripPricings)
                .Include(t => t.Bookings)
                .FirstOrDefaultAsync(t => t.Id == tripId);

            if (trip == null) return new List<string>();

            var pricing = trip.TripPricings.FirstOrDefault(p => p.TicketClass == ticketClass);
            if (pricing == null) return new List<string>();

            var totalSeats = pricing.TotalSeats;
            var bookedSeats = trip.Bookings
                .Where(b => !b.IsCancelled && b.TicketClass == ticketClass)
                .SelectMany(b => b.SeatNumbers)
                .ToList();

            var availableSeats = new List<string>();
            for (int i = 1; i <= totalSeats; i++)
            {
                var seatNumber = $"{ticketClass.ToString()[0]}{i}";
                if (!bookedSeats.Contains(seatNumber))
                {
                    availableSeats.Add(seatNumber);
                }
            }

            return availableSeats;
        }
    }
}