using Xunit;
using Microsoft.Extensions.Logging;
using Moq;
using AlbumApp.Services;
using System.IO;
using System.Threading.Tasks;

namespace AlbumApp.Tests.Services;

public class MetadataServiceTests
{
    private readonly Mock<ILogger<MetadataService>> _mockLogger;
    private readonly MetadataService _metadataService;

    public MetadataServiceTests()
    {
        _mockLogger = new Mock<ILogger<MetadataService>>();
        _metadataService = new MetadataService(_mockLogger.Object);
    }

    [Fact]
    public async Task ExtractDateTakenAsync_WithNonExistentFile_ReturnsNull()
    {
        // Arrange
        var filePath = "non-existent-file.jpg";
        var contentType = "image/jpeg";

        // Act
        var result = await _metadataService.ExtractDateTakenAsync(filePath, contentType);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task ExtractMetadataAsync_WithNonExistentFile_ReturnsEmptyMetadata()
    {
        // Arrange
        var filePath = "non-existent-file.jpg";
        var contentType = "image/jpeg";

        // Act
        var result = await _metadataService.ExtractMetadataAsync(filePath, contentType);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.DateTaken);
        Assert.Null(result.Width);
        Assert.Null(result.Height);
    }

    [Theory]
    [InlineData("image/jpeg")]
    [InlineData("image/png")]
    [InlineData("image/heic")]
    public async Task ExtractMetadataAsync_WithImageContentType_ReturnsMetadata(string contentType)
    {
        // Arrange
        var filePath = "test-image.jpg"; // This would be a non-existent file in test

        // Act
        var result = await _metadataService.ExtractMetadataAsync(filePath, contentType);

        // Assert
        Assert.NotNull(result);
        // Since file doesn't exist, metadata will be empty but object should be created
    }

    [Theory]
    [InlineData("video/mp4")]
    [InlineData("video/quicktime")]
    public async Task ExtractMetadataAsync_WithVideoContentType_ReturnsMetadata(string contentType)
    {
        // Arrange
        var filePath = "test-video.mp4"; // This would be a non-existent file in test

        // Act
        var result = await _metadataService.ExtractMetadataAsync(filePath, contentType);

        // Assert
        Assert.NotNull(result);
        // Since file doesn't exist, metadata will be empty but object should be created
    }

    [Fact]
    public async Task ExtractMetadataAsync_WithUnsupportedContentType_ReturnsEmptyMetadata()
    {
        // Arrange
        var filePath = "test-file.txt";
        var contentType = "text/plain";

        // Act
        var result = await _metadataService.ExtractMetadataAsync(filePath, contentType);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.DateTaken);
        Assert.Null(result.Width);
        Assert.Null(result.Height);
        Assert.Null(result.Duration);
    }
}