using AutoMapper;
using CleanArchitecture.Application.Common.Exceptions;
using CleanArchitecture.Application.DTOs;
using CleanArchitecture.Application.DTOs.Authentication;
using CleanArchitecture.Domain.Entities;
using CleanArchitecture.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Configuration;

namespace CleanArchitecture.Application.Features.Authentication.Commands;

/// <summary>
/// Handler for register command
/// </summary>
public class RegisterCommandHandler : IRequestHandler<RegisterCommand, LoginResponse>
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenGenerator _tokenGenerator;
    private readonly IMapper _mapper;
    private readonly IConfiguration _configuration;

    public RegisterCommandHandler(
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher,
        ITokenGenerator tokenGenerator,
        IMapper mapper,
        IConfiguration configuration)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
        _tokenGenerator = tokenGenerator;
        _mapper = mapper;
        _configuration = configuration;
    }

    public async Task<LoginResponse> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        // Check if email is already taken
        if (await _userRepository.IsEmailTakenAsync(request.Request.Email, cancellationToken))
        {
            throw new ConflictException("Email is already registered.");
        }

        // Create new user
        var user = User.Create(request.Request.Name, request.Request.Email);
        
        // Hash password and set it
        var passwordHash = _passwordHasher.HashPassword(request.Request.Password);
        user.SetPasswordHash(passwordHash);
        
        // Generate email confirmation token
        var emailConfirmationToken = _tokenGenerator.GenerateEmailConfirmationToken();
        var tokenExpiry = DateTime.UtcNow.AddHours(24); // 24 hours to confirm email
        user.SetEmailConfirmationToken(emailConfirmationToken, tokenExpiry);

        // Generate tokens for immediate login
        var refreshToken = _tokenGenerator.GenerateRefreshToken();
        
        // Set refresh token (1 day expiry for new registrations)
        var refreshTokenExpiry = DateTime.UtcNow.AddDays(1);
        user.SetRefreshToken(refreshToken, refreshTokenExpiry);
        
        // Record login
        user.RecordLogin();

        // Save user (this will generate the Id)
        await _userRepository.AddAsync(user, cancellationToken);
        
        // Commit changes to database
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        
        // Generate access token after user is saved (so we have the Id)
        var accessToken = _tokenGenerator.GenerateAccessToken(user);

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