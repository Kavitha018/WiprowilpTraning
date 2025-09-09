using System.ComponentModel.DataAnnotations;

namespace RentAPlace.Core.Models
{
    public class Property
    {
        public int Id { get; set; }
        
        [Required]
        public int OwnerId { get; set; }
        
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
        public PropertyType Type { get; set; }
        
        [StringLength(1000)]
        public string Features { get; set; } = string.Empty; // JSON string of features
        
        public decimal PricePerNight { get; set; }
        
        public int MaxGuests { get; set; }
        
        public int Bedrooms { get; set; }
        
        public int Bathrooms { get; set; }
        
        public bool IsAvailable { get; set; } = true;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation properties
        public virtual User Owner { get; set; } = null!;
        public virtual ICollection<PropertyImage> Images { get; set; } = new List<PropertyImage>();
        public virtual ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
    }
    
    public enum PropertyType
    {
        Apartment = 0,
        House = 1,
        Villa = 2,
        Condo = 3,
        Studio = 4
    }
}

