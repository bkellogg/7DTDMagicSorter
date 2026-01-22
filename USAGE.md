# MagicSorter - Usage Guide

## Installation

1. Download the MagicSorter zip file
2. Extract the `MagicSorter` folder to your game's Mods folder:
   `7 Days To Die\Mods\MagicSorter`
3. Launch the game

## Setup

### Input Container
Place a storage crate and rename it (using the sign) to:
```
[MagicSort]
```
Put items you want sorted into this container.

### Output Containers
Create destination containers with labels like:
```
[ms:Food]
[ms:Ammo]
[ms:Weapons]
[ms:Resources]
```

## Sorting Items

### Radial Menu (Recommended)
1. Look at your `[MagicSort]` container
2. Hold **E** to open the radial menu
3. Select **Sort Items**

### Console Commands
Open the F1 console and type:

| Command | Description |
|---------|-------------|
| `ms sort` | Sort items from [MagicSort] into destination containers |
| `ms plan` | Preview what would be sorted (dry run) |
| `ms list` | Show all containers in range |
| `ms scan` | Show items grouped by category |
| `ms missing` | Show categories that need containers |

## Category Reference

Items go to the **most specific** matching container. If you only want a few containers, use the broad categories (Weapons, Food, Resources, etc.) and items will automatically go there.

### Weapons
```
Weapons
├── Ranged ─── Pistols, Rifles, Shotguns, SMGs, MachineGuns, Bows
├── Melee ──── Blades, Clubs, Spears, Sledges, Knuckles
└── Explosives (grenades, rockets)
```
Aliases: `Guns` → Ranged, `Handguns` → Pistols, `Snipers` → Rifles

### Ammo
```
Ammo
├── Ammo9mm, Ammo44, Ammo762
├── AmmoShotgun, AmmoArrow, AmmoRocket
```
Aliases: `Bullets` → Ammo, `Shells` → AmmoShotgun, `Arrows` → AmmoArrow

### Armor & Clothing
```
Armor ─── ArmorHead, ArmorChest, ArmorLegs, ArmorBoots, ArmorGloves
Clothing ─── ClothingHead, ClothingChest, ClothingLegs, ClothingFeet, Eyewear
```
Aliases: `Helmets` → ArmorHead, `Glasses` → Eyewear

### Food & Drinks
```
Food
├── CookedFood, RawFood, CannedFood
├── Drinks
└── Farming (seeds)
```
Aliases: `Meals` → CookedFood, `Water` → Drinks, `Seeds` → Farming

### Medical
```
Medical
├── FirstAid (bandages, kits)
├── Medicine (antibiotics, painkillers)
└── Buffs (vitamins, steroids)
```
Aliases: `Meds` → Medical, `Bandages` → FirstAid

### Resources
```
Resources
├── RawResources (stone, iron, wood)
├── CraftedResources (forged iron, steel)
├── Electrical (wiring, batteries, relays)
├── Mechanical (engines, parts)
├── Chemicals (acid, gas)
└── Organic (leather, cloth, bones)
```
Aliases: `Electronics` → Electrical, `Parts` → Mechanical, `From Earth` → RawResources, `From Animals` → Organic, `Man Made` → CraftedResources

### Building
```
Building
├── Workstations (forge, workbench, chemistry)
├── Lighting (torches, lights)
├── Traps (spikes, turrets)
├── Doors (doors, hatches, gates)
├── Storage, Furniture, Decorations
```
Aliases: `Lights` → Lighting, `Forges` → Workstations

### Tools
```
Tools
├── MiningTools (pickaxe, auger)
├── HarvestingTools (axe, shovel)
├── RepairTools (wrench)
└── ConstructionTools (nailgun)
```
Aliases: `Mining` → MiningTools, `Wrenches` → RepairTools

### Mods
```
Mods
├── WeaponMods ─── ScopeMods, BarrelMods, Grips
└── ArmorMods
```
Aliases: `Attachments` → Mods, `Scopes` → ScopeMods

### Vehicles
```
Vehicles
├── VehicleParts
└── VehicleMods
```
Aliases: `Bikes` → Vehicles, `Tires` → VehicleParts

### Books
```
Books
├── Schematics (recipes)
└── SkillBooks (magazines)
```
Aliases: `Recipes` → Schematics, `Magazines` → SkillBooks

### Other
```
Treasure ─── TreasureMaps, Dukes
Junk (scrap items)
Unknown (fallback for uncategorized items)
```
Aliases: `Money` → Dukes, `Coins` → Dukes, `Trash` → Junk

## Multiplayer

- **Console commands** work with server-only installation
- **Radial menu button** requires the mod on each client to display
- For best experience, install on both server and all clients

## Tips

- Categories are case-insensitive (`[ms:food]` = `[ms:Food]`)
- Use `ms plan` before `ms sort` to preview changes
- Aliases work too: `[ms:Guns]` = `[ms:Weapons]`, `[ms:Meds]` = `[ms:Medicine]`
- Items go to the most specific matching container
- If no match is found, items stay in [MagicSort]
