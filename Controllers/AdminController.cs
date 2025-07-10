using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TrainBookingAppMVC.DTOs.RequestModel;
using TrainBookingAppMVC.DTOs.ResponseModel;
using TrainBookingAppMVC.Models.Enum;
using TrainBookingAppMVC.Services.Interface;

namespace TrainBookingAppMVC.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ITrainService _trainService;
        private readonly ITripService _tripService;
        private readonly IUserService _userService;
        private readonly IBookingService _bookingService;
        private readonly TimeZoneInfo _watTimeZone;

        public AdminController(ITrainService trainService, ITripService tripService, IUserService userService, IBookingService bookingService)
        {
            _trainService = trainService;
            _tripService = tripService;
            _userService = userService;
            _bookingService = bookingService;
            _watTimeZone = TimeZoneInfo.FindSystemTimeZoneById("W. Central Africa Standard Time");
        }

        #region Dashboard
        public async Task<IActionResult> Index()
        {
            try
            {
                ViewBag.IsAuthenticated = User.Identity.IsAuthenticated;
                ViewBag.IsAdmin = User.IsInRole("Admin");
                ViewBag.FullName = User.FindFirst(ClaimTypes.GivenName)?.Value;

                var trainsResult = await _trainService.GetAllTrainsAsync();
                var tripsResult = await _tripService.GetAllTripsAsync();

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

        #region User Management
        public async Task<IActionResult> Users()
        {
            try
            {
                ViewBag.IsAuthenticated = User.Identity.IsAuthenticated;
                ViewBag.IsAdmin = User.IsInRole("Admin");
                ViewBag.FullName = User.Identity.Name;

                var usersResult = await _userService.GetAllUsersAsync();
                if (!usersResult.Success)
                {
                    TempData["ErrorMessage"] = usersResult.Message;
                    return View(new UserListResponseModel { Users = new List<UserResponseModel>() });
                }

                return View(usersResult.Data);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error loading users: {ex.Message}";
                return View(new UserListResponseModel { Users = new List<UserResponseModel>() });
            }
        }

        public async Task<IActionResult> UserBookings(Guid id)
        {
            try
            {
                ViewBag.IsAuthenticated = User.Identity.IsAuthenticated;
                ViewBag.IsAdmin = User.IsInRole("Admin");
                ViewBag.FullName = User.Identity.Name;

                var userResult = await _userService.GetUserByIdAsync(id);
                if (!userResult.Success)
                {
                    TempData["ErrorMessage"] = userResult.Message;
                    return RedirectToAction("Users");
                }

                var bookingsResult = await _bookingService.GetBookingsByUserIdAsync(id);
                if (!bookingsResult.Success)
                {
                    TempData["ErrorMessage"] = bookingsResult.Message;
                }

                var model = new
                {
                    User = userResult.Data,
                    Bookings = bookingsResult.Success ? bookingsResult.Data.Bookings : new List<BookingResponseModel>()
                };

                return View(model);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error loading user bookings: {ex.Message}";
                return RedirectToAction("Users");
            }
        }
        #endregion

        #region Train Management
        public IActionResult CreateTrain()
        {
            ViewBag.IsAuthenticated = User.Identity.IsAuthenticated;
            ViewBag.IsAdmin = User.IsInRole("Admin");
            ViewBag.FullName = User.Identity.Name;
            return View(new CreateTrainRequestModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateTrain(CreateTrainRequestModel request)
        {
            ViewBag.IsAuthenticated = User.Identity.IsAuthenticated;
            ViewBag.IsAdmin = User.IsInRole("Admin");
            ViewBag.FullName = User.Identity.Name;

            if (!ModelState.IsValid)
            {
                return View(request);
            }

            try
            {
                var result = await _trainService.CreateTrainAsync(request);
                if (result.Success)
                {
                    TempData["SuccessMessage"] = "Train created successfully!";
                    return RedirectToAction("Index");
                }

                TempData["ErrorMessage"] = result.Message;
                return View(request);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error creating train: {ex.Message}";
                return View(request);
            }
        }

        public async Task<IActionResult> EditTrain(Guid id)
        {
            ViewBag.IsAuthenticated = User.Identity.IsAuthenticated;
            ViewBag.IsAdmin = User.IsInRole("Admin");
            ViewBag.FullName = User.Identity.Name;

            if (id == Guid.Empty)
            {
                TempData["ErrorMessage"] = "Invalid train ID.";
                return RedirectToAction("Index");
            }

            try
            {
                var result = await _trainService.GetTrainByIdAsync(id);
                if (!result.Success)
                {
                    TempData["ErrorMessage"] = result.Message;
                    return RedirectToAction("Index");
                }

                var updateModel = new UpdateTrainRequestModel
                {
                    TrainNumber = result.Data.TrainNumber,
                    Name = result.Data.Name,
                    EconomicCapacity = result.Data.EconomyCapacity,
                    BusinessCapacity = result.Data.BusinessCapacity,
                    FirstClassCapacity = result.Data.FirstClassCapacity,
                    Description = result.Data.Description
                };

                return View(updateModel);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error loading train: {ex.Message}";
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditTrain(Guid id, UpdateTrainRequestModel request)
        {
            ViewBag.IsAuthenticated = User.Identity.IsAuthenticated;
            ViewBag.IsAdmin = User.IsInRole("Admin");
            ViewBag.FullName = User.Identity.Name;

            if (id == Guid.Empty)
            {
                TempData["ErrorMessage"] = "Invalid train ID.";
                return View(request);
            }

            if (!ModelState.IsValid)
            {
                return View(request);
            }

            try
            {
                var result = await _trainService.UpdateTrainAsync(id, request);
                if (result.Success)
                {
                    TempData["SuccessMessage"] = "Train updated successfully!";
                    return RedirectToAction("Index");
                }

                TempData["ErrorMessage"] = result.Message;
                return View(request);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error updating train: {ex.Message}";
                return View(request);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteTrain(Guid id)
        {
            ViewBag.IsAuthenticated = User.Identity.IsAuthenticated;
            ViewBag.IsAdmin = User.IsInRole("Admin");
            ViewBag.FullName = User.Identity.Name;

            if (id == Guid.Empty)
            {
                TempData["ErrorMessage"] = "Invalid train ID.";
                return RedirectToAction("Index");
            }

            try
            {
                var result = await _trainService.DeleteTrainAsync(id);
                if (result.Success)
                {
                    TempData["SuccessMessage"] = "Train deleted successfully!";
                }
                else
                {
                    TempData["ErrorMessage"] = result.Message;
                }
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error deleting train: {ex.Message}";
                return RedirectToAction("Index");
            }
        }

        [HttpGet]
        public IActionResult InitializeSampleTrains()
        {
            ViewBag.IsAuthenticated = User.Identity.IsAuthenticated;
            ViewBag.IsAdmin = User.IsInRole("Admin");
            ViewBag.FullName = User.Identity.Name;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> InitializeSampleTrainsPost()
        {
            ViewBag.IsAuthenticated = User.Identity.IsAuthenticated;
            ViewBag.IsAdmin = User.IsInRole("Admin");
            ViewBag.FullName = User.Identity.Name;

            try
            {
                var result = await _trainService.EnsureSampleTrainsExistAsync();
                if (result.Success)
                {
                    TempData["SuccessMessage"] = result.Message;
                }
                else
                {
                    TempData["ErrorMessage"] = result.Message;
                }
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error initializing sample trains: {ex.Message}";
                return RedirectToAction("Index");
            }
        }

        public async Task<IActionResult> TrainDetails(Guid id)
        {
            ViewBag.IsAuthenticated = User.Identity.IsAuthenticated;
            ViewBag.IsAdmin = User.IsInRole("Admin");
            ViewBag.FullName = User.Identity.Name;

            if (id == Guid.Empty)
            {
                TempData["ErrorMessage"] = "Invalid train ID.";
                return RedirectToAction("Index");
            }

            try
            {
                var trainResult = await _trainService.GetTrainByIdAsync(id);
                if (!trainResult.Success)
                {
                    TempData["ErrorMessage"] = trainResult.Message;
                    return RedirectToAction("Index");
                }

                var tripsResult = await _tripService.GetAllTripsAsync();
                var trips = tripsResult.Success
                    ? tripsResult.Data.Where(t => t.TrainId == id).ToList()
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

        #region Trip Management
        public async Task<IActionResult> CreateTrip()
        {
            ViewBag.IsAuthenticated = User.Identity.IsAuthenticated;
            ViewBag.IsAdmin = User.IsInRole("Admin");
            ViewBag.FullName = User.Identity.Name;

            try
            {
                var trainsResult = await _trainService.GetAllTrainsAsync();
                ViewBag.Trains = trainsResult.Success ? trainsResult.Data.Trains : new List<TrainResponseModel>();
                return View(new CreateTripDto());
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error loading create trip form: {ex.Message}";
                return View(new CreateTripDto());
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateTrip(CreateTripDto createTripDto)
        {
            ViewBag.IsAuthenticated = User.Identity.IsAuthenticated;
            ViewBag.IsAdmin = User.IsInRole("Admin");
            ViewBag.FullName = User.Identity.Name;

            if (!ModelState.IsValid)
            {
                var trainsResult = await _trainService.GetAllTrainsAsync();
                ViewBag.Trains = trainsResult.Success ? trainsResult.Data.Trains : new List<TrainResponseModel>();
                return View(createTripDto);
            }

            try
            {
                // Convert DepartureTime from WAT to UTC
                if (createTripDto.DepartureTime.Kind != DateTimeKind.Utc)
                {
                    createTripDto.DepartureTime = TimeZoneInfo.ConvertTimeToUtc(createTripDto.DepartureTime, _watTimeZone);
                }

                var result = await _tripService.CreateTripAsync(createTripDto);
                if (result.Success)
                {
                    TempData["SuccessMessage"] = "Trip created successfully!";
                    return RedirectToAction("Index");
                }

                TempData["ErrorMessage"] = result.Message;
                var trainsResult = await _trainService.GetAllTrainsAsync();
                ViewBag.Trains = trainsResult.Success ? trainsResult.Data.Trains : new List<TrainResponseModel>();
                return View(createTripDto);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error creating trip: {ex.Message}";
                var trainsResult = await _trainService.GetAllTrainsAsync();
                ViewBag.Trains = trainsResult.Success ? trainsResult.Data.Trains : new List<TrainResponseModel>();
                return View(createTripDto);
            }
        }

        public async Task<IActionResult> EditTrip(Guid id)
        {
            ViewBag.IsAuthenticated = User.Identity.IsAuthenticated;
            ViewBag.IsAdmin = User.IsInRole("Admin");
            ViewBag.FullName = User.Identity.Name;

            if (id == Guid.Empty)
            {
                TempData["ErrorMessage"] = "Invalid trip ID.";
                return RedirectToAction("Index");
            }

            try
            {
                var result = await _tripService.GetTripByIdAsync(id);
                if (!result.Success)
                {
                    TempData["ErrorMessage"] = result.Message;
                    return RedirectToAction("Index");
                }

                var updateModel = new UpdateTripDto
                {
                    TrainId = result.Data.TrainId,
                    Source = result.Data.Source,
                    Destination = result.Data.Destination,
                    DepartureTime = result.Data.DepartureTime,
                    Pricings = result.Data.Pricings.Select(p => new UpdateTripPricingDto
                    {
                        Id = p.Id,
                        TicketClass = Enum.Parse<TicketClass>(p.TicketClass),
                        Price = p.Price
                    }).ToList()
                };

                var trainsResult = await _trainService.GetAllTrainsAsync();
                ViewBag.Trains = trainsResult.Success ? trainsResult.Data.Trains : new List<TrainResponseModel>();
                return View(updateModel);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error loading trip: {ex.Message}";
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditTrip(Guid id, UpdateTripDto updateTripDto)
        {
            ViewBag.IsAuthenticated = User.Identity.IsAuthenticated;
            ViewBag.IsAdmin = User.IsInRole("Admin");
            ViewBag.FullName = User.Identity.Name;

            if (id == Guid.Empty)
            {
                TempData["ErrorMessage"] = "Invalid trip ID.";
                var trainsResult = await _trainService.GetAllTrainsAsync();
                ViewBag.Trains = trainsResult.Success ? trainsResult.Data.Trains : new List<TrainResponseModel>();
                return View(updateTripDto);
            }

            if (!ModelState.IsValid)
            {
                var trainsResult = await _trainService.GetAllTrainsAsync();
                ViewBag.Trains = trainsResult.Success ? trainsResult.Data.Trains : new List<TrainResponseModel>();
                return View(updateTripDto);
            }

            try
            {
                // Convert DepartureTime from WAT to UTC
                if (updateTripDto.DepartureTime.Kind != DateTimeKind.Utc)
                {
                    updateTripDto.DepartureTime = TimeZoneInfo.ConvertTimeToUtc(updateTripDto.DepartureTime, _watTimeZone);
                }

                var result = await _tripService.UpdateTripAsync(id, updateTripDto);
                if (result.Success)
                {
                    TempData["SuccessMessage"] = "Trip updated successfully!";
                    return RedirectToAction("Index");
                }

                TempData["ErrorMessage"] = result.Message;
                var trainsResult = await _trainService.GetAllTrainsAsync();
                ViewBag.Trains = trainsResult.Success ? trainsResult.Data.Trains : new List<TrainResponseModel>();
                return View(updateTripDto);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error updating trip: {ex.Message}";
                var trainsResult = await _trainService.GetAllTrainsAsync();
                ViewBag.Trains = trainsResult.Success ? trainsResult.Data.Trains : new List<TrainResponseModel>();
                return View(updateTripDto);
            }
        }

        public async Task<IActionResult> TripDetails(Guid id)
        {
            ViewBag.IsAuthenticated = User.Identity.IsAuthenticated;
            ViewBag.IsAdmin = User.IsInRole("Admin");
            ViewBag.FullName = User.Identity.Name;

            if (id == Guid.Empty)
            {
                TempData["ErrorMessage"] = "Invalid trip ID.";
                return RedirectToAction("Index");
            }

            try
            {
                var result = await _tripService.GetTripByIdAsync(id);
                if (!result.Success)
                {
                    TempData["ErrorMessage"] = result.Message;
                    return RedirectToAction("Index");
                }

                return View(result.Data);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error loading trip details: {ex.Message}";
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteTrip(Guid id)
        {
            ViewBag.IsAuthenticated = User.Identity.IsAuthenticated;
            ViewBag.IsAdmin = User.IsInRole("Admin");
            ViewBag.FullName = User.Identity.Name;

            if (id == Guid.Empty)
            {
                TempData["ErrorMessage"] = "Invalid trip ID.";
                return RedirectToAction("Index");
            }

            try
            {
                var result = await _tripService.DeleteTripAsync(id);
                if (result.Success)
                {
                    TempData["SuccessMessage"] = "Trip deleted successfully!";
                }
                else
                {
                    TempData["ErrorMessage"] = result.Message;
                }
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error deleting trip: {ex.Message}";
                return RedirectToAction("Index");
            }
        }
        #endregion
    }
}