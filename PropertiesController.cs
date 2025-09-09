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
    public class PropertiesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<PropertiesController> _logger;

        public PropertiesController(ApplicationDbContext context, ILogger<PropertiesController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<PropertyDto>>> GetProperties([FromQuery] PropertySearchRequest request)
        {
            try
            {
                var query = _context.Properties
                    .Include(p => p.Owner)
                    .Include(p => p.Images)
                    .Where(p => p.IsAvailable);

                // Apply filters
                if (!string.IsNullOrEmpty(request.Location))
                {
                    query = query.Where(p => p.Location.Contains(request.Location));
                }

                if (!string.IsNullOrEmpty(request.Type) && Enum.TryParse<PropertyType>(request.Type, true, out var propertyType))
                {
                    query = query.Where(p => p.Type == propertyType);
                }

                if (request.MaxGuests.HasValue)
                {
                    query = query.Where(p => p.MaxGuests >= request.MaxGuests.Value);
                }

                if (request.MinPrice.HasValue)
                {
                    query = query.Where(p => p.PricePerNight >= request.MinPrice.Value);
                }

                if (request.MaxPrice.HasValue)
                {
                    query = query.Where(p => p.PricePerNight <= request.MaxPrice.Value);
                }

                // Check availability for date range
                if (request.CheckInDate.HasValue && request.CheckOutDate.HasValue)
                {
                    query = query.Where(p => !p.Reservations.Any(r => 
                        r.Status == ReservationStatus.Confirmed &&
                        ((r.CheckInDate <= request.CheckInDate.Value && r.CheckOutDate > request.CheckInDate.Value) ||
                         (r.CheckInDate < request.CheckOutDate.Value && r.CheckOutDate >= request.CheckOutDate.Value) ||
                         (r.CheckInDate >= request.CheckInDate.Value && r.CheckOutDate <= request.CheckOutDate.Value))));
                }

                // Apply sorting
                query = request.SortBy?.ToLower() switch
                {
                    "price" => request.SortOrder?.ToLower() == "asc" ? query.OrderBy(p => p.PricePerNight) : query.OrderByDescending(p => p.PricePerNight),
                    "createdat" => request.SortOrder?.ToLower() == "asc" ? query.OrderBy(p => p.CreatedAt) : query.OrderByDescending(p => p.CreatedAt),
                    _ => query.OrderByDescending(p => p.CreatedAt)
                };

                // Apply pagination
                var totalCount = await query.CountAsync();
                var properties = await query
                    .Skip((request.Page - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .ToListAsync();

                var propertyDtos = properties.Select(p => new PropertyDto
                {
                    Id = p.Id,
                    OwnerId = p.OwnerId,
                    OwnerName = p.Owner.Name,
                    Title = p.Title,
                    Description = p.Description,
                    Location = p.Location,
                    Type = p.Type.ToString(),
                    Features = string.IsNullOrEmpty(p.Features) ? new List<string>() : System.Text.Json.JsonSerializer.Deserialize<List<string>>(p.Features) ?? new List<string>(),
                    PricePerNight = p.PricePerNight,
                    MaxGuests = p.MaxGuests,
                    Bedrooms = p.Bedrooms,
                    Bathrooms = p.Bathrooms,
                    IsAvailable = p.IsAvailable,
                    CreatedAt = p.CreatedAt,
                    ImageUrls = p.Images.Select(i => i.ImagePath).ToList(),
                    PrimaryImageUrl = p.Images.FirstOrDefault(i => i.IsPrimary)?.ImagePath ?? p.Images.FirstOrDefault()?.ImagePath ?? ""
                }).ToList();

                return Ok(new { properties = propertyDtos, totalCount, page = request.Page, pageSize = request.PageSize });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting properties");
                return StatusCode(500, new { message = "An error occurred while fetching properties" });
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<PropertyDto>> GetProperty(int id)
        {
            try
            {
                var property = await _context.Properties
                    .Include(p => p.Owner)
                    .Include(p => p.Images)
                    .FirstOrDefaultAsync(p => p.Id == id);

                if (property == null)
                {
                    return NotFound(new { message = "Property not found" });
                }

                var propertyDto = new PropertyDto
                {
                    Id = property.Id,
                    OwnerId = property.OwnerId,
                    OwnerName = property.Owner.Name,
                    Title = property.Title,
                    Description = property.Description,
                    Location = property.Location,
                    Type = property.Type.ToString(),
                    Features = string.IsNullOrEmpty(property.Features) ? new List<string>() : System.Text.Json.JsonSerializer.Deserialize<List<string>>(property.Features) ?? new List<string>(),
                    PricePerNight = property.PricePerNight,
                    MaxGuests = property.MaxGuests,
                    Bedrooms = property.Bedrooms,
                    Bathrooms = property.Bathrooms,
                    IsAvailable = property.IsAvailable,
                    CreatedAt = property.CreatedAt,
                    ImageUrls = property.Images.Select(i => i.ImagePath).ToList(),
                    PrimaryImageUrl = property.Images.FirstOrDefault(i => i.IsPrimary)?.ImagePath ?? property.Images.FirstOrDefault()?.ImagePath ?? ""
                };

                return Ok(propertyDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting property {id}");
                return StatusCode(500, new { message = "An error occurred while fetching the property" });
            }
        }

        [HttpPost]
        [Authorize(Roles = "Owner")]
        public async Task<ActionResult<PropertyDto>> CreateProperty(CreatePropertyRequest request)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
                {
                    return Unauthorized(new { message = "Invalid token" });
                }

                if (!Enum.TryParse<PropertyType>(request.Type, true, out var propertyType))
                {
                    return BadRequest(new { message = "Invalid property type" });
                }

                var property = new Property
                {
                    OwnerId = userId,
                    Title = request.Title,
                    Description = request.Description,
                    Location = request.Location,
                    Type = propertyType,
                    Features = System.Text.Json.JsonSerializer.Serialize(request.Features),
                    PricePerNight = request.PricePerNight,
                    MaxGuests = request.MaxGuests,
                    Bedrooms = request.Bedrooms,
                    Bathrooms = request.Bathrooms,
                    IsAvailable = true,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Properties.Add(property);
                await _context.SaveChangesAsync();

                // Load the property with owner and images for response
                await _context.Entry(property)
                    .Reference(p => p.Owner)
                    .LoadAsync();

                var propertyDto = new PropertyDto
                {
                    Id = property.Id,
                    OwnerId = property.OwnerId,
                    OwnerName = property.Owner.Name,
                    Title = property.Title,
                    Description = property.Description,
                    Location = property.Location,
                    Type = property.Type.ToString(),
                    Features = request.Features,
                    PricePerNight = property.PricePerNight,
                    MaxGuests = property.MaxGuests,
                    Bedrooms = property.Bedrooms,
                    Bathrooms = property.Bathrooms,
                    IsAvailable = property.IsAvailable,
                    CreatedAt = property.CreatedAt,
                    ImageUrls = new List<string>(),
                    PrimaryImageUrl = ""
                };

                _logger.LogInformation($"Property created successfully: {property.Title} by user {userId}");
                return CreatedAtAction(nameof(GetProperty), new { id = property.Id }, propertyDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating property");
                return StatusCode(500, new { message = "An error occurred while creating the property" });
            }
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Owner")]
        public async Task<IActionResult> UpdateProperty(int id, UpdatePropertyRequest request)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
                {
                    return Unauthorized(new { message = "Invalid token" });
                }

                var property = await _context.Properties.FindAsync(id);
                if (property == null)
                {
                    return NotFound(new { message = "Property not found" });
                }

                if (property.OwnerId != userId)
                {
                    return Forbid("You can only update your own properties");
                }

                // Update only provided fields
                if (!string.IsNullOrEmpty(request.Title))
                    property.Title = request.Title;
                if (!string.IsNullOrEmpty(request.Description))
                    property.Description = request.Description;
                if (!string.IsNullOrEmpty(request.Location))
                    property.Location = request.Location;
                if (!string.IsNullOrEmpty(request.Type) && Enum.TryParse<PropertyType>(request.Type, true, out var propertyType))
                    property.Type = propertyType;
                if (request.Features != null)
                    property.Features = System.Text.Json.JsonSerializer.Serialize(request.Features);
                if (request.PricePerNight.HasValue)
                    property.PricePerNight = request.PricePerNight.Value;
                if (request.MaxGuests.HasValue)
                    property.MaxGuests = request.MaxGuests.Value;
                if (request.Bedrooms.HasValue)
                    property.Bedrooms = request.Bedrooms.Value;
                if (request.Bathrooms.HasValue)
                    property.Bathrooms = request.Bathrooms.Value;
                if (request.IsAvailable.HasValue)
                    property.IsAvailable = request.IsAvailable.Value;

                await _context.SaveChangesAsync();

                _logger.LogInformation($"Property updated successfully: {property.Title} by user {userId}");
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating property {id}");
                return StatusCode(500, new { message = "An error occurred while updating the property" });
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Owner")]
        public async Task<IActionResult> DeleteProperty(int id)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
                {
                    return Unauthorized(new { message = "Invalid token" });
                }

                var property = await _context.Properties.FindAsync(id);
                if (property == null)
                {
                    return NotFound(new { message = "Property not found" });
                }

                if (property.OwnerId != userId)
                {
                    return Forbid("You can only delete your own properties");
                }

                _context.Properties.Remove(property);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Property deleted successfully: {property.Title} by user {userId}");
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting property {id}");
                return StatusCode(500, new { message = "An error occurred while deleting the property" });
            }
        }

        [HttpGet("my-properties")]
        [Authorize(Roles = "Owner")]
        public async Task<ActionResult<IEnumerable<PropertyDto>>> GetMyProperties()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
                {
                    return Unauthorized(new { message = "Invalid token" });
                }

                var properties = await _context.Properties
                    .Include(p => p.Owner)
                    .Include(p => p.Images)
                    .Where(p => p.OwnerId == userId)
                    .OrderByDescending(p => p.CreatedAt)
                    .ToListAsync();

                var propertyDtos = properties.Select(p => new PropertyDto
                {
                    Id = p.Id,
                    OwnerId = p.OwnerId,
                    OwnerName = p.Owner.Name,
                    Title = p.Title,
                    Description = p.Description,
                    Location = p.Location,
                    Type = p.Type.ToString(),
                    Features = string.IsNullOrEmpty(p.Features) ? new List<string>() : System.Text.Json.JsonSerializer.Deserialize<List<string>>(p.Features) ?? new List<string>(),
                    PricePerNight = p.PricePerNight,
                    MaxGuests = p.MaxGuests,
                    Bedrooms = p.Bedrooms,
                    Bathrooms = p.Bathrooms,
                    IsAvailable = p.IsAvailable,
                    CreatedAt = p.CreatedAt,
                    ImageUrls = p.Images.Select(i => i.ImagePath).ToList(),
                    PrimaryImageUrl = p.Images.FirstOrDefault(i => i.IsPrimary)?.ImagePath ?? p.Images.FirstOrDefault()?.ImagePath ?? ""
                }).ToList();

                return Ok(propertyDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user properties");
                return StatusCode(500, new { message = "An error occurred while fetching your properties" });
            }
        }
    }
}

