# LLM Guide: Editing MagicSorter mappings.json

This document provides instructions for AI assistants (Claude, GPT, Cursor, etc.) to help users modify the `mappings.json` file for the MagicSorter mod in 7 Days to Die.

## File Location

The file is located at: `Mods/MagicSorter/mappings.json`

After editing, the user should run `ms reload` in the game's F1 console to apply changes without restarting.

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
