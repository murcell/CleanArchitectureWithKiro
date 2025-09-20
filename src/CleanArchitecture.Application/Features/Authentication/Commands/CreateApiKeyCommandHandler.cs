using AutoMapper;
using CleanArchitecture.Application.DTOs.Authentication;
using CleanArchitecture.Domain.Entities;
using CleanArchitecture.Domain.Interfaces;
using MediatR;

namespace CleanArchitecture.Application.Features.Authentication.Commands;

/// <summary>
/// Handler for create API key command
/// </summary>
public class CreateApiKeyCommandHandler : IRequestHandler<CreateApiKeyCommand, CreateApiKeyResponse>
{
    private readonly IApiKeyRepository _apiKeyRepository;
    private readonly ITokenGenerator _tokenGenerator;
    private readonly IMapper _mapper;

    public CreateApiKeyCommandHandler(
        IApiKeyRepository apiKeyRepository,
        ITokenGenerator tokenGenerator,
        IMapper mapper)
    {
        _apiKeyRepository = apiKeyRepository;
        _tokenGenerator = tokenGenerator;
        _mapper = mapper;
    }

    public async Task<CreateApiKeyResponse> Handle(CreateApiKeyCommand request, CancellationToken cancellationToken)
    {
        // Generate API key
        var (apiKey, keyHash, keyPrefix) = _tokenGenerator.GenerateApiKey();
        
        // Create API key entity
        var apiKeyEntity = ApiKey.Create(
            request.Request.Name,
            keyHash,
            keyPrefix,
            request.Request.Scopes,
            request.Request.Description,
            request.Request.ExpiresAt,
            request.Request.UserId);

        // Save API key
        await _apiKeyRepository.AddAsync(apiKeyEntity, cancellationToken);

        return new CreateApiKeyResponse
        {
            ApiKey = apiKey,
            ApiKeyInfo = _mapper.Map<ApiKeyDto>(apiKeyEntity)
        };
    }
}