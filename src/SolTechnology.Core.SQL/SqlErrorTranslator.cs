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
    /// Unknown exceptions are re-thrown.
    /// </summary>
    public static async Task<Result<T>> Execute<T>(Func<Task<T>> operation)
    {
        try
        {
            var data = await operation();
            return Result<T>.Success(data);
        }
        catch (SqlException ex)
        {
            return Result<T>.Fail(Translate(ex));
        }
    }

    /// <summary>
    /// Wraps a void database operation and translates known SQL errors into typed Result failures.
    /// </summary>
    public static async Task<Result> Execute(Func<Task> operation)
    {
        try
        {
            await operation();
            return Result.Success();
        }
        catch (SqlException ex)
        {
            return Result.Fail(Translate(ex));
        }
    }

    /// <summary>
    /// Maps a <see cref="SqlException"/> to a typed <see cref="Error"/>.
    /// Returns null if the error is not a known translatable case (caller should re-throw).
    /// </summary>
    public static Error Translate(SqlException ex)
    {
        return ex.Number switch
        {
            2627 or 2601 => new ConflictError
            {
                Message = "Duplicate key violation",
                Description = ex.Message
            },
            1205 => new DeadlockError
            {
                Message = "Transaction was deadlocked",
                Description = ex.Message,
                Recoverable = true
            },
            -2 => new TimeoutError
            {
                Message = "SQL query timed out",
                Description = ex.Message,
                Recoverable = true
            },
            _ => new Error
            {
                Message = "Database error",
                Description = ex.Message
            }
        };
    }
}

