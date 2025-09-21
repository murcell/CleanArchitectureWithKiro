using CleanArchitecture.Domain.Exceptions;
using CleanArchitecture.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace CleanArchitecture.Application.Features.Products.Commands;

public record UpdateProductStockCommand(int Id, int Stock) : IRequest<bool>;

public class UpdateProductStockCommandHandler : IRequestHandler<UpdateProductStockCommand, bool>
{
    private readonly IProductRepository _productRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UpdateProductStockCommandHandler> _logger;

    public UpdateProductStockCommandHandler(
        IProductRepository productRepository,
        IUnitOfWork unitOfWork,
        ILogger<UpdateProductStockCommandHandler> logger)
    {
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<bool> Handle(UpdateProductStockCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Updating stock for product: {ProductId} to {Stock}", request.Id, request.Stock);

        var product = await _productRepository.GetByIdAsync(request.Id, cancellationToken);
        if (product == null)
        {
            _logger.LogWarning("Product not found: {ProductId}", request.Id);
            throw new NotFoundException($"Product with ID {request.Id} not found");
        }

        product.UpdateStock(request.Stock);

        _productRepository.Update(product);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Product stock updated successfully: {ProductId}", request.Id);
        return true;
    }
}