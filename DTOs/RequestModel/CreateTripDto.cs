using System.ComponentModel.DataAnnotations;
using TrainBookingAppMVC.Models.Enum;

namespace TrainBookingAppMVC.DTOs.RequestModel
{
    public class CreateTripDto
    {
        [Required]
        public Guid TrainId { get; set; }

        [Required]
        [EnumDataType(typeof(Terminal))]
        public string Source { get; set; } = string.Empty;

        [Required]
        [EnumDataType(typeof(Terminal))]
        public string Destination { get; set; } = string.Empty;

        [Required]
        public DateTime DepartureTime { get; set; } 

        [Required]
        public List<CreateTripPricingDto> Pricings { get; set; } = new List<CreateTripPricingDto>();
    }
}