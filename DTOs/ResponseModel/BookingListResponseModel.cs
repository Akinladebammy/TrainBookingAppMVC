namespace TrainBookingAppMVC.DTOs.ResponseModel
{
    public class BookingListResponseModel
    {
        public List<BookingResponseModel> Bookings { get; set; } = new();
        public int TotalCount { get; set; }
    }
}
