namespace CleanArchitecture.Application.Common.DTOs;

/// <summary>
/// Base DTO class for common properties
/// </summary>
public abstract class BaseDto
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}