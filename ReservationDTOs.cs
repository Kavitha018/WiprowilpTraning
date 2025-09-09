using System.ComponentModel.DataAnnotations;

namespace RentAPlace.Core.DTOs
{
    public class ReservationDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty;
        public int PropertyId { get; set; }
        public string PropertyTitle { get; set; } = string.Empty;
        public string PropertyLocation { get; set; } = string.Empty;
        public DateTime CheckInDate { get; set; }
        public DateTime CheckOutDate { get; set; }
        public int NumberOfGuests { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? SpecialRequests { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ConfirmedAt { get; set; }
    }
    
    public class CreateReservationRequest
    {
        [Required]
        public int PropertyId { get; set; }
        
        [Required]
        public DateTime CheckInDate { get; set; }
        
        [Required]
        public DateTime CheckOutDate { get; set; }
        
        [Required]
        [Range(1, int.MaxValue)]
        public int NumberOfGuests { get; set; }
        
        [StringLength(500)]
        public string? SpecialRequests { get; set; }
    }
    
    public class UpdateReservationStatusRequest
    {
        [Required]
        public string Status { get; set; } = string.Empty; // "Confirmed", "Rejected", "Cancelled"
    }
}

