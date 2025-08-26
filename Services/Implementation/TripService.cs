using TrainBookingAppMVC.Models;
using TrainBookingAppMVC.DTOs.RequestModel;
using TrainBookingAppMVC.DTOs.ResponseModel;
using TrainBookingAppMVC.DTOs.Wrapper;
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
                if (!Enum.TryParse<Terminal>(createTripDto.Source, out var source) ||
                    !Enum.TryParse<Terminal>(createTripDto.Destination, out var destination))
                {
                    return ResponseWrapper<TripDto>.ErrorResponse("Invalid source or destination terminal.");
                }

                if (source == destination)
                {
                    return ResponseWrapper<TripDto>.ErrorResponse("Source and destination terminals cannot be the same.");
                }

                if (createTripDto.DepartureTime <= DateTime.UtcNow)
                {
                    return ResponseWrapper<TripDto>.ErrorResponse("Departure time must be in the future.");
                }

                var train = await _trainRepository.GetByIdAsync(createTripDto.TrainId);
                if (train == null)
                {
                    return ResponseWrapper<TripDto>.ErrorResponse("Train not found.");
                }

                // Define WAT time zone
                var watTimeZone = TimeZoneInfo.FindSystemTimeZoneById("W. Central Africa Standard Time");

                // Check for conflicting departure times within 4 hours for the same train
                var existingTrips = await _tripRepository.GetTripsByTrainIdAsync(createTripDto.TrainId);
                var timeWindow = TimeSpan.FromHours(6); // Adjust to TimeSpan.FromHours(10) if you want a 10-hour window
                foreach (var existingTrip in existingTrips)
                {
                    var timeDifference = (createTripDto.DepartureTime - existingTrip.DepartureTime).Duration();
                    if (timeDifference <= timeWindow)
                    {
                        // Convert existingTrip.DepartureTime from UTC to WAT for display
                        var departureTimeInWAT = TimeZoneInfo.ConvertTimeFromUtc(existingTrip.DepartureTime, watTimeZone);
                        return ResponseWrapper<TripDto>.ErrorResponse(
                            $"Cannot create trip. Another trip for this train is scheduled at {departureTimeInWAT.ToString("g")} (WAT), " +
                            "which is within 6 hours of the proposed departure time."
                        );
                    }
                }

                if (!createTripDto.Pricings.Any())
                {
                    return ResponseWrapper<TripDto>.ErrorResponse("At least one pricing must be provided.");
                }

                var trip = new Trip
                {
                    Id = Guid.NewGuid(),
                    TrainId = createTripDto.TrainId,
                    Source = source,
                    Destination = destination,
                    DepartureTime = createTripDto.DepartureTime,
                    CreatedAt = DateTime.UtcNow
                };

                foreach (var pricingDto in createTripDto.Pricings)
                {
                    if (!Enum.TryParse<TicketClass>(pricingDto.TicketClass, out var ticketClass))
                    {
                        return ResponseWrapper<TripDto>.ErrorResponse($"Invalid ticket class: {pricingDto.TicketClass}.");
                    }

                    var totalSeats = GetTotalSeatsByClass(train, ticketClass);
                    if (totalSeats == 0)
                    {
                        return ResponseWrapper<TripDto>.ErrorResponse($"Train does not have {pricingDto.TicketClass} class seats.");
                    }

                    trip.TripPricings.Add(new TripPricing
                    {
                        Id = Guid.NewGuid(),
                        TripId = trip.Id,
                        TicketClass = ticketClass,
                        Price = pricingDto.Price,
                        TotalSeats = totalSeats,
                        AvailableSeats = totalSeats
                    });
                }

                var createdTrip = await _tripRepository.CreateTripAsync(trip);
                var tripDto = MapToDto(createdTrip);
                return ResponseWrapper<TripDto>.SuccessResponse(tripDto, "Trip created successfully.");
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
                    return ResponseWrapper<TripDto>.ErrorResponse("Trip not found.");
                }

                if (await _tripRepository.HasActiveBookingsAsync(id))
                {
                    return ResponseWrapper<TripDto>.ErrorResponse("Cannot update trip with active bookings.");
                }

                if (!Enum.TryParse<Terminal>(updateTripDto.Source, out var source) ||
                    !Enum.TryParse<Terminal>(updateTripDto.Destination, out var destination))
                {
                    return ResponseWrapper<TripDto>.ErrorResponse("Invalid source or destination terminal.");
                }

                if (source == destination)
                {
                    return ResponseWrapper<TripDto>.ErrorResponse("Source and destination terminals cannot be the same.");
                }

                if (updateTripDto.DepartureTime <= DateTime.UtcNow)
                {
                    return ResponseWrapper<TripDto>.ErrorResponse("Departure time must be in the future.");
                }

                var train = await _trainRepository.GetByIdAsync(updateTripDto.TrainId);
                if (train == null)
                {
                    return ResponseWrapper<TripDto>.ErrorResponse("Train not found.");
                }

                existingTrip.TrainId = updateTripDto.TrainId;
                existingTrip.Source = source;
                existingTrip.Destination = destination;
                existingTrip.DepartureTime = updateTripDto.DepartureTime;

                existingTrip.TripPricings.Clear();
                foreach (var pricingDto in updateTripDto.Pricings)
                {
                    if (!Enum.TryParse<TicketClass>(pricingDto.TicketClass, out var ticketClass))
                    {
                        return ResponseWrapper<TripDto>.ErrorResponse($"Invalid ticket class: {pricingDto.TicketClass}.");
                    }

                    var totalSeats = GetTotalSeatsByClass(train, ticketClass);
                    if (totalSeats == 0)
                    {
                        return ResponseWrapper<TripDto>.ErrorResponse($"Train does not have {pricingDto.TicketClass} class seats.");
                    }

                    existingTrip.TripPricings.Add(new TripPricing
                    {
                        Id = pricingDto.Id ?? Guid.NewGuid(),
                        TripId = existingTrip.Id,
                        TicketClass = ticketClass,
                        Price = pricingDto.Price,
                        TotalSeats = totalSeats,
                        AvailableSeats = totalSeats
                    });
                }

                var updatedTrip = await _tripRepository.UpdateTripAsync(existingTrip);
                var tripDto = MapToDto(updatedTrip);
                return ResponseWrapper<TripDto>.SuccessResponse(tripDto, "Trip updated successfully.");
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
                if (!await _tripRepository.TripExistsAsync(id))
                {
                    return ResponseWrapper.ErrorResponse("Trip not found.");
                }

                if (await _tripRepository.HasActiveBookingsAsync(id))
                {
                    return ResponseWrapper.ErrorResponse("Cannot delete trip with active bookings.");
                }

                var result = await _tripRepository.DeleteTripAsync(id);
                return result
                    ? ResponseWrapper.SuccessResponse("Trip deleted successfully.")
                    : ResponseWrapper.ErrorResponse("Failed to delete trip.");
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
                Source = trip.Source.ToString(),
                Destination = trip.Destination.ToString(),
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