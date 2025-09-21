using AutoMapper;
using CleanArchitecture.Application.DTOs.Authentication;
using CleanArchitecture.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace CleanArchitecture.Application.Features.Authentication.Queries;

public record GetUserApiKeysQuery(int UserId) : IRequest<List<ApiKeyDto>>;

public class GetUserApiKeysQueryHandler : IRequestHandler<GetUserApiKeysQuery, List<ApiKeyDto>>
{
    private readonly IApiKeyRepository _apiKeyRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<GetUserApiKeysQueryHandler> _logger;

    public GetUserApiKeysQueryHandler(
        IApiKeyRepository apiKeyRepository,
        IMapper mapper,
        ILogger<GetUserApiKeysQueryHandler> logger)
    {
        _apiKeyRepository = apiKeyRepository;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<List<ApiKeyDto>> Handle(GetUserApiKeysQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting API keys for user: {UserId}", request.UserId);

        var apiKeys = await _apiKeyRepository.GetByUserIdAsync(request.UserId, cancellationToken);
        var result = _mapper.Map<List<ApiKeyDto>>(apiKeys);

        _logger.LogInformation("Retrieved {Count} API keys for user: {UserId}", 
            result.Count, request.UserId);

        return result;
    }
}