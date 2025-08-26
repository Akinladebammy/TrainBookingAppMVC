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

        // Store seat numbers as a comma-separated string in the database
        private string _seatNumbersString = string.Empty;

        [Column("SeatNumbers")]
        public string SeatNumbersString
        {
            get => _seatNumbersString;
            set => _seatNumbersString = value;
        }

        // Transient property for application logic
        [NotMapped]
        public List<string> SeatNumbers
        {
            get => string.IsNullOrEmpty(_seatNumbersString) ? new List<string>() : _seatNumbersString.Split(',').ToList();
            set => _seatNumbersString = value != null && value.Any() ? string.Join(",", value) : string.Empty;
        }

        public int NumberOfSeats { get; set; }
        public decimal TotalPrice { get; set; }
        public DateTime BookingDate { get; set; } = DateTime.UtcNow;
        public bool IsCancelled { get; set; } = false;
        public string TransactionReference { get; set; }
        public byte[] RowVersion { get; set; } = null!; // Added for concurrency

        public virtual Trip Trip { get; set; } = null!;
        public virtual User User { get; set; } = null!;
    }
}


