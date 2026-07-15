using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using FairyGUI;
using ZEngine.Manager.Resource;

namespace Main.FuncModule.Camera2D
{
    public enum Camera2DMode
    {
        GodView,   // 上帝视角：玩家自由平移 + 缩放
        Follow,    // 跟随视角：相机跟随绑定目标
        Cinematic, // 剧情模式：程序驱动，禁用玩家所有输入
    }

    /// <summary>
    /// 2D 正交相机控制器（RimWorld 风格）。
    ///
    /// 使用方式：
    ///   1. 挂载到主相机 GameObject。
    ///   2. 在 Inspector 中指定 Camera2DSettings 资产。
    ///   3. 通过 SetMode / SetFollowTarget / EnqueueCinematic 等接口驱动。
    ///
    /// 依赖：DOTween（用于 CinematicOp）、Camera2DSettings、CinematicOp。
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class Camera2DController : MonoBehaviour
    {
        [SerializeField] public Camera2DSettings settings;

        // ── Components ──────────────────────────────────────────────
        private Camera cam;

        // ── Mode ────────────────────────────────────────────────────
        private Camera2DMode mode = Camera2DMode.GodView;

        // ── Smooth target ───────────────────────────────────────────
        // "目标值"：所有输入/逻辑只修改这两个值，实际相机通过 SmoothDamp 靠近它们
        private Vector2 targetPos;
        private float   targetZoom;

        // SmoothDamp 内部速度状态
        private Vector2 posVelocity;
        private float   zoomVelocity;

        // ── Drag ────────────────────────────────────────────────────
        private bool    isDragging;
        private Vector2 dragPrevScreen;  // 上一帧鼠标屏幕坐标（像素）
        private Vector2 dragMomentum;    // 当前惯性速度（世界单位/秒）

        // ── Follow ──────────────────────────────────────────────────
        private Transform followTarget;

        // ── Cinematic ───────────────────────────────────────────────
        private readonly Queue<CinematicOp> cinematicQueue = new();
        private bool cinematicBusy;

        // ── Public read-only ─────────────────────────────────────────
        public Camera2DMode Mode         => mode;
        public Camera       ActiveCamera => cam;

        // ─────────────────────────────────────────────────────────────
        #region Unity 生命周期

        private void Awake()
        {
            if (settings == null)
            {
                var handle = ResourceManager.Instance.LoadAssetSync<Camera2DSettings>("SO/Camera/Camera2DSettings");
                settings = handle?.AssetObject as Camera2DSettings;
            }
            if (settings == null)
            {
                Debug.LogError("[Camera2DController] Settings 未赋值，请在 Inspector 中指定 Camera2DSettings 资产。", this);
                enabled = false;
                return;
            }
            else
            {
                Debug.Log("[Camera2DController] Awake");
            }

            cam = GetComponent<Camera>();
            cam.orthographic = true;

            targetPos  = new Vector2(transform.position.x, transform.position.y);
            targetZoom = cam.orthographicSize;
            DontDestroyOnLoad(this);
        }

        private void OnDisable()
        {
            transform.DOKill();
            if (cam != null) cam.DOKill();
            // 并行 op 的 onComplete 在 DOKill 后不触发，必须手动重置，否则重启后队列永久卡死
            cinematicBusy = false;
            // 禁用期间鼠标 ButtonUp 事件丢失，重置拖拽状态防止重启后相机飞出
            isDragging   = false;
            dragMomentum = Vector2.zero;
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            // 窗口失焦时鼠标按键事件会丢失，强制重置拖拽状态防止卡死
            if (!hasFocus)
            {
                isDragging   = false;
                dragMomentum = Vector2.zero;
            }
        }

        private void Update()
        {
            //if (Stage.isTouchOnUI) return;
            
            if (mode == Camera2DMode.Cinematic)
            {
                DrainCinematicQueue();
                return;
            }

            if (mode == Camera2DMode.Follow)
                TickFollow();
            else
                TickGodView();

            if (settings.enableMomentum)
                ApplyMomentum();

            ClampTarget();
            ApplySmooth();
        }

        #endregion

        // ─────────────────────────────────────────────────────────────
        #region 上帝视角（GodView）

        private void TickGodView()
        {
            if (Stage.isTouchOnUI) return;
            HandleZoom();
            HandleDrag();
            HandleKeyboardPan();
            if (settings.enableEdgeScroll)
                HandleEdgeScroll();
        }

        // ── 缩放 ──────────────────────────────────────────────────────

        private void HandleZoom()
        {
            float scroll = Input.GetAxis("Mouse ScrollWheel");

            float keyDelta = 0f;
            if (Input.GetKey(KeyCode.PageDown)) keyDelta -= settings.keyZoomSpeed * Time.deltaTime;
            if (Input.GetKey(KeyCode.PageUp))   keyDelta += settings.keyZoomSpeed * Time.deltaTime;

            float delta = -scroll * settings.scrollZoomSpeed + keyDelta;
            if (Mathf.Abs(delta) < 0.0001f) return;

            if (Mathf.Abs(scroll) > 0.001f)
                ZoomTowardCursor(delta);
            else
                targetZoom = Mathf.Clamp(targetZoom + delta, settings.minZoom, settings.maxZoom);
        }

        /// <summary>
        /// 以鼠标所在的世界点为锚点缩放，使鼠标下方的地图位置保持不动。
        ///
        /// 原理：正交相机中，屏幕偏移量转为世界单位 = offsetPx * (2 * orthoSize / screenH)。
        /// 缩放前后以相同屏幕偏移对应的世界偏移量之差即为需要补偿的相机位移。
        /// </summary>
        private void ZoomTowardCursor(float delta)
        {
            Vector2 mouseScreen  = Input.mousePosition;
            Vector2 screenCenter = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            Vector2 offsetPx     = mouseScreen - screenCenter;

            float prevZoom = targetZoom;
            targetZoom = Mathf.Clamp(targetZoom + delta, settings.minZoom, settings.maxZoom);

            // 屏幕像素转世界单位的比例（正交相机高度方向）
            float wppPrev = 2f * prevZoom      / Screen.height;
            float wppNew  = 2f * targetZoom    / Screen.height;

            // 补偿位移，使鼠标锚点世界坐标不变
            targetPos += offsetPx * (wppPrev - wppNew);
        }

        // ── 拖拽平移 ────────────────────────────────────────────────────

        private void HandleDrag()
        {
            int btn = settings.dragMouseButton;

            if (Input.GetMouseButtonDown(btn))
            {
                isDragging     = true;
                dragPrevScreen = Input.mousePosition;
                dragMomentum   = Vector2.zero;
            }

            if (isDragging && Input.GetMouseButton(btn))
            {
                Vector2 currScreen = Input.mousePosition;
                Vector2 deltaPx    = currScreen - dragPrevScreen;

                // 像素转世界单位（以实际相机当前尺寸计算，拖拽时 target 可能与 actual 有差距）
                float worldPerPixel = 2f * cam.orthographicSize / Screen.height;
                Vector2 worldDelta  = deltaPx * worldPerPixel;

                targetPos -= worldDelta;

                // 指数移动平均：平滑追踪拖拽速度（用于惯性）
                // Time.deltaTime 在 timeScale=0 时为 0，跳过以防 NaN 扩散
                if (Time.deltaTime > 0f)
                {
                    float t = 1f - Mathf.Exp(-12f * Time.deltaTime);
                    dragMomentum = Vector2.Lerp(dragMomentum, -worldDelta / Time.deltaTime, t);
                }

                dragPrevScreen = currScreen;
            }

            if (Input.GetMouseButtonUp(btn) && isDragging)
                isDragging = false;
        }

        // ── 键盘平移 ─────────────────────────────────────────────────────

        private void HandleKeyboardPan()
        {
            // 速度随缩放等比放大，使不同缩放级别下移动手感一致
            float zoomBase = Mathf.Max(settings.minZoom, 0.01f);
            float speed = settings.keyPanSpeed * (targetZoom / zoomBase);
            if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
                speed *= settings.shiftMultiplier;

            Vector2 dir = Vector2.zero;
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))    dir.y += 1f;
            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))  dir.y -= 1f;
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))  dir.x -= 1f;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) dir.x += 1f;

            if (dir.sqrMagnitude < 0.001f) return;

            targetPos   += dir.normalized * speed * Time.deltaTime;
            dragMomentum  = Vector2.zero; // 键盘输入取消拖拽惯性
        }

        // ── 边缘滚动 ─────────────────────────────────────────────────────

        private void HandleEdgeScroll()
        {
            float speed = settings.edgeScrollSpeed * (targetZoom / Mathf.Max(settings.minZoom, 0.01f));
            float t     = settings.edgeScrollThreshold;
            Vector2 m   = Input.mousePosition;

            Vector2 dir = Vector2.zero;
            if (m.x < t)                    dir.x -= 1f;
            if (m.x > Screen.width  - t)    dir.x += 1f;
            if (m.y < t)                    dir.y -= 1f;
            if (m.y > Screen.height - t)    dir.y += 1f;

            if (dir.sqrMagnitude > 0.001f)
                targetPos += dir.normalized * speed * Time.deltaTime;
        }

        #endregion

        // ─────────────────────────────────────────────────────────────
        #region 跟随模式（Follow）

        private void TickFollow()
        {
            if (followTarget == null)
            {
                // 目标被销毁时自动退出跟随，防止相机悬空
                SetMode(Camera2DMode.GodView);
                return;
            }

            Vector2 tp = new Vector2(followTarget.position.x, followTarget.position.y);

            // 死亡区：以实际相机位置为基准，目标仍在中心附近时不更新 targetPos，防止微抖动
            Vector2 actualPos = new Vector2(transform.position.x, transform.position.y);
            if (Vector2.Distance(actualPos, tp) > settings.followDeadZone)
                targetPos = tp;

            if (settings.lockZoomInFollow)
                targetZoom = Mathf.Clamp(settings.followLockedZoom, settings.minZoom, settings.maxZoom);
        }

        #endregion

        // ─────────────────────────────────────────────────────────────
        #region 惯性（Momentum）

        private void ApplyMomentum()
        {
            if (isDragging || dragMomentum.sqrMagnitude < 0.0001f) return;

            targetPos    += dragMomentum * Time.deltaTime;
            // 帧率无关的指数衰减：e^(-decay * dt)；decay 必须 >= 0，否则惯性会持续增长
            dragMomentum *= Mathf.Exp(-Mathf.Max(0f, settings.momentumDecay) * Time.deltaTime);

            if (dragMomentum.sqrMagnitude < 0.0001f)
                dragMomentum = Vector2.zero;
        }

        #endregion

        // ─────────────────────────────────────────────────────────────
        #region 边界约束与平滑应用

        private void ClampTarget()
        {
            float halfH = targetZoom;
            float halfW = cam.aspect * halfH;

            float minX = settings.minBounds.x + halfW;
            float maxX = settings.maxBounds.x - halfW;
            float minY = settings.minBounds.y + halfH;
            float maxY = settings.maxBounds.y - halfH;

            // 若地图小于视口则居中（防止 min > max 时 Clamp 行为异常）
            targetPos.x = minX < maxX
                ? Mathf.Clamp(targetPos.x, minX, maxX)
                : (settings.minBounds.x + settings.maxBounds.x) * 0.5f;

            targetPos.y = minY < maxY
                ? Mathf.Clamp(targetPos.y, minY, maxY)
                : (settings.minBounds.y + settings.maxBounds.y) * 0.5f;
        }

        private void ApplySmooth()
        {
            float z = transform.position.z;

            if (isDragging)
            {
                // 拖拽时直接跟手，不引入延迟
                transform.position = new Vector3(targetPos.x, targetPos.y, z);
                posVelocity = Vector2.zero;
            }
            else
            {
                float smoothTime = mode == Camera2DMode.Follow
                    ? settings.followSmoothTime
                    : settings.panSmoothTime;

                Vector2 cur = new Vector2(transform.position.x, transform.position.y);
                cur = Vector2.SmoothDamp(cur, targetPos, ref posVelocity, smoothTime);
                transform.position = new Vector3(cur.x, cur.y, z);
            }

            cam.orthographicSize = Mathf.SmoothDamp(
                cam.orthographicSize, targetZoom, ref zoomVelocity, settings.zoomSmoothTime);

            if (settings.enablePixelSnap)
                SnapToPixelGrid();
        }

        // 将相机位置对齐到像素网格，消除亚像素偏移导致的瓦片缝隙。
        // 单像素的世界尺寸 = 1 / PPU；屏幕上每个"游戏像素"对应的世界单位
        // 由正交尺寸决定，但对齐到物理像素格即可消除撕裂。
        private void SnapToPixelGrid()
        {
            float ppu = Mathf.Max(settings.pixelsPerUnit, 1f);
            Vector3 p = transform.position;
            p.x = Mathf.Round(p.x * ppu) / ppu;
            p.y = Mathf.Round(p.y * ppu) / ppu;
            transform.position = p;
        }

        #endregion

        // ─────────────────────────────────────────────────────────────
        #region 剧情队列（Cinematic）

        private void DrainCinematicQueue()
        {
            if (cinematicBusy || cinematicQueue.Count == 0) return;
            cinematicBusy = true;
            try
            {
                cinematicQueue.Dequeue().Execute(this, () => cinematicBusy = false);
            }
            catch (System.Exception e)
            {
                // CinematicCallback 等用户代码抛出异常时，确保队列不永久卡死
                Debug.LogException(e, this);
                cinematicBusy = false;
            }
        }

        #endregion

        // ─────────────────────────────────────────────────────────────
        #region 公共 API

        /// <summary>切换相机模式。切换离开 Follow 时自动解绑目标。</summary>
        public void SetMode(Camera2DMode newMode)
        {
            if (newMode != Camera2DMode.Follow) followTarget = null;
            mode         = newMode;
            isDragging   = false;
            dragMomentum = Vector2.zero;
        }

        /// <summary>绑定跟随目标并切换到 Follow 模式。</summary>
        public void SetFollowTarget(Transform target)
        {
            followTarget = target;
            mode         = Camera2DMode.Follow;
            dragMomentum = Vector2.zero;
        }

        /// <summary>
        /// 瞬间跳转到指定位置（同步 target 与实际相机，不触发平滑过渡）。
        /// 通常由 CinematicOp 在 DOTween 动画结束后调用以同步内部状态。
        /// </summary>
        public void SnapTo(Vector2 position, float? zoom = null)
        {
            targetPos  = position;
            transform.position = new Vector3(position.x, position.y, transform.position.z);
            posVelocity = Vector2.zero;

            if (zoom.HasValue) SnapZoom(zoom.Value);
        }

        /// <summary>瞬间同步缩放（不触发平滑过渡）。</summary>
        public void SnapZoom(float zoom)
        {
            targetZoom           = Mathf.Clamp(zoom, settings.minZoom, settings.maxZoom);
            cam.orthographicSize = targetZoom;
            zoomVelocity         = 0f;
        }

        /// <summary>平滑移动到目标位置（修改 target，经 SmoothDamp 过渡）。</summary>
        public void MoveTo(Vector2 position, float? zoom = null)
        {
            targetPos    = position;
            dragMomentum = Vector2.zero;
            if (zoom.HasValue)
                targetZoom = Mathf.Clamp(zoom.Value, settings.minZoom, settings.maxZoom);
        }

        /// <summary>入队一个剧情操作。需先 SetMode(Cinematic) 激活剧情模式。</summary>
        public void EnqueueCinematic(CinematicOp op)
        {
            if (op == null) return;
            cinematicQueue.Enqueue(op);
        }

        /// <summary>清空剧情队列并终止当前动画（用于紧急打断剧情）。</summary>
        public void ClearCinematic()
        {
            cinematicQueue.Clear();
            cinematicBusy = false;
            // 终止正在运行的 DOTween，防止 OnComplete 回调在打断后仍然执行 SnapTo/SnapZoom
            transform.DOKill();
            if (cam != null) cam.DOKill();
        }

        #endregion
    }
}
