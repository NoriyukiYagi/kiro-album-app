using Xunit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using AlbumApp.Services;
using System;
using System.IO;
using System.Threading.Tasks;

namespace AlbumApp.Tests.Services;

public class FileStorageServiceTests : IDisposable
{
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly Mock<ILogger<FileStorageService>> _mockLogger;
    private readonly FileStorageService _fileStorageService;
    private readonly string _testBaseDirectory;

    public FileStorageServiceTests()
    {
        _mockConfiguration = new Mock<IConfiguration>();
        _mockLogger = new Mock<ILogger<FileStorageService>>();
        
        // Create a temporary directory for testing
        _testBaseDirectory = Path.Combine(Path.GetTempPath(), "FileStorageServiceTests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testBaseDirectory);

        _mockConfiguration.Setup(c => c["FileStorage:PictureDirectory"]).Returns(_testBaseDirectory);
        
        _fileStorageService = new FileStorageService(_mockConfiguration.Object, _mockLogger.Object);
    }

    [Fact]
    public void GenerateDateBasedPath_WithValidDate_ReturnsCorrectFormat()
    {
        // Arrange
        var testDate = new DateTime(2024, 3, 15);

        // Act
        var result = _fileStorageService.GenerateDateBasedPath(testDate);

        // Assert
        Assert.Equal("20240315", result);
    }

    [Fact]
    public void GetFullPath_WithRelativePath_ReturnsCorrectFullPath()
    {
        // Arrange
        var relativePath = "20240315/test.jpg";

        // Act
        var result = _fileStorageService.GetFullPath(relativePath);

        // Assert
        var expected = Path.Combine(_testBaseDirectory, relativePath);
        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task SaveFileAsync_WithValidFile_SavesFileSuccessfully()
    {
        // Arrange
        var sourceFilePath = Path.Combine(_testBaseDirectory, "source.txt");
        var fileName = "test.txt";
        var testDate = new DateTime(2024, 3, 15);
        
        // Create a test source file
        await File.WriteAllTextAsync(sourceFilePath, "Test content");

        // Act
        var result = await _fileStorageService.SaveFileAsync(sourceFilePath, fileName, testDate);

        // Assert
        Assert.Equal("20240315/test.txt", result);
        
        var savedFilePath = _fileStorageService.GetFullPath(result);
        Assert.True(File.Exists(savedFilePath));
        
        var content = await File.ReadAllTextAsync(savedFilePath);
        Assert.Equal("Test content", content);
    }

    [Fact]
    public async Task SaveFileAsync_WithoutDateTaken_UsesCurrentDate()
    {
        // Arrange
        var sourceFilePath = Path.Combine(_testBaseDirectory, "source.txt");
        var fileName = "test.txt";
        
        // Create a test source file
        await File.WriteAllTextAsync(sourceFilePath, "Test content");

        // Act
        var result = await _fileStorageService.SaveFileAsync(sourceFilePath, fileName);

        // Assert
        var expectedDatePath = DateTime.Now.ToString("yyyyMMdd");
        Assert.StartsWith(expectedDatePath, result);
        Assert.EndsWith("test.txt", result);
    }

    [Fact]
    public async Task SaveFileAsync_WithDuplicateFileName_CreatesUniqueFileName()
    {
        // Arrange
        var sourceFilePath = Path.Combine(_testBaseDirectory, "source.txt");
        var fileName = "test.txt";
        var testDate = new DateTime(2024, 3, 15);
        
        // Create a test source file
        await File.WriteAllTextAsync(sourceFilePath, "Test content");

        // Act - Save the same file twice
        var result1 = await _fileStorageService.SaveFileAsync(sourceFilePath, fileName, testDate);
        var result2 = await _fileStorageService.SaveFileAsync(sourceFilePath, fileName, testDate);

        // Assert
        Assert.Equal("20240315/test.txt", result1);
        Assert.Equal("20240315/test_1.txt", result2);
        
        Assert.True(File.Exists(_fileStorageService.GetFullPath(result1)));
        Assert.True(File.Exists(_fileStorageService.GetFullPath(result2)));
    }

    [Fact]
    public async Task FileExistsAsync_WithExistingFile_ReturnsTrue()
    {
        // Arrange
        var relativePath = "20240315/test.txt";
        var fullPath = _fileStorageService.GetFullPath(relativePath);
        
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        await File.WriteAllTextAsync(fullPath, "Test content");

        // Act
        var result = await _fileStorageService.FileExistsAsync(relativePath);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task FileExistsAsync_WithNonExistentFile_ReturnsFalse()
    {
        // Arrange
        var relativePath = "20240315/nonexistent.txt";

        // Act
        var result = await _fileStorageService.FileExistsAsync(relativePath);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task GetFileAsync_WithExistingFile_ReturnsStream()
    {
        // Arrange
        var relativePath = "20240315/test.txt";
        var fullPath = _fileStorageService.GetFullPath(relativePath);
        var testContent = "Test content";
        
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        await File.WriteAllTextAsync(fullPath, testContent);

        // Act
        using var stream = await _fileStorageService.GetFileAsync(relativePath);

        // Assert
        Assert.NotNull(stream);
        using var reader = new StreamReader(stream);
        var content = await reader.ReadToEndAsync();
        Assert.Equal(testContent, content);
    }

    [Fact]
    public async Task GetFileAsync_WithNonExistentFile_ReturnsNull()
    {
        // Arrange
        var relativePath = "20240315/nonexistent.txt";

        // Act
        var result = await _fileStorageService.GetFileAsync(relativePath);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task DeleteFileAsync_WithExistingFile_DeletesFileAndReturnsTrue()
    {
        // Arrange
        var relativePath = "20240315/test.txt";
        var fullPath = _fileStorageService.GetFullPath(relativePath);
        
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        await File.WriteAllTextAsync(fullPath, "Test content");

        // Act
        var result = await _fileStorageService.DeleteFileAsync(relativePath);

        // Assert
        Assert.True(result);
        Assert.False(File.Exists(fullPath));
    }

    [Fact]
    public async Task DeleteFileAsync_WithNonExistentFile_ReturnsFalse()
    {
        // Arrange
        var relativePath = "20240315/nonexistent.txt";

        // Act
        var result = await _fileStorageService.DeleteFileAsync(relativePath);

        // Assert
        Assert.False(result);
    }

    public void Dispose()
    {
        // Clean up test directory
        if (Directory.Exists(_testBaseDirectory))
        {
            Directory.Delete(_testBaseDirectory, true);
        }
    }
}