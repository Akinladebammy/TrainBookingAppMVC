using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TrainBookingAppMVC.DTOs.RequestModel;
using TrainBookingAppMVC.DTOs.ResponseModel;
using TrainBookingAppMVC.DTOs.Wrapper;
using TrainBookingAppMVC.Services;
using TrainBookingAppMVC.Services.Interface;
using System.Text.Json;

namespace TrainBookingAppMVC.Controllers
{
    [Authorize(Roles = "Regular")]
    public class UserController : Controller
    {
        private readonly ITrainService _trainService;
        private readonly ITripService _tripService;
        private readonly IBookingService _bookingService;
        private readonly IUserService _userService;
        private readonly PaystackService _paystackService;

        public UserController(ITrainService trainService, ITripService tripService, IBookingService bookingService, IUserService userService, PaystackService paystackService)
        {
            _trainService = trainService;
            _tripService = tripService;
            _bookingService = bookingService;
            _userService = userService;
            _paystackService = paystackService;
        }

        #region Dashboard
        public async Task<IActionResult> Index()
        {
            try
            {
                ViewBag.IsAuthenticated = User.Identity.IsAuthenticated;
                ViewBag.FullName = User.FindFirst(ClaimTypes.GivenName)?.Value;

                var trainsResult = await _trainService.GetAllTrainsAsync();
                var tripsResult = await _tripService.GetAvailableTripsAsync();

                if (!trainsResult.Success)
                {
                    TempData["ErrorMessage"] = trainsResult.Message;
                }

                if (!tripsResult.Success)
                {
                    TempData["ErrorMessage"] = tripsResult.Message;
                }

                var model = new
                {
                    Trains = trainsResult.Success ? trainsResult.Data.Trains : new List<TrainResponseModel>(),
                    Trips = tripsResult.Success ? tripsResult.Data.ToList() : new List<TripDto>()
                };

                return View(model);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error loading dashboard: {ex.Message}";
                return View(new { Trains = new List<TrainResponseModel>(), Trips = new List<TripDto>() });
            }
        }
        #endregion

        #region Train Management
        [AllowAnonymous]
        public async Task<IActionResult> ViewTrains()
        {
            ViewBag.IsAuthenticated = User.Identity.IsAuthenticated;
            ViewBag.FullName = User.Identity.Name;

            try
            {
                var result = await _trainService.GetAllTrainsAsync();
                if (!result.Success)
                {
                    TempData["ErrorMessage"] = result.Message;
                    return RedirectToAction("Index");
                }

                return View(result.Data);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error loading trains: {ex.Message}";
                return RedirectToAction("Index");
            }
        }

        public async Task<IActionResult> ViewTrips()
        {
            ViewBag.IsAuthenticated = User.Identity.IsAuthenticated;
            ViewBag.FullName = User.Identity.Name;

            try
            {
                var result = await _tripService.GetAvailableTripsAsync();
                if (!result.Success)
                {
                    TempData["ErrorMessage"] = result.Message;
                    return View("ViewTrips", new ResponseWrapper<IEnumerable<TripDto>> { Data = new List<TripDto>() });
                }

                return View("ViewTrips", result);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error loading trips: {ex.Message}";
                return View("ViewTrips", new ResponseWrapper<IEnumerable<TripDto>> { Data = new List<TripDto>() });
            }
        }

        public async Task<IActionResult> TrainDetails(Guid id)
        {
            ViewBag.IsAuthenticated = User.Identity.IsAuthenticated;
            ViewBag.FullName = User.Identity.Name;

            if (id == Guid.Empty)
            {
                TempData["ErrorMessage"] = "Invalid train ID.";
                return RedirectToAction("Index");
            }

            try
            {
                var trainResult = await _trainService.GetTrainByIdAsync(id);
                var tripsResult = await _tripService.GetAvailableTripsAsync();

                if (!trainResult.Success)
                {
                    TempData["ErrorMessage"] = trainResult.Message;
                    return RedirectToAction("Index");
                }

                var trips = tripsResult.Success
                    ? tripsResult.Data.Where(t => t.TrainId == id && !t.IsExpired).ToList()
                    : new List<TripDto>();

                var model = new
                {
                    Train = trainResult.Data,
                    Trips = trips
                };

                return View(model);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error loading train details: {ex.Message}";
                return RedirectToAction("Index");
            }
        }
        #endregion

        #region Booking Management
        public async Task<IActionResult> BookTrip(Guid tripId)
        {
            ViewBag.IsAuthenticated = User.Identity.IsAuthenticated;
            ViewBag.FullName = User.FindFirst(ClaimTypes.GivenName)?.Value ?? User.Identity.Name;

            if (tripId == Guid.Empty)
            {
                TempData["ErrorMessage"] = "Invalid trip ID.";
                return RedirectToAction("Index");
            }

            try
            {
                // Validate UserId from claims
                var claimUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(claimUserId) || !Guid.TryParse(claimUserId, out var userId))
                {
                    TempData["ErrorMessage"] = "User authentication failed. Please log in again.";
                    return RedirectToAction("Index");
                }

                Console.WriteLine($"BookTrip GET: Authenticated UserId={userId}, User.Identity.Name={User.Identity.Name}, ClaimTypes.GivenName={User.FindFirst(ClaimTypes.GivenName)?.Value}");

                var tripResult = await _tripService.GetTripByIdAsync(tripId);
                if (!tripResult.Success)
                {
                    TempData["ErrorMessage"] = tripResult.Message;
                    return RedirectToAction("Index");
                }

                var economySeatsResult = await _bookingService.GetAvailableSeatsAsync(tripId, "Economy");
                var businessSeatsResult = await _bookingService.GetAvailableSeatsAsync(tripId, "Business");
                var firstClassSeatsResult = await _bookingService.GetAvailableSeatsAsync(tripId, "FirstClass");

                var model = new CreateBookingRequestModel
                {
                    TripId = tripId,
                    UserId = userId,
                    SeatNumbers = new List<string>()
                };

                ViewBag.Trip = tripResult.Data;
                ViewBag.EconomySeats = economySeatsResult.Success ? economySeatsResult.Data : new List<string>();
                ViewBag.BusinessSeats = businessSeatsResult.Success ? businessSeatsResult.Data : new List<string>();
                ViewBag.FirstClassSeats = firstClassSeatsResult.Success ? firstClassSeatsResult.Data : new List<string>();

                return View(model);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error loading booking form: {ex.Message}";
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BookTrip(CreateBookingRequestModel request)
        {
            ViewBag.IsAuthenticated = User.Identity.IsAuthenticated;
            ViewBag.FullName = User.FindFirst(ClaimTypes.GivenName)?.Value ?? User.Identity.Name;

            try
            {
                // Validate UserId from claims
                var claimUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(claimUserId) || !Guid.TryParse(claimUserId, out var userId))
                {
                    TempData["ErrorMessage"] = "User authentication failed. Please log in again.";
                    return RedirectToAction("Index");
                }

                Console.WriteLine($"BookTrip POST: Authenticated UserId={userId}, User.Identity.Name={User.Identity.Name}, ClaimTypes.GivenName={User.FindFirst(ClaimTypes.GivenName)?.Value}");

                // Ensure request.UserId matches authenticated user
                if (request.UserId != userId)
                {
                    Console.WriteLine($"BookTrip: Mismatch detected. Request UserId={request.UserId}, Authenticated UserId={userId}");
                    request.UserId = userId; // Force correct UserId
                }

                // Log ModelState errors
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                    Console.WriteLine($"ModelState invalid. Errors: {string.Join(", ", errors)}");
                    TempData["ErrorMessage"] = $"Validation failed: {string.Join("; ", errors)}";
                }

                Console.WriteLine($"TransactionReference received: {request.TransactionReference ?? "null"}");

                if (!ModelState.IsValid || request.SeatNumbers == null || !request.SeatNumbers.Any())
                {
                    var tripResult = await _tripService.GetTripByIdAsync(request.TripId);
                    ViewBag.Trip = tripResult.Success ? tripResult.Data : null;

                    var economySeatsResult = await _bookingService.GetAvailableSeatsAsync(request.TripId, "Economy");
                    var businessSeatsResult = await _bookingService.GetAvailableSeatsAsync(request.TripId, "Business");
                    var firstClassSeatsResult = await _bookingService.GetAvailableSeatsAsync(request.TripId, "FirstClass");

                    ViewBag.EconomySeats = economySeatsResult.Success ? economySeatsResult.Data : new List<string>();
                    ViewBag.BusinessSeats = businessSeatsResult.Success ? businessSeatsResult.Data : new List<string>();
                    ViewBag.FirstClassSeats = firstClassSeatsResult.Success ? firstClassSeatsResult.Data : new List<string>();
                    ViewBag.PaystackPublicKey = _paystackService.GetPublicKey();

                    if (request.SeatNumbers == null || !request.SeatNumbers.Any())
                    {
                        ModelState.AddModelError("SeatNumbers", "Please select at least one seat.");
                        TempData["ErrorMessage"] = "Please select at least one seat.";
                    }

                    return View(request);
                }

                // Validate seat numbers format
                var seatNumberRegex = new System.Text.RegularExpressions.Regex($@"^{request.TicketClass[0]}\d+$");
                foreach (var seatNumber in request.SeatNumbers)
                {
                    if (string.IsNullOrEmpty(seatNumber) || seatNumber.Length > 10 || !seatNumberRegex.IsMatch(seatNumber))
                    {
                        ModelState.AddModelError("SeatNumbers", $"Invalid seat number: {seatNumber}. Must be in format like '{request.TicketClass[0]}1' and up to 10 characters.");
                    }
                }

                if (!ModelState.IsValid)
                {
                    var tripResult = await _tripService.GetTripByIdAsync(request.TripId);
                    ViewBag.Trip = tripResult.Success ? tripResult.Data : null;

                    var economySeatsResult = await _bookingService.GetAvailableSeatsAsync(request.TripId, "Economy");
                    var businessSeatsResult = await _bookingService.GetAvailableSeatsAsync(request.TripId, "Business");
                    var firstClassSeatsResult = await _bookingService.GetAvailableSeatsAsync(request.TripId, "FirstClass");

                    ViewBag.EconomySeats = economySeatsResult.Success ? economySeatsResult.Data : new List<string>();
                    ViewBag.BusinessSeats = businessSeatsResult.Success ? businessSeatsResult.Data : new List<string>();
                    ViewBag.FirstClassSeats = firstClassSeatsResult.Success ? firstClassSeatsResult.Data : new List<string>();
                    ViewBag.PaystackPublicKey = _paystackService.GetPublicKey();

                    var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                    TempData["ErrorMessage"] = $"Validation failed: {string.Join("; ", errors)}";
                    return View(request);
                }

                // Get user email
                var user = await _userService.GetUserByIdAsync(request.UserId);
                if (user == null)
                {
                    TempData["ErrorMessage"] = "User not found.";
                    return RedirectToAction("Index");
                }

                // Initialize Paystack payment
                string callbackUrl = "https://localhost:7256/User/VerifyPayment";
                var paymentResult = await _paystackService.InitializePaymentAsync(
                    email: user.Data.Email,
                    amount: request.PaymentAmount,
                    callbackUrl: callbackUrl
                );

                Console.WriteLine($"Paystack response: Success={paymentResult.success}, Reference={paymentResult.reference}, AuthorizationUrl={paymentResult.authorizationUrl}");

                if (!paymentResult.success)
                {
                    TempData["ErrorMessage"] = $"Paystack payment initialization failed: {paymentResult.authorizationUrl}";
                    var tripResult = await _tripService.GetTripByIdAsync(request.TripId);
                    ViewBag.Trip = tripResult.Success ? tripResult.Data : null;

                    var economySeatsResult = await _bookingService.GetAvailableSeatsAsync(request.TripId, "Economy");
                    var businessSeatsResult = await _bookingService.GetAvailableSeatsAsync(request.TripId, "Business");
                    var firstClassSeatsResult = await _bookingService.GetAvailableSeatsAsync(request.TripId, "FirstClass");

                    ViewBag.EconomySeats = economySeatsResult.Success ? economySeatsResult.Data : new List<string>();
                    ViewBag.BusinessSeats = businessSeatsResult.Success ? businessSeatsResult.Data : new List<string>();
                    ViewBag.FirstClassSeats = firstClassSeatsResult.Success ? firstClassSeatsResult.Data : new List<string>();
                    ViewBag.PaystackPublicKey = _paystackService.GetPublicKey();

                    return View(request);
                }

                // Store booking request in TempData with unique key
                request.TransactionReference = paymentResult.reference;
                string tempDataKey = $"BookingRequest_{userId}_{paymentResult.reference}";
                TempData[tempDataKey] = JsonSerializer.Serialize(request);
                TempData.Keep(tempDataKey); // Ensure TempData persists across redirects
                Console.WriteLine($"BookTrip: Storing TempData[{tempDataKey}] for UserId={userId}, Reference={paymentResult.reference}");

                return Redirect(paymentResult.authorizationUrl);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error initiating payment: {ex.Message}";
                var tripResult = await _tripService.GetTripByIdAsync(request.TripId);
                ViewBag.Trip = tripResult.Success ? tripResult.Data : null;

                var economySeatsResult = await _bookingService.GetAvailableSeatsAsync(request.TripId, "Economy");
                var businessSeatsResult = await _bookingService.GetAvailableSeatsAsync(request.TripId, "Business");
                var firstClassSeatsResult = await _bookingService.GetAvailableSeatsAsync(request.TripId, "FirstClass");

                ViewBag.EconomySeats = economySeatsResult.Success ? economySeatsResult.Data : new List<string>();
                ViewBag.BusinessSeats = businessSeatsResult.Success ? businessSeatsResult.Data : new List<string>();
                ViewBag.FirstClassSeats = firstClassSeatsResult.Success ? firstClassSeatsResult.Data : new List<string>();
                ViewBag.PaystackPublicKey = _paystackService.GetPublicKey();

                Console.WriteLine($"Exception in BookTrip: {ex.Message}, StackTrace: {ex.StackTrace}");
                return View(request);
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetAvailableSeats(Guid tripId, string ticketClass)
        {
            var response = await _bookingService.GetAvailableSeatsAsync(tripId, ticketClass);
            if (response.Success)
            {
                return Json(new { success = true, data = response.Data });
            }

            return Json(new { success = false, message = response.Message });
        }

        public async Task<IActionResult> ViewBookings()
        {
            ViewBag.IsAuthenticated = User.Identity.IsAuthenticated;
            ViewBag.FullName = User.FindFirst(ClaimTypes.GivenName)?.Value ?? User.Identity.Name;

            try
            {
                var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                Console.WriteLine($"ViewBookings: Authenticated UserId={userId}, User.Identity.Name={User.Identity.Name}, ClaimTypes.GivenName={User.FindFirst(ClaimTypes.GivenName)?.Value}");
                var result = await _bookingService.GetBookingsByUserIdAsync(userId);
                if (!result.Success)
                {
                    TempData["ErrorMessage"] = result.Message;
                    return View(new BookingListResponseModel());
                }

                return View(result.Data);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error loading bookings: {ex.Message}";
                return View(new BookingListResponseModel());
            }
        }

        public async Task<IActionResult> BookingDetails(Guid id)
        {
            ViewBag.IsAuthenticated = User.Identity.IsAuthenticated;
            ViewBag.FullName = User.FindFirst(ClaimTypes.GivenName)?.Value ?? User.Identity.Name;

            if (id == Guid.Empty)
            {
                TempData["ErrorMessage"] = "Invalid booking ID.";
                return RedirectToAction("ViewBookings");
            }

            try
            {
                var result = await _bookingService.GetBookingByIdAsync(id);
                if (!result.Success)
                {
                    TempData["ErrorMessage"] = result.Message;
                    return RedirectToAction("ViewBookings");
                }

                return View(result.Data);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error loading booking details: {ex.Message}";
                return RedirectToAction("ViewBookings");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelBooking(Guid id)
        {
            ViewBag.IsAuthenticated = User.Identity.IsAuthenticated;
            ViewBag.FullName = User.FindFirst(ClaimTypes.GivenName)?.Value ?? User.Identity.Name;

            if (id == Guid.Empty)
            {
                TempData["ErrorMessage"] = "Invalid booking ID.";
                return RedirectToAction("ViewBookings");
            }

            try
            {
                var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                var result = await _bookingService.CancelBookingAsync(id, new CancelBookingRequestModel { UserId = userId });
                if (result.Success)
                {
                    TempData["SuccessMessage"] = result.Data;
                }
                else
                {
                    TempData["ErrorMessage"] = result.Message;
                }
                return RedirectToAction("ViewBookings");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error cancelling booking: {ex.Message}";
                return RedirectToAction("ViewBookings");
            }
        }

        [HttpGet]
        public async Task<IActionResult> VerifyPayment(string reference)
        {
            ViewBag.IsAuthenticated = User.Identity.IsAuthenticated;
            ViewBag.FullName = User.FindFirst(ClaimTypes.GivenName)?.Value ?? User.Identity.Name;

            try
            {
                // Validate UserId from claims
                var claimUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(claimUserId) || !Guid.TryParse(claimUserId, out var authenticatedUserId))
                {
                    Console.WriteLine($"VerifyPayment: No authenticated user found. User.Identity.Name={User.Identity.Name}, ClaimTypes.GivenName={User.FindFirst(ClaimTypes.GivenName)?.Value}");
                    TempData["ErrorMessage"] = "User authentication failed. Please log in again.";
                    return RedirectToAction("Index", new { area = "" });
                }

                Console.WriteLine($"VerifyPayment: Authenticated UserId={authenticatedUserId}, User.Identity.Name={User.Identity.Name}, ClaimTypes.GivenName={User.FindFirst(ClaimTypes.GivenName)?.Value}");

                if (string.IsNullOrEmpty(reference))
                {
                    TempData["ErrorMessage"] = "Invalid transaction reference.";
                    return RedirectToAction("Index", new { area = "" });
                }

                var paymentResult = await _paystackService.VerifyPaymentAsync(reference);
                Console.WriteLine($"VerifyPayment: Reference={reference}, Success={paymentResult.success}, Message={paymentResult.message}");

                if (!paymentResult.success)
                {
                    TempData["ErrorMessage"] = paymentResult.message;
                    return RedirectToAction("BookTrip", new { tripId = Guid.Empty });
                }

                // Retrieve booking request with unique TempData key
                string tempDataKey = $"BookingRequest_{authenticatedUserId}_{reference}";
                if (TempData[tempDataKey] == null)
                {
                    Console.WriteLine($"VerifyPayment: TempData[{tempDataKey}] not found.");
                    TempData["ErrorMessage"] = "Booking session expired or invalid. Please try again.";
                    return RedirectToAction("Index", new { area = "" });
                }

                var bookingRequest = JsonSerializer.Deserialize<CreateBookingRequestModel>(TempData[tempDataKey].ToString());
                Console.WriteLine($"VerifyPayment: Deserialized UserId={bookingRequest.UserId}, TransactionReference={reference}");

                // Validate UserId matches authenticated user
                if (bookingRequest.UserId != authenticatedUserId)
                {
                    Console.WriteLine($"VerifyPayment: UserId mismatch. TempData UserId={bookingRequest.UserId}, Authenticated UserId={authenticatedUserId}");
                    TempData["ErrorMessage"] = "Booking session invalid. Please try again.";
                    return RedirectToAction("Index", new { area = "" });
                }

                bookingRequest.TransactionReference = reference;
                var bookingResult = await _bookingService.CreateBookingAsync(bookingRequest);
                if (!bookingResult.Success)
                {
                    Console.WriteLine($"VerifyPayment: Booking creation failed. Message={bookingResult.Message}");
                    TempData["ErrorMessage"] = $"Failed to create booking: {bookingResult.Message}";
                    return RedirectToAction("BookTrip", new { tripId = bookingRequest.TripId });
                }

                // Clear TempData
                TempData.Remove(tempDataKey);
                Console.WriteLine($"VerifyPayment: Cleared TempData[{tempDataKey}]");

                // Redirect to PaymentConfirmation using ngrok domain
                var paymentConfirmationUrl = $"https://localhost:7256/User/PaymentConfirmation?bookingId={bookingResult.Data.BookingId}&tripId={bookingRequest.TripId}";
                Console.WriteLine($"VerifyPayment: Redirecting to {paymentConfirmationUrl}");
                return Redirect(paymentConfirmationUrl);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"VerifyPayment exception: Reference={reference}, Message={ex.Message}, StackTrace={ex.StackTrace}");
                TempData["ErrorMessage"] = $"Error verifying payment: {ex.Message}";
                return RedirectToAction("Index", new { area = "" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> PaymentConfirmation(Guid bookingId, Guid tripId)
        {
            ViewBag.IsAuthenticated = User.Identity.IsAuthenticated;
            ViewBag.FullName = User.FindFirst(ClaimTypes.GivenName)?.Value ?? User.Identity.Name;

            try
            {
                // Validate UserId from claims
                var claimUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(claimUserId) || !Guid.TryParse(claimUserId, out var authenticatedUserId))
                {
                    Console.WriteLine($"PaymentConfirmation: No authenticated user found. User.Identity.Name={User.Identity.Name}, ClaimTypes.GivenName={User.FindFirst(ClaimTypes.GivenName)?.Value}");
                    TempData["ErrorMessage"] = "User authentication failed. Please log in again.";
                    return RedirectToAction("Index");
                }

                Console.WriteLine($"PaymentConfirmation: Authenticated UserId={authenticatedUserId}, User.Identity.Name={User.Identity.Name}, ClaimTypes.GivenName={User.FindFirst(ClaimTypes.GivenName)?.Value}");

                // Get booking details
                var bookingDetailsResult = await _bookingService.GetBookingByIdAsync(bookingId);
                if (!bookingDetailsResult.Success)
                {
                    Console.WriteLine($"PaymentConfirmation: Failed to retrieve booking. BookingId={bookingId}, Message={bookingDetailsResult.Message}");
                    TempData["ErrorMessage"] = $"Failed to retrieve booking details: {bookingDetailsResult.Message}";
                    return RedirectToAction("Index");
                }

                // Verify booking belongs to authenticated user
                if (bookingDetailsResult.Data.UserId != authenticatedUserId)
                {
                    Console.WriteLine($"PaymentConfirmation: UserId mismatch. Booking UserId={bookingDetailsResult.Data.UserId}, Authenticated UserId={authenticatedUserId}");
                    TempData["ErrorMessage"] = "Invalid booking access. This booking does not belong to you.";
                    return RedirectToAction("Index");
                }

                // Get trip details
                var tripResult = await _tripService.GetTripByIdAsync(tripId);
                ViewBag.Trip = tripResult.Success ? tripResult.Data : null;

                TempData["SuccessMessage"] = "Payment successful! Your booking has been confirmed.";
                return View("PaymentConfirmation", bookingDetailsResult.Data);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"PaymentConfirmation exception: BookingId={bookingId}, Message={ex.Message}, StackTrace={ex.StackTrace}");
                TempData["ErrorMessage"] = $"Error loading payment confirmation: {ex.Message}";
                return RedirectToAction("Index");
            }
        }
        #endregion
    }
}