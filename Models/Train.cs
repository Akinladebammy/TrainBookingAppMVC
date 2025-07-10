namespace TrainBookingAppMVC.Models
{
    public class Train
    {
        public Guid Id { get; set; }
        public string TrainNumber { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        // Capacity by class
        public int EconomyCapacity { get; set; }
        public int BusinessCapacity { get; set; }
        public int FirstClassCapacity { get; set; }

        // Total capacity
        public int TotalCapacity => EconomyCapacity + BusinessCapacity + FirstClassCapacity;

        public virtual ICollection<Trip> Trips { get; set; } = new List<Trip>();
    }
}
