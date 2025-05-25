// using System.Text.Json;
// using SolTechnology.Core.Journey.Workflow.ChainFramework;
// // For JourneyInstance, IJourneyInstanceRepository, FlowStatus
//
// namespace SolTechnology.Core.Journey.Workflow.Persistence.Sqlite
// {
//     public class SqliteJourneyInstanceRepository : IJourneyInstanceRepository, IDisposable
//     {
//         private readonly SqliteConnection _connection;
//         private const string TableName = "JourneyInstances";
//
//         // Default database path, can be made configurable via options pattern if needed
//         private static string GetDefaultDbPath()
//         {
//             // Place the DB in a common application data folder or alongside the assembly for simplicity
//             var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
//             var companyFolder = Path.Combine(appDataPath, "SolTechnology"); // Or your company/app name
//             var appFolder = Path.Combine(companyFolder, "JourneyFramework");
//             Directory.CreateDirectory(appFolder); // Ensure directory exists
//             return Path.Combine(appFolder, "journeys.db");
//         }
//
//         public SqliteJourneyInstanceRepository() : this(GetDefaultDbPath())
//         {
//         }
//
//         public SqliteJourneyInstanceRepository(string dbPath)
//         {
//             _connection = new SqliteConnection($"Data Source={dbPath}");
//             _connection.Open(); // Open connection on creation
//             InitializeDatabase();
//         }
//
//         private void InitializeDatabase()
//         {
//             var command = _connection.CreateCommand();
//             command.CommandText = $@"
//                 CREATE TABLE IF NOT EXISTS {TableName} (
//                     JourneyId TEXT PRIMARY KEY,
//                     FlowHandlerName TEXT NOT NULL,
//                     ContextDataJson TEXT,
//                     CreatedAt TEXT NOT NULL,
//                     LastUpdatedAt TEXT NOT NULL,
//                     CurrentStatus INTEGER NOT NULL 
//                 );
//             "; // CurrentStatus stored as INTEGER (enum value)
//             command.ExecuteNonQuery();
//         }
//
//         public async Task<JourneyInstance?> GetByIdAsync(string journeyId)
//         {
//             var command = _connection.CreateCommand();
//             command.CommandText = $@"
//                 SELECT JourneyId, FlowHandlerName, ContextDataJson, CreatedAt, LastUpdatedAt, CurrentStatus
//                 FROM {TableName}
//                 WHERE JourneyId = $journeyId;
//             ";
//             command.Parameters.AddWithValue("$journeyId", journeyId);
//
//             using (var reader = await command.ExecuteReaderAsync())
//             {
//                 if (await reader.ReadAsync())
//                 {
//                     var flowHandlerName = reader.GetString(reader.GetOrdinal("FlowHandlerName"));
//                     var contextDataJson = reader.IsDBNull(reader.GetOrdinal("ContextDataJson")) ? null : reader.GetString(reader.GetOrdinal("ContextDataJson"));
//                     
//                     Type? contextType = null;
//                     object? contextData = null;
//
//                     if (!string.IsNullOrEmpty(flowHandlerName) && !string.IsNullOrEmpty(contextDataJson))
//                     {
//                         // Infer TContext type from FlowHandlerName.
//                         // FlowHandlerName should store AssemblyQualifiedName of the PausableChainHandler<TInput, TContext, TOutput>.
//                         // We need to extract TContext from it.
//                         Type? handlerType = Type.GetType(flowHandlerName);
//                         if (handlerType != null)
//                         {
//                             // Assuming base type is PausableChainHandler<,,> and TContext is the second generic argument
//                             if (handlerType.BaseType != null && handlerType.BaseType.IsGenericType && 
//                                 handlerType.BaseType.GetGenericTypeDefinition() == typeof(PausableChainHandler<,,>))
//                             {
//                                 contextType = handlerType.BaseType.GetGenericArguments()[1]; // Index 1 for TContext
//                                 try
//                                 {
//                                      contextData = JsonSerializer.Deserialize(contextDataJson, contextType, GetJsonSerializerOptions());
//                                 }
//                                 catch (JsonException ex)
//                                 {
//                                      // Log deserialization error, contextData will remain null
//                                      System.Diagnostics.Debug.WriteLine($"Error deserializing ContextData for JourneyId {journeyId}: {ex.Message}");
//                                 }
//                             }
//                         }
//                     }
//                     
//                     return new JourneyInstance(
//                         reader.GetString(reader.GetOrdinal("JourneyId")),
//                         flowHandlerName,
//                         contextData ?? new object() // Fallback to empty object if deserialization fails or no data
//                     )
//                     {
//                         CreatedAt = DateTime.Parse(reader.GetString(reader.GetOrdinal("CreatedAt"))),
//                         LastUpdatedAt = DateTime.Parse(reader.GetString(reader.GetOrdinal("LastUpdatedAt"))),
//                         CurrentStatus = (FlowStatus)reader.GetInt32(reader.GetOrdinal("CurrentStatus"))
//                     };
//                 }
//             }
//             return null;
//         }
//
//         public async Task SaveAsync(JourneyInstance journeyInstance)
//         {
//             if (journeyInstance == null) throw new ArgumentNullException(nameof(journeyInstance));
//
//             var command = _connection.CreateCommand();
//             // Using UPSERT for SQLite
//             command.CommandText = $@"
//                 INSERT INTO {TableName} (JourneyId, FlowHandlerName, ContextDataJson, CreatedAt, LastUpdatedAt, CurrentStatus)
//                 VALUES ($journeyId, $flowHandlerName, $contextDataJson, $createdAt, $lastUpdatedAt, $currentStatus)
//                 ON CONFLICT(JourneyId) DO UPDATE SET
//                     FlowHandlerName = excluded.FlowHandlerName,
//                     ContextDataJson = excluded.ContextDataJson,
//                     LastUpdatedAt = excluded.LastUpdatedAt,
//                     CurrentStatus = excluded.CurrentStatus;
//             ";
//
//             command.Parameters.AddWithValue("$journeyId", journeyInstance.JourneyId);
//             command.Parameters.AddWithValue("$flowHandlerName", journeyInstance.FlowHandlerName);
//
//             string? contextDataJson = null;
//             if (journeyInstance.ContextData != null)
//             {
//                 // Serialize ContextData using its actual type
//                 try 
//                 {
//                     contextDataJson = JsonSerializer.Serialize(journeyInstance.ContextData, journeyInstance.ContextData.GetType(), GetJsonSerializerOptions());
//                 }
//                 catch (JsonException ex)
//                 {
//                     // Log serialization error
//                     System.Diagnostics.Debug.WriteLine($"Error serializing ContextData for JourneyId {journeyInstance.JourneyId}: {ex.Message}");
//                     // Potentially throw or handle as per application requirements. Here, we'll save null.
//                 }
//             }
//             command.Parameters.AddWithValue("$contextDataJson", (object?)contextDataJson ?? DBNull.Value);
//             
//             command.Parameters.AddWithValue("$createdAt", journeyInstance.CreatedAt.ToString("o")); // ISO 8601
//             command.Parameters.AddWithValue("$lastUpdatedAt", DateTime.UtcNow.ToString("o")); // Update LastUpdatedAt on save
//             command.Parameters.AddWithValue("$currentStatus", (int)journeyInstance.CurrentStatus);
//
//             await command.ExecuteNonQueryAsync();
//         }
//
//         public async Task DeleteAsync(string journeyId)
//         {
//             var command = _connection.CreateCommand();
//             command.CommandText = $"DELETE FROM {TableName} WHERE JourneyId = $journeyId;";
//             command.Parameters.AddWithValue("$journeyId", journeyId);
//             await command.ExecuteNonQueryAsync();
//         }
//
//         private static JsonSerializerOptions GetJsonSerializerOptions()
//         {
//             return new JsonSerializerOptions
//             {
//                 // TypeNameHandling is generally discouraged for security reasons if JSON comes from untrusted sources.
//                 // However, for internal storage where we control the types, it can be useful.
//                 // For simplicity and security, we rely on knowing the TContext type via FlowHandlerName on deserialization.
//                 // Add any custom converters if needed for specific types within ContextData.
//                 // PropertyNameCaseInsensitive = true, // If needed
//             };
//         }
//
//         public void Dispose()
//         {
//             _connection.Close(); // Close connection on dispose
//             _connection.Dispose();
//             GC.SuppressFinalize(this);
//         }
//     }
// }
