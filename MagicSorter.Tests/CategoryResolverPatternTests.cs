using System.Collections.Generic;
using System.IO;
using MagicSorter.Services;
using NUnit.Framework;

namespace MagicSorter.Tests
{
    /// <summary>
    /// Tests for CategoryResolver.GetCategoriesFromPattern to ensure pattern matching
    /// behavior is preserved during migration to data-driven patterns.
    /// </summary>
    [TestFixture]
    public class CategoryResolverPatternTests
    {
        [OneTimeSetUp]
        public void Setup()
        {
            // Load patterns from mappings.json
            var mappingsPath = Path.Combine(TestContext.CurrentContext.TestDirectory, "mappings.json");
            CategoryResolver.SetMappingsPath(mappingsPath);
        }

        private static void AssertCategories(string itemName, params string[] expected)
        {
            var result = CategoryResolver.GetCategoriesFromPattern(itemName);
            Assert.AreEqual(new List<string>(expected), result,
                $"Item '{itemName}' should map to [{string.Join(", ", expected)}] but got [{string.Join(", ", result)}]");
        }

        private static void AssertNoMatch(string itemName)
        {
            var result = CategoryResolver.GetCategoriesFromPattern(itemName);
            Assert.IsEmpty(result, $"Item '{itemName}' should not match any pattern but got [{string.Join(", ", result)}]");
        }

        #region Edge Cases

        [Test]
        public void NullOrEmpty_ReturnsEmptyList()
        {
            AssertNoMatch(null);
            AssertNoMatch("");
        }

        [Test]
        public void UnknownItem_ReturnsEmptyList()
        {
            AssertNoMatch("someRandomItemName");
            AssertNoMatch("unknownThing123");
        }

        #endregion

        #region Schematics - Must be checked early due to misleading prefixes

        [Test]
        public void Schematics_PrefixPattern()
        {
            AssertCategories("schematicGunPistol", "books", "schematics");
            AssertCategories("schematicArmorChest", "books", "schematics");
        }

        [Test]
        public void Schematics_ContainsPattern()
        {
            // This is the key case - plantedGraceCorn1Schematic starts with "planted"
            // but should be categorized as schematic, not farming
            AssertCategories("plantedGraceCorn1Schematic", "books", "schematics");
            AssertCategories("someItemSchematic", "books", "schematics");
        }

        #endregion

        #region Mechanical and Electrical Parts (Resources)

        [Test]
        public void MechanicalParts_AsResources()
        {
            AssertCategories("resourceMechanicalParts", "resources", "mechanical");
            AssertCategories("Mechanical_Parts", "resources", "mechanical");
        }

        [Test]
        public void ElectricalParts_AsResources()
        {
            AssertCategories("resourceElectricParts", "resources", "electrical");
            AssertCategories("Electric_Parts", "resources", "electrical");
            AssertCategories("resourceElectricalParts", "resources", "electrical");
        }

        [Test]
        public void WeaponParts_AsMechanical()
        {
            // Gun parts should go to mechanical, not weapons
            AssertCategories("gunRocketLauncherParts", "resources", "mechanical");
            AssertCategories("gunShotgunParts", "resources", "mechanical");
            AssertCategories("meleeToolParts", "resources", "mechanical");
        }

        #endregion

        #region Guns - Ranged Weapons

        [Test]
        public void Turrets()
        {
            AssertCategories("gunBotT1JunkTurret", "weapons", "turrets");
            AssertCategories("gunBotT2Turret", "weapons", "turrets");
        }

        [Test]
        public void SMGs()
        {
            // SMG-5 is gunHandgunT3SMG5 - should be SMG not pistol
            AssertCategories("gunHandgunT3SMG5", "weapons", "ranged", "smgs");
            AssertCategories("gunSMG", "weapons", "ranged", "smgs");
        }

        [Test]
        public void Pistols()
        {
            AssertCategories("gunHandgunT1Pistol", "weapons", "ranged", "pistols");
            AssertCategories("gunHandgunT2Revolver", "weapons", "ranged", "pistols");
            AssertCategories("gunHandgunT3Magnum44", "weapons", "ranged", "pistols");
        }

        [Test]
        public void Shotguns()
        {
            AssertCategories("gunShotgunT1PumpShotgun", "weapons", "ranged", "shotguns");
            AssertCategories("gunShotgunT2DoubleBarrel", "weapons", "ranged", "shotguns");
            AssertCategories("gunShotgunT3AutoShotgun", "weapons", "ranged", "shotguns");
        }

        [Test]
        public void Rifles()
        {
            AssertCategories("gunRifleT1HuntingRifle", "weapons", "ranged", "rifles");
            AssertCategories("gunRifleT2SniperRifle", "weapons", "ranged", "rifles");
            AssertCategories("gunTacticalAR", "weapons", "ranged", "rifles");
            AssertCategories("gunLeverActionRifle", "weapons", "ranged", "rifles");
        }

        [Test]
        public void Rifles_TacticalARandAK47()
        {
            // These are gunMG* but should be rifles
            AssertCategories("gunMGT3TacticalAR", "weapons", "ranged", "rifles");
            AssertCategories("gunMGT2AK47", "weapons", "ranged", "rifles");
        }

        [Test]
        public void MachineGuns()
        {
            // Note: gunMGT1AK47 contains "AK47" so it matches rifles pattern first
            // Only generic gunMG* items without AK47/TacticalAR go to machineguns
            AssertCategories("gunMGT3M60", "weapons", "ranged", "machineguns");
        }

        [Test]
        public void Bows()
        {
            AssertCategories("gunBowT1WoodenBow", "weapons", "ranged", "bows");
            AssertCategories("gunBowT2CompoundBow", "weapons", "ranged", "bows");
            AssertCategories("gunCrossbowT1", "weapons", "ranged", "bows");
        }

        [Test]
        public void Explosives_Ranged()
        {
            AssertCategories("gunExplosivesT1PipeBomb", "weapons", "ranged", "explosives");
            AssertCategories("gunRocketLauncher", "weapons", "ranged", "explosives");
        }

        #endregion

        #region Melee Weapons

        [Test]
        public void Blades()
        {
            AssertCategories("meleeWpnBladeT1Machete", "weapons", "melee", "blades");
            AssertCategories("meleeWpnBladeT2Knife", "weapons", "melee", "blades");
        }

        [Test]
        public void Clubs()
        {
            AssertCategories("meleeWpnClubT1WoodenClub", "weapons", "melee", "clubs");
            AssertCategories("meleeWpnClubT2SteelClub", "weapons", "melee", "clubs");
            AssertCategories("meleeWpnPipeBaton", "weapons", "melee", "clubs");
            AssertCategories("meleeWpnBaseballBat", "weapons", "melee", "clubs");
        }

        [Test]
        public void Spears()
        {
            AssertCategories("meleeWpnSpearT1WoodenSpear", "weapons", "melee", "spears");
            AssertCategories("meleeWpnSpearT2IronSpear", "weapons", "melee", "spears");
        }

        [Test]
        public void Sledges()
        {
            AssertCategories("meleeWpnSledgeT1SledgeHammer", "weapons", "melee", "sledges");
            AssertCategories("meleeWpnSledgeT2SteelSledge", "weapons", "melee", "sledges");
        }

        [Test]
        public void Knuckles()
        {
            AssertCategories("meleeWpnKnucklesT1", "weapons", "melee", "knuckles");
            AssertCategories("meleeWpnKnucklesT2Steel", "weapons", "melee", "knuckles");
        }

        [Test]
        public void GenericMelee()
        {
            // Generic melee weapons that don't match specific subcategories
            AssertCategories("meleeWpnStunBaton", "weapons", "melee");
        }

        #endregion

        #region Tools

        [Test]
        public void Torch_AsLighting()
        {
            AssertCategories("meleeToolTorch", "building", "lighting");
        }

        [Test]
        public void Pickaxes()
        {
            AssertCategories("meleeToolPickT1StoneAxe", "tools", "miningtools");
            AssertCategories("meleeToolPickT2IronPickaxe", "tools", "miningtools");
        }

        [Test]
        public void HarvestingTools()
        {
            AssertCategories("meleeToolAxeT1StoneAxe", "tools", "harvestingtools");
            AssertCategories("meleeToolAxeT2IronAxe", "tools", "harvestingtools");
            AssertCategories("meleeToolShovelT1Stone", "tools", "harvestingtools");
        }

        [Test]
        public void RepairTools()
        {
            AssertCategories("meleeToolRepairT1StoneAxe", "tools", "repairtools");
            AssertCategories("meleeToolSalvageT1Ratchet", "tools", "repairtools");
        }

        [Test]
        public void ToolForge_AsWorkstation()
        {
            AssertCategories("toolForge", "building", "workstations");
        }

        [Test]
        public void GenericTools()
        {
            AssertCategories("toolWrench", "tools");
        }

        #endregion

        #region Ammo

        [Test]
        public void AmmoComponents()
        {
            AssertCategories("resourceBulletTip", "ammo", "ammocomponents");
            AssertCategories("resourceBulletCasing", "ammo", "ammocomponents");
            AssertCategories("resourceBuckshot", "ammo", "ammocomponents");
            AssertCategories("resourceGunPowder", "ammo", "ammocomponents");
        }

        [Test]
        public void AmmoGasCan_AsChemicals()
        {
            // Gas cans are not ammo, they're chemicals
            AssertCategories("ammoGasCan", "resources", "chemicals");
        }

        [Test]
        public void Ammo9mm()
        {
            AssertCategories("ammo9mmBullet", "ammo", "ammo9mm");
            AssertCategories("ammo9mmBulletHP", "ammo", "ammo9mm");
            AssertCategories("ammo9mmBulletAP", "ammo", "ammo9mm");
        }

        [Test]
        public void Ammo44Magnum()
        {
            AssertCategories("ammo44MagnumBullet", "ammo", "ammo44");
            AssertCategories("ammo44MagnumBulletHP", "ammo", "ammo44");
        }

        [Test]
        public void Ammo762()
        {
            AssertCategories("ammo762mmBullet", "ammo", "ammo762");
            AssertCategories("ammo762mmBulletAP", "ammo", "ammo762");
        }

        [Test]
        public void AmmoShotgun()
        {
            AssertCategories("ammoShotgunShell", "ammo", "ammoshotgun");
            AssertCategories("ammoShotgunSlug", "ammo", "ammoshotgun");
        }

        [Test]
        public void AmmoArrow()
        {
            AssertCategories("ammoArrowStone", "ammo", "ammoarrow");
            AssertCategories("ammoArrowIron", "ammo", "ammoarrow");
            AssertCategories("ammoCrossbowBoltSteel", "ammo", "ammoarrow");
        }

        [Test]
        public void AmmoRocket()
        {
            AssertCategories("ammoRocketHE", "ammo", "ammorocket");
            AssertCategories("ammoRocketFrag", "ammo", "ammorocket");
        }

        [Test]
        public void GenericAmmo()
        {
            AssertCategories("ammoSomeNewType", "ammo");
        }

        #endregion

        #region Throwables

        [Test]
        public void Throwables_AsExplosives()
        {
            AssertCategories("thrownDynamiteStick", "weapons", "explosives");
            AssertCategories("thrownPipeBomb", "weapons", "explosives");
            AssertCategories("thrownGrenade", "weapons", "explosives");
            AssertCategories("thrownFragGrenade", "weapons", "explosives");
            AssertCategories("thrownMolotovCocktail", "weapons", "explosives");
        }

        #endregion

        #region Armor

        [Test]
        public void ArmorHead()
        {
            AssertCategories("armorIronHelmet", "armor", "armorhead");
            AssertCategories("armorMilitaryHead", "armor", "armorhead");
        }

        [Test]
        public void ArmorChest()
        {
            AssertCategories("armorIronChest", "armor", "armorchest");
            AssertCategories("armorMilitaryChest", "armor", "armorchest");
        }

        [Test]
        public void ArmorLegs()
        {
            AssertCategories("armorIronLegs", "armor", "armorlegs");
            AssertCategories("armorMilitaryLegs", "armor", "armorlegs");
        }

        [Test]
        public void ArmorBoots()
        {
            AssertCategories("armorIronBoots", "armor", "armorboots");
            AssertCategories("armorMilitaryFeet", "armor", "armorboots");
        }

        [Test]
        public void ArmorGloves()
        {
            AssertCategories("armorIronGloves", "armor", "armorgloves");
            AssertCategories("armorMilitaryHands", "armor", "armorgloves");
        }

        [Test]
        public void GenericArmor()
        {
            AssertCategories("armorSomethingElse", "armor");
        }

        #endregion

        #region Resources

        [Test]
        public void ForgedResources()
        {
            AssertCategories("resourceForgedIron", "resources", "craftedresources");
            AssertCategories("resourceForgedSteel", "resources", "craftedresources");
        }

        [Test]
        public void ScrapResources()
        {
            AssertCategories("resourceScrapIron", "resources", "rawresources");
            AssertCategories("resourceScrapBrass", "resources", "rawresources");
        }

        [Test]
        public void RottenFlesh_AsFarming()
        {
            AssertCategories("resourceRottingFlesh", "food", "farming");
            AssertCategories("rottenFlesh", "food", "farming");
        }

        [Test]
        public void AnimalFat_MultiCategory()
        {
            // Animal fat goes to food first, then resources
            AssertCategories("resourceAnimalFat", "food", "resources", "rawresources");
        }

        [Test]
        public void OrganicResources()
        {
            AssertCategories("resourceBone", "resources", "organic");
            AssertCategories("resourceFeather", "resources", "organic");
            AssertCategories("resourceAnimalHide", "resources", "organic");
            AssertCategories("resourceLeather", "resources", "organic");
        }

        [Test]
        public void Ores()
        {
            AssertCategories("resourceIronOre", "resources", "ores");
            AssertCategories("resourceOilShale", "resources", "ores");
            AssertCategories("resourceNitratePowder", "resources", "ores");
            AssertCategories("resourceCoal", "resources", "ores");
        }

        [Test]
        public void Honey_AsFood()
        {
            AssertCategories("resourceHoney", "food");
        }

        [Test]
        public void GenericResources()
        {
            AssertCategories("resourceWood", "resources");
            AssertCategories("resourceClay", "resources");
        }

        #endregion

        #region Food and Drinks

        [Test]
        public void Drinks()
        {
            AssertCategories("drinkJarPureMineralWater", "food", "drinks");
            AssertCategories("drinkJarCoffee", "food", "drinks");
        }

        [Test]
        public void CannedFood()
        {
            AssertCategories("foodCanChili", "food", "cannedfood");
            AssertCategories("foodCanPasta", "food", "cannedfood");
        }

        [Test]
        public void RawFood()
        {
            AssertCategories("foodRawMeat", "food", "rawfood");
            AssertCategories("foodCropCorn", "food", "rawfood");
        }

        [Test]
        public void CookedFood()
        {
            AssertCategories("foodGrilledMeat", "food", "cookedfood");
            AssertCategories("foodBaconAndEggs", "food", "cookedfood");
        }

        #endregion

        #region Medical

        [Test]
        public void Buffs()
        {
            AssertCategories("drugVitamins", "medical", "buffs");
            AssertCategories("drugSteroids", "medical", "buffs");
            AssertCategories("drugRecog", "medical", "buffs");
            AssertCategories("drugFortBites", "medical", "buffs");
        }

        [Test]
        public void Medicine()
        {
            AssertCategories("drugAntibiotics", "medical", "medicine");
            AssertCategories("drugPainkillers", "medical", "medicine");
        }

        [Test]
        public void FirstAid()
        {
            AssertCategories("medicalFirstAidKit", "medical", "firstaid");
            AssertCategories("medicalBandage", "medical", "firstaid");
        }

        [Test]
        public void GenericMedical()
        {
            AssertCategories("medicalSplint", "medical");
        }

        [Test]
        public void MedicalJournal_NotMedical()
        {
            // Journals should not match medical pattern
            // This tests the exclusion of "journal" from medical
            var result = CategoryResolver.GetCategoriesFromPattern("medicaljournal");
            Assert.IsFalse(result.Contains("medical") && result.Count == 1,
                "Medical journals should not be categorized as medical supplies");
        }

        #endregion

        #region Mods

        [Test]
        public void WeaponMods()
        {
            AssertCategories("modGunScopeSmall", "mods", "weaponmods");
            AssertCategories("modGunBarrelExtender", "mods", "weaponmods");
        }

        [Test]
        public void ArmorMods()
        {
            AssertCategories("modArmorBandolier", "mods", "armormods");
            AssertCategories("modArmorHelmetLight", "mods", "armormods");
        }

        [Test]
        public void GenericMods()
        {
            AssertCategories("modSomeOtherMod", "mods");
        }

        #endregion

        #region Vehicles

        [Test]
        public void VehicleParts_NotBooks()
        {
            AssertCategories("vehicleWheelsPart", "vehicles", "vehicleparts");
            AssertCategories("vehicleEnginePart", "vehicles", "vehicleparts");
        }

        [Test]
        public void VehicleBooks_AsBooks()
        {
            // Vehicle books should NOT match vehicle pattern
            var result = CategoryResolver.GetCategoriesFromPattern("vehicleBicycleBook");
            Assert.IsFalse(result.Contains("vehicles"),
                "Vehicle books should not be categorized as vehicles");
        }

        [Test]
        public void CompleteVehicles()
        {
            // Note: All vehicle* items match the generic vehicleparts pattern first (line 239)
            // because it comes before the specific complete vehicles check (line 326)
            // This is existing behavior we're preserving
            AssertCategories("vehicleMinibike", "vehicles", "vehicleparts");
            AssertCategories("vehicleMotorcycle", "vehicles", "vehicleparts");
            AssertCategories("vehicle4x4Truck", "vehicles", "vehicleparts");
            AssertCategories("vehicleGyrocopter", "vehicles", "vehicleparts");
            AssertCategories("vehicleBicycle", "vehicles", "vehicleparts");
            AssertCategories("vehiclePlaceableBicycle", "vehicles", "vehicleparts");
        }

        [Test]
        public void VehiclePartsByName()
        {
            AssertCategories("vehicleWheelPart", "vehicles", "vehicleparts");
            AssertCategories("vehicleHandleBars", "vehicles", "vehicleparts");
            AssertCategories("vehicleChassisMinibike", "vehicles", "vehicleparts");
        }

        #endregion

        #region Books and Magazines

        [Test]
        public void SkillBooks()
        {
            // Note: "bookMiningForEngineers" contains "orE" (case-insensitive match for "Ore")
            // so it matches the ores pattern. Using a different example.
            AssertCategories("bookGunsmithing", "books", "skillbooks");
            AssertCategories("perkBookElectrician", "books", "skillbooks");
        }

        [Test]
        public void Magazines()
        {
            AssertCategories("magazinePistolPete", "books", "skillbooks");
            AssertCategories("vehicleMagazine", "books", "skillbooks");
        }

        [Test]
        public void Journals()
        {
            AssertCategories("journalTip", "books", "skillbooks");
        }

        #endregion

        #region Farming and Seeds

        [Test]
        public void PlantedSeeds()
        {
            AssertCategories("plantedCorn1", "food", "farming");
            AssertCategories("plantedPotato1", "food", "farming");
        }

        [Test]
        public void Seeds()
        {
            AssertCategories("seedCorn", "food", "farming");
            AssertCategories("seedPotato", "food", "farming");
        }

        [Test]
        public void TreeSeeds()
        {
            AssertCategories("treePineSeed", "food", "farming");
            AssertCategories("treePlantedMaple", "food", "farming");
        }

        #endregion

        #region Building - Lighting

        [Test]
        public void Lighting_NotFlashlight()
        {
            AssertCategories("candlestick", "building", "lighting");
            AssertCategories("wallTorch", "building", "lighting");
            AssertCategories("ceilingLight", "building", "lighting");
            AssertCategories("wallLightIndustrial", "building", "lighting");
            // Note: pattern checks for "floorLight" not "floorLamp"
            AssertCategories("floorLightModern", "building", "lighting");
            // Note: Any word containing "ore" (like "Fluorescent") matches ores pattern first
            // Using "lightStreetLamp" which has no "ore"
            AssertCategories("lightStreetLamp", "building", "lighting");
        }

        [Test]
        public void Flashlight_AsTool()
        {
            // Flashlights should NOT match lighting pattern
            AssertCategories("flashlightT1", "tools", "lighting");
        }

        #endregion

        #region Building - Workstations

        [Test]
        public void Workstations()
        {
            AssertCategories("crucible", "building", "workstations");
            AssertCategories("forgeIron", "building", "workstations");
            AssertCategories("workbenchPortable", "building", "workstations");
            AssertCategories("campfireBasic", "building", "workstations");
            AssertCategories("chemistryStationBasic", "building", "workstations");
            AssertCategories("cementMixer", "building", "workstations");
        }

        #endregion

        #region Electrical

        [Test]
        public void Batteries()
        {
            AssertCategories("carBattery", "resources", "electrical");
            AssertCategories("batteryBank", "resources", "electrical");
            AssertCategories("smallBattery", "resources", "electrical");
        }

        [Test]
        public void ElectricalItems()
        {
            AssertCategories("generatorBank", "resources", "electrical");
            AssertCategories("solarPanel", "resources", "electrical");
            AssertCategories("electricRelay", "resources", "electrical");
            AssertCategories("switch", "resources", "electrical");
        }

        [Test]
        public void ElectricFence_NotElectrical()
        {
            // Electric fence should be a trap, not electrical
            AssertCategories("electricfencepost", "building", "traps");
        }

        #endregion

        #region Mechanical

        [Test]
        public void Engines()
        {
            AssertCategories("enginePart", "resources", "mechanical");
            AssertCategories("smallEnginePart", "resources", "mechanical");
        }

        #endregion

        #region Cooking Items

        [Test]
        public void CookingItems_AsTools()
        {
            AssertCategories("cookingPot", "tools");
            AssertCategories("cookingGrill", "tools");
            AssertCategories("beaker", "tools");
        }

        #endregion

        #region Paint

        [Test]
        public void Paint_AsResources()
        {
            AssertCategories("paintBrush", "resources");
            AssertCategories("dyePowderRed", "resources");
        }

        #endregion

        #region Stone Resources

        [Test]
        public void StoneResources()
        {
            AssertCategories("stoneAxe", "resources", "rawresources");
            AssertCategories("smallStone", "resources", "rawresources");
            AssertCategories("cobblestoneBlock", "resources", "rawresources");
        }

        #endregion

        #region Money and Treasure

        [Test]
        public void Money()
        {
            AssertCategories("casinoToken", "treasure", "dukes");
            AssertCategories("oldCash", "treasure", "dukes");
            AssertCategories("casinoCoin", "treasure", "dukes");
        }

        [Test]
        public void TreasureMaps()
        {
            // Note: "treasureMapForest" contains "ore" (case-insensitive) so matches ores pattern first
            // Using items that don't accidentally contain "Ore"
            AssertCategories("TreasureMapDesert", "treasure", "treasuremaps");
            AssertCategories("treasureQuestNote", "treasure", "treasuremaps");
            AssertCategories("questBuriedSupplies", "treasure", "treasuremaps");
        }

        #endregion

        #region Flashlight and Wire Tools

        [Test]
        public void Flashlights()
        {
            AssertCategories("flashlightT1", "tools", "lighting");
            AssertCategories("meleeToolFlashlightT2", "tools", "lighting");
        }

        [Test]
        public void WireTools()
        {
            // Note: "wireTool" prefix matches wire tools pattern
            AssertCategories("wireToolElectrical", "tools", "electrical");
            // Note: "toolWire*" items start with "tool" so they match generic tools pattern first
            // Only "wireTool*" prefix matches the wire tools pattern specifically
        }

        #endregion

        #region Doors and Hatches

        [Test]
        public void DoorsAndHatches()
        {
            AssertCategories("woodHatch", "building", "doors");
            AssertCategories("steelDoor", "building", "doors");
            AssertCategories("vaultGate", "building", "doors");
        }

        #endregion

        #region Traps

        [Test]
        public void Traps()
        {
            AssertCategories("turretSledge", "building", "traps");
            AssertCategories("landmineTrap", "building", "traps");
            AssertCategories("TriggerPlateZombie", "building", "traps");
            AssertCategories("motionSensor", "building", "traps");
            AssertCategories("barbedWireFence", "building", "traps");
            AssertCategories("woodSpikes", "building", "traps");
        }

        #endregion

        #region Quest Rewards

        [Test]
        public void QuestRewardPistol()
        {
            AssertCategories("questRewardHandgunBundle", "weapons", "ranged", "pistols");
            AssertCategories("questRewardPistolT2", "weapons", "ranged", "pistols");
        }

        [Test]
        public void QuestRewardRifle()
        {
            AssertCategories("questRewardRifleBundle", "weapons", "ranged", "rifles");
            AssertCategories("questRewardAK47", "weapons", "ranged", "rifles");
        }

        [Test]
        public void QuestRewardShotgun()
        {
            AssertCategories("questRewardShotgunBundle", "weapons", "ranged", "shotguns");
        }

        [Test]
        public void QuestRewardSMG()
        {
            AssertCategories("questRewardSMGBundle", "weapons", "ranged", "smgs");
            AssertCategories("questRewardMachineGun", "weapons", "ranged", "smgs");
        }

        [Test]
        public void QuestRewardBow()
        {
            AssertCategories("questRewardBowBundle", "weapons", "ranged", "bows");
            AssertCategories("questRewardCrossbowT1", "weapons", "ranged", "bows");
        }

        [Test]
        public void QuestRewardMelee()
        {
            AssertCategories("questRewardBladeBundle", "weapons", "melee", "blades");
            AssertCategories("questRewardKnifeT2", "weapons", "melee", "blades");
            AssertCategories("questRewardMachete", "weapons", "melee", "blades");
            AssertCategories("questRewardClubBundle", "weapons", "melee", "clubs");
            AssertCategories("questRewardBatT1", "weapons", "melee", "clubs");
            AssertCategories("questRewardSledgeBundle", "weapons", "melee", "sledges");
        }

        [Test]
        public void QuestRewardGenericWeapon()
        {
            AssertCategories("questRewardWeaponBundle", "weapons");
        }

        #endregion

        #region Ammo Bundles

        [Test]
        public void AmmoBundles()
        {
            // Note: ammoBundle* items match the generic ammo pattern first,
            // which just returns ["ammo"] without extracting subcategory
            AssertCategories("ammoBundle9mm", "ammo");
            AssertCategories("ammoBundle44Magnum", "ammo");
            AssertCategories("ammoBundle762", "ammo");
            AssertCategories("ammoBundleShotgunShell", "ammo");
            AssertCategories("ammoBundleArrowIron", "ammo");
            AssertCategories("ammoBundleBoltSteel", "ammo");
            AssertCategories("ammoBundleRocketHE", "ammo");
            AssertCategories("ammoBundleGeneric", "ammo");
        }

        #endregion

        #region Case Insensitivity

        [Test]
        public void CaseInsensitive_Prefix()
        {
            // Should work regardless of case
            AssertCategories("GUNHANDGUNT1PISTOL", "weapons", "ranged", "pistols");
            AssertCategories("GunHandgunT1Pistol", "weapons", "ranged", "pistols");
        }

        [Test]
        public void CaseInsensitive_Contains()
        {
            AssertCategories("somethingSCHEMATIC", "books", "schematics");
            AssertCategories("itemWithSCHEMATICinName", "books", "schematics");
        }

        #endregion
    }
}
