# LLM Guide: Editing MagicSorter mappings.json

This document provides instructions for AI assistants (Claude, GPT, Cursor, etc.) to help users modify the `mappings.json` file for the MagicSorter mod in 7 Days to Die.

---

## What is MagicSorter?

MagicSorter is a mod that automatically sorts items between storage containers. Players set up:

1. **Input containers** - labeled with a sign reading `[MagicSort]`
2. **Output containers** - labeled with signs like `[ms:food]`, `[ms:ammo]`, `[ms:weapons]`, etc.

When the player runs `ms sort` in the console (or uses the UI button), items move from input containers to the appropriate output containers based on category mappings.

## What does mappings.json do?

The `mappings.json` file defines:
- **What categories exist** (food, weapons, ammo, medical, etc.)
- **Which items belong to which categories** (via patterns or direct mappings)
- **Fallback behavior** (if no `[ms:pistols]` container exists, pistols go to `[ms:weapons]` instead)

## Why would someone customize it?

Common reasons users want to edit mappings:

| Scenario | Solution |
|----------|----------|
| "I want to separate canned food from cooked food" | Create a new category or use existing subcategories |
| "I installed a mod that adds new items and they're not sorting" | Add patterns or direct mappings for the modded items |
| "I want all explosives-related stuff in one box" | Create a custom category with patterns matching those items |
| "I don't like how X item is categorized" | Add a higher-priority pattern or direct item mapping |
| "I want a shortcut name for my container" | Add an alias |

## Understanding User Requests

When a user asks for help, they're typically trying to:

1. **Create a new organizational category** - "I want a box just for building materials"
2. **Recategorize specific items** - "Put gunpowder with ammo, not resources"
3. **Handle new/modded items** - "Items from [ModName] aren't sorting anywhere"
4. **Fix unexpected behavior** - "Why is X going to the wrong container?"

Ask clarifying questions like:
- "What items should go in this category?"
- "What container do you want these items to fall back to if your specific container doesn't exist?"
- "Do you know the internal item names, or should I suggest patterns based on common naming?"

---

## File Location

The file is located at: `Mods/MagicSorter/mappings.json`

After editing, the user should run `ms reload` in the game's F1 console to apply changes without restarting.

---

## Important Concepts

### Case Sensitivity

- **Pattern matching is case-sensitive** - `"Contains": "Chip"` will NOT match `foodPotatochips` (lowercase 'c')
- **Category names should be lowercase** - Use `snacks` not `Snacks`
- **Aliases should be lowercase** - Use `junkfood` not `JunkFood`
- **Item names preserve game casing** - Match exactly what `ms scan` shows (e.g., `foodBaconAndEggs`)

### Why Multiple Categories in Arrays?

When a pattern lists `["food", "cannedfood"]`, the item belongs to BOTH categories. This serves two purposes:

1. **Container matching** - The item can go to either `[ms:food]` OR `[ms:cannedfood]` containers. The most specific matching container wins.
2. **Fallback chain** - If no `[ms:cannedfood]` exists, the item falls back to `[ms:food]`.

Always list from broadest to most specific: `["weapons", "ranged", "pistols"]`

### What Happens to Unmatched Items?

Items that don't match ANY pattern or direct mapping:
- Stay in the input container
- Are reported as "uncategorized" in `ms plan` output

This is why fallback chains matter - you want items to land somewhere rather than being stuck.

### Display Names vs Internal Names

Users see "Bacon and Eggs" in-game, but the internal name is `foodBaconAndEggs`. Patterns match against **internal names only**.

If the user doesn't know internal names:
- Ask them to run `ms scan` in-game with items in a container
- Guess based on common patterns (food items start with `food`, weapons with `gun` or `melee`, etc.)
- The Item Name Reference section below shows common prefixes

---

## JSON Structure Overview

```json
{
  "version": "2.0.0",
  "patterns": [ ... ],
  "categories": { ... },
  "items": { ... },
  "aliases": { ... },
  "tags": { ... },
  "categoryFallbacks": { ... }
}
```

---

## Section: `patterns`

An array of rules that match item names to categories. Patterns are evaluated in priority order (highest first).

### Pattern Object Schema

```json
{
  "type": "Prefix|Contains|Equals",
  "match": "string to match",
  "categories": ["category1", "category2"],
  "priority": 100-1000,
  "alsoMatch": "optional second string",
  "alsoMatchType": "Prefix|Contains|Equals",
  "exclude": "optional string to exclude",
  "excludeType": "Prefix|Contains|Equals"
}
```

### Fields

| Field | Required | Description |
|-------|----------|-------------|
| `type` | Yes | How to match: `Prefix` (starts with), `Contains` (anywhere), `Equals` (exact) |
| `match` | Yes | The string to look for in item names |
| `categories` | Yes | Array of category names this item belongs to (include parent + specific) |
| `priority` | Yes | Higher = checked first. Use 600-800 for custom patterns |
| `alsoMatch` | No | Second condition that must ALSO match |
| `alsoMatchType` | No | How to match `alsoMatch` (defaults to same as `type`) |
| `exclude` | No | If this matches, the pattern does NOT apply |
| `excludeType` | No | How to match `exclude` |

### Priority Guidelines

| Priority Range | Use For |
|----------------|---------|
| 900-1000 | Critical overrides (schematics, special items) |
| 800-900 | Weapon subtypes (pistols, rifles, etc.) |
| 700-800 | Standard categories (food types, resources) |
| 600-700 | Custom user categories |
| 500-600 | Fallback/general patterns |

### Where to Insert New Patterns

**Position in the array doesn't matter** - priority determines evaluation order, not array position.

For readability, add new patterns:
- Near similar patterns (group food patterns together, weapon patterns together, etc.)
- Or at the end of the array, before the closing `]`

Either works functionally.

### Examples

**Simple pattern:**
```json
{ "type": "Prefix", "match": "foodCan", "categories": ["food", "cannedfood"], "priority": 705 }
```

**Pattern with exclusion:**
```json
{ "type": "Prefix", "match": "medical", "exclude": "journal", "excludeType": "Contains", "categories": ["medical"], "priority": 675 }
```

**Compound pattern (must match both):**
```json
{ "type": "Prefix", "match": "armor", "alsoMatch": "Helmet", "alsoMatchType": "Contains", "categories": ["armor", "armorhead"], "priority": 765 }
```

---

## Section: `categories`

Defines all valid category names. Keys are lowercase, no spaces.

### Category Object Schema

```json
{
  "categoryname": {
    "specificity": 10-100,
    "description": "Human-readable description"
  }
}
```

### Specificity Guidelines

| Specificity | Use For |
|-------------|---------|
| 90-100 | Very specific (pistols, cannedfood, ammo9mm) |
| 70-80 | Moderately specific (electrical, organic) |
| 50-60 | Broad categories (weapons, food, tools) |
| 30-50 | Very broad (resources, building) |
| 10-20 | Catch-all (misc, unknown) |

### Example

```json
"snacks": {
  "specificity": 90,
  "description": "Snack foods like chips and candy"
}
```

---

## Section: `items`

Direct item name to categories mapping. Use when patterns don't work well for specific items.

### Schema

```json
{
  "exactItemName": ["category1", "category2", "category3"]
}
```

### Example

```json
"foodSpecialTreat": ["food", "snacks"],
"gunPistol": ["weapons", "ranged", "pistols"]
```

### When to Use

- Item name doesn't follow patterns
- Item would match wrong pattern
- One-off items that need specific placement

---

## Section: `aliases`

Maps alternative names to canonical category names. Allows users to use different names on container signs.

### Schema

```json
{
  "alternativename": "canonicalcategory"
}
```

### Example

```json
"junkfood": "snacks",
"chips": "snacks",
"candy": "snacks",
"handguns": "pistols",
"9mm": "ammo9mm"
```

---

## Section: `tags`

Groups of related categories for potential future use. Not required for basic functionality.

### Schema

```json
{
  "tagname": ["category1", "category2", "category3"]
}
```

---

## Section: `categoryFallbacks`

Defines where items go if the specific category container doesn't exist.

### Schema

```json
{
  "specificcategory": "parentcategory"
}
```

### Example

```json
"snacks": "food",
"pistols": "ranged",
"ranged": "weapons",
"cannedfood": "food",
"food": "misc"
```

### Rules

- Create a chain: specific → parent → grandparent
- Every custom category should have a fallback
- Ultimate fallbacks are usually: `misc`, `resources`, `unknown`

---

## Common Tasks

### Task: Add a New Category

1. **Add to `categories`:**
```json
"newcategory": {
  "specificity": 90,
  "description": "Description here"
}
```

2. **Add patterns to `patterns`:**
```json
{ "type": "Contains", "match": "MatchString", "categories": ["parentcategory", "newcategory"], "priority": 700 }
```

3. **Add fallback to `categoryFallbacks`:**
```json
"newcategory": "parentcategory"
```

4. **Optionally add aliases:**
```json
"alternatename": "newcategory"
```

### Task: Add Items to Existing Category

**Option A - Add a pattern:**
```json
{ "type": "Contains", "match": "ItemNamePart", "categories": ["existingcategory"], "priority": 700 }
```

**Option B - Add direct item mapping:**
```json
"exactItemName": ["parentcategory", "existingcategory"]
```

### Task: Move Items from One Category to Another

Add a higher-priority pattern that captures those items:
```json
{ "type": "Contains", "match": "SpecificItem", "categories": ["newcategory"], "priority": 750 }
```

Higher priority (750) overrides lower priority patterns.

### Task: Exclude Items from a Category

Add exclusion to existing pattern or create override pattern:
```json
{ "type": "Prefix", "match": "food", "exclude": "foodRotten", "excludeType": "Prefix", "categories": ["food", "cookedfood"], "priority": 695 }
```

---

## Item Name Reference

Item names in 7 Days to Die follow patterns:

| Prefix | Type | Examples |
|--------|------|----------|
| `food` | Food items | `foodCanChili`, `foodRawMeat`, `foodSteakAndPotato` |
| `drink` | Beverages | `drinkJarBeer`, `drinkCanCoffee` |
| `gun` | Ranged weapons | `gunPistol`, `gunShotgunPump`, `gunAK47` |
| `melee` | Melee items | `meleeWpnClub`, `meleeToolAxe` |
| `ammo` | Ammunition | `ammo9mmBullet`, `ammoShotgunShell` |
| `resource` | Resources | `resourceWood`, `resourceForgedIron` |
| `armor` | Armor pieces | `armorLightChest`, `armorHeavyHelmet` |
| `mod` | Modifications | `modGunScopeLarge`, `modArmorHelmetLight` |
| `drug` | Medicine/buffs | `drugPainkillers`, `drugVitamins` |
| `medical` | Medical items | `medicalBandage`, `medicalFirstAidKit` |
| `planted` | Seeds/farming | `plantedCorn1`, `plantedPotato1` |
| `schematic` | Schematics | `schematicGunPistol` |
| `book` | Skill books | `bookGunslinger`, `perkBookElectrician` |
| `vehicle` | Vehicles/parts | `vehicleMinibike`, `vehiclePartWheel` |

Users can run `ms scan` in-game to see actual item names in their containers.

---

## Discovering Existing Categories

**Always read the user's actual `mappings.json` file** to see what categories, patterns, and aliases currently exist. Don't assume - the user may have customized it.

To find existing categories, look at the `"categories"` section of their mappings.json. Each key is a valid category name.

**Common category naming conventions:**
- Broad categories: `weapons`, `food`, `resources`, `tools`, `medical`
- Specific subcategories: `pistols`, `cannedfood`, `rawresources`, `miningtools`
- Always lowercase, no spaces or special characters

When adding a new category, check if a similar one already exists that the user could use instead.

---

## Validation Rules

Before saving, verify:

1. **Valid JSON syntax** - Use a JSON validator if unsure
2. **All category references exist** - Categories in patterns/items must be defined in `categories`
3. **Fallbacks exist for new categories** - Every custom category needs a `categoryFallbacks` entry
4. **No duplicate keys** - Each item/category/alias name must be unique
5. **Arrays use square brackets** - `["item1", "item2"]` not `("item1", "item2")`
6. **Strings use double quotes** - `"text"` not `'text'`
7. **No trailing commas** - Last item in array/object has no comma after it

---

## Testing Instructions

Tell the user to:

1. Save the edited `mappings.json`
2. In-game, open F1 console
3. Run `ms reload` - should see "Mappings reloaded successfully"
4. Run `ms plan` to preview sorting
5. Run `ms sort` to execute

If reload fails, check JSON syntax at https://jsonlint.com

---

## Example: Complete New Category

User request: "Add a category for explosive materials"

**Add to `categories`:**
```json
"explosivematerials": {
  "specificity": 85,
  "description": "Explosive crafting materials"
}
```

**Add to `patterns`:**
```json
{ "type": "Contains", "match": "GunPowder", "categories": ["resources", "explosivematerials"], "priority": 720 },
{ "type": "Contains", "match": "Dynamite", "categories": ["resources", "explosivematerials"], "priority": 720 },
{ "type": "Contains", "match": "RocketTip", "categories": ["resources", "explosivematerials"], "priority": 720 }
```

**Add to `categoryFallbacks`:**
```json
"explosivematerials": "resources"
```

**Add to `aliases`:**
```json
"explosives-materials": "explosivematerials",
"boom": "explosivematerials"
```

**Container sign:** `[ms:explosivematerials]` or `[ms:boom]`
