using TrainBookinAppMVC.Models;
using TrainBookingAppMVC.Models;

namespace TrainBookingAppMVC.Repository.Interfaces
{
    public interface ITrainRepository
    {
        Task<Train?> GetByIdAsync(Guid id);
        Task<Train?> GetByTrainNumberAsync(string trainNumber);
        Task<Train?> GetByNameAsync(string name);
        Task<List<Train>> GetAllAsync();
        Task<bool> AddAsync(Train train);
        Task<bool> UpdateAsync(Train train);
        Task<bool> DeleteAsync(Guid id);
        Task<bool> ExistsAsync(Guid id);
        Task<bool> ExistsByTrainNumberAsync(string trainNumber);
        Task<bool> ExistsByTrainNumberAsync(string trainNumber, Guid excludeId);
        Task<bool> HasTrainsAsync();
        Task<bool> HasActiveTripsAsync(Guid trainId);
        Task<int> GetMaxBookedSeatsForActiveTripsByTrainIdAsync(Guid trainId);
        Task<int> GetActiveTripsCountAsync(Guid trainId);
        Task UpdateAvailableSeatsForActiveTripsAsync(Guid trainId, int newCapacity, int businessCapacity, int firstClassCapacity);
        Task<Train> GetTrainByIdAsync(Guid trainId);
    }
}
