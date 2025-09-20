using AutoMapper;
using CleanArchitecture.Application.Common.Queries;
using CleanArchitecture.Application.DTOs;
using CleanArchitecture.Domain.Entities;
using CleanArchitecture.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace CleanArchitecture.Application.Features.Users.Queries.GetUser;

/// <summary>
/// Handler for GetUserQuery
/// </summary>
public class GetUserQueryHandler : BaseQueryHandler<GetUserQuery, UserDto>
{
    private readonly IRepository<User> _userRepository;
    private readonly IMapper _mapper;

    public GetUserQueryHandler(
        IRepository<User> userRepository,
        IMapper mapper,
        ILogger<GetUserQueryHandler> logger) : base(logger)
    {
        _userRepository = userRepository;
        _mapper = mapper;
    }

    public override async Task<UserDto> Handle(GetUserQuery request, CancellationToken cancellationToken)
    {
        Logger.LogInformation("Getting user with ID: {UserId}", request.Id);

        var user = await _userRepository.GetByIdAsync(request.Id, cancellationToken);

        if (user == null)
        {
            Logger.LogWarning("User with ID {UserId} not found", request.Id);
            throw new KeyNotFoundException($"User with ID {request.Id} not found");
        }

        var userDto = _mapper.Map<UserDto>(user);

        Logger.LogInformation("User retrieved successfully: {UserId}", request.Id);

        return userDto;
    }
}