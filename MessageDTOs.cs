using System.ComponentModel.DataAnnotations;

namespace RentAPlace.Core.DTOs
{
    public class MessageDto
    {
        public int Id { get; set; }
        public int SenderId { get; set; }
        public string SenderName { get; set; } = string.Empty;
        public int ReceiverId { get; set; }
        public string ReceiverName { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public bool IsRead { get; set; }
        public int? PropertyId { get; set; }
        public string? PropertyTitle { get; set; }
    }
    
    public class SendMessageRequest
    {
        [Required]
        public int ReceiverId { get; set; }
        
        [Required]
        [StringLength(2000)]
        public string Content { get; set; } = string.Empty;
        
        public int? PropertyId { get; set; }
    }
    
    public class ConversationDto
    {
        public int OtherUserId { get; set; }
        public string OtherUserName { get; set; } = string.Empty;
        public string? OtherUserRole { get; set; }
        public List<MessageDto> Messages { get; set; } = new List<MessageDto>();
        public int UnreadCount { get; set; }
        public DateTime LastMessageTime { get; set; }
    }
}

