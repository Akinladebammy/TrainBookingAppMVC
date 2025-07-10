using System.Text.RegularExpressions;
using TrainBookinAppMVC.DTOs.ResponseModel;
using TrainBookingAppMVC.DTOs.RequestModel;
using TrainBookingAppMVC.DTOs.Wrapper;
using TrainBookingAppMVC.Models;
using TrainBookingAppMVC.Models.Enum;
using TrainBookingAppMVC.PasswordValidation;
using TrainBookingAppMVC.Repository.Interfaces;
using TrainBookingAppMVC.Services.Interface;

namespace TrainBookingAppMVC.Services.Implementation
{
    public class AuthService : IAuthService
    {
        private readonly IAuthRepository _authRepository;
        private readonly IPasswordHashing _passwordHashing;

        // Email validation regex pattern
        private static readonly Regex EmailRegex = new Regex(
            @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // Password validation regex pattern (at least 8 characters, contains numbers and symbols)
        private static readonly Regex PasswordRegex = new Regex(
            @"^(?=.*[0-9])(?=.*[!@#$%^&*()_+\-=\[\]{};':""\\|,.<>\/?]).*$",
            RegexOptions.Compiled);

        public AuthService(IAuthRepository authRepository, IPasswordHashing passwordHashing)
        {
            _authRepository = authRepository;
            _passwordHashing = passwordHashing;
        }

        public async Task<ResponseWrapper<AuthResponseModel>> LoginAsync(AuthRequestModel request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.Username) || string.IsNullOrEmpty(request.Password))
                {
                    return ResponseWrapper<AuthResponseModel>.ErrorResponse("Username and password are required");
                }

                var user = await _authRepository.GetUserByUsernameAsync(request.Username);
                if (user == null)
                {
                    return ResponseWrapper<AuthResponseModel>.ErrorResponse("Invalid username or password");
                }

                bool isPasswordValid = _passwordHashing.VerifyPassword(request.Password, user.Password, user.Salt);
                if (!isPasswordValid)
                {
                    return ResponseWrapper<AuthResponseModel>.ErrorResponse("Invalid username or password");
                }

                var authResponse = new AuthResponseModel
                {
                    Id = user.Id,
                    FullName = user.FullName,
                    Username = user.Username,
                    Role = user.Role.ToString()
                };

                return ResponseWrapper<AuthResponseModel>.SuccessResponse(authResponse, "Login successful");
            }
            catch (Exception ex)
            {
                return ResponseWrapper<AuthResponseModel>.ErrorResponse("An error occurred during login");
            }
        }

        public async Task<ResponseWrapper<AuthResponseModel>> RegisterAsync(RegisterRequestModel request)
        {
            try
            {
                // Basic null/empty validation
                if (string.IsNullOrEmpty(request.Username) || string.IsNullOrEmpty(request.Password) ||
                    string.IsNullOrEmpty(request.FullName) || string.IsNullOrEmpty(request.Email))
                {
                    return ResponseWrapper<AuthResponseModel>.ErrorResponse("All fields are required");
                }

                // Email validation
                if (!IsValidEmail(request.Email))
                {
                    return ResponseWrapper<AuthResponseModel>.ErrorResponse("Please enter a valid email address");
                }

                // Password validation
                if (!IsValidPassword(request.Password))
                {
                    return ResponseWrapper<AuthResponseModel>.ErrorResponse("Password must be at least 8 characters long and contain at least one number and one symbol");
                }

                // Check if username already exists
                if (await _authRepository.UserExistsAsync(request.Username))
                {
                    return ResponseWrapper<AuthResponseModel>.ErrorResponse("Username already exists");
                }

                var salt = _passwordHashing.GenerateSalt();
                var hashedPassword = _passwordHashing.HashPassword(request.Password, salt);

                var newUser = new User
                {
                    FullName = request.FullName,
                    Username = request.Username,
                    Email = request.Email,
                    Password = hashedPassword,
                    Salt = salt,
                    Role = UserRole.Regular
                };

                var createdUser = await _authRepository.CreateUserAsync(newUser);

                var authResponse = new AuthResponseModel
                {
                    Id = createdUser.Id,
                    FullName = createdUser.FullName,
                    Username = createdUser.Username,
                    Role = createdUser.Role.ToString()
                };

                return ResponseWrapper<AuthResponseModel>.SuccessResponse(authResponse, "Registration successful");
            }
            catch (Exception ex)
            {
                return ResponseWrapper<AuthResponseModel>.ErrorResponse("An error occurred during registration");
            }
        }

        private bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            return EmailRegex.IsMatch(email);
        }

        private bool IsValidPassword(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
                return false;

            // Check minimum length
            if (password.Length < 8)
                return false;

            // Check if password contains at least one number and one symbol
            return PasswordRegex.IsMatch(password);
        }
    }
}