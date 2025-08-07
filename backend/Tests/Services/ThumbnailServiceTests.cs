using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using AlbumApp.Services;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Xunit;

namespace AlbumApp.Tests.Services;

public class ThumbnailServiceTests : IDisposable
{
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly Mock<ILogger<ThumbnailService>> _mockLogger;
    private readonly ThumbnailService _thumbnailService;
    private readonly string _testThumbnailDirectory;
    private readonly string _testImagePath;
    private readonly string _testVideoPath;

    public ThumbnailServiceTests()
    {
        _mockConfiguration = new Mock<IConfiguration>();
        _mockLogger = new Mock<ILogger<ThumbnailService>>();
        
        // テスト用の一時ディレクトリを作成
        _testThumbnailDirectory = Path.Combine(Path.GetTempPath(), "test_thumbnails", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testThumbnailDirectory);
        
        _mockConfiguration.Setup(x => x["FileStorage:ThumbnailDirectory"])
            .Returns(_testThumbnailDirectory);
        
        _thumbnailService = new ThumbnailService(_mockConfiguration.Object, _mockLogger.Object);
        
        // テスト用の画像ファイルを作成
        _testImagePath = CreateTestImage();
        _testVideoPath = CreateTestVideoPlaceholder();
    }

    [Fact]
    public void GenerateDateBasedPath_ShouldReturnCorrectFormat()
    {
        // Arrange
        var testDate = new DateTime(2024, 1, 15);
        
        // Act
        var result = _thumbnailService.GenerateDateBasedPath(testDate);
        
        // Assert
        Assert.Equal("20240115", result);
    }

    [Fact]
    public async Task GenerateImageThumbnailAsync_ShouldCreateThumbnail()
    {
        // Arrange
        var fileName = "test.jpg";
        var dateTaken = new DateTime(2024, 1, 15);
        
        // Act
        var result = await _thumbnailService.GenerateImageThumbnailAsync(_testImagePath, fileName, dateTaken);
        
        // Assert
        Assert.NotNull(result);
        Assert.Contains("20240115", result);
        Assert.EndsWith(".jpg", result);
        
        // サムネイルファイルが実際に作成されているかチェック
        var fullPath = Path.Combine(_testThumbnailDirectory, result);
        Assert.True(File.Exists(fullPath));
        
        // サムネイルのサイズをチェック
        using var image = await Image.LoadAsync(fullPath);
        Assert.True(image.Width <= 300);
        Assert.True(image.Height <= 300);
    }

    [Fact]
    public async Task GenerateImageThumbnailAsync_WithoutDateTaken_ShouldUseCurrentDate()
    {
        // Arrange
        var fileName = "test.jpg";
        var today = DateTime.Now.ToString("yyyyMMdd");
        
        // Act
        var result = await _thumbnailService.GenerateImageThumbnailAsync(_testImagePath, fileName);
        
        // Assert
        Assert.NotNull(result);
        Assert.Contains(today, result);
    }

    [Fact]
    public async Task GenerateImageThumbnailAsync_WithDuplicateFileName_ShouldCreateUniqueFileName()
    {
        // Arrange
        var fileName = "duplicate.jpg";
        var dateTaken = new DateTime(2024, 1, 15);
        
        // Act - 同じファイル名で2回生成
        var result1 = await _thumbnailService.GenerateImageThumbnailAsync(_testImagePath, fileName, dateTaken);
        var result2 = await _thumbnailService.GenerateImageThumbnailAsync(_testImagePath, fileName, dateTaken);
        
        // Assert
        Assert.NotEqual(result1, result2);
        Assert.Contains("duplicate_1.jpg", result2);
    }

    [Fact]
    public async Task GetThumbnailAsync_WithExistingFile_ShouldReturnStream()
    {
        // Arrange
        var fileName = "test.jpg";
        var dateTaken = new DateTime(2024, 1, 15);
        var thumbnailPath = await _thumbnailService.GenerateImageThumbnailAsync(_testImagePath, fileName, dateTaken);
        
        // Act
        var stream = await _thumbnailService.GetThumbnailAsync(thumbnailPath);
        
        // Assert
        Assert.NotNull(stream);
        Assert.True(stream.CanRead);
        stream.Dispose();
    }

    [Fact]
    public async Task GetThumbnailAsync_WithNonExistentFile_ShouldReturnNull()
    {
        // Arrange
        var nonExistentPath = "20240115/nonexistent.jpg";
        
        // Act
        var stream = await _thumbnailService.GetThumbnailAsync(nonExistentPath);
        
        // Assert
        Assert.Null(stream);
    }

    [Fact]
    public async Task ThumbnailExistsAsync_WithExistingFile_ShouldReturnTrue()
    {
        // Arrange
        var fileName = "test.jpg";
        var dateTaken = new DateTime(2024, 1, 15);
        var thumbnailPath = await _thumbnailService.GenerateImageThumbnailAsync(_testImagePath, fileName, dateTaken);
        
        // Act
        var exists = await _thumbnailService.ThumbnailExistsAsync(thumbnailPath);
        
        // Assert
        Assert.True(exists);
    }

    [Fact]
    public async Task ThumbnailExistsAsync_WithNonExistentFile_ShouldReturnFalse()
    {
        // Arrange
        var nonExistentPath = "20240115/nonexistent.jpg";
        
        // Act
        var exists = await _thumbnailService.ThumbnailExistsAsync(nonExistentPath);
        
        // Assert
        Assert.False(exists);
    }

    [Fact]
    public async Task DeleteThumbnailAsync_WithExistingFile_ShouldReturnTrue()
    {
        // Arrange
        var fileName = "test.jpg";
        var dateTaken = new DateTime(2024, 1, 15);
        var thumbnailPath = await _thumbnailService.GenerateImageThumbnailAsync(_testImagePath, fileName, dateTaken);
        
        // Act
        var deleted = await _thumbnailService.DeleteThumbnailAsync(thumbnailPath);
        
        // Assert
        Assert.True(deleted);
        Assert.False(await _thumbnailService.ThumbnailExistsAsync(thumbnailPath));
    }

    [Fact]
    public async Task DeleteThumbnailAsync_WithNonExistentFile_ShouldReturnFalse()
    {
        // Arrange
        var nonExistentPath = "20240115/nonexistent.jpg";
        
        // Act
        var deleted = await _thumbnailService.DeleteThumbnailAsync(nonExistentPath);
        
        // Assert
        Assert.False(deleted);
    }

    private string CreateTestImage()
    {
        var testImagePath = Path.Combine(Path.GetTempPath(), $"test_image_{Guid.NewGuid()}.jpg");
        
        // 500x400の赤い画像を作成
        using var image = new Image<Rgba32>(500, 400);
        image.Mutate(x => x.BackgroundColor(Color.Red));
        image.SaveAsJpeg(testImagePath);
        
        return testImagePath;
    }

    private string CreateTestVideoPlaceholder()
    {
        // 実際の動画ファイルの代わりにプレースホルダーファイルを作成
        // FFMpegのテストは統合テストで行う
        var testVideoPath = Path.Combine(Path.GetTempPath(), $"test_video_{Guid.NewGuid()}.mp4");
        File.WriteAllText(testVideoPath, "placeholder video file");
        return testVideoPath;
    }

    public void Dispose()
    {
        // テスト用ファイルとディレクトリをクリーンアップ
        try
        {
            if (File.Exists(_testImagePath))
                File.Delete(_testImagePath);
            
            if (File.Exists(_testVideoPath))
                File.Delete(_testVideoPath);
            
            if (Directory.Exists(_testThumbnailDirectory))
                Directory.Delete(_testThumbnailDirectory, true);
        }
        catch (Exception)
        {
            // クリーンアップエラーは無視
        }
    }
}