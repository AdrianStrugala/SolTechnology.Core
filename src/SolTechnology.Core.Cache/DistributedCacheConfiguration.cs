namespace SolTechnology.Core.Cache;

public class DistributedCacheConfiguration
{
    public string ConnectionString { get; set; } = null!;
    public string InstanceName { get; set; } = "SolTechnology:";
    public int ExpirationSeconds { get; set; } = 5 * 60;
}

