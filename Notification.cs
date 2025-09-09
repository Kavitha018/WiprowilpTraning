using System.ComponentModel.DataAnnotations;

namespace RentAPlace.Core.Models
{
    public class Notification
    {
        public int Id { get; set; }
        
        [Required]
        public int UserId { get; set; }
        
        [Required]
        [StringLength(500)]
        public string Message { get; set; } = string.Empty;
        
        public NotificationType Type { get; set; }
        
        public bool IsRead { get; set; } = false;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public int? RelatedEntityId { get; set; } // ID of related reservation, property, etc.
        
        [StringLength(100)]
        public string? RelatedEntityType { get; set; } // "Reservation", "Property", etc.
        
        // Navigation property
        public virtual User User { get; set; } = null!;
    }
    
    public enum NotificationType
    {
        ReservationRequest = 0,
        ReservationConfirmed = 1,
        ReservationRejected = 2,
        NewMessage = 3,
        PropertyUpdate = 4,
        General = 5
    }
}

