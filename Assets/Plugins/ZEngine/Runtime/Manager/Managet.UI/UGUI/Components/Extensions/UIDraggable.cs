//------------------------------
// ZEngine
// 作者: Chenyu
//------------------------------

using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ZEngine.Manager.UI.UGUI.Components
{
    /// <summary>
    /// 可拖拽元素：实现 IDragHandler/IBeginDragHandler/IEndDragHandler，提供拖拽事件。
    /// 适用于背包物品、技能图标等需要拖拽交互的元素。
    /// 挂载到需要拖拽的 GameObject 上，配合 CanvasGroup 和 Image 使用。
    /// 用法：draggable.OnDragStart += (pos) => { ... }; draggable.OnDragEnd += (pos) => { ... };
    /// </summary>
    public class UIDraggable : UIComponentBase, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        /// <summary>拖拽开始（参数为屏幕坐标）。</summary>
        public event Action<Vector2> OnDragStart;
        /// <summary>拖拽中（参数为屏幕坐标）。</summary>
        public event Action<Vector2> OnDragging;
        /// <summary>拖拽结束（参数为屏幕坐标）。</summary>
        public event Action<Vector2> OnDragEnd;

        private Canvas _canvas;
        private RectTransform _rt;
        private Vector2 _originalPos;
        private Transform _originalParent;

        protected virtual void Awake()
        {
            _rt = GetComponent<RectTransform>();
            _canvas = GetComponentInParent<Canvas>();
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            _originalPos = _rt.anchoredPosition;
            _originalParent = _rt.parent;
            // 临时提升到 Canvas 层级以显示在最前
            _rt.SetParent(_canvas != null ? _canvas.transform : _rt.parent, true);
            _rt.SetAsLastSibling();
            OnDragStart?.Invoke(eventData.position);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (_canvas != null && _canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    (RectTransform)_canvas.transform, eventData.position,
                    _canvas.worldCamera, out var localPoint);
                _rt.localPosition = localPoint;
            }
            OnDragging?.Invoke(eventData.position);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            OnDragEnd?.Invoke(eventData.position);
            // 注：由外部决定是放回原位置还是放置到目标槽位
        }

        /// <summary>回退到拖拽前的原位（父节点不动不还原，caller 自己处理 SetParent）。</summary>
        public void RestorePosition()
        {
            if (_rt != null)
            {
                _rt.anchoredPosition = _originalPos;
                if (_originalParent != null)
                    _rt.SetParent(_originalParent, true);
            }
        }

        public override void OnRelease()
        {
            OnDragStart = null;
            OnDragging = null;
            OnDragEnd = null;
        }
    }
}
