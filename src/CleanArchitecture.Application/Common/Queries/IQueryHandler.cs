using MediatR;

namespace CleanArchitecture.Application.Common.Queries;

/// <summary>
/// Handler interface for queries
/// </summary>
/// <typeparam name="TQuery">The type of query</typeparam>
/// <typeparam name="TResponse">The type of response</typeparam>
public interface IQueryHandler<in TQuery, TResponse> : IRequestHandler<TQuery, TResponse>
    where TQuery : IQuery<TResponse>
{
}