using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System.Security.Claims;
using Xunit;
using AlbumApp.Controllers;
using AlbumApp.Data;
using AlbumApp.Models;
using AlbumApp.Models.DTOs;
using AlbumApp.Services;

namespace AlbumApp.Tests.Controllers;

public class MediaControllerTests : IDisposable
{
    private readonly AlbumDbContext _context;
    private readonly Mock<IFileValidationService> _mockFileValidationService;
    private readonly Mock<IMetadataService> _mockMetadataService;
    private readonly Mock<IFileStorageService> _mockFileStorageService;
    private readonly Mock<IMediaRepository> _mockMediaRepository;
    private readonly Mock<ILogger<MediaController>> _mockLogger;
    private readonly MediaController _controller;

    public MediaControllerTests()
    {
        var options = new DbContextOptionsBuilder<AlbumDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AlbumDbContext(options);
        
        _mockFileValidationService = new Mock<IFileValidationService>();
        _mockMetadataService = new Mock<IMetadataService>();
        _mockFileStorageService = new Mock<IFileStorageService>();
        _mockMediaRepository = new Mock<IMediaRepository>();
        _mockLogger = new Mock<ILogger<MediaController>>();
        
        _controller = new MediaController(
            _context, 
            _mockFileValidationService.Object, 
            _mockMetadataService.Object,
            _mockFileStorageService.Object,
            _mockMediaRepository.Object,
            _mockLogger.Object);
        
        // Setup user context
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, "1"),
            new Claim(ClaimTypes.Email, "test@example.com")
        };
        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        
        _controller.ControllerContext = new ControllerContext()
        {
            HttpContext = new DefaultHttpContext() { User = claimsPrincipal }
        };
    }

    [Fact]
    public async Task UploadFile_ValidFile_ReturnsSuccess()
    {
        // Arrange
        var user = new User
        {
            Id = 1,
            GoogleId = "google1",
            Email = "test@example.com",
            Name = "Test User"
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var mockFile = new Mock<IFormFile>();
        mockFile.Setup(f => f.FileName).Returns("test.jpg");
        mockFile.Setup(f => f.ContentType).Returns("image/jpeg");
        mockFile.Setup(f => f.Length).Returns(1024);
        mockFile.Setup(f => f.CopyToAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
               .Returns(Task.CompletedTask);

        _mockFileValidationService.Setup(s => s.ValidateFile(It.IsAny<IFormFile>()))
            .Returns(new FileUploadValidationResult { IsValid = true });

        var testDate = DateTime.UtcNow.AddDays(-1);
        _mockMetadataService.Setup(s => s.ExtractDateTakenAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(testDate);

        _mockFileStorageService.Setup(s => s.SaveFileAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime?>()))
            .ReturnsAsync("20240101/test.jpg");

        _mockFileStorageService.Setup(s => s.GetFullPath(It.IsAny<string>()))
            .Returns("/data/pict/20240101/test.jpg");

        // Act
        var result = await _controller.UploadFile(mockFile.Object);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<MediaUploadResponseDto>(okResult.Value);
        Assert.Equal("test.jpg", response.OriginalFileName);
        Assert.Equal("image/jpeg", response.ContentType);
        Assert.Equal(1024, response.FileSize);
    }

    [Fact]
    public async Task UploadFile_InvalidFileSize_ReturnsBadRequest()
    {
        // Arrange
        var mockFile = new Mock<IFormFile>();
        mockFile.Setup(f => f.FileName).Returns("large.jpg");
        mockFile.Setup(f => f.Length).Returns(200 * 1024 * 1024); // 200MB

        _mockFileValidationService.Setup(s => s.ValidateFile(It.IsAny<IFormFile>()))
            .Returns(new FileUploadValidationResult 
            { 
                IsValid = false, 
                ErrorCode = "INVALID_FILE_SIZE",
                ErrorMessage = "ファイルサイズが上限を超えています。最大100MBまでアップロード可能です"
            });

        // Act
        var result = await _controller.UploadFile(mockFile.Object);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        var errorResponse = badRequestResult.Value;
        Assert.NotNull(errorResponse);
    }

    [Fact]
    public async Task UploadFile_InvalidFileExtension_ReturnsBadRequest()
    {
        // Arrange
        var mockFile = new Mock<IFormFile>();
        mockFile.Setup(f => f.FileName).Returns("document.pdf");
        mockFile.Setup(f => f.Length).Returns(1024);

        _mockFileValidationService.Setup(s => s.ValidateFile(It.IsAny<IFormFile>()))
            .Returns(new FileUploadValidationResult 
            { 
                IsValid = false, 
                ErrorCode = "INVALID_FILE_EXTENSION",
                ErrorMessage = "許可されていないファイル形式です。対応形式: JPG, JPEG, PNG, HEIC, MP4, MOV"
            });

        // Act
        var result = await _controller.UploadFile(mockFile.Object);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        var errorResponse = badRequestResult.Value;
        Assert.NotNull(errorResponse);
    }

    [Fact]
    public async Task UploadFile_EmptyFile_ReturnsBadRequest()
    {
        // Arrange
        var mockFile = new Mock<IFormFile>();
        mockFile.Setup(f => f.FileName).Returns("empty.jpg");
        mockFile.Setup(f => f.Length).Returns(0);

        _mockFileValidationService.Setup(s => s.ValidateFile(It.IsAny<IFormFile>()))
            .Returns(new FileUploadValidationResult 
            { 
                IsValid = false, 
                ErrorCode = "EMPTY_FILE",
                ErrorMessage = "ファイルが選択されていません"
            });

        // Act
        var result = await _controller.UploadFile(mockFile.Object);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        var errorResponse = badRequestResult.Value;
        Assert.NotNull(errorResponse);
    }

    [Fact]
    public async Task GetMediaFiles_ReturnsPagedMediaFilesList()
    {
        // Arrange
        var mediaFiles = new List<MediaFileDto>
        {
            new MediaFileDto
            {
                Id = 1,
                FileName = "file1.jpg",
                OriginalFileName = "original1.jpg",
                ContentType = "image/jpeg",
                FileSize = 1024,
                TakenAt = DateTime.UtcNow.AddDays(-1),
                UploadedAt = DateTime.UtcNow.AddDays(-1),
                ThumbnailPath = "/data/thumb/20240101/file1.jpg"
            },
            new MediaFileDto
            {
                Id = 2,
                FileName = "file2.png",
                OriginalFileName = "original2.png",
                ContentType = "image/png",
                FileSize = 2048,
                TakenAt = DateTime.UtcNow,
                UploadedAt = DateTime.UtcNow,
                ThumbnailPath = "/data/thumb/20240102/file2.png"
            }
        };

        var pagedResult = new PagedResult<MediaFileDto>
        {
            Items = mediaFiles,
            TotalCount = 2,
            Page = 1,
            PageSize = 20
        };

        _mockMediaRepository.Setup(r => r.GetMediaFilesAsync(1, 20))
            .ReturnsAsync(pagedResult);

        // Act
        var result = await _controller.GetMediaFiles(1, 20);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedResult = Assert.IsType<PagedResult<MediaFileDto>>(okResult.Value);
        
        Assert.Equal(2, returnedResult.TotalCount);
        Assert.Equal(1, returnedResult.Page);
        Assert.Equal(20, returnedResult.PageSize);
        Assert.Equal(2, returnedResult.Items.Count());
    }

    [Fact]
    public async Task GetMediaFile_ExistingId_ReturnsMediaFile()
    {
        // Arrange
        var mediaFileDto = new MediaFileDto
        {
            Id = 1,
            FileName = "test.jpg",
            OriginalFileName = "original.jpg",
            ContentType = "image/jpeg",
            FileSize = 1024,
            TakenAt = DateTime.UtcNow,
            UploadedAt = DateTime.UtcNow,
            ThumbnailPath = "/data/thumb/20240101/test.jpg"
        };

        _mockMediaRepository.Setup(r => r.GetMediaFileByIdAsync(1))
            .ReturnsAsync(mediaFileDto);

        // Act
        var result = await _controller.GetMediaFile(1);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedMediaFile = Assert.IsType<MediaFileDto>(okResult.Value);
        Assert.Equal("test.jpg", returnedMediaFile.FileName);
        Assert.Equal("original.jpg", returnedMediaFile.OriginalFileName);
    }

    [Fact]
    public async Task GetMediaFile_NonExistingId_ReturnsNotFound()
    {
        // Arrange
        _mockMediaRepository.Setup(r => r.GetMediaFileByIdAsync(999))
            .ReturnsAsync((MediaFileDto?)null);

        // Act
        var result = await _controller.GetMediaFile(999);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
        var errorResponse = notFoundResult.Value;
        Assert.NotNull(errorResponse);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}