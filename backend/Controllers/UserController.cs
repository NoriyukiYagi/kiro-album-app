using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using AlbumApp.Data;
using AlbumApp.Models;
using AlbumApp.Models.DTOs;
using AlbumApp.Attributes;
using AlbumApp.Services;

namespace AlbumApp.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UserController : ControllerBase
{
    private readonly AlbumDbContext _context;
    private readonly IAdminService _adminService;
    private readonly ILogger<UserController> _logger;

    public UserController(
        AlbumDbContext context,
        IAdminService adminService,
        ILogger<UserController> logger)
    {
        _context = context;
        _adminService = adminService;
        _logger = logger;
    }

    [HttpGet]
    [AdminOnly]
    public async Task<ActionResult<ApiResponse<List<UserListResponse>>>> GetUsers()
    {
        try
        {
            var users = await _context.Users
                .Include(u => u.MediaFiles)
                .Select(u => new UserListResponse
                {
                    Id = u.Id,
                    Email = u.Email,
                    Name = u.Name,
                    IsAdmin = u.IsAdmin,
                    CreatedAt = u.CreatedAt,
                    LastLoginAt = u.LastLoginAt,
                    MediaFilesCount = u.MediaFiles.Count
                })
                .OrderBy(u => u.Email)
                .ToListAsync();

            return Ok(new ApiResponse<List<UserListResponse>>
            {
                Success = true,
                Data = users,
                Message = "Users retrieved successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving users");
            return StatusCode(500, new ApiResponse<List<UserListResponse>>
            {
                Success = false,
                Error = "INTERNAL_ERROR",
                Message = "An error occurred while retrieving users"
            });
        }
    }

    [HttpGet("{id}")]
    [AdminOnly]
    public async Task<ActionResult<ApiResponse<UserDetailsResponse>>> GetUser(int id)
    {
        try
        {
            var user = await _context.Users
                .Include(u => u.MediaFiles)
                .Where(u => u.Id == id)
                .Select(u => new UserDetailsResponse
                {
                    Id = u.Id,
                    GoogleId = u.GoogleId,
                    Email = u.Email,
                    Name = u.Name,
                    IsAdmin = u.IsAdmin,
                    CreatedAt = u.CreatedAt,
                    LastLoginAt = u.LastLoginAt,
                    MediaFilesCount = u.MediaFiles.Count
                })
                .FirstOrDefaultAsync();

            if (user == null)
            {
                return NotFound(new ApiResponse<UserDetailsResponse>
                {
                    Success = false,
                    Error = "USER_NOT_FOUND",
                    Message = "User not found"
                });
            }

            return Ok(new ApiResponse<UserDetailsResponse>
            {
                Success = true,
                Data = user,
                Message = "User retrieved successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user {UserId}", id);
            return StatusCode(500, new ApiResponse<UserDetailsResponse>
            {
                Success = false,
                Error = "INTERNAL_ERROR",
                Message = "An error occurred while retrieving user"
            });
        }
    }

    [HttpPost]
    [AdminOnly]
    public async Task<ActionResult<ApiResponse<UserDetailsResponse>>> CreateUser([FromBody] CreateUserRequest request)
    {
        try
        {
            // Check if user already exists
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == request.Email);

            if (existingUser != null)
            {
                return BadRequest(new ApiResponse<UserDetailsResponse>
                {
                    Success = false,
                    Error = "USER_EXISTS",
                    Message = "User with this email already exists"
                });
            }

            // Check if email is in admin list from configuration
            var isConfigAdmin = _adminService.IsAdminUser(request.Email);

            var user = new User
            {
                GoogleId = "", // Will be set when user first logs in with Google
                Email = request.Email,
                Name = request.Name,
                IsAdmin = request.IsAdmin || isConfigAdmin,
                CreatedAt = DateTime.UtcNow,
                LastLoginAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var response = new UserDetailsResponse
            {
                Id = user.Id,
                GoogleId = user.GoogleId,
                Email = user.Email,
                Name = user.Name,
                IsAdmin = user.IsAdmin,
                CreatedAt = user.CreatedAt,
                LastLoginAt = user.LastLoginAt,
                MediaFilesCount = 0
            };

            _logger.LogInformation("User {Email} created by admin", user.Email);

            return CreatedAtAction(nameof(GetUser), new { id = user.Id }, new ApiResponse<UserDetailsResponse>
            {
                Success = true,
                Data = response,
                Message = "User created successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating user");
            return StatusCode(500, new ApiResponse<UserDetailsResponse>
            {
                Success = false,
                Error = "INTERNAL_ERROR",
                Message = "An error occurred while creating user"
            });
        }
    }

    [HttpPut("{id}")]
    [AdminOnly]
    public async Task<ActionResult<ApiResponse<UserDetailsResponse>>> UpdateUser(int id, [FromBody] UpdateUserRequest request)
    {
        try
        {
            var user = await _context.Users
                .Include(u => u.MediaFiles)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
            {
                return NotFound(new ApiResponse<UserDetailsResponse>
                {
                    Success = false,
                    Error = "USER_NOT_FOUND",
                    Message = "User not found"
                });
            }

            // Update fields if provided
            if (!string.IsNullOrEmpty(request.Name))
            {
                user.Name = request.Name;
            }

            if (request.IsAdmin.HasValue)
            {
                // Check if email is in admin list from configuration
                var isConfigAdmin = _adminService.IsAdminUser(user.Email);
                user.IsAdmin = request.IsAdmin.Value || isConfigAdmin;
            }

            await _context.SaveChangesAsync();

            var response = new UserDetailsResponse
            {
                Id = user.Id,
                GoogleId = user.GoogleId,
                Email = user.Email,
                Name = user.Name,
                IsAdmin = user.IsAdmin,
                CreatedAt = user.CreatedAt,
                LastLoginAt = user.LastLoginAt,
                MediaFilesCount = user.MediaFiles.Count
            };

            _logger.LogInformation("User {Email} updated by admin", user.Email);

            return Ok(new ApiResponse<UserDetailsResponse>
            {
                Success = true,
                Data = response,
                Message = "User updated successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user {UserId}", id);
            return StatusCode(500, new ApiResponse<UserDetailsResponse>
            {
                Success = false,
                Error = "INTERNAL_ERROR",
                Message = "An error occurred while updating user"
            });
        }
    }

    [HttpDelete("{id}")]
    [AdminOnly]
    public async Task<ActionResult<ApiResponse<object>>> DeleteUser(int id)
    {
        try
        {
            var user = await _context.Users
                .Include(u => u.MediaFiles)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
            {
                return NotFound(new ApiResponse<object>
                {
                    Success = false,
                    Error = "USER_NOT_FOUND",
                    Message = "User not found"
                });
            }

            // Check if user has media files
            if (user.MediaFiles.Any())
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Error = "USER_HAS_MEDIA",
                    Message = "Cannot delete user with existing media files. Please delete media files first."
                });
            }

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            _logger.LogInformation("User {Email} deleted by admin", user.Email);

            return Ok(new ApiResponse<object>
            {
                Success = true,
                Message = "User deleted successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting user {UserId}", id);
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Error = "INTERNAL_ERROR",
                Message = "An error occurred while deleting user"
            });
        }
    }
}