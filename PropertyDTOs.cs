using System.ComponentModel.DataAnnotations;

namespace RentAPlace.Core.DTOs
{
    public class PropertyDto
    {
        public int Id { get; set; }
        public int OwnerId { get; set; }
        public string OwnerName { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public List<string> Features { get; set; } = new List<string>();
        public decimal PricePerNight { get; set; }
        public int MaxGuests { get; set; }
        public int Bedrooms { get; set; }
        public int Bathrooms { get; set; }
        public bool IsAvailable { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<string> ImageUrls { get; set; } = new List<string>();
        public string PrimaryImageUrl { get; set; } = string.Empty;
    }
    
    public class CreatePropertyRequest
    {
        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;
        
        [Required]
        [StringLength(2000)]
        public string Description { get; set; } = string.Empty;
        
        [Required]
        [StringLength(200)]
        public string Location { get; set; } = string.Empty;
        
        [Required]
        public string Type { get; set; } = string.Empty;
        
        public List<string> Features { get; set; } = new List<string>();
        
        [Required]
        [Range(0.01, double.MaxValue)]
        public decimal PricePerNight { get; set; }
        
        [Required]
        [Range(1, int.MaxValue)]
        public int MaxGuests { get; set; }
        
        [Required]
        [Range(0, int.MaxValue)]
        public int Bedrooms { get; set; }
        
        [Required]
        [Range(0, int.MaxValue)]
        public int Bathrooms { get; set; }
    }
    
    public class UpdatePropertyRequest
    {
        [StringLength(200)]
        public string? Title { get; set; }
        
        [StringLength(2000)]
        public string? Description { get; set; }
        
        [StringLength(200)]
        public string? Location { get; set; }
        
        public string? Type { get; set; }
        
        public List<string>? Features { get; set; }
        
        [Range(0.01, double.MaxValue)]
        public decimal? PricePerNight { get; set; }
        
        [Range(1, int.MaxValue)]
        public int? MaxGuests { get; set; }
        
        [Range(0, int.MaxValue)]
        public int? Bedrooms { get; set; }
        
        [Range(0, int.MaxValue)]
        public int? Bathrooms { get; set; }
        
        public bool? IsAvailable { get; set; }
    }
    
    public class PropertySearchRequest
    {
        public string? Location { get; set; }
        public string? Type { get; set; }
        public DateTime? CheckInDate { get; set; }
        public DateTime? CheckOutDate { get; set; }
        public int? MaxGuests { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public List<string>? Features { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? SortBy { get; set; } = "CreatedAt";
        public string? SortOrder { get; set; } = "desc";
    }
}

