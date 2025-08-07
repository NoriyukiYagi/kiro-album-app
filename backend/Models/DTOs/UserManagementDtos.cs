using System.ComponentModel.DataAnnotations;

namespace AlbumApp.Models.DTOs;

public class CreateUserRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
    
    [Required]
    public string Name { get; set; } = string.Empty;
    
    public bool IsAdmin { get; set; } = false;
}

public class UpdateUserRequest
{
    public string? Name { get; set; }
    public bool? IsAdmin { get; set; }
}

public class UserListResponse
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsAdmin { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime LastLoginAt { get; set; }
    public int MediaFilesCount { get; set; }
}

public class UserDetailsResponse
{
    public int Id { get; set; }
    public string GoogleId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsAdmin { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime LastLoginAt { get; set; }
    public int MediaFilesCount { get; set; }
}