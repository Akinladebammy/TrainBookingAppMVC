using System.ComponentModel.DataAnnotations.Schema;
using TrainBookingAppMVC.Models.Enum;

namespace TrainBookingAppMVC.Models
{
    public class TripPricing
    {
        public Guid Id { get; set; }
        public Guid TripId { get; set; }
        public TicketClass TicketClass { get; set; }
        public decimal Price { get; set; }
        public int AvailableSeats { get; set; }
        public int TotalSeats { get; set; }

        [ForeignKey("TripId")]
        public virtual Trip Trip { get; set; } = null!;
    }
}
