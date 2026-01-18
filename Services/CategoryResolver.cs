using System.Collections.Generic;
using System.Linq;
using MagicSorter.Extensions;
using MagicSorter.Models;

namespace MagicSorter.Services
{
    /// <summary>
    ///     Resolves item categories and finds best matching containers using specificity
    /// </summary>
    public class CategoryResolver
    {
        private readonly ModConfiguration _config;
        private readonly MappingLoader _mappingLoader;

        public CategoryResolver(MappingLoader mappingLoader, ModConfiguration config)
        {
            _mappingLoader = mappingLoader;
            _config = config;
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

            // Second, try pattern-based matching on item name
            var patternCategories = GetCategoriesFromPattern(itemName);
            if (patternCategories.Count > 0) return patternCategories;

            // Fall back to built-in Groups if enabled
            if (!_config.FallbackToBuiltIn)
                return result;

            var groups = itemStack.itemValue.ItemClass.Groups;
            if (groups != null)
                result.AddRange(groups.Where(g => !string.IsNullOrEmpty(g)));

            return result;
        }

        /// <summary>
        ///     Gets categories based on item name patterns (e.g., gunHandgun* -> pistols)
        /// </summary>
        private static List<string> GetCategoriesFromPattern(string itemName)
        {
            if (string.IsNullOrEmpty(itemName))
                return new List<string>();

            // Schematics - check EARLY because some schematics have misleading prefixes
            // (e.g., plantedGraceCorn1Schematic starts with "planted" but is a schematic)
            if (itemName.HasPrefix("schematic") || itemName.Includes("Schematic"))
                return new List<string> { "books", "schematics" };

            // Mechanical parts and electrical parts - resources first
            if (itemName.Includes("MechanicalParts") || itemName.Includes("Mechanical_Parts"))
                return new List<string> { "resources", "mechanical" };
            if (itemName.Includes("ElectricParts") || itemName.Includes("Electric_Parts") ||
                itemName.Includes("ElectricalParts"))
                return new List<string> { "resources", "electrical" };

            // Weapon/gun parts - check BEFORE weapon patterns (e.g., gunRocketLauncherParts, gunShotgunParts)
            if (itemName.Includes("Parts") &&
                (itemName.HasPrefix("gun") || itemName.HasPrefix("melee")))
                return new List<string> { "resources", "mechanical" };

            // Guns - check most specific patterns first
            if (itemName.HasPrefix("gunBot"))
                return new List<string> { "weapons", "turrets" };
            // SMG-5 is gunHandgunT3SMG5 - check for SMG before generic handgun pattern
            if (itemName.Includes("SMG"))
                return new List<string> { "weapons", "ranged", "smgs" };
            if (itemName.HasPrefix("gunHandgun"))
                return new List<string> { "weapons", "ranged", "pistols" };
            if (itemName.HasPrefix("gunShotgun"))
                return new List<string> { "weapons", "ranged", "shotguns" };
            if (itemName.HasPrefix("gunRifle") || itemName.HasPrefix("gunTactical") ||
                itemName.Includes("AssaultRifle") || itemName.Includes("SniperRifle") ||
                itemName.Includes("HuntingRifle") || itemName.Includes("LeverAction"))
                return new List<string> { "weapons", "ranged", "rifles" };
            // Tactical AR and AK-47 are gunMG* but should be rifles, check before generic MG pattern
            if (itemName.Includes("TacticalAR") || itemName.Includes("AK47"))
                return new List<string> { "weapons", "ranged", "rifles" };
            if (itemName.HasPrefix("gunMG"))
                return new List<string> { "weapons", "ranged", "machineguns" };
            if (itemName.HasPrefix("gunBow") || itemName.HasPrefix("gunCrossbow"))
                return new List<string> { "weapons", "ranged", "bows" };
            if (itemName.HasPrefix("gunExplosives") || itemName.HasPrefix("gunRocketLauncher"))
                return new List<string> { "weapons", "ranged", "explosives" };

            // Melee weapons
            if (itemName.HasPrefix("meleeWpnBlade"))
                return new List<string> { "weapons", "melee", "blades" };
            if (itemName.HasPrefix("meleeWpnClub") || itemName.Includes("PipeBaton") ||
                itemName.Includes("BaseballBat"))
                return new List<string> { "weapons", "melee", "clubs" };
            if (itemName.HasPrefix("meleeWpnSpear"))
                return new List<string> { "weapons", "melee", "spears" };
            if (itemName.HasPrefix("meleeWpnSledge"))
                return new List<string> { "weapons", "melee", "sledges" };
            if (itemName.HasPrefix("meleeWpnKnuckles"))
                return new List<string> { "weapons", "melee", "knuckles" };
            // Generic melee weapons catch-all
            if (itemName.HasPrefix("meleeWpn") ||
                itemName.HasPrefix("melee") && !itemName.HasPrefix("meleeTool"))
                return new List<string> { "weapons", "melee" };

            // Tools - specific patterns first
            if (itemName.HasPrefix("meleeToolTorch"))
                return new List<string> { "building", "lighting" };
            if (itemName.HasPrefix("meleeToolPick"))
                return new List<string> { "tools", "miningtools" };
            if (itemName.HasPrefix("meleeToolAxe") || itemName.HasPrefix("meleeToolShovel"))
                return new List<string> { "tools", "harvestingtools" };
            if (itemName.HasPrefix("meleeToolRepair") || itemName.HasPrefix("meleeToolSalvage"))
                return new List<string> { "tools", "repairtools" };

            // Tool items (workstations, cooking, etc.)
            if (itemName.HasPrefix("toolForge"))
                return new List<string> { "building", "workstations" };
            if (itemName.HasPrefix("tool"))
                return new List<string> { "tools" };

            // Ammo components (bullet tips, casings, buckshot, gunpowder)
            if (itemName.Includes("BulletTip") || itemName.Includes("BulletCasing") ||
                itemName.Includes("Buckshot") || itemName.Includes("GunPowder"))
                return new List<string> { "ammo", "ammocomponents" };

            // Ammo - specific patterns first (exclude non-ammo items that start with ammo)
            if (itemName.HasPrefix("ammoGasCan"))
                return new List<string> { "resources", "chemicals" };
            if (itemName.HasPrefix("ammo9mm"))
                return new List<string> { "ammo", "ammo9mm" };
            if (itemName.HasPrefix("ammo44Magnum"))
                return new List<string> { "ammo", "ammo44" };
            if (itemName.HasPrefix("ammo762mm"))
                return new List<string> { "ammo", "ammo762" };
            if (itemName.HasPrefix("ammoShotgun"))
                return new List<string> { "ammo", "ammoshotgun" };
            if (itemName.HasPrefix("ammoArrow") || itemName.HasPrefix("ammoCrossbow"))
                return new List<string> { "ammo", "ammoarrow" };
            if (itemName.HasPrefix("ammoRocket"))
                return new List<string> { "ammo", "ammorocket" };
            if (itemName.HasPrefix("ammo"))
                return new List<string> { "ammo" };

            // Throwables
            if (itemName.HasPrefix("thrownDynamite") || itemName.HasPrefix("thrownPipe") ||
                itemName.HasPrefix("thrownGrenade") || itemName.HasPrefix("thrownFrag") ||
                itemName.HasPrefix("thrownMolotov"))
                return new List<string> { "weapons", "explosives" };

            // Armor
            if (itemName.HasPrefix("armor"))
            {
                if (itemName.Includes("Helmet") || itemName.Includes("Head"))
                    return new List<string> { "armor", "armorhead" };
                if (itemName.Includes("Chest"))
                    return new List<string> { "armor", "armorchest" };
                if (itemName.Includes("Legs"))
                    return new List<string> { "armor", "armorlegs" };
                if (itemName.Includes("Boots") || itemName.Includes("Feet"))
                    return new List<string> { "armor", "armorboots" };
                if (itemName.Includes("Gloves") || itemName.Includes("Hands"))
                    return new List<string> { "armor", "armorgloves" };
                return new List<string> { "armor" };
            }

            // Resources
            if (itemName.HasPrefix("resourceForged"))
                return new List<string> { "resources", "craftedresources" };
            if (itemName.HasPrefix("resourceScrap"))
                return new List<string> { "resources", "rawresources" };
            if (itemName.Includes("rottingFlesh") || itemName.Includes("rottenFlesh"))
                return new List<string> { "food", "farming" };
            // Animal fat - food first, then rawresources (matches [ms:Natural] and [ms:From Earth])
            if (itemName.Includes("AnimalFat"))
                return new List<string> { "food", "resources", "rawresources" };
            // Organic resources (bones, feathers, hides, leather, etc.)
            if (itemName.Includes("Bone") || itemName.Includes("Feather") ||
                itemName.Includes("AnimalHide") || itemName.Includes("Leather"))
                return new List<string> { "resources", "organic" };
            // Ores and mining resources
            if (itemName.Includes("Ore") || itemName.Includes("OreDeposit") ||
                itemName.Includes("OilShale") || itemName.Includes("Nitrate") ||
                itemName.Includes("Coal"))
                return new List<string> { "resources", "ores" };
            // Honey goes to food
            if (itemName.Includes("Honey"))
                return new List<string> { "food" };
            if (itemName.HasPrefix("resource"))
                return new List<string> { "resources" };

            // Food and drinks
            if (itemName.HasPrefix("drink"))
                return new List<string> { "food", "drinks" };
            if (itemName.HasPrefix("foodCan"))
                return new List<string> { "food", "cannedfood" };
            if (itemName.HasPrefix("foodRaw") || itemName.HasPrefix("foodCrop"))
                return new List<string> { "food", "rawfood" };
            if (itemName.HasPrefix("food"))
                return new List<string> { "food", "cookedfood" };

            // Medical
            if (itemName.HasPrefix("drugVitamin") || itemName.HasPrefix("drugSteroid") ||
                itemName.HasPrefix("drugRecog") || itemName.HasPrefix("drugFort"))
                return new List<string> { "medical", "buffs" };
            if (itemName.HasPrefix("drug"))
                return new List<string> { "medical", "medicine" };
            if (itemName.HasPrefix("medicalFirstAid") || itemName.HasPrefix("medicalBandage"))
                return new List<string> { "medical", "firstaid" };
            if (itemName.HasPrefix("medical") && !itemName.Includes("journal"))
                return new List<string> { "medical" };

            // Mods
            if (itemName.HasPrefix("modGun"))
                return new List<string> { "mods", "weaponmods" };
            if (itemName.HasPrefix("modArmor"))
                return new List<string> { "mods", "armormods" };
            if (itemName.HasPrefix("mod"))
                return new List<string> { "mods" };

            // Vehicle parts (but not vehicle books/magazines)
            if (itemName.HasPrefix("vehicle") && !itemName.Includes("book") &&
                !itemName.Includes("magazine") && !itemName.Includes("journal") &&
                !itemName.Includes("schematic"))
                return new List<string> { "vehicles", "vehicleparts" };

            // Books, magazines, journals (schematics already handled at top of function)
            if (itemName.HasPrefix("book") || itemName.HasPrefix("perkBook") ||
                itemName.Includes("magazine") || itemName.Includes("journal"))
                return new List<string> { "books", "skillbooks" };

            // Seeds/planting (including tree seeds like treePineSeed, treePlantable, etc.)
            if (itemName.HasPrefix("planted") || itemName.HasPrefix("seed") ||
                itemName.HasPrefix("tree") || itemName.Includes("Seed") ||
                itemName.Includes("Plantable"))
                return new List<string> { "food", "farming" };

            // Lighting (but not flashlights which are tools, or light mods)
            if ((itemName.Includes("Torch") || itemName.Includes("Candle") ||
                 itemName.Includes("Lantern") || itemName.HasPrefix("light") ||
                 itemName.Includes("ceilingLight") || itemName.Includes("wallLight") ||
                 itemName.Includes("floorLight") || itemName.Includes("Fluorescent")) &&
                !itemName.HasPrefix("flashlight") && !itemName.HasPrefix("mod"))
                return new List<string> { "building", "lighting" };

            // Workstations
            if (itemName.HasPrefix("crucible") || itemName.HasPrefix("forge") ||
                itemName.HasPrefix("workbench") || itemName.HasPrefix("campfire") ||
                itemName.HasPrefix("chemistryStation") || itemName.HasPrefix("cementMixer"))
                return new List<string> { "building", "workstations" };

            // Electrical/batteries and power items (but not electricfence which is a trap)
            if (itemName.HasPrefix("carBattery") || itemName.HasPrefix("battery") ||
                itemName.Includes("Battery") || itemName.HasPrefix("generator") ||
                itemName.HasPrefix("solar") || itemName.Includes("relay") ||
                (itemName.HasPrefix("electric") && !itemName.HasPrefix("electricfence")) ||
                itemName.IsEqual("switch"))
                return new List<string> { "resources", "electrical" };

            // Engines/mechanical
            if (itemName.HasPrefix("engine") || itemName.HasPrefix("smallEngine"))
                return new List<string> { "resources", "mechanical" };

            // Cooking items (pots, grills, etc)
            if (itemName.HasPrefix("cookingPot") || itemName.HasPrefix("cookingGrill") ||
                itemName.HasPrefix("beaker"))
                return new List<string> { "tools" };

            // Paint
            if (itemName.HasPrefix("paint") || itemName.HasPrefix("dyePowder"))
                return new List<string> { "resources" };

            // Stone/basic resources
            if (itemName.HasPrefix("stone") || itemName.HasPrefix("smallStone") ||
                itemName.HasPrefix("cobblestone"))
                return new List<string> { "resources", "rawresources" };

            // Money/cash (old cash, casino tokens, dukes)
            if (itemName.Includes("oldCash") || itemName.Includes("casinoCoin") ||
                itemName.Includes("casinoToken"))
                return new List<string> { "treasure", "dukes" };

            // Treasure maps
            if (itemName.Includes("TreasureMap") || itemName.Includes("treasureQuest") ||
                itemName.Includes("BuriedSupplies"))
                return new List<string> { "treasure", "treasuremaps" };

            // Flashlight and handheld lights
            if (itemName.HasPrefix("flashlight") || itemName.HasPrefix("meleeToolFlashlight"))
                return new List<string> { "tools", "lighting" };

            // Wire tool and electrical tools
            if (itemName.HasPrefix("wireTool") || itemName.HasPrefix("toolWire"))
                return new List<string> { "tools", "electrical" };

            // Hatches and doors
            if (itemName.Includes("Hatch") || itemName.Includes("Door") ||
                itemName.Includes("Gate"))
                return new List<string> { "building", "doors" };

            // Traps and defenses
            if (itemName.Includes("Trap") || itemName.Includes("turret") ||
                itemName.HasPrefix("electricfence") || itemName.Includes("TriggerPlate") ||
                itemName.Includes("motionSensor") || itemName.Includes("barbedWire") ||
                itemName.Includes("spikes"))
                return new List<string> { "building", "traps" };

            // Complete vehicles (not parts)
            if (itemName.IsEqual("vehicleMinibike") || itemName.IsEqual("vehicleMotorcycle") ||
                itemName.IsEqual("vehicle4x4Truck") || itemName.IsEqual("vehicleGyrocopter") ||
                itemName.IsEqual("vehicleBicycle") || itemName.HasPrefix("vehiclePlaceable"))
                return new List<string> { "vehicles" };

            // Wheels and vehicle parts
            if (itemName.Includes("Wheel") || itemName.Includes("vehiclePart") ||
                itemName.HasPrefix("vehicleHandle") || itemName.HasPrefix("vehicleChassis"))
                return new List<string> { "vehicles", "vehicleparts" };

            // Quest reward bundles - extract weapon type from name
            if (itemName.HasPrefix("questReward"))
            {
                if (itemName.Includes("Handgun") || itemName.Includes("Pistol"))
                    return new List<string> { "weapons", "ranged", "pistols" };
                if (itemName.Includes("Rifle") || itemName.Includes("AK"))
                    return new List<string> { "weapons", "ranged", "rifles" };
                if (itemName.Includes("Shotgun"))
                    return new List<string> { "weapons", "ranged", "shotguns" };
                if (itemName.Includes("SMG") || itemName.Includes("MachineGun"))
                    return new List<string> { "weapons", "ranged", "smgs" };
                if (itemName.Includes("Bow") || itemName.Includes("Crossbow"))
                    return new List<string> { "weapons", "ranged", "bows" };
                if (itemName.Includes("Blade") || itemName.Includes("Knife") ||
                    itemName.Includes("Machete"))
                    return new List<string> { "weapons", "melee", "blades" };
                if (itemName.Includes("Club") || itemName.Includes("Bat"))
                    return new List<string> { "weapons", "melee", "clubs" };
                if (itemName.Includes("Sledge"))
                    return new List<string> { "weapons", "melee", "sledges" };
                // Generic weapon bundle
                return new List<string> { "weapons" };
            }

            // Ammo bundles - extract ammo type from name
            if (itemName.HasPrefix("ammoBundle"))
            {
                if (itemName.Includes("9mm"))
                    return new List<string> { "ammo", "ammo9mm" };
                if (itemName.Includes("44") || itemName.Includes("Magnum"))
                    return new List<string> { "ammo", "ammo44" };
                if (itemName.Includes("762"))
                    return new List<string> { "ammo", "ammo762" };
                if (itemName.Includes("Shotgun"))
                    return new List<string> { "ammo", "ammoshotgun" };
                if (itemName.Includes("Arrow") || itemName.Includes("Bolt"))
                    return new List<string> { "ammo", "ammoarrow" };
                if (itemName.Includes("Rocket"))
                    return new List<string> { "ammo", "ammorocket" };
                return new List<string> { "ammo" };
            }

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
                                IsExactMatch = true
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
                                    IsExactMatch = true
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
                                        IsExactMatch = true
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
                                        IsExactMatch = false // Fallback is not exact
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
                                        IsExactMatch = false
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
                                            IsExactMatch = false
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

            // Sort by: specificity (highest first), exact match (true first), fullness (fullest first)
            if (_config.UseSpecificityResolution)
                candidates = candidates
                    .OrderByDescending(c => c.Specificity)
                    .ThenByDescending(c => c.IsExactMatch)
                    .ThenByDescending(c => c.Container.GetFullness())
                    .ToList();
            else
                // Original behavior: prefer fullest container
                candidates = candidates
                    .OrderByDescending(c => c.IsExactMatch)
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
        }
    }
}