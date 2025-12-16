namespace DreamTravel.Sql.DbModels;

public record AlternativeNameEntity : BaseEntity
{
    public long CityId { get; set; }
    public required string AlternativeName { get; set; }
    
    public CityEntity City { get; set; } = null!;
}