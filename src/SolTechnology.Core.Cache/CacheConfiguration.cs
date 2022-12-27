namespace SolTechnology.Core.Cache
{
    public class CacheConfiguration
    {
        public ExpirationMode ExpirationMode { get; set; }
        public int ExpirationSeconds { get; set; }
    }


    public enum ExpirationMode
    {
        Absolute = 1,
        Sliding = 2
    }
}