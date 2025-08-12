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
    
    private DateTime _takenAt;
    public DateTime TakenAt 
    { 
        get => _takenAt;
        set => _takenAt = value.Kind == DateTimeKind.Unspecified 
            ? DateTime.SpecifyKind(value, DateTimeKind.Utc) 
            : value.ToUniversalTime();
    }
    
    private DateTime _uploadedAt = DateTime.UtcNow;
    public DateTime UploadedAt 
    { 
        get => _uploadedAt;
        set => _uploadedAt = value.Kind == DateTimeKind.Unspecified 
            ? DateTime.SpecifyKind(value, DateTimeKind.Utc) 
            : value.ToUniversalTime();
    }
    
    public int UploadedBy { get; set; }
    
    // Navigation property
    public User User { get; set; } = null!;
}