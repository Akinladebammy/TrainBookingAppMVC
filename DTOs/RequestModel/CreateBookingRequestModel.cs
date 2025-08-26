using System.ComponentModel.DataAnnotations;
using TrainBookingAppMVC.Models.Enum;

namespace TrainBookingAppMVC.DTOs.RequestModel
{
    public class CreateBookingRequestModel
    {
        [Required]
        public Guid TripId { get; set; }

        [Required]
        public Guid UserId { get; set; }

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Number of seats must be at least 1")]
        public int NumberOfSeats { get; set; }

        [Required]
        [EnumDataType(typeof(TicketClass))]
        public string TicketClass { get; set; } = string.Empty;

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Payment amount must be greater than 0")]
        public decimal PaymentAmount { get; set; }

        [Required(ErrorMessage = "At least one seat must be selected")]
        public List<string> SeatNumbers { get; set; }
        public string? TransactionReference { get; set; } // Add for Paystack transaction reference
    }
}



