namespace TrainBookingAppMVC.DTOs.ResponseModel
{
    public class BookingResponseModel
    {
        public Guid Id { get; set; }
        public Guid TripId { get; set; }
        public Guid UserId { get; set; }
        public string TicketClass { get; set; } = string.Empty;
        public int NumberOfSeats { get; set; }
        public decimal TotalPrice { get; set; }
        public DateTime BookingDate { get; set; }
        public bool IsCancelled { get; set; }

        // Trip details
        public string TrainNumber { get; set; } = string.Empty;
        public string TrainName { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty;
        public string Destination { get; set; } = string.Empty;
        public DateTime DepartureTime { get; set; }
        public bool IsExpired { get; set; }
        public string TripStatus { get; set; } = string.Empty;

        // User details (for admin view)
        public string? Username { get; set; }
        public string? FullName { get; set; }
    }
}
