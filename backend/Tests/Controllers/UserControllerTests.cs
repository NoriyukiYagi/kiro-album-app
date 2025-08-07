using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using AlbumApp.Controllers;
using AlbumApp.Data;
using AlbumApp.Models;
using AlbumApp.Models.DTOs;
using AlbumApp.Services;

namespace AlbumApp.Tests.Controllers;

public class UserControllerTests : IDisposable
{
    private readonly AlbumDbContext _context;
    private readonly Mock<IAdminService> _mockAdminService;
    private readonly Mock<ILogger<UserController>> _mockLogger;
    private readonly UserController _controller;

    public UserControllerTests()
    {
        var options = new DbContextOptionsBuilder<AlbumDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AlbumDbContext(options);
        _mockAdminService = new Mock<IAdminService>();
        _mockLogger = new Mock<ILogger<UserController>>();
        
        _controller = new UserController(_context, _mockAdminService.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task GetUsers_ReturnsUserList()
    {
        // Arrange
        var user1 = new User
        {
            Id = 1,
            GoogleId = "google1",
            Email = "user1@example.com",
            Name = "User 1",
            IsAdmin = false,
            CreatedAt = DateTime.UtcNow,
            LastLoginAt = DateTime.UtcNow
        };

        var user2 = new User
        {
            Id = 2,
            GoogleId = "google2",
            Email = "admin@example.com",
            Name = "Admin User",
            IsAdmin = true,
            CreatedAt = DateTime.UtcNow,
            LastLoginAt = DateTime.UtcNow
        };

        _context.Users.AddRange(user1, user2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetUsers();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<List<UserListResponse>>>(okResult.Value);
        Assert.True(response.Success);
        Assert.Equal(2, response.Data?.Count);
    }

    [Fact]
    public async Task CreateUser_WithValidData_CreatesUser()
    {
        // Arrange
        var request = new CreateUserRequest
        {
            Email = "newuser@example.com",
            Name = "New User",
            IsAdmin = false
        };

        _mockAdminService.Setup(x => x.IsAdminUser(request.Email)).Returns(false);

        // Act
        var result = await _controller.CreateUser(request);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        var response = Assert.IsType<ApiResponse<UserDetailsResponse>>(createdResult.Value);
        Assert.True(response.Success);
        Assert.Equal(request.Email, response.Data?.Email);
        Assert.Equal(request.Name, response.Data?.Name);
    }

    [Fact]
    public async Task CreateUser_WithExistingEmail_ReturnsBadRequest()
    {
        // Arrange
        var existingUser = new User
        {
            GoogleId = "google1",
            Email = "existing@example.com",
            Name = "Existing User",
            IsAdmin = false
        };

        _context.Users.Add(existingUser);
        await _context.SaveChangesAsync();

        var request = new CreateUserRequest
        {
            Email = "existing@example.com",
            Name = "New User",
            IsAdmin = false
        };

        // Act
        var result = await _controller.CreateUser(request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<UserDetailsResponse>>(badRequestResult.Value);
        Assert.False(response.Success);
        Assert.Equal("USER_EXISTS", response.Error);
    }

    [Fact]
    public async Task DeleteUser_WithMediaFiles_ReturnsBadRequest()
    {
        // Arrange
        var user = new User
        {
            GoogleId = "google1",
            Email = "user@example.com",
            Name = "User",
            IsAdmin = false
        };

        var mediaFile = new MediaFile
        {
            FileName = "test.jpg",
            OriginalFileName = "test.jpg",
            FilePath = "/path/test.jpg",
            ThumbnailPath = "/path/thumb.jpg",
            ContentType = "image/jpeg",
            FileSize = 1000,
            TakenAt = DateTime.UtcNow,
            UploadedAt = DateTime.UtcNow,
            User = user
        };

        _context.Users.Add(user);
        _context.MediaFiles.Add(mediaFile);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.DeleteUser(user.Id);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<object>>(badRequestResult.Value);
        Assert.False(response.Success);
        Assert.Equal("USER_HAS_MEDIA", response.Error);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}