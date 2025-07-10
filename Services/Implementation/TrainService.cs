using TrainBookinAppMVC.Models;
using TrainBookingAppMVC.DTOs.RequestModel;
using TrainBookingAppMVC.DTOs.ResponseModel;
using TrainBookingAppMVC.DTOs.Wrapper;
using TrainBookingAppMVC.Models;
using TrainBookingAppMVC.Repository.Interfaces;
using TrainBookingAppMVC.Services.Interface;

namespace TrainBookingAppMVC.Services.Implementation
{
    public class TrainService : ITrainService
    {
        private readonly ITrainRepository _trainRepository;

        public TrainService(ITrainRepository trainRepository)
        {
            _trainRepository = trainRepository;
        }

        public async Task<ResponseWrapper<TrainResponseModel>> CreateTrainAsync(CreateTrainRequestModel request)
        {
            // Check if train number already exists
            if (await _trainRepository.ExistsByTrainNumberAsync(request.TrainNumber))
            {
                return ResponseWrapper<TrainResponseModel>.ErrorResponse("Train number already exists.");
            }

            var train = new Train
            {
                Id = Guid.NewGuid(),
                TrainNumber = request.TrainNumber,
                Name = request.Name,
                EconomyCapacity = request.EconomicCapacity,
                BusinessCapacity = request.BusinessCapacity,
                FirstClassCapacity = request.FirstClassCapacity,
                Description = request.Description
            };

            bool success = await _trainRepository.AddAsync(train);

            if (success)
            {
                return ResponseWrapper<TrainResponseModel>.SuccessResponse(
                    MapToResponse(train),
                    "Train added successfully."
                );
            }

            return ResponseWrapper<TrainResponseModel>.ErrorResponse("Failed to add train.");
        }

        public async Task<ResponseWrapper<TrainListResponseModel>> GetAllTrainsAsync()
        {
            try
            {
                var trains = await _trainRepository.GetAllAsync();
                var trainListResponse = new TrainListResponseModel
                {
                    Trains = trains.Select(MapToResponse).ToList(),
                    TotalCount = trains.Count
                };

                return ResponseWrapper<TrainListResponseModel>.SuccessResponse(
                    trainListResponse,
                    "Trains retrieved successfully."
                );
            }
            catch (Exception ex)
            {
                return ResponseWrapper<TrainListResponseModel>.ErrorResponse($"Failed to retrieve trains: {ex.Message}");
            }
        }

        public async Task<ResponseWrapper<TrainResponseModel>> GetTrainByIdAsync(Guid id)
        {
            try
            {
                var train = await _trainRepository.GetByIdAsync(id);

                if (train == null)
                {
                    return ResponseWrapper<TrainResponseModel>.ErrorResponse("Train not found.");
                }

                return ResponseWrapper<TrainResponseModel>.SuccessResponse(
                    MapToResponse(train),
                    "Train retrieved successfully."
                );
            }
            catch (Exception ex)
            {
                return ResponseWrapper<TrainResponseModel>.ErrorResponse($"Failed to retrieve train: {ex.Message}");
            }
        }

        public async Task<ResponseWrapper<TrainResponseModel>> GetTrainByTrainNumberAsync(string trainNumber)
        {
            try
            {
                var train = await _trainRepository.GetByTrainNumberAsync(trainNumber);

                if (train == null)
                {
                    return ResponseWrapper<TrainResponseModel>.ErrorResponse("Train not found.");
                }

                return ResponseWrapper<TrainResponseModel>.SuccessResponse(
                    MapToResponse(train),
                    "Train retrieved successfully."
                );
            }
            catch (Exception ex)
            {
                return ResponseWrapper<TrainResponseModel>.ErrorResponse($"Failed to retrieve train: {ex.Message}");
            }
        }

        public async Task<ResponseWrapper<TrainResponseModel>> GetTrainByNameAsync(string name)
        {
            try
            {
                var train = await _trainRepository.GetByNameAsync(name);

                if (train == null)
                {
                    return ResponseWrapper<TrainResponseModel>.ErrorResponse("Train not found.");
                }

                return ResponseWrapper<TrainResponseModel>.SuccessResponse(
                    MapToResponse(train),
                    "Train retrieved successfully."
                );
            }
            catch (Exception ex)
            {
                return ResponseWrapper<TrainResponseModel>.ErrorResponse($"Failed to retrieve train: {ex.Message}");
            }
        }

        public async Task<ResponseWrapper<TrainResponseModel>> UpdateTrainAsync(Guid id, UpdateTrainRequestModel request)
        {
            // Check if train exists
            var existingTrain = await _trainRepository.GetByIdAsync(id);
            if (existingTrain == null)
            {
                return ResponseWrapper<TrainResponseModel>.ErrorResponse("Train not found.");
            }

            // Check if new train number already exists for another train
            if (await _trainRepository.ExistsByTrainNumberAsync(request.TrainNumber, id))
            {
                return ResponseWrapper<TrainResponseModel>.ErrorResponse("Train number already exists for another train.");
            }

            // Check if there are active trips and if new capacity affects them
            int activeTripsCount = await _trainRepository.GetActiveTripsCountAsync(id);
            if (activeTripsCount > 0)
            {
                int maxBookedSeats = await _trainRepository.GetMaxBookedSeatsForActiveTripsByTrainIdAsync(id);
                int newTotalCapacity = request.EconomicCapacity + request.BusinessCapacity + request.FirstClassCapacity;

                if (newTotalCapacity < maxBookedSeats)
                {
                    return ResponseWrapper<TrainResponseModel>.ErrorResponse(
                        $"Cannot update capacity to {newTotalCapacity} because there are active trips with {maxBookedSeats} booked seats. Please ensure the new capacity is at least equal to the maximum booked seats for active trips."
                    );
                }
            }

            // Update train properties
            existingTrain.TrainNumber = request.TrainNumber;
            existingTrain.Name = request.Name;
            existingTrain.EconomyCapacity = request.EconomicCapacity;
            existingTrain.BusinessCapacity = request.BusinessCapacity;
            existingTrain.FirstClassCapacity = request.FirstClassCapacity;
            existingTrain.Description = request.Description;

            bool success = await _trainRepository.UpdateAsync(existingTrain);

            if (success)
            {
                // If capacity changed and there are active trips, update available seats
                if (activeTripsCount > 0)
                {
                    await _trainRepository.UpdateAvailableSeatsForActiveTripsAsync(
                        id,
                        request.EconomicCapacity,
                        request.BusinessCapacity,
                        request.FirstClassCapacity
                    );
                }

                var message = "Train updated successfully.";
                if (activeTripsCount > 0)
                {
                    message += $" Available seats for {activeTripsCount} active trip(s) have been adjusted accordingly.";
                }

                return ResponseWrapper<TrainResponseModel>.SuccessResponse(
                    MapToResponse(existingTrain),
                    message
                );
            }

            return ResponseWrapper<TrainResponseModel>.ErrorResponse("Failed to update train.");
        }

        public async Task<ResponseWrapper> DeleteTrainAsync(Guid id)
        {
            // Check if train exists
            if (!await _trainRepository.ExistsAsync(id))
            {
                return ResponseWrapper.ErrorResponse("Train not found.");
            }

            // Check if there are active trips for this train
            if (await _trainRepository.HasActiveTripsAsync(id))
            {
                return ResponseWrapper.ErrorResponse("Cannot delete train because there are active trips for it. You need to delete all trips for this train first.");
            }

            bool success = await _trainRepository.DeleteAsync(id);

            return success
                ? ResponseWrapper.SuccessResponse("Train deleted successfully.")
                : ResponseWrapper.ErrorResponse("Failed to delete train.");
        }

        public async Task<ResponseWrapper> EnsureSampleTrainsExistAsync()
        {
            try
            {
                if (!await _trainRepository.HasTrainsAsync())
                {
                    var sampleTrains = new List<CreateTrainRequestModel>
                    {
                        new CreateTrainRequestModel
                        {
                            TrainNumber = "TR101",
                            Name = "Express Voyager",
                            EconomicCapacity = 120,
                            BusinessCapacity = 30,
                            FirstClassCapacity = 20,
                            Description = "High-speed passenger train with modern amenities"
                        },
                        new CreateTrainRequestModel
                        {
                            TrainNumber = "TR102",
                            Name = "Metro Cruiser",
                            EconomicCapacity = 150,
                            BusinessCapacity = 40,
                            FirstClassCapacity = 25,
                            Description = "Comfortable train for mid-range journeys"
                        },
                        new CreateTrainRequestModel
                        {
                            TrainNumber = "TR103",
                            Name = "Royal Transit",
                            EconomicCapacity = 100,
                            BusinessCapacity = 50,
                            FirstClassCapacity = 30,
                            Description = "Luxury train with premium services and facilities"
                        }
                    };

                    int successCount = 0;
                    foreach (var sampleTrain in sampleTrains)
                    {
                        var result = await CreateTrainAsync(sampleTrain);
                        if (result.Success)
                        {
                            successCount++;
                        }
                    }

                    if (successCount == sampleTrains.Count)
                    {
                        return ResponseWrapper.SuccessResponse($"Successfully initialized {successCount} sample trains.");
                    }
                    else if (successCount > 0)
                    {
                        return ResponseWrapper.SuccessResponse($"Partially initialized {successCount} out of {sampleTrains.Count} sample trains.");
                    }
                    else
                    {
                        return ResponseWrapper.ErrorResponse("Failed to initialize sample trains.");
                    }
                }

                return ResponseWrapper.SuccessResponse("Sample trains already exist, no initialization needed.");
            }
            catch (Exception ex)
            {
                return ResponseWrapper.ErrorResponse($"Failed to initialize sample trains: {ex.Message}");
            }
        }

        private TrainResponseModel MapToResponse(Train train)
        {
            return new TrainResponseModel
            {
                Id = train.Id,
                TrainNumber = train.TrainNumber,
                Name = train.Name,
                EconomyCapacity = train.EconomyCapacity,
                BusinessCapacity = train.BusinessCapacity,
                FirstClassCapacity = train.FirstClassCapacity,
                Description = train.Description
            };
        }
    }
}