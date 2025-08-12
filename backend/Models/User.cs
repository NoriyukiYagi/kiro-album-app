using System.ComponentModel.DataAnnotations;

namespace AlbumApp.Models;

public class User
{
    public int Id { get; set; }
    
    [Required]
    [MaxLength(255)]
    public string GoogleId { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(255)]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(255)]
    public string Name { get; set; } = string.Empty;
    
    public bool IsAdmin { get; set; } = false;
    
    private DateTime _createdAt = DateTime.UtcNow;
    public DateTime CreatedAt 
    { 
        get => _createdAt;
        set => _createdAt = value.Kind == DateTimeKind.Unspecified 
            ? DateTime.SpecifyKind(value, DateTimeKind.Utc) 
            : value.ToUniversalTime();
    }
    
    private DateTime _lastLoginAt = DateTime.UtcNow;
    public DateTime LastLoginAt 
    { 
        get => _lastLoginAt;
        set => _lastLoginAt = value.Kind == DateTimeKind.Unspecified 
            ? DateTime.SpecifyKind(value, DateTimeKind.Utc) 
            : value.ToUniversalTime();
    }
    
    // Navigation property
    public ICollection<MediaFile> MediaFiles { get; set; } = new List<MediaFile>();
}