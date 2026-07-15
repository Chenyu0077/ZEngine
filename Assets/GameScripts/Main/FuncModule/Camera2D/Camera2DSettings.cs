using UnityEngine;

namespace Main.FuncModule.Camera2D
{
    [CreateAssetMenu(fileName = "Camera2DSettings", menuName = "Game/Camera 2D/Settings")]
    public class Camera2DSettings : ScriptableObject
    {
        [Header("缩放 Zoom")]
        [Tooltip("正交相机最小尺寸（最近视野）")]
        public float minZoom = 3f;
        [Tooltip("正交相机最大尺寸（最远视野）")]
        public float maxZoom = 20f;
        [Tooltip("鼠标滚轮缩放速度")]
        public float scrollZoomSpeed = 3f;
        [Tooltip("键盘 PageDown/PageUp 缩放速度（单位/秒）")]
        public float keyZoomSpeed = 5f;
        [Tooltip("缩放平滑时间（SmoothDamp smoothTime）")]
        public float zoomSmoothTime = 0.12f;

        [Header("平移 Pan")]
        [Tooltip("键盘平移基础速度（世界单位/秒），实际速度随缩放等比缩放")]
        public float keyPanSpeed = 10f;
        [Tooltip("Shift 加速倍率")]
        public float shiftMultiplier = 2.5f;
        [Tooltip("位置平滑时间（SmoothDamp smoothTime）")]
        public float panSmoothTime = 0.08f;

        [Header("拖拽 Drag")]
        [Range(0, 6)]
        [Tooltip("拖拽使用的鼠标按键（0=左键, 1=右键, 2=中键，最大 6）")]
        public int dragMouseButton = 1;
        [Tooltip("是否开启松手后的惯性滑动")]
        public bool enableMomentum = true;
        [Min(0f)]
        [Tooltip("惯性衰减系数（越大衰减越快；推荐 4~10；为 0 则惯性永不消退）")]
        public float momentumDecay = 6f;

        [Header("屏幕边缘滚动 Edge Scroll")]
        public bool enableEdgeScroll = false;
        [Tooltip("触发边缘滚动的屏幕边缘宽度（像素）")]
        public float edgeScrollThreshold = 24f;
        [Tooltip("边缘滚动基础速度（世界单位/秒），随缩放等比缩放")]
        public float edgeScrollSpeed = 8f;

        [Header("跟随模式 Follow")]
        [Tooltip("跟随平滑时间（越大跟随越迟缓）")]
        public float followSmoothTime = 0.25f;
        [Tooltip("死亡区：目标与相机距离小于此值时不更新目标位置，减少抖动（世界单位）")]
        public float followDeadZone = 0.5f;
        [Tooltip("跟随模式下是否锁定缩放到固定值")]
        public bool lockZoomInFollow = false;
        [Tooltip("跟随模式锁定的正交尺寸（lockZoomInFollow = true 时生效）")]
        public float followLockedZoom = 8f;

        [Header("像素对齐 Pixel Snap")]
        [Tooltip("开启后相机位置会对齐到像素网格，消除缩放时瓦片间的缝隙")]
        public bool enablePixelSnap = true;
        [Tooltip("贴图的 Pixels Per Unit，需与 Sprite Import Settings 中的 PPU 一致（通常为 16 或 32）")]
        public float pixelsPerUnit = 32f;

        [Header("地图边界 Bounds")]
        public Vector2 minBounds = new Vector2(-100f, -100f);
        public Vector2 maxBounds = new Vector2(100f, 100f);
    }
}
