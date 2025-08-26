using Microsoft.EntityFrameworkCore;
using TrainBookinAppWeb.Data;
using TrainBookingAppMVC.Models;
using TrainBookingAppMVC.Repository.Interfaces;

namespace TrainBookingAppMVC.Repository.Implementations
{
    public class TrainRepository : ITrainRepository
    {
        private readonly TrainAppContext _context;

        public TrainRepository(TrainAppContext context)
        {
            _context = context;
        }

        public async Task<Train?> GetByIdAsync(Guid id)
        {
            return await _context.Trains.FirstOrDefaultAsync(t => t.Id == id);
        }

        public async Task<Train?> GetByTrainNumberAsync(string trainNumber)
        {
            return await _context.Trains.FirstOrDefaultAsync(t => t.TrainNumber == trainNumber);
        }

        public async Task<Train?> GetByNameAsync(string name)
        {
            return await _context.Trains.FirstOrDefaultAsync(t => t.Name == name);
        }

        public async Task<List<Train>> GetAllAsync()
        {
            return await _context.Trains.OrderBy(t => t.Id).ToListAsync();
        }

        public async Task<bool> AddAsync(Train train, IFormFile? imageFile = null)
        {
            try
            {
                if (imageFile != null && imageFile.Length > 0)
                {
                    // Define the path to save the image (e.g., wwwroot/images/trains)
                    var fileName = $"{Guid.NewGuid()}_{Path.GetFileName(imageFile.FileName)}";
                    var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/trains", fileName);

                    // Ensure the directory exists
                    Directory.CreateDirectory(Path.GetDirectoryName(filePath));

                    // Save the image file
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await imageFile.CopyToAsync(stream);
                    }

                    // Store the relative path in the database
                    train.ImagePath = $"/images/trains/{fileName}";
                }

                _context.Trains.Add(train);
                return await _context.SaveChangesAsync() > 0;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> UpdateAsync(Train train, IFormFile? imageFile = null)
        {
            try
            {
                var existingTrain = await GetByIdAsync(train.Id);
                if (existingTrain == null) return false;

                // Update train properties
                existingTrain.TrainNumber = train.TrainNumber;
                existingTrain.Name = train.Name;
                existingTrain.Description = train.Description;
                existingTrain.EconomyCapacity = train.EconomyCapacity;
                existingTrain.BusinessCapacity = train.BusinessCapacity;
                existingTrain.FirstClassCapacity = train.FirstClassCapacity;

                if (imageFile != null && imageFile.Length > 0)
                {
                    // Delete old image if it exists
                    if (!string.IsNullOrEmpty(existingTrain.ImagePath))
                    {
                        var oldImagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", existingTrain.ImagePath.TrimStart('/'));
                        if (File.Exists(oldImagePath))
                        {
                            File.Delete(oldImagePath);
                        }
                    }

                    // Save new image
                    var fileName = $"{Guid.NewGuid()}_{Path.GetFileName(imageFile.FileName)}";
                    var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/trains", fileName);
                    Directory.CreateDirectory(Path.GetDirectoryName(filePath));

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await imageFile.CopyToAsync(stream);
                    }

                    existingTrain.ImagePath = $"/images/trains/{fileName}";
                }

                _context.Entry(existingTrain).State = EntityState.Modified;
                return await _context.SaveChangesAsync() > 0;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            try
            {
                var train = await GetByIdAsync(id);
                if (train == null) return false;

                // Delete associated image if it exists
                if (!string.IsNullOrEmpty(train.ImagePath))
                {
                    var imagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", train.ImagePath.TrimStart('/'));
                    if (File.Exists(imagePath))
                    {
                        File.Delete(imagePath);
                    }
                }

                _context.Trains.Remove(train);
                return await _context.SaveChangesAsync() > 0;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> ExistsAsync(Guid id)
        {
            return await _context.Trains.AnyAsync(t => t.Id == id);
        }

        public async Task<bool> ExistsByTrainNumberAsync(string trainNumber)
        {
            return await _context.Trains.AnyAsync(t => t.TrainNumber == trainNumber);
        }

        public async Task<bool> ExistsByTrainNumberAsync(string trainNumber, Guid excludeId)
        {
            return await _context.Trains.AnyAsync(t => t.TrainNumber == trainNumber && t.Id != excludeId);
        }

        public async Task<bool> HasTrainsAsync()
        {
            return await _context.Trains.CountAsync() > 0;
        }

        public async Task<bool> HasActiveTripsAsync(Guid trainId)
        {
            return await _context.Trips.AnyAsync(t => t.TrainId == trainId && t.DepartureTime > DateTime.UtcNow);
        }

        public async Task<int> GetMaxBookedSeatsForActiveTripsByTrainIdAsync(Guid trainId)
        {
            var train = await _context.Trains.FirstOrDefaultAsync(t => t.Id == trainId);
            if (train == null) return 0;

            var activeTrips = await _context.Trips
                .Where(t => t.TrainId == trainId && t.DepartureTime > DateTime.UtcNow && !t.IsExpired)
                .Include(t => t.TripPricings)
                .ToListAsync();

            if (!activeTrips.Any()) return 0;

            int maxBookedSeats = 0;
            foreach (var trip in activeTrips)
            {
                int bookedSeatsForTrip = 0;
                foreach (var pricing in trip.TripPricings)
                {
                    bookedSeatsForTrip += pricing.TotalSeats - pricing.AvailableSeats;
                }
                maxBookedSeats = Math.Max(maxBookedSeats, bookedSeatsForTrip);
            }

            return maxBookedSeats;
        }

        public async Task<int> GetActiveTripsCountAsync(Guid trainId)
        {
            return await _context.Trips.CountAsync(t => t.TrainId == trainId && t.DepartureTime > DateTime.UtcNow && !t.IsExpired);
        }

        public async Task UpdateAvailableSeatsForActiveTripsAsync(Guid trainId, int newEconomyCapacity, int newBusinessCapacity, int newFirstClassCapacity)
        {
            var activeTrips = await _context.Trips
                .Where(t => t.TrainId == trainId && t.DepartureTime > DateTime.UtcNow && !t.IsExpired)
                .Include(t => t.TripPricings)
                .ToListAsync();

            foreach (var trip in activeTrips)
            {
                foreach (var pricing in trip.TripPricings)
                {
                    int bookedSeats = pricing.TotalSeats - pricing.AvailableSeats;

                    int newTotalSeats = pricing.TicketClass switch
                    {
                        Models.Enum.TicketClass.Economy => newEconomyCapacity,
                        Models.Enum.TicketClass.Business => newBusinessCapacity,
                        Models.Enum.TicketClass.FirstClass => newFirstClassCapacity,
                        _ => pricing.TotalSeats
                    };

                    pricing.TotalSeats = newTotalSeats;
                    pricing.AvailableSeats = Math.Max(0, newTotalSeats - bookedSeats);
                }
            }

            await _context.SaveChangesAsync();
        }

        public async Task<Train?> GetTrainByIdAsync(Guid trainId)
        {
            return await _context.Trains.FirstOrDefaultAsync(t => t.Id == trainId);
        }
    }
}