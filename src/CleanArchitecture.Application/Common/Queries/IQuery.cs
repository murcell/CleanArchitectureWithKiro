using MediatR;

namespace CleanArchitecture.Application.Common.Queries;

/// <summary>
/// Marker interface for queries
/// </summary>
/// <typeparam name="TResponse">The type of response</typeparam>
public interface IQuery<out TResponse> : IRequest<TResponse>
{
}