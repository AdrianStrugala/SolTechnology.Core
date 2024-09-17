namespace SolTechnology.TaleCode.Domain;

public abstract record EntityBase
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ModifiedAt { get; set; }
}