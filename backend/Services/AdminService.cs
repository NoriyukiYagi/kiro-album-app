using AlbumApp.Models;

namespace AlbumApp.Services;

public interface IAdminService
{
    bool IsAdminUser(string email);
    Task<bool> IsUserAdminAsync(int userId);
    List<string> GetAdminEmails();
}

public class AdminService : IAdminService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<AdminService> _logger;

    public AdminService(IConfiguration configuration, ILogger<AdminService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public bool IsAdminUser(string email)
    {
        var adminUsers = _configuration.GetSection("AdminUsers").Get<List<string>>() ?? new List<string>();
        return adminUsers.Contains(email, StringComparer.OrdinalIgnoreCase);
    }

    public async Task<bool> IsUserAdminAsync(int userId)
    {
        // This method can be used if we need to check admin status from database
        // For now, we rely on the IsAdmin flag in the User entity
        return await Task.FromResult(true); // Placeholder - actual implementation would check database
    }

    public List<string> GetAdminEmails()
    {
        return _configuration.GetSection("AdminUsers").Get<List<string>>() ?? new List<string>();
    }
}