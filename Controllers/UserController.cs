using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TrainBookingAppMVC.DTOs.RequestModel;
using TrainBookingAppMVC.DTOs.ResponseModel;
using TrainBookingAppMVC.DTOs.Wrapper;
using TrainBookingAppMVC.Services.Interface;

namespace TrainBookingAppMVC.Controllers
{
    [Authorize(Roles = "Regular")]
    public class UserController : Controller
    {
        private readonly ITrainService _trainService;
        private readonly ITripService _tripService;
        private readonly IBookingService _bookingService;

        public UserController(ITrainService trainService, ITripService tripService, IBookingService bookingService)
        {
            _trainService = trainService;
            _tripService = tripService;
            _bookingService = bookingService;
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
            ViewBag.FullName = User.Identity.Name;

            if (tripId == Guid.Empty)
            {
                TempData["ErrorMessage"] = "Invalid trip ID.";
                return RedirectToAction("Index");
            }

            try
            {
                var tripResult = await _tripService.GetTripByIdAsync(tripId);
                if (!tripResult.Success)
                {
                    TempData["ErrorMessage"] = tripResult.Message;
                    return RedirectToAction("Index");
                }

                var model = new CreateBookingRequestModel
                {
                    TripId = tripId,
                    UserId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value)
                };

                ViewBag.Trip = tripResult.Data;
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
            ViewBag.FullName = User.Identity.Name;

            ResponseWrapper<TripDto> tripResult = null;

            try
            {
                if (!ModelState.IsValid)
                {
                    tripResult = await _tripService.GetTripByIdAsync(request.TripId);
                    ViewBag.Trip = tripResult.Success ? tripResult.Data : null;
                    return View(request);
                }

                tripResult = await _tripService.GetTripByIdAsync(request.TripId);
                if (!tripResult.Success)
                {
                    TempData["ErrorMessage"] = tripResult.Message;
                    ViewBag.Trip = null;
                    return View(request);
                }

                var result = await _bookingService.CreateBookingAsync(request);
                if (result.Success)
                {
                    TempData["SuccessMessage"] = result.Data.Message;
                    return RedirectToAction("ViewBookings");
                }

                TempData["ErrorMessage"] = result.Message;
                ViewBag.Trip = tripResult.Success ? tripResult.Data : null;
                return View(request);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error creating booking: {ex.Message}";
                if (tripResult == null)
                {
                    tripResult = await _tripService.GetTripByIdAsync(request.TripId);
                }
                ViewBag.Trip = tripResult.Success ? tripResult.Data : null;
                return View(request);
            }
        }

        public async Task<IActionResult> ViewBookings()
        {
            ViewBag.IsAuthenticated = User.Identity.IsAuthenticated;
            ViewBag.FullName = User.Identity.Name;

            try
            {
                var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
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
            ViewBag.FullName = User.Identity.Name;

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

                return View(result.Data); // Fixed: Corrected from 'view (result.Data);' to 'return View(result.Data);'
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
            ViewBag.FullName = User.Identity.Name;

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

        
        #endregion
    }
}