using System.ComponentModel.DataAnnotations;

namespace RentAPlace.Core.Models
{
    public class PropertyImage
    {
        public int Id { get; set; }
        
        [Required]
        public int PropertyId { get; set; }
        
        [Required]
        [StringLength(500)]
        public string ImagePath { get; set; } = string.Empty;
        
        [StringLength(200)]
        public string AltText { get; set; } = string.Empty;
        
        public bool IsPrimary { get; set; } = false;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation property
        public virtual Property Property { get; set; } = null!;
    }
}

