using System.ComponentModel.DataAnnotations;


namespace TrainBookingAppMVC.DTOs.RequestModel
{
    public class UpdateUserRequestModel
    {
        [Required]
        [StringLength(100, MinimumLength = 3)]
        public string FullName { get; set; } = string.Empty;
        [Required]
        [StringLength(50, MinimumLength = 3)]
        public string Username { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

    }
}
