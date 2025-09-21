using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using System.Data.Common;

namespace CleanArchitecture.Infrastructure.Data.Interceptors;

/// <summary>
/// Interceptor for monitoring database query performance
/// </summary>
public class PerformanceInterceptor : DbCommandInterceptor
{
    private readonly ILogger<PerformanceInterceptor> _logger;
    private readonly TimeSpan _slowQueryThreshold;

    public PerformanceInterceptor(ILogger<PerformanceInterceptor> logger)
    {
        _logger = logger;
        _slowQueryThreshold = TimeSpan.FromMilliseconds(500); // 500ms threshold
    }

    public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result,
        CancellationToken cancellationToken = default)
    {
        LogQueryStart(command, "ReaderExecutingAsync");
        return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
    }

    public override ValueTask<DbDataReader> ReaderExecutedAsync(
        DbCommand command,
        CommandExecutedEventData eventData,
        DbDataReader result,
        CancellationToken cancellationToken = default)
    {
        LogQueryEnd(command, eventData, "ReaderExecutedAsync");
        return base.ReaderExecutedAsync(command, eventData, result, cancellationToken);
    }

    public override ValueTask<InterceptionResult<object>> ScalarExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<object> result,
        CancellationToken cancellationToken = default)
    {
        LogQueryStart(command, "ScalarExecutingAsync");
        return base.ScalarExecutingAsync(command, eventData, result, cancellationToken);
    }

    public override ValueTask<object> ScalarExecutedAsync(
        DbCommand command,
        CommandExecutedEventData eventData,
        object result,
        CancellationToken cancellationToken = default)
    {
        LogQueryEnd(command, eventData, "ScalarExecutedAsync");
        return base.ScalarExecutedAsync(command, eventData, result, cancellationToken);
    }

    public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        LogQueryStart(command, "NonQueryExecutingAsync");
        return base.NonQueryExecutingAsync(command, eventData, result, cancellationToken);
    }

    public override ValueTask<int> NonQueryExecutedAsync(
        DbCommand command,
        CommandExecutedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        LogQueryEnd(command, eventData, "NonQueryExecutedAsync");
        return base.NonQueryExecutedAsync(command, eventData, result, cancellationToken);
    }

    private void LogQueryStart(DbCommand command, string operation)
    {
        _logger.LogDebug("Starting database operation: {Operation}, Command: {CommandText}", 
            operation, command.CommandText);
    }

    private void LogQueryEnd(DbCommand command, CommandExecutedEventData eventData, string operation)
    {
        var duration = eventData.Duration;
        var commandText = command.CommandText;

        if (duration > _slowQueryThreshold)
        {
            _logger.LogWarning("Slow query detected: {Operation}, Duration: {Duration}ms, Command: {CommandText}",
                operation, duration.TotalMilliseconds, commandText);
        }
        else
        {
            _logger.LogDebug("Database operation completed: {Operation}, Duration: {Duration}ms",
                operation, duration.TotalMilliseconds);
        }

        // Log query statistics
        LogQueryStatistics(command, duration);
    }

    private void LogQueryStatistics(DbCommand command, TimeSpan duration)
    {
        var commandType = GetCommandType(command.CommandText);
        var tableNames = ExtractTableNames(command.CommandText);

        _logger.LogDebug("Query Statistics - Type: {CommandType}, Duration: {Duration}ms, Tables: {Tables}, Parameters: {ParameterCount}",
            commandType, duration.TotalMilliseconds, string.Join(", ", tableNames), command.Parameters.Count);
    }

    private static string GetCommandType(string commandText)
    {
        if (string.IsNullOrWhiteSpace(commandText))
            return "Unknown";

        var trimmed = commandText.Trim().ToUpperInvariant();
        
        if (trimmed.StartsWith("SELECT"))
            return "SELECT";
        if (trimmed.StartsWith("INSERT"))
            return "INSERT";
        if (trimmed.StartsWith("UPDATE"))
            return "UPDATE";
        if (trimmed.StartsWith("DELETE"))
            return "DELETE";
        if (trimmed.StartsWith("CREATE"))
            return "CREATE";
        if (trimmed.StartsWith("ALTER"))
            return "ALTER";
        if (trimmed.StartsWith("DROP"))
            return "DROP";

        return "Other";
    }

    private static List<string> ExtractTableNames(string commandText)
    {
        var tables = new List<string>();
        
        if (string.IsNullOrWhiteSpace(commandText))
            return tables;

        // Simple table name extraction (this could be more sophisticated)
        var words = commandText.Split(new[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries);
        
        for (int i = 0; i < words.Length - 1; i++)
        {
            var word = words[i].ToUpperInvariant();
            if (word == "FROM" || word == "JOIN" || word == "UPDATE" || word == "INTO")
            {
                var nextWord = words[i + 1].Trim('[', ']', '`', '"');
                if (!string.IsNullOrWhiteSpace(nextWord) && !nextWord.StartsWith("("))
                {
                    tables.Add(nextWord);
                }
            }
        }

        return tables.Distinct().ToList();
    }
}