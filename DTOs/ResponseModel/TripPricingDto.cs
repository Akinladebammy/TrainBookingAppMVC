using System.ComponentModel.DataAnnotations;
using TrainBookingAppMVC.Models.Enum;

namespace TrainBookingAppMVC.DTOs.ResponseModel
{
    public class TripPricingDto
    {
        public Guid Id { get; set; }
        [EnumDataType(typeof(TicketClass))]
        public string TicketClass { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int AvailableSeats { get; set; }
        public int TotalSeats { get; set; }
    }
}