// ============================================================
//  WaterObstructor.cs
//  水体障碍物标记组件
//
//  使用方法：
//    将此组件挂载到任何需要在水面产生描边的障碍物上。
//    组件会自动将该物体切换到 "Obstructors" 图层，
//    使其被 WaterObstructionRenderer 的专用相机渲染到遮罩贴图。
//
//  注意：
//    - 障碍物的 Renderer 必须使用不透明材质（或带白色 Alpha 的材质）
//    - 切换图层后该物体不再被主相机渲染（需要双层方案，见 useShadowCopy 说明）
// ============================================================

using UnityEngine;

namespace GameLogic
{
    [ExecuteAlways]
    public class WaterObstructor : MonoBehaviour
    {
        [Header("── 障碍物设置 ──────────────────────")]

        [Tooltip("是否创建一个影子副本保持原始物体在主相机中可见。\n" +
                 "关闭：物体被移到 Obstructors 图层，在主相机中不可见（适合纯水下障碍物）。\n" +
                 "开启：保留原始物体，额外创建一个只在 Obstructors 图层渲染的副本（推荐）。")]
        public bool useShadowCopy = true;

        [Tooltip("保存切换前的原始图层，用于还原。")]
        [HideInInspector]
        public int originalLayer;

        // ────────────────────────────────────────────────────
        //  私有字段
        // ────────────────────────────────────────────────────

        private GameObject _shadowCopy;   // useShadowCopy=true 时创建的副本

        // ================================================================
        //  Unity 生命周期
        // ================================================================

        void OnEnable()
        {
            originalLayer = gameObject.layer;
            Setup();
        }

        void OnDisable()
        {
            Cleanup();
        }

        void OnDestroy()
        {
            Cleanup();
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            // Inspector 中修改 useShadowCopy 时立即重新配置
            Cleanup();
            Setup();
        }
#endif

        // ================================================================
        //  核心逻辑
        // ================================================================

        /// <summary>
        /// 根据 useShadowCopy 模式设置障碍物的图层
        /// </summary>
        void Setup()
        {
            int obsLayerIdx = LayerMask.NameToLayer(WaterObstructionRenderer.ObstructorLayerName);
            if (obsLayerIdx == -1)
            {
                Debug.LogWarning($"[WaterObstructor] 图层 '{WaterObstructionRenderer.ObstructorLayerName}' " +
                                 "不存在！请先在场景中添加 WaterObstructionRenderer 组件（会自动创建图层）。");
                return;
            }

            if (useShadowCopy)
            {
                // 模式1：创建副本放到 Obstructors 图层，原物体保持原始图层不变
                CreateShadowCopy(obsLayerIdx);
            }
            else
            {
                // 模式2：将自身直接切换到 Obstructors 图层
                SetLayerRecursively(gameObject, obsLayerIdx);
            }
        }

        /// <summary>
        /// 还原图层设置，销毁副本
        /// </summary>
        void Cleanup()
        {
            // 还原原始图层（仅在非副本模式下）
            if (!useShadowCopy)
                SetLayerRecursively(gameObject, originalLayer);

            // 销毁副本
            if (_shadowCopy != null)
            {
                if (Application.isPlaying) Destroy(_shadowCopy);
                else DestroyImmediate(_shadowCopy);
                _shadowCopy = null;
            }
        }

        /// <summary>
        /// 创建一个轻量级副本，仅用于渲染到障碍物遮罩贴图。
        /// 副本：
        ///   - 只保留 Renderer 组件（无脚本）
        ///   - 放置在 Obstructors 图层（主相机不渲染）
        ///   - 跟随原物体 Transform 同步位置
        /// </summary>
        void CreateShadowCopy(int obsLayerIdx)
        {
            if (_shadowCopy != null) return;

            _shadowCopy = new GameObject($"[OBS] {gameObject.name}");
            _shadowCopy.hideFlags = HideFlags.DontSave | HideFlags.NotEditable;

            // 只复制 Renderer 相关组件（不复制脚本和碰撞体）
            foreach (var renderer in GetComponentsInChildren<Renderer>())
            {
                var copyGo = new GameObject(renderer.gameObject.name);
                copyGo.transform.SetParent(_shadowCopy.transform);
                copyGo.layer = obsLayerIdx;

                // 复制 Mesh（根据 Renderer 类型）
                if (renderer is SpriteRenderer sr)
                {
                    var copySr = copyGo.AddComponent<SpriteRenderer>();
                    copySr.sprite        = sr.sprite;
                    copySr.color         = Color.white; // 遮罩只需要形状，统一用白色
                    copySr.sortingOrder  = sr.sortingOrder;
                }
                else if (renderer is MeshRenderer mr)
                {
                    var mf = renderer.GetComponent<MeshFilter>();
                    if (mf != null)
                    {
                        copyGo.AddComponent<MeshFilter>().sharedMesh = mf.sharedMesh;
                        var copyMr = copyGo.AddComponent<MeshRenderer>();
                        // 使用白色 Unlit 材质，只渲染形状轮廓
                        copyMr.sharedMaterial = GetWhiteMaterial();
                    }
                }
            }

            // 注册跟随更新（副本每帧同步原物体的世界变换）
            StartCoroutine(FollowOriginal());
        }

        /// <summary>
        /// 每帧同步副本的世界变换与原物体一致
        /// </summary>
        System.Collections.IEnumerator FollowOriginal()
        {
            while (_shadowCopy != null && this != null)
            {
                _shadowCopy.transform.position   = transform.position;
                _shadowCopy.transform.rotation   = transform.rotation;
                _shadowCopy.transform.localScale  = transform.lossyScale;
                yield return null;
            }
        }

        // ================================================================
        //  工具函数
        // ================================================================

        /// <summary>
        /// 递归设置 GameObject 及所有子物体的图层
        /// </summary>
        static void SetLayerRecursively(GameObject go, int layer)
        {
            go.layer = layer;
            foreach (Transform child in go.transform)
                SetLayerRecursively(child.gameObject, layer);
        }

        /// <summary>
        /// 获取白色 Unlit 材质（缓存复用，避免每帧创建）
        /// </summary>
        static Material _whiteMat;
        static Material GetWhiteMaterial()
        {
            if (_whiteMat == null)
            {
                _whiteMat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                _whiteMat.color = Color.white;
                _whiteMat.name  = "_WaterObstructorWhite";
            }
            return _whiteMat;
        }
    }
}
