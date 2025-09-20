using CleanArchitecture.Application.DTOs.Authentication;
using MediatR;

namespace CleanArchitecture.Application.Features.Authentication.Commands;

/// <summary>
/// Command for creating an API key
/// </summary>
public record CreateApiKeyCommand(CreateApiKeyRequest Request) : IRequest<CreateApiKeyResponse>;