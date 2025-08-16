using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
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
    private readonly IThumbnailService _thumbnailService;
    private readonly IMediaRepository _mediaRepository;
    private readonly ILogger<MediaController> _logger;
    
    public MediaController(
        AlbumDbContext context,
        IFileValidationService fileValidationService,
        IMetadataService metadataService,
        IFileStorageService fileStorageService,
        IThumbnailService thumbnailService,
        IMediaRepository mediaRepository,
        ILogger<MediaController> logger)
    {
        _context = context;
        _fileValidationService = fileValidationService;
        _metadataService = metadataService;
        _fileStorageService = fileStorageService;
        _thumbnailService = thumbnailService;
        _mediaRepository = mediaRepository;
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
                return Unauthorized(new ApiResponse<object>
                {
                    Success = false,
                    Error = "INVALID_USER",
                    Message = "無効なユーザーです"
                });
            }
            
            // Validate file
            var validationResult = _fileValidationService.ValidateFile(file);
            if (!validationResult.IsValid)
            {
                _logger.LogWarning("File validation failed: {ErrorCode} - {ErrorMessage}", 
                    validationResult.ErrorCode, validationResult.ErrorMessage);
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Error = validationResult.ErrorCode,
                    Message = validationResult.ErrorMessage
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

                // Generate thumbnail
                string thumbnailPath = "";
                try
                {
                    if (file.ContentType.StartsWith("image/"))
                    {
                        thumbnailPath = await _thumbnailService.GenerateImageThumbnailAsync(fullPath, uniqueFileName, takenAt);
                        _logger.LogInformation("Image thumbnail generated: {ThumbnailPath}", thumbnailPath);
                    }
                    else if (file.ContentType.StartsWith("video/"))
                    {
                        thumbnailPath = await _thumbnailService.GenerateVideoThumbnailAsync(fullPath, uniqueFileName, takenAt);
                        _logger.LogInformation("Video thumbnail generated: {ThumbnailPath}", thumbnailPath);
                    }
                }
                catch (Exception thumbnailEx)
                {
                    _logger.LogWarning(thumbnailEx, "Failed to generate thumbnail for {FileName}, continuing without thumbnail", uniqueFileName);
                    // Continue without thumbnail - don't fail the entire upload
                }
            
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
                    ThumbnailPath = thumbnailPath // Set generated thumbnail path
                };
            
                // Save to database using repository
                await _mediaRepository.AddMediaFileAsync(mediaFile);
                
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
                
                return Ok(new ApiResponse<MediaUploadResponseDto>
                {
                    Success = true,
                    Data = response,
                    Message = "ファイルが正常にアップロードされました"
                });
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
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Error = "UPLOAD_ERROR",
                Message = "ファイルのアップロード中にエラーが発生しました"
            });
        }
    }
    
    [HttpGet]
    public async Task<ActionResult<ApiResponse<MediaListResponseDto>>> GetMediaFiles(
        [FromQuery] int page = 1, 
        [FromQuery] int pageSize = 20)
    {
        try
        {
            var result = await _mediaRepository.GetMediaFilesAsync(page, pageSize);
            
            // Convert PagedResult to MediaListResponseDto with 0-based pageIndex
            var response = new MediaListResponseDto
            {
                Items = result.Items,
                TotalCount = result.TotalCount,
                PageIndex = result.Page - 1, // Convert 1-based to 0-based for frontend
                PageSize = result.PageSize,
                TotalPages = result.TotalPages
            };
            
            _logger.LogInformation("Retrieved {Count} media files for page {Page} of {TotalPages}", 
                result.Items.Count(), result.Page, result.TotalPages);
            
            return Ok(new ApiResponse<MediaListResponseDto>
            {
                Success = true,
                Data = response,
                Message = "メディアファイルの取得が完了しました"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving media files for page {Page}, pageSize {PageSize}", 
                page, pageSize);
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Error = "RETRIEVAL_ERROR",
                Message = "メディアファイルの取得中にエラーが発生しました"
            });
        }
    }
    
    [HttpGet("{id}")]
    public async Task<ActionResult<MediaFileDto>> GetMediaFile(int id)
    {
        try
        {
            var mediaFile = await _mediaRepository.GetMediaFileByIdAsync(id);
            
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

    [HttpPost("generate-thumbnails")]
    public async Task<ActionResult<ApiResponse<object>>> GenerateMissingThumbnails()
    {
        try
        {
            var mediaFilesWithoutThumbnails = await _context.MediaFiles
                .Where(m => string.IsNullOrEmpty(m.ThumbnailPath))
                .ToListAsync();

            if (!mediaFilesWithoutThumbnails.Any())
            {
                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "すべてのメディアファイルにサムネイルが存在します"
                });
            }

            int successCount = 0;
            int failureCount = 0;

            foreach (var mediaFile in mediaFilesWithoutThumbnails)
            {
                try
                {
                    var fullPath = _fileStorageService.GetFullPath(mediaFile.FilePath);
                    
                    if (!System.IO.File.Exists(fullPath))
                    {
                        _logger.LogWarning("Source file not found for thumbnail generation: {FilePath}", fullPath);
                        failureCount++;
                        continue;
                    }

                    string thumbnailPath = "";
                    if (mediaFile.ContentType.StartsWith("image/"))
                    {
                        thumbnailPath = await _thumbnailService.GenerateImageThumbnailAsync(fullPath, mediaFile.FileName, mediaFile.TakenAt);
                    }
                    else if (mediaFile.ContentType.StartsWith("video/"))
                    {
                        thumbnailPath = await _thumbnailService.GenerateVideoThumbnailAsync(fullPath, mediaFile.FileName, mediaFile.TakenAt);
                    }

                    if (!string.IsNullOrEmpty(thumbnailPath))
                    {
                        mediaFile.ThumbnailPath = thumbnailPath;
                        successCount++;
                        _logger.LogInformation("Generated thumbnail for media file ID {Id}: {ThumbnailPath}", mediaFile.Id, thumbnailPath);
                    }
                    else
                    {
                        failureCount++;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to generate thumbnail for media file ID {Id}", mediaFile.Id);
                    failureCount++;
                }
            }

            await _context.SaveChangesAsync();

            return Ok(new ApiResponse<object>
            {
                Success = true,
                Data = new { successCount, failureCount },
                Message = $"サムネイル生成完了: 成功 {successCount}件, 失敗 {failureCount}件"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during batch thumbnail generation");
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Error = "THUMBNAIL_GENERATION_ERROR",
                Message = "サムネイル生成中にエラーが発生しました"
            });
        }
    }

    [HttpGet("thumbnail/{id}")]
    [AllowAnonymous] // Allow anonymous access for thumbnails for better performance
    public async Task<IActionResult> GetThumbnail(int id)
    {
        try
        {
            // Get media file from database
            var mediaFile = await _mediaRepository.GetMediaFileByIdAsync(id);
            
            if (mediaFile == null)
            {
                _logger.LogWarning("Media file not found for thumbnail request: ID {Id}", id);
                return NotFound(new { 
                    error = new { 
                        code = "MEDIA_NOT_FOUND", 
                        message = "指定されたメディアファイルが見つかりません" 
                    } 
                });
            }
            
            // Check if thumbnail path exists
            if (string.IsNullOrEmpty(mediaFile.ThumbnailPath))
            {
                _logger.LogWarning("Thumbnail path is empty for media file: ID {Id}", id);
                return NotFound(new { 
                    error = new { 
                        code = "THUMBNAIL_NOT_FOUND", 
                        message = "サムネイルが見つかりません" 
                    } 
                });
            }
            
            // Check if thumbnail file exists
            var thumbnailExists = await _thumbnailService.ThumbnailExistsAsync(mediaFile.ThumbnailPath);
            if (!thumbnailExists)
            {
                _logger.LogWarning("Thumbnail file does not exist: {ThumbnailPath}", mediaFile.ThumbnailPath);
                return NotFound(new { 
                    error = new { 
                        code = "THUMBNAIL_FILE_NOT_FOUND", 
                        message = "サムネイルファイルが見つかりません" 
                    } 
                });
            }
            
            // Get thumbnail stream
            var thumbnailStream = await _thumbnailService.GetThumbnailAsync(mediaFile.ThumbnailPath);
            
            if (thumbnailStream == null)
            {
                _logger.LogError("Failed to get thumbnail stream for: {ThumbnailPath}", mediaFile.ThumbnailPath);
                return StatusCode(500, new { 
                    error = new { 
                        code = "THUMBNAIL_READ_ERROR", 
                        message = "サムネイルの読み込み中にエラーが発生しました" 
                    } 
                });
            }
            
            // Set appropriate Content-Type header (thumbnails are always JPEG)
            var contentType = "image/jpeg";
            
            _logger.LogInformation("Serving thumbnail for media file: ID {Id}, Path {ThumbnailPath}", 
                id, mediaFile.ThumbnailPath);
            
            // Create FileStreamResult and set cache headers
            var fileResult = File(thumbnailStream, contentType);
            
            // Set cache headers for better performance
            Response.Headers.Append("Cache-Control", "public, max-age=3600"); // Cache for 1 hour
            Response.Headers.Append("ETag", $"\"{mediaFile.Id}-{mediaFile.UploadedAt.Ticks}\"");
            
            return fileResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error serving thumbnail for media file ID: {Id}", id);
            return StatusCode(500, new { 
                error = new { 
                    code = "THUMBNAIL_SERVER_ERROR", 
                    message = "サムネイルの配信中にエラーが発生しました" 
                } 
            });
        }
    }
}