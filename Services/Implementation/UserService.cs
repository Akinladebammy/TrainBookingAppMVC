using System.Text.RegularExpressions;
using TrainBookingAppMVC.DTOs.RequestModel;
using TrainBookingAppMVC.DTOs.ResponseModel;
using TrainBookingAppMVC.DTOs.Wrapper;
using TrainBookingAppMVC.Repository.Interfaces;
using TrainBookingAppMVC.Services.Interface;

namespace TrainBookingAppMVC.Services.Implementation
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;

        // Email validation regex pattern
        private static readonly Regex EmailRegex = new Regex(
            @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public UserService(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task<ResponseWrapper<UserListResponseModel>> GetAllUsersAsync()
        {
            try
            {
                var users = await _userRepository.GetAllUsersAsync();

                var userListResponse = new UserListResponseModel
                {
                    Users = users.Select(user => new UserResponseModel
                    {
                        Id = user.Id,
                        FullName = user.FullName,
                        Username = user.Username,
                        Email = user.Email,
                        Role = user.Role.ToString()
                    }).ToList(),
                    TotalCount = users.Count
                };

                return ResponseWrapper<UserListResponseModel>.SuccessResponse(userListResponse, "Users retrieved successfully");
            }
            catch (Exception ex)
            {
                return ResponseWrapper<UserListResponseModel>.ErrorResponse($"An error occurred while retrieving users: {ex.Message}");
            }
        }

        public async Task<ResponseWrapper<UserResponseModel>> GetUserByIdAsync(Guid id)
        {
            try
            {
                if (id == Guid.Empty)
                {
                    return ResponseWrapper<UserResponseModel>.ErrorResponse("User ID cannot be empty");
                }

                var user = await _userRepository.GetUserByIdAsync(id);
                if (user == null)
                {
                    return ResponseWrapper<UserResponseModel>.ErrorResponse("User not found");
                }

                var userResponse = new UserResponseModel
                {
                    Id = user.Id,
                    FullName = user.FullName,
                    Username = user.Username,
                    Email = user.Email,
                    Role = user.Role.ToString()
                };

                return ResponseWrapper<UserResponseModel>.SuccessResponse(userResponse, "User retrieved successfully");
            }
            catch (Exception ex)
            {
                return ResponseWrapper<UserResponseModel>.ErrorResponse($"An error occurred while retrieving the user: {ex.Message}");
            }
        }

        public async Task<ResponseWrapper<UserResponseModel>> GetUserByEmailAsync(string email)
        {
            try
            {
                if (string.IsNullOrEmpty(email))
                {
                    return ResponseWrapper<UserResponseModel>.ErrorResponse("Email is required");
                }

                if (!IsValidEmail(email))
                {
                    return ResponseWrapper<UserResponseModel>.ErrorResponse("Please enter a valid email address");
                }

                var user = await _userRepository.GetUserByEmailAsync(email);
                if (user == null)
                {
                    return ResponseWrapper<UserResponseModel>.ErrorResponse("User not found");
                }

                var userResponse = new UserResponseModel
                {
                    Id = user.Id,
                    FullName = user.FullName,
                    Username = user.Username,
                    Email = user.Email,
                    Role = user.Role.ToString()
                };

                return ResponseWrapper<UserResponseModel>.SuccessResponse(userResponse, "User retrieved successfully");
            }
            catch (Exception ex)
            {
                return ResponseWrapper<UserResponseModel>.ErrorResponse($"An error occurred while retrieving the user: {ex.Message}");
            }
        }

        public async Task<ResponseWrapper<UserResponseModel>> UpdateUserAsync(Guid id, UpdateUserRequestModel request)
        {
            try
            {
                if (id == Guid.Empty)
                {
                    return ResponseWrapper<UserResponseModel>.ErrorResponse("User ID cannot be empty");
                }

                if (request == null)
                {
                    return ResponseWrapper<UserResponseModel>.ErrorResponse("Request cannot be null");
                }

                // Validate required fields
                if (string.IsNullOrEmpty(request.FullName) || string.IsNullOrEmpty(request.Email) ||
                    string.IsNullOrEmpty(request.Username))
                {
                    return ResponseWrapper<UserResponseModel>.ErrorResponse("All fields are required");
                }

                // Validate email format
                if (!IsValidEmail(request.Email))
                {
                    return ResponseWrapper<UserResponseModel>.ErrorResponse("Please enter a valid email address");
                }

                // Check if user exists
                var existingUser = await _userRepository.GetUserByIdAsync(id);
                if (existingUser == null)
                {
                    return ResponseWrapper<UserResponseModel>.ErrorResponse("User not found");
                }

                // Check if username is already taken by another user
                if (await _userRepository.UserExistsByUsernameAsync(request.Username, id))
                {
                    return ResponseWrapper<UserResponseModel>.ErrorResponse("Username already exists");
                }

                // Check if email is already taken by another user
                var userWithEmail = await _userRepository.GetUserByEmailAsync(request.Email);
                if (userWithEmail != null && userWithEmail.Id != id)
                {
                    return ResponseWrapper<UserResponseModel>.ErrorResponse("Email already exists");
                }

                // Update user properties
                existingUser.FullName = request.FullName;
                existingUser.Email = request.Email;
                existingUser.Username = request.Username;

                var updatedUser = await _userRepository.UpdateUserAsync(existingUser);
                var userResponse = new UserResponseModel
                {
                    Id = updatedUser.Id,
                    FullName = updatedUser.FullName,
                    Username = updatedUser.Username,
                    Email = updatedUser.Email,
                    Role = updatedUser.Role.ToString()
                };

                return ResponseWrapper<UserResponseModel>.SuccessResponse(userResponse, "User updated successfully");
            }
            catch (Exception ex)
            {
                return ResponseWrapper<UserResponseModel>.ErrorResponse($"An error occurred while updating the user: {ex.Message}");
            }
        }

        public async Task<ResponseWrapper> DeleteUserAsync(Guid id)
        {
            try
            {
                if (id == Guid.Empty)
                {
                    return ResponseWrapper.ErrorResponse("User ID cannot be empty");
                }

                var user = await _userRepository.GetUserByIdAsync(id);
                if (user == null)
                {
                    return ResponseWrapper.ErrorResponse("User not found");
                }

                var result = await _userRepository.DeleteUserAsync(id);
                if (result)
                {
                    return ResponseWrapper.SuccessResponse("User deleted successfully");
                }

                return ResponseWrapper.ErrorResponse("Failed to delete user");
            }
            catch (Exception ex)
            {
                return ResponseWrapper.ErrorResponse($"An error occurred while deleting the user: {ex.Message}");
            }
        }

        private bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            return EmailRegex.IsMatch(email);
        }
    }
}