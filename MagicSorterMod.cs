using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using HarmonyLib;
using MagicSorter.Models;
using MagicSorter.Services;

namespace MagicSorter
{
    /// <summary>
    ///     Main mod entry point. Instantiated by the game engine.
    /// </summary>
    [SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
    public class MagicSorterMod : IModApi
    {
        public const string Version = "0.3.0";

        /// <summary>
        ///     Path to the mod folder
        /// </summary>
        private static string ModPath { get; set; }

        /// <summary>
        ///     Global configuration instance
        /// </summary>
        public static ModConfiguration Config { get; private set; }

        /// <summary>
        ///     Global mapping loader instance
        /// </summary>
        public static MappingLoader MappingLoader { get; private set; }

        /// <summary>
        ///     Global category resolver instance
        /// </summary>
        public static CategoryResolver Resolver { get; private set; }

        /// <summary>
        ///     Outputs message to both the console (for multiplayer clients) and the log
        /// </summary>
        public static void Output(string message)
        {
            // Output to console (visible to player who ran command in multiplayer)
            SingletonMonoBehaviour<SdtdConsole>.Instance.Output(message);
            // Also log for server-side debugging
            Log.Out(message);
        }

        public void InitMod(Mod modInstance)
        {
            // Store mod path for config/cache access
            ModPath = modInstance.Path;

            // Load configuration
            Config = ConfigurationLoader.Load(ModPath);

            // Initialize mapping loader
            MappingLoader = new MappingLoader(ModPath);

            // Initialize category resolver
            Resolver = new CategoryResolver(MappingLoader, Config);

            // Load mappings from local file
            MappingLoader.Initialize();

            // Apply Harmony patches
            ApplyHarmonyPatches();

            Log.Out("[MagicSorter] Mod loaded successfully.");
        }

        private void ApplyHarmonyPatches()
        {
            try
            {
                var harmony = new HarmonyLib.Harmony("com.magicsorter.patches");
                harmony.PatchAll(Assembly.GetExecutingAssembly());

                // Log what was patched
                var patchedMethods = harmony.GetPatchedMethods();
                foreach (var method in patchedMethods)
                {
                    Log.Out($"[MagicSorter] Patched: {method.DeclaringType?.Name}.{method.Name}");
                }

                Log.Out("[MagicSorter] Harmony patches applied successfully.");
            }
            catch (Exception ex)
            {
                Log.Error($"[MagicSorter] Failed to apply Harmony patches: {ex.Message}");
            }
        }
    }
}