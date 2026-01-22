using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace MagicSorter.Models
{
    /// <summary>
    ///     Defines a pattern rule for matching item names to categories
    /// </summary>
    [SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
    public class PatternRule
    {
        /// <summary>
        ///     The type of pattern matching to use
        /// </summary>
        [JsonProperty("type")]
        [JsonConverter(typeof(StringEnumConverter))]
        public PatternType Type { get; set; }

        /// <summary>
        ///     The string to match against
        /// </summary>
        [JsonProperty("match")]
        public string Match { get; set; }

        /// <summary>
        ///     Categories to assign when this pattern matches (most general to most specific)
        /// </summary>
        [JsonProperty("categories")]
        public List<string> Categories { get; set; } = new List<string>();

        /// <summary>
        ///     Higher priority patterns are checked first. Default is 0.
        /// </summary>
        [JsonProperty("priority")]
        public int Priority { get; set; }

        /// <summary>
        ///     Optional pattern to exclude (item must NOT match this to qualify)
        /// </summary>
        [JsonProperty("exclude")]
        public string Exclude { get; set; }

        /// <summary>
        ///     Optional secondary match - item must also match this pattern
        /// </summary>
        [JsonProperty("alsoMatch")]
        public string AlsoMatch { get; set; }

        /// <summary>
        ///     Type of the AlsoMatch pattern (defaults to Contains)
        /// </summary>
        [JsonProperty("alsoMatchType")]
        [JsonConverter(typeof(StringEnumConverter))]
        public PatternType AlsoMatchType { get; set; } = PatternType.Contains;

        /// <summary>
        ///     Type of the Exclude pattern (defaults to Contains)
        /// </summary>
        [JsonProperty("excludeType")]
        [JsonConverter(typeof(StringEnumConverter))]
        public PatternType ExcludeType { get; set; } = PatternType.Contains;
    }

    /// <summary>
    ///     Types of pattern matching supported
    /// </summary>
    public enum PatternType
    {
        /// <summary>
        ///     Item name starts with the match string (case-insensitive)
        /// </summary>
        Prefix,

        /// <summary>
        ///     Item name contains the match string (case-insensitive)
        /// </summary>
        Contains,

        /// <summary>
        ///     Item name equals the match string exactly (case-insensitive)
        /// </summary>
        Equals
    }
}
