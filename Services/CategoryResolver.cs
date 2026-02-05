using System.Collections.Generic;
using System.IO;
using System.Linq;
using MagicSorter.Extensions;
using MagicSorter.Models;
using Newtonsoft.Json;

namespace MagicSorter.Services
{
    /// <summary>
    ///     Resolves item categories and finds best matching containers using specificity
    /// </summary>
    public class CategoryResolver
    {
        private static PatternMatcher _staticPatternMatcher;
        private static readonly object _staticLock = new object();

        private readonly ModConfiguration _config;
        private readonly MappingLoader _mappingLoader;
        private PatternMatcher _patternMatcher;

        public CategoryResolver(MappingLoader mappingLoader, ModConfiguration config)
        {
            _mappingLoader = mappingLoader;
            _config = config;
        }

        /// <summary>
        ///     Sets the mappings path for static pattern matching (used by tests)
        /// </summary>
        internal static void SetMappingsPath(string path)
        {
            lock (_staticLock)
            {
                _staticPatternMatcher = null;
                if (!string.IsNullOrEmpty(path) && File.Exists(path))
                {
                    var json = File.ReadAllText(path);
                    var mappings = JsonConvert.DeserializeObject<MappingData>(json);
                    if (mappings?.Patterns != null && mappings.Patterns.Count > 0)
                    {
                        _staticPatternMatcher = new PatternMatcher(mappings.Patterns);
                    }
                }
            }
        }

        private static PatternMatcher GetStaticPatternMatcher()
        {
            lock (_staticLock)
            {
                return _staticPatternMatcher;
            }
        }

        private PatternMatcher GetPatternMatcher()
        {
            if (_patternMatcher != null)
                return _patternMatcher;

            var mappings = _mappingLoader?.GetMappings();
            if (mappings?.Patterns != null && mappings.Patterns.Count > 0)
                _patternMatcher = new PatternMatcher(mappings.Patterns);

            return _patternMatcher;
        }

        /// <summary>
        ///     Gets categories for an item, checking mappings first, then patterns, then falling back to Groups
        /// </summary>
        public List<string> GetItemCategories(ItemStack itemStack)
        {
            var result = new List<string>();

            if (itemStack?.itemValue?.ItemClass == null)
                return result;

            var itemName = itemStack.itemValue.ItemClass.Name;

            // First, check explicit item mappings
            var mappings = _mappingLoader?.GetMappings();
            if (mappings != null)
            {
                var mappedCategories = mappings.GetItemCategories(itemName);
                if (mappedCategories.Count > 0) return mappedCategories;
            }

            // Second, try pattern-based matching using instance PatternMatcher
            var patternMatcher = GetPatternMatcher();
            if (patternMatcher != null)
            {
                var patternCategories = patternMatcher.GetCategories(itemName);
                if (patternCategories.Count > 0) return patternCategories;
            }

            // Fall back to built-in Groups if enabled
            if (!_config.FallbackToBuiltIn)
                return result;

            var groups = itemStack.itemValue.ItemClass.Groups;
            if (groups != null)
                result.AddRange(groups.Where(g => !string.IsNullOrEmpty(g)));

            return result;
        }

        /// <summary>
        ///     Gets categories based on item name patterns using the static PatternMatcher.
        ///     This method is primarily for unit tests - runtime code should use GetItemCategories().
        ///     Call SetMappingsPath() first to initialize the static matcher.
        /// </summary>
        internal static List<string> GetCategoriesFromPattern(string itemName)
        {
            if (string.IsNullOrEmpty(itemName))
                return new List<string>();

            var patternMatcher = GetStaticPatternMatcher();
            if (patternMatcher != null)
                return patternMatcher.GetCategories(itemName);

            return new List<string>();
        }

        /// <summary>
        ///     Finds the best container for an item based on categories and specificity
        /// </summary>
        /// <param name="itemCategories">Categories the item belongs to</param>
        /// <param name="categoryContainers">Map of category -> containers</param>
        /// <param name="itemStack">The item to find space for (for stack checking)</param>
        /// <returns>Best matching container or null</returns>
        public ContainerWrapper FindBestContainer(
            List<string> itemCategories,
            Dictionary<string, List<ContainerWrapper>> categoryContainers,
            ItemStack itemStack = null)
        {
            if (itemCategories == null || itemCategories.Count == 0)
                return null;

            var mappings = _mappingLoader?.GetMappings();
            var candidates = new List<ContainerCandidate>();
            var itemType = itemStack?.itemValue?.type ?? -1;

            // Find all matching containers with their specificity
            foreach (var category in itemCategories)
            {
                // Try exact match first
                if (categoryContainers.TryGetValue(category, out var containers))
                {
                    var specificity = GetCategorySpecificity(mappings, category);
                    foreach (var container in containers)
                        if (container.HasSpaceFor(itemStack))
                            candidates.Add(new ContainerCandidate
                            {
                                Container = container,
                                Specificity = specificity,
                                Category = category,
                                IsExactMatch = true,
                                HasSameItemType = itemType >= 0 && container.ContainsItemType(itemType)
                            });
                }

                // Try alias resolution (item category is an alias)
                if (mappings != null)
                {
                    var resolvedCategory = mappings.ResolveAlias(category);
                    if (resolvedCategory != category &&
                        categoryContainers.TryGetValue(resolvedCategory, out var aliasContainers))
                    {
                        var specificity = GetCategorySpecificity(mappings, resolvedCategory);
                        foreach (var container in aliasContainers)
                            if (container.HasSpaceFor(itemStack))
                                candidates.Add(new ContainerCandidate
                                {
                                    Container = container,
                                    Specificity = specificity,
                                    Category = resolvedCategory,
                                    IsExactMatch = true,
                                    HasSameItemType = itemType >= 0 && container.ContainsItemType(itemType)
                                });
                    }

                    // Try reverse alias (container label is an alias for item's category)
                    foreach (var kvp in categoryContainers)
                    {
                        var containerResolved = mappings.ResolveAlias(kvp.Key);
                        if (containerResolved != kvp.Key &&
                            containerResolved.IsEqual(category))
                        {
                            var specificity = GetCategorySpecificity(mappings, category);
                            foreach (var container in kvp.Value)
                            {
                                if (candidates.Any(c => c.Container == container))
                                    continue;

                                if (container.HasSpaceFor(itemStack))
                                    candidates.Add(new ContainerCandidate
                                    {
                                        Container = container,
                                        Specificity = specificity,
                                        Category = category,
                                        IsExactMatch = true,
                                        HasSameItemType = itemType >= 0 && container.ContainsItemType(itemType)
                                    });
                            }
                        }
                    }
                }

                // Note: Removed overly aggressive partial matching that was causing false matches
                // (e.g., "Melee Weapons" alias containing "weapons" would match items with category "weapons" to [ms:Melee])
                // Now we only use exact matches and alias resolution, with fallback chain for broader categories
            }

            // If no direct matches, try fallback categories
            if (candidates.Count == 0 && mappings != null)
                foreach (var category in itemCategories)
                {
                    // First resolve alias (e.g., "Decor/Miscellaneous" -> "decorations")
                    // then get fallback chain for the resolved category
                    var resolvedCategory = mappings.ResolveAlias(category);
                    var fallbackChain = mappings.GetFallbackChain(resolvedCategory);

                    // If no fallback for resolved category, try original category too
                    if (fallbackChain.Count == 0 && resolvedCategory != category)
                        fallbackChain = mappings.GetFallbackChain(category);
                    foreach (var fallbackCategory in fallbackChain)
                    {
                        // Try exact match on fallback
                        if (categoryContainers.TryGetValue(fallbackCategory, out var containers))
                        {
                            var specificity = GetCategorySpecificity(mappings, fallbackCategory);
                            foreach (var container in containers)
                                if (container.HasSpaceFor(itemStack))
                                    candidates.Add(new ContainerCandidate
                                    {
                                        Container = container,
                                        Specificity = specificity,
                                        Category = fallbackCategory,
                                        IsExactMatch = false,
                                        HasSameItemType = itemType >= 0 && container.ContainsItemType(itemType)
                                    });
                        }

                        // Try alias resolution on fallback
                        var resolvedFallback = mappings.ResolveAlias(fallbackCategory);
                        if (resolvedFallback != fallbackCategory &&
                            categoryContainers.TryGetValue(resolvedFallback, out var aliasContainers))
                        {
                            var specificity = GetCategorySpecificity(mappings, resolvedFallback);
                            foreach (var container in aliasContainers)
                            {
                                if (candidates.Any(c => c.Container == container))
                                    continue;

                                if (container.HasSpaceFor(itemStack))
                                    candidates.Add(new ContainerCandidate
                                    {
                                        Container = container,
                                        Specificity = specificity,
                                        Category = resolvedFallback,
                                        IsExactMatch = false,
                                        HasSameItemType = itemType >= 0 && container.ContainsItemType(itemType)
                                    });
                            }
                        }

                        // Also check reverse alias (container label is an alias for fallback category)
                        foreach (var kvp in categoryContainers)
                        {
                            var containerResolved = mappings.ResolveAlias(kvp.Key);
                            if (containerResolved != kvp.Key &&
                                containerResolved.IsEqual(fallbackCategory))
                            {
                                var specificity = GetCategorySpecificity(mappings, fallbackCategory);
                                foreach (var container in kvp.Value)
                                {
                                    if (candidates.Any(c => c.Container == container))
                                        continue;

                                    if (container.HasSpaceFor(itemStack))
                                        candidates.Add(new ContainerCandidate
                                        {
                                            Container = container,
                                            Specificity = specificity,
                                            Category = fallbackCategory,
                                            IsExactMatch = false,
                                            HasSameItemType = itemType >= 0 && container.ContainsItemType(itemType)
                                        });
                                }
                            }
                        }

                        // If we found candidates at this fallback level, stop searching deeper
                        if (candidates.Count > 0)
                            break;
                    }

                    if (candidates.Count > 0)
                        break;
                }

            if (candidates.Count == 0)
                return null;

            // Sort by: specificity, exact match, same item type (group like items), fullness
            if (_config.UseSpecificityResolution)
                candidates = candidates
                    .OrderByDescending(c => c.Specificity)
                    .ThenByDescending(c => c.IsExactMatch)
                    .ThenByDescending(c => c.HasSameItemType)
                    .ThenByDescending(c => c.Container.GetFullness())
                    .ToList();
            else
                // Original behavior: prefer fullest container
                candidates = candidates
                    .OrderByDescending(c => c.IsExactMatch)
                    .ThenByDescending(c => c.HasSameItemType)
                    .ThenByDescending(c => c.Container.GetFullness())
                    .ToList();

            if (_config.DebugLogging && candidates.Count > 0)
            {
                var best = candidates[0];
                Log.Out(
                    $"[MagicSorter] Best match: [{best.Category}] (specificity: {best.Specificity}, exact: {best.IsExactMatch})");
            }

            return candidates[0].Container;
        }

        /// <summary>
        ///     Checks if the item name matches any known patterns (used for debug output)
        /// </summary>
        public static bool HasPatternMatch(string itemName)
        {
            return !string.IsNullOrEmpty(itemName) && GetCategoriesFromPattern(itemName).Count > 0;
        }

        private static int GetCategorySpecificity(MappingData mappings, string category)
        {
            return mappings?.GetSpecificity(category) ?? 50;
        }

        private class ContainerCandidate
        {
            public ContainerWrapper Container { get; set; }
            public int Specificity { get; set; }
            public string Category { get; set; }
            public bool IsExactMatch { get; set; }
            public bool HasSameItemType { get; set; }
        }
    }
}