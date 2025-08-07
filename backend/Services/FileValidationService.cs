using AlbumApp.Models.DTOs;
using Microsoft.Extensions.Options;

namespace AlbumApp.Services;

public class FileValidationService : IFileValidationService
{
    private readonly FileStorageOptions _options;
    
    public FileValidationService(IOptions<FileStorageOptions> options)
    {
        _options = options.Value;
    }
    
    public FileUploadValidationResult ValidateFile(IFormFile file)
    {
        var result = new FileUploadValidationResult { IsValid = true };
        
        // Check if file is null or empty
        if (file == null || file.Length == 0)
        {
            result.IsValid = false;
            result.ErrorCode = "EMPTY_FILE";
            result.ErrorMessage = "ファイルが選択されていません";
            return result;
        }
        
        // Check file size
        if (file.Length > _options.MaxFileSizeBytes)
        {
            result.IsValid = false;
            result.ErrorCode = "INVALID_FILE_SIZE";
            result.ErrorMessage = $"ファイルサイズが上限を超えています。最大{_options.MaxFileSizeBytes / (1024 * 1024)}MBまでアップロード可能です";
            return result;
        }
        
        // Check file extension
        var fileExtension = Path.GetExtension(file.FileName)?.ToLowerInvariant()?.TrimStart('.');
        if (string.IsNullOrEmpty(fileExtension) || !_options.AllowedExtensions.Contains(fileExtension))
        {
            result.IsValid = false;
            result.ErrorCode = "INVALID_FILE_EXTENSION";
            result.ErrorMessage = $"許可されていないファイル形式です。対応形式: {string.Join(", ", _options.AllowedExtensions.Select(ext => ext.ToUpperInvariant()))}";
            return result;
        }
        
        return result;
    }
    
    public bool IsImageFile(string fileName)
    {
        var extension = Path.GetExtension(fileName)?.ToLowerInvariant()?.TrimStart('.');
        return extension != null && new[] { "jpg", "jpeg", "png", "heic" }.Contains(extension);
    }
    
    public bool IsVideoFile(string fileName)
    {
        var extension = Path.GetExtension(fileName)?.ToLowerInvariant()?.TrimStart('.');
        return extension != null && new[] { "mp4", "mov" }.Contains(extension);
    }
}

public class FileStorageOptions
{
    public long MaxFileSizeBytes { get; set; }
    public string[] AllowedExtensions { get; set; } = Array.Empty<string>();
    public string PictureDirectory { get; set; } = string.Empty;
    public string ThumbnailDirectory { get; set; } = string.Empty;
}