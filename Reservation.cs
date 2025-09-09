using System.ComponentModel.DataAnnotations;

namespace RentAPlace.Core.Models
{
    public class Reservation
    {
        public int Id { get; set; }
        
        [Required]
        public int UserId { get; set; }
        
        [Required]
        public int PropertyId { get; set; }
        
        [Required]
        public DateTime CheckInDate { get; set; }
        
        [Required]
        public DateTime CheckOutDate { get; set; }
        
        public int NumberOfGuests { get; set; }
        
        public decimal TotalAmount { get; set; }
        
        public ReservationStatus Status { get; set; } = ReservationStatus.Pending;
        
        [StringLength(500)]
        public string? SpecialRequests { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime? ConfirmedAt { get; set; }
        
        // Navigation properties
        public virtual User User { get; set; } = null!;
        public virtual Property Property { get; set; } = null!;
    }
    
    public enum ReservationStatus
    {
        Pending = 0,
        Confirmed = 1,
        Rejected = 2,
        Cancelled = 3,
        Completed = 4
    }
}

