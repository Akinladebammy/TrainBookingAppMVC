using System.ComponentModel.DataAnnotations;

namespace TrainBookingAppMVC.DTOs.RequestModel
{
    public class CancelBookingRequestModel
    {
        [Required]
        public Guid UserId { get; set; }
    }
}
