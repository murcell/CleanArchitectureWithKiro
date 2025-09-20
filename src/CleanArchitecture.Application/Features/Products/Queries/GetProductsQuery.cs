using CleanArchitecture.Application.DTOs;
using MediatR;

namespace CleanArchitecture.Application.Features.Products.Queries;

/// <summary>
/// Query to get products with filtering and pagination
/// </summary>
public class GetProductsQuery : IRequest<IEnumerable<ProductDto>>
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int? UserId { get; set; }
    public bool? IsAvailable { get; set; }
    public string? SearchTerm { get; set; }

    public GetProductsQuery(int page = 1, int pageSize = 10, int? userId = null, bool? isAvailable = null, string? searchTerm = null)
    {
        Page = page;
        PageSize = pageSize;
        UserId = userId;
        IsAvailable = isAvailable;
        SearchTerm = searchTerm;
    }
}