# Adding Custom Categories to MagicSorter

This guide explains how to add your own item categories to MagicSorter by editing the `mappings.json` file.

## Before You Start

- The `mappings.json` file is located in your MagicSorter mod folder
- Use a text editor like Notepad++ or VS Code (regular Notepad works but is harder to read)
- **Make a backup** of `mappings.json` before editing
- JSON is picky about commas and quotes - one mistake can break the whole file

## Quick Reference: Where Things Go

| What you want to do | Section to edit |
|---------------------|-----------------|
| Define a new category name | `categories` |
| Match items by their internal name | `patterns` |
| Map a specific item directly | `items` |
| Add shorthand names for your category | `aliases` |
| Set where items go if your container doesn't exist | `categoryFallbacks` |

---

## Step-by-Step: Adding a New Category

Let's say you want to create a **"snacks"** category for chips, candy, and other junk food.

### Step 1: Define Your Category

Find the `"categories"` section (around line 256). Add your new category inside the curly braces.

**Find this section:**
```json
  "categories": {
    "weapons": {
      "specificity": 50,
      "description": "All weapons"
    },
```

**Add your category** (put it anywhere inside `categories`, just maintain the comma pattern):
```json
    "snacks": {
      "specificity": 90,
      "description": "Snack foods like chips and candy"
    },
```

**What the numbers mean:**
- `specificity`: How specific this category is (higher = more specific). Use 90 for specific categories, 50 for broad ones.

### Step 2: Create Patterns to Match Items

Patterns tell MagicSorter which items belong in your category. Find the `"patterns"` section at the top of the file.

**Add patterns that match your items:**
```json
    { "type": "Contains", "match": "Chips", "categories": ["food", "snacks"], "priority": 700 },
    { "type": "Contains", "match": "Candy", "categories": ["food", "snacks"], "priority": 700 },
    { "type": "Contains", "match": "Crackers", "categories": ["food", "snacks"], "priority": 700 },
```

**Pattern types:**
| Type | What it does | Example |
|------|--------------|---------|
| `Prefix` | Item name starts with this | `"Prefix": "food"` matches `foodCanChili` |
| `Contains` | Item name has this anywhere | `"Contains": "Chips"` matches `foodPotatoChips` |
| `Equals` | Item name is exactly this | `"Equals": "apple"` only matches `apple` |

**Tips:**
- `priority`: Higher numbers are checked first. Use 700-800 for most custom patterns.
- `categories`: Always include both your new category AND a parent (like `"food"`) so items have a fallback.
- Item names are the internal game names, not display names. Use `ms scan` in-game to see actual item names.

### Step 3: Set Up a Fallback

If someone doesn't have a `[ms:snacks]` container, where should snack items go? Find `"categoryFallbacks"` near the bottom of the file.

**Add your fallback:**
```json
    "snacks": "food",
```

This means: if there's no snacks container, put snacks in the food container.

### Step 4: Add Aliases (Optional)

Aliases let you use different names for your category on container signs. Find the `"aliases"` section.

**Add some aliases:**
```json
    "junkfood": "snacks",
    "chips": "snacks",
    "candy": "snacks",
```

Now you can label containers `[ms:junkfood]` or `[ms:chips]` and they'll work the same as `[ms:snacks]`.

---

## Adding Items Directly (Alternative to Patterns)

If a specific item doesn't fit any pattern, you can map it directly. Find the `"items"` section.

```json
    "foodSpecialSnack": [
      "food",
      "snacks"
    ],
```

This maps the exact item `foodSpecialSnack` to your categories.

---

## Common Mistakes

### Missing or Extra Commas
Every item needs a comma after it, EXCEPT the last one in a list.

**Wrong:**
```json
    "snacks": "food"
    "drinks": "food",
```

**Right:**
```json
    "snacks": "food",
    "drinks": "food"
```

### Mismatched Quotes or Braces
Every `"` needs a partner. Every `{` needs a `}`. Every `[` needs a `]`.

### Forgetting to Reload
After editing, save the file and run `ms reload` in the F1 console to load your changes. No need to restart the game.

---

## Testing Your Changes

1. Save your edited `mappings.json`
2. Open the F1 console and run `ms reload` to load your changes
3. Place a storage crate and rename it to `[ms:snacks]` (or whatever your category is)
4. Put some items in an input container (`[MagicSort]`)
5. Run these commands in the F1 console:
   - `ms list` - Check that your container shows up
   - `ms plan` - Preview where items will go
   - `ms sort` - Actually sort the items

**Tip:** You can edit mappings and run `ms reload` while playing - no need to restart the game. This makes it easy to test and tweak your categories.

If the game won't load or sorting doesn't work, check your JSON syntax. You can use an online JSON validator like [jsonlint.com](https://jsonlint.com) to find errors.

---

## Finding Item Names

To see what items are actually called internally:

1. Put items in an input container
2. Open F1 console
3. Run `ms scan`

This shows the internal item names that patterns match against.

---

## Example: Complete "Snacks" Category

Here's everything you'd add for a complete snacks category:

**In `categories`:**
```json
    "snacks": {
      "specificity": 90,
      "description": "Snack foods like chips and candy"
    },
```

**In `patterns`:**
```json
    { "type": "Contains", "match": "Chips", "categories": ["food", "snacks"], "priority": 700 },
    { "type": "Contains", "match": "Candy", "categories": ["food", "snacks"], "priority": 700 },
```

**In `categoryFallbacks`:**
```json
    "snacks": "food",
```

**In `aliases`:**
```json
    "junkfood": "snacks",
```

**Container sign:** `[ms:snacks]`
