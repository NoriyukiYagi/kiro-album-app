using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using AlbumApp.Data;
using AlbumApp.Models;
using AlbumApp.Services;

namespace AlbumApp.Tests.Services;

public class MediaRepositoryTests : IDisposable
{
    private readonly AlbumDbContext _context;
    private readonly MediaRepository _repository;
    private readonly Mock<ILogger<MediaRepository>> _mockLogger;

    public MediaRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<AlbumDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AlbumDbContext(options);
        _mockLogger = new Mock<ILogger<MediaRepository>>();
        _repository = new MediaRepository(_context, _mockLogger.Object);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    [Fact]
    public async Task GetMediaFilesAsync_ReturnsPagedResults_OrderedByTakenAtDescending()
    {
        // Arrange
        var user = new User
        {
            GoogleId = "test-google-id",
            Email = "test@example.com",
            Name = "Test User",
            IsAdmin = false
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var mediaFiles = new List<MediaFile>
        {
            new MediaFile
            {
                FileName = "file1.jpg",
                OriginalFileName = "original1.jpg",
                FilePath = "/data/pict/20240101/file1.jpg",
                ContentType = "image/jpeg",
                FileSize = 1000,
                TakenAt = new DateTime(2024, 1, 1),
                UploadedAt = new DateTime(2024, 1, 1),
                UploadedBy = user.Id,
                ThumbnailPath = "/data/thumb/20240101/file1.jpg"
            },
            new MediaFile
            {
                FileName = "file2.jpg",
                OriginalFileName = "original2.jpg",
                FilePath = "/data/pict/20240102/file2.jpg",
                ContentType = "image/jpeg",
                FileSize = 2000,
                TakenAt = new DateTime(2024, 1, 2),
                UploadedAt = new DateTime(2024, 1, 2),
                UploadedBy = user.Id,
                ThumbnailPath = "/data/thumb/20240102/file2.jpg"
            },
            new MediaFile
            {
                FileName = "file3.jpg",
                OriginalFileName = "original3.jpg",
                FilePath = "/data/pict/20240103/file3.jpg",
                ContentType = "image/jpeg",
                FileSize = 3000,
                TakenAt = new DateTime(2024, 1, 3),
                UploadedAt = new DateTime(2024, 1, 3),
                UploadedBy = user.Id,
                ThumbnailPath = "/data/thumb/20240103/file3.jpg"
            }
        };

        _context.MediaFiles.AddRange(mediaFiles);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetMediaFilesAsync(1, 2);

        // Assert
        Assert.Equal(3, result.TotalCount);
        Assert.Equal(1, result.Page);
        Assert.Equal(2, result.PageSize);
        Assert.Equal(2, result.TotalPages);
        Assert.True(result.HasNextPage);
        Assert.False(result.HasPreviousPage);
        Assert.Equal(2, result.Items.Count());

        // Verify ordering (newest first)
        var items = result.Items.ToList();
        Assert.Equal("file3.jpg", items[0].FileName);
        Assert.Equal("file2.jpg", items[1].FileName);
    }

    [Fact]
    public async Task GetMediaFilesAsync_ReturnsSecondPage_WhenPageIsTwo()
    {
        // Arrange
        var user = new User
        {
            GoogleId = "test-google-id",
            Email = "test@example.com",
            Name = "Test User",
            IsAdmin = false
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var mediaFiles = new List<MediaFile>
        {
            new MediaFile
            {
                FileName = "file1.jpg",
                OriginalFileName = "original1.jpg",
                FilePath = "/data/pict/20240101/file1.jpg",
                ContentType = "image/jpeg",
                FileSize = 1000,
                TakenAt = new DateTime(2024, 1, 1),
                UploadedAt = new DateTime(2024, 1, 1),
                UploadedBy = user.Id,
                ThumbnailPath = "/data/thumb/20240101/file1.jpg"
            },
            new MediaFile
            {
                FileName = "file2.jpg",
                OriginalFileName = "original2.jpg",
                FilePath = "/data/pict/20240102/file2.jpg",
                ContentType = "image/jpeg",
                FileSize = 2000,
                TakenAt = new DateTime(2024, 1, 2),
                UploadedAt = new DateTime(2024, 1, 2),
                UploadedBy = user.Id,
                ThumbnailPath = "/data/thumb/20240102/file2.jpg"
            },
            new MediaFile
            {
                FileName = "file3.jpg",
                OriginalFileName = "original3.jpg",
                FilePath = "/data/pict/20240103/file3.jpg",
                ContentType = "image/jpeg",
                FileSize = 3000,
                TakenAt = new DateTime(2024, 1, 3),
                UploadedAt = new DateTime(2024, 1, 3),
                UploadedBy = user.Id,
                ThumbnailPath = "/data/thumb/20240103/file3.jpg"
            }
        };

        _context.MediaFiles.AddRange(mediaFiles);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetMediaFilesAsync(2, 2);

        // Assert
        Assert.Equal(3, result.TotalCount);
        Assert.Equal(2, result.Page);
        Assert.Equal(2, result.PageSize);
        Assert.Equal(2, result.TotalPages);
        Assert.False(result.HasNextPage);
        Assert.True(result.HasPreviousPage);
        Assert.Single(result.Items);

        // Verify the last item is returned
        var items = result.Items.ToList();
        Assert.Equal("file1.jpg", items[0].FileName);
    }

    [Fact]
    public async Task GetMediaFilesAsync_HandlesInvalidPageParameters()
    {
        // Arrange
        var user = new User
        {
            GoogleId = "test-google-id",
            Email = "test@example.com",
            Name = "Test User",
            IsAdmin = false
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Act & Assert - Page less than 1 should be treated as 1
        var result1 = await _repository.GetMediaFilesAsync(0, 10);
        Assert.Equal(1, result1.Page);

        var result2 = await _repository.GetMediaFilesAsync(-5, 10);
        Assert.Equal(1, result2.Page);

        // Act & Assert - PageSize less than 1 should be treated as 1
        var result3 = await _repository.GetMediaFilesAsync(1, 0);
        Assert.Equal(1, result3.PageSize);

        var result4 = await _repository.GetMediaFilesAsync(1, -10);
        Assert.Equal(1, result4.PageSize);

        // Act & Assert - PageSize greater than 100 should be limited to 100
        var result5 = await _repository.GetMediaFilesAsync(1, 200);
        Assert.Equal(100, result5.PageSize);
    }

    [Fact]
    public async Task GetMediaFileByIdAsync_ReturnsMediaFile_WhenExists()
    {
        // Arrange
        var user = new User
        {
            GoogleId = "test-google-id",
            Email = "test@example.com",
            Name = "Test User",
            IsAdmin = false
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var mediaFile = new MediaFile
        {
            FileName = "test.jpg",
            OriginalFileName = "original.jpg",
            FilePath = "/data/pict/20240101/test.jpg",
            ContentType = "image/jpeg",
            FileSize = 1000,
            TakenAt = new DateTime(2024, 1, 1),
            UploadedAt = new DateTime(2024, 1, 1),
            UploadedBy = user.Id,
            ThumbnailPath = "/data/thumb/20240101/test.jpg"
        };

        _context.MediaFiles.Add(mediaFile);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetMediaFileByIdAsync(mediaFile.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(mediaFile.Id, result.Id);
        Assert.Equal(mediaFile.FileName, result.FileName);
        Assert.Equal(mediaFile.OriginalFileName, result.OriginalFileName);
        Assert.Equal(mediaFile.ContentType, result.ContentType);
        Assert.Equal(mediaFile.FileSize, result.FileSize);
        Assert.Equal(mediaFile.TakenAt, result.TakenAt);
        Assert.Equal(mediaFile.UploadedAt, result.UploadedAt);
        Assert.Equal(mediaFile.ThumbnailPath, result.ThumbnailPath);
    }

    [Fact]
    public async Task GetMediaFileByIdAsync_ReturnsNull_WhenNotExists()
    {
        // Act
        var result = await _repository.GetMediaFileByIdAsync(999);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task AddMediaFileAsync_AddsMediaFile_ReturnsAddedFile()
    {
        // Arrange
        var user = new User
        {
            GoogleId = "test-google-id",
            Email = "test@example.com",
            Name = "Test User",
            IsAdmin = false
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var mediaFile = new MediaFile
        {
            FileName = "new.jpg",
            OriginalFileName = "original.jpg",
            FilePath = "/data/pict/20240101/new.jpg",
            ContentType = "image/jpeg",
            FileSize = 1000,
            TakenAt = new DateTime(2024, 1, 1),
            UploadedAt = new DateTime(2024, 1, 1),
            UploadedBy = user.Id,
            ThumbnailPath = "/data/thumb/20240101/new.jpg"
        };

        // Act
        var result = await _repository.AddMediaFileAsync(mediaFile);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Id > 0);
        Assert.Equal(mediaFile.FileName, result.FileName);

        // Verify it was actually saved to database
        var savedFile = await _context.MediaFiles.FindAsync(result.Id);
        Assert.NotNull(savedFile);
        Assert.Equal(mediaFile.FileName, savedFile.FileName);
    }

    [Fact]
    public async Task DeleteMediaFileAsync_DeletesMediaFile_ReturnsTrue_WhenExists()
    {
        // Arrange
        var user = new User
        {
            GoogleId = "test-google-id",
            Email = "test@example.com",
            Name = "Test User",
            IsAdmin = false
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var mediaFile = new MediaFile
        {
            FileName = "delete.jpg",
            OriginalFileName = "original.jpg",
            FilePath = "/data/pict/20240101/delete.jpg",
            ContentType = "image/jpeg",
            FileSize = 1000,
            TakenAt = new DateTime(2024, 1, 1),
            UploadedAt = new DateTime(2024, 1, 1),
            UploadedBy = user.Id,
            ThumbnailPath = "/data/thumb/20240101/delete.jpg"
        };

        _context.MediaFiles.Add(mediaFile);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.DeleteMediaFileAsync(mediaFile.Id);

        // Assert
        Assert.True(result);

        // Verify it was actually deleted from database
        var deletedFile = await _context.MediaFiles.FindAsync(mediaFile.Id);
        Assert.Null(deletedFile);
    }

    [Fact]
    public async Task DeleteMediaFileAsync_ReturnsFalse_WhenNotExists()
    {
        // Act
        var result = await _repository.DeleteMediaFileAsync(999);

        // Assert
        Assert.False(result);
    }
}