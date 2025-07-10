namespace TrainBookingAppMVC.DTOs.RequestModel
{
    public class UpdateTripDto
    {
        public Guid TrainId { get; set; }
        public string Source { get; set; } = string.Empty;
        public string Destination { get; set; } = string.Empty;
        public DateTime DepartureTime { get; set; }
        public List<UpdateTripPricingDto> Pricings { get; set; } = new List<UpdateTripPricingDto>();
    }
}
