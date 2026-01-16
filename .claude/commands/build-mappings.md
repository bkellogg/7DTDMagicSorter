# Build Mappings

Combines the individual mapping files from `mappings/` folder into the main `mappings.json` file.

## Source Files

- `mappings/categories.json` - Category definitions with specificity values
- `mappings/items.json` - Item-to-category mappings
- `mappings/aliases.json` - Container label aliases
- `mappings/tags.json` - Tag definitions for grouping categories

## Output

- `mappings.json` - Combined runtime mapping file

## Instructions

1. Read all four source files from the `mappings/` folder
2. Combine them into a single JSON object with this structure:
   ```json
   {
     "version": "1.x.x",
     "categories": { ... from categories.json ... },
     "items": { ... from items.json ... },
     "aliases": { ... from aliases.json ... },
     "tags": { ... from tags.json ... }
   }
   ```
3. Increment the patch version number (e.g., 1.0.0 -> 1.0.1)
4. Write the combined JSON to `mappings.json` in the project root
5. Report the number of categories, items, and aliases in the output

## Usage

Run this command after editing any of the individual mapping files to regenerate the combined `mappings.json`.
