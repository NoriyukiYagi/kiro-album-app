using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using AlbumApp.Models.DTOs;
using AlbumApp.Services;
using AlbumApp.Data;
using Microsoft.EntityFrameworkCore;

namespace AlbumApp.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IGoogleAuthService _googleAuthService;
    private readonly IJwtService _jwtService;
    private readonly AlbumDbContext _context;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        IGoogleAuthService googleAuthService,
        IJwtService jwtService,
        AlbumDbContext context,
        ILogger<AuthController> logger)
    {
        _googleAuthService = googleAuthService;
        _jwtService = jwtService;
        _context = context;
        _logger = logger;
    }

    [HttpPost("google-login")]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> GoogleLogin([FromBody] GoogleLoginRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.IdToken))
            {
                return BadRequest(new ApiResponse<AuthResponse>
                {
                    Success = false,
                    Error = "INVALID_REQUEST",
                    Message = "ID token is required"
                });
            }

            var user = await _googleAuthService.ValidateGoogleTokenAsync(request.IdToken);
            if (user == null)
            {
                return Unauthorized(new ApiResponse<AuthResponse>
                {
                    Success = false,
                    Error = "UNAUTHORIZED",
                    Message = "Invalid Google token or user not authorized"
                });
            }

            var accessToken = _jwtService.GenerateToken(user);
            
            var response = new AuthResponse
            {
                AccessToken = accessToken,
                TokenType = "Bearer",
                ExpiresIn = 3600, // 1 hour
                User = new UserInfo
                {
                    Id = user.Id,
                    Email = user.Email,
                    Name = user.Name,
                    IsAdmin = user.IsAdmin
                }
            };

            _logger.LogInformation("User {Email} logged in successfully", user.Email);

            return Ok(new ApiResponse<AuthResponse>
            {
                Success = true,
                Data = response,
                Message = "Login successful"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during Google login");
            return StatusCode(500, new ApiResponse<AuthResponse>
            {
                Success = false,
                Error = "INTERNAL_ERROR",
                Message = "An error occurred during login"
            });
        }
    }

    [HttpGet("user-info")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<UserInfo>>> GetUserInfo()
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                return Unauthorized(new ApiResponse<UserInfo>
                {
                    Success = false,
                    Error = "INVALID_TOKEN",
                    Message = "Invalid user token"
                });
            }

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return NotFound(new ApiResponse<UserInfo>
                {
                    Success = false,
                    Error = "USER_NOT_FOUND",
                    Message = "User not found"
                });
            }

            var userInfo = new UserInfo
            {
                Id = user.Id,
                Email = user.Email,
                Name = user.Name,
                IsAdmin = user.IsAdmin
            };

            return Ok(new ApiResponse<UserInfo>
            {
                Success = true,
                Data = userInfo,
                Message = "User info retrieved successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user info");
            return StatusCode(500, new ApiResponse<UserInfo>
            {
                Success = false,
                Error = "INTERNAL_ERROR",
                Message = "An error occurred while retrieving user info"
            });
        }
    }

    [HttpPost("logout")]
    [Authorize]
    public ActionResult<ApiResponse<object>> Logout()
    {
        try
        {
            // In a JWT-based system, logout is typically handled client-side
            // by removing the token from storage. We can log the logout event here.
            var userEmail = User.FindFirst(ClaimTypes.Email)?.Value;
            _logger.LogInformation("User {Email} logged out", userEmail);

            return Ok(new ApiResponse<object>
            {
                Success = true,
                Message = "Logout successful"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during logout");
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Error = "INTERNAL_ERROR",
                Message = "An error occurred during logout"
            });
        }
    }

    [HttpGet("validate-token")]
    [Authorize]
    public ActionResult<ApiResponse<object>> ValidateToken()
    {
        return Ok(new ApiResponse<object>
        {
            Success = true,
            Message = "Token is valid"
        });
    }
}