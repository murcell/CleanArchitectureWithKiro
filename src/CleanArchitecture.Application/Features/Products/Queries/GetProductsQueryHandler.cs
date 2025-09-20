using AutoMapper;
using CleanArchitecture.Application.DTOs;
using CleanArchitecture.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace CleanArchitecture.Application.Features.Products.Queries;

/// <summary>
/// Handler for GetProductsQuery
/// </summary>
public class GetProductsQueryHandler : IRequestHandler<GetProductsQuery, IEnumerable<ProductDto>>
{
    private readonly IProductRepository _productRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<GetProductsQueryHandler> _logger;

    public GetProductsQueryHandler(
        IProductRepository productRepository,
        IMapper mapper,
        ILogger<GetProductsQueryHandler> logger)
    {
        _productRepository = productRepository;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<IEnumerable<ProductDto>> Handle(GetProductsQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting products - Page: {Page}, PageSize: {PageSize}, UserId: {UserId}, IsAvailable: {IsAvailable}", 
            request.Page, request.PageSize, request.UserId, request.IsAvailable);

        IEnumerable<Domain.Entities.Product> products;

        // Apply filters based on query parameters
        if (request.UserId.HasValue)
        {
            products = await _productRepository.GetByUserIdAsync(request.UserId.Value, cancellationToken);
        }
        else if (request.IsAvailable.HasValue && request.IsAvailable.Value)
        {
            products = await _productRepository.GetAvailableProductsAsync(cancellationToken);
        }
        else if (!string.IsNullOrEmpty(request.SearchTerm))
        {
            products = await _productRepository.SearchAsync(request.SearchTerm, cancellationToken);
        }
        else
        {
            products = await _productRepository.GetAllAsync(cancellationToken);
        }

        // Apply additional filtering if needed
        if (request.IsAvailable.HasValue && !request.UserId.HasValue)
        {
            products = products.Where(p => p.IsAvailable == request.IsAvailable.Value);
        }

        // Apply pagination
        var pagedProducts = products
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize);

        var result = _mapper.Map<IEnumerable<ProductDto>>(pagedProducts);

        _logger.LogInformation("Retrieved {Count} products", result.Count());
        return result;
    }
}