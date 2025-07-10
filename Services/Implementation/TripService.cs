using TrainBookinAppMVC.Models;
using TrainBookingAppMVC.DTOs.RequestModel;
using TrainBookingAppMVC.DTOs.ResponseModel;
using TrainBookingAppMVC.DTOs.Wrapper;
using TrainBookingAppMVC.Models;
using TrainBookingAppMVC.Models.Enum;
using TrainBookingAppMVC.Repository.Interfaces;
using TrainBookingAppMVC.Services.Interface;

namespace TrainBookingAppMVC.Services.Implementation
{
    public class TripService : ITripService
    {
        private readonly ITripRepository _tripRepository;
        private readonly ITrainRepository _trainRepository;

        public TripService(ITripRepository tripRepository, ITrainRepository trainRepository)
        {
            _tripRepository = tripRepository;
            _trainRepository = trainRepository;
        }

        public async Task<ResponseWrapper<IEnumerable<TripDto>>> GetAllTripsAsync()
        {
            try
            {
                var trips = await _tripRepository.GetAllTripsAsync();
                var tripDtos = trips.Select(MapToDto);
                return ResponseWrapper<IEnumerable<TripDto>>.SuccessResponse(tripDtos, "All trips retrieved successfully");
            }
            catch (Exception ex)
            {
                return ResponseWrapper<IEnumerable<TripDto>>.ErrorResponse($"Error retrieving trips: {ex.Message}");
            }
        }

        public async Task<ResponseWrapper<IEnumerable<TripDto>>> GetAvailableTripsAsync()
        {
            try
            {
                var trips = await _tripRepository.GetAvailableTripsAsync();
                var tripDtos = trips.Select(MapToDto);
                return ResponseWrapper<IEnumerable<TripDto>>.SuccessResponse(tripDtos, "Available trips retrieved successfully");
            }
            catch (Exception ex)
            {
                return ResponseWrapper<IEnumerable<TripDto>>.ErrorResponse($"Error retrieving available trips: {ex.Message}");
            }
        }

        public async Task<ResponseWrapper<TripDto>> GetTripByIdAsync(Guid id)
        {
            try
            {
                var trip = await _tripRepository.GetTripByIdAsync(id);
                if (trip == null)
                {
                    return ResponseWrapper<TripDto>.ErrorResponse("Trip not found");
                }

                var tripDto = MapToDto(trip);
                return ResponseWrapper<TripDto>.SuccessResponse(tripDto, "Trip retrieved successfully");
            }
            catch (Exception ex)
            {
                return ResponseWrapper<TripDto>.ErrorResponse($"Error retrieving trip: {ex.Message}");
            }
        }

        public async Task<ResponseWrapper<TripDto>> CreateTripAsync(CreateTripDto createTripDto)
        {
            try
            {
                if (createTripDto.Source.ToLower() == createTripDto.Destination.ToLower())
                {
                    return ResponseWrapper<TripDto>.ErrorResponse("Source and Destination cannot be the same");
                }
                // Validate departure time
                if (createTripDto.DepartureTime <= DateTime.UtcNow)
                {
                    return ResponseWrapper<TripDto>.ErrorResponse("Departure time must be in the future");
                }

                // Validate train exists
                var train = await _trainRepository.GetByIdAsync(createTripDto.TrainId);
                if (train == null)
                {
                    return ResponseWrapper<TripDto>.ErrorResponse("Train not found");
                }

                // Validate pricing data
                if (!createTripDto.Pricings.Any())
                {
                    return ResponseWrapper<TripDto>.ErrorResponse("At least one pricing must be provided");
                }

                var trip = new Trip
                {
                    Id = Guid.NewGuid(),
                    TrainId = createTripDto.TrainId,
                    Source = createTripDto.Source,
                    Destination = createTripDto.Destination,
                    DepartureTime = createTripDto.DepartureTime,
                    CreatedAt = DateTime.UtcNow
                };

                // Create pricing entries
                foreach (var pricingDto in createTripDto.Pricings)
                {
                    var totalSeats = GetTotalSeatsByClass(train, Enum.Parse<TicketClass>(pricingDto.TicketClass));
                    if (totalSeats == 0)
                    {
                        return ResponseWrapper<TripDto>.ErrorResponse($"Train does not have {pricingDto.TicketClass} class seats");
                    }

                    trip.TripPricings.Add(new TripPricing
                    {
                        Id = Guid.NewGuid(),
                        TripId = trip.Id,
                        TicketClass = Enum.Parse<TicketClass>(pricingDto.TicketClass),
                        Price = pricingDto.Price,
                        TotalSeats = totalSeats,
                        AvailableSeats = totalSeats
                    });
                }

                var createdTrip = await _tripRepository.CreateTripAsync(trip);
                var tripDto = MapToDto(createdTrip);
                return ResponseWrapper<TripDto>.SuccessResponse(tripDto, "Trip created successfully");
            }
            catch (Exception ex)
            {
                return ResponseWrapper<TripDto>.ErrorResponse($"Error creating trip: {ex.Message}");
            }
        }

        public async Task<ResponseWrapper<TripDto>> UpdateTripAsync(Guid id, UpdateTripDto updateTripDto)
        {
            try
            {
                var existingTrip = await _tripRepository.GetTripWithDetailsAsync(id);
                if (existingTrip == null)
                {
                    return ResponseWrapper<TripDto>.ErrorResponse("Trip not found");
                }

                // Check if trip has active bookings
                if (await _tripRepository.HasActiveBookingsAsync(id))
                {
                    return ResponseWrapper<TripDto>.ErrorResponse("Cannot update trip with active bookings");
                }

                // Validate departure time
                if (updateTripDto.DepartureTime <= DateTime.UtcNow)
                {
                    return ResponseWrapper<TripDto>.ErrorResponse("Departure time must be in the future");
                }

                // Validate train exists
                var train = await _trainRepository.GetByIdAsync(updateTripDto.TrainId);
                if (train == null)
                {
                    return ResponseWrapper<TripDto>.ErrorResponse("Train not found");
                }

                // Update trip properties
                existingTrip.TrainId = updateTripDto.TrainId;
                existingTrip.Source = updateTripDto.Source;
                existingTrip.Destination = updateTripDto.Destination;
                existingTrip.DepartureTime = updateTripDto.DepartureTime;

                // Update pricing
                existingTrip.TripPricings.Clear();
                foreach (var pricingDto in updateTripDto.Pricings)
                {
                    var totalSeats = GetTotalSeatsByClass(train, pricingDto.TicketClass);
                    if (totalSeats == 0)
                    {
                        return ResponseWrapper<TripDto>.ErrorResponse($"Train does not have {pricingDto.TicketClass} class seats");
                    }

                    existingTrip.TripPricings.Add(new TripPricing
                    {
                        Id = pricingDto.Id ?? Guid.NewGuid(),
                        TripId = existingTrip.Id,
                        TicketClass = pricingDto.TicketClass,
                        Price = pricingDto.Price,
                        TotalSeats = totalSeats,
                        AvailableSeats = totalSeats
                    });
                }

                var updatedTrip = await _tripRepository.UpdateTripAsync(existingTrip);
                var tripDto = MapToDto(updatedTrip);
                return ResponseWrapper<TripDto>.SuccessResponse(tripDto, "Trip updated successfully");
            }
            catch (Exception ex)
            {
                return ResponseWrapper<TripDto>.ErrorResponse($"Error updating trip: {ex.Message}");
            }
        }

        public async Task<ResponseWrapper> DeleteTripAsync(Guid id)
        {
            try
            {
                // Check if trip exists
                var tripExists = await _tripRepository.TripExistsAsync(id);
                if (!tripExists)
                {
                    return ResponseWrapper.ErrorResponse("Trip not found");
                }

                // Check if trip has active bookings
                if (await _tripRepository.HasActiveBookingsAsync(id))
                {
                    return ResponseWrapper.ErrorResponse("Cannot delete trip with active bookings");
                }

                var result = await _tripRepository.DeleteTripAsync(id);
                if (result)
                {
                    return ResponseWrapper.SuccessResponse("Trip deleted successfully");
                }
                else
                {
                    return ResponseWrapper.ErrorResponse("Failed to delete trip");
                }
            }
            catch (Exception ex)
            {
                return ResponseWrapper.ErrorResponse($"Error deleting trip: {ex.Message}");
            }
        }

        private static TripDto MapToDto(Trip trip)
        {
            return new TripDto
            {
                Id = trip.Id,
                TrainId = trip.TrainId,
                TrainNumber = trip.Train?.TrainNumber ?? string.Empty,
                TrainName = trip.Train?.Name ?? string.Empty,
                Source = trip.Source,
                Destination = trip.Destination,
                DepartureTime = trip.DepartureTime,
                IsExpired = trip.IsExpired,
                Pricings = trip.TripPricings.Select(tp => new TripPricingDto
                {
                    Id = tp.Id,
                    TicketClass = tp.TicketClass.ToString(),
                    Price = tp.Price,
                    AvailableSeats = tp.AvailableSeats,
                    TotalSeats = tp.TotalSeats
                }).ToList()
            };
        }

        private static int GetTotalSeatsByClass(Train train, TicketClass ticketClass)
        {
            return ticketClass switch
            {
                TicketClass.Economy => train.EconomyCapacity,
                TicketClass.Business => train.BusinessCapacity,
                TicketClass.FirstClass => train.FirstClassCapacity,
                _ => 0
            };
        }
    }
}