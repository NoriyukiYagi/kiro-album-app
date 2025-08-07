using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;
using AlbumApp.Services;

namespace AlbumApp.Tests.Services;

public class FileValidationServiceTests
{
    private readonly FileValidationService _service;
    private readonly FileStorageOptions _options;

    public FileValidationServiceTests()
    {
        _options = new FileStorageOptions
        {
            MaxFileSizeBytes = 104857600, // 100MB
            AllowedExtensions = new[] { "jpg", "jpeg", "png", "heic", "mp4", "mov" },
            PictureDirectory = "/data/pict",
            ThumbnailDirectory = "/data/thumb"
        };

        var optionsMock = Options.Create(_options);
        _service = new FileValidationService(optionsMock);
    }

    [Fact]
    public void ValidateFile_ValidJpgFile_ReturnsValid()
    {
        // Arrange
        var mockFile = new Mock<IFormFile>();
        mockFile.Setup(f => f.FileName).Returns("test.jpg");
        mockFile.Setup(f => f.Length).Returns(1024);

        // Act
        var result = _service.ValidateFile(mockFile.Object);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.ErrorMessage);
        Assert.Empty(result.ErrorCode);
    }

    [Fact]
    public void ValidateFile_ValidPngFile_ReturnsValid()
    {
        // Arrange
        var mockFile = new Mock<IFormFile>();
        mockFile.Setup(f => f.FileName).Returns("test.PNG");
        mockFile.Setup(f => f.Length).Returns(2048);

        // Act
        var result = _service.ValidateFile(mockFile.Object);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public void ValidateFile_ValidMp4File_ReturnsValid()
    {
        // Arrange
        var mockFile = new Mock<IFormFile>();
        mockFile.Setup(f => f.FileName).Returns("video.mp4");
        mockFile.Setup(f => f.Length).Returns(50 * 1024 * 1024); // 50MB

        // Act
        var result = _service.ValidateFile(mockFile.Object);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public void ValidateFile_NullFile_ReturnsInvalid()
    {
        // Act
        var result = _service.ValidateFile(null);

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal("EMPTY_FILE", result.ErrorCode);
        Assert.Equal("ファイルが選択されていません", result.ErrorMessage);
    }

    [Fact]
    public void ValidateFile_EmptyFile_ReturnsInvalid()
    {
        // Arrange
        var mockFile = new Mock<IFormFile>();
        mockFile.Setup(f => f.FileName).Returns("empty.jpg");
        mockFile.Setup(f => f.Length).Returns(0);

        // Act
        var result = _service.ValidateFile(mockFile.Object);

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal("EMPTY_FILE", result.ErrorCode);
        Assert.Equal("ファイルが選択されていません", result.ErrorMessage);
    }

    [Fact]
    public void ValidateFile_FileTooLarge_ReturnsInvalid()
    {
        // Arrange
        var mockFile = new Mock<IFormFile>();
        mockFile.Setup(f => f.FileName).Returns("large.jpg");
        mockFile.Setup(f => f.Length).Returns(200 * 1024 * 1024); // 200MB

        // Act
        var result = _service.ValidateFile(mockFile.Object);

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal("INVALID_FILE_SIZE", result.ErrorCode);
        Assert.Contains("ファイルサイズが上限を超えています", result.ErrorMessage);
        Assert.Contains("100MB", result.ErrorMessage);
    }

    [Fact]
    public void ValidateFile_InvalidExtension_ReturnsInvalid()
    {
        // Arrange
        var mockFile = new Mock<IFormFile>();
        mockFile.Setup(f => f.FileName).Returns("document.pdf");
        mockFile.Setup(f => f.Length).Returns(1024);

        // Act
        var result = _service.ValidateFile(mockFile.Object);

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal("INVALID_FILE_EXTENSION", result.ErrorCode);
        Assert.Contains("許可されていないファイル形式です", result.ErrorMessage);
    }

    [Fact]
    public void ValidateFile_NoExtension_ReturnsInvalid()
    {
        // Arrange
        var mockFile = new Mock<IFormFile>();
        mockFile.Setup(f => f.FileName).Returns("noextension");
        mockFile.Setup(f => f.Length).Returns(1024);

        // Act
        var result = _service.ValidateFile(mockFile.Object);

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal("INVALID_FILE_EXTENSION", result.ErrorCode);
    }

    [Theory]
    [InlineData("test.jpg", true)]
    [InlineData("test.jpeg", true)]
    [InlineData("test.png", true)]
    [InlineData("test.HEIC", true)]
    [InlineData("test.mp4", false)]
    [InlineData("test.mov", false)]
    [InlineData("test.pdf", false)]
    public void IsImageFile_VariousExtensions_ReturnsExpected(string fileName, bool expected)
    {
        // Act
        var result = _service.IsImageFile(fileName);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("video.mp4", true)]
    [InlineData("video.MOV", true)]
    [InlineData("image.jpg", false)]
    [InlineData("image.png", false)]
    [InlineData("document.pdf", false)]
    public void IsVideoFile_VariousExtensions_ReturnsExpected(string fileName, bool expected)
    {
        // Act
        var result = _service.IsVideoFile(fileName);

        // Assert
        Assert.Equal(expected, result);
    }
}