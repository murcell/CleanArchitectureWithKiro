using CleanArchitecture.Application.Common.DTOs;

namespace CleanArchitecture.Application.DTOs;

/// <summary>
/// Data Transfer Object for Product entity
/// </summary>
public class ProductDto : BaseDto
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Currency { get; set; } = string.Empty;
    public int Stock { get; set; }
    public bool IsAvailable { get; set; }
    public int UserId { get; set; }
}