using CleanArchitecture.Application.Common.Queries;
using CleanArchitecture.Application.DTOs;

namespace CleanArchitecture.Application.Features.Users.Queries.GetUser;

/// <summary>
/// Query to get a user by ID
/// </summary>
public record GetUserQuery(int Id) : IQuery<UserDto>;