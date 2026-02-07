using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace MagicSorter
{
    /// <summary>
    ///     Console command handler. Instantiated by the game engine.
    /// </summary>
    [SuppressMessage("ReSharper", "UnusedType.Global")]
    public class ConsoleCmdMagicSort : ConsoleCmdAbstract
    {
        public override string[] getCommands()
        {
            return new[] { "magicsort", "ms" };
        }

        public override string getDescription()
        {
            return "Magic sort manager. Usage: ms <sort|list|plan|config|mappings> [range]";
        }

        public override int DefaultPermissionLevel
        {
            get { return 1000; } // 0 = admin only, 1000 = any player
        }

        private static void Output(string message)
        {
            MagicSorterMod.Output(message);
        }

        public override void Execute(List<string> args, CommandSenderInfo senderInfo)
        {
            if (args.Count == 0)
            {
                ShowHelp();
                return;
            }

            var subcommand = args[0].ToLower();

            // Handle commands that don't need a player
            switch (subcommand)
            {
                case "config":
                    ShowConfig();
                    return;
                case "mappings":
                    ShowMappings();
                    return;
                case "version":
                    Output($"[MagicSorter] Version {MagicSorterMod.Version}");
                    return;
                case "reload":
                    ReloadMappings();
                    return;
            }

            // Get default range from config
            var defaultRange = MagicSorterMod.Config?.DefaultRange ?? 20;
            var range = defaultRange;

            // Parse optional range argument
            if (args.Count > 1)
                if (!int.TryParse(args[1], out range) || range <= 0)
                {
                    Output("[MagicSorter] Invalid range. Usage: ms <sort|list|plan> [range]");
                    return;
                }

            // Get the player who ran the command
            var player = GetPlayerFromSender(senderInfo);
            if (player == null)
            {
                Output("[MagicSorter] Could not find player. This command must be run by a player.");
                return;
            }

            var manager = new ContainerManager(player, range);

            switch (subcommand)
            {
                case "sort":
                    manager.Sort();
                    break;
                case "list":
                    manager.ListContainers();
                    break;
                case "plan":
                    manager.Plan();
                    break;
                case "scan":
                    manager.Scan();
                    break;
                case "missing":
                    manager.Missing();
                    break;
                case "invalid":
                    manager.Invalid();
                    break;
                case "suggest":
                    manager.Suggest();
                    break;
                case "debug":
                    manager.DebugItems();
                    break;
                case "resort":
                    manager.Resort();
                    break;
                default:
                    ShowHelp();
                    break;
            }
        }

        private static void ShowConfig()
        {
            var config = MagicSorterMod.Config;
            if (config == null)
            {
                Output("[MagicSorter] No configuration loaded");
                return;
            }

            Output("[MagicSorter] Current configuration:");
            Output($"  FallbackToBuiltIn: {config.FallbackToBuiltIn}");
            Output($"  UseSpecificityResolution: {config.UseSpecificityResolution}");
            Output($"  DefaultRange: {config.DefaultRange}");
            Output($"  DebugLogging: {config.DebugLogging}");
        }

        private static void ShowMappings()
        {
            var loader = MagicSorterMod.MappingLoader;
            if (loader == null)
            {
                Output("[MagicSorter] Mapping loader not initialized");
                return;
            }

            Output($"[MagicSorter] Mappings status: {loader.GetStatus()}");
            if (!loader.IsInitialized)
            {
                return;
            }

            var mappings = loader.GetMappings();
            if (mappings == null)
            {
                return;
            }

            Output($"  Aliases defined: {mappings.ContainerAliases.Count}");
            Output($"  Tags defined: {mappings.Tags.Count}");
        }

        private static void ReloadMappings()
        {
            var loader = MagicSorterMod.MappingLoader;
            if (loader == null)
            {
                Output("[MagicSorter] Mapping loader not initialized");
                return;
            }

            if (loader.Reload())
            {
                // Reinitialize the resolver with new mappings
                MagicSorterMod.ReinitializeResolver();
                Output("[MagicSorter] Mappings reloaded successfully");
                Output($"[MagicSorter] {loader.GetStatus()}");
            }
            else
            {
                Output("[MagicSorter] Failed to reload mappings");
            }
        }

        private static void ShowHelp()
        {
            var defaultRange = MagicSorterMod.Config?.DefaultRange ?? 20;
            Output("[MagicSorter] Usage: ms <command> [range]");
            Output("  sort     - Sort items from [MagicSort] into [ms:X] containers");
            Output("  resort   - Re-sort all items already in [ms:X] containers");
            Output("  list     - List all recognized containers in range");
            Output("  plan     - Show what items would be sorted where (dry run)");
            Output("  scan     - Show items in [MagicSort] grouped by category");
            Output("  missing  - Show categories that need containers");
            Output("  suggest  - Show unsortable items and suggested containers");
            Output("  invalid  - Show containers with invalid/unknown labels");
            Output("  debug    - Show internal item names for mapping");
            Output("  config   - Show current configuration");
            Output("  mappings - Show loaded mappings status");
            Output("  reload   - Reload mappings from disk");
            Output("  version  - Show mod version");
            Output($"  [range]  - Optional search radius (default: {defaultRange})");
        }

        private static EntityPlayer GetPlayerFromSender(CommandSenderInfo senderInfo)
        {
            var world = GameManager.Instance.World;
            if (world == null)
                return null;

            // Remote client (multiplayer) - get player by entity ID
            if (senderInfo.RemoteClientInfo != null)
            {
                var entity = world.GetEntity(senderInfo.RemoteClientInfo.entityId);
                if (entity is EntityPlayer remotePlayer)
                    return remotePlayer;
            }

            // Local player (single player or host)
            var primaryPlayer = world.GetPrimaryPlayer();
            if (primaryPlayer != null)
                return primaryPlayer;

            // Fallback: first available player (dedicated server edge case)
            var players = world.Players?.list;
            if (players == null || players.Count == 0)
            {
                return null;
            }

            Log.Warning("[MagicSorter] Could not identify specific player, using first available");
            return players[0];
        }
    }
}
