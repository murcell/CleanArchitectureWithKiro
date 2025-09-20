namespace CleanArchitecture.Application.DTOs.Requests;

/// <summary>
/// Request DTO for creating a new product
/// </summary>
public class CreateProductRequest
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Currency { get; set; } = string.Empty;
    public int Stock { get; set; }
    public int UserId { get; set; }
}