using System.ComponentModel.DataAnnotations;

namespace TrainBookingAppMVC.DTOs.RequestModel
{
    public class AuthRequestModel
    {
        [Required]
        [StringLength(100, MinimumLength = 3)]
        public string Username { get; set; } = string.Empty;
        [Required]
        [StringLength(50, MinimumLength = 8)]
        public string Password { get; set; } = string.Empty;
    }
}
