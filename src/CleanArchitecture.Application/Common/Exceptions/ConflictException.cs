namespace CleanArchitecture.Application.Common.Exceptions;

/// <summary>
/// Exception thrown when there is a conflict with the current state of the resource
/// </summary>
public class ConflictException : Exception
{
    public ConflictException() : base("Conflict occurred")
    {
    }

    public ConflictException(string message) : base(message)
    {
    }

    public ConflictException(string message, Exception innerException) : base(message, innerException)
    {
    }
}