// ============================================================
//  WaterController.cs
//  2D 水体系统控制脚本
//
//  功能：向 water2d/waterg_hw Shader 注入所有运行时参数，包括：
//    - 相机矩阵（屏幕空间 UV 计算基础）
//    - 横版反射 Y 轴位置（随相机实时更新）
//    - 水面以下贴图 UV 范围（随相机视口实时映射）
//    - 水面纹理 UV 范围
//    - 玩家坐标（视差滚动）
//    - 反射颜色/透明度等参数
//    - 模拟贴图（涟漪系统，可选）
//    - 动态波浪高度图（可选）
//
//  用法：
//    将此脚本挂载到带 SpriteRenderer（使用 water2d/waterg_hw 材质）的 GameObject 上。
// ============================================================

using UnityEngine;

namespace GameLogic
{
    [ExecuteAlways]
    [RequireComponent(typeof(SpriteRenderer))]
    public class WaterController : MonoBehaviour
    {
        // ────────────────────────────────────────────────────
        //  Inspector — 相机
        // ────────────────────────────────────────────────────
        [Header("── 相机 ──────────────────────────────")]

        [Tooltip("用于渲染水面效果的相机。若为空则自动使用 Camera.main。\n" +
                 "脚本读取其投影矩阵注入 Shader，使反射/障碍物贴图与场景对齐。")]
        public Camera mainCamera;

        // ────────────────────────────────────────────────────
        //  Inspector — 玩家
        // ────────────────────────────────────────────────────
        [Header("── 玩家 ──────────────────────────────")]

        [Tooltip("玩家 Transform，用于水面视差滚动偏移。留空则禁用视差。")]
        public Transform playerTransform;

        // ────────────────────────────────────────────────────
        //  Inspector — 反射
        // ────────────────────────────────────────────────────
        [Header("── 反射 ──────────────────────────────")]

        [Tooltip("反射基准线在水体 Sprite 上的归一化 Y 位置（0=底部, 1=顶部）。\n" +
                 "超过此 Y 位置的部分会被翻转显示为反射。\n" +
                 "开启 autoComputeReflectionY 时此值由相机和 Sprite 位置自动计算。")]
        [Range(0f, 1f)]
        public float reflectionY = 1f;

        [Tooltip("是否根据相机视口自动计算 _reflectionY。\n" +
                 "开启后忽略上方手动值，反射线随相机移动保持在水面顶部。")]
        public bool autoComputeReflectionY = true;

        [Tooltip("反射颜色色调，直接乘以反射贴图采样结果。白色=不改变颜色。")]
        public Color reflectionColor = Color.white;

        [Tooltip("反射整体透明度乘数（0=不可见, 1=完全显示）。")]
        [Range(0f, 1f)]
        public float reflectionAlpha = 1f;

        [Tooltip("原始场景色保留比例（0=完全使用 reflectionColor 替换, 1=保留原场景色）。")]
        [Range(0f, 1f)]
        public float orgColorRetain = 0f;

        // ────────────────────────────────────────────────────
        //  Inspector — 水面以下贴图
        // ────────────────────────────────────────────────────
        [Header("── 水面以下贴图 ──────────────────────")]

        [Tooltip("是否启用水面以下背景贴图。\n" +
                 "开启后脚本会自动计算水体 Sprite 在相机视口中的 UV 范围，\n" +
                 "并注入 _belowWaterTexUV，使背景贴图与场景位置正确对齐。")]
        public bool enableBelowWaterTex = false;

        [Tooltip("用于水面以下效果的相机渲染纹理。\n" +
                 "通常是一个专门渲染水下场景的相机输出的 RenderTexture。")]
        public Texture belowWaterTexture;

        // ────────────────────────────────────────────────────
        //  Inspector — 水面纹理
        // ────────────────────────────────────────────────────
        [Header("── 水面纹理 ──────────────────────────")]

        [Tooltip("水面纹理对应的 SpriteRenderer（用于计算 UV 映射范围）。\n" +
                 "若水面纹理 Sprite 与水体 Sprite 大小相同则留空，使用默认 UV (0,1,0,1)。")]
        public SpriteRenderer surfaceTextureSpriteRenderer;

        // ────────────────────────────────────────────────────
        //  Inspector — 模拟
        // ────────────────────────────────────────────────────
        [Header("── 模拟（涟漪）─────────────────────────")]

        [Tooltip("是否启用实时波纹模拟。需要配合外部模拟系统（如 WaterSimulation）使用。")]
        public bool enableSimulation = false;

        [Tooltip("由模拟系统生成并写入的模拟结果贴图（R通道=波高）。\n" +
                 "此贴图由外部模拟脚本每帧更新，WaterController 负责注入到 Shader。")]
        public RenderTexture simulationTexture;

        [Tooltip("模拟区域在水体 UV 空间中的覆盖范围 (xMin, yMin, xMax, yMax)。\n" +
                 "通常为 (0, 0, 1, 1) 表示模拟覆盖整个水体。\n" +
                 "若模拟区域小于水体，调整此值以正确对齐。")]
        public Vector4 simulationUVs = new Vector4(0f, 0f, 1f, 1f);

        // ────────────────────────────────────────────────────
        //  Inspector — 动态波浪
        // ────────────────────────────────────────────────────
        [Header("── 动态波浪边缘 ──────────────────────")]

        [Tooltip("是否启用动态波浪边缘效果（_dwaves 开关）。")]
        public bool enableDynamicWaves = false;

        [Tooltip("波浪高度贴图（R通道存储水面 Y 位置）。\n" +
                 "由波浪模拟系统每帧写入。")]
        public Texture wavesHeightTexture;

        // ────────────────────────────────────────────────────
        //  私有字段
        // ────────────────────────────────────────────────────

        private SpriteRenderer _sr;
        private Material       _mat;

        // ── Shader 属性 ID 缓存（避免字符串 HashTable 查找，提升 CPU 性能）──
        static readonly int ID_projMat          = Shader.PropertyToID("_projectionMatrix");
        static readonly int ID_viewMat          = Shader.PropertyToID("_worldToCamMatrix");
        static readonly int ID_camRect          = Shader.PropertyToID("_camRect");
        static readonly int ID_camSize          = Shader.PropertyToID("_camSize");
        static readonly int ID_reflectionY      = Shader.PropertyToID("_reflectionY");
        static readonly int ID_refTransform     = Shader.PropertyToID("_ref_transform");
        static readonly int ID_RFcolor          = Shader.PropertyToID("_RFcolor");
        static readonly int ID_RFalpha          = Shader.PropertyToID("_RFalpha");
        static readonly int ID_RForgColor       = Shader.PropertyToID("_RForgColor");
        static readonly int ID_playerPos        = Shader.PropertyToID("_playerPosition");
        static readonly int ID_belowWaterTex    = Shader.PropertyToID("_belowWaterTex");
        static readonly int ID_belowWaterUV     = Shader.PropertyToID("_belowWaterTexUV");
        static readonly int ID_belowWaterAlpha  = Shader.PropertyToID("_belowWaterTexAlpha");
        static readonly int ID_surfaceTexUV     = Shader.PropertyToID("_surfaceTexUV");
        static readonly int ID_simTex           = Shader.PropertyToID("_simTex");
        static readonly int ID_simUvs           = Shader.PropertyToID("_simUvs");
        static readonly int ID_enableSim        = Shader.PropertyToID("_enable_sim");
        static readonly int ID_dwaves           = Shader.PropertyToID("_dwaves");
        static readonly int ID_wavesHeight      = Shader.PropertyToID("_wavesHeight");
        static readonly int ID_obsTransform     = Shader.PropertyToID("_obs_transform");

        // ================================================================
        //  Unity 生命周期
        // ================================================================

        void OnEnable()
        {
            Init();
        }

        void Update()
        {
            if (_mat == null || _sr == null) return;

            // 每帧更新所有运行时注入的参数
            UpdateCameraMatrices();       // 相机矩阵（必须）
            UpdateReflectionY();          // 反射基准线 Y 坐标
            UpdateReflectionParams();     // 反射颜色/透明度
            UpdatePlayerPosition();       // 玩家坐标（视差）
            UpdateBelowWaterUV();         // 水面以下贴图 UV
            UpdateSurfaceTexUV();         // 水面纹理 UV
            UpdateSimulation();           // 模拟贴图注入
            UpdateDynamicWaves();         // 动态波浪
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            // Inspector 修改参数时立即刷新（编辑器模式）
            if (_mat != null)
            {
                UpdateReflectionParams();
                UpdateSimulation();
                UpdateDynamicWaves();
            }
        }
#endif

        // ================================================================
        //  初始化
        // ================================================================

        /// <summary>
        /// 初始化：获取 SpriteRenderer 和材质，建立初始状态
        /// </summary>
        void Init()
        {
            _sr = GetComponent<SpriteRenderer>();
            if (_sr == null)
            {
                Debug.LogError("[WaterController] 未找到 SpriteRenderer 组件！", this);
                return;
            }

            // 在 PlayMode 使用 material（实例化），Editor 使用 sharedMaterial（共享资产）
            // 避免在 Editor 中修改原始材质文件
#if UNITY_EDITOR
            _mat = Application.isPlaying ? _sr.material : _sr.sharedMaterial;
#else
            _mat = _sr.material;
#endif

            if (_mat == null)
            {
                Debug.LogError("[WaterController] SpriteRenderer 未设置材质！", this);
                return;
            }

            // 立即执行一次全量更新，确保材质参数正确
            UpdateCameraMatrices();
            UpdateReflectionParams();
            UpdateSimulation();
            UpdateDynamicWaves();
        }

        /// <summary>
        /// 获取当前有效相机（优先 mainCamera，否则使用 Camera.main）
        /// </summary>
        Camera GetCamera()
        {
            if (mainCamera != null) return mainCamera;
            return Camera.main;
        }

        // ================================================================
        //  相机矩阵注入（每帧）
        // ================================================================

        /// <summary>
        /// 将相机的投影矩阵和视图矩阵注入 Shader。
        /// Shader 内的 WorldToScreenUV() 函数依赖这两个矩阵将世界坐标转换为屏幕空间 UV，
        /// 用于反射贴图采样和障碍物贴图采样的正确对齐。
        /// </summary>
        void UpdateCameraMatrices()
        {
            Camera cam = GetCamera();
            if (cam == null) return;

            // 注入投影矩阵（Clip Space 变换）
            _mat.SetMatrix(ID_projMat, cam.projectionMatrix);

            // 注入视图矩阵（World → Camera Space 变换）
            _mat.SetMatrix(ID_viewMat, cam.worldToCameraMatrix);

            // 相机矩形（viewport rect，通常为 0,0,1,1）
            _mat.SetVector(ID_camRect, new Vector4(
                cam.rect.x, cam.rect.y, cam.rect.width, cam.rect.height));

            // 相机像素分辨率（用于反射贴图分辨率匹配）
            _mat.SetVector(ID_camSize, new Vector2(cam.pixelWidth, cam.pixelHeight));

            // _obs_transform：障碍物贴图的 UV 变换
            // 正交相机：根据相机尺寸倍率计算变换（使障碍物贴图与相机视口对齐）
            // 透视相机：使用单位变换（1,1,0,0）
            if (cam.orthographic)
            {
                // sizeMLP 默认为 1，若障碍物相机覆盖范围比水体更大则调整
                const float sizeMLP = 1f;
                _mat.SetVector(ID_obsTransform, new Vector4(
                    1f / sizeMLP,
                    1f / sizeMLP,
                    (1f - 1f / sizeMLP) / 2f,
                    (1f - 1f / sizeMLP) / 2f));
            }
            else
            {
                _mat.SetVector(ID_obsTransform, new Vector4(1f, 1f, 0f, 0f));
            }
        }

        // ================================================================
        //  反射基准线 Y（每帧）
        // ================================================================

        /// <summary>
        /// 计算并注入 _reflectionY（横版反射的 Y 轴翻转基准线）。
        ///
        /// 原理：
        ///   水体 Sprite 的顶部边缘在相机视口中对应的归一化 Y 坐标即为反射线。
        ///   Shader 以此 Y 为轴，将该线以上的场景翻转显示为水面反射。
        ///
        /// 自动计算模式：
        ///   reflectionY = (水体Sprite顶部世界Y - 相机底部世界Y) / 相机高度
        /// </summary>
        void UpdateReflectionY()
        {
            Camera cam = GetCamera();
            if (cam == null) return;

            float finalY = reflectionY;

            if (autoComputeReflectionY)
            {
                // 水体 Sprite 顶部边界的世界 Y 坐标
                float spriteTopY = _sr.bounds.max.y;

                // 相机视口的世界坐标范围（底部和顶部）
                float camYMin = cam.ViewportToWorldPoint(new Vector3(0, 0, cam.nearClipPlane)).y;
                float camYMax = cam.ViewportToWorldPoint(new Vector3(1, 1, cam.nearClipPlane)).y;

                float camHeight = Mathf.Abs(camYMax - camYMin);
                if (camHeight > 0.0001f)
                    finalY = (spriteTopY - camYMin) / camHeight;
            }

            _mat.SetFloat(ID_reflectionY, Mathf.Clamp01(finalY));

            // _ref_transform：控制反射贴图在屏幕空间的 UV 变换
            // 正交相机：使用固定缩放补偿（匹配原 ModernWater2D 的算法）
            // 透视相机：使用单位变换
            if (cam.orthographic)
                _mat.SetVector(ID_refTransform, new Vector4(1f, 0.6666666f, 0f, 0.1666666f));
            else
                _mat.SetVector(ID_refTransform, new Vector4(1f, 1f, 0f, 0f));
        }

        // ================================================================
        //  反射颜色/透明度参数（按需更新）
        // ================================================================

        /// <summary>
        /// 注入反射的颜色色调、透明度和原始色保留比例到 Shader 全局变量。
        /// 这些参数是全局的，影响场景内所有水体（使用相同 Shader 的材质）。
        /// </summary>
        void UpdateReflectionParams()
        {
            Shader.SetGlobalVector(ID_RFcolor, new Vector4(
                reflectionColor.r, reflectionColor.g, reflectionColor.b, reflectionColor.a));
            Shader.SetGlobalFloat(ID_RFalpha,    reflectionAlpha);
            Shader.SetGlobalFloat(ID_RForgColor, orgColorRetain);
        }

        // ================================================================
        //  玩家位置（每帧）
        // ================================================================

        /// <summary>
        /// 将玩家世界坐标注入材质，用于水面视差滚动偏移。
        /// Shader 内：scrollX = playerPos.x * 0.05 * _scrStrength
        /// </summary>
        void UpdatePlayerPosition()
        {
            Vector3 pos = playerTransform != null ? playerTransform.position : Vector3.zero;
            _mat.SetVector(ID_playerPos, pos);
        }

        // ================================================================
        //  水面以下贴图 UV（每帧）
        // ================================================================

        /// <summary>
        /// 计算并注入 _belowWaterTexUV——水体 Sprite 在相机视口中的归一化 UV 范围。
        ///
        /// 原理：
        ///   水面以下背景贴图通常是相机渲染的全屏 RenderTexture，
        ///   但水体 Sprite 只占视口的一部分。
        ///   此函数计算 Sprite 在视口中的 (xMin, xMax, yMin, yMax) 范围，
        ///   让 Shader 从贴图的正确位置采样背景。
        /// </summary>
        void UpdateBelowWaterUV()
        {
            if (!enableBelowWaterTex)
            {
                // 禁用时将透明度设为 0，使贴图不可见
                _mat.SetFloat(ID_belowWaterAlpha, 0f);
                return;
            }

            Camera cam = GetCamera();
            if (cam == null) return;

            // 水体 Sprite 的世界空间边界
            Bounds spriteBounds = _sr.bounds;

            // 相机视口的世界空间角点
            Vector2 camBL = cam.ViewportToWorldPoint(new Vector3(0f, 0f, cam.nearClipPlane));
            Vector2 camTR = cam.ViewportToWorldPoint(new Vector3(1f, 1f, cam.nearClipPlane));
            float camW = Mathf.Abs(camTR.x - camBL.x);
            float camH = Mathf.Abs(camTR.y - camBL.y);

            if (camW < 0.0001f || camH < 0.0001f) return;

            // 将 Sprite 边界映射到视口 UV 空间 [0, 1]
            float xMin = (spriteBounds.min.x - camBL.x) / camW;
            float xMax = (spriteBounds.max.x - camBL.x) / camW;
            float yMin = (spriteBounds.min.y - camBL.y) / camH;
            float yMax = (spriteBounds.max.y - camBL.y) / camH;

            _mat.SetVector(ID_belowWaterUV, new Vector4(xMin, xMax, yMin, yMax));

            // 注入贴图和透明度
            if (belowWaterTexture != null)
                _mat.SetTexture(ID_belowWaterTex, belowWaterTexture);

            _mat.SetFloat(ID_belowWaterAlpha, 1f);
        }

        // ================================================================
        //  水面纹理 UV（每帧）
        // ================================================================

        /// <summary>
        /// 计算并注入 _surfaceTexUV——水面表面纹理 Sprite 在水体 Sprite 上的 UV 范围。
        ///
        /// 原理：
        ///   当水面纹理 Sprite 与水体 Sprite 大小不一致时，
        ///   需要计算水体 Sprite 在纹理 Sprite 坐标系中的 (xMin, xMax, yMin, yMax)，
        ///   确保水面纹理与水体边界正确对齐。
        /// </summary>
        void UpdateSurfaceTexUV()
        {
            if (surfaceTextureSpriteRenderer == null)
            {
                // 无参考 Sprite 时使用默认值（贴图完全覆盖水体）
                _mat.SetVector(ID_surfaceTexUV, new Vector4(0f, 1f, 0f, 1f));
                return;
            }

            Bounds waterB  = _sr.bounds;
            Bounds surfB   = surfaceTextureSpriteRenderer.bounds;
            float surfW = Mathf.Abs(surfB.max.x - surfB.min.x);
            float surfH = Mathf.Abs(surfB.max.y - surfB.min.y);

            if (surfW < 0.0001f || surfH < 0.0001f) return;

            // 水体 Sprite 在纹理 Sprite 上的 UV 坐标
            float x0 = (waterB.min.x - surfB.min.x) / surfW;
            float x1 = (waterB.max.x - surfB.min.x) / surfW;
            float y0 = (waterB.min.y - surfB.min.y) / surfH;
            float y1 = (waterB.max.y - surfB.min.y) / surfH;

            _mat.SetVector(ID_surfaceTexUV, new Vector4(x0, x1, y0, y1));
        }

        // ================================================================
        //  水体模拟注入（每帧）
        // ================================================================

        /// <summary>
        /// 将模拟贴图和 UV 映射注入 Shader。
        ///
        /// 模拟贴图（_simTex）由外部模拟系统（如 GPU 波纹模拟）每帧写入，
        /// 此方法只负责"注入"，不执行实际模拟计算。
        ///
        /// _simUvs 定义了模拟贴图覆盖的水体 UV 区域，通常为 (0,0,1,1)。
        /// 若模拟区域小于水体，调整 simulationUVs 使两者对齐。
        /// </summary>
        void UpdateSimulation()
        {
            float simEnabled = enableSimulation ? 1f : 0f;
            _mat.SetFloat(ID_enableSim, simEnabled);

            if (enableSimulation && simulationTexture != null)
            {
                _mat.SetTexture(ID_simTex, simulationTexture);
                Shader.SetGlobalVector(ID_simUvs, simulationUVs);
            }
            else
            {
                // 注入空/黑色贴图，防止 Shader 采样无效纹理
                Shader.SetGlobalVector(ID_simUvs, new Vector4(0f, 0f, 0f, 0f));
            }
        }

        // ================================================================
        //  动态波浪注入（每帧）
        // ================================================================

        /// <summary>
        /// 注入动态波浪边缘的开关和高度图。
        ///
        /// _dwaves = 1 时，Shader 会读取 _wavesHeight 贴图（R通道）
        /// 与当前像素的 UV.y 比较，决定哪些像素在水面有效区域内，
        /// 同时在边界处叠加 _edgeColor 的彩色边缘效果。
        /// </summary>
        void UpdateDynamicWaves()
        {
            Shader.SetGlobalFloat(ID_dwaves, enableDynamicWaves ? 1f : 0f);

            if (enableDynamicWaves && wavesHeightTexture != null)
                Shader.SetGlobalTexture(ID_wavesHeight, wavesHeightTexture);
        }

        // ================================================================
        //  Gizmos（编辑器辅助显示）
        // ================================================================
#if UNITY_EDITOR
        void OnDrawGizmos()
        {
            if (_sr == null) _sr = GetComponent<SpriteRenderer>();
            if (_sr == null) return;

            Camera cam = GetCamera();
            if (cam == null) return;

            // 绘制横版反射基准线（黄色横线）
            float finalY = reflectionY;
            if (autoComputeReflectionY)
            {
                float camYMin = cam.ViewportToWorldPoint(new Vector3(0, 0, cam.nearClipPlane)).y;
                float camYMax = cam.ViewportToWorldPoint(new Vector3(1, 1, cam.nearClipPlane)).y;
                float spriteTopY = _sr.bounds.max.y;
                finalY = Mathf.Clamp01((spriteTopY - camYMin) / Mathf.Abs(camYMax - camYMin));
            }

            // 反射线世界 Y 坐标
            float reflWorldY = Mathf.Lerp(_sr.bounds.min.y, _sr.bounds.max.y, finalY);
            Gizmos.color = new Color(1f, 1f, 0f, 0.8f);
            Gizmos.DrawLine(
                new Vector3(_sr.bounds.min.x, reflWorldY),
                new Vector3(_sr.bounds.max.x, reflWorldY));

            // 绘制模拟区域矩形（青色）
            if (enableSimulation)
            {
                Gizmos.color = new Color(0f, 1f, 1f, 0.5f);
                Bounds b = _sr.bounds;
                float simX0 = Mathf.Lerp(b.min.x, b.max.x, simulationUVs.x);
                float simY0 = Mathf.Lerp(b.min.y, b.max.y, simulationUVs.y);
                float simX1 = Mathf.Lerp(b.min.x, b.max.x, simulationUVs.z);
                float simY1 = Mathf.Lerp(b.min.y, b.max.y, simulationUVs.w);
                Vector3 simCenter = new Vector3((simX0 + simX1) / 2f, (simY0 + simY1) / 2f);
                Vector3 simSize   = new Vector3(simX1 - simX0, simY1 - simY0, 0.01f);
                Gizmos.DrawWireCube(simCenter, simSize);
            }
        }
#endif

        // ================================================================
        //  公共 API（供外部脚本调用）
        // ================================================================

        /// <summary>
        /// 动态切换主相机。切换后立即刷新相机矩阵。
        /// 可用于分屏、相机切换等场景。
        /// </summary>
        public void SetCamera(Camera cam)
        {
            mainCamera = cam;
            UpdateCameraMatrices();
            UpdateReflectionY();
        }

        /// <summary>
        /// 动态设置模拟贴图（由外部模拟系统调用）。
        /// </summary>
        public void SetSimulationTexture(RenderTexture rt, Vector4 uvs)
        {
            simulationTexture = rt;
            simulationUVs     = uvs;
            UpdateSimulation();
        }

        /// <summary>
        /// 动态设置波浪高度图（由外部波浪系统调用）。
        /// </summary>
        public void SetWavesHeightTexture(Texture tex)
        {
            wavesHeightTexture = tex;
            UpdateDynamicWaves();
        }

        /// <summary>
        /// 动态设置水面以下背景贴图（由相机渲染系统调用）。
        /// </summary>
        public void SetBelowWaterTexture(Texture tex, bool enable = true)
        {
            belowWaterTexture    = tex;
            enableBelowWaterTex  = enable;
        }
    }
}
