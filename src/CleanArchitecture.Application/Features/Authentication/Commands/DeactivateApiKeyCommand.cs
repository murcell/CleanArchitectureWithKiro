using CleanArchitecture.Domain.Exceptions;
using CleanArchitecture.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace CleanArchitecture.Application.Features.Authentication.Commands;

public record DeactivateApiKeyCommand(int Id, int UserId) : IRequest<bool>;

public class DeactivateApiKeyCommandHandler : IRequestHandler<DeactivateApiKeyCommand, bool>
{
    private readonly IApiKeyRepository _apiKeyRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DeactivateApiKeyCommandHandler> _logger;

    public DeactivateApiKeyCommandHandler(
        IApiKeyRepository apiKeyRepository,
        IUnitOfWork unitOfWork,
        ILogger<DeactivateApiKeyCommandHandler> logger)
    {
        _apiKeyRepository = apiKeyRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<bool> Handle(DeactivateApiKeyCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Deactivating API key: {ApiKeyId} for user: {UserId}", 
            request.Id, request.UserId);

        var apiKey = await _apiKeyRepository.GetByIdAsync(request.Id, cancellationToken);
        
        if (apiKey == null)
        {
            _logger.LogWarning("API key not found: {ApiKeyId}", request.Id);
            throw new NotFoundException($"API key with ID {request.Id} not found");
        }

        // Check if the user owns this API key
        if (apiKey.UserId != request.UserId)
        {
            _logger.LogWarning("User {UserId} attempted to deactivate API key {ApiKeyId} they don't own", 
                request.UserId, request.Id);
            throw new DomainException("You can only deactivate your own API keys");
        }

        if (!apiKey.IsActive)
        {
            _logger.LogInformation("API key {ApiKeyId} is already inactive", request.Id);
            return true;
        }

        apiKey.Deactivate();

        _apiKeyRepository.Update(apiKey);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("API key deactivated successfully: {ApiKeyId}", request.Id);
        return true;
    }
}