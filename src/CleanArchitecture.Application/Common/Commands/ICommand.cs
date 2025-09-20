using MediatR;

namespace CleanArchitecture.Application.Common.Commands;

/// <summary>
/// Marker interface for commands that don't return a value
/// </summary>
public interface ICommand : IRequest
{
}

/// <summary>
/// Marker interface for commands that return a value
/// </summary>
/// <typeparam name="TResponse">The type of response</typeparam>
public interface ICommand<out TResponse> : IRequest<TResponse>
{
}