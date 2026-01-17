using System;
using System.IO;
using System.Net;
using System.Threading;
using MagicSorter.Models;
using Newtonsoft.Json;

namespace MagicSorter.Services
{
    /// <summary>
    /// Handles loading, caching, and refreshing of category mappings
    /// </summary>
    public class MappingLoader
    {
        private const string CacheFileName = "mappings_cache.json";
        private const string LocalMappingsFileName = "mappings.json";

        private readonly string _modPath;
        private readonly ModConfiguration _config;
        private MappingData _currentMappings;
        private DateTime _lastFetchTime;
        private bool _isInitialized;
        private int _isFetching; // int for Interlocked operations (0 = false, 1 = true)
        private readonly object _lock = new object();

        public MappingLoader(string modPath, ModConfiguration config)
        {
            _modPath = modPath;
            _config = config;
            _currentMappings = new MappingData();
        }

        /// <summary>
        /// Gets the currently loaded mappings
        /// </summary>
        public MappingData GetMappings()
        {
            lock (_lock)
            {
                return _currentMappings;
            }
        }

        /// <summary>
        /// Returns true if mappings have been loaded (from any source)
        /// </summary>
        public bool IsInitialized => _isInitialized;

        /// <summary>
        /// Returns true if currently fetching from remote
        /// </summary>
        public bool IsFetching => _isFetching != 0;

        /// <summary>
        /// Gets count of items in current mappings
        /// </summary>
        public int ItemCount
        {
            get
            {
                lock (_lock)
                {
                    return _currentMappings?.Items?.Count ?? 0;
                }
            }
        }

        /// <summary>
        /// Gets count of categories in current mappings
        /// </summary>
        public int CategoryCount
        {
            get
            {
                lock (_lock)
                {
                    return _currentMappings?.Categories?.Count ?? 0;
                }
            }
        }

        /// <summary>
        /// Gets the version of current mappings
        /// </summary>
        public string Version
        {
            get
            {
                lock (_lock)
                {
                    return _currentMappings?.Version ?? "unknown";
                }
            }
        }

        /// <summary>
        /// Initializes mappings - tries local first, then starts async remote fetch
        /// </summary>
        public void Initialize()
        {
            // First, try to load local mappings synchronously
            if (TryLoadLocalMappings())
            {
                _isInitialized = true;
                Log.Out($"[MagicSorter] Loaded local mappings (v{Version}, {CategoryCount} categories, {ItemCount} items)");
            }

            // Then try cached mappings
            else if (TryLoadFromCache())
            {
                _isInitialized = true;
                Log.Out($"[MagicSorter] Loaded cached mappings (v{Version}, {CategoryCount} categories, {ItemCount} items)");
            }

            // Start async remote fetch if URL is configured
            if (!string.IsNullOrEmpty(_config.RemoteMappingsUrl))
            {
                InitializeAsync();
            }
            else if (!_isInitialized)
            {
                Log.Out("[MagicSorter] No mappings loaded - will use built-in Groups fallback");
                _isInitialized = true;
            }
        }

        /// <summary>
        /// Starts background fetch of remote mappings
        /// </summary>
        public void InitializeAsync()
        {
            if (string.IsNullOrEmpty(_config.RemoteMappingsUrl))
            {
                return;
            }

            ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {
                    FetchRemote();
                }
                catch (Exception ex)
                {
                    Log.Warning($"[MagicSorter] Background fetch failed: {ex.Message}");
                }
            });
        }

        /// <summary>
        /// Forces a refresh of remote mappings (blocking)
        /// </summary>
        public bool ForceRefresh()
        {
            if (string.IsNullOrEmpty(_config.RemoteMappingsUrl))
            {
                Log.Warning("[MagicSorter] No remote URL configured");
                return false;
            }

            return FetchRemote();
        }

        private bool TryLoadLocalMappings()
        {
            var localPath = Path.Combine(_modPath, LocalMappingsFileName);
            if (!File.Exists(localPath))
            {
                return false;
            }

            try
            {
                var json = File.ReadAllText(localPath);
                var mappings = JsonConvert.DeserializeObject<MappingData>(json);

                if (mappings != null && (mappings.Categories.Count > 0 || mappings.Items.Count > 0))
                {
                    mappings.NormalizeDictionaries();
                    lock (_lock)
                    {
                        _currentMappings = mappings;
                    }
                    return true;
                }
            }
            catch (Exception ex)
            {
                Log.Warning($"[MagicSorter] Failed to load local mappings: {ex.Message}");
            }

            return false;
        }

        private bool TryLoadFromCache()
        {
            var cachePath = GetCachePath();
            if (!File.Exists(cachePath))
            {
                return false;
            }

            try
            {
                // Check if cache is expired
                var cacheAge = DateTime.Now - File.GetLastWriteTime(cachePath);
                if (cacheAge.TotalHours > _config.CacheDurationHours)
                {
                    if (_config.DebugLogging)
                    {
                        Log.Out($"[MagicSorter] Cache expired ({cacheAge.TotalHours:F1} hours old)");
                    }
                    // Still load it as fallback, but mark as needing refresh
                }

                var json = File.ReadAllText(cachePath);
                var mappings = JsonConvert.DeserializeObject<MappingData>(json);

                if (mappings != null && (mappings.Categories.Count > 0 || mappings.Items.Count > 0))
                {
                    mappings.NormalizeDictionaries();
                    lock (_lock)
                    {
                        _currentMappings = mappings;
                        _lastFetchTime = File.GetLastWriteTime(cachePath);
                    }
                    return true;
                }
            }
            catch (Exception ex)
            {
                Log.Warning($"[MagicSorter] Failed to load cached mappings: {ex.Message}");
            }

            return false;
        }

        private bool FetchRemote()
        {
            // Atomically check and set _isFetching to prevent race conditions
            // CompareExchange returns the original value; if it was 0, we set it to 1 and proceed
            if (Interlocked.CompareExchange(ref _isFetching, 1, 0) != 0)
            {
                return false; // Another thread is already fetching
            }

            try
            {
                if (_config.DebugLogging)
                {
                    Log.Out($"[MagicSorter] Fetching mappings from {_config.RemoteMappingsUrl}");
                }

                // Use HttpWebRequest instead of WebClient for proper timeout support
                var json = DownloadWithTimeout(_config.RemoteMappingsUrl, _config.ConnectionTimeoutSeconds * 1000);

                if (string.IsNullOrEmpty(json))
                {
                    Log.Warning("[MagicSorter] Empty response from remote");
                    return false;
                }

                var mappings = JsonConvert.DeserializeObject<MappingData>(json);

                if (mappings == null || (mappings.Categories.Count == 0 && mappings.Items.Count == 0))
                {
                    Log.Warning("[MagicSorter] Invalid mappings data from remote");
                    return false;
                }

                mappings.NormalizeDictionaries();
                lock (_lock)
                {
                    _currentMappings = mappings;
                    _lastFetchTime = DateTime.Now;
                }

                // Save to cache
                SaveToCache(json);

                _isInitialized = true;
                Log.Out($"[MagicSorter] Loaded remote mappings (v{Version}, {CategoryCount} categories, {ItemCount} items)");
                return true;
            }
            catch (WebException ex)
            {
                Log.Warning($"[MagicSorter] Network error fetching mappings: {ex.Message}");
            }
            catch (TimeoutException ex)
            {
                Log.Warning($"[MagicSorter] {ex.Message}");
            }
            catch (Exception ex)
            {
                Log.Error($"[MagicSorter] Error fetching mappings: {ex.Message}");
            }
            finally
            {
                // Atomically reset _isFetching to 0
                Interlocked.Exchange(ref _isFetching, 0);
            }

            return false;
        }

        /// <summary>
        /// Downloads content from URL with proper timeout support using HttpWebRequest.
        /// Unlike WebClient with background threads, this properly respects timeouts
        /// without leaving orphaned downloads or leaking resources.
        /// </summary>
        private string DownloadWithTimeout(string url, int timeoutMs)
        {
            var request = (HttpWebRequest)WebRequest.Create(url);
            request.Timeout = timeoutMs;
            request.ReadWriteTimeout = timeoutMs;
            request.UserAgent = "MagicSorter/1.0";
            request.Method = "GET";

            using (var response = (HttpWebResponse)request.GetResponse())
            using (var stream = response.GetResponseStream())
            {
                if (stream == null)
                {
                    return null;
                }

                using (var reader = new StreamReader(stream))
                {
                    return reader.ReadToEnd();
                }
            }
        }

        private void SaveToCache(string json)
        {
            try
            {
                var cachePath = GetCachePath();
                var cacheDir = Path.GetDirectoryName(cachePath);

                if (!Directory.Exists(cacheDir))
                {
                    Directory.CreateDirectory(cacheDir);
                }

                File.WriteAllText(cachePath, json);

                if (_config.DebugLogging)
                {
                    Log.Out($"[MagicSorter] Saved mappings to cache: {cachePath}");
                }
            }
            catch (Exception ex)
            {
                Log.Warning($"[MagicSorter] Failed to save cache: {ex.Message}");
            }
        }

        private string GetCachePath()
        {
            return Path.Combine(_modPath, "Cache", CacheFileName);
        }

        /// <summary>
        /// Gets status information about the mappings
        /// </summary>
        public string GetStatus()
        {
            lock (_lock)
            {
                var status = $"Version: {Version}, Categories: {CategoryCount}, Items: {ItemCount}";

                if (_lastFetchTime != default)
                {
                    var age = DateTime.Now - _lastFetchTime;
                    status += $", Last fetch: {age.TotalHours:F1}h ago";
                }

                if (_isFetching != 0)
                {
                    status += " (fetching...)";
                }

                return status;
            }
        }
    }
}
