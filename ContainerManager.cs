using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MagicSorter.Models;
using MagicSorter.Services;
using UnityEngine;

namespace MagicSorter
{
    public class ContainerManager
    {
        private const string SortMeTag = "[MagicSort]";
        private const string SortPrefix = "[ms:";
        private const string UnknownCategory = "Unknown";
        private const string UnknownItemName = "Unknown Item";

        private readonly EntityPlayer _player;
        private readonly int _range;
        private readonly CategoryResolver _resolver;

        private readonly Dictionary<string, List<string>> _sortedItems =
            new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

        private readonly List<string> _failedItems = new List<string>();
        private readonly World _world;

        public ContainerManager(EntityPlayer player, int range)
        {
            _player = player;
            _range = range;
            _world = GameManager.Instance.World;
            _resolver = MagicSorterMod.Resolver;
        }

        public void Sort()
        {
            try
            {
                SortInternal();
            }
            catch (Exception ex)
            {
                MagicSorterMod.Output($"[MagicSorter] Unexpected error: {ex.Message}");
            }
        }

        public void Resort()
        {
            try
            {
                ResortInternal();
            }
            catch (Exception ex)
            {
                MagicSorterMod.Output($"[MagicSorter] Unexpected error: {ex.Message}");
            }
        }

        public void ListContainers()
        {
            try
            {
                var containers = FindContainersInRange();
                if (containers.Count == 0)
                {
                    MagicSorterMod.Output("[MagicSorter] No containers found in range.");
                    return;
                }

                var sortMe = FindSortMeContainer(containers);
                var categoryMap = BuildCategoryMap(containers, sortMe);

                MagicSorterMod.Output($"[MagicSorter] Found {containers.Count} containers in range:");

                // List SortMe container
                if (sortMe != null)
                {
                    var items = sortMe.GetItems();
                    var itemCount = items?.Count(s => !s.IsEmpty()) ?? 0;
                    MagicSorterMod.Output($"  [MagicSort] at {sortMe.Position} - {itemCount} items");
                }
                else
                {
                    MagicSorterMod.Output("  [MagicSort] - NOT FOUND");
                }

                // List Sort containers by category
                foreach (var kvp in categoryMap.OrderBy(k => k.Key))
                foreach (var container in kvp.Value)
                {
                    var items = container.GetItems();
                    var used = items?.Count(s => !s.IsEmpty()) ?? 0;
                    var total = items?.Length ?? 0;
                    MagicSorterMod.Output($"  [ms:{kvp.Key}] at {container.Position} - {used}/{total} slots");
                }
            }
            catch (Exception ex)
            {
                MagicSorterMod.Output($"[MagicSorter] Unexpected error: {ex.Message}");
            }
        }

        public void Plan()
        {
            try
            {
                PlanInternal();
            }
            catch (Exception ex)
            {
                MagicSorterMod.Output($"[MagicSorter] Unexpected error: {ex.Message}");
            }
        }

        public void Scan()
        {
            try
            {
                ScanInternal();
            }
            catch (Exception ex)
            {
                MagicSorterMod.Output($"[MagicSorter] Unexpected error: {ex.Message}");
            }
        }

        private void ScanInternal()
        {
            var containers = FindContainersInRange();
            var sortMeContainer = FindSortMeContainer(containers);

            if (sortMeContainer == null)
            {
                MagicSorterMod.Output("[MagicSorter] No [MagicSort] container found in range.");
                return;
            }

            var items = sortMeContainer.GetItems();
            if (items == null)
            {
                MagicSorterMod.Output("[MagicSorter] Could not access items in [MagicSort] container");
                return;
            }

            // Group items by category
            var itemsByCategory = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

            foreach (var itemStack in items)
            {
                if (itemStack.IsEmpty()) continue;

                var itemName = GetItemName(itemStack);
                var itemDesc = $"{itemName} x{itemStack.count}";
                var categories = GetItemCategories(itemStack);

                if (categories.Count == 0)
                {
                    if (!itemsByCategory.ContainsKey("(no category)"))
                        itemsByCategory["(no category)"] = new List<string>();
                    itemsByCategory["(no category)"].Add(itemDesc);
                }
                else
                {
                    // Use the most specific (last) category for grouping
                    var primaryCategory = categories[categories.Count - 1];
                    if (!itemsByCategory.ContainsKey(primaryCategory))
                        itemsByCategory[primaryCategory] = new List<string>();
                    itemsByCategory[primaryCategory].Add(itemDesc);
                }
            }

            if (itemsByCategory.Count == 0)
            {
                MagicSorterMod.Output("[MagicSorter] [MagicSort] is empty");
                return;
            }

            MagicSorterMod.Output("[MagicSorter] Items in [MagicSort] by category:");
            foreach (var kvp in itemsByCategory.OrderBy(k => k.Key))
            {
                MagicSorterMod.Output($"  {kvp.Key}:");
                foreach (var item in kvp.Value) MagicSorterMod.Output($"    - {item}");
            }
        }

        public void Missing()
        {
            try
            {
                MissingInternal();
            }
            catch (Exception ex)
            {
                MagicSorterMod.Output($"[MagicSorter] Unexpected error: {ex.Message}");
            }
        }

        public void Invalid()
        {
            try
            {
                InvalidInternal();
            }
            catch (Exception ex)
            {
                MagicSorterMod.Output($"[MagicSorter] Unexpected error: {ex.Message}");
            }
        }

        private void InvalidInternal()
        {
            var containers = FindContainersInRange();
            if (containers.Count == 0)
            {
                MagicSorterMod.Output("[MagicSorter] No containers found in range.");
                return;
            }

            var sortMe = FindSortMeContainer(containers);
            var categoryMap = BuildCategoryMap(containers, sortMe);

            if (categoryMap.Count == 0)
            {
                MagicSorterMod.Output("[MagicSorter] No [ms:X] containers found in range.");
                return;
            }

            var mappings = MagicSorterMod.MappingLoader?.GetMappings();
            var invalidContainers = new List<(string label, Vector3i pos, string suggestion)>();

            foreach (var kvp in categoryMap)
            {
                var label = kvp.Key;
                var isValid = false;
                string suggestion = null;

                // Check if it's a known category in mappings
                if (mappings != null && mappings.Categories.ContainsKey(label))
                {
                    isValid = true;
                }
                // Check if it's an alias
                else if (mappings != null && mappings.ContainerAliases.ContainsKey(label))
                {
                    isValid = true;
                }
                // Check if it resolves to a known category
                else if (mappings != null)
                {
                    var resolved = mappings.ResolveAlias(label);
                    if (resolved != label && mappings.Categories.ContainsKey(resolved)) isValid = true;
                }

                // If still not valid, check for close matches (typos)
                if (!isValid && mappings != null) suggestion = FindClosestCategory(label, mappings);

                // "Unknown" is always valid as a fallback
                if (label.Equals(UnknownCategory, StringComparison.OrdinalIgnoreCase)) isValid = true;

                if (!isValid)
                    foreach (var container in kvp.Value)
                        invalidContainers.Add((label, container.Position, suggestion));
            }

            if (invalidContainers.Count == 0)
            {
                MagicSorterMod.Output("[MagicSorter] All container labels are valid!");
                return;
            }

            MagicSorterMod.Output($"[MagicSorter] Found {invalidContainers.Count} container(s) with invalid/unknown labels:");
            foreach (var (label, pos, suggestion) in invalidContainers)
            {
                var suggestionText = !string.IsNullOrEmpty(suggestion) ? $" (did you mean '{suggestion}'?)" : "";
                MagicSorterMod.Output($"  [ms:{label}] at {pos}{suggestionText}");
            }

            MagicSorterMod.Output("[MagicSorter] These containers won't receive any items during sorting.");
            MagicSorterMod.Output("[MagicSorter] Use 'ms mappings' to see available categories, or check for typos.");
        }

        private string FindClosestCategory(string label, MappingData mappings)
        {
            string bestMatch = null;
            var bestDistance = int.MaxValue;
            var threshold = Math.Max(2, label.Length / 3); // Allow more errors for longer labels

            // Check categories
            foreach (var category in mappings.Categories.Keys)
            {
                var distance = LevenshteinDistance(label.ToLower(), category.ToLower());
                if (distance < bestDistance && distance <= threshold)
                {
                    bestDistance = distance;
                    bestMatch = category;
                }
            }

            // Check aliases
            foreach (var alias in mappings.ContainerAliases.Keys)
            {
                var distance = LevenshteinDistance(label.ToLower(), alias.ToLower());
                if (distance < bestDistance && distance <= threshold)
                {
                    bestDistance = distance;
                    bestMatch = alias;
                }
            }

            return bestMatch;
        }

        public void Suggest()
        {
            try
            {
                SuggestInternal();
            }
            catch (Exception ex)
            {
                MagicSorterMod.Output($"[MagicSorter] Unexpected error: {ex.Message}");
            }
        }

        private void SuggestInternal()
        {
            var containers = FindContainersInRange();
            var sortMeContainer = FindSortMeContainer(containers);

            if (sortMeContainer == null)
            {
                MagicSorterMod.Output("[MagicSorter] No [MagicSort] container found in range.");
                return;
            }

            var categoryContainers = BuildCategoryMap(containers, sortMeContainer);

            var items = sortMeContainer.GetItems();
            if (items == null)
            {
                MagicSorterMod.Output("[MagicSorter] Could not access items in [MagicSort] container");
                return;
            }

            // Find items that can't be sorted and group by their categories
            var unsortableItems = new List<(string name, int count, List<string> categories)>();

            foreach (var itemStack in items)
            {
                if (itemStack.IsEmpty()) continue;

                var categories = GetItemCategories(itemStack);
                var matchingContainer = FindBestContainer(categories, categoryContainers, itemStack);

                // Also check Unknown fallback
                if (matchingContainer == null && categoryContainers.ContainsKey(UnknownCategory))
                    matchingContainer = GetFullestContainerWithSpace(categoryContainers[UnknownCategory], itemStack);

                if (matchingContainer == null)
                {
                    var itemName = GetItemName(itemStack);
                    unsortableItems.Add((itemName, itemStack.count, categories));
                }
            }

            if (unsortableItems.Count == 0)
            {
                MagicSorterMod.Output("[MagicSorter] All items can be sorted with current containers!");
                return;
            }

            MagicSorterMod.Output($"[MagicSorter] {unsortableItems.Count} item(s) cannot be sorted. Suggested containers:");

            // Group by suggested container (most specific category)
            var suggestions = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

            foreach (var (name, count, categories) in unsortableItems)
            {
                string suggestedCategory;
                if (categories.Count == 0)
                    suggestedCategory = UnknownCategory;
                else
                    // Use most specific category (last in list)
                    suggestedCategory = categories[categories.Count - 1];

                if (!suggestions.ContainsKey(suggestedCategory))
                    suggestions[suggestedCategory] = new List<string>();

                var allCats = categories.Count > 0 ? string.Join(" → ", categories) : "(none)";
                suggestions[suggestedCategory].Add($"{name} x{count} [{allCats}]");
            }

            foreach (var kvp in suggestions.OrderBy(k => k.Key))
            {
                MagicSorterMod.Output($"  Create [ms:{kvp.Key}] for:");
                foreach (var item in kvp.Value) MagicSorterMod.Output($"    - {item}");
            }
        }

        public void DebugItems()
        {
            try
            {
                DebugInternal();
            }
            catch (Exception ex)
            {
                MagicSorterMod.Output($"[MagicSorter] Unexpected error: {ex.Message}");
            }
        }

        private void DebugInternal()
        {
            var containers = FindContainersInRange();
            var sortMeContainer = FindSortMeContainer(containers);

            if (sortMeContainer == null)
            {
                MagicSorterMod.Output("[MagicSorter] No [MagicSort] container found in range.");
                return;
            }

            var items = sortMeContainer.GetItems();
            if (items == null)
            {
                MagicSorterMod.Output("[MagicSorter] Could not access items in [MagicSort] container");
                return;
            }

            var mappings = MagicSorterMod.MappingLoader?.GetMappings();

            MagicSorterMod.Output("[MagicSorter] Debug - Item details in [MagicSort]:");

            foreach (var itemStack in items)
            {
                if (itemStack.IsEmpty()) continue;

                var itemClass = itemStack.itemValue?.ItemClass;
                if (itemClass == null) continue;

                LogItemDebugInfo(itemStack, itemClass, mappings);
            }

            if (items.All(s => s.IsEmpty())) MagicSorterMod.Output("[MagicSorter] [MagicSort] is empty");
        }

        private void LogItemDebugInfo(ItemStack itemStack, ItemClass itemClass, MappingData mappings)
        {
            var internalName = itemClass.Name ?? "(null)";
            var localizedName = itemClass.GetLocalizedItemName() ?? internalName;
            var groups = itemClass.Groups ?? Array.Empty<string>();
            var groupsStr = groups.Length > 0 ? string.Join(", ", groups) : "(none)";

            // Extract additional properties via reflection
            var (customIcon, descriptionKey, extendsName, parentName, allProps) = ExtractItemClassProperties(itemClass);

            // Determine mapping source
            var inMappings = mappings != null && mappings.Items.ContainsKey(internalName);
            var fromPattern = !inMappings && CategoryResolver.HasPatternMatch(internalName);
            var mappingStatus = inMappings ? "MAPPED" : fromPattern ? "PATTERN" : "FALLBACK";

            // Get resolved categories
            var categories = GetItemCategories(itemStack);
            var categoriesStr = categories.Count > 0 ? string.Join(", ", categories) : "(none)";

            // Log all details
            MagicSorterMod.Output($"  {localizedName} x{itemStack.count}:");
            MagicSorterMod.Output($"    Internal name: {internalName}");
            if (!string.IsNullOrEmpty(extendsName))
                MagicSorterMod.Output($"    Extends: {extendsName}");
            if (!string.IsNullOrEmpty(parentName))
                MagicSorterMod.Output($"    Parent: {parentName}");
            if (!string.IsNullOrEmpty(customIcon))
                MagicSorterMod.Output($"    CustomIcon: {customIcon}");
            if (!string.IsNullOrEmpty(descriptionKey))
                MagicSorterMod.Output($"    DescriptionKey: {descriptionKey}");
            MagicSorterMod.Output($"    Game Groups: {groupsStr}");
            MagicSorterMod.Output($"    Resolved categories: {categoriesStr} [{mappingStatus}]");
            if (allProps.Count > 0)
                MagicSorterMod.Output($"    All string props: {string.Join(", ", allProps)}");
        }

        private (string customIcon, string descriptionKey, string extendsName, string parentName, List<string> allProps)
            ExtractItemClassProperties(ItemClass itemClass)
        {
            string customIcon = null;
            string descriptionKey = null;
            string extendsName = null;
            string parentName = null;
            var allProps = new List<string>();

            try
            {
                var customIconProp = itemClass.GetType().GetProperty("CustomIcon");
                if (customIconProp?.GetValue(itemClass) is string icon)
                    customIcon = icon;

                var descProp = itemClass.GetType().GetProperty("DescriptionKey");
                if (descProp?.GetValue(itemClass) is string desc)
                    descriptionKey = desc;

                // Check for Extends property (inheritance from items.xml)
                var properties = itemClass.Properties;
                if (properties != null && properties.Contains("Extends")) extendsName = properties.GetString("Extends");

                // Try to get parent/base class if exists
                var parentField = itemClass.GetType().GetField("parent",
                    BindingFlags.Public |
                    BindingFlags.NonPublic |
                    BindingFlags.Instance);
                var parent = parentField?.GetValue(itemClass);
                if (parent != null)
                {
                    var parentNameProp = parent.GetType().GetProperty("Name");
                    if (parentNameProp?.GetValue(parent) is string name)
                        parentName = name;
                }

                // List all string properties for debugging
                foreach (var prop in itemClass.GetType().GetProperties())
                    try
                    {
                        if (prop.PropertyType == typeof(string) && prop.CanRead &&
                            prop.GetValue(itemClass) is string val &&
                            !string.IsNullOrEmpty(val) && val.Length < 100)
                            allProps.Add($"{prop.Name}={val}");
                    }
                    catch
                    {
                    }
            }
            catch
            {
            }

            return (customIcon, descriptionKey, extendsName, parentName, allProps);
        }

        private int LevenshteinDistance(string s1, string s2)
        {
            // Optimize: ensure s1 is the shorter string to minimize memory usage
            if (s1.Length > s2.Length)
            {
                var temp = s1;
                s1 = s2;
                s2 = temp;
            }

            var len1 = s1.Length;
            var len2 = s2.Length;

            // Use two rows instead of full 2D array - O(min(m,n)) space instead of O(m*n)
            var prevRow = new int[len1 + 1];
            var currRow = new int[len1 + 1];

            // Initialize first row
            for (var i = 0; i <= len1; i++)
                prevRow[i] = i;

            for (var j = 1; j <= len2; j++)
            {
                currRow[0] = j;

                for (var i = 1; i <= len1; i++)
                {
                    var cost = s1[i - 1] == s2[j - 1] ? 0 : 1;
                    currRow[i] = Math.Min(
                        Math.Min(prevRow[i] + 1, currRow[i - 1] + 1),
                        prevRow[i - 1] + cost);
                }

                // Swap rows
                var swap = prevRow;
                prevRow = currRow;
                currRow = swap;
            }

            return prevRow[len1];
        }

        private void MissingInternal()
        {
            var containers = FindContainersInRange();
            var sortMeContainer = FindSortMeContainer(containers);

            if (sortMeContainer == null)
            {
                MagicSorterMod.Output("[MagicSorter] No [MagicSort] container found in range.");
                return;
            }

            var categoryContainers = BuildCategoryMap(containers, sortMeContainer);

            var items = sortMeContainer.GetItems();
            if (items == null)
            {
                MagicSorterMod.Output("[MagicSorter] Could not access items in [MagicSort] container");
                return;
            }

            // Find categories that have items but no container
            var missingCategories = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            foreach (var itemStack in items)
            {
                if (itemStack.IsEmpty()) continue;

                var categories = GetItemCategories(itemStack);

                if (categories.Count == 0)
                {
                    // No category - would need [ms:Unknown]
                    if (!categoryContainers.ContainsKey(UnknownCategory))
                    {
                        if (!missingCategories.ContainsKey(UnknownCategory))
                            missingCategories[UnknownCategory] = 0;
                        missingCategories[UnknownCategory]++;
                    }
                }
                else
                {
                    // Use the resolver to check if any container matches (respects aliases)
                    var matchingContainer = FindBestContainer(categories, categoryContainers, itemStack);

                    if (matchingContainer == null)
                    {
                        // Use the most specific category for the suggestion
                        var suggestedCategory = categories[categories.Count - 1];
                        if (!missingCategories.ContainsKey(suggestedCategory))
                            missingCategories[suggestedCategory] = 0;
                        missingCategories[suggestedCategory]++;
                    }
                }
            }

            if (missingCategories.Count == 0)
            {
                MagicSorterMod.Output("[MagicSorter] All items have matching containers!");
                return;
            }

            MagicSorterMod.Output("[MagicSorter] Missing containers for categories:");
            foreach (var kvp in missingCategories.OrderByDescending(k => k.Value))
                MagicSorterMod.Output($"  - {kvp.Key} ({kvp.Value} items)");

            MagicSorterMod.Output("[MagicSorter] Suggested containers to create:");
            foreach (var category in missingCategories.Keys.OrderBy(k => k)) MagicSorterMod.Output($"  [ms:{category}]");
        }

        private void PlanInternal()
        {
            var containers = FindContainersInRange();
            if (containers.Count == 0)
            {
                MagicSorterMod.Output("[MagicSorter] No containers found in range.");
                return;
            }

            var sortMeContainer = FindSortMeContainer(containers);
            if (sortMeContainer == null)
            {
                MagicSorterMod.Output("[MagicSorter] No [MagicSort] container found in range.");
                return;
            }

            if (IsContainerEmpty(sortMeContainer))
            {
                MagicSorterMod.Output("[MagicSorter] Nothing to sort - [MagicSort] is empty");
                return;
            }

            var categoryContainers = BuildCategoryMap(containers, sortMeContainer);
            if (categoryContainers.Count == 0)
            {
                MagicSorterMod.Output("[MagicSorter] No [ms:X] containers found in range.");
                return;
            }

            // Preview sorting
            var items = sortMeContainer.GetItems();
            if (items == null)
            {
                MagicSorterMod.Output("[MagicSorter] Could not access items in [MagicSort] container");
                return;
            }

            var previewResults = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            var noContainer = new List<string>();
            var containerFull = new List<string>();

            for (var i = 0; i < items.Length; i++)
            {
                var itemStack = items[i];
                if (itemStack.IsEmpty()) continue;

                var itemName = GetItemName(itemStack);
                var itemDesc = $"{itemName} x{itemStack.count}";
                var categories = GetItemCategories(itemStack);

                var targetContainer = FindBestContainer(categories, categoryContainers, itemStack);

                if (targetContainer == null &&
                    categoryContainers.TryGetValue(UnknownCategory, out var unknownContainers))
                    targetContainer = GetFullestContainerWithSpace(unknownContainers, itemStack);

                if (targetContainer != null)
                {
                    var targetCategory = ExtractCategory(targetContainer.Name) ?? UnknownCategory;
                    if (!previewResults.ContainsKey(targetCategory))
                        previewResults[targetCategory] = new List<string>();
                    previewResults[targetCategory].Add(itemDesc);
                }
                else
                {
                    // Determine why there's no destination: container full or no container exists
                    var matchingCategory = FindMatchingCategory(categories, categoryContainers);
                    var catStr = categories.Count > 0 ? categories[categories.Count - 1] : "unknown";

                    if (matchingCategory != null)
                        // Container exists but is full
                        containerFull.Add($"{itemDesc} → [ms:{matchingCategory}] is full");
                    else
                        // No container for this category
                        noContainer.Add($"{itemDesc} (category: {catStr})");
                }
            }

            // Output preview
            MagicSorterMod.Output("[MagicSorter] Preview - items would be sorted as follows:");
            foreach (var kvp in previewResults.OrderBy(k => k.Key))
            {
                MagicSorterMod.Output($"  [ms:{kvp.Key}]:");
                foreach (var item in kvp.Value) MagicSorterMod.Output($"    - {item}");
            }

            if (containerFull.Count > 0)
            {
                MagicSorterMod.Output("  Container full:");
                foreach (var item in containerFull) MagicSorterMod.Output($"    - {item}");
            }

            if (noContainer.Count > 0)
            {
                MagicSorterMod.Output("  No container:");
                foreach (var item in noContainer) MagicSorterMod.Output($"    - {item}");
            }

            var totalItems = previewResults.Values.Sum(v => v.Count);
            var remainCount = containerFull.Count + noContainer.Count;
            MagicSorterMod.Output($"[MagicSorter] Summary: {totalItems} items would be sorted, {remainCount} would remain");
        }

        private void SortInternal()
        {
            // Find all containers in range
            var containers = FindContainersInRange();
            if (containers.Count == 0)
            {
                MagicSorterMod.Output("[MagicSorter] No containers found in range.");
                return;
            }

            // Find the SortMe container (closest to player)
            var sortMeContainer = FindSortMeContainer(containers);
            if (sortMeContainer == null)
            {
                MagicSorterMod.Output("[MagicSorter] No [MagicSort] container found in range.");
                return;
            }

            // Check if empty
            if (IsContainerEmpty(sortMeContainer))
            {
                MagicSorterMod.Output("[MagicSorter] Nothing to sort - [MagicSort] is empty");
                return;
            }

            // Build map of category -> containers
            var categoryContainers = BuildCategoryMap(containers, sortMeContainer);
            if (categoryContainers.Count == 0)
            {
                MagicSorterMod.Output("[MagicSorter] No [ms:X] containers found in range.");
                return;
            }

            // Phase 1: Determine which containers will be affected by the sort
            // We need ALL containers that match item categories, not just ones with space,
            // so we can consolidate items of the same type together
            var sortMeItems = sortMeContainer.GetItems();
            var affectedContainers = new HashSet<ContainerWrapper>();

            foreach (var itemStack in sortMeItems)
            {
                if (itemStack.IsEmpty()) continue;

                var categories = GetItemCategories(itemStack);

                // Add ALL containers that match any of the item's categories
                foreach (var category in categories)
                {
                    if (categoryContainers.TryGetValue(category, out var matchingContainers))
                    {
                        foreach (var c in matchingContainers)
                            affectedContainers.Add(c);
                    }
                }

                // Also include Unknown containers if item has no matching category
                if (categories.Count == 0 && categoryContainers.TryGetValue(UnknownCategory, out var unknownContainers))
                {
                    foreach (var c in unknownContainers)
                        affectedContainers.Add(c);
                }
            }

            if (affectedContainers.Count == 0)
            {
                MagicSorterMod.Output("[MagicSorter] No destination containers found for items.");
                return;
            }

            // Phase 2: Collect all items from MagicSort and affected containers
            var collectedItems = new List<ItemStack>();

            // Collect from MagicSort
            foreach (var itemStack in sortMeItems)
            {
                if (itemStack.IsEmpty()) continue;
                collectedItems.Add(itemStack.Clone());
            }

            // Clear MagicSort
            for (var i = 0; i < sortMeItems.Length; i++)
                sortMeItems[i] = ItemStack.Empty.Clone();
            sortMeContainer.SetModified();

            // Collect from affected destination containers
            foreach (var container in affectedContainers)
            {
                var items = container.GetItems();
                if (items == null) continue;

                for (var i = 0; i < items.Length; i++)
                {
                    if (items[i].IsEmpty()) continue;
                    collectedItems.Add(items[i].Clone());
                    items[i] = ItemStack.Empty.Clone();
                }
                container.SetModified();
            }

            MagicSorterMod.Output($"[MagicSorter] Consolidating {collectedItems.Count} item stacks across {affectedContainers.Count} container(s)...");

            // Phase 3: Re-sort all collected items, grouped by type
            var unsortedItems = PlaceItemsIntoContainers(collectedItems, categoryContainers);

            // Phase 4: Put unsorted items back into MagicSort (or any container with space)
            if (unsortedItems.Count > 0)
            {
                var trulyLostItems = new List<string>();
                foreach (var itemStack in unsortedItems)
                {
                    var itemName = GetItemName(itemStack);
                    var itemDesc = $"{itemName} x{itemStack.count}";

                    // Try MagicSort first
                    if (TryPlaceItem(sortMeContainer, itemStack, out _))
                        continue;

                    // MagicSort full - try any affected container
                    var placed = false;
                    foreach (var container in affectedContainers)
                    {
                        if (TryPlaceItem(container, itemStack, out _))
                        {
                            placed = true;
                            break;
                        }
                    }

                    if (!placed)
                        trulyLostItems.Add(itemDesc);
                }
                sortMeContainer.SetModified();

                if (trulyLostItems.Count > 0)
                {
                    MagicSorterMod.Output($"[MagicSorter] WARNING: {trulyLostItems.Count} item(s) could not be placed anywhere!");
                    foreach (var item in trulyLostItems)
                        MagicSorterMod.Output($"  LOST: {item}");
                }
            }

            // Log summary
            LogSummary();
        }

        private void ResortInternal()
        {
            // Find all containers in range
            var containers = FindContainersInRange();
            if (containers.Count == 0)
            {
                MagicSorterMod.Output("[MagicSorter] No containers found in range.");
                return;
            }

            // Build map of category -> containers (exclude SortMe from the map)
            var sortMeContainer = FindSortMeContainer(containers);
            var categoryContainers = BuildCategoryMap(containers, sortMeContainer);
            if (categoryContainers.Count == 0)
            {
                MagicSorterMod.Output("[MagicSorter] No [ms:X] containers found in range.");
                return;
            }

            // Phase 1: Collect all items from all [ms:X] containers into memory
            var collectedItems = new List<ItemStack>();
            var containersWithItems = new List<ContainerWrapper>();

            foreach (var kvp in categoryContainers)
            {
                foreach (var container in kvp.Value)
                {
                    var items = container.GetItems();
                    if (items == null) continue;

                    for (var i = 0; i < items.Length; i++)
                    {
                        if (items[i].IsEmpty()) continue;

                        // Clone the item to memory
                        collectedItems.Add(items[i].Clone());

                        // Clear the slot
                        items[i] = ItemStack.Empty.Clone();
                    }

                    if (!containersWithItems.Contains(container))
                        containersWithItems.Add(container);
                }
            }

            // Mark all modified containers
            foreach (var container in containersWithItems)
                container.SetModified();

            if (collectedItems.Count == 0)
            {
                MagicSorterMod.Output("[MagicSorter] No items to resort - all containers are empty");
                return;
            }

            MagicSorterMod.Output($"[MagicSorter] Collected {collectedItems.Count} item stacks for resorting...");

            // Phase 2: Re-sort all collected items, grouped by item type
            var unsortedItems = PlaceItemsIntoContainers(collectedItems, categoryContainers);

            // Phase 3: Put unsorted items into MagicSort or any container with space
            if (unsortedItems.Count > 0)
            {
                var trulyLostItems = new List<string>();
                foreach (var itemStack in unsortedItems)
                {
                    var itemName = GetItemName(itemStack);
                    var itemDesc = $"{itemName} x{itemStack.count}";
                    var placed = false;

                    // Try MagicSort first (if available)
                    if (sortMeContainer != null && TryPlaceItem(sortMeContainer, itemStack, out _))
                    {
                        placed = true;
                    }
                    else
                    {
                        // Try any category container
                        foreach (var kvp in categoryContainers)
                        {
                            foreach (var container in kvp.Value)
                            {
                                if (TryPlaceItem(container, itemStack, out _))
                                {
                                    placed = true;
                                    break;
                                }
                            }
                            if (placed) break;
                        }
                    }

                    if (!placed)
                        trulyLostItems.Add(itemDesc);
                }

                if (sortMeContainer != null)
                    sortMeContainer.SetModified();

                if (trulyLostItems.Count > 0)
                {
                    MagicSorterMod.Output($"[MagicSorter] WARNING: {trulyLostItems.Count} item(s) could not be placed anywhere!");
                    foreach (var item in trulyLostItems)
                        MagicSorterMod.Output($"  LOST: {item}");
                }
            }

            // Log summary
            LogResortSummary();
        }

        /// <summary>
        ///     Places an item into a container (used by resort when items are already in memory)
        /// </summary>
        /// <summary>
        ///     Sorts items into category containers, tracking results in _sortedItems/_failedItems.
        ///     Items are grouped by type before placement so same-type items stay together.
        ///     Returns any items that could not be placed.
        /// </summary>
        private List<ItemStack> PlaceItemsIntoContainers(
            List<ItemStack> items,
            Dictionary<string, List<ContainerWrapper>> categoryContainers)
        {
            var sortedByType = items.OrderBy(item => item.itemValue.type).ToList();
            var unsortedItems = new List<ItemStack>();

            foreach (var itemStack in sortedByType)
            {
                var itemName = GetItemName(itemStack);
                var itemDesc = $"{itemName} x{itemStack.count}";
                var categories = GetItemCategories(itemStack);

                var targetContainer = FindBestContainer(categories, categoryContainers, itemStack);

                if (targetContainer == null &&
                    categoryContainers.TryGetValue(UnknownCategory, out var unknownContainers))
                    targetContainer = GetFullestContainerWithSpace(unknownContainers, itemStack);

                if (targetContainer == null)
                {
                    unsortedItems.Add(itemStack);
                    var matchingCategory = FindMatchingCategory(categories, categoryContainers);
                    if (matchingCategory != null)
                        _failedItems.Add($"{itemDesc} → [ms:{matchingCategory}] is full");
                    else if (categories.Count == 0)
                        _failedItems.Add($"{itemDesc} → no category, no [ms:Unknown]");
                    else
                        _failedItems.Add(
                            $"{itemDesc} → no container for [{string.Join(", ", categories)}]");
                    continue;
                }

                if (TryPlaceItem(targetContainer, itemStack, out var targetCategory))
                {
                    if (!_sortedItems.ContainsKey(targetCategory))
                        _sortedItems[targetCategory] = new List<string>();
                    _sortedItems[targetCategory].Add(itemDesc);
                }
                else
                {
                    unsortedItems.Add(itemStack);
                    _failedItems.Add($"{itemDesc} → [ms:{targetCategory}] no space");
                }
            }

            return unsortedItems;
        }

        private bool TryPlaceItem(ContainerWrapper target, ItemStack itemStack, out string targetCategory)
        {
            targetCategory = ExtractCategory(target.Name) ?? UnknownCategory;

            var targetItems = target.GetItems();
            if (targetItems == null)
                return false;

            // Try to stack with existing items first
            for (var i = 0; i < targetItems.Length; i++)
            {
                if (!targetItems[i].IsEmpty() &&
                    targetItems[i].itemValue.type == itemStack.itemValue.type)
                {
                    var maxStack = itemStack.itemValue.ItemClass.Stacknumber.Value;
                    var canAdd = maxStack - targetItems[i].count;

                    if (canAdd > 0)
                    {
                        var toMove = Math.Min(canAdd, itemStack.count);
                        targetItems[i].count += toMove;
                        itemStack.count -= toMove;

                        if (itemStack.count <= 0)
                        {
                            target.SetModified();
                            return true;
                        }
                    }
                }
            }

            // Try to put in empty slot
            for (var i = 0; i < targetItems.Length; i++)
            {
                if (targetItems[i].IsEmpty())
                {
                    targetItems[i] = itemStack.Clone();
                    target.SetModified();
                    return true;
                }
            }

            // Partial success - we stacked some but not all
            if (itemStack.count < itemStack.itemValue.ItemClass.Stacknumber.Value)
            {
                target.SetModified();
                return true;
            }

            return false;
        }

        private void LogResortSummary()
        {
            var totalSorted = _sortedItems.Values.Sum(list => list.Count);

            if (totalSorted == 0 && _failedItems.Count == 0)
            {
                MagicSorterMod.Output("[MagicSorter] Nothing was resorted");
                return;
            }

            if (totalSorted > 0)
            {
                MagicSorterMod.Output("[MagicSorter] Resort complete - items redistributed as follows:");
                foreach (var kvp in _sortedItems.OrderBy(k => k.Key))
                {
                    MagicSorterMod.Output($"  [ms:{kvp.Key}]:");
                    foreach (var item in kvp.Value)
                        MagicSorterMod.Output($"    - {item}");
                }
            }

            if (_failedItems.Count > 0)
            {
                MagicSorterMod.Output("  Failed to place:");
                foreach (var item in _failedItems)
                    MagicSorterMod.Output($"    - {item}");
            }

            MagicSorterMod.Output($"[MagicSorter] Resort summary: {totalSorted} items placed, {_failedItems.Count} failed");
        }

        public List<EntityVehicle> FindVehiclesInRange()
        {
            var playerPos = _player.position;
            var center = playerPos - Origin.position;
            var size = new Vector3(_range * 2, _range * 2, _range * 2);
            var bounds = new Bounds(center, size);
            var entities = new List<Entity>();
            _world.GetEntitiesInBounds(typeof(EntityVehicle), bounds, entities);

            var vehicles = new List<EntityVehicle>();
            foreach (var entity in entities)
            {
                var vehicle = entity as EntityVehicle;
                if (vehicle == null) continue;
                if (vehicle.GetVehicle() == null || !vehicle.hasStorage()) continue;
                vehicles.Add(vehicle);
            }

            // Sort by distance from player
            vehicles.Sort((a, b) =>
            {
                var distA = Vector3.Distance(playerPos, a.position);
                var distB = Vector3.Distance(playerPos, b.position);
                return distA.CompareTo(distB);
            });

            return vehicles;
        }

        public EntityVehicle FindVehicleById(int entityId)
        {
            var vehicles = FindVehiclesInRange();
            foreach (var v in vehicles)
            {
                if (v.entityId == entityId)
                    return v;
            }
            return null;
        }

        public void ListVehicles()
        {
            try
            {
                var vehicles = FindVehiclesInRange();
                if (vehicles.Count == 0)
                {
                    MagicSorterMod.Output("[MagicSorter] No vehicles with storage found in range.");
                    return;
                }

                MagicSorterMod.Output($"[MagicSorter] Found {vehicles.Count} vehicle(s) with storage:");
                foreach (var v in vehicles)
                {
                    var className = GetVehicleClassName(v);
                    var slots = v.bag.GetSlots();
                    var used = slots.Count(s => !s.IsEmpty());
                    var total = slots.Length;
                    var pos = v.GetBlockPosition();
                    MagicSorterMod.Output($"  {className} (ID:{v.entityId}) at {pos} - {used}/{total} slots");
                }
            }
            catch (Exception ex)
            {
                MagicSorterMod.Output($"[MagicSorter] Unexpected error: {ex.Message}");
            }
        }

        public void SortVehicle(EntityVehicle vehicle)
        {
            try
            {
                SortVehicleInternal(vehicle);
            }
            catch (Exception ex)
            {
                MagicSorterMod.Output($"[MagicSorter] Unexpected error: {ex.Message}");
            }
        }

        private void SortVehicleInternal(EntityVehicle vehicle)
        {
            if (vehicle == null)
            {
                MagicSorterMod.Output("[MagicSorter] Vehicle not found.");
                return;
            }

            // Find [MagicSort] near the player
            var nearbyContainers = FindContainersInRange();
            var sortMeContainer = FindSortMeContainer(nearbyContainers);
            if (sortMeContainer == null)
            {
                MagicSorterMod.Output("[MagicSorter] No [MagicSort] container found in range.");
                return;
            }

            // Find [ms:X] destination containers around the [MagicSort] box
            var containersAroundSortBox = FindContainersAroundPosition(sortMeContainer.Position);
            var categoryContainers = BuildCategoryMap(containersAroundSortBox, sortMeContainer);
            if (categoryContainers.Count == 0)
            {
                MagicSorterMod.Output("[MagicSorter] No [ms:X] containers found near [MagicSort].");
                return;
            }

            // Collect items from vehicle
            var vehicleSlots = vehicle.bag.GetSlots();
            var collectedItems = new List<ItemStack>();

            for (var i = 0; i < vehicleSlots.Length; i++)
            {
                if (vehicleSlots[i].IsEmpty()) continue;
                collectedItems.Add(vehicleSlots[i].Clone());
                vehicleSlots[i] = ItemStack.Empty.Clone();
            }

            if (collectedItems.Count == 0)
            {
                MagicSorterMod.Output("[MagicSorter] Vehicle storage is empty.");
                return;
            }

            vehicle.SetBagModified();

            var className = GetVehicleClassName(vehicle);
            MagicSorterMod.Output($"[MagicSorter] Sorting {collectedItems.Count} item stacks from {className}...");

            // Sort items into containers
            var unsortedItems = PlaceItemsIntoContainers(collectedItems, categoryContainers);

            // Put unsorted items back: try [MagicSort] first, then back into vehicle
            if (unsortedItems.Count > 0)
            {
                var trulyLostItems = new List<string>();
                foreach (var itemStack in unsortedItems)
                {
                    var itemName = GetItemName(itemStack);
                    var itemDesc = $"{itemName} x{itemStack.count}";
                    var placed = false;

                    // Try MagicSort first
                    if (sortMeContainer != null && TryPlaceItem(sortMeContainer, itemStack, out _))
                    {
                        placed = true;
                    }

                    // Fall back to vehicle bag
                    if (!placed)
                    {
                        for (var i = 0; i < vehicleSlots.Length; i++)
                        {
                            if (vehicleSlots[i].IsEmpty())
                            {
                                vehicleSlots[i] = itemStack.Clone();
                                placed = true;
                                break;
                            }
                        }
                    }

                    if (!placed)
                        trulyLostItems.Add(itemDesc);
                }

                if (sortMeContainer != null)
                    sortMeContainer.SetModified();
                vehicle.SetBagModified();

                if (trulyLostItems.Count > 0)
                {
                    MagicSorterMod.Output(
                        $"[MagicSorter] WARNING: {trulyLostItems.Count} item(s) could not be placed anywhere!");
                    foreach (var item in trulyLostItems)
                        MagicSorterMod.Output($"  LOST: {item}");
                }
            }

            // Log summary
            LogVehicleSortSummary();
        }

        private void LogVehicleSortSummary()
        {
            var totalSorted = _sortedItems.Values.Sum(list => list.Count);

            if (totalSorted == 0 && _failedItems.Count == 0)
            {
                MagicSorterMod.Output("[MagicSorter] Nothing was sorted from vehicle");
                return;
            }

            if (totalSorted > 0)
            {
                MagicSorterMod.Output("[MagicSorter] Vehicle sort complete:");
                foreach (var kvp in _sortedItems.OrderBy(k => k.Key))
                {
                    MagicSorterMod.Output($"  [ms:{kvp.Key}]:");
                    foreach (var item in kvp.Value)
                        MagicSorterMod.Output($"    - {item}");
                }
            }

            if (_failedItems.Count > 0)
            {
                MagicSorterMod.Output("  Returned to vehicle/[MagicSort]:");
                foreach (var item in _failedItems)
                    MagicSorterMod.Output($"    - {item}");
            }

            MagicSorterMod.Output(
                $"[MagicSorter] Summary: {totalSorted} items sorted, {_failedItems.Count} returned");
        }

        private List<ContainerWrapper> FindContainersInRange()
        {
            return FindContainersAroundPosition(_player.GetBlockPosition());
        }

        private List<ContainerWrapper> FindContainersAroundPosition(Vector3i center)
        {
            var result = new List<ContainerWrapper>();

            for (var x = -_range; x <= _range; x++)
            for (var y = -_range; y <= _range; y++)
            for (var z = -_range; z <= _range; z++)
            {
                var pos = new Vector3i(center.x + x, center.y + y, center.z + z);
                var tileEntity = _world.GetTileEntity(0, pos);

                if (tileEntity is TileEntityLootContainer lootContainer)
                {
                    var name = GetLootContainerName(lootContainer);
                    result.Add(new ContainerWrapper(lootContainer, name, pos));
                }
                else if (tileEntity is TileEntityComposite composite)
                {
                    var name = SignTextHelper.GetCompositeSignText(composite);
                    if (!string.IsNullOrEmpty(name)) result.Add(new ContainerWrapper(composite, name, pos));
                }
            }

            return result;
        }

        private ContainerWrapper FindSortMeContainer(List<ContainerWrapper> containers)
        {
            var playerPos = _player.GetBlockPosition();
            ContainerWrapper closest = null;
            var closestDist = float.MaxValue;

            foreach (var container in containers)
                if (container.Name != null &&
                    container.Name.IndexOf(SortMeTag, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    var dist = Vector3.Distance(
                        new Vector3(playerPos.x, playerPos.y, playerPos.z),
                        new Vector3(container.Position.x, container.Position.y, container.Position.z));

                    if (dist < closestDist)
                    {
                        closestDist = dist;
                        closest = container;
                    }
                }

            return closest;
        }

        private Dictionary<string, List<ContainerWrapper>> BuildCategoryMap(
            List<ContainerWrapper> containers,
            ContainerWrapper exclude)
        {
            var result = new Dictionary<string, List<ContainerWrapper>>(StringComparer.OrdinalIgnoreCase);

            foreach (var container in containers)
            {
                if (container == exclude) continue;

                var name = container.Name;
                if (name == null) continue;

                var category = ExtractCategory(name);
                if (category == null) continue;

                if (!result.ContainsKey(category)) result[category] = new List<ContainerWrapper>();
                result[category].Add(container);
            }

            return result;
        }

        private string ExtractCategory(string containerName)
        {
            var startIdx = containerName.IndexOf(SortPrefix, StringComparison.OrdinalIgnoreCase);
            if (startIdx < 0) return null;

            startIdx += SortPrefix.Length;
            var endIdx = containerName.IndexOf(']', startIdx);
            if (endIdx < 0) return null;

            return containerName.Substring(startIdx, endIdx - startIdx).Trim().Replace(" ", "");
        }

        private bool IsContainerEmpty(ContainerWrapper container)
        {
            var items = container.GetItems();
            return items == null || items.All(slot => slot.IsEmpty());
        }

        private List<string> GetItemCategories(ItemStack itemStack)
        {
            // Use the resolver if available, otherwise fall back to built-in Groups
            if (_resolver != null) return _resolver.GetItemCategories(itemStack);

            // Fallback: use built-in Groups directly
            var result = new List<string>();

            if (itemStack.itemValue?.ItemClass == null) return result;

            var groups = itemStack.itemValue.ItemClass.Groups;
            if (groups == null || groups.Length == 0) return result;

            foreach (var group in groups)
                if (!string.IsNullOrEmpty(group))
                    result.Add(group);

            return result;
        }

        private ContainerWrapper FindBestContainer(List<string> itemCategories,
            Dictionary<string, List<ContainerWrapper>> categoryContainers,
            ItemStack itemStack = null)
        {
            // Use the resolver if available for specificity-based matching
            if (_resolver != null) return _resolver.FindBestContainer(itemCategories, categoryContainers, itemStack);

            // Fallback: original behavior - try categories in reverse order (most specific first)
            for (var i = itemCategories.Count - 1; i >= 0; i--)
            {
                var category = itemCategories[i];

                // Try exact match first
                if (categoryContainers.TryGetValue(category, out var containers))
                {
                    var container = GetFullestContainerWithSpace(containers, null);
                    if (container != null) return container;
                }

                // Try partial match (container category is substring of item category or vice versa)
                foreach (var kvp in categoryContainers)
                {
                    if (kvp.Key.Equals(UnknownCategory, StringComparison.OrdinalIgnoreCase)) continue;

                    if (category.IndexOf(kvp.Key, StringComparison.OrdinalIgnoreCase) >= 0 ||
                        kvp.Key.IndexOf(category, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        var container = GetFullestContainerWithSpace(kvp.Value, null);
                        if (container != null) return container;
                    }
                }
            }

            return null;
        }

        private ContainerWrapper GetFullestContainerWithSpace(List<ContainerWrapper> containers, ItemStack itemToFit)
        {
            // Sort by fullness descending (fullest first), using ContainerWrapper's methods
            return containers
                .Where(c => c.HasSpaceFor(itemToFit))
                .OrderByDescending(c => c.GetFullness())
                .FirstOrDefault();
        }

        /// <summary>
        ///     Finds a matching category for the item, ignoring space constraints.
        ///     Returns the category name if a container exists (even if full), null otherwise.
        /// </summary>
        private string FindMatchingCategory(List<string> itemCategories,
            Dictionary<string, List<ContainerWrapper>> categoryContainers)
        {
            // Check item categories in reverse order (most specific first)
            for (var i = itemCategories.Count - 1; i >= 0; i--)
            {
                var category = itemCategories[i];

                // Try exact match
                if (categoryContainers.ContainsKey(category))
                    return category;

                // Try partial match
                foreach (var kvp in categoryContainers)
                {
                    if (kvp.Key.Equals(UnknownCategory, StringComparison.OrdinalIgnoreCase)) continue;

                    if (category.IndexOf(kvp.Key, StringComparison.OrdinalIgnoreCase) >= 0 ||
                        kvp.Key.IndexOf(category, StringComparison.OrdinalIgnoreCase) >= 0)
                        return kvp.Key;
                }
            }

            // Check Unknown fallback
            if (categoryContainers.ContainsKey(UnknownCategory))
                return UnknownCategory;

            return null;
        }

        private bool TryMoveItem(ContainerWrapper source, int sourceSlot,
            ContainerWrapper target, ItemStack itemStack, out string targetCategory)
        {
            targetCategory = ExtractCategory(target.Name) ?? UnknownCategory;

            var targetItems = target.GetItems();
            var sourceItems = source.GetItems();

            // Try to stack with existing items first
            for (var i = 0; i < targetItems.Length; i++)
                if (!targetItems[i].IsEmpty() &&
                    targetItems[i].itemValue.type == itemStack.itemValue.type)
                {
                    var maxStack = itemStack.itemValue.ItemClass.Stacknumber.Value;
                    var canAdd = maxStack - targetItems[i].count;

                    if (canAdd > 0)
                    {
                        var toMove = Math.Min(canAdd, itemStack.count);
                        targetItems[i].count += toMove;
                        itemStack.count -= toMove;

                        if (itemStack.count <= 0)
                        {
                            sourceItems[sourceSlot] = ItemStack.Empty.Clone();
                            source.SetModified();
                            target.SetModified();
                            return true;
                        }
                    }
                }

            // Try to put in empty slot
            for (var i = 0; i < targetItems.Length; i++)
                if (targetItems[i].IsEmpty())
                {
                    targetItems[i] = itemStack.Clone();
                    sourceItems[sourceSlot] = ItemStack.Empty.Clone();
                    source.SetModified();
                    target.SetModified();
                    return true;
                }

            // If we moved some but not all, still mark as modified
            if (itemStack.count < sourceItems[sourceSlot].count)
            {
                sourceItems[sourceSlot].count = itemStack.count;
                source.SetModified();
                target.SetModified();
                return true;
            }

            return false;
        }

        private string GetLootContainerName(TileEntityLootContainer container)
        {
            // For signed secure loot containers
            if (container is TileEntitySecureLootContainerSigned signedContainer)
            {
                var authoredText = signedContainer.signText;
                if (authoredText != null && !string.IsNullOrEmpty(authoredText.Text)) return authoredText.Text;
            }

            // Fallback to lootListName (block type name)
            if (!string.IsNullOrEmpty(container.lootListName)) return container.lootListName;

            return null;
        }

        private string GetItemName(ItemStack itemStack)
        {
            if (itemStack?.itemValue?.ItemClass == null) return UnknownItemName;
            return itemStack.itemValue.ItemClass.GetLocalizedItemName() ??
                   itemStack.itemValue.ItemClass.Name ?? UnknownItemName;
        }

        private void LogSummary()
        {
            var totalSorted = _sortedItems.Values.Sum(list => list.Count);

            if (totalSorted == 0 && _failedItems.Count == 0)
            {
                MagicSorterMod.Output("[MagicSorter] Nothing to sort - [MagicSort] is empty");
                return;
            }

            if (totalSorted > 0)
            {
                MagicSorterMod.Output("[MagicSorter] Sort complete - items sorted as follows:");
                foreach (var kvp in _sortedItems.OrderBy(k => k.Key))
                {
                    MagicSorterMod.Output($"  [ms:{kvp.Key}]:");
                    foreach (var item in kvp.Value)
                        MagicSorterMod.Output($"    - {item}");
                }
            }

            if (_failedItems.Count > 0)
            {
                MagicSorterMod.Output("  Failed to sort:");
                foreach (var item in _failedItems)
                    MagicSorterMod.Output($"    - {item}");
            }

            MagicSorterMod.Output($"[MagicSorter] Summary: {totalSorted} items sorted, {_failedItems.Count} failed");
        }

        private static string GetVehicleClassName(EntityVehicle vehicle)
        {
            if (EntityClass.list.ContainsKey(vehicle.entityClass))
                return EntityClass.list[vehicle.entityClass].entityClassName;
            return "Unknown";
        }
    }
}