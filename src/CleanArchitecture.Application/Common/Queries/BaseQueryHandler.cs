using Microsoft.Extensions.Logging;

namespace CleanArchitecture.Application.Common.Queries;

/// <summary>
/// Base class for query handlers providing common functionality
/// </summary>
/// <typeparam name="TQuery">The type of query</typeparam>
/// <typeparam name="TResponse">The type of response</typeparam>
public abstract class BaseQueryHandler<TQuery, TResponse> : IQueryHandler<TQuery, TResponse>
    where TQuery : IQuery<TResponse>
{
    protected readonly ILogger<BaseQueryHandler<TQuery, TResponse>> Logger;

    protected BaseQueryHandler(ILogger<BaseQueryHandler<TQuery, TResponse>> logger)
    {
        Logger = logger;
    }

    public abstract Task<TResponse> Handle(TQuery request, CancellationToken cancellationToken);
}