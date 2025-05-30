namespace SolTechnology.Core.Journey.Models;

public class DataField
{
    public required string Name { get; set; }                
    public required string Type { get; set; }            
    public required bool IsComplex { get; set; }
    public List<DataField> Children { get; set; } = new();
}