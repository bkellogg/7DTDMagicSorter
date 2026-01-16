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
}
