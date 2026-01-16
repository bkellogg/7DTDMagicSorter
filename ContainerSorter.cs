using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MagicSorter
{
    /// <summary>
    /// Wrapper to handle both TileEntityLootContainer and TileEntityComposite uniformly
    /// </summary>
    public class ContainerWrapper
    {
        public TileEntityLootContainer LootContainer { get; }
        public TileEntityComposite Composite { get; }
        public string Name { get; }
        public Vector3i Position { get; }

        private object _storageFeature;

        public bool IsComposite => Composite != null;

        public ContainerWrapper(TileEntityLootContainer lootContainer, string name, Vector3i pos)
        {
            LootContainer = lootContainer;
            Name = name;
            Position = pos;
        }

        public ContainerWrapper(TileEntityComposite composite, string name, Vector3i pos)
        {
            Composite = composite;
            Name = name;
            Position = pos;
            _storageFeature = GetStorageFeature(composite);
        }

        public ItemStack[] GetItems()
        {
            if (LootContainer != null)
            {
                return LootContainer.GetItems();
            }

            if (_storageFeature != null)
            {
                try
                {
                    var type = _storageFeature.GetType();
                    var bindingFlags = System.Reflection.BindingFlags.Public |
                                       System.Reflection.BindingFlags.NonPublic |
                                       System.Reflection.BindingFlags.Instance;

                    // Try GetItems method
                    var getItemsMethod = type.GetMethod("GetItems", bindingFlags);
                    if (getItemsMethod != null)
                    {
                        var result = getItemsMethod.Invoke(_storageFeature, null);
                        if (result is ItemStack[] methodItems)
                            return methodItems;
                    }

                    // Try "items" property
                    var itemsProp = type.GetProperty("items", bindingFlags);
                    if (itemsProp != null)
                    {
                        var val = itemsProp.GetValue(_storageFeature);
                        if (val is ItemStack[] propItems)
                            return propItems;
                    }

                    // Try fields
                    var fields = type.GetFields(bindingFlags);
                    foreach (var field in fields)
                    {
                        var val = field.GetValue(_storageFeature);
                        if (val is ItemStack[] items)
                            return items;
                    }
                }
                catch { }
            }

            return null;
        }

        public void SetModified()
        {
            if (LootContainer != null)
            {
                LootContainer.SetModified();
            }
            else if (Composite != null)
            {
                Composite.SetModified();
            }
        }

        private static object GetStorageFeature(TileEntityComposite composite)
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
                            if (module != null && module.GetType().Name.Contains("Storage"))
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
    }

    public class ContainerSorter
    {
        private const string SortMeTag = "[SortMe]";
        private const string SortPrefix = "[Sort:";
        private const string UnknownCategory = "Unknown";

        private readonly EntityPlayer _player;
        private readonly int _range;
        private readonly World _world;

        private readonly Dictionary<string, int> _sortedCounts = new Dictionary<string, int>();
        private int _failedCount;

        public ContainerSorter(EntityPlayer player, int range)
        {
            _player = player;
            _range = range;
            _world = GameManager.Instance.World;
        }

        public void Execute()
        {
            try
            {
                ExecuteInternal();
            }
            catch (Exception ex)
            {
                Log.Error($"[MagicSorter] Unexpected error: {ex.Message}");
            }
        }

        private void ExecuteInternal()
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
                var targetContainer = FindBestContainer(categories, categoryContainers);

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
                            Log.Error($"[MagicSorter] Failed to move {itemName}: unknown category and no [Sort:Unknown] container");
                        }
                        else
                        {
                            Log.Error($"[MagicSorter] Failed to move {itemName}: no [Sort:X] container for category [{string.Join(", ", categories)}]");
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
                    Log.Error($"[MagicSorter] Failed to move {itemName}: no space in [Sort:{targetCategory}] containers");
                    _failedCount++;
                }
            }
        }

        private List<string> GetItemCategories(ItemStack itemStack)
        {
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
            Dictionary<string, List<ContainerWrapper>> categoryContainers)
        {
            // Try categories in reverse order (most specific first, rightmost in the Groups array)
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
                Log.Out($"[MagicSorter] {_failedCount} items could not be moved (see errors above)");
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
