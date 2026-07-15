// ============================================================
//  WaterObstructionRenderer.cs
//  水体障碍物遮罩渲染器
//
//  功能：
//    将场景中的障碍物（石块、木桩、墙壁等）渲染到一张 RenderTexture，
//    然后注入到水体 Shader 的 _OBStexture 全局变量。
//    Shader 通过比较贴图上下两行的差值，在障碍物底边产生发光描边效果。
//
//  使用步骤：
//    1. 创建一个空 GameObject，挂载此脚本。
//    2. 将需要产生描边的障碍物放到 "Obstructors" Layer（脚本会自动创建该层）。
//    3. 给障碍物挂上 WaterObstructor 组件（同目录下），它会自动切换图层。
//    4. 在水体材质 Inspector 中启用"开启障碍物描边"并调整颜色和宽度。
//    5. WaterController 脚本会自动设置 _obs_transform，确认场景中有 WaterController。
// ============================================================

using UnityEngine;

namespace GameLogic
{
    [ExecuteAlways]
    public class WaterObstructionRenderer : MonoBehaviour
    {
        // ────────────────────────────────────────────────────
        //  Inspector
        // ────────────────────────────────────────────────────

        [Header("── 相机设置 ──────────────────────────")]

        [Tooltip("渲染障碍物遮罩所用的正交相机。\n" +
                 "建议与场景主相机同步位置和大小，确保遮罩与场景对齐。\n" +
                 "留空则运行时自动创建。")]
        public Camera obstructionCamera;

        [Tooltip("渲染遮罩时需要跟随的主相机（通常为 Camera.main）。\n" +
                 "障碍物相机每帧同步此相机的位置和尺寸。")]
        public Camera mainCamera;

        [Tooltip("遮罩贴图分辨率。越高边缘越精细，性能消耗越大。\n" +
                 "推荐：512×512 或与屏幕分辨率一致。")]
        public Vector2Int resolution = new Vector2Int(512, 512);

        [Tooltip("相机覆盖范围相对主相机的放大倍率（>1 覆盖更大区域）。\n" +
                 "通常设为 1.25，为边缘留出余量，防止贴图边界处描边截断。")]
        [Range(1f, 2f)]
        public float sizeMLP = 1.25f;

        [Header("── 调试 ──────────────────────────────")]

        [Tooltip("在 Inspector 中预览生成的障碍物遮罩贴图（仅编辑器可见）。")]
        public bool showDebugTexture = false;

        // ────────────────────────────────────────────────────
        //  私有字段
        // ────────────────────────────────────────────────────

        private RenderTexture _rt;
        private bool          _cameraCreatedByUs;

        // 障碍物图层名称（与 ObstructorManager 保持一致）
        public const string ObstructorLayerName = "Obstructors";

        // 障碍物使用的纯色材质（白色，只需要遮挡形状）
        private static Material _obsMaterial;

        // Shader 全局变量 ID
        static readonly int ID_OBStexture   = Shader.PropertyToID("_OBStexture");
        static readonly int ID_obsTransform = Shader.PropertyToID("_obs_transform");

        // ================================================================
        //  Unity 生命周期
        // ================================================================

        void OnEnable()
        {
            EnsureLayer();
            EnsureCamera();
            EnsureRenderTexture();
            BindToShader();
        }

        void OnDisable()
        {
            // 清空全局贴图，避免残留
            Shader.SetGlobalTexture(ID_OBStexture, null);

            if (_cameraCreatedByUs && obstructionCamera != null)
            {
                if (Application.isPlaying) Destroy(obstructionCamera.gameObject);
                else DestroyImmediate(obstructionCamera.gameObject);
                obstructionCamera = null;
            }

            ReleaseRT();
        }

        void OnDestroy()
        {
            ReleaseRT();
        }

        void Update()
        {
            if (obstructionCamera == null || _rt == null) return;

            Camera cam = GetMainCamera();
            if (cam == null) return;

            // 每帧同步障碍物相机与主相机的位置和大小
            SyncCameraWithMain(cam);

            // 渲染本帧的障碍物遮罩
            obstructionCamera.Render();

            // 更新 _obs_transform（每帧更新以应对相机运动）
            UpdateObsTransform();
        }

#if UNITY_EDITOR
        void OnDrawGizmosSelected()
        {
            Camera cam = GetMainCamera();
            if (cam == null || !cam.orthographic) return;

            // 画出障碍物相机的覆盖范围（比主相机稍大）
            float h = cam.orthographicSize * 2f * sizeMLP;
            float w = h * cam.aspect;
            Vector3 pos = cam.transform.position;
            pos.z = 0;

            Gizmos.color = new Color(1f, 0.5f, 0f, 0.5f);
            Gizmos.DrawWireCube(pos, new Vector3(w, h, 0.1f));
        }

        void OnGUI()
        {
            if (!showDebugTexture || _rt == null) return;
            // 左下角显示障碍物遮罩预览
            GUI.DrawTexture(new Rect(10, Screen.height - 170, 160, 160), _rt);
        }
#endif

        // ================================================================
        //  初始化
        // ================================================================

        /// <summary>
        /// 确保 "Obstructors" 图层存在（编辑器中自动创建）
        /// </summary>
        void EnsureLayer()
        {
#if UNITY_EDITOR
            AddLayerIfMissing(ObstructorLayerName);
#endif
        }

        /// <summary>
        /// 确保障碍物相机存在并配置正确
        /// </summary>
        void EnsureCamera()
        {
            if (obstructionCamera != null) return;

            // 自动创建障碍物专用相机
            var go = new GameObject("[Water] ObstructionCamera");
            go.hideFlags = HideFlags.DontSave;
            go.transform.SetParent(transform);

            obstructionCamera = go.AddComponent<Camera>();
            _cameraCreatedByUs = true;

            ConfigureCamera();
        }

        /// <summary>
        /// 配置障碍物相机参数
        /// </summary>
        void ConfigureCamera()
        {
            if (obstructionCamera == null) return;

            int obsLayerIdx = LayerMask.NameToLayer(ObstructorLayerName);

            obstructionCamera.orthographic    = true;
            obstructionCamera.clearFlags      = CameraClearFlags.SolidColor;
            obstructionCamera.backgroundColor = Color.clear;
            obstructionCamera.nearClipPlane   = -100f;
            obstructionCamera.farClipPlane    = 100f;
            obstructionCamera.enabled         = false;   // 手动调用 Render()，不自动渲染
            obstructionCamera.targetTexture   = _rt;

            // 只渲染 Obstructors 图层
            if (obsLayerIdx != -1)
                obstructionCamera.cullingMask = 1 << obsLayerIdx;
            else
                Debug.LogWarning($"[WaterObstruction] 图层 '{ObstructorLayerName}' 不存在！" +
                                 "请在 Project Settings → Tags and Layers 中添加该图层。");

            // 深度比主相机低，优先渲染
            Camera main = GetMainCamera();
            if (main != null)
                obstructionCamera.depth = main.depth - 1;
        }

        /// <summary>
        /// 创建或重建 RenderTexture
        /// </summary>
        void EnsureRenderTexture()
        {
            if (_rt != null &&
                _rt.width  == resolution.x &&
                _rt.height == resolution.y) return;

            ReleaseRT();

            // RG16：R通道=障碍物遮罩，G通道=深度（可扩展）
            _rt = new RenderTexture(resolution.x, resolution.y, 0,
                                    RenderTextureFormat.RG16)
            {
                name        = "Water_OBStexture",
                filterMode  = FilterMode.Bilinear,
                wrapMode    = TextureWrapMode.Clamp
            };
            _rt.Create();

            if (obstructionCamera != null)
                obstructionCamera.targetTexture = _rt;
        }

        void ReleaseRT()
        {
            if (_rt == null) return;
            _rt.Release();
            if (Application.isPlaying) Destroy(_rt);
            else DestroyImmediate(_rt);
            _rt = null;
        }

        /// <summary>
        /// 将贴图绑定到 Shader 全局变量
        /// </summary>
        void BindToShader()
        {
            if (_rt == null) return;
            Shader.SetGlobalTexture(ID_OBStexture, _rt);
            UpdateObsTransform();
        }

        // ================================================================
        //  每帧更新
        // ================================================================

        /// <summary>
        /// 同步障碍物相机与主相机的位置/朝向/尺寸，并放大 sizeMLP 倍
        /// </summary>
        void SyncCameraWithMain(Camera main)
        {
            Transform ot = obstructionCamera.transform;
            Transform mt = main.transform;

            // 跟随主相机位置和旋转
            ot.position = mt.position;
            ot.rotation = mt.rotation;

            // 放大覆盖范围（确保边缘障碍物也被渲染到贴图中）
            obstructionCamera.orthographicSize = main.orthographicSize * sizeMLP;
            obstructionCamera.aspect           = main.aspect;
        }

        /// <summary>
        /// 计算并注入 _obs_transform
        ///
        /// 原理：障碍物相机比主相机大 sizeMLP 倍，
        /// 所以主相机的屏幕UV [0,1] 需要缩放并居中才能对应障碍物贴图的UV。
        ///
        /// 公式：obsUV = offset + scale * screenUV
        ///   scale  = 1 / sizeMLP
        ///   offset = (1 - scale) / 2
        ///
        /// Shader 中读取方式：
        ///   float2 obsUV = _obs_transform.wz + _obs_transform.xy * screenUV;
        ///   （注意：.xy=scale, .wz=offset，注意分量顺序）
        /// </summary>
        void UpdateObsTransform()
        {
            float scale  = 1f / sizeMLP;
            float offset = (1f - scale) * 0.5f;

            // x=scaleX, y=scaleY, z=offsetY(→.z), w=offsetX(→.w)
            // Shader 中：obsUV.x = _obs_transform.w + _obs_transform.x * screenUV.x
            //            obsUV.y = _obs_transform.z + _obs_transform.y * screenUV.y
            Shader.SetGlobalVector(ID_obsTransform,
                new Vector4(scale, scale, offset, offset));
        }

        // ================================================================
        //  工具
        // ================================================================

        Camera GetMainCamera()
        {
            if (mainCamera != null) return mainCamera;
            return Camera.main;
        }

#if UNITY_EDITOR
        /// <summary>
        /// 在 Project Settings → Tags and Layers 中添加图层（仅编辑器可用）
        /// </summary>
        static void AddLayerIfMissing(string layerName)
        {
            if (LayerMask.NameToLayer(layerName) != -1) return; // 已存在

            var tagManager = new UnityEditor.SerializedObject(
                UnityEditor.AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);

            var layers = tagManager.FindProperty("layers");
            for (int i = 8; i < layers.arraySize; i++) // 0~7 是内置层
            {
                var layer = layers.GetArrayElementAtIndex(i);
                if (string.IsNullOrEmpty(layer.stringValue))
                {
                    layer.stringValue = layerName;
                    tagManager.ApplyModifiedProperties();
                    Debug.Log($"[WaterObstruction] 自动创建图层: '{layerName}' (index {i})");
                    return;
                }
            }
            Debug.LogWarning($"[WaterObstruction] 无法自动创建图层 '{layerName}'，图层槽已满！" +
                             "请在 Project Settings → Tags and Layers 手动添加。");
        }
#endif
    }
}
