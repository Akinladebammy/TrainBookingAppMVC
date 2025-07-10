using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TrainBookingAppMVC.Models;
using TrainBookingAppMVC.Services.Interface;

namespace TrainBookingAppMVC.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ITripService _tripService;

        public HomeController(ILogger<HomeController> logger, ITripService tripService)
        {
            _logger = logger;
            _tripService = tripService;
        }

        // Public home page - displays available trips
        [AllowAnonymous]
        public async Task<IActionResult> Index()
        {
            try
            {
                // Set user data for view
                if (User.Identity.IsAuthenticated)
                {
                    ViewBag.IsAuthenticated = true;
                    ViewBag.Username = User.FindFirst(ClaimTypes.Name)?.Value;
                    ViewBag.FullName = User.FindFirst(ClaimTypes.GivenName)?.Value;
                    ViewBag.Role = User.FindFirst(ClaimTypes.Role)?.Value;
                    ViewBag.IsAdmin = User.IsInRole("Admin");
                    ViewBag.IsRegular = User.IsInRole("Regular");
                }
                else
                {
                    ViewBag.IsAuthenticated = false;
                }

                // Fetch available trips
                var tripsResult = await _tripService.GetAvailableTripsAsync();
                if (!tripsResult.Success)
                {
                    TempData["ErrorMessage"] = tripsResult.Message;
                    return View(new List<TrainBookingAppMVC.DTOs.ResponseModel.TripDto>());
                }

                return View(tripsResult.Data.ToList());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading home page with available trips");
                TempData["ErrorMessage"] = "An error occurred while loading available trips.";
                return View(new List<TrainBookingAppMVC.DTOs.ResponseModel.TripDto>());
            }
        }

        // About page - public access
        [AllowAnonymous]
        public IActionResult About()
        {
            return View();
        }

        // Contact page - public access
        [AllowAnonymous]
        public IActionResult Contact()
        {
            return View();
        }

        // Privacy page - public access
        [AllowAnonymous]
        public IActionResult Privacy()
        {
            return View();
        }

        // Error page - public access
        [AllowAnonymous]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        // Access denied page
        [AllowAnonymous]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}