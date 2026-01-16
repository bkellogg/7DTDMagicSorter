using System.Collections.Generic;

namespace MagicSorter
{
    public class ConsoleCmdMagicSort : ConsoleCmdAbstract
    {
        private const int DefaultRange = 20;

        public override string[] getCommands()
        {
            return new[] { "magicsort", "ms" };
        }

        public override string getDescription()
        {
            return "Magic item sorter. Usage: ms <sort|list|preview> [range]";
        }

        public override void Execute(List<string> args, CommandSenderInfo senderInfo)
        {
            if (args.Count == 0)
            {
                ShowHelp();
                return;
            }

            string subcommand = args[0].ToLower();
            int range = DefaultRange;

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

            var sorter = new ContainerSorter(player, range);

            switch (subcommand)
            {
                case "sort":
                    sorter.Execute();
                    break;
                case "list":
                    sorter.ListContainers();
                    break;
                case "preview":
                    sorter.Preview();
                    break;
                default:
                    ShowHelp();
                    break;
            }
        }

        private void ShowHelp()
        {
            Log.Out("[MagicSorter] Usage: ms <command> [range]");
            Log.Out("  sort    - Sort items from [SortMe] into [Sort:X] containers");
            Log.Out("  list    - List all recognized containers in range");
            Log.Out("  preview - Show what items would be sorted where (dry run)");
            Log.Out("  [range] - Optional search radius (default: 20)");
        }

        private EntityPlayer GetPlayerFromSender(CommandSenderInfo senderInfo)
        {
            if (senderInfo.RemoteClientInfo != null)
            {
                return GameManager.Instance.World.GetEntity(senderInfo.RemoteClientInfo.entityId) as EntityPlayer;
            }

            // Local player (single player or host)
            return GameManager.Instance.World.GetPrimaryPlayer();
        }
    }
}
