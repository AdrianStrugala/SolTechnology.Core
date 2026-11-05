using Microsoft.Data.SqlClient;
using SolTechnology.Core;
using SolTechnology.Core.Errors;

namespace SolTechnology.Core.SQL;

/// <summary>
/// Translates SQL Server exceptions into typed <see cref="Result"/> failures.
/// Use in repositories to avoid leaking SqlException details to the application layer.
/// </summary>
public static class SqlErrorTranslator
{
    /// <summary>
    /// Wraps a database operation and translates known SQL errors into typed Result failures.
    /// Any other exception, including a <see cref="SqlException"/> with an unknown error number, propagates.
    /// </summary>
    public static async Task<Result<T>> Execute<T>(Func<Task<T>> operation)
    {
        try
        {
            var data = await operation();
            return Result<T>.Success(data);
        }
        catch (SqlException ex) when (IsTranslatable(ex.Number))
        {
            return Result<T>.Fail(Translate(ex));
        }
    }

    /// <summary>
    /// Wraps a void database operation and translates known SQL errors into typed Result failures.
    /// Any other exception, including a <see cref="SqlException"/> with an unknown error number, propagates.
    /// </summary>
    public static async Task<Result> Execute(Func<Task> operation)
    {
        try
        {
            await operation();
            return Result.Success();
        }
        catch (SqlException ex) when (IsTranslatable(ex.Number))
        {
            return Result.Fail(Translate(ex));
        }
    }

    /// <summary>
    /// Maps a <see cref="SqlException"/> to a typed <see cref="Error"/> whose <c>Description</c> is a fixed
    /// text per error class, so constraint, table, and value names never leave the data layer.
    /// Unknown error numbers map to a plain <see cref="Error"/>.
    /// </summary>
    public static Error Translate(SqlException ex)
    {
        return ex.Number switch
        {
            2627 or 2601 => new ConflictError
            {
                Message = "Duplicate key violation",
                Description = "A row with the same unique key already exists."
            },
            1205 => new DeadlockError
            {
                Message = "Transaction was deadlocked",
                Description = "The transaction was chosen as a deadlock victim; retry the operation.",
                Recoverable = true
            },
            -2 => new TimeoutError
            {
                Message = "SQL query timed out",
                Description = "The SQL command did not finish within its timeout.",
                Recoverable = true
            },
            _ => new Error
            {
                Message = "Database error",
                Description = $"SQL Server returned error number {ex.Number}."
            }
        };
    }

    private static bool IsTranslatable(int number) => number is 2627 or 2601 or 1205 or -2;
}

