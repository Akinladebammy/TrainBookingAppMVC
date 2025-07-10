namespace TrainBookingAppMVC.DTOs.ResponseModel
{
    public class BookingCreationResponseModel
    {
        public Guid BookingId { get; set; }
        public string Message { get; set; } = string.Empty;
        public decimal Change { get; set; }
    }
}
