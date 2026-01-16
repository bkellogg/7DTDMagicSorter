using System;
using System.Collections.Generic;
using System.Linq;
using MagicSorter.Services;
using UnityEngine;

namespace MagicSorter
{
    public class ContainerManager
    {
        private const string SortMeTag = "[SortMe]";
        private const string SortPrefix = "[Sort:";
        private const string UnknownCategory = "Unknown";

        private readonly EntityPlayer _player;
        private readonly int _range;
        private readonly World _world;
        private readonly CategoryResolver _resolver;

        private readonly Dictionary<string, int> _sortedCounts = new Dictionary<string, int>();
        private int _failedCount;

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
                Log.Error($"[MagicSorter] Unexpected error: {ex.Message}");
            }
        }

        public void ListContainers()
        {
            try
            {
                var containers = FindContainersInRange();
                if (containers.Count == 0)
                {
                    Log.Out("[MagicSorter] No containers found in range.");
                    return;
                }

                var sortMe = FindSortMeContainer(containers);
                var categoryMap = BuildCategoryMap(containers, sortMe);

                Log.Out($"[MagicSorter] Found {containers.Count} containers in range:");

                // List SortMe container
                if (sortMe != null)
                {
                    var items = sortMe.GetItems();
                    int itemCount = items?.Count(s => !s.IsEmpty()) ?? 0;
                    Log.Out($"  [SortMe] at {sortMe.Position} - {itemCount} items");
                }
                else
                {
                    Log.Out("  [SortMe] - NOT FOUND");
                }

                // List Sort containers by category
                foreach (var kvp in categoryMap.OrderBy(k => k.Key))
                {
                    foreach (var container in kvp.Value)
                    {
                        var items = container.GetItems();
                        int used = items?.Count(s => !s.IsEmpty()) ?? 0;
                        int total = items?.Length ?? 0;
                        Log.Out($"  [Sort:{kvp.Key}] at {container.Position} - {used}/{total} slots");
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[MagicSorter] Unexpected error: {ex.Message}");
            }
        }

        public void Preview()
        {
            try
            {
                PreviewInternal();
            }
            catch (Exception ex)
            {
                Log.Error($"[MagicSorter] Unexpected error: {ex.Message}");
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
                Log.Error($"[MagicSorter] Unexpected error: {ex.Message}");
            }
        }

        private void ScanInternal()
        {
            var containers = FindContainersInRange();
            var sortMeContainer = FindSortMeContainer(containers);

            if (sortMeContainer == null)
            {
                Log.Error("[MagicSorter] No [SortMe] container found in range.");
                return;
            }

            var items = sortMeContainer.GetItems();
            if (items == null)
            {
                Log.Error("[MagicSorter] Could not access items in [SortMe] container");
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
                Log.Out("[MagicSorter] [SortMe] is empty");
                return;
            }

            Log.Out("[MagicSorter] Items in [SortMe] by category:");
            foreach (var kvp in itemsByCategory.OrderBy(k => k.Key))
            {
                Log.Out($"  {kvp.Key}:");
                foreach (var item in kvp.Value)
                {
                    Log.Out($"    - {item}");
                }
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
                Log.Error($"[MagicSorter] Unexpected error: {ex.Message}");
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
                Log.Error($"[MagicSorter] Unexpected error: {ex.Message}");
            }
        }

        private void InvalidInternal()
        {
            var containers = FindContainersInRange();
            if (containers.Count == 0)
            {
                Log.Out("[MagicSorter] No containers found in range.");
                return;
            }

            var sortMe = FindSortMeContainer(containers);
            var categoryMap = BuildCategoryMap(containers, sortMe);

            if (categoryMap.Count == 0)
            {
                Log.Out("[MagicSorter] No [Sort:X] containers found in range.");
                return;
            }

            var mappings = MagicSorterMod.MappingLoader?.GetMappings();
            var invalidContainers = new List<(string label, Vector3i pos, string suggestion)>();

            foreach (var kvp in categoryMap)
            {
                var label = kvp.Key;
                bool isValid = false;
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
                    if (resolved != label && mappings.Categories.ContainsKey(resolved))
                    {
                        isValid = true;
                    }
                }

                // If still not valid, check for close matches (typos)
                if (!isValid && mappings != null)
                {
                    suggestion = FindClosestCategory(label, mappings);
                }

                // "Unknown" is always valid as a fallback
                if (label.Equals(UnknownCategory, StringComparison.OrdinalIgnoreCase))
                {
                    isValid = true;
                }

                if (!isValid)
                {
                    foreach (var container in kvp.Value)
                    {
                        invalidContainers.Add((label, container.Position, suggestion));
                    }
                }
            }

            if (invalidContainers.Count == 0)
            {
                Log.Out("[MagicSorter] All container labels are valid!");
                return;
            }

            Log.Out($"[MagicSorter] Found {invalidContainers.Count} container(s) with invalid/unknown labels:");
            foreach (var (label, pos, suggestion) in invalidContainers)
            {
                var suggestionText = !string.IsNullOrEmpty(suggestion) ? $" (did you mean '{suggestion}'?)" : "";
                Log.Out($"  [Sort:{label}] at {pos}{suggestionText}");
            }

            Log.Out("[MagicSorter] These containers won't receive any items during sorting.");
            Log.Out("[MagicSorter] Use 'ms mappings' to see available categories, or check for typos.");
        }

        private string FindClosestCategory(string label, Models.MappingData mappings)
        {
            string bestMatch = null;
            int bestDistance = int.MaxValue;
            int threshold = Math.Max(2, label.Length / 3); // Allow more errors for longer labels

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
                Log.Error($"[MagicSorter] Unexpected error: {ex.Message}");
            }
        }

        private void SuggestInternal()
        {
            var containers = FindContainersInRange();
            var sortMeContainer = FindSortMeContainer(containers);

            if (sortMeContainer == null)
            {
                Log.Error("[MagicSorter] No [SortMe] container found in range.");
                return;
            }

            var categoryContainers = BuildCategoryMap(containers, sortMeContainer);

            var items = sortMeContainer.GetItems();
            if (items == null)
            {
                Log.Error("[MagicSorter] Could not access items in [SortMe] container");
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
                {
                    matchingContainer = GetFullestContainerWithSpace(categoryContainers[UnknownCategory], itemStack);
                }

                if (matchingContainer == null)
                {
                    var itemName = GetItemName(itemStack);
                    unsortableItems.Add((itemName, itemStack.count, categories));
                }
            }

            if (unsortableItems.Count == 0)
            {
                Log.Out("[MagicSorter] All items can be sorted with current containers!");
                return;
            }

            Log.Out($"[MagicSorter] {unsortableItems.Count} item(s) cannot be sorted. Suggested containers:");

            // Group by suggested container (most specific category)
            var suggestions = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

            foreach (var (name, count, categories) in unsortableItems)
            {
                string suggestedCategory;
                if (categories.Count == 0)
                {
                    suggestedCategory = "Unknown";
                }
                else
                {
                    // Use most specific category (last in list)
                    suggestedCategory = categories[categories.Count - 1];
                }

                if (!suggestions.ContainsKey(suggestedCategory))
                    suggestions[suggestedCategory] = new List<string>();

                var allCats = categories.Count > 0 ? string.Join(" → ", categories) : "(none)";
                suggestions[suggestedCategory].Add($"{name} x{count} [{allCats}]");
            }

            foreach (var kvp in suggestions.OrderBy(k => k.Key))
            {
                Log.Out($"  Create [Sort:{kvp.Key}] for:");
                foreach (var item in kvp.Value)
                {
                    Log.Out($"    - {item}");
                }
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
                Log.Error($"[MagicSorter] Unexpected error: {ex.Message}");
            }
        }

        private void DebugInternal()
        {
            var containers = FindContainersInRange();
            var sortMeContainer = FindSortMeContainer(containers);

            if (sortMeContainer == null)
            {
                Log.Error("[MagicSorter] No [SortMe] container found in range.");
                return;
            }

            var items = sortMeContainer.GetItems();
            if (items == null)
            {
                Log.Error("[MagicSorter] Could not access items in [SortMe] container");
                return;
            }

            var mappings = MagicSorterMod.MappingLoader?.GetMappings();

            Log.Out("[MagicSorter] Debug - Item details in [SortMe]:");

            foreach (var itemStack in items)
            {
                if (itemStack.IsEmpty()) continue;

                var itemClass = itemStack.itemValue?.ItemClass;
                if (itemClass == null) continue;

                var internalName = itemClass.Name ?? "(null)";
                var localizedName = itemClass.GetLocalizedItemName() ?? internalName;
                var groups = itemClass.Groups ?? new string[0];
                var groupsStr = groups.Length > 0 ? string.Join(", ", groups) : "(none)";

                // Get additional ItemClass fields for investigation
                string customIcon = null;
                string descriptionKey = null;
                string extendsName = null;
                string parentName = null;
                var allProps = new List<string>();

                try
                {
                    var customIconProp = itemClass.GetType().GetProperty("CustomIcon");
                    if (customIconProp != null)
                        customIcon = customIconProp.GetValue(itemClass) as string;

                    var descProp = itemClass.GetType().GetProperty("DescriptionKey");
                    if (descProp != null)
                        descriptionKey = descProp.GetValue(itemClass) as string;

                    // Check for Extends property (inheritance from items.xml)
                    var properties = itemClass.Properties;
                    if (properties != null)
                    {
                        if (properties.Contains("Extends"))
                        {
                            extendsName = properties.GetString("Extends");
                        }
                    }

                    // Try to get parent/base class if exists
                    var parentField = itemClass.GetType().GetField("parent",
                        System.Reflection.BindingFlags.Public |
                        System.Reflection.BindingFlags.NonPublic |
                        System.Reflection.BindingFlags.Instance);
                    if (parentField != null)
                    {
                        var parent = parentField.GetValue(itemClass);
                        if (parent != null)
                        {
                            var parentNameProp = parent.GetType().GetProperty("Name");
                            if (parentNameProp != null)
                                parentName = parentNameProp.GetValue(parent) as string;
                        }
                    }

                    // List all string properties for debugging
                    foreach (var prop in itemClass.GetType().GetProperties())
                    {
                        try
                        {
                            if (prop.PropertyType == typeof(string) && prop.CanRead)
                            {
                                var val = prop.GetValue(itemClass) as string;
                                if (!string.IsNullOrEmpty(val) && val.Length < 100)
                                {
                                    allProps.Add($"{prop.Name}={val}");
                                }
                            }
                        }
                        catch { }
                    }
                }
                catch { }

                // Check source of categories
                bool inMappings = mappings != null && mappings.Items.ContainsKey(internalName);
                bool fromPattern = !inMappings && _resolver != null && HasPatternMatch(internalName);
                var mappingStatus = inMappings ? "MAPPED" : (fromPattern ? "PATTERN" : "FALLBACK");

                // Get resolved categories
                var categories = GetItemCategories(itemStack);
                var categoriesStr = categories.Count > 0 ? string.Join(", ", categories) : "(none)";

                Log.Out($"  {localizedName} x{itemStack.count}:");
                Log.Out($"    Internal name: {internalName}");
                if (!string.IsNullOrEmpty(extendsName))
                    Log.Out($"    Extends: {extendsName}");
                if (!string.IsNullOrEmpty(parentName))
                    Log.Out($"    Parent: {parentName}");
                if (!string.IsNullOrEmpty(customIcon))
                    Log.Out($"    CustomIcon: {customIcon}");
                if (!string.IsNullOrEmpty(descriptionKey))
                    Log.Out($"    DescriptionKey: {descriptionKey}");
                Log.Out($"    Game Groups: {groupsStr}");
                Log.Out($"    Resolved categories: {categoriesStr} [{mappingStatus}]");
                if (allProps.Count > 0)
                    Log.Out($"    All string props: {string.Join(", ", allProps)}");
            }

            if (items.All(s => s.IsEmpty()))
            {
                Log.Out("[MagicSorter] [SortMe] is empty");
            }
        }

        private bool HasPatternMatch(string itemName)
        {
            // Check if the item name would match any of our patterns
            if (string.IsNullOrEmpty(itemName))
                return false;

            // Check gun patterns
            if (itemName.StartsWith("gunHandgun", StringComparison.OrdinalIgnoreCase) ||
                itemName.StartsWith("gunShotgun", StringComparison.OrdinalIgnoreCase) ||
                itemName.StartsWith("gunRifle", StringComparison.OrdinalIgnoreCase) ||
                itemName.StartsWith("gunMG", StringComparison.OrdinalIgnoreCase) ||
                itemName.StartsWith("gunBow", StringComparison.OrdinalIgnoreCase) ||
                itemName.StartsWith("gunCrossbow", StringComparison.OrdinalIgnoreCase) ||
                itemName.StartsWith("gunExplosives", StringComparison.OrdinalIgnoreCase) ||
                itemName.StartsWith("gunRocketLauncher", StringComparison.OrdinalIgnoreCase))
                return true;

            // Melee weapons
            if (itemName.StartsWith("meleeWpnBlade", StringComparison.OrdinalIgnoreCase) ||
                itemName.StartsWith("meleeWpnClub", StringComparison.OrdinalIgnoreCase) ||
                itemName.StartsWith("meleeWpnSpear", StringComparison.OrdinalIgnoreCase) ||
                itemName.StartsWith("meleeWpnSledge", StringComparison.OrdinalIgnoreCase) ||
                itemName.StartsWith("meleeWpnKnuckles", StringComparison.OrdinalIgnoreCase))
                return true;

            // Tools
            if (itemName.StartsWith("meleeToolPick", StringComparison.OrdinalIgnoreCase) ||
                itemName.StartsWith("meleeToolAxe", StringComparison.OrdinalIgnoreCase) ||
                itemName.StartsWith("meleeToolShovel", StringComparison.OrdinalIgnoreCase) ||
                itemName.StartsWith("meleeToolRepair", StringComparison.OrdinalIgnoreCase) ||
                itemName.StartsWith("meleeToolSalvage", StringComparison.OrdinalIgnoreCase))
                return true;

            // Ammo
            if (itemName.StartsWith("ammo", StringComparison.OrdinalIgnoreCase))
                return true;

            // Throwables
            if (itemName.StartsWith("thrownDynamite", StringComparison.OrdinalIgnoreCase) ||
                itemName.StartsWith("thrownPipe", StringComparison.OrdinalIgnoreCase) ||
                itemName.StartsWith("thrownGrenade", StringComparison.OrdinalIgnoreCase) ||
                itemName.StartsWith("thrownFrag", StringComparison.OrdinalIgnoreCase))
                return true;

            // Armor
            if (itemName.StartsWith("armor", StringComparison.OrdinalIgnoreCase))
                return true;

            // Resources
            if (itemName.StartsWith("resource", StringComparison.OrdinalIgnoreCase))
                return true;

            // Food and drinks
            if (itemName.StartsWith("drink", StringComparison.OrdinalIgnoreCase) ||
                itemName.StartsWith("food", StringComparison.OrdinalIgnoreCase))
                return true;

            // Medical
            if (itemName.StartsWith("drug", StringComparison.OrdinalIgnoreCase) ||
                itemName.StartsWith("medical", StringComparison.OrdinalIgnoreCase))
                return true;

            // Mods
            if (itemName.StartsWith("mod", StringComparison.OrdinalIgnoreCase))
                return true;

            // Vehicle parts
            if (itemName.StartsWith("vehicle", StringComparison.OrdinalIgnoreCase))
                return true;

            // Books and schematics
            if (itemName.StartsWith("schematic", StringComparison.OrdinalIgnoreCase) ||
                itemName.StartsWith("book", StringComparison.OrdinalIgnoreCase) ||
                itemName.StartsWith("perkBook", StringComparison.OrdinalIgnoreCase))
                return true;

            // Seeds/planting
            if (itemName.StartsWith("planted", StringComparison.OrdinalIgnoreCase) ||
                itemName.StartsWith("seed", StringComparison.OrdinalIgnoreCase))
                return true;

            return false;
        }

        private int LevenshteinDistance(string s1, string s2)
        {
            int[,] d = new int[s1.Length + 1, s2.Length + 1];

            for (int i = 0; i <= s1.Length; i++)
                d[i, 0] = i;
            for (int j = 0; j <= s2.Length; j++)
                d[0, j] = j;

            for (int i = 1; i <= s1.Length; i++)
            {
                for (int j = 1; j <= s2.Length; j++)
                {
                    int cost = (s1[i - 1] == s2[j - 1]) ? 0 : 1;
                    d[i, j] = Math.Min(
                        Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                        d[i - 1, j - 1] + cost);
                }
            }

            return d[s1.Length, s2.Length];
        }

        private void MissingInternal()
        {
            var containers = FindContainersInRange();
            var sortMeContainer = FindSortMeContainer(containers);

            if (sortMeContainer == null)
            {
                Log.Error("[MagicSorter] No [SortMe] container found in range.");
                return;
            }

            var categoryContainers = BuildCategoryMap(containers, sortMeContainer);

            var items = sortMeContainer.GetItems();
            if (items == null)
            {
                Log.Error("[MagicSorter] Could not access items in [SortMe] container");
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
                    // No category - would need [Sort:Unknown]
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
                Log.Out("[MagicSorter] All items have matching containers!");
                return;
            }

            Log.Out("[MagicSorter] Missing containers for categories:");
            foreach (var kvp in missingCategories.OrderByDescending(k => k.Value))
            {
                Log.Out($"  - {kvp.Key} ({kvp.Value} items)");
            }

            Log.Out("[MagicSorter] Suggested containers to create:");
            foreach (var category in missingCategories.Keys.OrderBy(k => k))
            {
                Log.Out($"  [Sort:{category}]");
            }
        }

        private void PreviewInternal()
        {
            var containers = FindContainersInRange();
            if (containers.Count == 0)
            {
                Log.Out("[MagicSorter] No containers found in range.");
                return;
            }

            var sortMeContainer = FindSortMeContainer(containers);
            if (sortMeContainer == null)
            {
                Log.Error("[MagicSorter] No [SortMe] container found in range.");
                return;
            }

            if (IsContainerEmpty(sortMeContainer))
            {
                Log.Out("[MagicSorter] Nothing to sort - [SortMe] is empty");
                return;
            }

            var categoryContainers = BuildCategoryMap(containers, sortMeContainer);
            if (categoryContainers.Count == 0)
            {
                Log.Error("[MagicSorter] No [Sort:X] containers found in range.");
                return;
            }

            // Preview sorting
            var items = sortMeContainer.GetItems();
            if (items == null)
            {
                Log.Error("[MagicSorter] Could not access items in [SortMe] container");
                return;
            }

            var previewResults = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            var failures = new List<string>();

            for (int i = 0; i < items.Length; i++)
            {
                var itemStack = items[i];
                if (itemStack.IsEmpty()) continue;

                var itemName = GetItemName(itemStack);
                var itemDesc = $"{itemName} x{itemStack.count}";
                var categories = GetItemCategories(itemStack);

                var targetContainer = FindBestContainer(categories, categoryContainers, itemStack);

                if (targetContainer == null && categoryContainers.TryGetValue(UnknownCategory, out var unknownContainers))
                {
                    targetContainer = GetFullestContainerWithSpace(unknownContainers, itemStack);
                }

                if (targetContainer != null)
                {
                    var targetCategory = ExtractCategory(targetContainer.Name) ?? UnknownCategory;
                    if (!previewResults.ContainsKey(targetCategory))
                    {
                        previewResults[targetCategory] = new List<string>();
                    }
                    previewResults[targetCategory].Add(itemDesc);
                }
                else
                {
                    var catStr = categories.Count > 0 ? string.Join(", ", categories) : "unknown";
                    failures.Add($"{itemDesc} (category: {catStr})");
                }
            }

            // Output preview
            Log.Out("[MagicSorter] Preview - items would be sorted as follows:");
            foreach (var kvp in previewResults.OrderBy(k => k.Key))
            {
                Log.Out($"  [Sort:{kvp.Key}]:");
                foreach (var item in kvp.Value)
                {
                    Log.Out($"    - {item}");
                }
            }

            if (failures.Count > 0)
            {
                Log.Out("  No destination:");
                foreach (var item in failures)
                {
                    Log.Out($"    - {item}");
                }
            }

            int totalItems = previewResults.Values.Sum(v => v.Count);
            Log.Out($"[MagicSorter] Summary: {totalItems} items would be sorted, {failures.Count} would remain");
        }

        private void SortInternal()
        {
            // Find all containers in range
            var containers = FindContainersInRange();
            if (containers.Count == 0)
            {
                Log.Out("[MagicSorter] No containers found in range.");
                return;
            }

            // Find the SortMe container (closest to player)
            var sortMeContainer = FindSortMeContainer(containers);
            if (sortMeContainer == null)
            {
                Log.Error("[MagicSorter] No [SortMe] container found in range.");
                return;
            }

            // Check if empty
            if (IsContainerEmpty(sortMeContainer))
            {
                Log.Out("[MagicSorter] Nothing to sort - [SortMe] is empty");
                return;
            }

            // Build map of category -> containers
            var categoryContainers = BuildCategoryMap(containers, sortMeContainer);
            if (categoryContainers.Count == 0)
            {
                Log.Error("[MagicSorter] No [Sort:X] containers found in range.");
                return;
            }

            // Sort items
            SortItems(sortMeContainer, categoryContainers);

            // Log summary
            LogSummary();
        }

        private List<ContainerWrapper> FindContainersInRange()
        {
            var result = new List<ContainerWrapper>();
            var playerPos = _player.GetBlockPosition();

            // Search in a cube around the player
            for (int x = -_range; x <= _range; x++)
            {
                for (int y = -_range; y <= _range; y++)
                {
                    for (int z = -_range; z <= _range; z++)
                    {
                        var pos = new Vector3i(playerPos.x + x, playerPos.y + y, playerPos.z + z);
                        var tileEntity = _world.GetTileEntity(0, pos);

                        if (tileEntity is TileEntityLootContainer lootContainer)
                        {
                            var name = GetLootContainerName(lootContainer);
                            result.Add(new ContainerWrapper(lootContainer, name, pos));
                        }
                        else if (tileEntity is TileEntityComposite composite)
                        {
                            var name = GetCompositeSignText(composite);
                            if (!string.IsNullOrEmpty(name))
                            {
                                result.Add(new ContainerWrapper(composite, name, pos));
                            }
                        }
                    }
                }
            }

            return result;
        }

        private ContainerWrapper FindSortMeContainer(List<ContainerWrapper> containers)
        {
            var playerPos = _player.GetBlockPosition();
            ContainerWrapper closest = null;
            float closestDist = float.MaxValue;

            foreach (var container in containers)
            {
                if (container.Name != null && container.Name.IndexOf(SortMeTag, StringComparison.OrdinalIgnoreCase) >= 0)
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

                if (!result.ContainsKey(category))
                {
                    result[category] = new List<ContainerWrapper>();
                }
                result[category].Add(container);
            }

            return result;
        }

        private string ExtractCategory(string containerName)
        {
            int startIdx = containerName.IndexOf(SortPrefix, StringComparison.OrdinalIgnoreCase);
            if (startIdx < 0) return null;

            startIdx += SortPrefix.Length;
            int endIdx = containerName.IndexOf(']', startIdx);
            if (endIdx < 0) return null;

            return containerName.Substring(startIdx, endIdx - startIdx).Trim();
        }

        private bool IsContainerEmpty(ContainerWrapper container)
        {
            var items = container.GetItems();
            return items == null || items.All(slot => slot.IsEmpty());
        }

        private void SortItems(ContainerWrapper sortMe,
            Dictionary<string, List<ContainerWrapper>> categoryContainers)
        {
            var items = sortMe.GetItems();
            if (items == null)
            {
                Log.Error("[MagicSorter] Could not access items in [SortMe] container");
                return;
            }

            for (int i = 0; i < items.Length; i++)
            {
                var itemStack = items[i];
                if (itemStack.IsEmpty()) continue;

                var itemName = GetItemName(itemStack);
                var categories = GetItemCategories(itemStack);

                // Try to find a matching container
                var targetContainer = FindBestContainer(categories, categoryContainers, itemStack);

                if (targetContainer == null)
                {
                    // Try unknown fallback
                    if (categoryContainers.TryGetValue(UnknownCategory, out var unknownContainers))
                    {
                        targetContainer = GetFullestContainerWithSpace(unknownContainers, itemStack);
                    }

                    if (targetContainer == null)
                    {
                        if (categories.Count == 0)
                        {
                            Log.Warning($"[MagicSorter] Failed to move {itemName}: unknown category and no [Sort:Unknown] container");
                        }
                        else
                        {
                            Log.Warning($"[MagicSorter] Failed to move {itemName}: no [Sort:X] container for category [{string.Join(", ", categories)}]");
                        }
                        _failedCount++;
                        continue;
                    }
                }

                // Try to move the item
                if (TryMoveItem(sortMe, i, targetContainer, itemStack, out string targetCategory))
                {
                    if (!_sortedCounts.ContainsKey(targetCategory))
                    {
                        _sortedCounts[targetCategory] = 0;
                    }
                    _sortedCounts[targetCategory]++;
                }
                else
                {
                    Log.Warning($"[MagicSorter] Failed to move {itemName}: no space in [Sort:{targetCategory}] containers");
                    _failedCount++;
                }
            }
        }

        private List<string> GetItemCategories(ItemStack itemStack)
        {
            // Use the resolver if available, otherwise fall back to built-in Groups
            if (_resolver != null)
            {
                return _resolver.GetItemCategories(itemStack);
            }

            // Fallback: use built-in Groups directly
            var result = new List<string>();

            if (itemStack.itemValue?.ItemClass == null) return result;

            var groups = itemStack.itemValue.ItemClass.Groups;
            if (groups == null || groups.Length == 0) return result;

            foreach (var group in groups)
            {
                if (!string.IsNullOrEmpty(group))
                {
                    result.Add(group);
                }
            }

            return result;
        }

        private ContainerWrapper FindBestContainer(List<string> itemCategories,
            Dictionary<string, List<ContainerWrapper>> categoryContainers,
            ItemStack itemStack = null)
        {
            // Use the resolver if available for specificity-based matching
            if (_resolver != null)
            {
                return _resolver.FindBestContainer(itemCategories, categoryContainers, itemStack);
            }

            // Fallback: original behavior - try categories in reverse order (most specific first)
            for (int i = itemCategories.Count - 1; i >= 0; i--)
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
            // Sort by fullness descending (fullest first)
            var sorted = containers
                .Select(c => new { Container = c, Fullness = GetContainerFullness(c) })
                .Where(x => HasSpaceForItem(x.Container, itemToFit))
                .OrderByDescending(x => x.Fullness)
                .ToList();

            return sorted.FirstOrDefault()?.Container;
        }

        private float GetContainerFullness(ContainerWrapper container)
        {
            var items = container.GetItems();
            if (items == null || items.Length == 0) return 0;

            int usedSlots = items.Count(s => !s.IsEmpty());
            return (float)usedSlots / items.Length;
        }

        private bool HasSpaceForItem(ContainerWrapper container, ItemStack itemToAdd)
        {
            var items = container.GetItems();
            if (items == null) return false;

            // Check for empty slot
            if (items.Any(s => s.IsEmpty())) return true;

            // Check for stackable slot (if we know what item we're adding)
            if (itemToAdd != null && !itemToAdd.IsEmpty())
            {
                foreach (var slot in items)
                {
                    if (!slot.IsEmpty() &&
                        slot.itemValue.type == itemToAdd.itemValue.type &&
                        slot.count < slot.itemValue.ItemClass.Stacknumber.Value)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private bool TryMoveItem(ContainerWrapper source, int sourceSlot,
            ContainerWrapper target, ItemStack itemStack, out string targetCategory)
        {
            targetCategory = ExtractCategory(target.Name) ?? UnknownCategory;

            var targetItems = target.GetItems();
            var sourceItems = source.GetItems();

            // Try to stack with existing items first
            for (int i = 0; i < targetItems.Length; i++)
            {
                if (!targetItems[i].IsEmpty() &&
                    targetItems[i].itemValue.type == itemStack.itemValue.type)
                {
                    int maxStack = itemStack.itemValue.ItemClass.Stacknumber.Value;
                    int canAdd = maxStack - targetItems[i].count;

                    if (canAdd > 0)
                    {
                        int toMove = Math.Min(canAdd, itemStack.count);
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
            }

            // Try to put in empty slot
            for (int i = 0; i < targetItems.Length; i++)
            {
                if (targetItems[i].IsEmpty())
                {
                    targetItems[i] = itemStack.Clone();
                    sourceItems[sourceSlot] = ItemStack.Empty.Clone();
                    source.SetModified();
                    target.SetModified();
                    return true;
                }
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
                if (authoredText != null && !string.IsNullOrEmpty(authoredText.Text))
                {
                    return authoredText.Text;
                }
            }

            // Fallback to lootListName (block type name)
            if (!string.IsNullOrEmpty(container.lootListName))
            {
                return container.lootListName;
            }

            return null;
        }

        private string GetItemName(ItemStack itemStack)
        {
            if (itemStack?.itemValue?.ItemClass == null) return "Unknown Item";
            return itemStack.itemValue.ItemClass.GetLocalizedItemName() ?? itemStack.itemValue.ItemClass.Name ?? "Unknown Item";
        }

        private void LogSummary()
        {
            int totalSorted = _sortedCounts.Values.Sum();

            if (totalSorted == 0 && _failedCount == 0)
            {
                Log.Out("[MagicSorter] Nothing to sort - [SortMe] is empty");
                return;
            }

            if (totalSorted > 0)
            {
                var breakdown = string.Join(", ", _sortedCounts.Select(kvp => $"{kvp.Value} to [Sort:{kvp.Key}]"));
                Log.Out($"[MagicSorter] Sorted {totalSorted} items: {breakdown}");
            }

            if (_failedCount > 0)
            {
                Log.Out($"[MagicSorter] {_failedCount} items could not be moved (see warnings above)");
            }
        }

        private object GetCompositeModule(TileEntityComposite composite, string moduleName)
        {
            try
            {
                var type = composite.GetType();
                var modulesField = type.GetField("modulesCustomOrder",
                    System.Reflection.BindingFlags.Public |
                    System.Reflection.BindingFlags.NonPublic |
                    System.Reflection.BindingFlags.Instance);

                if (modulesField != null)
                {
                    var modules = modulesField.GetValue(composite) as System.Array;
                    if (modules != null)
                    {
                        foreach (var module in modules)
                        {
                            if (module != null && module.GetType().Name.Contains(moduleName))
                            {
                                return module;
                            }
                        }
                    }
                }
            }
            catch { }
            return null;
        }

        private string GetCompositeSignText(TileEntityComposite composite)
        {
            var signable = GetCompositeModule(composite, "Signable");
            if (signable == null) return null;

            try
            {
                var signTextField = signable.GetType().GetField("signText",
                    System.Reflection.BindingFlags.Public |
                    System.Reflection.BindingFlags.NonPublic |
                    System.Reflection.BindingFlags.Instance);

                if (signTextField != null)
                {
                    var signTextValue = signTextField.GetValue(signable);
                    if (signTextValue != null)
                    {
                        // It's AuthoredText, get the Text property
                        var textProp = signTextValue.GetType().GetProperty("Text");
                        if (textProp != null)
                        {
                            return textProp.GetValue(signTextValue) as string;
                        }
                    }
                }
            }
            catch { }
            return null;
        }
    }
}
