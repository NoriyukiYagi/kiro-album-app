using System.ComponentModel.DataAnnotations;

namespace AlbumApp.Models;

public class MediaFile
{
    public int Id { get; set; }
    
    [Required]
    [MaxLength(255)]
    public string FileName { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(255)]
    public string OriginalFileName { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(500)]
    public string FilePath { get; set; } = string.Empty;
    
    [MaxLength(500)]
    public string ThumbnailPath { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(100)]
    public string ContentType { get; set; } = string.Empty;
    
    public long FileSize { get; set; }
    
    public DateTime TakenAt { get; set; }
    
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    
    public int UploadedBy { get; set; }
    
    // Navigation property
    public User User { get; set; } = null!;
}