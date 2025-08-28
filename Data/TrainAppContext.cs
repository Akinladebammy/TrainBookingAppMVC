using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using TrainBookinAppMVC.Models;
using TrainBookingAppMVC.Models;
using TrainBookingAppMVC.Models.Enum;

namespace TrainBookinAppWeb.Data
{
    public class TrainAppContext : DbContext
    {
        public TrainAppContext(DbContextOptions<TrainAppContext> options) : base(options)
        {
        }
        public DbSet<User> Users { get; set; }
        public DbSet<Trip> Trips { get; set; }
        public DbSet<Train> Trains { get; set; }
        public DbSet<TripPricing> TripPricings { get; set; }
        public DbSet<Booking> Bookings { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Trip configuration
            modelBuilder.Entity<Trip>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Source).IsRequired().HasConversion<string>().HasMaxLength(100);
                entity.Property(e => e.Destination).IsRequired().HasConversion<string>().HasMaxLength(100);
                entity.Property(e => e.DepartureTime).IsRequired();
                entity.Property(e => e.CreatedAt).IsRequired();

                entity.HasOne(e => e.Train)
                    .WithMany(t => t.Trips)
                    .HasForeignKey(e => e.TrainId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Train configuration
            modelBuilder.Entity<Train>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.TrainNumber).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Description).HasMaxLength(500);
                entity.Property(e => e.ImagePath).HasMaxLength(500);
                entity.Property(e => e.EconomyCapacity).IsRequired();
                entity.Property(e => e.BusinessCapacity).IsRequired();
                entity.Property(e => e.FirstClassCapacity).IsRequired();

                entity.HasIndex(e => e.TrainNumber).IsUnique();
            });

            // TripPricing configuration
            modelBuilder.Entity<TripPricing>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Price).HasColumnType("decimal(18,2)").IsRequired();
                entity.Property(e => e.TicketClass).IsRequired().HasConversion<string>();
                entity.Property(e => e.AvailableSeats).IsRequired();
                entity.Property(e => e.TotalSeats).IsRequired();

                entity.HasOne(e => e.Trip)
                    .WithMany(t => t.TripPricings)
                    .HasForeignKey(e => e.TripId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => new { e.TripId, e.TicketClass }).IsUnique();
            });

            // Booking configuration
            modelBuilder.Entity<Booking>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.TotalPrice).HasColumnType("decimal(18,2)").IsRequired();
                entity.Property(e => e.NumberOfSeats).IsRequired();
                entity.Property(e => e.BookingDate).IsRequired();
                entity.Property(e => e.SeatNumbers).IsRequired().HasMaxLength(255); // Updated to SeatNumbers
                entity.Property(e => e.TicketClass).IsRequired().HasConversion<string>();
                entity.Property(e => e.RowVersion).IsRowVersion()
                    .HasColumnName("xmin")
                    .HasColumnType("xid")
                    .IsRowVersion();
                entity.Property(e => e.TransactionReference).HasMaxLength(100); // Added for Paystack

                entity.HasOne(e => e.Trip)
                    .WithMany(t => t.Bookings)
                    .HasForeignKey(e => e.TripId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.User)
                    .WithMany(u => u.Bookings)
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.Property(e => e.UserId).HasColumnName("UserId");
                entity.Property(e => e.TripId).HasColumnName("TripId");
            });

            // User configuration
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.FullName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Username).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Email).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Password).IsRequired();
                entity.Property(e => e.Salt).IsRequired();
                entity.Property(e => e.Role).IsRequired().HasConversion<string>();

                entity.HasIndex(e => e.Username).IsUnique();
                entity.HasIndex(e => e.Email).IsUnique();
            });

            // Call the seed method
            SeedAdminUser(modelBuilder);
        }

        private void SeedAdminUser(ModelBuilder modelBuilder)
        {
            var adminId = Guid.Parse("11111111-1111-1111-1111-111111111111");
            var salt = GenerateSalt();
            var hashedPassword = HashPassword("Admin@123", salt);

            var adminUser = new User
            {
                Id = adminId,
                FullName = "System Administrator",
                Username = "admin",
                Email = "admin@gmail.com",
                Password = hashedPassword,
                Salt = salt,
                Role = UserRole.Admin
            };

            modelBuilder.Entity<User>().HasData(adminUser);
        }

        public byte[] GenerateSalt()
        {
            return RandomNumberGenerator.GetBytes(16);
        }

        public string HashPassword(string password, byte[] salt)
        {
            using (var sha256 = SHA256.Create())
            {
                var encodedData = Encoding.UTF8.GetBytes(password + Convert.ToBase64String(salt));
                var hash = sha256.ComputeHash(encodedData);
                return Convert.ToHexString(hash);
            }
        }

        public bool VerifyPassword(string password, string storedHash, byte[] salt)
        {
            var hashedPassword = HashPassword(password, salt);
            return hashedPassword == storedHash;
        }
    }
}