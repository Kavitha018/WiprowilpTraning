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
    public class TestController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<TestController> _logger;

        public TestController(ApplicationDbContext context, ILogger<TestController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet("database-stats")]
        public async Task<IActionResult> GetDatabaseStats()
        {
            try
            {
                var stats = new
                {
                    Users = await _context.Users.CountAsync(),
                    Properties = await _context.Properties.CountAsync(),
                    PropertyImages = await _context.PropertyImages.CountAsync(),
                    Reservations = await _context.Reservations.CountAsync(),
                    Messages = await _context.Messages.CountAsync(),
                    Notifications = await _context.Notifications.CountAsync(),
                    AvailableProperties = await _context.Properties.CountAsync(p => p.IsAvailable),
                    ConfirmedReservations = await _context.Reservations.CountAsync(r => r.Status == ReservationStatus.Confirmed),
                    PendingReservations = await _context.Reservations.CountAsync(r => r.Status == ReservationStatus.Pending)
                };

                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting database stats");
                return StatusCode(500, new { message = "An error occurred while getting database stats" });
            }
        }

        [HttpGet("sample-data")]
        public async Task<IActionResult> GetSampleData()
        {
            try
            {
                var sampleData = new
                {
                    Users = await _context.Users
                        .Select(u => new { u.Id, u.Name, u.Email, u.Role })
                        .Take(5)
                        .ToListAsync(),
                    Properties = await _context.Properties
                        .Include(p => p.Owner)
                        .Select(p => new { p.Id, p.Title, p.Location, p.PricePerNight, OwnerName = p.Owner.Name })
                        .Take(5)
                        .ToListAsync(),
                    Reservations = await _context.Reservations
                        .Include(r => r.User)
                        .Include(r => r.Property)
                        .Select(r => new { 
                            r.Id, 
                            UserName = r.User.Name, 
                            PropertyTitle = r.Property.Title, 
                            r.CheckInDate, 
                            r.CheckOutDate, 
                            r.Status 
                        })
                        .Take(5)
                        .ToListAsync()
                };

                return Ok(sampleData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting sample data");
                return StatusCode(500, new { message = "An error occurred while getting sample data" });
            }
        }

        [HttpPost("test-reservation")]
        [Authorize]
        public async Task<IActionResult> TestReservation([FromBody] TestReservationRequest request)
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

                // Get a random available property
                var property = await _context.Properties
                    .Include(p => p.Owner)
                    .Where(p => p.IsAvailable)
                    .OrderBy(x => Guid.NewGuid())
                    .FirstOrDefaultAsync();

                if (property == null)
                {
                    return BadRequest(new { message = "No available properties found" });
                }

                // Create test reservation
                var reservation = new Reservation
                {
                    UserId = userId,
                    PropertyId = property.Id,
                    CheckInDate = request.CheckInDate ?? DateTime.UtcNow.AddDays(7),
                    CheckOutDate = request.CheckOutDate ?? DateTime.UtcNow.AddDays(10),
                    NumberOfGuests = request.NumberOfGuests ?? 2,
                    TotalAmount = property.PricePerNight * ((request.CheckOutDate ?? DateTime.UtcNow.AddDays(10)) - (request.CheckInDate ?? DateTime.UtcNow.AddDays(7))).Days,
                    Status = ReservationStatus.Pending,
                    SpecialRequests = request.SpecialRequests ?? "Test reservation",
                    CreatedAt = DateTime.UtcNow
                };

                _context.Reservations.Add(reservation);
                await _context.SaveChangesAsync();

                // Create notification for property owner
                var notification = new Notification
                {
                    UserId = property.OwnerId,
                    Message = $"New test reservation request for {property.Title}",
                    Type = NotificationType.ReservationRequest,
                    RelatedEntityId = reservation.Id,
                    RelatedEntityType = "Reservation",
                    CreatedAt = DateTime.UtcNow
                };

                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();

                var result = new
                {
                    Message = "Test reservation created successfully!",
                    Reservation = new
                    {
                        reservation.Id,
                        PropertyTitle = property.Title,
                        PropertyLocation = property.Location,
                        reservation.CheckInDate,
                        reservation.CheckOutDate,
                        reservation.NumberOfGuests,
                        reservation.TotalAmount,
                        reservation.Status,
                        reservation.SpecialRequests
                    },
                    Notification = new
                    {
                        notification.Id,
                        notification.Message,
                        notification.Type,
                        OwnerName = property.Owner.Name
                    }
                };

                _logger.LogInformation($"Test reservation created: {reservation.Id} by user {userId}");
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating test reservation");
                return StatusCode(500, new { message = "An error occurred while creating test reservation" });
            }
        }

        [HttpPost("test-property")]
        [Authorize(Roles = "Owner")]
        public async Task<IActionResult> TestProperty([FromBody] TestPropertyRequest request)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
                {
                    return Unauthorized(new { message = "Invalid token" });
                }

                // Create test property
                var property = new Property
                {
                    OwnerId = userId,
                    Title = request.Title ?? "Test Property",
                    Description = request.Description ?? "This is a test property created for demonstration purposes.",
                    Location = request.Location ?? "Test City, TC",
                    Type = request.Type ?? PropertyType.Apartment,
                    Features = System.Text.Json.JsonSerializer.Serialize(new List<string> { "WiFi", "Air Conditioning", "Kitchen" }),
                    PricePerNight = request.PricePerNight ?? 100.00m,
                    MaxGuests = request.MaxGuests ?? 4,
                    Bedrooms = request.Bedrooms ?? 2,
                    Bathrooms = request.Bathrooms ?? 1,
                    IsAvailable = true,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Properties.Add(property);
                await _context.SaveChangesAsync();

                // Add test images
                var images = new List<PropertyImage>
                {
                    new PropertyImage
                    {
                        PropertyId = property.Id,
                        ImagePath = "https://picsum.photos/800/600?random=" + property.Id,
                        AltText = "Test Property Image 1",
                        IsPrimary = true,
                        CreatedAt = DateTime.UtcNow
                    },
                    new PropertyImage
                    {
                        PropertyId = property.Id,
                        ImagePath = "https://picsum.photos/800/600?random=" + (property.Id + 1),
                        AltText = "Test Property Image 2",
                        IsPrimary = false,
                        CreatedAt = DateTime.UtcNow
                    }
                };

                _context.PropertyImages.AddRange(images);
                await _context.SaveChangesAsync();

                var result = new
                {
                    Message = "Test property created successfully!",
                    Property = new
                    {
                        property.Id,
                        property.Title,
                        property.Description,
                        property.Location,
                        property.Type,
                        property.PricePerNight,
                        property.MaxGuests,
                        property.Bedrooms,
                        property.Bathrooms,
                        property.IsAvailable,
                        property.CreatedAt
                    },
                    Images = images.Select(i => new { i.Id, i.ImagePath, i.AltText, i.IsPrimary })
                };

                _logger.LogInformation($"Test property created: {property.Id} by user {userId}");
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating test property");
                return StatusCode(500, new { message = "An error occurred while creating test property" });
            }
        }

        [HttpPut("confirm-reservation/{id}")]
        [Authorize(Roles = "Owner")]
        public async Task<IActionResult> ConfirmTestReservation(int id)
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
                    return Forbid("You can only confirm reservations for your properties");
                }

                if (reservation.Status != ReservationStatus.Pending)
                {
                    return BadRequest(new { message = "Only pending reservations can be confirmed" });
                }

                reservation.Status = ReservationStatus.Confirmed;
                reservation.ConfirmedAt = DateTime.UtcNow;

                // Create notification for the renter
                var notification = new Notification
                {
                    UserId = reservation.UserId,
                    Message = $"Your test reservation for {reservation.Property.Title} has been confirmed!",
                    Type = NotificationType.ReservationConfirmed,
                    RelatedEntityId = reservation.Id,
                    RelatedEntityType = "Reservation",
                    CreatedAt = DateTime.UtcNow
                };

                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();

                var result = new
                {
                    Message = "Reservation confirmed successfully!",
                    Reservation = new
                    {
                        reservation.Id,
                        PropertyTitle = reservation.Property.Title,
                        UserName = reservation.User.Name,
                        reservation.CheckInDate,
                        reservation.CheckOutDate,
                        reservation.Status,
                        reservation.ConfirmedAt
                    },
                    Notification = new
                    {
                        notification.Id,
                        notification.Message,
                        notification.Type
                    }
                };

                _logger.LogInformation($"Test reservation {id} confirmed by user {userId}");
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error confirming test reservation {id}");
                return StatusCode(500, new { message = "An error occurred while confirming reservation" });
            }
        }

        [HttpGet("health")]
        public IActionResult Health()
        {
            return Ok(new { 
                message = "RentAPlace API is running successfully!", 
                timestamp = DateTime.UtcNow,
                status = "Healthy"
            });
        }

        [HttpGet("crud-demo")]
        public async Task<IActionResult> CrudDemo()
        {
            try
            {
                var demo = new
                {
                    Message = "RentAPlace CRUD Operations Demo",
                    AvailableEndpoints = new
                    {
                        Authentication = new[]
                        {
                            "POST /api/auth/register - Register new user",
                            "POST /api/auth/login - Login user",
                            "GET /api/auth/me - Get current user info"
                        },
                        Properties = new[]
                        {
                            "GET /api/properties - Get all properties (with search/filter)",
                            "GET /api/properties/{id} - Get property by ID",
                            "POST /api/properties - Create property (Owner only)",
                            "PUT /api/properties/{id} - Update property (Owner only)",
                            "DELETE /api/properties/{id} - Delete property (Owner only)",
                            "GET /api/properties/my-properties - Get my properties (Owner only)"
                        },
                        Reservations = new[]
                        {
                            "POST /api/reservations - Create reservation",
                            "GET /api/reservations/{id} - Get reservation by ID",
                            "PUT /api/reservations/{id}/status - Update reservation status (Owner only)",
                            "GET /api/reservations/my-reservations - Get my reservations",
                            "DELETE /api/reservations/{id} - Cancel reservation"
                        },
                        TestEndpoints = new[]
                        {
                            "GET /api/test/database-stats - Get database statistics",
                            "GET /api/test/sample-data - Get sample data",
                            "POST /api/test/test-reservation - Create test reservation",
                            "POST /api/test/test-property - Create test property (Owner only)",
                            "PUT /api/test/confirm-reservation/{id} - Confirm test reservation (Owner only)"
                        }
                    },
                    SampleUsers = new[]
                    {
                        new { Email = "john.smith@email.com", Password = "password123", Role = "Owner" },
                        new { Email = "emily.davis@email.com", Password = "password123", Role = "Renter" }
                    },
                    DatabaseStats = new
                    {
                        Users = await _context.Users.CountAsync(),
                        Properties = await _context.Properties.CountAsync(),
                        Reservations = await _context.Reservations.CountAsync(),
                        AvailableProperties = await _context.Properties.CountAsync(p => p.IsAvailable)
                    }
                };

                return Ok(demo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting CRUD demo info");
                return StatusCode(500, new { message = "An error occurred while getting demo info" });
            }
        }
    }

    public class TestReservationRequest
    {
        public DateTime? CheckInDate { get; set; }
        public DateTime? CheckOutDate { get; set; }
        public int? NumberOfGuests { get; set; }
        public string? SpecialRequests { get; set; }
    }

    public class TestPropertyRequest
    {
        public string? Title { get; set; }
        public string? Description { get; set; }
        public string? Location { get; set; }
        public PropertyType? Type { get; set; }
        public decimal? PricePerNight { get; set; }
        public int? MaxGuests { get; set; }
        public int? Bedrooms { get; set; }
        public int? Bathrooms { get; set; }
    }
}