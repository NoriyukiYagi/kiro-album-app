using Microsoft.EntityFrameworkCore;
using AlbumApp.Data;
using AlbumApp.Models;
using AlbumApp.Models.DTOs;

namespace AlbumApp.Services;

public class MediaRepository : IMediaRepository
{
    private readonly AlbumDbContext _context;
    private readonly ILogger<MediaRepository> _logger;

    public MediaRepository(AlbumDbContext context, ILogger<MediaRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<PagedResult<MediaFileDto>> GetMediaFilesAsync(int page, int pageSize)
    {
        try
        {
            // Ensure page is at least 1
            page = Math.Max(1, page);
            
            // Ensure pageSize is within reasonable bounds
            pageSize = Math.Max(1, Math.Min(100, pageSize));

            // Get total count
            var totalCount = await _context.MediaFiles.CountAsync();

            // Get paginated results ordered by TakenAt descending (newest first)
            var mediaFiles = await _context.MediaFiles
                .OrderByDescending(m => m.TakenAt)
                .ThenByDescending(m => m.UploadedAt) // Secondary sort by upload date
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
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
                .ToListAsync();

            _logger.LogInformation("Retrieved {Count} media files for page {Page} (page size: {PageSize})", 
                mediaFiles.Count, page, pageSize);

            return new PagedResult<MediaFileDto>
            {
                Items = mediaFiles,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving paginated media files for page {Page}, pageSize {PageSize}", 
                page, pageSize);
            throw;
        }
    }

    public async Task<MediaFileDto?> GetMediaFileByIdAsync(int id)
    {
        try
        {
            var mediaFile = await _context.MediaFiles
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
                .FirstOrDefaultAsync();

            if (mediaFile != null)
            {
                _logger.LogInformation("Retrieved media file with ID: {Id}", id);
            }
            else
            {
                _logger.LogWarning("Media file with ID {Id} not found", id);
            }

            return mediaFile;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving media file with ID: {Id}", id);
            throw;
        }
    }

    public async Task<MediaFile> AddMediaFileAsync(MediaFile mediaFile)
    {
        try
        {
            _context.MediaFiles.Add(mediaFile);
            await _context.SaveChangesAsync();
            
            _logger.LogInformation("Added new media file: {FileName} (ID: {Id})", 
                mediaFile.FileName, mediaFile.Id);
            
            return mediaFile;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding media file: {FileName}", mediaFile.FileName);
            throw;
        }
    }

    public async Task<bool> DeleteMediaFileAsync(int id)
    {
        try
        {
            var mediaFile = await _context.MediaFiles.FindAsync(id);
            if (mediaFile == null)
            {
                _logger.LogWarning("Attempted to delete non-existent media file with ID: {Id}", id);
                return false;
            }

            _context.MediaFiles.Remove(mediaFile);
            await _context.SaveChangesAsync();
            
            _logger.LogInformation("Deleted media file: {FileName} (ID: {Id})", 
                mediaFile.FileName, id);
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting media file with ID: {Id}", id);
            throw;
        }
    }
}