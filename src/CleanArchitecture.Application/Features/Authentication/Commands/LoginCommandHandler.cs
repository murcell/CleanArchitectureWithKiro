using AutoMapper;
using CleanArchitecture.Application.Common.Exceptions;
using CleanArchitecture.Application.DTOs;
using CleanArchitecture.Application.DTOs.Authentication;
using CleanArchitecture.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Configuration;

namespace CleanArchitecture.Application.Features.Authentication.Commands;

/// <summary>
/// Handler for login command
/// </summary>
public class LoginCommandHandler : IRequestHandler<LoginCommand, LoginResponse>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenGenerator _tokenGenerator;
    private readonly IMapper _mapper;
    private readonly IConfiguration _configuration;

    public LoginCommandHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        ITokenGenerator tokenGenerator,
        IMapper mapper,
        IConfiguration configuration)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _tokenGenerator = tokenGenerator;
        _mapper = mapper;
        _configuration = configuration;
    }

    public async Task<LoginResponse> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByEmailAsync(request.Request.Email, cancellationToken);
        
        if (user == null)
        {
            throw new UnauthorizedException("Invalid email or password.");
        }

        if (!user.IsActive)
        {
            throw new UnauthorizedException("Account is deactivated.");
        }

        if (user.IsLockedOut())
        {
            throw new UnauthorizedException($"Account is locked until {user.LockoutEnd:yyyy-MM-dd HH:mm:ss} UTC.");
        }

        if (!_passwordHasher.VerifyPassword(user.PasswordHash, request.Request.Password))
        {
            user.RecordFailedLoginAttempt();
            await _userRepository.UpdateAsync(user, cancellationToken);
            
            throw new UnauthorizedException("Invalid email or password.");
        }

        // Generate tokens
        var accessToken = _tokenGenerator.GenerateAccessToken(user);
        var refreshToken = _tokenGenerator.GenerateRefreshToken();
        
        // Set refresh token expiry (7 days for remember me, 1 day otherwise)
        var refreshTokenExpiry = DateTime.UtcNow.AddDays(request.Request.RememberMe ? 7 : 1);
        user.SetRefreshToken(refreshToken, refreshTokenExpiry);
        
        // Record successful login
        user.RecordLogin();
        await _userRepository.UpdateAsync(user, cancellationToken);

        // Get token expiry from configuration (default 15 minutes)
        var tokenExpiryMinutes = _configuration.GetValue<int>("Authentication:AccessTokenExpiryMinutes", 15);

        return new LoginResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            TokenType = "Bearer",
            ExpiresIn = tokenExpiryMinutes * 60, // Convert to seconds
            User = _mapper.Map<UserDto>(user)
        };
    }
}