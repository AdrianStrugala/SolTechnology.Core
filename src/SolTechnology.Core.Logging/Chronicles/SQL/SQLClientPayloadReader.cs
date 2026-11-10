using System.Data;

namespace SolTechnology.Core.Logging.Chronicles.SQL;

/// <summary>The fields Chronicle needs from a SqlClient command event.</summary>
internal readonly record struct SQLClientPayload(Guid OperationId, IDbCommand? Command, Exception? Exception);

/// <summary>
/// Reads SqlClient's command payloads (<c>SqlClientCommandBefore</c>/<c>After</c>/<c>Error</c>),
/// which are <see cref="IReadOnlyList{T}"/> of key/value pairs, without reflection and without a
/// <c>Microsoft.Data.SqlClient</c> reference: the command is read as <see cref="IDbCommand"/>.
/// </summary>
internal static class SQLClientPayloadReader
{
    internal static SQLClientPayload Read(object? payload)
    {
        if (payload is not IReadOnlyList<KeyValuePair<string, object>> pairs)
        {
            return default;
        }

        var (operationId, command, exception) = (Guid.Empty, (IDbCommand?)null, (Exception?)null);
        for (var i = 0; i < pairs.Count; i++)
        {
            switch (pairs[i].Key)
            {
                case "OperationId" when pairs[i].Value is Guid id:
                    operationId = id;
                    break;
                case "Command":
                    command = pairs[i].Value as IDbCommand;
                    break;
                case "Exception":
                    exception = pairs[i].Value as Exception;
                    break;
            }
        }

        return new SQLClientPayload(operationId, command, exception);
    }
}
