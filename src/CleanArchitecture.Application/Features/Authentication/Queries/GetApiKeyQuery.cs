using AutoMapper;
using CleanArchitecture.Application.DTOs.Authentication;
using CleanArchitecture.Domain.Interfaces;
using CleanArchitecture.Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace CleanArchitecture.Application.Features.Authentication.Queries;

public record GetApiKeyQuery(int Id) : IRequest<ApiKeyDto>;

public class GetApiKeyQueryHandler : IRequestHandler<GetApiKeyQuery, ApiKeyDto>
{
    private readonly IApiKeyRepository _apiKeyRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<GetApiKeyQueryHandler> _logger;

    public GetApiKeyQueryHandler(
        IApiKeyRepository apiKeyRepository,
        IMapper mapper,
        ILogger<GetApiKeyQueryHandler> logger)
    {
        _apiKeyRepository = apiKeyRepository;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<ApiKeyDto> Handle(GetApiKeyQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting API key with ID: {ApiKeyId}", request.Id);

        var apiKey = await _apiKeyRepository.GetByIdAsync(request.Id, cancellationToken);
        
        if (apiKey == null)
        {
            _logger.LogWarning("API key not found: {ApiKeyId}", request.Id);
            throw new NotFoundException($"API key with ID {request.Id} not found");
        }

        var result = _mapper.Map<ApiKeyDto>(apiKey);
        _logger.LogInformation("API key retrieved successfully: {ApiKeyId}", request.Id);
        
        return result;
    }
}