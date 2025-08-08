using AlbumApp.Models;
using AlbumApp.Models.DTOs;

namespace AlbumApp.Services;

public interface IMediaRepository
{
    Task<PagedResult<MediaFileDto>> GetMediaFilesAsync(int page, int pageSize);
    Task<MediaFileDto?> GetMediaFileByIdAsync(int id);
    Task<MediaFile> AddMediaFileAsync(MediaFile mediaFile);
    Task<bool> DeleteMediaFileAsync(int id);
}