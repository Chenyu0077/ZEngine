using System;
using System.Collections.Generic;
using UnityEngine;

namespace ZEngine.Config
{
    /// <summary>
    /// Manages loading and caching of ConfigTable assets.
    /// </summary>
    public class ConfigManager : Core.Singleton<ConfigManager>
    {
        private readonly Dictionary<Type, ConfigTable> _tables = new Dictionary<Type, ConfigTable>();

        /// <summary>
        /// Load and register a config table from the Resources folder.
        /// </summary>
        public T LoadTable<T>(string path) where T : ConfigTable
        {
            var type = typeof(T);
            if (_tables.TryGetValue(type, out var cached))
                return cached as T;

            var table = Resources.Load<T>(path);
            if (table == null)
            {
                Debug.LogWarning($"[ConfigManager] ConfigTable not found at: {path}");
                return null;
            }
            table.OnLoad();
            _tables[type] = table;
            return table;
        }

        /// <summary>
        /// Get a previously loaded config table.
        /// </summary>
        public T GetTable<T>() where T : ConfigTable
        {
            _tables.TryGetValue(typeof(T), out var table);
            return table as T;
        }

        /// <summary>
        /// Unload all cached config tables.
        /// </summary>
        public void UnloadAll()
        {
            _tables.Clear();
        }
    }
}
