using System.Collections.Generic;

namespace MagicSorter
{
    public class ConsoleCmdSortMagicBox : ConsoleCmdAbstract
    {
        private const int DefaultRange = 20;

        public override string[] getCommands()
        {
            return new[] { "sortmagicbox", "smb" };
        }

        public override string getDescription()
        {
            return "Sorts items from [SortMe] container into [Sort:X] containers. Usage: smb [range]";
        }

        public override void Execute(List<string> args, CommandSenderInfo senderInfo)
        {
            // Parse range argument
            int range = DefaultRange;
            if (args.Count > 0)
            {
                if (!int.TryParse(args[0], out range) || range <= 0)
                {
                    Log.Error("[MagicSorter] Invalid range. Usage: smb [range]");
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

            // Run the sorter
            var sorter = new ContainerSorter(player, range);
            sorter.Execute();
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
