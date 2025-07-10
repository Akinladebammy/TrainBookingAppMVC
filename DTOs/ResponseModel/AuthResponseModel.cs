using TrainBookingAppMVC.Models.Enum;

namespace TrainBookinAppMVC.DTOs.ResponseModel
{
    public class AuthResponseModel
    {
        public Guid Id { get; set; } = Guid.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Role { get; set; } = UserRole.Regular.ToString();
    }
}
