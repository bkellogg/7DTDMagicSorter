using System.Diagnostics.CodeAnalysis;

namespace MagicSorter.Models
{
    /// <summary>
    ///     Defines a sorting category with specificity for prioritization.
    ///     Instantiated by JSON deserialization.
    /// </summary>
    [SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
    public class CategoryDefinition
    {
        /// <summary>
        ///     Specificity value - higher is more specific (e.g., "pistols" = 100 vs "weapons" = 50)
        /// </summary>
        // ReSharper disable once AutoPropertyCanBeMadeGetOnly.Global - needed for JSON deserialization
        public int Specificity { get; set; } = 50;

        /// <summary>
        ///     Human-readable description of what this category contains (used in JSON schema)
        /// </summary>
        // ReSharper disable once UnusedAutoPropertyAccessor.Global - used in JSON schema
        public string Description { get; set; } = "";
    }
}