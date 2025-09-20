namespace CleanArchitecture.Application.DTOs.Requests;

/// <summary>
/// Request DTO for updating an existing product
/// </summary>
public class UpdateProductRequest
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Currency { get; set; } = string.Empty;
    public int Stock { get; set; }
}