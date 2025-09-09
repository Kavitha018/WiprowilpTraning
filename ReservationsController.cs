using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RentAPlace.Core.DTOs;
using RentAPlace.Core.Models;
using RentAPlace.Database;
using RentAPlace.Infrastructure.Services;
using System.Security.Claims;

namespace RentAPlace.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ReservationsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;
        private readonly ILogger<ReservationsController> _logger;

        public ReservationsController(ApplicationDbContext context, IEmailService emailService, ILogger<ReservationsController> logger)
        {
            _context = context;
            _emailService = emailService;
            _logger = logger;
        }

        [HttpPost]
        public async Task<ActionResult<ReservationDto>> CreateReservation(CreateReservationRequest request)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
                {
                    return Unauthorized(new { message = "Invalid token" });
                }

                // Get user role
                var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
                if (userRole == "Owner")
                {
                    return BadRequest(new { message = "Owners cannot make reservations" });
                }

                // Validate property exists and is available
                var property = await _context.Properties
                    .Include(p => p.Owner)
                    .FirstOrDefaultAsync(p => p.Id == request.PropertyId);

                if (property == null)
                {
                    return NotFound(new { message = "Property not found" });
                }

                if (!property.IsAvailable)
                {
                    return BadRequest(new { message = "Property is not available" });
                }

                // Check if property can accommodate the number of guests
                if (request.NumberOfGuests > property.MaxGuests)
                {
                    return BadRequest(new { message = $"Property can only accommodate {property.MaxGuests} guests" });
                }

                // Check for date conflicts
                var hasConflict = await _context.Reservations
                    .AnyAsync(r => r.PropertyId == request.PropertyId &&
                                  r.Status == ReservationStatus.Confirmed &&
                                  ((r.CheckInDate <= request.CheckInDate && r.CheckOutDate > request.CheckInDate) ||
                                   (r.CheckInDate < request.CheckOutDate && r.CheckOutDate >= request.CheckOutDate) ||
                                   (r.CheckInDate >= request.CheckInDate && r.CheckOutDate <= request.CheckOutDate)));

                if (hasConflict)
                {
                    return BadRequest(new { message = "Property is not available for the selected dates" });
                }

                // Calculate total amount
                var nights = (request.CheckOutDate - request.CheckInDate).Days;
                var totalAmount = property.PricePerNight * nights;

                // Create reservation
                var reservation = new Reservation
                {
                    UserId = userId,
                    PropertyId = request.PropertyId,
                    CheckInDate = request.CheckInDate,
                    CheckOutDate = request.CheckOutDate,
                    NumberOfGuests = request.NumberOfGuests,
                    TotalAmount = totalAmount,
                    Status = ReservationStatus.Pending,
                    SpecialRequests = request.SpecialRequests,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Reservations.Add(reservation);
                await _context.SaveChangesAsync();

                // Create notification for property owner
                var notification = new Notification
                {
                    UserId = property.OwnerId,
                    Message = $"New reservation request for {property.Title} from {request.CheckInDate:MMM dd} to {request.CheckOutDate:MMM dd}",
                    Type = NotificationType.ReservationRequest,
                    RelatedEntityId = reservation.Id,
                    RelatedEntityType = "Reservation",
                    CreatedAt = DateTime.UtcNow
                };

                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();

                // Send email notification to owner
                try
                {
                    await _emailService.SendReservationNotificationAsync(
                        property.Owner.Email,
                        User.FindFirst(ClaimTypes.Name)?.Value ?? "Guest",
                        property.Title,
                        request.CheckInDate,
                        request.CheckOutDate);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send email notification to property owner");
                }

                // Load reservation with related data for response
                await _context.Entry(reservation)
                    .Reference(r => r.User)
                    .LoadAsync();
                await _context.Entry(reservation)
                    .Reference(r => r.Property)
                    .LoadAsync();

                var reservationDto = new ReservationDto
                {
                    Id = reservation.Id,
                    UserId = reservation.UserId,
                    UserName = reservation.User.Name,
                    UserEmail = reservation.User.Email,
                    PropertyId = reservation.PropertyId,
                    PropertyTitle = reservation.Property.Title,
                    PropertyLocation = reservation.Property.Location,
                    CheckInDate = reservation.CheckInDate,
                    CheckOutDate = reservation.CheckOutDate,
                    NumberOfGuests = reservation.NumberOfGuests,
                    TotalAmount = reservation.TotalAmount,
                    Status = reservation.Status.ToString(),
                    SpecialRequests = reservation.SpecialRequests,
                    CreatedAt = reservation.CreatedAt,
                    ConfirmedAt = reservation.ConfirmedAt
                };

                _logger.LogInformation($"Reservation created successfully: {reservation.Id} by user {userId}");
                return CreatedAtAction(nameof(GetReservation), new { id = reservation.Id }, reservationDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating reservation");
                return StatusCode(500, new { message = "An error occurred while creating the reservation" });
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ReservationDto>> GetReservation(int id)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
                {
                    return Unauthorized(new { message = "Invalid token" });
                }

                var reservation = await _context.Reservations
                    .Include(r => r.User)
                    .Include(r => r.Property)
                    .ThenInclude(p => p.Owner)
                    .FirstOrDefaultAsync(r => r.Id == id);

                if (reservation == null)
                {
                    return NotFound(new { message = "Reservation not found" });
                }

                // Check if user has access to this reservation
                var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
                if (userRole != "Owner" && reservation.UserId != userId)
                {
                    return Forbid("You can only view your own reservations");
                }

                if (userRole == "Owner" && reservation.Property.OwnerId != userId)
                {
                    return Forbid("You can only view reservations for your properties");
                }

                var reservationDto = new ReservationDto
                {
                    Id = reservation.Id,
                    UserId = reservation.UserId,
                    UserName = reservation.User.Name,
                    UserEmail = reservation.User.Email,
                    PropertyId = reservation.PropertyId,
                    PropertyTitle = reservation.Property.Title,
                    PropertyLocation = reservation.Property.Location,
                    CheckInDate = reservation.CheckInDate,
                    CheckOutDate = reservation.CheckOutDate,
                    NumberOfGuests = reservation.NumberOfGuests,
                    TotalAmount = reservation.TotalAmount,
                    Status = reservation.Status.ToString(),
                    SpecialRequests = reservation.SpecialRequests,
                    CreatedAt = reservation.CreatedAt,
                    ConfirmedAt = reservation.ConfirmedAt
                };

                return Ok(reservationDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting reservation {id}");
                return StatusCode(500, new { message = "An error occurred while fetching the reservation" });
            }
        }

        [HttpPut("{id}/status")]
        [Authorize(Roles = "Owner")]
        public async Task<IActionResult> UpdateReservationStatus(int id, UpdateReservationStatusRequest request)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
                {
                    return Unauthorized(new { message = "Invalid token" });
                }

                var reservation = await _context.Reservations
                    .Include(r => r.User)
                    .Include(r => r.Property)
                    .ThenInclude(p => p.Owner)
                    .FirstOrDefaultAsync(r => r.Id == id);

                if (reservation == null)
                {
                    return NotFound(new { message = "Reservation not found" });
                }

                if (reservation.Property.OwnerId != userId)
                {
                    return Forbid("You can only update reservations for your properties");
                }

                if (reservation.Status != ReservationStatus.Pending)
                {
                    return BadRequest(new { message = "Only pending reservations can be updated" });
                }

                if (!Enum.TryParse<ReservationStatus>(request.Status, true, out var newStatus))
                {
                    return BadRequest(new { message = "Invalid status. Must be 'Confirmed' or 'Rejected'" });
                }

                if (newStatus != ReservationStatus.Confirmed && newStatus != ReservationStatus.Rejected)
                {
                    return BadRequest(new { message = "Status must be 'Confirmed' or 'Rejected'" });
                }

                reservation.Status = newStatus;
                if (newStatus == ReservationStatus.Confirmed)
                {
                    reservation.ConfirmedAt = DateTime.UtcNow;
                }

                // Create notification for the renter
                var notification = new Notification
                {
                    UserId = reservation.UserId,
                    Message = $"Your reservation for {reservation.Property.Title} has been {newStatus.ToString().ToLower()}",
                    Type = newStatus == ReservationStatus.Confirmed ? NotificationType.ReservationConfirmed : NotificationType.ReservationRejected,
                    RelatedEntityId = reservation.Id,
                    RelatedEntityType = "Reservation",
                    CreatedAt = DateTime.UtcNow
                };

                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();

                // Send email notification to renter
                try
                {
                    await _emailService.SendReservationConfirmationAsync(
                        reservation.User.Email,
                        reservation.Property.Title,
                        reservation.CheckInDate,
                        reservation.CheckOutDate,
                        newStatus.ToString());
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send email notification to renter");
                }

                _logger.LogInformation($"Reservation {id} status updated to {newStatus} by user {userId}");
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating reservation {id} status");
                return StatusCode(500, new { message = "An error occurred while updating the reservation" });
            }
        }

        [HttpGet("my-reservations")]
        public async Task<ActionResult<IEnumerable<ReservationDto>>> GetMyReservations()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
                {
                    return Unauthorized(new { message = "Invalid token" });
                }

                var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
                IQueryable<Reservation> query;

                if (userRole == "Owner")
                {
                    // Get reservations for owner's properties
                    query = _context.Reservations
                        .Include(r => r.User)
                        .Include(r => r.Property)
                        .Where(r => r.Property.OwnerId == userId);
                }
                else
                {
                    // Get user's own reservations
                    query = _context.Reservations
                        .Include(r => r.User)
                        .Include(r => r.Property)
                        .Where(r => r.UserId == userId);
                }

                var reservations = await query
                    .OrderByDescending(r => r.CreatedAt)
                    .ToListAsync();

                var reservationDtos = reservations.Select(r => new ReservationDto
                {
                    Id = r.Id,
                    UserId = r.UserId,
                    UserName = r.User.Name,
                    UserEmail = r.User.Email,
                    PropertyId = r.PropertyId,
                    PropertyTitle = r.Property.Title,
                    PropertyLocation = r.Property.Location,
                    CheckInDate = r.CheckInDate,
                    CheckOutDate = r.CheckOutDate,
                    NumberOfGuests = r.NumberOfGuests,
                    TotalAmount = r.TotalAmount,
                    Status = r.Status.ToString(),
                    SpecialRequests = r.SpecialRequests,
                    CreatedAt = r.CreatedAt,
                    ConfirmedAt = r.ConfirmedAt
                }).ToList();

                return Ok(reservationDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user reservations");
                return StatusCode(500, new { message = "An error occurred while fetching reservations" });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> CancelReservation(int id)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
                {
                    return Unauthorized(new { message = "Invalid token" });
                }

                var reservation = await _context.Reservations
                    .Include(r => r.User)
                    .Include(r => r.Property)
                    .FirstOrDefaultAsync(r => r.Id == id);

                if (reservation == null)
                {
                    return NotFound(new { message = "Reservation not found" });
                }

                if (reservation.UserId != userId)
                {
                    return Forbid("You can only cancel your own reservations");
                }

                if (reservation.Status == ReservationStatus.Cancelled)
                {
                    return BadRequest(new { message = "Reservation is already cancelled" });
                }

                if (reservation.Status == ReservationStatus.Completed)
                {
                    return BadRequest(new { message = "Cannot cancel a completed reservation" });
                }

                reservation.Status = ReservationStatus.Cancelled;
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Reservation {id} cancelled by user {userId}");
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error cancelling reservation {id}");
                return StatusCode(500, new { message = "An error occurred while cancelling the reservation" });
            }
        }
    }
}

