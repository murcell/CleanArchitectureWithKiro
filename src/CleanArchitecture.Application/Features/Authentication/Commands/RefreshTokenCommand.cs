using CleanArchitecture.Application.DTOs.Authentication;
using MediatR;

namespace CleanArchitecture.Application.Features.Authentication.Commands;

/// <summary>
/// Command for refreshing access token
/// </summary>
public record RefreshTokenCommand(RefreshTokenRequest Request) : IRequest<LoginResponse>;