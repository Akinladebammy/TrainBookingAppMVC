using System.ComponentModel.DataAnnotations.Schema;
using TrainBookingAppMVC.Models;
using TrainBookingAppMVC.Models.Enum;

namespace TrainBookinAppMVC.Models
{
    public class Booking
    {
        public Guid Id { get; set; }
        public Guid TripId { get; set; }
        public Guid UserId { get; set; }
        public TicketClass TicketClass { get; set; }
        public int NumberOfSeats { get; set; }
        public decimal TotalPrice { get; set; }
        public DateTime BookingDate { get; set; } = DateTime.UtcNow;
        public bool IsCancelled { get; set; } = false;

        //[ForeignKey("TripId")]
        public virtual Trip Trip { get; set; } = null!;

        //[ForeignKey("UserId")]
        public virtual User User { get; set; } = null!;
    }
}
