# MagicSorter

A 7 Days to Die mod that automatically sorts items from a designated input container into categorized storage containers based on naming conventions.

## User Flow

1. Rename storage containers with category labels (e.g., `[ms:Food]`, `[ms:Tools]`)
2. Rename one container as `[MagicSort]`
3. Dump loot into the `[MagicSort]` container
4. Stand near the containers and run `ms sort` in the F1 console
5. Items move from `[MagicSort]` to appropriate `[ms:X]` containers

## Installation

1. Download the latest release
2. Extract to `7 Days to Die/Mods/MagicSorter/`
3. The folder should contain:
   - `MagicSorter.dll`
   - `ModInfo.xml`
   - `mappings.json`
   - `Config/MagicSorter.xml`

## Multiplayer Support

| Mode | Installation | Who Can Use |
|------|--------------|-------------|
| Single Player | Install on your game | You |
| Peer-to-Peer (Host) | Install on host's game | All players |
| Dedicated Server | Install on server | All players |

**How it works:**
- The mod only needs to be installed on the **server** (or host in peer-to-peer)
- All connected players can use the `ms` commands
- Each player's commands operate on containers near **their** position
- Players can sort items independently without affecting each other

**Note:** Clients do NOT need to install the mod - the server handles everything.

## Console Commands

```
magicsort <command> [range]
ms <command> [range]
```

### Commands

| Command | Description |
|---------|-------------|
| `sort` | Sort items from [MagicSort] into [ms:X] containers |
| `list` | List all recognized containers in range |
| `plan` | Show what items would be sorted where (dry run) |
| `suggest` | Show items that can't be sorted and suggest containers to create |

- **range** (optional): Search radius in blocks. Default: 20

### Examples

```
ms sort        # Sort items within 20 blocks
ms sort 30     # Sort items within 30 blocks
ms list        # List all containers in range
ms plan        # Preview what would happen without moving items
ms suggest     # See what containers you need to create for unsorted items
```

## Container Naming

| Container Name | Purpose |
|----------------|---------|
| `[MagicSort]` | Source container - items to be sorted |
| `[ms:<Label>]` | Target container, where `<Label>` matches a category |
| `[ms:Unknown]` | (Optional) Fallback for items with no matching category |

Multiple containers can share the same label (e.g., two `[ms:Food]` crates). Items fill the fullest container first to consolidate items.

### Example Setup

```
[MagicSort]         <- Dump all your loot here
[ms:Food]           <- Food, drinks, seeds
[ms:Ammo]           <- Ammunition
[ms:Weapons]        <- Guns, melee weapons
[ms:Tools]          <- Tools (pickaxe, wrench, etc.)
[ms:Medical]        <- First aid, medicine
[ms:From Earth]     <- Raw resources (stone, iron, wood)
[ms:Man Made]       <- Crafted resources (forged iron, etc.)
[ms:From Animals]   <- Organic materials (leather, fat, bone)
[ms:Armor]          <- Armor pieces
[ms:Books]          <- Skill books, schematics
[ms:Treasure]       <- Valuables, treasure maps, dukes
[ms:Unknown]        <- Everything else
```

## Category System

MagicSorter uses a category-based sorting system. Understanding how it works:

### 1. Items Have Categories

Each item belongs to one or more **categories** (defined in `mappings.json` or detected from patterns):

| Item | Categories |
|------|------------|
| Forged Iron | `resources`, `craftedresources` |
| Animal Fat | `resources`, `organic` |
| Small Stone | `resources`, `rawresources` |
| Pistol | `weapons`, `ranged`, `pistols` |
| Steel Pickaxe | `tools`, `miningtools` |

### 2. Aliases Map Container Labels to Categories

When you name a container `[ms:X]`, the label `X` is resolved to a category using **aliases**:

| Container Label | Alias Resolves To | What Items Go Here |
|-----------------|-------------------|-------------------|
| `[ms:Man Made]` | `craftedresources` | Forged iron, bullet casings, concrete |
| `[ms:From Animals]` | `organic` | Leather, animal fat, bone, feathers |
| `[ms:From Earth]` | `rawresources` | Stone, wood, iron ore, coal |
| `[ms:Handguns]` | `pistols` | Pistols, revolvers, magnums |
| `[ms:Medicine]` | `medical` | First aid kits, bandages, antibiotics |

You can also use the category name directly: `[ms:craftedresources]` works the same as `[ms:Man Made]`.

### 3. Matching: Item Category = Container Category

An item goes into a container when one of the item's categories matches the container's resolved category:

```
Forged Iron (categories: resources, craftedresources)
    ↓
[ms:Man Made] (alias resolves to: craftedresources)
    ↓
Match! craftedresources = craftedresources → Item goes here
```

```
Animal Fat (categories: resources, organic)
    ↓
[ms:From Animals] (alias resolves to: organic)
    ↓
Match! organic = organic → Item goes here
```

### 4. Specificity: Most Specific Match Wins

When an item matches multiple containers, the one with higher **specificity** wins. This applies to direct matches, aliases, AND fallbacks:

| Category | Specificity |
|----------|-------------|
| `pistols` | 100 (most specific) |
| `ranged` | 60 |
| `weapons` | 50 (least specific) |

**Example with aliases:**
A pistol has categories `[weapons, ranged, pistols]`
```
Containers:
  [ms:Guns]      → alias resolves to "ranged" (specificity 60)
  [ms:Handguns]  → alias resolves to "pistols" (specificity 100)

Result: Goes to [ms:Handguns] because pistols (100) > ranged (60)
```

**Example with fallbacks:**
A steel pickaxe has categories `[tools, miningtools]`, no direct match exists:
```
Containers:
  [ms:Man Made]   → alias resolves to "craftedresources" (specificity 70)
  [ms:Resources]  → category "resources" (specificity 30)

Fallback chain: miningtools → tools → craftedresources → resources

Result: Goes to [ms:Man Made] because craftedresources (70) > resources (30)
```

### 5. Fallbacks: When No Container Matches

If no container matches an item's categories directly, it walks the **fallback chain** until it finds a match:

```
Steel Pickaxe (categories: tools, miningtools)
    ↓
No [ms:Tools] or [ms:Mining] container exists
    ↓
Fallback: miningtools → tools → craftedresources
    ↓
[ms:Man Made] exists (alias for craftedresources)
    ↓
Item goes to [ms:Man Made]
```

The fallback system also respects specificity - if multiple fallback categories match different containers, the highest specificity wins.

### Main Categories

| Category | Specificity | Description | Fallback |
|----------|-------------|-------------|----------|
| `weapons` | 50 | All weapons | - |
| `ranged` | 60 | Ranged weapons | weapons |
| `pistols` | 100 | Pistols/handguns | ranged |
| `rifles` | 100 | Rifles | ranged |
| `shotguns` | 100 | Shotguns | ranged |
| `melee` | 60 | Melee weapons | weapons |
| `blades` | 90 | Bladed weapons | melee |
| `clubs` | 90 | Clubs/bats | melee |
| `ammo` | 50 | All ammunition | resources |
| `tools` | 50 | All tools | craftedresources |
| `miningtools` | 90 | Pickaxe, auger | tools |
| `armor` | 50 | All armor | - |
| `clothing` | 50 | All clothing | - |
| `food` | 50 | All food/drinks | - |
| `farming` | 70 | Seeds | food |
| `medical` | 50 | Medical items | - |
| `firstaid` | 90 | Bandages, kits | medical |
| `resources` | 30 | All resources | - |
| `rawresources` | 60 | Raw materials | resources |
| `craftedresources` | 70 | Crafted materials | resources |
| `building` | 50 | Building items | craftedresources |
| `vehicles` | 50 | Vehicles & parts | craftedresources |
| `books` | 50 | Books/schematics | - |
| `treasure` | 50 | Valuables | misc |
| `treasuremaps` | 90 | Treasure maps | treasure |
| `dukes` | 100 | Casino tokens | treasure |
| `misc` | 20 | Miscellaneous | - |

### Fallback Chains

When no container matches an item's category, items try fallback categories in order:

```
WEAPONS
pistols/rifles/shotguns/smgs/machineguns/bows ──► ranged ──► weapons
blades/clubs/spears/sledges/knuckles ──► melee ──► weapons
explosives/turrets ──► weapons

TOOLS & BUILDING
miningtools/harvestingtools/repairtools/constructiontools ──► tools ──┐
workstations/lighting/blocks/doors/traps/storage/furniture ──► building ──┼──► craftedresources ──► resources
decorations ──► building ──────────────────────────────────────────────┘

RESOURCES
electrical/mechanical ──► craftedresources ──► resources
rawresources/chemicals/organic/junk ──► resources

VEHICLES
vehicleparts/vehiclemods ──► vehicles ──► craftedresources ──► resources

MODS
scopemods/barrelsmods/grips ──► weaponmods ──► mods
armormods ──► mods

TREASURE
dukes/treasuremaps ──► treasure ──► misc
quest ──► misc

FOOD & MEDICAL
cookedfood/rawfood/cannedfood/drinks/farming ──► food
firstaid/medicine/buffs ──► medical

AMMO
ammo9mm/ammo44/ammo762/ammoshotgun/ammoarrow/ammorocket ──► ammo ──► resources

OTHER
schematics/skillbooks ──► books
armorhead/armorchest/armorlegs/armorboots/armorgloves ──► armor
clothinghead/clothingchest/clothinglegs/clothingfeet/clothinghands/eyewear ──► clothing
```

### Practical Fallback Example

With these containers:
```
[ms:Man Made]    ← alias for craftedresources
[ms:Weapons]
[ms:Food]
```

| Item | Categories | Destination | Fallback Chain |
|------|------------|-------------|----------------|
| Pistol | pistols, ranged, weapons | [ms:Weapons] | Direct match |
| Steel Pickaxe | miningtools, tools | [ms:Man Made] | tools → craftedresources |
| Crucible | workstations, building | [ms:Man Made] | building → craftedresources |
| Minibike | vehicles | [ms:Man Made] | vehicles → craftedresources |
| Treasure Map | treasuremaps, treasure | *unsorted* | treasure → misc (no container) |

### All Categories

| Category | Spec | Fallback | Category | Spec | Fallback |
|----------|------|----------|----------|------|----------|
| weapons | 50 | - | armor | 50 | - |
| ranged | 60 | weapons | armorhead | 90 | armor |
| melee | 60 | weapons | armorchest | 90 | armor |
| pistols | 100 | ranged | armorlegs | 90 | armor |
| rifles | 100 | ranged | armorboots | 90 | armor |
| shotguns | 100 | ranged | armorgloves | 90 | armor |
| smgs | 100 | ranged | clothing | 50 | - |
| machineguns | 100 | ranged | clothinghead | 90 | clothing |
| bows | 100 | ranged | clothingchest | 90 | clothing |
| explosives | 90 | weapons | clothinglegs | 90 | clothing |
| blades | 90 | melee | clothingfeet | 90 | clothing |
| clubs | 90 | melee | clothinghands | 90 | clothing |
| spears | 90 | melee | eyewear | 90 | clothing |
| sledges | 90 | melee | food | 50 | - |
| knuckles | 90 | melee | cookedfood | 90 | food |
| ammo | 50 | resources | rawfood | 90 | food |
| ammo9mm | 100 | ammo | cannedfood | 90 | food |
| ammo44 | 100 | ammo | drinks | 90 | food |
| ammo762 | 100 | ammo | farming | 70 | food |
| ammoshotgun | 100 | ammo | medical | 50 | - |
| ammoarrow | 100 | ammo | firstaid | 90 | medical |
| ammorocket | 100 | ammo | medicine | 90 | medical |
| tools | 50 | craftedresources | buffs | 90 | medical |
| miningtools | 90 | tools | books | 50 | - |
| harvestingtools | 90 | tools | schematics | 80 | books |
| repairtools | 90 | tools | skillbooks | 80 | books |
| constructiontools | 90 | tools | mods | 50 | - |
| resources | 30 | - | weaponmods | 70 | mods |
| rawresources | 60 | resources | armormods | 70 | mods |
| craftedresources | 70 | resources | scopemods | 90 | weaponmods |
| electrical | 80 | craftedresources | barrelsmods | 90 | weaponmods |
| chemicals | 80 | resources | grips | 90 | weaponmods |
| organic | 70 | resources | vehicles | 50 | craftedresources |
| mechanical | 80 | craftedresources | vehicleparts | 80 | vehicles |
| building | 50 | craftedresources | vehiclemods | 90 | vehicles |
| blocks | 70 | building | treasure | 50 | misc |
| doors | 90 | building | treasuremaps | 90 | treasure |
| traps | 90 | building | dukes | 100 | treasure |
| lighting | 90 | building | misc | 20 | - |
| storage | 90 | building | quest | 80 | misc |
| workstations | 90 | building | junk | 40 | resources |
| furniture | 80 | building | Unknown | 10 | - |
| decorations | 80 | building | | | |

### All Aliases

Aliases let you use friendly names on containers. Here's what each alias resolves to:

| Alias (Container Label) | Resolves To Category | Items That Go Here |
|-------------------------|---------------------|-------------------|
| **Resources** |||
| `From Earth`, `Natural`, `iron`, `stone`, `wood` | rawresources | Stone, wood, iron ore, coal |
| `Man Made`, `Crafted`, `forged`, `crafted` | craftedresources | Forged iron, concrete, duct tape |
| `From Animals`, `leather`, `cloth` | organic | Leather, animal fat, bone, cloth |
| `electronics`, `wiring` | electrical | Electrical parts, wire, sensors |
| `acid`, `gas`, `Chemicals`, `Science` | chemicals | Acid, gas cans |
| `parts`, `engines` | mechanical | Mechanical parts, springs, engines |
| `materials` | resources | All resources (general) |
| **Weapons** |||
| `Handguns`, `revolvers` | pistols | Pistols, magnums, pipe revolvers |
| `longrifles`, `snipers`, `assault`, `assaultrifle` | rifles | AK-47, hunting rifle, sniper |
| `automatics`, `smg` | smgs | SMG-5, pipe machine gun |
| `lmg`, `machinegun` | machineguns | M60 machine gun |
| `crossbows` | bows | Bows, crossbows |
| `grenades`, `bombs`, `rockets`, `dynamite` | explosives | Grenades, dynamite, rockets |
| `swords`, `knives`, `machetes`, `axes` | blades | Machete, hunting knife |
| `bats` | clubs | Baseball bat, wooden club |
| `hammers` | sledges | Sledgehammer |
| `fists` | knuckles | Brass/iron/steel knuckles |
| `guns`, `firearms`, `shooting` | ranged | All ranged weapons |
| **Ammo** |||
| `bullets`, `ammunition` | ammo | All ammo |
| `9mm` | ammo9mm | 9mm bullets |
| `44mag` | ammo44 | .44 Magnum bullets |
| `762` | ammo762 | 7.62mm bullets |
| `shells`, `shotgunshells` | ammoshotgun | Shotgun shells |
| `arrows`, `bolts` | ammoarrow | Arrows, crossbow bolts |
| **Tools** |||
| `pickaxes`, `augers`, `mining` | miningtools | Pickaxe, auger |
| `wrenches`, `repair` | repairtools | Wrench, impact driver |
| `nailguns`, `construction` | constructiontools | Nail gun |
| `Tools/Traps` | tools | All tools |
| **Food & Medical** |||
| `Food/Cooking` | food | All food |
| `meals`, `cooked` | cookedfood | Cooked meals |
| `meat`, `raw` | rawfood | Raw meat, crops |
| `cans`, `canned` | cannedfood | Canned food |
| `water`, `beverages`, `beer` | drinks | Water, beer, coffee |
| `seeds`, `crops` | farming | Seeds, farm plots |
| `Medicine`, `Medical`, `meds`, `drugs`, `pills`, `healing` | medical | All medical items |
| `bandages`, `kits` | firstaid | Bandages, first aid kits |
| `vitamins`, `steroids` | buffs | Vitamins, steroids, recog |
| **Building** |||
| `Building Supplies` | building | All building items |
| `walls`, `floors`, `frames` | blocks | Building blocks |
| `gates`, `hatches` | doors | Doors, hatches, gates |
| `spikes`, `turrets` | traps | Spike traps, turrets |
| `lights`, `torches` | lighting | Torches, lights |
| `chests`, `crates`, `boxes` | storage | Storage containers |
| `forges`, `workbenches`, `chemistry` | workstations | Forge, workbench, chem station |
| `Decor/Miscellaneous` | decorations | Decorative items |
| **Vehicles** |||
| `bikes`, `motorcycles`, `cars`, `gyrocopter` | vehicles | Complete vehicles |
| `tires`, `batteries` | vehicleparts | Vehicle parts |
| **Other** |||
| `gear` | armor | All armor |
| `helmets` | armorhead | Helmets |
| `glasses`, `goggles` | eyewear | Glasses, goggles |
| `reading` | books | All books |
| `magazines` | skillbooks | Skill magazines |
| `recipes`, `blueprints` | schematics | Schematics |
| `attachments` | mods | All mods |
| `scopes`, `sights` | scopemods | Scope mods |
| `barrels`, `silencers`, `suppressors` | barrelsmods | Barrel mods |
| `Treasure`, `loot`, `valuables` | treasure | Valuables |
| `Treasure Maps`, `maps` | treasuremaps | Treasure maps |
| `money`, `tokens`, `casino`, `coins` | dukes | Duke's Casino Tokens |
| `trash`, `scrap`, `garbage` | junk | Junk items |
| `Special Items` | misc | Miscellaneous |
| `misc`, `other`, `unsorted` | unknown | Unknown/fallback |

## Sorting Logic

1. Find all containers within range of player
2. Identify `[MagicSort]` container (closest if multiple exist)
3. Build map of `[ms:X]` containers by category
4. For each item in `[MagicSort]`:
   - Get item's categories (from mappings, patterns, or game Groups)
   - Find matching `[ms:X]` container using specificity
   - If no match, try fallback categories
   - If still no match, try `[ms:Unknown]`
   - Move item (stack with existing if possible)
5. Log summary to console

## Configuration

### mappings.json

Defines categories, item mappings, aliases, and fallbacks. Can be customized.

```json
{
  "categories": {
    "pistols": { "specificity": 100, "description": "Pistol-type weapons" }
  },
  "items": {
    "gunPistol": ["weapons", "ranged", "pistols"]
  },
  "aliases": {
    "handguns": "pistols"
  },
  "categoryFallbacks": {
    "pistols": "ranged",
    "ranged": "weapons"
  }
}
```

### Config/MagicSorter.xml

```xml
<MagicSorterConfig>
  <FallbackToBuiltIn>true</FallbackToBuiltIn>
  <UseSpecificityResolution>true</UseSpecificityResolution>
  <DebugLogging>false</DebugLogging>
</MagicSorterConfig>
```

## Building from Source

Requires JetBrains Rider or Visual Studio with .NET Framework 4.8.

```powershell
& 'C:\Program Files\JetBrains\JetBrains Rider 2025.3.1\tools\MSBuild\Current\Bin\MSBuild.exe' '7DTDMagicSorter.csproj' '/p:Configuration=Debug'
```

The build automatically deploys to the game's Mods folder.

### Project Structure

```
MagicSorterMod.cs              - Mod entry point (IModApi)
ConsoleCmdMagicSort.cs         - Console command handler
ContainerManager.cs            - Container operations (sort, list, suggest)
ContainerWrapper.cs            - Abstraction for container types
Models/
  MappingData.cs               - Category/item mapping data structure
  CategoryDefinition.cs        - Category with specificity
  ModConfiguration.cs          - Configuration settings
Services/
  CategoryResolver.cs          - Specificity-based category matching
  MappingLoader.cs             - Load mappings from JSON (uses Newtonsoft.Json)
  ConfigurationLoader.cs       - XML config loader
Config/
  MagicSorter.xml              - Default configuration
mappings.json                  - Category definitions and mappings
```

## License

MIT
