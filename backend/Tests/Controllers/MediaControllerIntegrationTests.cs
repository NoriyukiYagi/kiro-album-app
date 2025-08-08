using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http.Headers;
using System.Text.Json;
using Xunit;
using AlbumApp.Data;
using AlbumApp.Models;
using AlbumApp.Models.DTOs;
using AlbumApp.Services;

namespace AlbumApp.Tests.Controllers;

public class MediaControllerIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public MediaControllerIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Remove the real database context
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<AlbumDbContext>));
                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }

                // Add in-memory database for testing
                services.AddDbContext<AlbumDbContext>(options =>
                {
                    options.UseInMemoryDatabase("TestDb_" + Guid.NewGuid());
                });
            });
        });

        _client = _factory.CreateClient();
    }

    private async Task<string> GetJwtTokenAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var jwtService = scope.ServiceProvider.GetRequiredService<IJwtService>();
        var context = scope.ServiceProvider.GetRequiredService<AlbumDbContext>();

        // Create a test user
        var user = new User
        {
            GoogleId = "test-google-id",
            Email = "test@example.com",
            Name = "Test User",
            IsAdmin = false
        };

        context.Users.Add(user);
        await context.SaveChangesAsync();

        // Generate JWT token
        return jwtService.GenerateToken(user);
    }

    private async Task SeedTestDataAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AlbumDbContext>();

        // Clear existing data
        context.MediaFiles.RemoveRange(context.MediaFiles);
        await context.SaveChangesAsync();

        // Get the test user
        var user = await context.Users.FirstAsync();

        // Add test media files
        var mediaFiles = new List<MediaFile>();
        for (int i = 1; i <= 25; i++)
        {
            mediaFiles.Add(new MediaFile
            {
                FileName = $"file{i:D2}.jpg",
                OriginalFileName = $"original{i:D2}.jpg",
                FilePath = $"/data/pict/2024010{i % 10}/file{i:D2}.jpg",
                ContentType = "image/jpeg",
                FileSize = 1000 * i,
                TakenAt = new DateTime(2024, 1, i % 28 + 1), // Spread across January
                UploadedAt = new DateTime(2024, 1, i % 28 + 1),
                UploadedBy = user.Id,
                ThumbnailPath = $"/data/thumb/2024010{i % 10}/file{i:D2}.jpg"
            });
        }

        context.MediaFiles.AddRange(mediaFiles);
        await context.SaveChangesAsync();
    }

    [Fact]
    public async Task GetMediaFiles_ReturnsUnauthorized_WhenNoToken()
    {
        // Act
        var response = await _client.GetAsync("/api/media");

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetMediaFiles_ReturnsPagedResults_WithDefaultPagination()
    {
        // Arrange
        var token = await GetJwtTokenAsync();
        await SeedTestDataAsync();
        
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.GetAsync("/api/media");

        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<PagedResult<MediaFileDto>>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.NotNull(result);
        Assert.Equal(25, result.TotalCount);
        Assert.Equal(1, result.Page);
        Assert.Equal(20, result.PageSize); // Default page size
        Assert.Equal(2, result.TotalPages);
        Assert.True(result.HasNextPage);
        Assert.False(result.HasPreviousPage);
        Assert.Equal(20, result.Items.Count());

        // Verify ordering (newest first by TakenAt)
        var items = result.Items.ToList();
        Assert.True(items[0].TakenAt >= items[1].TakenAt);
    }

    [Fact]
    public async Task GetMediaFiles_ReturnsPagedResults_WithCustomPagination()
    {
        // Arrange
        var token = await GetJwtTokenAsync();
        await SeedTestDataAsync();
        
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.GetAsync("/api/media?page=2&pageSize=10");

        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<PagedResult<MediaFileDto>>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.NotNull(result);
        Assert.Equal(25, result.TotalCount);
        Assert.Equal(2, result.Page);
        Assert.Equal(10, result.PageSize);
        Assert.Equal(3, result.TotalPages);
        Assert.True(result.HasNextPage);
        Assert.True(result.HasPreviousPage);
        Assert.Equal(10, result.Items.Count());
    }

    [Fact]
    public async Task GetMediaFiles_ReturnsLastPage_WhenRequestingBeyondTotalPages()
    {
        // Arrange
        var token = await GetJwtTokenAsync();
        await SeedTestDataAsync();
        
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.GetAsync("/api/media?page=3&pageSize=10");

        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<PagedResult<MediaFileDto>>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.NotNull(result);
        Assert.Equal(25, result.TotalCount);
        Assert.Equal(3, result.Page);
        Assert.Equal(10, result.PageSize);
        Assert.Equal(3, result.TotalPages);
        Assert.False(result.HasNextPage);
        Assert.True(result.HasPreviousPage);
        Assert.Equal(5, result.Items.Count()); // Last 5 items
    }

    [Fact]
    public async Task GetMediaFiles_HandlesInvalidPageParameters()
    {
        // Arrange
        var token = await GetJwtTokenAsync();
        await SeedTestDataAsync();
        
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act - Test invalid page (should default to 1)
        var response1 = await _client.GetAsync("/api/media?page=0&pageSize=10");
        response1.EnsureSuccessStatusCode();
        var content1 = await response1.Content.ReadAsStringAsync();
        var result1 = JsonSerializer.Deserialize<PagedResult<MediaFileDto>>(content1, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.Equal(1, result1!.Page);

        // Act - Test invalid pageSize (should default to reasonable value)
        var response2 = await _client.GetAsync("/api/media?page=1&pageSize=0");
        response2.EnsureSuccessStatusCode();
        var content2 = await response2.Content.ReadAsStringAsync();
        var result2 = JsonSerializer.Deserialize<PagedResult<MediaFileDto>>(content2, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.Equal(1, result2!.PageSize);

        // Act - Test large pageSize (should be limited)
        var response3 = await _client.GetAsync("/api/media?page=1&pageSize=200");
        response3.EnsureSuccessStatusCode();
        var content3 = await response3.Content.ReadAsStringAsync();
        var result3 = JsonSerializer.Deserialize<PagedResult<MediaFileDto>>(content3, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.Equal(100, result3!.PageSize);
    }

    [Fact]
    public async Task GetMediaFile_ReturnsMediaFile_WhenExists()
    {
        // Arrange
        var token = await GetJwtTokenAsync();
        await SeedTestDataAsync();
        
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AlbumDbContext>();
        var firstMediaFile = await context.MediaFiles.FirstAsync();

        // Act
        var response = await _client.GetAsync($"/api/media/{firstMediaFile.Id}");

        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<MediaFileDto>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.NotNull(result);
        Assert.Equal(firstMediaFile.Id, result.Id);
        Assert.Equal(firstMediaFile.FileName, result.FileName);
        Assert.Equal(firstMediaFile.OriginalFileName, result.OriginalFileName);
        Assert.Equal(firstMediaFile.ContentType, result.ContentType);
    }

    [Fact]
    public async Task GetMediaFile_ReturnsNotFound_WhenNotExists()
    {
        // Arrange
        var token = await GetJwtTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.GetAsync("/api/media/999");

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);
    }
}