using Microsoft.Extensions.Logging;

namespace CleanArchitecture.Application.Common.Commands;

/// <summary>
/// Base class for command handlers providing common functionality
/// </summary>
/// <typeparam name="TCommand">The type of command</typeparam>
public abstract class BaseCommandHandler<TCommand> : ICommandHandler<TCommand>
    where TCommand : ICommand
{
    protected readonly ILogger<BaseCommandHandler<TCommand>> Logger;

    protected BaseCommandHandler(ILogger<BaseCommandHandler<TCommand>> logger)
    {
        Logger = logger;
    }

    public abstract Task Handle(TCommand request, CancellationToken cancellationToken);
}

/// <summary>
/// Base class for command handlers that return a response
/// </summary>
/// <typeparam name="TCommand">The type of command</typeparam>
/// <typeparam name="TResponse">The type of response</typeparam>
public abstract class BaseCommandHandler<TCommand, TResponse> : ICommandHandler<TCommand, TResponse>
    where TCommand : ICommand<TResponse>
{
    protected readonly ILogger<BaseCommandHandler<TCommand, TResponse>> Logger;

    protected BaseCommandHandler(ILogger<BaseCommandHandler<TCommand, TResponse>> logger)
    {
        Logger = logger;
    }

    public abstract Task<TResponse> Handle(TCommand request, CancellationToken cancellationToken);
}