namespace TrainBookingAppMVC.DTOs.ResponseModel
{
    public class TrainListResponseModel
    {
        public List<TrainResponseModel> Trains { get; set; } = new List<TrainResponseModel>();
        public int TotalCount { get; set; }
    }
}
