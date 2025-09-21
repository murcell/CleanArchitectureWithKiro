using CleanArchitecture.Domain.Exceptions;
using CleanArchitecture.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace CleanArchitecture.Application.Features.Products.Commands;

public record ToggleProductAvailabilityCommand(int Id) : IRequest<bool>;

public class ToggleProductAvailabilityCommandHandler : IRequestHandler<ToggleProductAvailabilityCommand, bool>
{
    private readonly IProductRepository _productRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ToggleProductAvailabilityCommandHandler> _logger;

    public ToggleProductAvailabilityCommandHandler(
        IProductRepository productRepository,
        IUnitOfWork unitOfWork,
        ILogger<ToggleProductAvailabilityCommandHandler> logger)
    {
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<bool> Handle(ToggleProductAvailabilityCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Toggling availability for product: {ProductId}", request.Id);

        var product = await _productRepository.GetByIdAsync(request.Id, cancellationToken);
        if (product == null)
        {
            _logger.LogWarning("Product not found: {ProductId}", request.Id);
            throw new NotFoundException($"Product with ID {request.Id} not found");
        }

        if (product.IsAvailable)
        {
            product.MarkAsUnavailable();
        }
        else
        {
            product.MarkAsAvailable();
        }

        _productRepository.Update(product);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Product availability toggled successfully: {ProductId}, IsAvailable: {IsAvailable}", 
            request.Id, product.IsAvailable);
        return true;
    }
}