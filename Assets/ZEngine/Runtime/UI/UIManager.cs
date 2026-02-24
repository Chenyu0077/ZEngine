using System;
using System.Collections.Generic;
using UnityEngine;

namespace ZEngine.UI
{
    /// <summary>
    /// Manages all UI panels in the game.
    /// Handles opening, closing, and lifecycle of UIPanel instances.
    /// </summary>
    public class UIManager : Core.MonoSingleton<UIManager>
    {
        private readonly Dictionary<string, UIPanel> _panelCache = new Dictionary<string, UIPanel>();
        private readonly List<UIPanel> _openPanels = new List<UIPanel>();

        private Transform _uiRoot;

        protected override void OnInit()
        {
            CreateUIRoot();
            Debug.Log("[UIManager] Initialized.");
        }

        private void CreateUIRoot()
        {
            var rootGo = new GameObject("[UIRoot]");
            rootGo.transform.SetParent(transform);
            _uiRoot = rootGo.transform;
        }

        /// <summary>
        /// Open a UI panel by prefab path (loaded from Resources).
        /// </summary>
        public T OpenPanel<T>(string prefabPath, UILayer layer = UILayer.Normal, object data = null) where T : UIPanel
        {
            string panelName = typeof(T).Name;

            if (_panelCache.TryGetValue(panelName, out var existing))
            {
                existing.Open(data);
                if (!_openPanels.Contains(existing))
                    _openPanels.Add(existing);
                return existing as T;
            }

            var prefab = Resources.Load<GameObject>(prefabPath);
            if (prefab == null)
            {
                Debug.LogError($"[UIManager] Prefab not found at path: {prefabPath}");
                return null;
            }

            var panelGo = Instantiate(prefab, _uiRoot);
            var panel = panelGo.GetComponent<T>();
            if (panel == null)
            {
                Debug.LogError($"[UIManager] Panel component {panelName} not found on prefab: {prefabPath}");
                Destroy(panelGo);
                return null;
            }

            panel.Init(layer);
            _panelCache[panelName] = panel;
            _openPanels.Add(panel);
            panel.Open(data);

            Event.EventManager.Instance.Dispatch(Event.EventIds.UIPanelOpen);
            return panel;
        }

        /// <summary>
        /// Close a UI panel by type.
        /// </summary>
        public void ClosePanel<T>() where T : UIPanel
        {
            string panelName = typeof(T).Name;
            if (_panelCache.TryGetValue(panelName, out var panel))
            {
                panel.Close();
                _openPanels.Remove(panel);
                Event.EventManager.Instance.Dispatch(Event.EventIds.UIPanelClose);
            }
        }

        /// <summary>
        /// Close and destroy a UI panel by type.
        /// </summary>
        public void DestroyPanel<T>() where T : UIPanel
        {
            string panelName = typeof(T).Name;
            if (_panelCache.TryGetValue(panelName, out var panel))
            {
                panel.Close();
                _openPanels.Remove(panel);
                _panelCache.Remove(panelName);
                Destroy(panel.gameObject);
                Event.EventManager.Instance.Dispatch(Event.EventIds.UIPanelClose);
            }
        }

        /// <summary>
        /// Check if a panel is currently open.
        /// </summary>
        public bool IsPanelOpen<T>() where T : UIPanel
        {
            string panelName = typeof(T).Name;
            return _panelCache.TryGetValue(panelName, out var panel) && panel.IsVisible;
        }

        /// <summary>
        /// Get a panel instance by type (returns null if not created).
        /// </summary>
        public T GetPanel<T>() where T : UIPanel
        {
            string panelName = typeof(T).Name;
            _panelCache.TryGetValue(panelName, out var panel);
            return panel as T;
        }

        /// <summary>
        /// Close all open panels.
        /// </summary>
        public void CloseAllPanels()
        {
            foreach (var panel in _openPanels)
            {
                panel.Close();
            }
            _openPanels.Clear();
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            for (int i = 0; i < _openPanels.Count; i++)
            {
                if (_openPanels[i].IsVisible)
                    _openPanels[i].OnUpdate(dt);
            }
        }
    }
}
