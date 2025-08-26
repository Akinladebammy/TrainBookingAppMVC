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
    public class BookingService : IBookingService
    {
        private readonly IBookingRepository _bookingRepository;
        private readonly ITripRepository _tripRepository;
        private readonly IUserRepository _userRepository;

        public BookingService(IBookingRepository bookingRepository, ITripRepository tripRepository, IUserRepository userRepository)
        {
            _bookingRepository = bookingRepository;
            _tripRepository = tripRepository;
            _userRepository = userRepository;
        }

        public async Task<ResponseWrapper<BookingListResponseModel>> GetAllBookingsAsync()
        {
            try
            {
                var bookings = await _bookingRepository.GetAllBookingsAsync();

                var bookingListResponse = new BookingListResponseModel
                {
                    Bookings = bookings.Select(MapToResponseModel).ToList(),
                    TotalCount = bookings.Count()
                };

                return ResponseWrapper<BookingListResponseModel>.SuccessResponse(
                    bookingListResponse,
                    "All bookings retrieved successfully."
                );
            }
            catch (Exception ex)
            {
                return ResponseWrapper<BookingListResponseModel>.ErrorResponse($"Failed to retrieve bookings: {ex.Message}");
            }
        }

        public async Task<ResponseWrapper<BookingListResponseModel>> GetBookingsByUserIdAsync(Guid userId)
        {
            try
            {
                if (userId == Guid.Empty)
                {
                    return ResponseWrapper<BookingListResponseModel>.ErrorResponse("User ID cannot be empty.");
                }

                var user = await _userRepository.GetUserByIdAsync(userId);
                if (user == null)
                {
                    return ResponseWrapper<BookingListResponseModel>.ErrorResponse("User not found.");
                }

                var bookings = await _bookingRepository.GetBookingsByUserIdAsync(userId);

                var bookingListResponse = new BookingListResponseModel
                {
                    Bookings = bookings.Select(MapToResponseModel).ToList(),
                    TotalCount = bookings.Count()
                };

                return ResponseWrapper<BookingListResponseModel>.SuccessResponse(
                    bookingListResponse,
                    bookings.Any() ? "User bookings retrieved successfully." : "No bookings found for this user."
                );
            }
            catch (Exception ex)
            {
                return ResponseWrapper<BookingListResponseModel>.ErrorResponse($"Failed to retrieve user bookings: {ex.Message}");
            }
        }

        public async Task<ResponseWrapper<BookingResponseModel>> GetBookingByIdAsync(Guid id)
        {
            try
            {
                if (id == Guid.Empty)
                {
                    return ResponseWrapper<BookingResponseModel>.ErrorResponse("Booking ID cannot be empty.");
                }

                var booking = await _bookingRepository.GetBookingByIdAsync(id);
                if (booking == null)
                {
                    return ResponseWrapper<BookingResponseModel>.ErrorResponse("Booking not found.");
                }

                return ResponseWrapper<BookingResponseModel>.SuccessResponse(
                    MapToResponseModel(booking),
                    "Booking retrieved successfully."
                );
            }
            catch (Exception ex)
            {
                return ResponseWrapper<BookingResponseModel>.ErrorResponse($"Failed to retrieve booking: {ex.Message}");
            }
        }

        public async Task<ResponseWrapper<List<string>>> GetAvailableSeatsAsync(Guid tripId, string ticketClass)
        {
            try
            {
                if (tripId == Guid.Empty)
                {
                    return ResponseWrapper<List<string>>.ErrorResponse("Trip ID cannot be empty.");
                }

                if (!Enum.TryParse<TicketClass>(ticketClass, out var ticketClassEnum))
                {
                    return ResponseWrapper<List<string>>.ErrorResponse("Invalid ticket class.");
                }

                var availableSeats = await _bookingRepository.GetAvailableSeatsAsync(tripId, ticketClassEnum);
                return ResponseWrapper<List<string>>.SuccessResponse(
                    availableSeats,
                    availableSeats.Any() ? "Available seats retrieved successfully." : "No seats available for this trip and class."
                );
            }
            catch (Exception ex)
            {
                return ResponseWrapper<List<string>>.ErrorResponse($"Failed to retrieve available seats: {ex.Message}");
            }
        }

        public async Task<ResponseWrapper<BookingCreationResponseModel>> CreateBookingAsync(CreateBookingRequestModel request)
        {
            try
            {
                var user = await _userRepository.GetUserByIdAsync(request.UserId);
                if (user == null)
                {
                    return ResponseWrapper<BookingCreationResponseModel>.ErrorResponse("User not found.");
                }

                var trip = await _tripRepository.GetTripWithDetailsAsync(request.TripId);
                if (trip == null)
                {
                    return ResponseWrapper<BookingCreationResponseModel>.ErrorResponse("Trip not found.");
                }

                if (trip.IsExpired || trip.DepartureTime <= DateTime.UtcNow)
                {
                    return ResponseWrapper<BookingCreationResponseModel>.ErrorResponse("Sorry, this trip has expired and is no longer available for booking.");
                }

                if (!Enum.TryParse<TicketClass>(request.TicketClass, out var ticketClass))
                {
                    return ResponseWrapper<BookingCreationResponseModel>.ErrorResponse("Invalid ticket class.");
                }

                var pricing = trip.TripPricings.FirstOrDefault(p => p.TicketClass == ticketClass);
                if (pricing == null)
                {
                    return ResponseWrapper<BookingCreationResponseModel>.ErrorResponse($"No pricing found for {request.TicketClass} class on this trip.");
                }

                if (!request.SeatNumbers.Any())
                {
                    return ResponseWrapper<BookingCreationResponseModel>.ErrorResponse("At least one seat must be selected.");
                }

                if (pricing.AvailableSeats < request.SeatNumbers.Count)
                {
                    return ResponseWrapper<BookingCreationResponseModel>.ErrorResponse($"Not enough seats available. Only {pricing.AvailableSeats} seats left in {request.TicketClass} class.");
                }

                var seatNumberRegex = new System.Text.RegularExpressions.Regex($@"^{request.TicketClass[0]}\d+$");
                foreach (var seatNumber in request.SeatNumbers)
                {
                    if (string.IsNullOrEmpty(seatNumber) || seatNumber.Length > 10 || !seatNumberRegex.IsMatch(seatNumber))
                    {
                        return ResponseWrapper<BookingCreationResponseModel>.ErrorResponse($"Invalid seat number: {seatNumber}. Must be in format like '{request.TicketClass[0]}1' and up to 10 characters.");
                    }
                }

                decimal totalPrice = pricing.Price * request.SeatNumbers.Count;

                if (request.PaymentAmount < totalPrice)
                {
                    return ResponseWrapper<BookingCreationResponseModel>.ErrorResponse($"Insufficient payment. Required: ₦{totalPrice:F2}, Provided: ₦{request.PaymentAmount:F2}");
                }

                request.NumberOfSeats = request.SeatNumbers.Count;

                (bool success, string message, Guid bookingId) = await _bookingRepository.ProcessBookingTransactionAsync(
                    request.TripId,
                    request.UserId,
                    request.NumberOfSeats,
                    totalPrice,
                    request.TicketClass,
                    request.SeatNumbers,
                    request.TransactionReference
                );

                if (!success)
                {
                    return ResponseWrapper<BookingCreationResponseModel>.ErrorResponse(message);
                }

                decimal change = request.PaymentAmount - totalPrice;

                var response = new BookingCreationResponseModel
                {
                    BookingId = bookingId,
                    Message = change > 0 ? $"Booking successful! Change: ₦{change:F2}" : "Booking successful!",
                    Change = change
                };

                return ResponseWrapper<BookingCreationResponseModel>.SuccessResponse(response, $"Successfully booked {request.SeatNumbers.Count} seats.");
            }
            catch (Exception ex)
            {
                return ResponseWrapper<BookingCreationResponseModel>.ErrorResponse($"Booking failed: {ex.Message}");
            }
        }

        public async Task<ResponseWrapper<string>> CancelBookingAsync(Guid bookingId, CancelBookingRequestModel request)
        {
            try
            {
                if (bookingId == Guid.Empty)
                {
                    return ResponseWrapper<string>.ErrorResponse("Booking ID cannot be empty.");
                }

                var booking = await _bookingRepository.GetBookingByIdAndUserIdAsync(bookingId, request.UserId);
                if (booking == null)
                {
                    return ResponseWrapper<string>.ErrorResponse("Booking not found or you don't have permission to cancel it.");
                }

                if (booking.IsCancelled)
                {
                    return ResponseWrapper<string>.ErrorResponse("Booking is already cancelled.");
                }

                if (booking.Trip.DepartureTime <= DateTime.UtcNow)
                {
                    return ResponseWrapper<string>.ErrorResponse("Cannot cancel booking after trip departure time.");
                }

                booking.IsCancelled = true;
                await _bookingRepository.UpdateTripAvailableSeatsAsync(booking.TripId, booking.NumberOfSeats, booking.TicketClass.ToString());

                var success = await _bookingRepository.DeleteBookingAsync(bookingId);
                if (!success)
                {
                    return ResponseWrapper<string>.ErrorResponse("Failed to cancel booking.");
                }

                return ResponseWrapper<string>.SuccessResponse($"Booking cancelled successfully. Refund amount: ₦{booking.TotalPrice:F2} will be processed.");
            }
            catch (Exception ex)
            {
                return ResponseWrapper<string>.ErrorResponse($"Cancellation failed: {ex.Message}");
            }
        }

        private static BookingResponseModel MapToResponseModel(Booking booking)
        {
            var departureTime = booking.Trip.DepartureTime;
            var isExpired = booking.Trip.IsExpired;

            string tripStatus = isExpired || departureTime <= DateTime.UtcNow ? "TRIP COMPLETED ✓" : "UPCOMING";

            return new BookingResponseModel
            {
                Id = booking.Id,
                TripId = booking.TripId,
                UserId = booking.UserId,
                TicketClass = booking.TicketClass.ToString(),
                SeatNumbers = booking.SeatNumbers,
                NumberOfSeats = booking.NumberOfSeats,
                TotalPrice = booking.TotalPrice,
                BookingDate = booking.BookingDate,
                IsCancelled = booking.IsCancelled,
                TransactionReference = booking.TransactionReference, // Add transaction reference
                TrainNumber = booking.Trip.Train?.TrainNumber ?? string.Empty,
                TrainName = booking.Trip.Train?.Name ?? string.Empty,
                Source = booking.Trip.Source.ToString(),
                Destination = booking.Trip.Destination.ToString(),
                DepartureTime = booking.Trip.DepartureTime,
                IsExpired = booking.Trip.IsExpired,
                TripStatus = tripStatus,
                Username = booking.User?.Username,
                FullName = booking.User?.FullName
            };
        }
    }


}