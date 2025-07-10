using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TrainBookingAppMVC.DTOs.RequestModel;
using TrainBookingAppMVC.Services.Interface;

namespace TrainBookingAppMVC.Controllers
{
    public class AuthController : Controller
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        // GET: Auth/Login
        [AllowAnonymous]
        public IActionResult Login()
        {
            // Redirect to home if already authenticated
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        // POST: Auth/Login
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(AuthRequestModel request)
        {
            if (!ModelState.IsValid)
            {
                return View(request);
            }

            var response = await _authService.LoginAsync(request);

            if (response.Success)
            {
                // Create claims for the authenticated user
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, response.Data.Id.ToString()),
                    new Claim(ClaimTypes.Name, response.Data.Username),
                    new Claim(ClaimTypes.GivenName, response.Data.FullName),
                    new Claim(ClaimTypes.Role, response.Data.Role)
                };

                // Create claims identity
                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

                // Sign in the user
                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    claimsPrincipal,
                    new AuthenticationProperties
                    {
                        IsPersistent = true, // Remember me functionality
                        ExpiresUtc = DateTimeOffset.UtcNow.AddHours(24) // Session expires in 24 hours
                    });

                // Redirect based on role - FIXED: Changed "Dashboard" to "Index"
                if (response.Data.Role == "Admin")
                {
                    return RedirectToAction("Index", "Admin");
                }
                else
                {
                    return RedirectToAction("Index", "User"); 
                }
            }

            // Add error message to ModelState
            if (response.Message.Contains("Invalid username or password"))
            {
                ModelState.AddModelError("", "Invalid username or password");
            }
            else if (response.Message.Contains("required"))
            {
                ModelState.AddModelError("", "All fields are required");
            }
            else
            {
                ModelState.AddModelError("", "An error occurred during login");
            }

            return View(request);
        }

        // GET: Auth/Register
        [AllowAnonymous]
        public IActionResult Register()
        {
            // Redirect to home if already authenticated
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        // POST: Auth/Register
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterRequestModel request)
        {
            if (!ModelState.IsValid)
            {
                return View(request);
            }

            var response = await _authService.RegisterAsync(request);

            if (response.Success)
            {
                // Redirect to login page on successful registration
                TempData["SuccessMessage"] = "Registration successful! Please login.";
                return RedirectToAction("Login");
            }

            // Add error message to ModelState
            if (response.Message.Contains("already exists"))
            {
                ModelState.AddModelError("", "User already exists");
            }
            else if (response.Message.Contains("required") ||
                     response.Message.Contains("valid email") ||
                     response.Message.Contains("Password must"))
            {
                ModelState.AddModelError("", response.Message);
            }
            else
            {
                ModelState.AddModelError("", "An error occurred during registration");
            }

            return View(request);
        }

        // GET: Auth/Logout
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            // Clear authentication cookies/session
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }

        // GET: Auth/AccessDenied
        [AllowAnonymous]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}