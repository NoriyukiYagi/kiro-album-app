using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using AlbumApp.Services;
using AlbumApp.Data;

namespace AlbumApp.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ThumbnailController : ControllerBase
{
    private readonly IMediaRepository _mediaRepository;
    private readonly IThumbnailService _thumbnailService;
    private readonly ILogger<ThumbnailController> _logger;
    
    public ThumbnailController(
        IMediaRepository mediaRepository,
        IThumbnailService thumbnailService,
        ILogger<ThumbnailController> logger)
    {
        _mediaRepository = mediaRepository;
        _thumbnailService = thumbnailService;
        _logger = logger;
    }
    
    [HttpGet("{id}")]
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
            
            // Set cache headers for better performance (only if Response is available)
            if (Response != null)
            {
                Response.Headers.Append("Cache-Control", "public, max-age=3600"); // Cache for 1 hour
                Response.Headers.Append("ETag", $"\"{mediaFile.Id}-{mediaFile.UploadedAt.Ticks}\"");
            }
            
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