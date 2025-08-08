using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using AlbumApp.Controllers;
using AlbumApp.Services;
using AlbumApp.Models.DTOs;

namespace AlbumApp.Tests.Controllers;

public class ThumbnailControllerTests
{
    private readonly Mock<IMediaRepository> _mockMediaRepository;
    private readonly Mock<IThumbnailService> _mockThumbnailService;
    private readonly Mock<ILogger<ThumbnailController>> _mockLogger;
    private readonly ThumbnailController _controller;

    public ThumbnailControllerTests()
    {
        _mockMediaRepository = new Mock<IMediaRepository>();
        _mockThumbnailService = new Mock<IThumbnailService>();
        _mockLogger = new Mock<ILogger<ThumbnailController>>();
        _controller = new ThumbnailController(_mockMediaRepository.Object, _mockThumbnailService.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task GetThumbnail_MediaFileNotFound_ReturnsNotFound()
    {
        // Arrange
        var mediaId = 1;
        _mockMediaRepository.Setup(x => x.GetMediaFileByIdAsync(mediaId))
            .ReturnsAsync((MediaFileDto?)null);

        // Act
        var result = await _controller.GetThumbnail(mediaId);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        var response = notFoundResult.Value;
        Assert.NotNull(response);
    }

    [Fact]
    public async Task GetThumbnail_EmptyThumbnailPath_ReturnsNotFound()
    {
        // Arrange
        var mediaId = 1;
        var mediaFile = new MediaFileDto
        {
            Id = mediaId,
            FileName = "test.jpg",
            ThumbnailPath = string.Empty
        };
        
        _mockMediaRepository.Setup(x => x.GetMediaFileByIdAsync(mediaId))
            .ReturnsAsync(mediaFile);

        // Act
        var result = await _controller.GetThumbnail(mediaId);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        var response = notFoundResult.Value;
        Assert.NotNull(response);
    }

    [Fact]
    public async Task GetThumbnail_ThumbnailDoesNotExist_ReturnsNotFound()
    {
        // Arrange
        var mediaId = 1;
        var thumbnailPath = "20240101/test.jpg";
        var mediaFile = new MediaFileDto
        {
            Id = mediaId,
            FileName = "test.jpg",
            ThumbnailPath = thumbnailPath
        };
        
        _mockMediaRepository.Setup(x => x.GetMediaFileByIdAsync(mediaId))
            .ReturnsAsync(mediaFile);
        _mockThumbnailService.Setup(x => x.ThumbnailExistsAsync(thumbnailPath))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.GetThumbnail(mediaId);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        var response = notFoundResult.Value;
        Assert.NotNull(response);
    }

    [Fact]
    public async Task GetThumbnail_ThumbnailStreamIsNull_ReturnsInternalServerError()
    {
        // Arrange
        var mediaId = 1;
        var thumbnailPath = "20240101/test.jpg";
        var mediaFile = new MediaFileDto
        {
            Id = mediaId,
            FileName = "test.jpg",
            ThumbnailPath = thumbnailPath
        };
        
        _mockMediaRepository.Setup(x => x.GetMediaFileByIdAsync(mediaId))
            .ReturnsAsync(mediaFile);
        _mockThumbnailService.Setup(x => x.ThumbnailExistsAsync(thumbnailPath))
            .ReturnsAsync(true);
        _mockThumbnailService.Setup(x => x.GetThumbnailAsync(thumbnailPath))
            .ReturnsAsync((Stream?)null);

        // Act
        var result = await _controller.GetThumbnail(mediaId);

        // Assert
        var statusResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusResult.StatusCode);
    }

    [Fact]
    public async Task GetThumbnail_ValidRequest_ReturnsFileResult()
    {
        // Arrange
        var mediaId = 1;
        var thumbnailPath = "20240101/test.jpg";
        var mediaFile = new MediaFileDto
        {
            Id = mediaId,
            FileName = "test.jpg",
            ThumbnailPath = thumbnailPath,
            UploadedAt = DateTime.UtcNow
        };
        
        var mockStream = new MemoryStream(new byte[] { 1, 2, 3, 4 });
        
        _mockMediaRepository.Setup(x => x.GetMediaFileByIdAsync(mediaId))
            .ReturnsAsync(mediaFile);
        _mockThumbnailService.Setup(x => x.ThumbnailExistsAsync(thumbnailPath))
            .ReturnsAsync(true);
        _mockThumbnailService.Setup(x => x.GetThumbnailAsync(thumbnailPath))
            .ReturnsAsync(mockStream);

        // Act
        var result = await _controller.GetThumbnail(mediaId);

        // Assert
        var fileResult = Assert.IsType<FileStreamResult>(result);
        Assert.Equal("image/jpeg", fileResult.ContentType);
        Assert.Equal(mockStream, fileResult.FileStream);
    }

    [Fact]
    public async Task GetThumbnail_ExceptionThrown_ReturnsInternalServerError()
    {
        // Arrange
        var mediaId = 1;
        _mockMediaRepository.Setup(x => x.GetMediaFileByIdAsync(mediaId))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.GetThumbnail(mediaId);

        // Assert
        var statusResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusResult.StatusCode);
    }
}