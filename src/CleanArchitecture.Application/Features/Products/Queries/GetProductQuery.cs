using AutoMapper;
using CleanArchitecture.Application.DTOs;
using CleanArchitecture.Domain.Exceptions;
using CleanArchitecture.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace CleanArchitecture.Application.Features.Products.Queries;

public record GetProductQuery(int Id) : IRequest<ProductDto>;

public class GetProductQueryHandler : IRequestHandler<GetProductQuery, ProductDto>
{
    private readonly IProductRepository _productRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<GetProductQueryHandler> _logger;

    public GetProductQueryHandler(
        IProductRepository productRepository,
        IMapper mapper,
        ILogger<GetProductQueryHandler> logger)
    {
        _productRepository = productRepository;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<ProductDto> Handle(GetProductQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting product: {ProductId}", request.Id);

        var product = await _productRepository.GetByIdAsync(request.Id, cancellationToken);
        if (product == null)
        {
            _logger.LogWarning("Product not found: {ProductId}", request.Id);
            throw new NotFoundException($"Product with ID {request.Id} not found");
        }

        var result = _mapper.Map<ProductDto>(product);
        _logger.LogInformation("Product retrieved successfully: {ProductId}", request.Id);
        
        return result;
    }
}