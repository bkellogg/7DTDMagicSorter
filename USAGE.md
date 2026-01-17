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

## Commands
Open the F1 console and type:

| Command | Description |
|---------|-------------|
| `ms sort` | Sort items from [MagicSort] into destination containers |
| `ms plan` | Preview what would be sorted (dry run) |
| `ms list` | Show all containers in range |
| `ms scan` | Show items grouped by category |
| `ms missing` | Show categories that need containers |

## Common Categories

**Combat:** Weapons, Ammo, Armor, Mods

**Survival:** Food, Medicine, Tools, Clothing

**Resources:** Resources, Electrical, Mechanical, Chemicals

**Building:** Building, Traps, Lighting, Workstations

**Other:** Books, Vehicles, Treasure

## Tips

- Categories are case-insensitive (`[ms:food]` = `[ms:Food]`)
- Use `ms plan` before `ms sort` to preview changes
- Aliases work too: `[ms:Guns]` = `[ms:Weapons]`, `[ms:Meds]` = `[ms:Medicine]`
- Items go to the most specific matching container
- If no match is found, items stay in [MagicSort]
