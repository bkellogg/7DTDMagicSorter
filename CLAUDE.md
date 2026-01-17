# MagicSorter - 7 Days to Die Mod

## Build Command
```powershell
powershell -Command "& 'C:\Program Files\JetBrains\JetBrains Rider 2025.3.1\tools\MSBuild\Current\Bin\MSBuild.exe' 'C:\Users\brend\Documents\projects\7DTDMagicSorter\7DTDMagicSorter.csproj' '/p:Configuration=Debug'"
```

The build automatically deploys the mod to the game folder via the DeployMod target in the csproj.

## Game Logs Location
Logs are visible in-game via the F1 console. The game also writes logs to:
- `C:\Users\brend\AppData\Roaming\7DaysToDie\logs`
- Look for `[MagicSorter]` prefix in the logs

### Reading Logs via PowerShell
Find the latest log file:
```powershell
powershell -Command "Get-ChildItem 'C:\\Users\\brend\\AppData\\Roaming\\7DaysToDie\\logs' -Filter '*.txt' | Sort-Object LastWriteTime -Descending | Select-Object -First 1 -ExpandProperty FullName"
```

Filter for MagicSorter entries and errors (with 2 lines of context):
```powershell
powershell -Command "Get-Content 'C:\\Users\\brend\\AppData\\Roaming\\7DaysToDie\\logs\\<LOG_FILE>.txt' | Select-String -Pattern 'MagicSorter|Exception|Error' -Context 2,2 | Select-Object -First 50"
```
## Coding Style

### General
- Prefer guard style if clauses that exit early to reduce nesting

### C# Version Constraints
- Project targets .NET Framework 4.8 which uses C# 7.3
- Do NOT use C# 8.0+ features:
  - `is not` pattern → use `!(x is Y)`
  - `is { }` pattern → use `!= null`
  - Null-coalescing assignment `??=`
  - Switch expressions

### Pattern Matching in CategoryResolver
- Order matters: more specific patterns must come BEFORE general ones
- Example: check for "Schematic" before "planted" (plantedGraceCorn1Schematic is a schematic, not farming)
- When adding new patterns, consider items that might match multiple patterns

### JSON Deserialization (Newtonsoft.Json)
- Properties that come from JSON need `{ get; set; }` (not readonly)
- Use `[JsonProperty("name")]` for different JSON key names
- Call `NormalizeDictionaries()` after deserialization for case-insensitive lookups

### Game Engine Integration
- Classes instantiated by the game (IModApi, ConsoleCmdAbstract) need:
  ```csharp
  [SuppressMessage("ReSharper", "UnusedType.Global")]
  ```
- Classes instantiated by JSON deserializer need similar suppression for "ClassNeverInstantiated"

## Project Structure
- `MagicSorterMod.cs` - IModApi entry point (auto-discovered by game)
- `ConsoleCmdMagicSort.cs` - Console command `magicsort` or `ms` with subcommands (sort, list, preview)
- `ContainerManager.cs` - Main sorting logic
- `ContainerWrapper.cs` - Abstraction for different container types

## Key Technical Details

### Container Types
- `TileEntityLootContainer` - Basic loot containers, use `GetItems()` directly
- `TileEntityComposite` - Writable storage crates (the ones players place)
  - Has modules: TEFeatureStorage, TEFeatureLockable, TEFeatureSignable
  - Sign text is in `TEFeatureSignable.signText` (AuthoredText type)
  - Items accessed via reflection on TEFeatureStorage module

### Testing
1. Build the mod (command above)
2. Launch 7 Days to Die
3. Load into a game world
4. Place storage crates and rename them with signs: `[MagicSort]` for input, `[ms:CategoryName]` for output
5. Open F1 console and run:
   - `ms list` - List containers found
   - `ms plan` - Preview what would be sorted where
   - `ms sort` - Actually sort items
6. Check F1 console for `[MagicSorter]` log output

## Dependencies
Referenced from `C:\Program Files (x86)\Steam\steamapps\common\7 Days To Die\7DaysToDie_Data\Managed\`:
- Assembly-CSharp.dll (game code)
- LogLibrary.dll (Log.Out, Log.Error)
- UnityEngine.*.dll (Unity engine)
