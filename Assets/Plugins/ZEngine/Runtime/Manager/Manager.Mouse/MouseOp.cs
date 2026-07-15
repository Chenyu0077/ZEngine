using UnityEngine;

namespace  ZEngine.Manager.Mouse
{
    public enum MouseOp
    {
        leftClick,          // 左键单击
        rightClick,         // 右键单击
        middleClick,        // 中键单击
        leftDoubleClick,    // 左键双击
        rightDoubleClick,   // 右键双击
        middleDoubleClick,  // 中键双击
        leftDragStart,      // 左键拖拽
        rightDragging,      // 右键拖拽
        middleDragEnd,      // 中键拖拽
    }
    
    /// <summary>
    /// 鼠标拖拽事件数据
    /// </summary>
    public class MouseDragEventArgs
    {
        // 拖拽起始屏幕坐标
        public Vector2 StartPosition;
        // 当前帧屏幕坐标
        public Vector2 CurrentPosition;
        // 上一帧屏幕坐标
        public Vector2 PreviousPosition;
        // 本帧相对位移（delta）
        public Vector2 Delta => CurrentPosition - PreviousPosition;
        // 距起点总位移
        public Vector2 TotalDelta => CurrentPosition - StartPosition;
    }

    /// <summary>
    /// 鼠标滚轮事件数据
    /// </summary>
    public class MouseScrollEventArgs
    {
        // 滚动量（正值向上，负值向下）
        public float ScrollDelta;
    }
}
