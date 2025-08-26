using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace TrainBookingAppMVC.DTOs.RequestModel
{
    public class CreateTrainRequestModel
    {
        [Required]
        [StringLength(20, ErrorMessage = "Train number cannot exceed 20 characters")]
        public string TrainNumber { get; set; } = string.Empty;

        [Required]
        [StringLength(100, ErrorMessage = "Name cannot exceed 100 characters")]
        public string Name { get; set; } = string.Empty;

        [Required]
        [Range(1, 1000, ErrorMessage = "Capacity must be between 1 and 1000")]
        public int EconomicCapacity { get; set; }

        [Required]
        [Range(1, 1000, ErrorMessage = "Capacity must be between 1 and 1000")]
        public int BusinessCapacity { get; set; }

        [Required]
        [Range(1, 1000, ErrorMessage = "Capacity must be between 1 and 1000")]
        public int FirstClassCapacity { get; set; }

        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
        public string Description { get; set; } = string.Empty;

        public IFormFile? ImageFile { get; set; }
    }
}