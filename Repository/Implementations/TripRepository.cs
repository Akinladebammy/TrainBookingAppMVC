using Microsoft.EntityFrameworkCore;
using TrainBookingAppMVC.Models;
using TrainBookingAppMVC.Models.Enum;
using TrainBookingAppMVC.Repository.Interfaces;
using TrainBookinAppWeb.Data;

namespace TrainBookingAppMVC.Repository.Implementations
{
    public class TripRepository : ITripRepository
    {
        private readonly TrainAppContext _context;

        public TripRepository(TrainAppContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Trip>> GetAllTripsAsync()
        {
            await MarkExpiredTripsAsync();
            return await _context.Trips
                .Include(t => t.Train)
                .Include(t => t.TripPricings)
                .OrderBy(t => t.DepartureTime)
                .ToListAsync();
        }

        public async Task<IEnumerable<Trip>> GetAvailableTripsAsync()
        {
            await MarkExpiredTripsAsync();
            return await _context.Trips
                .Include(t => t.Train)
                .Include(t => t.TripPricings)
                .Where(t => !t.IsExpired &&
                           t.DepartureTime > DateTime.UtcNow &&
                           t.TripPricings.Any(tp => tp.AvailableSeats > 0))
                .OrderBy(t => t.DepartureTime)
                .ToListAsync();
        }

        public async Task<Trip?> GetTripByIdAsync(Guid id)
        {
            return await _context.Trips
                .Include(t => t.Train)
                .Include(t => t.TripPricings)
                .FirstOrDefaultAsync(t => t.Id == id);
        }

        public async Task<Trip?> GetTripWithDetailsAsync(Guid id)
        {
            return await _context.Trips
                .Include(t => t.Train)
                .Include(t => t.TripPricings)
                .Include(t => t.Bookings)
                .FirstOrDefaultAsync(t => t.Id == id);
        }

        public async Task<IEnumerable<Trip>> GetTripsByTrainIdAsync(Guid trainId)
        {
            return await _context.Trips
                .Include(t => t.TripPricings)
                .Include(t => t.Train)
                .Where(t => t.TrainId == trainId)
                .ToListAsync();
        }

        public async Task<Trip> CreateTripAsync(Trip trip)
        {
            if (trip.Source == trip.Destination)
            {
                throw new ArgumentException("Source and destination terminals cannot be the same.");
            }

            _context.Trips.Add(trip);
            await _context.SaveChangesAsync();
            return trip;
        }

        public async Task<Trip> UpdateTripAsync(Trip trip)
        {
            if (trip.Source == trip.Destination)
            {
                throw new ArgumentException("Source and destination terminals cannot be the same.");
            }

            trip.UpdatedAt = DateTime.UtcNow;
            _context.Trips.Update(trip);
            await _context.SaveChangesAsync();
            return trip;
        }

        public async Task<bool> DeleteTripAsync(Guid id)
        {
            var trip = await _context.Trips.FindAsync(id);
            if (trip == null) return false;

            _context.Trips.Remove(trip);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> TripExistsAsync(Guid id)
        {
            return await _context.Trips.AnyAsync(t => t.Id == id);
        }

        public async Task<bool> HasActiveBookingsAsync(Guid tripId)
        {
            return await _context.Bookings
                .AnyAsync(b => b.TripId == tripId && !b.IsCancelled);
        }

        public async Task MarkExpiredTripsAsync()
        {
            var expiredTrips = await _context.Trips
                .Where(t => t.DepartureTime <= DateTime.UtcNow && !t.IsExpired)
                .ToListAsync();

            foreach (var trip in expiredTrips)
            {
                trip.IsExpired = true;
                trip.UpdatedAt = DateTime.UtcNow;
            }

            if (expiredTrips.Any())
            {
                await _context.SaveChangesAsync();
            }
        }

        public async Task<bool> UpdateSeatAvailabilityAsync(Guid tripId, TicketClass ticketClass, int seatsToBook)
        {
            var tripPricing = await _context.TripPricings
                .FirstOrDefaultAsync(tp => tp.TripId == tripId && tp.TicketClass == ticketClass);

            if (tripPricing == null || tripPricing.AvailableSeats < seatsToBook)
                return false;

            tripPricing.AvailableSeats -= seatsToBook;
            await _context.SaveChangesAsync();
            return true;
        }
    }
}