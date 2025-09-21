using CleanArchitecture.Application.Common.Interfaces;
using CleanArchitecture.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace CleanArchitecture.Application.Features.Authentication.Commands;

public record LogoutCommand(int UserId, string? RefreshToken = null) : IRequest<bool>;

public class LogoutCommandHandler : IRequestHandler<LogoutCommand, bool>
{
    private readonly IUserRepository _userRepository;
    private readonly ICacheService _cacheService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<LogoutCommandHandler> _logger;

    public LogoutCommandHandler(
        IUserRepository userRepository,
        ICacheService cacheService,
        IUnitOfWork unitOfWork,
        ILogger<LogoutCommandHandler> logger)
    {
        _userRepository = userRepository;
        _cacheService = cacheService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<bool> Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Processing logout for user: {UserId}", request.UserId);

        try
        {
            var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
            if (user == null)
            {
                _logger.LogWarning("User not found during logout: {UserId}", request.UserId);
                return false;
            }

            // Invalidate refresh token if provided
            if (!string.IsNullOrEmpty(request.RefreshToken))
            {
                // Add refresh token to blacklist cache (expires in 30 days)
                var blacklistKey = $"blacklisted_token:{request.RefreshToken}";
                await _cacheService.SetAsync(blacklistKey, "true", TimeSpan.FromDays(30));
                
                _logger.LogInformation("Refresh token blacklisted for user: {UserId}", request.UserId);
            }

            // Update user's last logout time
            user.RecordLogout();
            _userRepository.Update(user);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Clear user session cache
            var userCacheKey = $"user_session:{request.UserId}";
            await _cacheService.RemoveAsync(userCacheKey);

            _logger.LogInformation("User logged out successfully: {UserId}", request.UserId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during logout for user: {UserId}", request.UserId);
            throw;
        }
    }
}