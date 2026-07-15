using UnityEngine;

namespace Main.FuncModule.Shadow
{
    /// <summary>
    /// 阴影的底部锚点选择：哪条边贴地。
    /// 决定了高度从哪个方向算起，以及阴影向哪个方向投射。
    /// </summary>
    public enum ShadowAnchor
    {
        NegativeY = 0,  // Y 轴负方向边（下边）为底 —— 默认，适用于角色、下墙
        PositiveY  = 1, // Y 轴正方向边（上边）为底 —— 适用于上墙（北墙）
        NegativeX  = 2, // X 轴负方向边（左边）为底 —— 适用于左墙（西墙）
        PositiveX  = 3, // X 轴正方向边（右边）为底 —— 适用于右墙（东墙）
    }

    /// <summary>
    /// 2D 地面阴影组件。挂载到含 SpriteRenderer 的 GameObject 上，
    /// 自动创建子对象并通过顶点 Shader 渲染投影阴影。
    /// 通过 ShadowAnchor 支持上下左右四个朝向的墙体。
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class ShadowRenderer : MonoBehaviour
    {
        [SerializeField] private ShadowConfig _config;

        [Tooltip("哪条边作为阴影的地面锚点（贴地边）")]
        [SerializeField] private ShadowAnchor _shadowAnchor = ShadowAnchor.NegativeY;

        [Tooltip("留空则自动使用源 SpriteRenderer 的 Sorting Layer")]
        [SerializeField] private string _shadowSortingLayer = "";

        [Tooltip("相对于源 SpriteRenderer sortingOrder 的偏移，负值确保阴影渲染在物体之下")]
        [SerializeField] private int _shadowSortingOrderOffset = -1;

        [Tooltip("锚点微调：沿高度方向移动锚点（正值=向内，负值=向外），单位：世界单位")]
        [SerializeField] private float _anchorOffset = 0f;

        [SerializeField] private Shader _shadowShader;

        private SpriteRenderer        _sourceSR;
        private SpriteRenderer        _shadowSR;
        private GameObject            _shadowGO;
        private MaterialPropertyBlock _mpb;

        private Sprite       _lastSprite;
        private ShadowAnchor _lastAnchor;
        private float        _lastRotationZ = float.NaN;
        private float        _lastHour = -999f;
        private bool         _shadowVisible = true;

        // Shader 属性 ID
        private static readonly int IdSunDir             = Shader.PropertyToID("_SunDir");
        private static readonly int IdShadowLength       = Shader.PropertyToID("_ShadowLength");
        private static readonly int IdShadowScaleY       = Shader.PropertyToID("_ShadowScaleY");
        private static readonly int IdShadowColor        = Shader.PropertyToID("_ShadowColor");
        private static readonly int IdAlphaCutoff        = Shader.PropertyToID("_AlphaCutoff");
        private static readonly int IdMainTex            = Shader.PropertyToID("_MainTex");
        private static readonly int IdAnchorAndHeightDir = Shader.PropertyToID("_AnchorAndHeightDir");
        private static readonly int IdTotalHeight        = Shader.PropertyToID("_TotalHeight");
        private static readonly int IdSinZ               = Shader.PropertyToID("_SinZ");
        private static readonly int IdCosZ               = Shader.PropertyToID("_CosZ");

        private void Awake()
        {
            _sourceSR = GetComponent<SpriteRenderer>();

            if (_config == null)
            {
                Debug.LogWarning($"[ShadowRenderer] {name}: 未设置 ShadowConfig，阴影禁用", this);
                enabled = false;
                return;
            }

            if (_shadowShader == null)
                _shadowShader = Shader.Find("Game/Shadow2D");

            if (_shadowShader == null)
            {
                Debug.LogError("[ShadowRenderer] 未能找到 Shader \"Game/Shadow2D\"，请检查 Shader 资产", this);
                enabled = false;
                return;
            }

            _mpb = new MaterialPropertyBlock();
            CreateShadowObject();
            GlobalShadowManager.Instance.Register(this);
        }

        private void OnDestroy()
        {
            if (_shadowGO != null) Destroy(_shadowGO);
            GlobalShadowManager.Instance?.Unregister(this);
        }

        private void OnBecameInvisible()
        {
            if (_shadowSR != null) _shadowSR.enabled = false;
        }

        private void OnBecameVisible()
        {
            if (_shadowSR != null) _shadowSR.enabled = _shadowVisible;
            _lastHour = -999f;
        }

        private void Update()
        {
            SyncGeometryChange();

            // if (WorldTimeController.Instance == null) return;

            // float hour = WorldTimeController.Instance.CurrentHour % 24f;
            // if (Mathf.Abs(hour - _lastHour) < 0.03f) return;
            // _lastHour = hour;

            // RefreshShadow(hour);
        }

        // ── 内部方法 ──────────────────────────────────────────────────────

        /// <summary>
        /// 检测 Sprite / 锚点方向 / 旋转变化，同步重算 Shader 几何参数。
        /// </summary>
        private void SyncGeometryChange()
        {
            var   currentSprite    = _sourceSR.sprite;
            var   currentAnchor    = _shadowAnchor;
            float currentRotationZ = transform.eulerAngles.z;

            if (currentSprite == _lastSprite
                && currentAnchor == _lastAnchor
                && Mathf.Approximately(currentRotationZ, _lastRotationZ)) return;

            _lastSprite    = currentSprite;
            _lastAnchor    = currentAnchor;
            _lastRotationZ = currentRotationZ;

            if (_shadowSR != null)
                _shadowSR.sprite = currentSprite;

            ApplySpriteMetrics(currentSprite);
            _lastHour = -999f;
        }

        private void RefreshShadow(float hour)
        {
            bool isDaytime = GlobalShadowManager.IsDaytime(hour);
            _shadowVisible = isDaytime;
            if (_shadowSR != null) _shadowSR.enabled = isDaytime;

            if (!isDaytime) return;

            Vector2 sunDir = GlobalShadowManager.CalcSunDir(hour);
            float   length = CalcLength(hour);

            _mpb.SetVector(IdSunDir,       new Vector4(sunDir.x, sunDir.y, 0f, 0f));
            _mpb.SetFloat (IdShadowLength, length);
            _shadowSR.SetPropertyBlock(_mpb);

            if (_shadowSR.flipX != _sourceSR.flipX)
                _shadowSR.flipX = _sourceSR.flipX;

            int expectedOrder = _sourceSR.sortingOrder + _shadowSortingOrderOffset;
            if (_shadowSR.sortingOrder != expectedOrder)
                _shadowSR.sortingOrder = expectedOrder;
        }

        private float CalcLength(float hour)
        {
            float norm = _config.lengthCurve.Evaluate(hour);
            return Mathf.Lerp(_config.shadowLengthMin, _config.shadowLengthMax, 1f - norm);
        }

        private void CreateShadowObject()
        {
            _shadowGO = new GameObject("Shadow");
            var t = _shadowGO.transform;
            t.SetParent(transform);
            t.localPosition = Vector3.zero;
            t.localRotation = Quaternion.identity;
            t.localScale    = Vector3.one;

            _shadowSR = _shadowGO.AddComponent<SpriteRenderer>();
            _shadowSR.sprite           = _sourceSR.sprite;
            _shadowSR.sortingLayerName = string.IsNullOrEmpty(_shadowSortingLayer)
                ? _sourceSR.sortingLayerName
                : _shadowSortingLayer;
            _shadowSR.sortingOrder     = _sourceSR.sortingOrder + _shadowSortingOrderOffset;
            _shadowSR.flipX            = _sourceSR.flipX;

            _shadowSR.sharedMaterial = new Material(_shadowShader);

            _mpb.SetFloat(IdShadowScaleY, _config.shadowScaleY);
            _mpb.SetColor(IdShadowColor,  _config.shadowColor);
            _mpb.SetFloat(IdAlphaCutoff,  _config.alphaCutoff);
            ApplySpriteMetrics(_sourceSR.sprite);

            _shadowSR.SetPropertyBlock(_mpb);

            _lastSprite    = _sourceSR.sprite;
            _lastAnchor    = _shadowAnchor;
            _lastRotationZ = transform.eulerAngles.z;
        }

        /// <summary>
        /// 根据 ShadowAnchor 枚举计算锚点坐标和高度方向，写入 Shader。
        ///
        /// 各墙朝向对应关系（以局部坐标为准）：
        ///   NegativeY：下边为底，高度向上（0,+1），适合角色、下墙（南墙）
        ///   PositiveY ：上边为底，高度向下（0,-1），适合上墙（北墙）
        ///   NegativeX ：左边为底，高度向右（+1,0），适合左墙（西墙）
        ///   PositiveX ：右边为底，高度向左（-1,0），适合右墙（东墙）
        /// </summary>
        private void ApplySpriteMetrics(Sprite sprite)
        {
            if (sprite == null) return;

            var     bounds  = sprite.bounds;
            bool    flipX   = _sourceSR.flipX;
            Vector2 anchor;
            Vector2 heightDir;
            float   totalHeight;

            switch (_shadowAnchor)
            {
                case ShadowAnchor.PositiveY:
                    anchor      = new Vector2(0f, bounds.max.y);
                    heightDir   = new Vector2(0f, -1f);
                    totalHeight = bounds.size.y;
                    break;

                case ShadowAnchor.NegativeX:
                    float leftX = flipX ? -bounds.max.x : bounds.min.x;
                    anchor      = new Vector2(leftX, 0f);
                    heightDir   = new Vector2(1f, 0f);
                    totalHeight = bounds.size.x;
                    break;

                case ShadowAnchor.PositiveX:
                    float rightX = flipX ? -bounds.min.x : bounds.max.x;
                    anchor       = new Vector2(rightX, 0f);
                    heightDir    = new Vector2(-1f, 0f);
                    totalHeight  = bounds.size.x;
                    break;

                default: // NegativeY
                    anchor      = new Vector2(0f, bounds.min.y);
                    heightDir   = new Vector2(0f, 1f);
                    totalHeight = bounds.size.y;
                    break;
            }

            // 沿高度方向微调锚点（正值 = 向墙内移动）
            anchor += heightDir * _anchorOffset;

            _mpb.SetVector (IdAnchorAndHeightDir,
                new Vector4(anchor.x, anchor.y, heightDir.x, heightDir.y));
            _mpb.SetFloat  (IdTotalHeight, Mathf.Max(totalHeight, 0.001f));
            _mpb.SetTexture(IdMainTex,     sprite.texture);

            // 旋转 sin/cos，供 shader 将世界空间太阳位移转回局部空间
            float rad = transform.eulerAngles.z * Mathf.Deg2Rad;
            _mpb.SetFloat(IdSinZ, Mathf.Sin(rad));
            _mpb.SetFloat(IdCosZ, Mathf.Cos(rad));
        }

        // ── 公开 API ──────────────────────────────────────────────────────

        public void SetSprite(Sprite sprite)
        {
            _sourceSR.sprite = sprite;
        }

        public void OverrideColor(Color color)
        {
            _mpb.SetColor(IdShadowColor, color);
            _shadowSR?.SetPropertyBlock(_mpb);
        }

        public void ResetColor()
        {
            _mpb.SetColor(IdShadowColor, _config.shadowColor);
            _shadowSR?.SetPropertyBlock(_mpb);
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (_sourceSR == null || _config == null) return;
            var b = _sourceSR.bounds;

            Vector3 anchorPt;
            Vector3 dir;
            switch (_shadowAnchor)
            {
                case ShadowAnchor.PositiveY:
                    anchorPt = new Vector3(b.center.x, b.max.y, 0f);
                    dir      = Vector3.up;
                    break;
                case ShadowAnchor.NegativeX:
                    anchorPt = new Vector3(b.min.x, b.center.y, 0f);
                    dir      = Vector3.left;
                    break;
                case ShadowAnchor.PositiveX:
                    anchorPt = new Vector3(b.max.x, b.center.y, 0f);
                    dir      = Vector3.right;
                    break;
                default:
                    anchorPt = new Vector3(b.center.x, b.min.y, 0f);
                    dir      = Vector3.down;
                    break;
            }
            UnityEditor.Handles.color = new Color(0.2f, 0.6f, 1f, 0.6f);
            UnityEditor.Handles.DrawAAPolyLine(3f, anchorPt, anchorPt + dir * _config.shadowLengthMax);
        }
#endif
    }
}
