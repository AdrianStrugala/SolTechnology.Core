namespace SolTechnology.Core.Cache
{
    /// <summary>
    /// Expiration settings of the in-memory <see cref="ISingletonCache"/>, passed to <c>AddSolLocalCache</c>.
    /// </summary>
    public class CacheConfiguration
    {
        /// <summary>Whether an entry expires a fixed time after it is added or after its last read.</summary>
        public ExpirationMode ExpirationMode { get; set; }

        /// <summary>Entry lifetime in seconds; must be greater than 0, or the host fails at start.</summary>
        public int ExpirationSeconds { get; set; }
    }


    /// <summary>How <see cref="CacheConfiguration.ExpirationSeconds"/> is measured.</summary>
    public enum ExpirationMode
    {
        /// <summary>The entry expires the configured time after it was added.</summary>
        Absolute = 1,

        /// <summary>The entry expires the configured time after it was last read.</summary>
        Sliding = 2
    }
}