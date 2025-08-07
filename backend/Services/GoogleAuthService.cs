using Google.Apis.Auth;
using AlbumApp.Models;
using AlbumApp.Data;
using Microsoft.EntityFrameworkCore;

namespace AlbumApp.Services;

public interface IGoogleAuthService
{
    Task<User?> ValidateGoogleTokenAsync(string idToken);
}

public class GoogleAuthService : IGoogleAuthService
{
    private readonly IConfiguration _configuration;
    private readonly AlbumDbContext _context;
    private readonly ILogger<GoogleAuthService> _logger;

    public GoogleAuthService(IConfiguration configuration, AlbumDbContext context, ILogger<GoogleAuthService> logger)
    {
        _configuration = configuration;
        _context = context;
        _logger = logger;
    }

    public async Task<User?> ValidateGoogleTokenAsync(string idToken)
    {
        try
        {
            var clientId = _configuration["GoogleOAuth:ClientId"];
            if (string.IsNullOrEmpty(clientId))
            {
                _logger.LogError("Google OAuth ClientId is not configured");
                return null;
            }

            // Validate the Google ID token
            var payload = await GoogleJsonWebSignature.ValidateAsync(idToken, new GoogleJsonWebSignature.ValidationSettings
            {
                Audience = new[] { clientId }
            });

            if (payload == null)
            {
                _logger.LogWarning("Invalid Google ID token");
                return null;
            }

            // Check if user exists in database
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.GoogleId == payload.Subject);

            if (existingUser != null)
            {
                // Update last login time
                existingUser.LastLoginAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                return existingUser;
            }

            // Check if this user is in the admin list
            var adminUsers = _configuration.GetSection("AdminUsers").Get<string[]>() ?? Array.Empty<string>();
            var isAdmin = adminUsers.Contains(payload.Email, StringComparer.OrdinalIgnoreCase);

            // For non-admin users, check if they are already registered
            if (!isAdmin)
            {
                var registeredUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == payload.Email);
                
                if (registeredUser == null)
                {
                    _logger.LogWarning("User {Email} is not authorized to access the application", payload.Email);
                    return null;
                }
            }

            // Create new user or update existing user with Google ID
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == payload.Email);

            if (user == null)
            {
                // Create new user (only for admins)
                user = new User
                {
                    GoogleId = payload.Subject,
                    Email = payload.Email,
                    Name = payload.Name ?? payload.Email,
                    IsAdmin = isAdmin,
                    CreatedAt = DateTime.UtcNow,
                    LastLoginAt = DateTime.UtcNow
                };

                _context.Users.Add(user);
            }
            else
            {
                // Update existing user with Google ID
                user.GoogleId = payload.Subject;
                user.Name = payload.Name ?? user.Name;
                user.LastLoginAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            return user;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating Google token");
            return null;
        }
    }
}