# MagicSorter

A 7 Days to Die mod that automatically sorts items from a designated input container into categorized storage containers based on naming conventions.

## User Flow

1. Rename storage containers with category labels (e.g., `[Sort:Food]`, `[Sort:Tools]`)
2. Rename one container as `[SortMe]`
3. Dump loot into the `[SortMe]` container
4. Stand near the containers and run `smb` in the F1 console
5. Items move from `[SortMe]` to appropriate `[Sort:X]` containers

## Installation

1. Download the latest release
2. Extract to `7 Days to Die/Mods/MagicSorter/`
3. The folder should contain `MagicSorter.dll` and `ModInfo.xml`

## Console Command

```
magicsort <command> [range]
ms <command> [range]
```

### Commands

| Command | Description |
|---------|-------------|
| `sort` | Sort items from [SortMe] into [Sort:X] containers |
| `list` | List all recognized containers in range |
| `preview` | Show what items would be sorted where (dry run) |

- **range** (optional): Search radius in blocks. Default: 20

### Examples

```
ms sort        # Sort items within 20 blocks
ms sort 30     # Sort items within 30 blocks
ms list        # List all containers in range
ms preview     # Preview what would happen without moving items
```

## Naming Convention

| Container Name | Purpose |
|----------------|---------|
| `[SortMe]` | Source container - items to be sorted |
| `[Sort:<Label>]` | Target container, where `<Label>` matches the item's group |
| `[Sort:Unknown]` | (Optional) Fallback for items with no matching category |

Multiple containers can share the same label (e.g., two `[Sort:Food]` crates). Items fill the fullest container first to consolidate items and top off nearly-full containers before using emptier ones.

### Example Setup

```
[SortMe]           <- Dump all your loot here
[Sort:Food]        <- Food items
[Sort:Ammo]        <- Ammunition
[Sort:Resources]   <- Raw materials
[Sort:Tools]       <- Tools and weapons
[Sort:Unknown]     <- Everything else
```

## Category Mapping

Uses 7D2D's built-in `Groups` property from `itemValue.ItemClass.Groups`.

### Primary Groups (from base game)

| Group | Example Items |
|-------|---------------|
| `Food/Cooking` | Food, drinks, cooking ingredients |
| `Resources` | Raw materials, components |
| `Ammo/Weapons` | Weapons and ammunition |
| `Tools/Traps` | Tools, traps, utility items |
| `Science` | Medical items, chemicals |
| `Clothing` | Armor, clothing |
| `Books` | Skill books, schematics |
| `Special Items` | Quest items, keys, treasure |
| `Decor/Miscellaneous` | Decorative items |

### Matching Strategy

- **Partial matching:** `[Sort:Food]` matches items in `Food/Cooking`
- **Case-insensitive:** `[Sort:food]` works the same as `[Sort:Food]`
- **Priority:** If multiple containers match, the most specific (rightmost) group segment wins
  - Item with groups `Ammo/Weapons, Melee Weapons` with both `[Sort:Ammo]` and `[Sort:Melee Weapons]` → goes to `[Sort:Melee Weapons]`
- **No Group:** Items with empty/null Group route to `[Sort:Unknown]` if present

## Sorting Logic

1. Find all containers within range of player
2. Identify `[SortMe]` container - uses closest to player if multiple exist
3. Build map of `[Sort:X]` containers by category
4. For each item in `[SortMe]`:
   - Get item's Groups from game data
   - Find matching `[Sort:X]` container(s)
   - If no match, try `[Sort:Unknown]` fallback
   - If multiple containers match, fill fullest first
   - Move item (stack with existing matching items if possible)
   - If no space or no matching container, leave item and log failure
5. Log summary to console

## Failure Handling

Items that cannot be moved stay in `[SortMe]`. Failures are logged:

| Reason | Log Message |
|--------|-------------|
| No matching category | `Failed to move [itemName]: no [Sort:X] container for category [category]` |
| No space | `Failed to move [itemName]: no space in [Sort:X] containers` |
| Unknown item, no fallback | `Failed to move [itemName]: unknown category and no [Sort:Unknown] container` |

## Success Logging

```
Sorted 47 items: 12 to [Sort:Food], 8 to [Sort:Ammo], 5 to [Sort:Tools], ...
3 items could not be moved (see errors above)
```

If `[SortMe]` is empty: `Nothing to sort - [SortMe] is empty`

## Building from Source

Requires JetBrains Rider or Visual Studio with .NET Framework 4.8.

```powershell
& 'C:\Program Files\JetBrains\JetBrains Rider 2025.3.1\tools\MSBuild\Current\Bin\MSBuild.exe' '7DTDMagicSorter.csproj' '/p:Configuration=Debug'
```

The build automatically deploys to the game's Mods folder.

### Project Structure

```
MagicSorterMod.cs        - Mod entry point (IModApi)
ConsoleCmdMagicSort.cs   - Console command handler (sort, list, preview)
ContainerSorter.cs       - Main sorting logic
ContainerWrapper.cs      - Abstraction for different container types
```

## Known Issues

- Item retrieval from TileEntityComposite containers (writable storage crates) is still being debugged

## License

MIT
