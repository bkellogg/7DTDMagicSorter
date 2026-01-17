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
            return "Magic sort manager. Usage: ms <sort|list|plan|reload|config|mappings> [range]";
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
                case "reload":
                    ReloadMappings();
                    return;
                case "config":
                    ShowConfig();
                    return;
                case "mappings":
                    ShowMappings();
                    return;
            }

            // Get default range from config
            var defaultRange = MagicSorterMod.Config?.DefaultRange ?? 20;
            var range = defaultRange;

            // Parse optional range argument
            if (args.Count > 1)
                if (!int.TryParse(args[1], out range) || range <= 0)
                {
                    Log.Error("[MagicSorter] Invalid range. Usage: ms <sort|list|plan> [range]");
                    return;
                }

            // Get the player who ran the command
            var player = GetPlayerFromSender(senderInfo);
            if (player == null)
            {
                Log.Error("[MagicSorter] Could not find player. This command must be run by a player.");
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
                default:
                    ShowHelp();
                    break;
            }
        }

        private static void ReloadMappings()
        {
            Log.Out("[MagicSorter] Reloading mappings...");

            var loader = MagicSorterMod.MappingLoader;
            if (loader == null)
            {
                Log.Error("[MagicSorter] Mapping loader not initialized");
                return;
            }

            if (loader.ForceRefresh())
                Log.Out("[MagicSorter] Mappings reloaded successfully");
            else
                Log.Warning("[MagicSorter] Failed to reload mappings - using existing");
        }

        private static void ShowConfig()
        {
            var config = MagicSorterMod.Config;
            if (config == null)
            {
                Log.Out("[MagicSorter] No configuration loaded");
                return;
            }

            Log.Out("[MagicSorter] Current configuration:");
            Log.Out(
                $"  RemoteMappingsUrl: {(string.IsNullOrEmpty(config.RemoteMappingsUrl) ? "(not set)" : config.RemoteMappingsUrl)}");
            Log.Out($"  CacheDurationHours: {config.CacheDurationHours}");
            Log.Out($"  FallbackToBuiltIn: {config.FallbackToBuiltIn}");
            Log.Out($"  UseSpecificityResolution: {config.UseSpecificityResolution}");
            Log.Out($"  DefaultRange: {config.DefaultRange}");
            Log.Out($"  DebugLogging: {config.DebugLogging}");
            Log.Out($"  ConnectionTimeoutSeconds: {config.ConnectionTimeoutSeconds}");
        }

        private static void ShowMappings()
        {
            var loader = MagicSorterMod.MappingLoader;
            if (loader == null)
            {
                Log.Out("[MagicSorter] Mapping loader not initialized");
                return;
            }

            Log.Out($"[MagicSorter] Mappings status: {loader.GetStatus()}");
            if (!loader.IsInitialized)
            {
                return;
            }


            var mappings = loader.GetMappings();
            if (mappings == null)
            {
                return;
            }

            Log.Out($"  Aliases defined: {mappings.ContainerAliases.Count}");
            Log.Out($"  Tags defined: {mappings.Tags.Count}");
        }

        private static void ShowHelp()
        {
            var defaultRange = MagicSorterMod.Config?.DefaultRange ?? 20;
            Log.Out("[MagicSorter] Usage: ms <command> [range]");
            Log.Out("  sort     - Sort items from [MagicSort] into [ms:X] containers");
            Log.Out("  list     - List all recognized containers in range");
            Log.Out("  plan     - Show what items would be sorted where (dry run)");
            Log.Out("  scan     - Show items in [MagicSort] grouped by category");
            Log.Out("  missing  - Show categories that need containers");
            Log.Out("  suggest  - Show unsortable items and suggested containers");
            Log.Out("  invalid  - Show containers with invalid/unknown labels");
            Log.Out("  debug    - Show internal item names for mapping");
            Log.Out("  reload   - Force reload mappings from remote URL");
            Log.Out("  config   - Show current configuration");
            Log.Out("  mappings - Show loaded mappings status");
            Log.Out($"  [range]  - Optional search radius (default: {defaultRange})");
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