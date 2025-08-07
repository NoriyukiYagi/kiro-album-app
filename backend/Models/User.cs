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
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime LastLoginAt { get; set; } = DateTime.UtcNow;
    
    // Navigation property
    public ICollection<MediaFile> MediaFiles { get; set; } = new List<MediaFile>();
}