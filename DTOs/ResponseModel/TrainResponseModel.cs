namespace TrainBookingAppMVC.DTOs.ResponseModel
{
    public class TrainResponseModel
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string TrainNumber { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int EconomyCapacity { get; set; }
        public int BusinessCapacity { get; set; }
        public int FirstClassCapacity { get; set; }
        public string Description { get; set; } = string.Empty;
    }
}
