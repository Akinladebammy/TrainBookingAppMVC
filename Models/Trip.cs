using System.ComponentModel.DataAnnotations.Schema;
using TrainBookinAppMVC.Models;

namespace TrainBookingAppMVC.Models
{
    public class Trip
    {
        public Guid Id { get; set; }
        public Guid TrainId { get; set; }
        public string Source { get; set; } = string.Empty;
        public string Destination { get; set; } = string.Empty;
        public DateTime DepartureTime { get; set; }
        public bool IsExpired { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        [ForeignKey("TrainId")]
        public virtual Train Train { get; set; } = null!;

        public virtual ICollection<TripPricing> TripPricings { get; set; } = new List<TripPricing>();
        public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();
    }
}
