using System.ComponentModel.DataAnnotations;
using TrainBookingAppMVC.Models.Enum;

namespace TrainBookingAppMVC.DTOs.ResponseModel
{
    public class TripDto
    {
        public Guid Id { get; set; }
        public Guid TrainId { get; set; }
        public string TrainNumber { get; set; } = string.Empty;
        public string TrainName { get; set; } = string.Empty;
        [EnumDataType(typeof(Terminal))]
        public string Source { get; set; } = string.Empty;
        [EnumDataType(typeof(Terminal))]
        public string Destination { get; set; } = string.Empty;
        public DateTime DepartureTime { get; set; }
        public bool IsExpired { get; set; }
        public List<TripPricingDto> Pricings { get; set; } = new List<TripPricingDto>();
    }
}