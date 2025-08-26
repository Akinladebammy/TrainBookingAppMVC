using System.Collections.Generic;

namespace TrainBookingAppMVC.DTOs.ResponseModel
{
    public class BookingListResponseModel
    {
        public List<BookingResponseModel> Bookings { get; set; } = new List<BookingResponseModel>();
        public int TotalCount { get; set; }
    }
}