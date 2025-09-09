using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RentAPlace.Core.DTOs;
using RentAPlace.Core.Models;
using RentAPlace.Database;
using System.Security.Claims;

namespace RentAPlace.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class MessagesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<MessagesController> _logger;

        public MessagesController(ApplicationDbContext context, ILogger<MessagesController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpPost]
        public async Task<ActionResult<MessageDto>> SendMessage(SendMessageRequest request)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var senderId))
                {
                    return Unauthorized(new { message = "Invalid token" });
                }

                // Validate receiver exists
                var receiver = await _context.Users.FindAsync(request.ReceiverId);
                if (receiver == null)
                {
                    return NotFound(new { message = "Receiver not found" });
                }

                // Don't allow sending message to self
                if (senderId == request.ReceiverId)
                {
                    return BadRequest(new { message = "Cannot send message to yourself" });
                }

                // Validate property if provided
                if (request.PropertyId.HasValue)
                {
                    var property = await _context.Properties.FindAsync(request.PropertyId.Value);
                    if (property == null)
                    {
                        return NotFound(new { message = "Property not found" });
                    }
                }

                var message = new Message
                {
                    SenderId = senderId,
                    ReceiverId = request.ReceiverId,
                    Content = request.Content,
                    PropertyId = request.PropertyId,
                    Timestamp = DateTime.UtcNow,
                    IsRead = false
                };

                _context.Messages.Add(message);
                await _context.SaveChangesAsync();

                // Create notification for receiver
                var notification = new Notification
                {
                    UserId = request.ReceiverId,
                    Message = $"You have a new message from {User.FindFirst(ClaimTypes.Name)?.Value ?? "Unknown"}",
                    Type = NotificationType.NewMessage,
                    RelatedEntityId = message.Id,
                    RelatedEntityType = "Message",
                    CreatedAt = DateTime.UtcNow
                };

                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();

                // Load message with related data for response
                await _context.Entry(message)
                    .Reference(m => m.Sender)
                    .LoadAsync();
                await _context.Entry(message)
                    .Reference(m => m.Receiver)
                    .LoadAsync();
                if (message.PropertyId.HasValue)
                {
                    await _context.Entry(message)
                        .Reference(m => m.Property)
                        .LoadAsync();
                }

                var messageDto = new MessageDto
                {
                    Id = message.Id,
                    SenderId = message.SenderId,
                    SenderName = message.Sender.Name,
                    ReceiverId = message.ReceiverId,
                    ReceiverName = message.Receiver.Name,
                    Content = message.Content,
                    Timestamp = message.Timestamp,
                    IsRead = message.IsRead,
                    PropertyId = message.PropertyId,
                    PropertyTitle = message.Property?.Title
                };

                _logger.LogInformation($"Message sent successfully from user {senderId} to user {request.ReceiverId}");
                return CreatedAtAction(nameof(GetMessage), new { id = message.Id }, messageDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending message");
                return StatusCode(500, new { message = "An error occurred while sending the message" });
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<MessageDto>> GetMessage(int id)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
                {
                    return Unauthorized(new { message = "Invalid token" });
                }

                var message = await _context.Messages
                    .Include(m => m.Sender)
                    .Include(m => m.Receiver)
                    .Include(m => m.Property)
                    .FirstOrDefaultAsync(m => m.Id == id);

                if (message == null)
                {
                    return NotFound(new { message = "Message not found" });
                }

                // Check if user has access to this message
                if (message.SenderId != userId && message.ReceiverId != userId)
                {
                    return Forbid("You can only view messages you sent or received");
                }

                // Mark as read if user is the receiver
                if (message.ReceiverId == userId && !message.IsRead)
                {
                    message.IsRead = true;
                    await _context.SaveChangesAsync();
                }

                var messageDto = new MessageDto
                {
                    Id = message.Id,
                    SenderId = message.SenderId,
                    SenderName = message.Sender.Name,
                    ReceiverId = message.ReceiverId,
                    ReceiverName = message.Receiver.Name,
                    Content = message.Content,
                    Timestamp = message.Timestamp,
                    IsRead = message.IsRead,
                    PropertyId = message.PropertyId,
                    PropertyTitle = message.Property?.Title
                };

                return Ok(messageDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting message {id}");
                return StatusCode(500, new { message = "An error occurred while fetching the message" });
            }
        }

        [HttpGet("conversations")]
        public async Task<ActionResult<IEnumerable<ConversationDto>>> GetConversations()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
                {
                    return Unauthorized(new { message = "Invalid token" });
                }

                // Get all unique users that the current user has conversations with
                var conversationPartners = await _context.Messages
                    .Where(m => m.SenderId == userId || m.ReceiverId == userId)
                    .Select(m => m.SenderId == userId ? m.ReceiverId : m.SenderId)
                    .Distinct()
                    .ToListAsync();

                var conversations = new List<ConversationDto>();

                foreach (var partnerId in conversationPartners)
                {
                    var partner = await _context.Users.FindAsync(partnerId);
                    if (partner == null) continue;

                    // Get all messages between current user and this partner
                    var messages = await _context.Messages
                        .Include(m => m.Sender)
                        .Include(m => m.Receiver)
                        .Include(m => m.Property)
                        .Where(m => (m.SenderId == userId && m.ReceiverId == partnerId) ||
                                   (m.SenderId == partnerId && m.ReceiverId == userId))
                        .OrderBy(m => m.Timestamp)
                        .ToListAsync();

                    // Count unread messages from this partner
                    var unreadCount = messages.Count(m => m.ReceiverId == userId && !m.IsRead);

                    // Get last message time
                    var lastMessageTime = messages.Any() ? messages.Max(m => m.Timestamp) : DateTime.MinValue;

                    var messageDtos = messages.Select(m => new MessageDto
                    {
                        Id = m.Id,
                        SenderId = m.SenderId,
                        SenderName = m.Sender.Name,
                        ReceiverId = m.ReceiverId,
                        ReceiverName = m.Receiver.Name,
                        Content = m.Content,
                        Timestamp = m.Timestamp,
                        IsRead = m.IsRead,
                        PropertyId = m.PropertyId,
                        PropertyTitle = m.Property?.Title
                    }).ToList();

                    conversations.Add(new ConversationDto
                    {
                        OtherUserId = partnerId,
                        OtherUserName = partner.Name,
                        OtherUserRole = partner.Role.ToString(),
                        Messages = messageDtos,
                        UnreadCount = unreadCount,
                        LastMessageTime = lastMessageTime
                    });
                }

                // Sort by last message time
                conversations = conversations.OrderByDescending(c => c.LastMessageTime).ToList();

                return Ok(conversations);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting conversations");
                return StatusCode(500, new { message = "An error occurred while fetching conversations" });
            }
        }

        [HttpGet("conversation/{otherUserId}")]
        public async Task<ActionResult<ConversationDto>> GetConversation(int otherUserId)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
                {
                    return Unauthorized(new { message = "Invalid token" });
                }

                var otherUser = await _context.Users.FindAsync(otherUserId);
                if (otherUser == null)
                {
                    return NotFound(new { message = "User not found" });
                }

                // Get all messages between current user and the other user
                var messages = await _context.Messages
                    .Include(m => m.Sender)
                    .Include(m => m.Receiver)
                    .Include(m => m.Property)
                    .Where(m => (m.SenderId == userId && m.ReceiverId == otherUserId) ||
                               (m.SenderId == otherUserId && m.ReceiverId == userId))
                    .OrderBy(m => m.Timestamp)
                    .ToListAsync();

                // Mark all messages from the other user as read
                var unreadMessages = messages.Where(m => m.ReceiverId == userId && !m.IsRead).ToList();
                foreach (var message in unreadMessages)
                {
                    message.IsRead = true;
                }
                await _context.SaveChangesAsync();

                var messageDtos = messages.Select(m => new MessageDto
                {
                    Id = m.Id,
                    SenderId = m.SenderId,
                    SenderName = m.Sender.Name,
                    ReceiverId = m.ReceiverId,
                    ReceiverName = m.Receiver.Name,
                    Content = m.Content,
                    Timestamp = m.Timestamp,
                    IsRead = m.IsRead,
                    PropertyId = m.PropertyId,
                    PropertyTitle = m.Property?.Title
                }).ToList();

                var conversation = new ConversationDto
                {
                    OtherUserId = otherUserId,
                    OtherUserName = otherUser.Name,
                    OtherUserRole = otherUser.Role.ToString(),
                    Messages = messageDtos,
                    UnreadCount = 0, // All messages are now read
                    LastMessageTime = messages.Any() ? messages.Max(m => m.Timestamp) : DateTime.MinValue
                };

                return Ok(conversation);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting conversation with user {otherUserId}");
                return StatusCode(500, new { message = "An error occurred while fetching the conversation" });
            }
        }

        [HttpPut("{id}/read")]
        public async Task<IActionResult> MarkMessageAsRead(int id)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
                {
                    return Unauthorized(new { message = "Invalid token" });
                }

                var message = await _context.Messages.FindAsync(id);
                if (message == null)
                {
                    return NotFound(new { message = "Message not found" });
                }

                if (message.ReceiverId != userId)
                {
                    return Forbid("You can only mark messages you received as read");
                }

                message.IsRead = true;
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error marking message {id} as read");
                return StatusCode(500, new { message = "An error occurred while updating the message" });
            }
        }
    }
}

