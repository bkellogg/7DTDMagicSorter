using MagicSorter.Models;
using MagicSorter.Services;

namespace MagicSorter
{
    public class MagicSorterMod : IModApi
    {
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

        public void InitMod(Mod modInstance)
        {
            // Store mod path for config/cache access
            ModPath = modInstance.Path;

            // Load configuration
            Config = ConfigurationLoader.Load(ModPath);

            // Initialize mapping loader
            MappingLoader = new MappingLoader(ModPath, Config);

            // Initialize category resolver
            Resolver = new CategoryResolver(MappingLoader, Config);

            // Start loading mappings (local first, then async remote)
            MappingLoader.Initialize();

            Log.Out("[MagicSorter] Mod loaded successfully.");
        }
    }
}