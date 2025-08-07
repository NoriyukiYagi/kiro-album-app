using System.ComponentModel.DataAnnotations;

namespace AlbumApp.Models.DTOs;

public class MediaUploadResponseDto
{
    public int Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string OriginalFileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public DateTime TakenAt { get; set; }
    public DateTime UploadedAt { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class MediaFileDto
{
    public int Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string OriginalFileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public DateTime TakenAt { get; set; }
    public DateTime UploadedAt { get; set; }
    public string ThumbnailPath { get; set; } = string.Empty;
}

public class FileUploadValidationResult
{
    public bool IsValid { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
    public string ErrorCode { get; set; } = string.Empty;
}