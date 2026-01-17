using System;
using System.Linq;
using System.Reflection;

namespace MagicSorter
{
    /// <summary>
    ///     Wrapper to handle both TileEntityLootContainer and TileEntityComposite uniformly
    /// </summary>
    public class ContainerWrapper
    {
        private const BindingFlags ReflectionFlags =
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

        private readonly object _storageFeature;

        // Cached reflection members for performance - looked up once, reused on every GetItems() call
        private MethodInfo _getItemsMethod;
        private FieldInfo _itemsField;
        private PropertyInfo _itemsProperty;
        private bool _reflectionCached;

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

        private TileEntityLootContainer LootContainer { get; }
        private TileEntityComposite Composite { get; }
        public string Name { get; }
        public Vector3i Position { get; }
        
        public ItemStack[] GetItems()
        {
            if (LootContainer != null) return LootContainer.GetItems();

            if (_storageFeature == null) return null;

            try
            {
                // Cache reflection lookups on first call for performance
                if (!_reflectionCached) CacheReflectionMembers();

                // Use cached method/property/field
                if (_getItemsMethod != null)
                {
                    var result = _getItemsMethod.Invoke(_storageFeature, null);
                    if (result is ItemStack[] methodItems)
                        return methodItems;
                }

                if (_itemsProperty != null)
                {
                    var val = _itemsProperty.GetValue(_storageFeature);
                    if (val is ItemStack[] propItems)
                        return propItems;
                }

                if (_itemsField != null)
                {
                    var val = _itemsField.GetValue(_storageFeature);
                    if (val is ItemStack[] fieldItems)
                        return fieldItems;
                }
            }
            catch (Exception ex)
            {
                Log.Warning($"[MagicSorter] Error accessing items in composite container at {Position}: {ex.Message}");
            }

            return null;
        }

        private void CacheReflectionMembers()
        {
            _reflectionCached = true;
            var type = _storageFeature.GetType();

            // Try GetItems method first (most common)
            _getItemsMethod = type.GetMethod("GetItems", ReflectionFlags);
            if (_getItemsMethod != null)
                return;

            // Try "items" property
            _itemsProperty = type.GetProperty("items", ReflectionFlags);
            if (_itemsProperty != null)
                return;

            // Try to find an ItemStack[] field
            var fields = type.GetFields(ReflectionFlags);
            _itemsField = fields.FirstOrDefault(f => f.FieldType == typeof(ItemStack[]));
        }

        public void SetModified()
        {
            if (LootContainer != null)
                LootContainer.SetModified();
            else if (Composite != null) Composite.SetModified();
        }

        private static object GetStorageFeature(TileEntityComposite composite)
        {
            try
            {
                var type = composite.GetType();
                var modulesField = type.GetField("modulesCustomOrder", ReflectionFlags);

                if (modulesField == null) return null;

                if (!(modulesField.GetValue(composite) is Array modules))
                    return null;

                foreach (var module in modules)
                    if (module?.GetType().Name.Contains("Storage") == true)
                        return module;
            }
            catch (Exception ex)
            {
                Log.Warning($"[MagicSorter] Error finding storage feature in composite: {ex.Message}");
            }

            return null;
        }

        /// <summary>
        ///     Gets the fullness ratio (0.0 to 1.0) of this container
        /// </summary>
        public float GetFullness()
        {
            var items = GetItems();
            if (items == null || items.Length == 0)
                return 0f;

            var usedSlots = items.Count(s => !s.IsEmpty());
            return (float)usedSlots / items.Length;
        }

        /// <summary>
        ///     Checks if this container has space for the given item (empty slot or stackable)
        /// </summary>
        public bool HasSpaceFor(ItemStack itemToAdd)
        {
            var items = GetItems();
            if (items == null)
                return false;

            // Check for empty slot
            if (items.Any(s => s.IsEmpty()))
                return true;

            // Check for stackable slot
            if (itemToAdd == null || itemToAdd.IsEmpty())
                return false;

            return items.Any(slot =>
                !slot.IsEmpty() &&
                slot.itemValue.type == itemToAdd.itemValue.type &&
                slot.count < slot.itemValue.ItemClass.Stacknumber.Value);
        }
    }
}