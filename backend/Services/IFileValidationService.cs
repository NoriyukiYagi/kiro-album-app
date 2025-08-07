using AlbumApp.Models.DTOs;

namespace AlbumApp.Services;

public interface IFileValidationService
{
    FileUploadValidationResult ValidateFile(IFormFile file);
    bool IsImageFile(string fileName);
    bool IsVideoFile(string fileName);
}