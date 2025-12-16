using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace DreamTravel.Sql.EntityConfigurations;

/// <summary>
/// Entity Framework Core value converter for AUID type.
/// Converts between Auid struct and long (BIGINT) representation in the database.
/// AUID is a 64-bit identifier stored as BIGINT for optimal performance.
/// </summary>
public class AuidValueConverter : ValueConverter<Auid, long>
{
    public AuidValueConverter()
        : base(
            auid => auid.Value,                // Convert Auid to long for database
            value => Auid.Parse(value))        // Convert long from database to Auid
    {
    }
}
