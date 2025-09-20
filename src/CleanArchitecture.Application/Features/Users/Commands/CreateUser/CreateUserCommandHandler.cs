using CleanArchitecture.Application.Common.Commands;
using CleanArchitecture.Domain.Entities;
using CleanArchitecture.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace CleanArchitecture.Application.Features.Users.Commands.CreateUser;

/// <summary>
/// Handler for CreateUserCommand
/// </summary>
public class CreateUserCommandHandler : BaseCommandHandler<CreateUserCommand, int>
{
    private readonly IRepository<User> _userRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateUserCommandHandler(
        IRepository<User> userRepository,
        IUnitOfWork unitOfWork,
        ILogger<CreateUserCommandHandler> logger) : base(logger)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
    }

    public override async Task<int> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        Logger.LogInformation("Creating user with name: {Name} and email: {Email}", request.Name, request.Email);

        // Check if user with email already exists
        var existingUser = await _userRepository.FirstOrDefaultAsync(
            u => u.Email.Value == request.Email, 
            cancellationToken);

        if (existingUser != null)
        {
            Logger.LogWarning("User with email {Email} already exists", request.Email);
            throw new InvalidOperationException($"User with email {request.Email} already exists");
        }

        // Create new user using domain factory method
        var user = User.Create(request.Name, request.Email);

        // Add user to repository
        await _userRepository.AddAsync(user, cancellationToken);

        // Save changes
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        Logger.LogInformation("User created successfully with ID: {UserId}", user.Id);

        return user.Id;
    }
}