using CleanArchitecture.Application.Common.Commands;

namespace CleanArchitecture.Application.Features.Users.Commands.CreateUser;

/// <summary>
/// Command to create a new user
/// </summary>
public record CreateUserCommand(string Name, string Email) : ICommand<int>;