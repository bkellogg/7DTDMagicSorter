using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace MagicSorter
{
    /// <summary>
    ///     Shortcut command: mss = ms sort
    /// </summary>
    [SuppressMessage("ReSharper", "UnusedType.Global")]
    public class ConsoleCmdMss : ConsoleCmdAbstract
    {
        public override string[] getCommands()
        {
            return new[] { "mss" };
        }

        public override string getDescription()
        {
            return "Magic sort: sort items. Usage: mss [range]";
        }

        public override int DefaultPermissionLevel
        {
            get { return 1000; }
        }

        public override void Execute(List<string> args, CommandSenderInfo senderInfo)
        {
            var newArgs = new List<string> { "sort" };
            newArgs.AddRange(args);
            ConsoleCmdShortcutHelper.ExecuteMainCommand(newArgs, senderInfo);
        }
    }

    /// <summary>
    ///     Shortcut command: msp = ms plan
    /// </summary>
    [SuppressMessage("ReSharper", "UnusedType.Global")]
    public class ConsoleCmdMsp : ConsoleCmdAbstract
    {
        public override string[] getCommands()
        {
            return new[] { "msp" };
        }

        public override string getDescription()
        {
            return "Magic sort: plan (dry run). Usage: msp [range]";
        }

        public override int DefaultPermissionLevel
        {
            get { return 1000; }
        }

        public override void Execute(List<string> args, CommandSenderInfo senderInfo)
        {
            var newArgs = new List<string> { "plan" };
            newArgs.AddRange(args);
            ConsoleCmdShortcutHelper.ExecuteMainCommand(newArgs, senderInfo);
        }
    }

    /// <summary>
    ///     Shortcut command: msl = ms list
    /// </summary>
    [SuppressMessage("ReSharper", "UnusedType.Global")]
    public class ConsoleCmdMsl : ConsoleCmdAbstract
    {
        public override string[] getCommands()
        {
            return new[] { "msl" };
        }

        public override string getDescription()
        {
            return "Magic sort: list containers. Usage: msl [range]";
        }

        public override int DefaultPermissionLevel
        {
            get { return 1000; }
        }

        public override void Execute(List<string> args, CommandSenderInfo senderInfo)
        {
            var newArgs = new List<string> { "list" };
            newArgs.AddRange(args);
            ConsoleCmdShortcutHelper.ExecuteMainCommand(newArgs, senderInfo);
        }
    }

    /// <summary>
    ///     Shortcut command: msr = ms resort
    /// </summary>
    [SuppressMessage("ReSharper", "UnusedType.Global")]
    public class ConsoleCmdMsr : ConsoleCmdAbstract
    {
        public override string[] getCommands()
        {
            return new[] { "msr" };
        }

        public override string getDescription()
        {
            return "Magic sort: resort items in [ms:X] containers. Usage: msr [range]";
        }

        public override int DefaultPermissionLevel
        {
            get { return 1000; }
        }

        public override void Execute(List<string> args, CommandSenderInfo senderInfo)
        {
            var newArgs = new List<string> { "resort" };
            newArgs.AddRange(args);
            ConsoleCmdShortcutHelper.ExecuteMainCommand(newArgs, senderInfo);
        }
    }

    /// <summary>
    ///     Shortcut command: msm = ms missing
    /// </summary>
    [SuppressMessage("ReSharper", "UnusedType.Global")]
    public class ConsoleCmdMsm : ConsoleCmdAbstract
    {
        public override string[] getCommands()
        {
            return new[] { "msm" };
        }

        public override string getDescription()
        {
            return "Magic sort: show missing containers. Usage: msm [range]";
        }

        public override int DefaultPermissionLevel
        {
            get { return 1000; }
        }

        public override void Execute(List<string> args, CommandSenderInfo senderInfo)
        {
            var newArgs = new List<string> { "missing" };
            newArgs.AddRange(args);
            ConsoleCmdShortcutHelper.ExecuteMainCommand(newArgs, senderInfo);
        }
    }

    /// <summary>
    ///     Helper class to execute the main MagicSort command
    /// </summary>
    internal static class ConsoleCmdShortcutHelper
    {
        private static ConsoleCmdMagicSort _mainCommand;

        public static void ExecuteMainCommand(List<string> args, CommandSenderInfo senderInfo)
        {
            if (_mainCommand == null)
            {
                _mainCommand = new ConsoleCmdMagicSort();
            }
            _mainCommand.Execute(args, senderInfo);
        }
    }
}
