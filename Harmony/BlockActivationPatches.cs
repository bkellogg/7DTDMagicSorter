using System;
using System.Diagnostics.CodeAnalysis;
using HarmonyLib;

namespace MagicSorter.Harmony
{
    /// <summary>
    ///     Harmony patches for adding "Sort Items" to container activation menus
    /// </summary>
    [SuppressMessage("ReSharper", "UnusedType.Global")]
    [SuppressMessage("ReSharper", "UnusedMember.Local")]
    public static class BlockActivationPatches
    {
        private const string MagicSortTag = "[MagicSort]";
        private const string CommandId = "magicsort";
        private const string CommandIcon = "sort";

        /// <summary>
        ///     Patch BlockCompositeTileEntity.GetBlockActivationCommands (writable storage crates)
        /// </summary>
        [HarmonyPatch(typeof(BlockCompositeTileEntity), "GetBlockActivationCommands")]
        public static class CompositeGetCommandsPatch
        {
            [SuppressMessage("ReSharper", "InconsistentNaming")]
            static void Postfix(ref BlockActivationCommand[] __result,
                WorldBase _world, BlockValue _blockValue, int _clrIdx, Vector3i _blockPos,
                EntityAlive _entityFocusing)
            {
                try
                {
                    var signText = GetContainerSignText(_world, _clrIdx, _blockPos);

                    if (string.IsNullOrEmpty(signText)) return;

                    if (signText.IndexOf(MagicSortTag, StringComparison.OrdinalIgnoreCase) < 0)
                        return;

                    __result = AddSortCommand(__result);
                }
                catch (Exception ex)
                {
                    Log.Warning($"[MagicSorter] Error in CompositeGetCommandsPatch: {ex.Message}");
                }
            }
        }

        /// <summary>
        ///     Patch BlockCompositeTileEntity.OnBlockActivated
        /// </summary>
        [HarmonyPatch(typeof(BlockCompositeTileEntity), "OnBlockActivated",
            new[] { typeof(string), typeof(WorldBase), typeof(int), typeof(Vector3i),
                    typeof(BlockValue), typeof(EntityPlayerLocal) })]
        public static class CompositeOnActivatedPatch
        {
            [SuppressMessage("ReSharper", "InconsistentNaming")]
            static bool Prefix(string _commandName, EntityPlayerLocal _player, ref bool __result)
            {
                if (_commandName != CommandId)
                    return true;

                ExecuteSort(_player);
                __result = true;
                return false;
            }
        }

        /// <summary>
        ///     Patch BlockSecureLootSigned.GetBlockActivationCommands (signed secure containers)
        /// </summary>
        [HarmonyPatch(typeof(BlockSecureLootSigned), "GetBlockActivationCommands")]
        public static class SecureLootSignedGetCommandsPatch
        {
            [SuppressMessage("ReSharper", "InconsistentNaming")]
            static void Postfix(ref BlockActivationCommand[] __result,
                WorldBase _world, BlockValue _blockValue, int _clrIdx, Vector3i _blockPos,
                EntityAlive _entityFocusing)
            {
                try
                {
                    var signText = GetContainerSignText(_world, _clrIdx, _blockPos);

                    if (string.IsNullOrEmpty(signText)) return;

                    if (signText.IndexOf(MagicSortTag, StringComparison.OrdinalIgnoreCase) < 0)
                        return;

                    __result = AddSortCommand(__result);
                }
                catch (Exception ex)
                {
                    Log.Warning($"[MagicSorter] Error in SecureLootSignedGetCommandsPatch: {ex.Message}");
                }
            }
        }

        /// <summary>
        ///     Patch BlockSecureLootSigned.OnBlockActivated
        /// </summary>
        [HarmonyPatch(typeof(BlockSecureLootSigned), "OnBlockActivated",
            new[] { typeof(string), typeof(WorldBase), typeof(int), typeof(Vector3i),
                    typeof(BlockValue), typeof(EntityPlayerLocal) })]
        public static class SecureLootSignedOnActivatedPatch
        {
            [SuppressMessage("ReSharper", "InconsistentNaming")]
            static bool Prefix(string _commandName, EntityPlayerLocal _player, ref bool __result)
            {
                if (_commandName != CommandId)
                    return true;

                ExecuteSort(_player);
                __result = true;
                return false;
            }
        }

        private static BlockActivationCommand[] AddSortCommand(BlockActivationCommand[] commands)
        {
            if (commands == null)
                commands = new BlockActivationCommand[0];

            var newCommands = new BlockActivationCommand[commands.Length + 1];
            Array.Copy(commands, newCommands, commands.Length);
            newCommands[commands.Length] = new BlockActivationCommand(
                CommandId,
                CommandIcon,
                true
            );
            return newCommands;
        }

        private static void ExecuteSort(EntityPlayerLocal player)
        {
            try
            {
                var range = MagicSorterMod.Config?.DefaultRange ?? 20;
                var manager = new ContainerManager(player, range);
                manager.Sort();
                GameManager.ShowTooltip(player, "Items sorted!");
            }
            catch (Exception ex)
            {
                Log.Error($"[MagicSorter] Error executing sort: {ex.Message}");
                GameManager.ShowTooltip(player, "Sort failed - check console");
            }
        }

        private static string GetContainerSignText(WorldBase world, int clrIdx, Vector3i blockPos)
        {
            var tileEntity = world.GetTileEntity(clrIdx, blockPos);
            return SignTextHelper.GetSignText(tileEntity);
        }
    }

    /// <summary>
    ///     Harmony patches for adding "Sort Vehicle" to vehicle activation menus
    /// </summary>
    [SuppressMessage("ReSharper", "UnusedType.Global")]
    [SuppressMessage("ReSharper", "UnusedMember.Local")]
    public static class VehicleActivationPatches
    {
        private const string VehicleCommandId = "magicsortvehicle";
        private const string VehicleCommandIcon = "sort";

        /// <summary>
        ///     Tracks the vanilla command count so the OnEntityActivated prefix can identify
        ///     our custom command by index. Set in GetActivationCommands (postfix), read in
        ///     OnEntityActivated (prefix). Safe because both run on the main thread for the
        ///     same vehicle the player is looking at. Reset to -1 when our command is not added,
        ///     so it never accidentally matches a vanilla command index.
        /// </summary>
        private static int _lastVanillaCommandCount = -1;

        /// <summary>
        ///     Patch EntityVehicle.GetActivationCommands to append "Sort Vehicle"
        /// </summary>
        [HarmonyPatch(typeof(EntityVehicle), "GetActivationCommands")]
        public static class VehicleGetCommandsPatch
        {
            [SuppressMessage("ReSharper", "InconsistentNaming")]
            static void Postfix(ref EntityActivationCommand[] __result, EntityVehicle __instance,
                EntityAlive _entityFocusing)
            {
                try
                {
                    if (__result == null || __result.Length == 0)
                    {
                        _lastVanillaCommandCount = -1;
                        return;
                    }

                    if (__instance.GetVehicle() == null || !__instance.hasStorage())
                    {
                        _lastVanillaCommandCount = -1;
                        return;
                    }

                    var world = GameManager.Instance.World;
                    var range = MagicSorterMod.Config?.DefaultRange ?? 20;
                    if (!SignTextHelper.HasSortMeContainer(world, _entityFocusing.GetBlockPosition(), range))
                    {
                        _lastVanillaCommandCount = -1;
                        return;
                    }

                    _lastVanillaCommandCount = __result.Length;

                    var newCommands = new EntityActivationCommand[__result.Length + 1];
                    Array.Copy(__result, newCommands, __result.Length);
                    newCommands[__result.Length] = new EntityActivationCommand(
                        VehicleCommandId,
                        VehicleCommandIcon,
                        true
                    );
                    __result = newCommands;
                }
                catch (Exception ex)
                {
                    Log.Warning($"[MagicSorter] Error in VehicleGetCommandsPatch: {ex.Message}");
                }
            }
        }

        /// <summary>
        ///     Patch EntityVehicle.OnEntityActivated to handle our custom command
        /// </summary>
        [HarmonyPatch(typeof(EntityVehicle), "OnEntityActivated")]
        public static class VehicleOnActivatedPatch
        {
            [SuppressMessage("ReSharper", "InconsistentNaming")]
            static bool Prefix(int _indexInBlockActivationCommands, EntityAlive _entityFocusing,
                EntityVehicle __instance, ref bool __result)
            {
                try
                {
                    if (_indexInBlockActivationCommands != _lastVanillaCommandCount)
                        return true;

                    var player = _entityFocusing as EntityPlayerLocal;
                    if (player == null)
                        return true;

                    if (player.inventory.IsHoldingItemActionRunning())
                    {
                        __result = false;
                        return false;
                    }

                    ExecuteSortVehicle(player, __instance);
                    __result = false;
                    return false;
                }
                catch (Exception ex)
                {
                    Log.Error($"[MagicSorter] Error in VehicleOnActivatedPatch: {ex.Message}");
                    return true;
                }
            }
        }

        private static void ExecuteSortVehicle(EntityPlayerLocal player, EntityVehicle vehicle)
        {
            try
            {
                var range = MagicSorterMod.Config?.DefaultRange ?? 20;
                var manager = new ContainerManager(player, range);
                manager.SortVehicle(vehicle);
                GameManager.ShowTooltip(player, "Vehicle items sorted!");
            }
            catch (Exception ex)
            {
                Log.Error($"[MagicSorter] Error executing vehicle sort: {ex.Message}");
                GameManager.ShowTooltip(player, "Vehicle sort failed - check console");
            }
        }
    }
}
