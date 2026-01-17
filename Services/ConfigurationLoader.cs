using System;
using System.IO;
using System.Xml;
using MagicSorter.Models;

namespace MagicSorter.Services
{
    /// <summary>
    ///     Loads and parses mod configuration from XML file
    /// </summary>
    public static class ConfigurationLoader
    {
        private const string ConfigFileName = "MagicSorter.xml";

        /// <summary>
        ///     Loads configuration from the mod's Config folder
        /// </summary>
        /// <param name="modPath">Path to the mod folder</param>
        /// <returns>Loaded configuration or default if file not found/invalid</returns>
        public static ModConfiguration Load(string modPath)
        {
            var config = new ModConfiguration();
            var configPath = Path.Combine(modPath, "Config", ConfigFileName);

            if (!File.Exists(configPath))
            {
                Log.Out($"[MagicSorter] Config file not found at {configPath}, using defaults");
                return config;
            }

            try
            {
                var doc = new XmlDocument();
                doc.Load(configPath);

                var root = doc.DocumentElement;
                if (root == null || root.Name != "MagicSorter")
                {
                    Log.Warning("[MagicSorter] Invalid config file format, using defaults");
                    return config;
                }

                // Parse each setting
                config.FallbackToBuiltIn = GetNodeValueBool(root, "FallbackToBuiltIn", config.FallbackToBuiltIn);
                config.UseSpecificityResolution =
                    GetNodeValueBool(root, "UseSpecificityResolution", config.UseSpecificityResolution);
                config.DefaultRange = GetNodeValueInt(root, "DefaultRange", config.DefaultRange);
                config.DebugLogging = GetNodeValueBool(root, "DebugLogging", config.DebugLogging);

                Log.Out("[MagicSorter] Configuration loaded successfully");
            }
            catch (Exception ex)
            {
                Log.Error($"[MagicSorter] Error loading config: {ex.Message}");
            }

            return config;
        }

        private static string GetNodeValue(XmlElement root, string nodeName, string defaultValue)
        {
            var node = root.SelectSingleNode(nodeName);
            if (node != null && !string.IsNullOrEmpty(node.InnerText)) return node.InnerText.Trim();
            return defaultValue;
        }

        private static int GetNodeValueInt(XmlElement root, string nodeName, int defaultValue)
        {
            var value = GetNodeValue(root, nodeName, null);
            if (value != null && int.TryParse(value, out var result)) return result;
            return defaultValue;
        }

        private static bool GetNodeValueBool(XmlElement root, string nodeName, bool defaultValue)
        {
            var value = GetNodeValue(root, nodeName, null);
            if (value != null && bool.TryParse(value, out var result)) return result;
            return defaultValue;
        }
    }
}
