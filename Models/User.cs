using TrainBookinAppMVC.Models;
using TrainBookingAppMVC.Models.Enum;

namespace TrainBookingAppMVC.Models
{
    public class User
    {

        public Guid Id { get; set; }
        public string FullName { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string Password { get; set; } 
        public byte[] Salt { get; set; }
  
        public UserRole Role { get; set; }

        public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();

    }
}
