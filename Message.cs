using System.ComponentModel.DataAnnotations;

namespace RentAPlace.Core.Models
{
    public class Message
    {
        public int Id { get; set; }
        
        [Required]
        public int SenderId { get; set; }
        
        [Required]
        public int ReceiverId { get; set; }
        
        [Required]
        [StringLength(2000)]
        public string Content { get; set; } = string.Empty;
        
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        
        public bool IsRead { get; set; } = false;
        
        public int? PropertyId { get; set; } // Optional: link message to a specific property
        
        // Navigation properties
        public virtual User Sender { get; set; } = null!;
        public virtual User Receiver { get; set; } = null!;
        public virtual Property? Property { get; set; }
    }
}

