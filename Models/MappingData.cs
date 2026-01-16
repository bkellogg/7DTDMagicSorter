using System.Collections.Generic;

namespace MagicSorter.Models
{
    /// <summary>
    /// Complete mapping data structure containing categories, items, aliases, and tags
    /// </summary>
    public class MappingData
    {
        /// <summary>
        /// Version string for cache invalidation
        /// </summary>
        public string Version { get; set; } = "1.0.0";

        /// <summary>
        /// Category definitions with specificity values
        /// Key: category name (lowercase), Value: CategoryDefinition
        /// </summary>
        public Dictionary<string, CategoryDefinition> Categories { get; set; }
            = new Dictionary<string, CategoryDefinition>(System.StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Item-to-category mappings
        /// Key: item name/ID, Value: list of category names (most general to most specific)
        /// </summary>
        public Dictionary<string, List<string>> Items { get; set; }
            = new Dictionary<string, List<string>>(System.StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Container label aliases
        /// Key: alias (e.g., "guns"), Value: canonical category name (e.g., "weapons")
        /// </summary>
        public Dictionary<string, string> ContainerAliases { get; set; }
            = new Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Tag definitions for grouping related categories
        /// Key: tag name, Value: list of category names
        /// </summary>
        public Dictionary<string, List<string>> Tags { get; set; }
            = new Dictionary<string, List<string>>(System.StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Category fallback chain - if no container for a category, try the fallback
        /// Key: category name, Value: fallback category name
        /// </summary>
        public Dictionary<string, string> CategoryFallbacks { get; set; }
            = new Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Gets the specificity for a category, returning default if not found
        /// </summary>
        public int GetSpecificity(string category)
        {
            if (Categories.TryGetValue(category, out var def))
            {
                return def.Specificity;
            }
            return 50; // Default specificity
        }

        /// <summary>
        /// Gets item categories from mappings, or empty list if not found
        /// </summary>
        public List<string> GetItemCategories(string itemName)
        {
            if (Items.TryGetValue(itemName, out var categories))
            {
                return categories;
            }
            return new List<string>();
        }

        /// <summary>
        /// Resolves an alias to its canonical category name
        /// </summary>
        public string ResolveAlias(string label)
        {
            if (ContainerAliases.TryGetValue(label, out var canonical))
            {
                return canonical;
            }
            return label;
        }

        /// <summary>
        /// Gets all fallback categories for a given category (walks the chain)
        /// </summary>
        public List<string> GetFallbackChain(string category)
        {
            var result = new List<string>();
            var current = category;
            var visited = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);

            while (CategoryFallbacks.TryGetValue(current, out var fallback))
            {
                // Prevent infinite loops
                if (visited.Contains(fallback))
                    break;

                result.Add(fallback);
                visited.Add(fallback);
                current = fallback;
            }

            return result;
        }
    }
}
