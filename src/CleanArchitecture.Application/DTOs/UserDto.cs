using CleanArchitecture.Application.Common.DTOs;
using CleanArchitecture.Domain.Enums;

namespace CleanArchitecture.Application.DTOs;

/// <summary>
/// Data Transfer Object for User entity
/// </summary>
public class UserDto : BaseDto
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public UserRole Role { get; set; }
    public bool IsEmailConfirmed { get; set; }
    public int FailedLoginAttempts { get; set; }
    public DateTime? LockoutEnd { get; set; }
    public List<ProductDto> Products { get; set; } = new();
}