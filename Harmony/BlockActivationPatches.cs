using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
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
            if (tileEntity == null) return null;

            if (tileEntity is TileEntitySecureLootContainerSigned signedContainer)
            {
                return signedContainer.signText?.Text;
            }

            if (tileEntity is TileEntityComposite composite)
            {
                return GetCompositeSignText(composite);
            }

            return null;
        }

        private static string GetCompositeSignText(TileEntityComposite composite)
        {
            const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

            try
            {
                var type = composite.GetType();
                var modulesField = type.GetField("modulesCustomOrder", flags);

                if (modulesField == null) return null;
                if (!(modulesField.GetValue(composite) is Array modules)) return null;

                foreach (var module in modules)
                {
                    if (module?.GetType().Name.Contains("Signable") != true)
                        continue;

                    var signTextField = module.GetType().GetField("signText", flags);
                    var signTextValue = signTextField?.GetValue(module);

                    if (signTextValue == null) continue;

                    var textProp = signTextValue.GetType().GetProperty("Text");
                    if (textProp?.GetValue(signTextValue) is string text)
                        return text;
                }
            }
            catch (Exception ex)
            {
                Log.Warning($"[MagicSorter] Error getting composite sign text: {ex.Message}");
            }

            return null;
        }
    }
}
