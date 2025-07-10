using TrainBookingAppMVC.Models.Enum;

namespace TrainBookingAppMVC.DTOs.RequestModel
{
    public class UpdateTripPricingDto
    {
        public Guid? Id { get; set; } // Null for new pricing
        public TicketClass TicketClass { get; set; }
        public decimal Price { get; set; }
    }
}
