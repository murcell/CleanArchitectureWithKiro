using AutoMapper;
using CleanArchitecture.Application.Common.Exceptions;
using CleanArchitecture.Application.DTOs;
using CleanArchitecture.Application.DTOs.Authentication;
using CleanArchitecture.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Configuration;

namespace CleanArchitecture.Application.Features.Authentication.Commands;

/// <summary>
/// Handler for refresh token command
/// </summary>
public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, LoginResponse>
{
    private readonly IUserRepository _userRepository;
    private readonly ITokenGenerator _tokenGenerator;
    private readonly IMapper _mapper;
    private readonly IConfiguration _configuration;

    public RefreshTokenCommandHandler(
        IUserRepository userRepository,
        ITokenGenerator tokenGenerator,
        IMapper mapper,
        IConfiguration configuration)
    {
        _userRepository = userRepository;
        _tokenGenerator = tokenGenerator;
        _mapper = mapper;
        _configuration = configuration;
    }

    public async Task<LoginResponse> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByRefreshTokenAsync(request.Request.RefreshToken, cancellationToken);
        
        if (user == null)
        {
            throw new UnauthorizedException("Invalid refresh token.");
        }

        if (!user.IsActive)
        {
            throw new UnauthorizedException("Account is deactivated.");
        }

        if (!user.IsRefreshTokenValid(request.Request.RefreshToken))
        {
            throw new UnauthorizedException("Refresh token has expired.");
        }

        // Generate new tokens
        var accessToken = _tokenGenerator.GenerateAccessToken(user);
        var newRefreshToken = _tokenGenerator.GenerateRefreshToken();
        
        // Set new refresh token (extend expiry by 7 days)
        var refreshTokenExpiry = DateTime.UtcNow.AddDays(7);
        user.SetRefreshToken(newRefreshToken, refreshTokenExpiry);
        
        await _userRepository.UpdateAsync(user, cancellationToken);

        // Get token expiry from configuration (default 15 minutes)
        var tokenExpiryMinutes = _configuration.GetValue<int>("Authentication:AccessTokenExpiryMinutes", 15);

        return new LoginResponse
        {
            AccessToken = accessToken,
            RefreshToken = newRefreshToken,
            TokenType = "Bearer",
            ExpiresIn = tokenExpiryMinutes * 60, // Convert to seconds
            User = _mapper.Map<UserDto>(user)
        };
    }
}