using MediatR;

namespace CleanArchitecture.Application.Common.Commands;

/// <summary>
/// Handler interface for commands that don't return a value
/// </summary>
/// <typeparam name="TCommand">The type of command</typeparam>
public interface ICommandHandler<in TCommand> : IRequestHandler<TCommand>
    where TCommand : ICommand
{
}

/// <summary>
/// Handler interface for commands that return a value
/// </summary>
/// <typeparam name="TCommand">The type of command</typeparam>
/// <typeparam name="TResponse">The type of response</typeparam>
public interface ICommandHandler<in TCommand, TResponse> : IRequestHandler<TCommand, TResponse>
    where TCommand : ICommand<TResponse>
{
}