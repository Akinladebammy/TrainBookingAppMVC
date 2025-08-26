using System.ComponentModel.DataAnnotations;
using TrainBookingAppMVC.Models.Enum;

namespace TrainBookingAppMVC.DTOs.RequestModel
{
    public class UpdateTripPricingDto
    {
        public Guid? Id { get; set; } // Null for new pricing

        [Required]
        [EnumDataType(typeof(TicketClass))]
        public string TicketClass { get; set; } = string.Empty;

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than 0")]
        public decimal Price { get; set; }
    }
}