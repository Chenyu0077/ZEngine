using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ZEngine.Resource
{
    /// <summary>
    /// Manages loading and unloading of game resources.
    /// Supports Unity Resources folder and AssetBundle loading.
    /// </summary>
    public class ResourceManager : Core.MonoSingleton<ResourceManager>
    {
        private readonly Dictionary<string, Object> _resourceCache = new Dictionary<string, Object>();
        private readonly Dictionary<string, AssetBundle> _bundleCache = new Dictionary<string, AssetBundle>();

        protected override void OnInit()
        {
            Debug.Log("[ResourceManager] Initialized.");
        }

        #region Synchronous Loading

        /// <summary>
        /// Synchronously load a resource from the Resources folder.
        /// </summary>
        public T Load<T>(string path) where T : Object
        {
            if (_resourceCache.TryGetValue(path, out var cached))
                return cached as T;

            var asset = Resources.Load<T>(path);
            if (asset == null)
            {
                Debug.LogWarning($"[ResourceManager] Failed to load resource: {path}");
                return null;
            }
            _resourceCache[path] = asset;
            return asset;
        }

        /// <summary>
        /// Synchronously load a resource from an AssetBundle.
        /// </summary>
        public T LoadFromBundle<T>(string bundleName, string assetName) where T : Object
        {
            string cacheKey = $"{bundleName}/{assetName}";
            if (_resourceCache.TryGetValue(cacheKey, out var cached))
                return cached as T;

            if (!_bundleCache.TryGetValue(bundleName, out var bundle))
            {
                Debug.LogWarning($"[ResourceManager] AssetBundle not loaded: {bundleName}. Call LoadBundle first.");
                return null;
            }

            var asset = bundle.LoadAsset<T>(assetName);
            if (asset == null)
            {
                Debug.LogWarning($"[ResourceManager] Asset '{assetName}' not found in bundle '{bundleName}'.");
                return null;
            }
            _resourceCache[cacheKey] = asset;
            return asset;
        }

        #endregion

        #region Asynchronous Loading

        /// <summary>
        /// Asynchronously load a resource from the Resources folder.
        /// </summary>
        public void LoadAsync<T>(string path, Action<T> onComplete) where T : Object
        {
            if (_resourceCache.TryGetValue(path, out var cached))
            {
                onComplete?.Invoke(cached as T);
                return;
            }
            StartCoroutine(LoadAsyncCoroutine(path, onComplete));
        }

        private IEnumerator LoadAsyncCoroutine<T>(string path, Action<T> onComplete) where T : Object
        {
            var request = Resources.LoadAsync<T>(path);
            yield return request;

            if (request.asset == null)
            {
                Debug.LogWarning($"[ResourceManager] Async load failed for: {path}");
                onComplete?.Invoke(null);
                yield break;
            }
            _resourceCache[path] = request.asset;
            onComplete?.Invoke(request.asset as T);
        }

        /// <summary>
        /// Asynchronously load an AssetBundle.
        /// </summary>
        public void LoadBundleAsync(string bundlePath, Action<AssetBundle> onComplete)
        {
            string bundleName = System.IO.Path.GetFileName(bundlePath);
            if (_bundleCache.TryGetValue(bundleName, out var cached))
            {
                onComplete?.Invoke(cached);
                return;
            }
            StartCoroutine(LoadBundleAsyncCoroutine(bundlePath, bundleName, onComplete));
        }

        private IEnumerator LoadBundleAsyncCoroutine(string bundlePath, string bundleName, Action<AssetBundle> onComplete)
        {
            var request = AssetBundle.LoadFromFileAsync(bundlePath);
            yield return request;

            if (request.assetBundle == null)
            {
                Debug.LogWarning($"[ResourceManager] Failed to load AssetBundle: {bundlePath}");
                onComplete?.Invoke(null);
                yield break;
            }
            _bundleCache[bundleName] = request.assetBundle;
            onComplete?.Invoke(request.assetBundle);
        }

        #endregion

        #region Unload

        /// <summary>
        /// Unload a cached resource.
        /// </summary>
        public void Unload(string path, bool unloadObject = false)
        {
            if (_resourceCache.TryGetValue(path, out var asset))
            {
                _resourceCache.Remove(path);
                if (unloadObject && !(asset is GameObject))
                {
                    Resources.UnloadAsset(asset);
                }
            }
        }

        /// <summary>
        /// Unload an AssetBundle.
        /// </summary>
        public void UnloadBundle(string bundleName, bool unloadAllObjects = false)
        {
            if (_bundleCache.TryGetValue(bundleName, out var bundle))
            {
                bundle.Unload(unloadAllObjects);
                _bundleCache.Remove(bundleName);
            }
        }

        /// <summary>
        /// Unload all cached resources and AssetBundles.
        /// </summary>
        public void UnloadAll(bool unloadAllObjects = false)
        {
            _resourceCache.Clear();
            foreach (var bundle in _bundleCache.Values)
            {
                bundle.Unload(unloadAllObjects);
            }
            _bundleCache.Clear();
            Resources.UnloadUnusedAssets();
        }

        #endregion
    }
}
