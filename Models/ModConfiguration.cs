namespace MagicSorter.Models
{
    /// <summary>
    /// Configuration settings for MagicSorter mod
    /// </summary>
    public class ModConfiguration
    {
        /// <summary>
        /// URL to fetch remote category mappings from
        /// </summary>
        public string RemoteMappingsUrl { get; set; } = "";

        /// <summary>
        /// Hours before cached mappings expire and need refresh
        /// </summary>
        public int CacheDurationHours { get; set; } = 24;

        /// <summary>
        /// Whether to fall back to built-in Groups matching when item not in mappings
        /// </summary>
        public bool FallbackToBuiltIn { get; set; } = true;

        /// <summary>
        /// Whether to use specificity-based resolution for container matching
        /// </summary>
        public bool UseSpecificityResolution { get; set; } = true;

        /// <summary>
        /// Default range for container search (in blocks)
        /// </summary>
        public int DefaultRange { get; set; } = 20;

        /// <summary>
        /// Whether to show verbose debug logging
        /// </summary>
        public bool DebugLogging { get; set; } = false;

        /// <summary>
        /// Connection timeout in seconds for remote fetch
        /// </summary>
        public int ConnectionTimeoutSeconds { get; set; } = 10;
    }
}
