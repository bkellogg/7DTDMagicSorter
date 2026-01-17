using System;
using System.IO;
using MagicSorter.Models;
using Newtonsoft.Json;

namespace MagicSorter.Services
{
    /// <summary>
    ///     Handles loading of category mappings from local file
    /// </summary>
    public class MappingLoader
    {
        private const string LocalMappingsFileName = "mappings.json";
        private readonly string _modPath;
        private MappingData _currentMappings;

        public MappingLoader(string modPath)
        {
            _modPath = modPath;
            _currentMappings = new MappingData();
        }

        /// <summary>
        ///     Returns true if mappings have been loaded
        /// </summary>
        public bool IsInitialized { get; private set; }

        private int ItemCount => _currentMappings?.Items?.Count ?? 0;

        private int CategoryCount => _currentMappings?.Categories?.Count ?? 0;

        private string Version => _currentMappings?.Version ?? "unknown";

        /// <summary>
        ///     Gets the currently loaded mappings
        /// </summary>
        public MappingData GetMappings()
        {
            return _currentMappings;
        }

        /// <summary>
        ///     Initializes mappings from local file
        /// </summary>
        public void Initialize()
        {
            if (TryLoadLocalMappings())
            {
                IsInitialized = true;
                Log.Out(
                    $"[MagicSorter] Loaded local mappings (v{Version}, {CategoryCount} categories, {ItemCount} items)");
            }
            else
            {
                Log.Out("[MagicSorter] No mappings loaded - will use built-in Groups fallback");
                IsInitialized = true;
            }
        }

        private bool TryLoadLocalMappings()
        {
            var localPath = Path.Combine(_modPath, LocalMappingsFileName);
            if (!File.Exists(localPath)) return false;

            try
            {
                var json = File.ReadAllText(localPath);
                var mappings = JsonConvert.DeserializeObject<MappingData>(json);

                if (mappings == null || (mappings.Categories.Count == 0 && mappings.Items.Count == 0))
                    return false;

                mappings.NormalizeDictionaries();
                _currentMappings = mappings;

                return true;
            }
            catch (Exception ex)
            {
                Log.Warning($"[MagicSorter] Failed to load local mappings: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        ///     Gets status information about the mappings
        /// </summary>
        public string GetStatus()
        {
            return $"Version: {Version}, Categories: {CategoryCount}, Items: {ItemCount}";
        }
    }
}
