using System;
using UnityEngine;

namespace ZEngine.Manager.Mouse
{
    /// <summary>
    /// 鼠标事件处理器
    /// 支持：左键单击/双击、右键单击/双击、滚轮滚动、左键拖拽。
    /// 所有事件在 Update() 中驱动，无需其他依赖。
    /// </summary>
    public class MouseEventHandler
    {
        [Header("双击判定（秒）")]
        [SerializeField] private float doubleClickInterval = 0.25f;

        [Header("拖拽启动阈值（像素）")]
        [SerializeField] private float dragThreshold = 5f;

        #region 鼠标事件
        // 左键单击（确认不是双击后触发）
        public event Action<Vector2> OnLeftClick;
        // 左键双击
        public event Action<Vector2> OnLeftDoubleClick;

        // 右键单击（确认不是双击后触发）
        public event Action<Vector2> OnRightClick;
        // 右键双击
        public event Action<Vector2> OnRightDoubleClick;

        // 滚轮滚动
        public event Action<MouseScrollEventArgs> OnScroll;

        // 左键拖拽开始
        public event Action<Vector2> OnLeftDragBegin;
        // 左键拖拽中（每帧触发）
        public event Action<MouseDragEventArgs> OnLeftDragging;
        // 左键拖拽结束
        public event Action<MouseDragEventArgs> OnLeftDragEnd;
        #endregion

        #region 按键状态

        // 左键
        private int    _leftClickCount;
        private float  _leftLastClickTime;
        private bool   _leftPendingSingle;   // 等待判断是否双击
        private Vector2 _leftPendingPos;

        // 右键
        private int    _rightClickCount;
        private float  _rightLastClickTime;
        private bool   _rightPendingSingle;
        private Vector2 _rightPendingPos;

        // 拖拽
        private bool   _isDragging;
        private bool   _dragStarted;         // 是否已经超过阈值
        private Vector2 _dragStartPos;
        private Vector2 _dragPrevPos;
        private readonly MouseDragEventArgs _dragArgs = new MouseDragEventArgs();

        // 滚轮
        private readonly MouseScrollEventArgs _scrollArgs = new MouseScrollEventArgs();

        #endregion
        

        public void Update()
        {
            HandleLeftButton();
            HandleRightButton();
            HandleScroll();
            HandleDrag();
            FlushPendingClicks();
        }

        public void Destroy()
        {
            ClearAllListeners();
        }

        
        /// <summary>
        /// 左键处理
        /// </summary>
        private void HandleLeftButton()
        {
            if (!Input.GetMouseButtonDown(0)) return;

            Vector2 pos = Input.mousePosition;
            float now = Time.unscaledTime;

            if (now - _leftLastClickTime <= doubleClickInterval)
            {
                // 双击：取消待触发的单击
                _leftPendingSingle = false;
                _leftClickCount = 0;
                OnLeftDoubleClick?.Invoke(pos);
            }
            else
            {
                // 记录第一次点击，等待判定
                _leftClickCount = 1;
                _leftPendingSingle = true;
                _leftPendingPos = pos;
            }

            _leftLastClickTime = now;
        }

        
        /// <summary>
        /// 右键处理
        /// </summary>
        private void HandleRightButton()
        {
            if (!Input.GetMouseButtonDown(1)) return;

            Vector2 pos = Input.mousePosition;
            float now = Time.unscaledTime;

            if (now - _rightLastClickTime <= doubleClickInterval)
            {
                _rightPendingSingle = false;
                _rightClickCount = 0;
                OnRightDoubleClick?.Invoke(pos);
            }
            else
            {
                _rightClickCount = 1;
                _rightPendingSingle = true;
                _rightPendingPos = pos;
            }

            _rightLastClickTime = now;
        }

        
        /// <summary>
        /// 延迟派发单击（双击判定窗口结束后，双击不能触发单击，缺点是单击会有延迟）
        /// </summary>
        private void FlushPendingClicks()
        {
            float now = Time.unscaledTime;

            if (_leftPendingSingle && now - _leftLastClickTime > doubleClickInterval)
            {
                _leftPendingSingle = false;
                OnLeftClick?.Invoke(_leftPendingPos);
            }

            if (_rightPendingSingle && now - _rightLastClickTime > doubleClickInterval)
            {
                _rightPendingSingle = false;
                OnRightClick?.Invoke(_rightPendingPos);
            }
        }

        
        /// <summary>
        /// 滚轮处理
        /// </summary>
        private void HandleScroll()
        {
            float scroll = Input.mouseScrollDelta.y;
            if (Mathf.Approximately(scroll, 0f)) return;

            _scrollArgs.ScrollDelta = scroll;
            OnScroll?.Invoke(_scrollArgs);
        }

        
        /// <summary>
        /// 左键拖拽处理
        /// </summary>
        private void HandleDrag()
        {
            // 按下开始追踪
            if (Input.GetMouseButtonDown(0))
            {
                _isDragging = true;
                _dragStarted = false;
                _dragStartPos = Input.mousePosition;
                _dragPrevPos = _dragStartPos;
            }

            // 持续拖拽
            if (_isDragging && Input.GetMouseButton(0))
            {
                Vector2 curPos = Input.mousePosition;

                if (!_dragStarted)
                {
                    // 超过阈值才算真正开始拖拽
                    if (Vector2.Distance(curPos, _dragStartPos) >= dragThreshold)
                    {
                        _dragStarted = true;

                        _dragArgs.StartPosition = _dragStartPos;
                        _dragArgs.PreviousPosition = _dragPrevPos;
                        _dragArgs.CurrentPosition = curPos;

                        OnLeftDragBegin?.Invoke(_dragStartPos);
                        OnLeftDragging?.Invoke(_dragArgs);
                    }
                }
                else
                {
                    _dragArgs.StartPosition = _dragStartPos;
                    _dragArgs.PreviousPosition = _dragPrevPos;
                    _dragArgs.CurrentPosition = curPos;

                    OnLeftDragging?.Invoke(_dragArgs);
                }

                _dragPrevPos = curPos;
            }

            // 释放
            if (Input.GetMouseButtonUp(0))
            {
                if (_dragStarted)
                {
                    _dragArgs.StartPosition = _dragStartPos;
                    _dragArgs.PreviousPosition = _dragPrevPos;
                    _dragArgs.CurrentPosition = Input.mousePosition;

                    OnLeftDragEnd?.Invoke(_dragArgs);
                }

                _isDragging  = false;
                _dragStarted = false;
            }
        }
        

        /// <summary>
        /// 清除所有已注册的事件监听
        /// </summary>
        public void ClearAllListeners()
        {
            OnLeftClick = null;
            OnLeftDoubleClick = null;
            OnRightClick = null;
            OnRightDoubleClick = null;
            OnScroll = null;
            OnLeftDragBegin = null;
            OnLeftDragging = null;
            OnLeftDragEnd = null;
        }
    }
}
