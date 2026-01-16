using System.Collections.Generic;

namespace MagicSorter
{
    public class ConsoleCmdMagicSort : ConsoleCmdAbstract
    {
        public override string[] getCommands()
        {
            return new[] { "magicsort", "ms" };
        }

        public override string getDescription()
        {
            return "Magic item manager. Usage: ms <sort|list|preview|reload|config|mappings> [range]";
        }

        public override void Execute(List<string> args, CommandSenderInfo senderInfo)
        {
            if (args.Count == 0)
            {
                ShowHelp();
                return;
            }

            string subcommand = args[0].ToLower();

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
            int defaultRange = MagicSorterMod.Config?.DefaultRange ?? 20;
            int range = defaultRange;

            // Parse optional range argument
            if (args.Count > 1)
            {
                if (!int.TryParse(args[1], out range) || range <= 0)
                {
                    Log.Error("[MagicSorter] Invalid range. Usage: ms <sort|list|preview> [range]");
                    return;
                }
            }

            // Get the player who ran the command
            EntityPlayer player = GetPlayerFromSender(senderInfo);
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
                case "preview":
                    manager.Preview();
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

        private void ReloadMappings()
        {
            Log.Out("[MagicSorter] Reloading mappings...");

            var loader = MagicSorterMod.MappingLoader;
            if (loader == null)
            {
                Log.Error("[MagicSorter] Mapping loader not initialized");
                return;
            }

            if (loader.ForceRefresh())
            {
                Log.Out("[MagicSorter] Mappings reloaded successfully");
            }
            else
            {
                Log.Warning("[MagicSorter] Failed to reload mappings - using existing");
            }
        }

        private void ShowConfig()
        {
            var config = MagicSorterMod.Config;
            if (config == null)
            {
                Log.Out("[MagicSorter] No configuration loaded");
                return;
            }

            Log.Out("[MagicSorter] Current configuration:");
            Log.Out($"  RemoteMappingsUrl: {(string.IsNullOrEmpty(config.RemoteMappingsUrl) ? "(not set)" : config.RemoteMappingsUrl)}");
            Log.Out($"  CacheDurationHours: {config.CacheDurationHours}");
            Log.Out($"  FallbackToBuiltIn: {config.FallbackToBuiltIn}");
            Log.Out($"  UseSpecificityResolution: {config.UseSpecificityResolution}");
            Log.Out($"  DefaultRange: {config.DefaultRange}");
            Log.Out($"  DebugLogging: {config.DebugLogging}");
            Log.Out($"  ConnectionTimeoutSeconds: {config.ConnectionTimeoutSeconds}");
        }

        private void ShowMappings()
        {
            var loader = MagicSorterMod.MappingLoader;
            if (loader == null)
            {
                Log.Out("[MagicSorter] Mapping loader not initialized");
                return;
            }

            Log.Out($"[MagicSorter] Mappings status: {loader.GetStatus()}");

            if (loader.IsInitialized)
            {
                var mappings = loader.GetMappings();
                if (mappings != null)
                {
                    Log.Out($"  Aliases defined: {mappings.ContainerAliases.Count}");
                    Log.Out($"  Tags defined: {mappings.Tags.Count}");
                }
            }
        }

        private void ShowHelp()
        {
            int defaultRange = MagicSorterMod.Config?.DefaultRange ?? 20;
            Log.Out("[MagicSorter] Usage: ms <command> [range]");
            Log.Out("  sort     - Sort items from [SortMe] into [Sort:X] containers");
            Log.Out("  list     - List all recognized containers in range");
            Log.Out("  preview  - Show what items would be sorted where (dry run)");
            Log.Out("  scan     - Show items in [SortMe] grouped by category");
            Log.Out("  missing  - Show categories that need containers");
            Log.Out("  suggest  - Show unsortable items and suggested containers");
            Log.Out("  invalid  - Show containers with invalid/unknown labels");
            Log.Out("  debug    - Show internal item names for mapping");
            Log.Out("  reload   - Force reload mappings from remote URL");
            Log.Out("  config   - Show current configuration");
            Log.Out("  mappings - Show loaded mappings status");
            Log.Out($"  [range]  - Optional search radius (default: {defaultRange})");
        }

        private EntityPlayer GetPlayerFromSender(CommandSenderInfo senderInfo)
        {
            // Remote client (multiplayer)
            if (senderInfo.RemoteClientInfo != null)
            {
                var entity = GameManager.Instance.World?.GetEntity(senderInfo.RemoteClientInfo.entityId);
                if (entity is EntityPlayer remotePlayer)
                {
                    return remotePlayer;
                }
            }

            // Try to get player by client info's player ID
            if (senderInfo.RemoteClientInfo != null)
            {
                var players = GameManager.Instance.World?.Players?.list;
                if (players != null)
                {
                    foreach (var player in players)
                    {
                        if (player.entityId == senderInfo.RemoteClientInfo.entityId)
                        {
                            return player;
                        }
                    }
                }
            }

            // Local player (single player or host playing on their own server)
            var primaryPlayer = GameManager.Instance.World?.GetPrimaryPlayer();
            if (primaryPlayer != null)
            {
                return primaryPlayer;
            }

            // Last resort: find any player in the world (dedicated server with only one player)
            var allPlayers = GameManager.Instance.World?.Players?.list;
            if (allPlayers != null && allPlayers.Count > 0)
            {
                // This shouldn't normally happen, but provides a fallback
                Log.Warning("[MagicSorter] Could not identify specific player, using first available player");
                return allPlayers[0];
            }

            return null;
        }
    }
}
