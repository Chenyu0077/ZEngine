using UnityEngine;

namespace ZEngine.UI
{
    /// <summary>
    /// Base class for all UI panels in ZEngine.
    /// Override lifecycle methods to implement panel behavior.
    /// </summary>
    [RequireComponent(typeof(Canvas))]
    public abstract class UIPanel : MonoBehaviour
    {
        private Canvas _canvas;
        private CanvasGroup _canvasGroup;

        public string PanelName => GetType().Name;
        public UILayer Layer { get; private set; }
        public bool IsVisible => gameObject.activeSelf;

        /// <summary>
        /// Initialize the panel. Called by UIManager after instantiation.
        /// </summary>
        internal void Init(UILayer layer)
        {
            Layer = layer;
            _canvas = GetComponent<Canvas>();
            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
            {
                _canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
            _canvas.sortingOrder = (int)layer;
            OnInit();
        }

        /// <summary>
        /// Open the panel with optional data.
        /// </summary>
        internal void Open(object data = null)
        {
            gameObject.SetActive(true);
            OnOpen(data);
        }

        /// <summary>
        /// Close the panel.
        /// </summary>
        internal void Close()
        {
            OnClose();
            gameObject.SetActive(false);
        }

        /// <summary>
        /// Called once when the panel is first created.
        /// </summary>
        protected virtual void OnInit() { }

        /// <summary>
        /// Called every time the panel is opened.
        /// </summary>
        /// <param name="data">Optional data passed when opening.</param>
        protected virtual void OnOpen(object data) { }

        /// <summary>
        /// Called every time the panel is closed.
        /// </summary>
        protected virtual void OnClose() { }

        /// <summary>
        /// Called by UIManager every frame while panel is visible.
        /// </summary>
        public virtual void OnUpdate(float deltaTime) { }

        /// <summary>
        /// Set panel interactability.
        /// </summary>
        public void SetInteractable(bool interactable)
        {
            if (_canvasGroup != null)
                _canvasGroup.interactable = interactable;
        }

        /// <summary>
        /// Set panel alpha.
        /// </summary>
        public void SetAlpha(float alpha)
        {
            if (_canvasGroup != null)
                _canvasGroup.alpha = Mathf.Clamp01(alpha);
        }
    }
}
