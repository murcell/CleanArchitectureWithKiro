using AutoMapper;
using CleanArchitecture.Application.DTOs.Authentication;
using CleanArchitecture.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace CleanArchitecture.Application.Features.Authentication.Queries;

public record GetApiKeysQuery(
    int Page = 1,
    int PageSize = 10,
    bool? IsActive = null,
    string? Name = null) : IRequest<PaginatedResult<ApiKeyDto>>;

public class GetApiKeysQueryHandler : IRequestHandler<GetApiKeysQuery, PaginatedResult<ApiKeyDto>>
{
    private readonly IApiKeyRepository _apiKeyRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<GetApiKeysQueryHandler> _logger;

    public GetApiKeysQueryHandler(
        IApiKeyRepository apiKeyRepository,
        IMapper mapper,
        ILogger<GetApiKeysQueryHandler> logger)
    {
        _apiKeyRepository = apiKeyRepository;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<PaginatedResult<ApiKeyDto>> Handle(GetApiKeysQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting API keys - Page: {Page}, PageSize: {PageSize}, IsActive: {IsActive}, Name: {Name}",
            request.Page, request.PageSize, request.IsActive, request.Name);

        var (apiKeys, totalCount) = await _apiKeyRepository.GetPagedAsync(
            request.Page,
            request.PageSize,
            request.IsActive,
            request.Name,
            cancellationToken);

        var apiKeyDtos = _mapper.Map<List<ApiKeyDto>>(apiKeys);

        var result = new PaginatedResult<ApiKeyDto>
        {
            Items = apiKeyDtos,
            Page = request.Page,
            PageSize = request.PageSize,
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling((double)totalCount / request.PageSize)
        };

        _logger.LogInformation("Retrieved {Count} API keys out of {Total} total", 
            apiKeyDtos.Count, totalCount);

        return result;
    }
}

public class PaginatedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
    public bool HasNextPage => Page < TotalPages;
    public bool HasPreviousPage => Page > 1;
}