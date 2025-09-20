using CleanArchitecture.Application.DTOs.Authentication;
using MediatR;

namespace CleanArchitecture.Application.Features.Authentication.Commands;

/// <summary>
/// Command for user registration
/// </summary>
public record RegisterCommand(RegisterRequest Request) : IRequest<LoginResponse>;