using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using AlbumApp.Models;
using AlbumApp.Models.DTOs;
using AlbumApp.Data;
using AlbumApp.Services;
using System.Security.Claims;

namespace AlbumApp.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MediaController : ControllerBase
{
    private readonly AlbumDbContext _context;
    private readonly IFileValidationService _fileValidationService;
    private readonly IMetadataService _metadataService;
    private readonly IFileStorageService _fileStorageService;
    private readonly ILogger<MediaController> _logger;
    
    public MediaController(
        AlbumDbContext context,
        IFileValidationService fileValidationService,
        IMetadataService metadataService,
        IFileStorageService fileStorageService,
        ILogger<MediaController> logger)
    {
        _context = context;
        _fileValidationService = fileValidationService;
        _metadataService = metadataService;
        _fileStorageService = fileStorageService;
        _logger = logger;
    }
    
    [HttpPost("upload")]
    public async Task<ActionResult<MediaUploadResponseDto>> UploadFile(IFormFile file)
    {
        try
        {
            // Get current user ID from JWT token
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                _logger.LogWarning("Invalid user ID in token");
                return Unauthorized(new { error = new { code = "INVALID_USER", message = "無効なユーザーです" } });
            }
            
            // Validate file
            var validationResult = _fileValidationService.ValidateFile(file);
            if (!validationResult.IsValid)
            {
                _logger.LogWarning("File validation failed: {ErrorCode} - {ErrorMessage}", 
                    validationResult.ErrorCode, validationResult.ErrorMessage);
                return BadRequest(new { 
                    error = new { 
                        code = validationResult.ErrorCode, 
                        message = validationResult.ErrorMessage 
                    } 
                });
            }
            
            // Generate unique filename
            var fileExtension = Path.GetExtension(file.FileName);
            var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";
            
            // Save file to temporary location first for metadata extraction
            var tempFilePath = Path.Combine(Path.GetTempPath(), uniqueFileName);
            using (var stream = new FileStream(tempFilePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }
            
            try
            {
                // Extract metadata to get the taken date
                var dateTaken = await _metadataService.ExtractDateTakenAsync(tempFilePath, file.ContentType);
                
                // Use extracted date or fallback to upload date
                var takenAt = dateTaken ?? DateTime.UtcNow;
                
                // Save file using FileStorageService with date-based organization
                var relativePath = await _fileStorageService.SaveFileAsync(tempFilePath, uniqueFileName, takenAt);
                var fullPath = _fileStorageService.GetFullPath(relativePath);
                
                _logger.LogInformation("File saved with metadata-based organization: {RelativePath}, TakenAt: {TakenAt}", 
                    relativePath, takenAt);
            
                // Create MediaFile entity
                var mediaFile = new MediaFile
                {
                    FileName = uniqueFileName,
                    OriginalFileName = file.FileName,
                    FilePath = relativePath, // Store relative path instead of full path
                    ContentType = file.ContentType,
                    FileSize = file.Length,
                    TakenAt = takenAt,
                    UploadedAt = DateTime.UtcNow,
                    UploadedBy = userId,
                    ThumbnailPath = "" // Will be set in task 7 (thumbnail generation)
                };
            
                // Save to database
                _context.MediaFiles.Add(mediaFile);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("File uploaded successfully: {FileName} (ID: {Id})", 
                    file.FileName, mediaFile.Id);
                
                // Return response
                var response = new MediaUploadResponseDto
                {
                    Id = mediaFile.Id,
                    FileName = mediaFile.FileName,
                    OriginalFileName = mediaFile.OriginalFileName,
                    ContentType = mediaFile.ContentType,
                    FileSize = mediaFile.FileSize,
                    TakenAt = mediaFile.TakenAt,
                    UploadedAt = mediaFile.UploadedAt,
                    Message = "ファイルが正常にアップロードされました"
                };
                
                return Ok(response);
            }
            finally
            {
                // Clean up temporary file
                if (System.IO.File.Exists(tempFilePath))
                {
                    System.IO.File.Delete(tempFilePath);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading file: {FileName}", file?.FileName);
            return StatusCode(500, new { 
                error = new { 
                    code = "UPLOAD_ERROR", 
                    message = "ファイルのアップロード中にエラーが発生しました" 
                } 
            });
        }
    }
    
    [HttpGet]
    public async Task<ActionResult<IEnumerable<MediaFileDto>>> GetMediaFiles()
    {
        try
        {
            var mediaFiles = await Task.FromResult(_context.MediaFiles
                .OrderByDescending(m => m.UploadedAt)
                .Select(m => new MediaFileDto
                {
                    Id = m.Id,
                    FileName = m.FileName,
                    OriginalFileName = m.OriginalFileName,
                    ContentType = m.ContentType,
                    FileSize = m.FileSize,
                    TakenAt = m.TakenAt,
                    UploadedAt = m.UploadedAt,
                    ThumbnailPath = m.ThumbnailPath
                })
                .ToList());
            
            return Ok(mediaFiles);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving media files");
            return StatusCode(500, new { 
                error = new { 
                    code = "RETRIEVAL_ERROR", 
                    message = "メディアファイルの取得中にエラーが発生しました" 
                } 
            });
        }
    }
    
    [HttpGet("{id}")]
    public async Task<ActionResult<MediaFileDto>> GetMediaFile(int id)
    {
        try
        {
            var mediaFile = await Task.FromResult(_context.MediaFiles
                .Where(m => m.Id == id)
                .Select(m => new MediaFileDto
                {
                    Id = m.Id,
                    FileName = m.FileName,
                    OriginalFileName = m.OriginalFileName,
                    ContentType = m.ContentType,
                    FileSize = m.FileSize,
                    TakenAt = m.TakenAt,
                    UploadedAt = m.UploadedAt,
                    ThumbnailPath = m.ThumbnailPath
                })
                .FirstOrDefault());
            
            if (mediaFile == null)
            {
                return NotFound(new { 
                    error = new { 
                        code = "FILE_NOT_FOUND", 
                        message = "指定されたファイルが見つかりません" 
                    } 
                });
            }
            
            return Ok(mediaFile);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving media file with ID: {Id}", id);
            return StatusCode(500, new { 
                error = new { 
                    code = "RETRIEVAL_ERROR", 
                    message = "メディアファイルの取得中にエラーが発生しました" 
                } 
            });
        }
    }
}