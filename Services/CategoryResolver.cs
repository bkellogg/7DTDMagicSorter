using System;
using System.Collections.Generic;
using System.Linq;
using MagicSorter.Models;

namespace MagicSorter.Services
{
    /// <summary>
    /// Resolves item categories and finds best matching containers using specificity
    /// </summary>
    public class CategoryResolver
    {
        private readonly MappingLoader _mappingLoader;
        private readonly ModConfiguration _config;

        public CategoryResolver(MappingLoader mappingLoader, ModConfiguration config)
        {
            _mappingLoader = mappingLoader;
            _config = config;
        }

        /// <summary>
        /// Gets categories for an item, checking mappings first, then patterns, then falling back to Groups
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
                if (mappedCategories.Count > 0)
                {
                    return mappedCategories;
                }
            }

            // Second, try pattern-based matching on item name
            var patternCategories = GetCategoriesFromPattern(itemName);
            if (patternCategories.Count > 0)
            {
                return patternCategories;
            }

            // Fall back to built-in Groups if enabled
            if (_config.FallbackToBuiltIn)
            {
                var groups = itemStack.itemValue.ItemClass.Groups;
                if (groups != null && groups.Length > 0)
                {
                    foreach (var group in groups)
                    {
                        if (!string.IsNullOrEmpty(group))
                        {
                            result.Add(group);
                        }
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Gets categories based on item name patterns (e.g., gunHandgun* -> pistols)
        /// </summary>
        private List<string> GetCategoriesFromPattern(string itemName)
        {
            if (string.IsNullOrEmpty(itemName))
                return new List<string>();

            // Guns - check most specific patterns first
            if (itemName.StartsWith("gunBot", StringComparison.OrdinalIgnoreCase))
                return new List<string> { "weapons", "turrets" };
            // SMG-5 is gunHandgunT3SMG5 - check for SMG before generic handgun pattern
            if (itemName.IndexOf("SMG", StringComparison.OrdinalIgnoreCase) >= 0)
                return new List<string> { "weapons", "ranged", "smgs" };
            if (itemName.StartsWith("gunHandgun", StringComparison.OrdinalIgnoreCase))
                return new List<string> { "weapons", "ranged", "pistols" };
            if (itemName.StartsWith("gunShotgun", StringComparison.OrdinalIgnoreCase))
                return new List<string> { "weapons", "ranged", "shotguns" };
            if (itemName.StartsWith("gunRifle", StringComparison.OrdinalIgnoreCase) ||
                itemName.StartsWith("gunTactical", StringComparison.OrdinalIgnoreCase) ||
                itemName.IndexOf("AssaultRifle", StringComparison.OrdinalIgnoreCase) >= 0 ||
                itemName.IndexOf("SniperRifle", StringComparison.OrdinalIgnoreCase) >= 0 ||
                itemName.IndexOf("HuntingRifle", StringComparison.OrdinalIgnoreCase) >= 0 ||
                itemName.IndexOf("LeverAction", StringComparison.OrdinalIgnoreCase) >= 0)
                return new List<string> { "weapons", "ranged", "rifles" };
            // Tactical AR and AK-47 are gunMG* but should be rifles, check before generic MG pattern
            if (itemName.IndexOf("TacticalAR", StringComparison.OrdinalIgnoreCase) >= 0 ||
                itemName.IndexOf("AK47", StringComparison.OrdinalIgnoreCase) >= 0)
                return new List<string> { "weapons", "ranged", "rifles" };
            if (itemName.StartsWith("gunMG", StringComparison.OrdinalIgnoreCase))
                return new List<string> { "weapons", "ranged", "machineguns" };
            if (itemName.StartsWith("gunBow", StringComparison.OrdinalIgnoreCase))
                return new List<string> { "weapons", "ranged", "bows" };
            if (itemName.StartsWith("gunCrossbow", StringComparison.OrdinalIgnoreCase))
                return new List<string> { "weapons", "ranged", "bows" };
            if (itemName.StartsWith("gunExplosives", StringComparison.OrdinalIgnoreCase) ||
                itemName.StartsWith("gunRocketLauncher", StringComparison.OrdinalIgnoreCase))
                return new List<string> { "weapons", "ranged", "explosives" };

            // Melee weapons
            if (itemName.StartsWith("meleeWpnBlade", StringComparison.OrdinalIgnoreCase))
                return new List<string> { "weapons", "melee", "blades" };
            if (itemName.StartsWith("meleeWpnClub", StringComparison.OrdinalIgnoreCase))
                return new List<string> { "weapons", "melee", "clubs" };
            if (itemName.StartsWith("meleeWpnSpear", StringComparison.OrdinalIgnoreCase))
                return new List<string> { "weapons", "melee", "spears" };
            if (itemName.StartsWith("meleeWpnSledge", StringComparison.OrdinalIgnoreCase))
                return new List<string> { "weapons", "melee", "sledges" };
            if (itemName.StartsWith("meleeWpnKnuckles", StringComparison.OrdinalIgnoreCase))
                return new List<string> { "weapons", "melee", "knuckles" };

            // Tools - specific patterns first
            if (itemName.StartsWith("meleeToolTorch", StringComparison.OrdinalIgnoreCase))
                return new List<string> { "building", "lighting" };
            if (itemName.StartsWith("meleeToolPick", StringComparison.OrdinalIgnoreCase))
                return new List<string> { "tools", "miningtools" };
            if (itemName.StartsWith("meleeToolAxe", StringComparison.OrdinalIgnoreCase))
                return new List<string> { "tools", "harvestingtools" };
            if (itemName.StartsWith("meleeToolShovel", StringComparison.OrdinalIgnoreCase))
                return new List<string> { "tools", "harvestingtools" };
            if (itemName.StartsWith("meleeToolRepair", StringComparison.OrdinalIgnoreCase))
                return new List<string> { "tools", "repairtools" };
            if (itemName.StartsWith("meleeToolSalvage", StringComparison.OrdinalIgnoreCase))
                return new List<string> { "tools", "repairtools" };

            // Tool items (workstations, cooking, etc.)
            if (itemName.StartsWith("toolForge", StringComparison.OrdinalIgnoreCase))
                return new List<string> { "building", "workstations" };
            if (itemName.StartsWith("toolCooking", StringComparison.OrdinalIgnoreCase))
                return new List<string> { "tools" };
            if (itemName.StartsWith("tool", StringComparison.OrdinalIgnoreCase))
                return new List<string> { "tools" };

            // Ammo - specific patterns first (exclude non-ammo items that start with ammo)
            if (itemName.StartsWith("ammoGasCan", StringComparison.OrdinalIgnoreCase))
                return new List<string> { "resources", "chemicals" };
            if (itemName.StartsWith("ammo9mm", StringComparison.OrdinalIgnoreCase))
                return new List<string> { "ammo", "ammo9mm" };
            if (itemName.StartsWith("ammo44Magnum", StringComparison.OrdinalIgnoreCase))
                return new List<string> { "ammo", "ammo44" };
            if (itemName.StartsWith("ammo762mm", StringComparison.OrdinalIgnoreCase))
                return new List<string> { "ammo", "ammo762" };
            if (itemName.StartsWith("ammoShotgun", StringComparison.OrdinalIgnoreCase))
                return new List<string> { "ammo", "ammoshotgun" };
            if (itemName.StartsWith("ammoArrow", StringComparison.OrdinalIgnoreCase) ||
                itemName.StartsWith("ammoCrossbow", StringComparison.OrdinalIgnoreCase))
                return new List<string> { "ammo", "ammoarrow" };
            if (itemName.StartsWith("ammoRocket", StringComparison.OrdinalIgnoreCase))
                return new List<string> { "ammo", "ammorocket" };
            if (itemName.StartsWith("ammo", StringComparison.OrdinalIgnoreCase))
                return new List<string> { "ammo" };

            // Throwables
            if (itemName.StartsWith("thrownDynamite", StringComparison.OrdinalIgnoreCase) ||
                itemName.StartsWith("thrownPipe", StringComparison.OrdinalIgnoreCase) ||
                itemName.StartsWith("thrownGrenade", StringComparison.OrdinalIgnoreCase) ||
                itemName.StartsWith("thrownFrag", StringComparison.OrdinalIgnoreCase))
                return new List<string> { "weapons", "explosives" };

            // Armor
            if (itemName.StartsWith("armor", StringComparison.OrdinalIgnoreCase))
            {
                if (itemName.IndexOf("Helmet", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    itemName.IndexOf("Head", StringComparison.OrdinalIgnoreCase) >= 0)
                    return new List<string> { "armor", "armorhead" };
                if (itemName.IndexOf("Chest", StringComparison.OrdinalIgnoreCase) >= 0)
                    return new List<string> { "armor", "armorchest" };
                if (itemName.IndexOf("Legs", StringComparison.OrdinalIgnoreCase) >= 0)
                    return new List<string> { "armor", "armorlegs" };
                if (itemName.IndexOf("Boots", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    itemName.IndexOf("Feet", StringComparison.OrdinalIgnoreCase) >= 0)
                    return new List<string> { "armor", "armorboots" };
                if (itemName.IndexOf("Gloves", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    itemName.IndexOf("Hands", StringComparison.OrdinalIgnoreCase) >= 0)
                    return new List<string> { "armor", "armorgloves" };
                return new List<string> { "armor" };
            }

            // Resources
            if (itemName.StartsWith("resourceForged", StringComparison.OrdinalIgnoreCase))
                return new List<string> { "resources", "craftedresources" };
            if (itemName.StartsWith("resourceScrap", StringComparison.OrdinalIgnoreCase))
                return new List<string> { "resources", "rawresources" };
            if (itemName.StartsWith("resource", StringComparison.OrdinalIgnoreCase))
                return new List<string> { "resources" };

            // Food and drinks
            if (itemName.StartsWith("drink", StringComparison.OrdinalIgnoreCase))
                return new List<string> { "food", "drinks" };
            if (itemName.StartsWith("foodCan", StringComparison.OrdinalIgnoreCase))
                return new List<string> { "food", "cannedfood" };
            if (itemName.StartsWith("foodRaw", StringComparison.OrdinalIgnoreCase) ||
                itemName.StartsWith("foodCrop", StringComparison.OrdinalIgnoreCase))
                return new List<string> { "food", "rawfood" };
            if (itemName.StartsWith("food", StringComparison.OrdinalIgnoreCase))
                return new List<string> { "food", "cookedfood" };

            // Medical
            if (itemName.StartsWith("drugVitamin", StringComparison.OrdinalIgnoreCase) ||
                itemName.StartsWith("drugSteroid", StringComparison.OrdinalIgnoreCase) ||
                itemName.StartsWith("drugRecog", StringComparison.OrdinalIgnoreCase) ||
                itemName.StartsWith("drugFort", StringComparison.OrdinalIgnoreCase))
                return new List<string> { "medical", "buffs" };
            if (itemName.StartsWith("drug", StringComparison.OrdinalIgnoreCase))
                return new List<string> { "medical", "medicine" };
            if (itemName.StartsWith("medicalFirstAid", StringComparison.OrdinalIgnoreCase) ||
                itemName.StartsWith("medicalBandage", StringComparison.OrdinalIgnoreCase))
                return new List<string> { "medical", "firstaid" };
            if (itemName.StartsWith("medical", StringComparison.OrdinalIgnoreCase))
                return new List<string> { "medical" };

            // Mods
            if (itemName.StartsWith("modGun", StringComparison.OrdinalIgnoreCase))
                return new List<string> { "mods", "weaponmods" };
            if (itemName.StartsWith("modArmor", StringComparison.OrdinalIgnoreCase))
                return new List<string> { "mods", "armormods" };
            if (itemName.StartsWith("mod", StringComparison.OrdinalIgnoreCase))
                return new List<string> { "mods" };

            // Vehicle parts
            if (itemName.StartsWith("vehicle", StringComparison.OrdinalIgnoreCase))
                return new List<string> { "vehicles", "vehicleparts" };

            // Books and schematics
            if (itemName.StartsWith("schematic", StringComparison.OrdinalIgnoreCase))
                return new List<string> { "books", "schematics" };
            if (itemName.StartsWith("book", StringComparison.OrdinalIgnoreCase) ||
                itemName.StartsWith("perkBook", StringComparison.OrdinalIgnoreCase))
                return new List<string> { "books", "skillbooks" };

            // Seeds/planting
            if (itemName.StartsWith("planted", StringComparison.OrdinalIgnoreCase) ||
                itemName.StartsWith("seed", StringComparison.OrdinalIgnoreCase))
                return new List<string> { "food", "farming" };

            // Lighting (but not flashlights which are tools, or light mods)
            if ((itemName.IndexOf("Torch", StringComparison.OrdinalIgnoreCase) >= 0 ||
                 itemName.IndexOf("Candle", StringComparison.OrdinalIgnoreCase) >= 0 ||
                 itemName.IndexOf("Lantern", StringComparison.OrdinalIgnoreCase) >= 0 ||
                 itemName.StartsWith("light", StringComparison.OrdinalIgnoreCase) ||
                 itemName.IndexOf("ceilingLight", StringComparison.OrdinalIgnoreCase) >= 0 ||
                 itemName.IndexOf("wallLight", StringComparison.OrdinalIgnoreCase) >= 0 ||
                 itemName.IndexOf("floorLight", StringComparison.OrdinalIgnoreCase) >= 0 ||
                 itemName.IndexOf("Fluorescent", StringComparison.OrdinalIgnoreCase) >= 0) &&
                !itemName.StartsWith("flashlight", StringComparison.OrdinalIgnoreCase) &&
                !itemName.StartsWith("mod", StringComparison.OrdinalIgnoreCase))
                return new List<string> { "building", "lighting" };

            // Workstations
            if (itemName.StartsWith("crucible", StringComparison.OrdinalIgnoreCase) ||
                itemName.StartsWith("forge", StringComparison.OrdinalIgnoreCase) ||
                itemName.StartsWith("workbench", StringComparison.OrdinalIgnoreCase) ||
                itemName.StartsWith("campfire", StringComparison.OrdinalIgnoreCase) ||
                itemName.StartsWith("chemistryStation", StringComparison.OrdinalIgnoreCase) ||
                itemName.StartsWith("cementMixer", StringComparison.OrdinalIgnoreCase))
                return new List<string> { "building", "workstations" };

            // Electrical/batteries
            if (itemName.StartsWith("carBattery", StringComparison.OrdinalIgnoreCase) ||
                itemName.StartsWith("battery", StringComparison.OrdinalIgnoreCase) ||
                itemName.IndexOf("Battery", StringComparison.OrdinalIgnoreCase) >= 0)
                return new List<string> { "resources", "electrical" };

            // Engines/mechanical
            if (itemName.StartsWith("engine", StringComparison.OrdinalIgnoreCase) ||
                itemName.StartsWith("smallEngine", StringComparison.OrdinalIgnoreCase))
                return new List<string> { "resources", "mechanical" };

            // Cooking items (pots, grills, etc)
            if (itemName.StartsWith("cookingPot", StringComparison.OrdinalIgnoreCase) ||
                itemName.StartsWith("cookingGrill", StringComparison.OrdinalIgnoreCase) ||
                itemName.StartsWith("beaker", StringComparison.OrdinalIgnoreCase))
                return new List<string> { "tools" };

            // Paint
            if (itemName.StartsWith("paint", StringComparison.OrdinalIgnoreCase) ||
                itemName.StartsWith("dyePowder", StringComparison.OrdinalIgnoreCase))
                return new List<string> { "resources" };

            // Stone/basic resources
            if (itemName.StartsWith("stone", StringComparison.OrdinalIgnoreCase) ||
                itemName.StartsWith("smallStone", StringComparison.OrdinalIgnoreCase) ||
                itemName.StartsWith("cobblestone", StringComparison.OrdinalIgnoreCase))
                return new List<string> { "resources", "rawresources" };

            // Treasure maps
            if (itemName.IndexOf("TreasureMap", StringComparison.OrdinalIgnoreCase) >= 0 ||
                itemName.IndexOf("treasureQuest", StringComparison.OrdinalIgnoreCase) >= 0 ||
                itemName.IndexOf("BuriedSupplies", StringComparison.OrdinalIgnoreCase) >= 0)
                return new List<string> { "treasure", "treasuremaps" };

            // Flashlight and handheld lights
            if (itemName.StartsWith("flashlight", StringComparison.OrdinalIgnoreCase) ||
                itemName.StartsWith("meleeToolFlashlight", StringComparison.OrdinalIgnoreCase))
                return new List<string> { "tools", "lighting" };

            // Wire tool and electrical tools
            if (itemName.StartsWith("wireTool", StringComparison.OrdinalIgnoreCase) ||
                itemName.StartsWith("toolWire", StringComparison.OrdinalIgnoreCase))
                return new List<string> { "tools", "electrical" };

            // Hatches and doors
            if (itemName.IndexOf("Hatch", StringComparison.OrdinalIgnoreCase) >= 0 ||
                itemName.IndexOf("Door", StringComparison.OrdinalIgnoreCase) >= 0 ||
                itemName.IndexOf("Gate", StringComparison.OrdinalIgnoreCase) >= 0)
                return new List<string> { "building", "doors" };

            // Traps and defenses
            if (itemName.IndexOf("Trap", StringComparison.OrdinalIgnoreCase) >= 0 ||
                itemName.IndexOf("turret", StringComparison.OrdinalIgnoreCase) >= 0 ||
                itemName.StartsWith("electricfence", StringComparison.OrdinalIgnoreCase) ||
                itemName.IndexOf("TriggerPlate", StringComparison.OrdinalIgnoreCase) >= 0 ||
                itemName.IndexOf("motionSensor", StringComparison.OrdinalIgnoreCase) >= 0 ||
                itemName.IndexOf("barbedWire", StringComparison.OrdinalIgnoreCase) >= 0 ||
                itemName.IndexOf("spikes", StringComparison.OrdinalIgnoreCase) >= 0)
                return new List<string> { "building", "traps" };

            // Complete vehicles (not parts)
            if (itemName.Equals("vehicleMinibike", StringComparison.OrdinalIgnoreCase) ||
                itemName.Equals("vehicleMotorcycle", StringComparison.OrdinalIgnoreCase) ||
                itemName.Equals("vehicle4x4Truck", StringComparison.OrdinalIgnoreCase) ||
                itemName.Equals("vehicleGyrocopter", StringComparison.OrdinalIgnoreCase) ||
                itemName.Equals("vehicleBicycle", StringComparison.OrdinalIgnoreCase) ||
                itemName.StartsWith("vehiclePlaceable", StringComparison.OrdinalIgnoreCase))
                return new List<string> { "vehicles" };

            // Wheels and vehicle parts
            if (itemName.IndexOf("Wheel", StringComparison.OrdinalIgnoreCase) >= 0 ||
                itemName.IndexOf("vehiclePart", StringComparison.OrdinalIgnoreCase) >= 0 ||
                itemName.StartsWith("vehicleHandle", StringComparison.OrdinalIgnoreCase) ||
                itemName.StartsWith("vehicleChassis", StringComparison.OrdinalIgnoreCase))
                return new List<string> { "vehicles", "vehicleparts" };

            // Quest reward bundles - extract weapon type from name
            if (itemName.StartsWith("questReward", StringComparison.OrdinalIgnoreCase))
            {
                if (itemName.IndexOf("Handgun", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    itemName.IndexOf("Pistol", StringComparison.OrdinalIgnoreCase) >= 0)
                    return new List<string> { "weapons", "ranged", "pistols" };
                if (itemName.IndexOf("Rifle", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    itemName.IndexOf("AK", StringComparison.OrdinalIgnoreCase) >= 0)
                    return new List<string> { "weapons", "ranged", "rifles" };
                if (itemName.IndexOf("Shotgun", StringComparison.OrdinalIgnoreCase) >= 0)
                    return new List<string> { "weapons", "ranged", "shotguns" };
                if (itemName.IndexOf("SMG", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    itemName.IndexOf("MachineGun", StringComparison.OrdinalIgnoreCase) >= 0)
                    return new List<string> { "weapons", "ranged", "smgs" };
                if (itemName.IndexOf("Bow", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    itemName.IndexOf("Crossbow", StringComparison.OrdinalIgnoreCase) >= 0)
                    return new List<string> { "weapons", "ranged", "bows" };
                if (itemName.IndexOf("Blade", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    itemName.IndexOf("Knife", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    itemName.IndexOf("Machete", StringComparison.OrdinalIgnoreCase) >= 0)
                    return new List<string> { "weapons", "melee", "blades" };
                if (itemName.IndexOf("Club", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    itemName.IndexOf("Bat", StringComparison.OrdinalIgnoreCase) >= 0)
                    return new List<string> { "weapons", "melee", "clubs" };
                if (itemName.IndexOf("Sledge", StringComparison.OrdinalIgnoreCase) >= 0)
                    return new List<string> { "weapons", "melee", "sledges" };
                // Generic weapon bundle
                return new List<string> { "weapons" };
            }

            // Ammo bundles - extract ammo type from name
            if (itemName.StartsWith("ammoBundle", StringComparison.OrdinalIgnoreCase))
            {
                if (itemName.IndexOf("9mm", StringComparison.OrdinalIgnoreCase) >= 0)
                    return new List<string> { "ammo", "ammo9mm" };
                if (itemName.IndexOf("44", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    itemName.IndexOf("Magnum", StringComparison.OrdinalIgnoreCase) >= 0)
                    return new List<string> { "ammo", "ammo44" };
                if (itemName.IndexOf("762", StringComparison.OrdinalIgnoreCase) >= 0)
                    return new List<string> { "ammo", "ammo762" };
                if (itemName.IndexOf("Shotgun", StringComparison.OrdinalIgnoreCase) >= 0)
                    return new List<string> { "ammo", "ammoshotgun" };
                if (itemName.IndexOf("Arrow", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    itemName.IndexOf("Bolt", StringComparison.OrdinalIgnoreCase) >= 0)
                    return new List<string> { "ammo", "ammoarrow" };
                if (itemName.IndexOf("Rocket", StringComparison.OrdinalIgnoreCase) >= 0)
                    return new List<string> { "ammo", "ammorocket" };
                return new List<string> { "ammo" };
            }

            return new List<string>();
        }

        /// <summary>
        /// Finds the best container for an item based on categories and specificity
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

            // Find all matching containers with their specificity
            foreach (var category in itemCategories)
            {
                // Try exact match first
                if (categoryContainers.TryGetValue(category, out var containers))
                {
                    var specificity = GetCategorySpecificity(mappings, category);
                    foreach (var container in containers)
                    {
                        if (HasSpaceForItem(container, itemStack))
                        {
                            candidates.Add(new ContainerCandidate
                            {
                                Container = container,
                                Specificity = specificity,
                                Category = category,
                                IsExactMatch = true
                            });
                        }
                    }
                }

                // Try alias resolution (item category is an alias)
                if (mappings != null)
                {
                    var resolvedCategory = mappings.ResolveAlias(category);
                    if (resolvedCategory != category && categoryContainers.TryGetValue(resolvedCategory, out var aliasContainers))
                    {
                        var specificity = GetCategorySpecificity(mappings, resolvedCategory);
                        foreach (var container in aliasContainers)
                        {
                            if (HasSpaceForItem(container, itemStack))
                            {
                                candidates.Add(new ContainerCandidate
                                {
                                    Container = container,
                                    Specificity = specificity,
                                    Category = resolvedCategory,
                                    IsExactMatch = true
                                });
                            }
                        }
                    }

                    // Try reverse alias (container label is an alias for item's category)
                    foreach (var kvp in categoryContainers)
                    {
                        var containerResolved = mappings.ResolveAlias(kvp.Key);
                        if (containerResolved != kvp.Key &&
                            containerResolved.Equals(category, StringComparison.OrdinalIgnoreCase))
                        {
                            var specificity = GetCategorySpecificity(mappings, category);
                            foreach (var container in kvp.Value)
                            {
                                if (candidates.Any(c => c.Container == container))
                                    continue;

                                if (HasSpaceForItem(container, itemStack))
                                {
                                    candidates.Add(new ContainerCandidate
                                    {
                                        Container = container,
                                        Specificity = specificity,
                                        Category = category,
                                        IsExactMatch = true
                                    });
                                }
                            }
                        }
                    }
                }

                // Note: Removed overly aggressive partial matching that was causing false matches
                // (e.g., "Melee Weapons" alias containing "weapons" would match items with category "weapons" to [Sort:Melee])
                // Now we only use exact matches and alias resolution, with fallback chain for broader categories
            }

            // If no direct matches, try fallback categories
            if (candidates.Count == 0 && mappings != null)
            {
                foreach (var category in itemCategories)
                {
                    // First resolve alias (e.g., "Decor/Miscellaneous" -> "decorations")
                    // then get fallback chain for the resolved category
                    var resolvedCategory = mappings.ResolveAlias(category);
                    var fallbackChain = mappings.GetFallbackChain(resolvedCategory);

                    // If no fallback for resolved category, try original category too
                    if (fallbackChain.Count == 0 && resolvedCategory != category)
                    {
                        fallbackChain = mappings.GetFallbackChain(category);
                    }
                    foreach (var fallbackCategory in fallbackChain)
                    {
                        // Try exact match on fallback
                        if (categoryContainers.TryGetValue(fallbackCategory, out var containers))
                        {
                            var specificity = GetCategorySpecificity(mappings, fallbackCategory);
                            foreach (var container in containers)
                            {
                                if (HasSpaceForItem(container, itemStack))
                                {
                                    candidates.Add(new ContainerCandidate
                                    {
                                        Container = container,
                                        Specificity = specificity,
                                        Category = fallbackCategory,
                                        IsExactMatch = false // Fallback is not exact
                                    });
                                }
                            }
                        }

                        // Try alias resolution on fallback
                        var resolvedFallback = mappings.ResolveAlias(fallbackCategory);
                        if (resolvedFallback != fallbackCategory && categoryContainers.TryGetValue(resolvedFallback, out var aliasContainers))
                        {
                            var specificity = GetCategorySpecificity(mappings, resolvedFallback);
                            foreach (var container in aliasContainers)
                            {
                                if (candidates.Any(c => c.Container == container))
                                    continue;

                                if (HasSpaceForItem(container, itemStack))
                                {
                                    candidates.Add(new ContainerCandidate
                                    {
                                        Container = container,
                                        Specificity = specificity,
                                        Category = resolvedFallback,
                                        IsExactMatch = false
                                    });
                                }
                            }
                        }

                        // Also check reverse alias (container label is an alias for fallback category)
                        foreach (var kvp in categoryContainers)
                        {
                            var containerResolved = mappings.ResolveAlias(kvp.Key);
                            if (containerResolved != kvp.Key &&
                                containerResolved.Equals(fallbackCategory, StringComparison.OrdinalIgnoreCase))
                            {
                                var specificity = GetCategorySpecificity(mappings, fallbackCategory);
                                foreach (var container in kvp.Value)
                                {
                                    if (candidates.Any(c => c.Container == container))
                                        continue;

                                    if (HasSpaceForItem(container, itemStack))
                                    {
                                        candidates.Add(new ContainerCandidate
                                        {
                                            Container = container,
                                            Specificity = specificity,
                                            Category = fallbackCategory,
                                            IsExactMatch = false
                                        });
                                    }
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
            }

            if (candidates.Count == 0)
                return null;

            // Sort by: specificity (highest first), exact match (true first), fullness (fullest first)
            if (_config.UseSpecificityResolution)
            {
                candidates = candidates
                    .OrderByDescending(c => c.Specificity)
                    .ThenByDescending(c => c.IsExactMatch)
                    .ThenByDescending(c => GetContainerFullness(c.Container))
                    .ToList();
            }
            else
            {
                // Original behavior: prefer fullest container
                candidates = candidates
                    .OrderByDescending(c => c.IsExactMatch)
                    .ThenByDescending(c => GetContainerFullness(c.Container))
                    .ToList();
            }

            if (_config.DebugLogging && candidates.Count > 0)
            {
                var best = candidates[0];
                Log.Out($"[MagicSorter] Best match: [{best.Category}] (specificity: {best.Specificity}, exact: {best.IsExactMatch})");
            }

            return candidates[0].Container;
        }

        /// <summary>
        /// Resolves a container label alias to canonical form
        /// </summary>
        public string ResolveAlias(string label)
        {
            if (string.IsNullOrEmpty(label))
                return label;

            var mappings = _mappingLoader?.GetMappings();
            if (mappings != null)
            {
                return mappings.ResolveAlias(label);
            }

            return label;
        }

        private int GetCategorySpecificity(MappingData mappings, string category)
        {
            if (mappings != null)
            {
                return mappings.GetSpecificity(category);
            }
            return 50; // Default specificity
        }

        private bool HasSpaceForItem(ContainerWrapper container, ItemStack itemToAdd)
        {
            var items = container.GetItems();
            if (items == null) return false;

            // Check for empty slot
            if (items.Any(s => s.IsEmpty())) return true;

            // Check for stackable slot
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

        private float GetContainerFullness(ContainerWrapper container)
        {
            var items = container.GetItems();
            if (items == null || items.Length == 0) return 0;

            int usedSlots = items.Count(s => !s.IsEmpty());
            return (float)usedSlots / items.Length;
        }

        private class ContainerCandidate
        {
            public ContainerWrapper Container { get; set; }
            public int Specificity { get; set; }
            public string Category { get; set; }
            public bool IsExactMatch { get; set; }
        }
    }
}
