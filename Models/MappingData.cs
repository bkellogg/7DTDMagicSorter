using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace MagicSorter.Models
{
    /// <summary>
    ///     Complete mapping data structure containing categories, items, aliases, and tags
    /// </summary>
    public class MappingData
    {
        /// <summary>
        ///     Version string for cache invalidation
        /// </summary>
        // ReSharper disable once AutoPropertyCanBeMadeGetOnly.Global - needed for JSON deserialization
        public string Version { get; set; } = "1.0.0";

        /// <summary>
        ///     Category definitions with specificity values
        ///     Key: category name (lowercase), Value: CategoryDefinition
        /// </summary>
        public Dictionary<string, CategoryDefinition> Categories { get; private set; }
            = new Dictionary<string, CategoryDefinition>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        ///     Item-to-category mappings
        ///     Key: item name/ID, Value: list of category names (most general to most specific)
        /// </summary>
        public Dictionary<string, List<string>> Items { get; private set; }
            = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        ///     Container label aliases
        ///     Key: alias (e.g., "guns"), Value: canonical category name (e.g., "weapons")
        /// </summary>
        [JsonProperty("aliases")]
        public Dictionary<string, string> ContainerAliases { get; set; }
            = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        ///     Tag definitions for grouping related categories
        ///     Key: tag name, Value: list of category names
        /// </summary>
        public Dictionary<string, List<string>> Tags { get; private set; }
            = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        ///     Category fallback chain - if no container for a category, try the fallback
        ///     Key: category name, Value: fallback category name
        /// </summary>
        private Dictionary<string, string> CategoryFallbacks { get; set; }
            = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        ///     Gets the specificity for a category, returning default if not found
        /// </summary>
        public int GetSpecificity(string category)
        {
            return Categories.TryGetValue(category, out var def) ? def.Specificity : 50;
        }

        /// <summary>
        ///     Gets item categories from mappings, or empty list if not found
        /// </summary>
        public List<string> GetItemCategories(string itemName)
        {
            return Items.TryGetValue(itemName, out var categories) ? categories : new List<string>();
        }

        /// <summary>
        ///     Resolves an alias to its canonical category name
        /// </summary>
        public string ResolveAlias(string label)
        {
            return ContainerAliases.TryGetValue(label, out var canonical) ? canonical : label;
        }

        /// <summary>
        ///     Gets all fallback categories for a given category (walks the chain)
        /// </summary>
        public List<string> GetFallbackChain(string category)
        {
            var result = new List<string>();
            var current = category;
            var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

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

        /// <summary>
        ///     Converts all dictionaries to case-insensitive versions.
        ///     Call this after JSON deserialization since Newtonsoft creates new dictionary instances.
        /// </summary>
        public void NormalizeDictionaries()
        {
            if (Categories?.Count > 0)
                Categories = new Dictionary<string, CategoryDefinition>(Categories, StringComparer.OrdinalIgnoreCase);

            if (Items?.Count > 0)
                Items = new Dictionary<string, List<string>>(Items, StringComparer.OrdinalIgnoreCase);

            if (ContainerAliases?.Count > 0)
                ContainerAliases = new Dictionary<string, string>(ContainerAliases, StringComparer.OrdinalIgnoreCase);

            if (Tags?.Count > 0)
                Tags = new Dictionary<string, List<string>>(Tags, StringComparer.OrdinalIgnoreCase);

            if (CategoryFallbacks?.Count > 0)
                CategoryFallbacks = new Dictionary<string, string>(CategoryFallbacks, StringComparer.OrdinalIgnoreCase);
        }
    }
}