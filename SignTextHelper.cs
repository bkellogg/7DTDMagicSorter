using System;
using System.Reflection;

namespace MagicSorter
{
    /// <summary>
    ///     Shared utility for reading sign text from tile entities.
    ///     Used by both Harmony patches and ContainerManager.
    /// </summary>
    public static class SignTextHelper
    {
        private const BindingFlags InstanceFlags =
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

        private const string MagicSortTag = "[MagicSort]";

        /// <summary>
        ///     Gets the sign text from any supported tile entity type.
        /// </summary>
        public static string GetSignText(TileEntity tileEntity)
        {
            if (tileEntity is TileEntitySecureLootContainerSigned signed)
                return signed.signText?.Text;

            if (tileEntity is TileEntityComposite composite)
                return GetCompositeSignText(composite);

            return null;
        }

        /// <summary>
        ///     Gets the sign text from a TileEntityComposite via reflection on its Signable module.
        /// </summary>
        public static string GetCompositeSignText(TileEntityComposite composite)
        {
            try
            {
                var modulesField = composite.GetType().GetField("modulesCustomOrder", InstanceFlags);
                if (modulesField == null) return null;
                if (!(modulesField.GetValue(composite) is Array modules)) return null;

                foreach (var module in modules)
                {
                    if (module?.GetType().Name.Contains("Signable") != true)
                        continue;

                    var signTextField = module.GetType().GetField("signText", InstanceFlags);
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

        /// <summary>
        ///     Checks whether a [MagicSort] container exists within range of the given position.
        ///     Early-returns on first match for efficiency.
        /// </summary>
        public static bool HasSortMeContainer(World world, Vector3i center, int range)
        {
            if (world == null) return false;

            for (var x = -range; x <= range; x++)
            for (var y = -range; y <= range; y++)
            for (var z = -range; z <= range; z++)
            {
                var pos = new Vector3i(center.x + x, center.y + y, center.z + z);
                var tileEntity = world.GetTileEntity(0, pos);
                if (tileEntity == null) continue;

                var signText = GetSignText(tileEntity);
                if (signText != null &&
                    signText.IndexOf(MagicSortTag, StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            }

            return false;
        }
    }
}
