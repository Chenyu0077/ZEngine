using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ZEngine.Scene
{
    /// <summary>
    /// Manages scene loading and unloading with progress callbacks.
    /// </summary>
    public class SceneManager : Core.MonoSingleton<SceneManager>
    {
        public string CurrentSceneName => UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        public bool IsLoading { get; private set; }

        protected override void OnInit()
        {
            Debug.Log("[SceneManager] Initialized.");
        }

        /// <summary>
        /// Asynchronously load a scene by name.
        /// </summary>
        /// <param name="sceneName">Name of the scene to load.</param>
        /// <param name="mode">Additive or Single load mode.</param>
        /// <param name="onProgress">Callback for load progress (0..1).</param>
        /// <param name="onComplete">Callback when load is complete.</param>
        public void LoadScene(
            string sceneName,
            LoadSceneMode mode = LoadSceneMode.Single,
            Action<float> onProgress = null,
            Action onComplete = null)
        {
            if (IsLoading)
            {
                Debug.LogWarning("[SceneManager] A scene load is already in progress.");
                return;
            }
            StartCoroutine(LoadSceneAsync(sceneName, mode, onProgress, onComplete));
        }

        /// <summary>
        /// Asynchronously unload a scene by name (additive scenes only).
        /// </summary>
        public void UnloadScene(string sceneName, Action onComplete = null)
        {
            StartCoroutine(UnloadSceneAsync(sceneName, onComplete));
        }

        private IEnumerator LoadSceneAsync(
            string sceneName,
            LoadSceneMode mode,
            Action<float> onProgress,
            Action onComplete)
        {
            IsLoading = true;
            Event.EventManager.Instance.Dispatch(Event.EventIds.SceneLoadStart);

            var op = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(sceneName, mode);
            op.allowSceneActivation = false;

            while (op.progress < 0.9f)
            {
                onProgress?.Invoke(op.progress);
                yield return null;
            }
            onProgress?.Invoke(1f);
            op.allowSceneActivation = true;

            yield return op;

            IsLoading = false;
            Event.EventManager.Instance.Dispatch(Event.EventIds.SceneLoadFinish);
            onComplete?.Invoke();
        }

        private IEnumerator UnloadSceneAsync(string sceneName, Action onComplete)
        {
            Event.EventManager.Instance.Dispatch(Event.EventIds.SceneUnloadStart);
            var op = UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync(sceneName);
            yield return op;
            Event.EventManager.Instance.Dispatch(Event.EventIds.SceneUnloadFinish);
            onComplete?.Invoke();
        }
    }
}
