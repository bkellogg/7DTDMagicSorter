namespace MagicSorter.Models
{
    /// <summary>
    /// Defines a sorting category with specificity for prioritization
    /// </summary>
    public class CategoryDefinition
    {
        /// <summary>
        /// Specificity value - higher is more specific (e.g., "pistols" = 100 vs "weapons" = 50)
        /// </summary>
        public int Specificity { get; set; } = 50;

        /// <summary>
        /// Human-readable description of what this category contains
        /// </summary>
        public string Description { get; set; } = "";

        public CategoryDefinition() { }

        public CategoryDefinition(int specificity, string description = "")
        {
            Specificity = specificity;
            Description = description;
        }
    }
}
