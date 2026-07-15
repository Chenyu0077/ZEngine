using System;
using System.Collections.Generic;
using Main.Core;
using UnityEngine;
using UnityEngine.Rendering;
using ZEngine.Manager.Pool;
using ZEngine.Core;
using ZEngine.Manager.Log;
using Main.FuncModule.Building;

namespace Hotfix.FuncModule.Building
{
    /// <summary>
    /// 放置物管理器
    /// </summary>
    [DefaultExecutionOrder(50)]
    public class PlacedObjManager : BehaviourSingleton<PlacedObjManager>
    {
        [Header("依赖")]
        [SerializeField] private Camera _worldCamera;

        private static MapLoader Map => MapLoader.Instance;

        [Header("Ghost 颜色")]
        public Color GhostValidColor   = new Color(0.10f, 0.90f, 0.10f, 0.40f);
        public Color GhostInvalidColor = new Color(0.90f, 0.10f, 0.10f, 0.40f);
        public Color GhostRemoveColor  = new Color(0.90f, 0.35f, 0.10f, 0.50f);
        public Color GhostBorderColor  = new Color(1.00f, 1.00f, 1.00f, 0.85f);

        public struct RegisteredItem
        {
            public cfg.PlacedItem Config;
            public GameObject        PrefabFallback;
        }

        public event Action<bool>           OnModeChanged;
        public event Action<PlacedObj> OnBuildingPlaced;
        public event Action<PlacedObj> OnBuildingRemoved;

        public bool InBuildingMode { get; private set; }
        public bool RemoveMode     { get; private set; }

        private readonly List<RegisteredItem>             _registeredItems = new List<RegisteredItem>();
        private readonly Dictionary<long, PlacedObj> _occupancy       = new Dictionary<long, PlacedObj>();
        private readonly List<PlacedObj>             _buildings        = new List<PlacedObj>();

        private cfg.PlacedItem _selectedConfig;
        private int               _selectedSizeX = 1;
        private int               _selectedSizeY = 1;

        private Vector2Int _hoverGrid;
        private Vector2Int _lastValidHoverGrid;  // 上次有效格子，Ghost 离开格子时保持在最后位置
        private bool       _hoverValid;
        private bool       _mouseOnGrid;

        private Material _glMat;

        #region ── 公共 API ──────────────────────────────────────────────────────────

        public IReadOnlyList<RegisteredItem> Items => _registeredItems;

        public void RegisterItem(cfg.PlacedItem config, GameObject prefabFallback = null)
        {
            _registeredItems.Add(new RegisteredItem { Config = config, PrefabFallback = prefabFallback });
        }

        public void EnterBuildingMode()
        {
            InBuildingMode = true;
            OnModeChanged?.Invoke(true);
        }

        public void ExitBuildingMode()
        {
            InBuildingMode = false;
            Deselect();
            RemoveMode = false;
            OnModeChanged?.Invoke(false);
        }

        public void SelectBuilding(cfg.PlacedItem config)
        {
            _selectedConfig = config;
            RemoveMode      = false;
            var prefab = FindPrefabFallback(config.Id);
            var bc     = prefab != null ? prefab.GetComponent<PlacedObjConfig>() : null;
            _selectedSizeX = bc != null ? bc.SizeX : 1;
            _selectedSizeY = bc != null ? bc.SizeY : 1;
        }

        public void Deselect()
        {
            _selectedConfig = null;
            _selectedSizeX  = 1;
            _selectedSizeY  = 1;
        }

        public void ToggleRemoveMode()
        {
            RemoveMode = !RemoveMode;
            Deselect();
        }

        /// <summary>
        /// 检查指定区域是否可放置。
        /// 读 MapLoader.CellRuntime.GetBool("buildable") 作为设计时可建标记，
        /// _occupancy 字典作为运行时已占用检查。
        /// </summary>
        public bool CanPlace(int x, int y, int sizeX, int sizeY)
        {
            for (int dx = 0; dx < sizeX; dx++)
            for (int dy = 0; dy < sizeY; dy++)
            {
                int cx = x + dx, cy = y + dy;
                if (!Map.IsValid(cx, cy)) return false;
                var cell = Map.GetCell(cx, cy);
                if (cell == null || !cell.GetBool("buildable")) return false;
                if (_occupancy.ContainsKey(CellKey(cx, cy))) return false;
            }
            return true;
        }

        #endregion
        


        #region ── Unity 生命周期 ────────────────────────────────────────────────────

        private void OnEnable()  => RenderPipelineManager.endCameraRendering += OnSRPEndCamera;
        private void OnDisable() => RenderPipelineManager.endCameraRendering -= OnSRPEndCamera;

        private void OnDestroy()
        {
            if (_glMat != null) DestroyImmediate(_glMat);
        }

        private void Update()
        {
            if (!InBuildingMode) return;

            _mouseOnGrid = TryGetMouseGridPos(out _hoverGrid);
            if (_mouseOnGrid) _lastValidHoverGrid = _hoverGrid;

            if (!RemoveMode && _selectedConfig != null)
            {
                _hoverValid = _mouseOnGrid
                              && _selectedConfig.CanPlace
                              && CanPlace(_hoverGrid.x, _hoverGrid.y, _selectedSizeX, _selectedSizeY);

                if (Input.GetMouseButtonDown(0) && _hoverValid)
                    DoPlace(_hoverGrid.x, _hoverGrid.y);
                else if (Input.GetMouseButtonDown(1))
                    Deselect();
            }
            else if (RemoveMode)
            {
                _hoverValid = _mouseOnGrid && _occupancy.ContainsKey(CellKey(_hoverGrid.x, _hoverGrid.y));

                if (Input.GetMouseButtonDown(0) && _hoverValid)
                    TryRemoveBuildingAt(_hoverGrid.x, _hoverGrid.y);
            }

            if (Input.GetKeyDown(KeyCode.Escape))
                ExitBuildingMode();
        }

        #endregion


        #region ── 内部：放置 / 移除 ────────────────────────────────────────────────

        private void DoPlace(int x, int y)
        {
            if (_selectedConfig == null || !_selectedConfig.CanPlace) return;

            var placed = new PlacedObj
            {
                ConfigId = _selectedConfig.Id,
                GridX    = x,
                GridY    = y,
                SizeX    = _selectedSizeX,
                SizeY    = _selectedSizeY,
            };

            _buildings.Add(placed);
            OccupyCells(placed);
            SpawnVisual(_selectedConfig, FindPrefabFallback(_selectedConfig.Id), x, y, _selectedSizeX, _selectedSizeY, placed);
            OnBuildingPlaced?.Invoke(placed);
        }

        private void TryRemoveBuildingAt(int x, int y)
        {
            if (!_occupancy.TryGetValue(CellKey(x, y), out var placed)) return;

            var config = FindConfig(placed.ConfigId);
            if (config != null && !config.CanRemove)
            {
                LogManager.Instance.Info($"[BuildingManager] 建筑 {placed.ConfigId}（{config.Name}）不允许移除");
                return;
            }

            FreeCells(placed);
            _buildings.Remove(placed);
            ReturnToPool(placed);
            OnBuildingRemoved?.Invoke(placed);
        }

        #endregion


        #region ── 内部：对象池 / 直接实例化 ────────────────────────────────────────

        private void SpawnVisual(cfg.PlacedItem config, GameObject prefabFallback,
            int x, int y, int sizeX, int sizeY, PlacedObj placed)
        {
            var origin = Map.MapOrigin;
            float cs = Map.CellSize;

            float worldX = origin.x + (x + sizeX * 0.5f) * cs;
            float worldY = origin.y + y * cs;

            var sr = prefabFallback != null ? prefabFallback.GetComponentInChildren<SpriteRenderer>() : null;
            if (sr != null && sr.sprite != null)
                worldY += sr.sprite.pivot.y * sr.sprite.bounds.size.y;

            var worldPos = new Vector3(worldX, worldY, origin.z);

            bool usePool = !string.IsNullOrEmpty(config.Path) && ZEngineMain.Contains<ObjectPoolManager>();

            if (usePool)
            {
                var spawn = ObjectPoolManager.Instance.Spawn(config.Path, tag: "building");
                spawn.Completed += (s) =>
                {
                    if (s.Go == null) return;
                    s.Go.transform.SetPositionAndRotation(worldPos, Quaternion.identity);
                    s.Go.transform.SetParent(transform);
                    s.Go.SetActive(true);
                    placed.SpawnHandle = s;
                    placed.Instance    = s.Go;
                };
            }
            else if (prefabFallback != null)
            {
                var go = Instantiate(prefabFallback, worldPos, Quaternion.identity, transform);
                go.SetActive(true);
                placed.Instance = go;
            }
        }

        private void ReturnToPool(PlacedObj placed)
        {
            if (placed.SpawnHandle != null)
                placed.SpawnHandle.Restore();
            else if (placed.Instance != null)
                Destroy(placed.Instance);
        }

        #endregion


        #region ── 内部：辅助 ───────────────────────────────────────────────────────

        private cfg.PlacedItem FindConfig(string configId)
        {
            foreach (var item in _registeredItems)
                if (item.Config.Id == configId) return item.Config;
            return null;
        }

        private GameObject FindPrefabFallback(string configId)
        {
            foreach (var item in _registeredItems)
                if (item.Config.Id == configId) return item.PrefabFallback;
            return null;
        }

        private static long CellKey(int x, int y) => ((long)x << 32) | (uint)y;

        private void OccupyCells(PlacedObj b)
        {
            for (int dx = 0; dx < b.SizeX; dx++)
            for (int dy = 0; dy < b.SizeY; dy++)
                _occupancy[CellKey(b.GridX + dx, b.GridY + dy)] = b;
        }

        private void FreeCells(PlacedObj b)
        {
            for (int dx = 0; dx < b.SizeX; dx++)
            for (int dy = 0; dy < b.SizeY; dy++)
                _occupancy.Remove(CellKey(b.GridX + dx, b.GridY + dy));
        }

        private bool TryGetMouseGridPos(out Vector2Int gridPos)
        {
            gridPos = default;
            if (!Map.IsLoaded) return false;

            // _worldCamera 未赋值时自动回退到主相机
            var cam = _worldCamera != null ? _worldCamera : Camera.main;
            if (cam == null) return false;

            var mouseScreen = Input.mousePosition;
            mouseScreen.z   = Mathf.Abs(cam.transform.position.z);
            var worldPos    = cam.ScreenToWorldPoint(mouseScreen);
            gridPos         = Map.WorldToGrid(worldPos);
            return Map.IsValid(gridPos.x, gridPos.y);
        }

        #endregion


        #region ── GL 渲染（网格叠加 + Ghost 预览）──────────────────────────────────────

        private void OnSRPEndCamera(ScriptableRenderContext ctx, Camera cam)
        {
            if (!InBuildingMode || cam.cameraType == CameraType.SceneView) return;
            if (Map == null || !Map.IsLoaded) return;

            EnsureGLMaterial();

            // 1. 网格线 + 可建性颜色叠加（全图）
            DrawBuildingGrid(cam);

            // 2. Ghost 预览：配置已选中即显示（用上次有效格子位置，避免鼠标离开时消失）
            if (!RemoveMode && _selectedConfig != null)
            {
                var pos   = _mouseOnGrid ? _hoverGrid : _lastValidHoverGrid;
                var color = (_mouseOnGrid && _hoverValid) ? GhostValidColor : GhostInvalidColor;
                DrawGhostRect(cam, pos.x, pos.y, _selectedSizeX, _selectedSizeY, color);
            }
            else if (RemoveMode && _mouseOnGrid && _hoverValid)
            {
                if (_occupancy.TryGetValue(CellKey(_hoverGrid.x, _hoverGrid.y), out var hovered))
                    DrawGhostRect(cam, hovered.GridX, hovered.GridY, hovered.SizeX, hovered.SizeY, GhostRemoveColor);
            }
        }

        // ── 网格线 + 可建性颜色叠加 ─────────────────────────────────────
        private static readonly Color CellBuildableColor  = new Color(0.20f, 0.90f, 0.20f, 0.18f);
        private static readonly Color CellOccupiedColor   = new Color(0.90f, 0.55f, 0.10f, 0.35f);
        private static readonly Color CellForbidColor     = new Color(0.90f, 0.15f, 0.15f, 0.30f);
        private static readonly Color GridLineColor        = new Color(0.80f, 0.80f, 0.80f, 0.25f);

        private void DrawBuildingGrid(Camera cam)
        {
            _glMat.SetPass(0);
            GL.PushMatrix();
            GL.LoadProjectionMatrix(cam.projectionMatrix);
            GL.modelview = cam.worldToCameraMatrix;

            float cs = Map.CellSize;
            var   o  = Map.MapOrigin;
            int   mw = Map.MapWidth;
            int   mh = Map.MapHeight;
            float z  = o.z - 0.01f;  // 略低于 Tilemap 层

            // 格子颜色填充
            GL.Begin(GL.TRIANGLES);
            for (int y = 0; y < mh; y++)
            for (int x = 0; x < mw; x++)
            {
                Color c;
                if (_occupancy.ContainsKey(CellKey(x, y)))
                    c = CellOccupiedColor;
                else
                {
                    var cell = Map.GetCell(x, y);
                    c = (cell != null && cell.GetBool("buildable")) ? CellBuildableColor : CellForbidColor;
                }

                float x0 = o.x + x * cs, y0 = o.y + y * cs;
                float x1 = x0 + cs,       y1 = y0 + cs;
                GL.Color(c);
                GL.Vertex3(x0, y0, z); GL.Vertex3(x1, y0, z); GL.Vertex3(x1, y1, z);
                GL.Vertex3(x0, y0, z); GL.Vertex3(x1, y1, z); GL.Vertex3(x0, y1, z);
            }
            GL.End();

            // 网格线
            GL.Begin(GL.LINES);
            GL.Color(GridLineColor);
            float tw = mw * cs, th = mh * cs;
            for (int yi = 0; yi <= mh; yi++)
            {
                GL.Vertex3(o.x,      o.y + yi * cs, z);
                GL.Vertex3(o.x + tw, o.y + yi * cs, z);
            }
            for (int xi = 0; xi <= mw; xi++)
            {
                GL.Vertex3(o.x + xi * cs, o.y,      z);
                GL.Vertex3(o.x + xi * cs, o.y + th, z);
            }
            GL.End();

            GL.PopMatrix();
        }

        private void DrawGhostRect(Camera cam, int x, int y, int w, int h, Color fill)
        {
            _glMat.SetPass(0);
            GL.PushMatrix();
            GL.LoadProjectionMatrix(cam.projectionMatrix);
            GL.modelview = cam.worldToCameraMatrix;

            float cs     = Map.CellSize;
            var   center = Map.GridToWorld(x, y);
            float x0     = center.x - cs * 0.5f;
            float y0     = center.y - cs * 0.5f;
            float x1     = x0 + w * cs;
            float y1     = y0 + h * cs;
            float z      = Map.MapOrigin.z;

            GL.Begin(GL.TRIANGLES);
            GL.Color(fill);
            GL.Vertex3(x0, y0, z); GL.Vertex3(x1, y0, z); GL.Vertex3(x1, y1, z);
            GL.Vertex3(x0, y0, z); GL.Vertex3(x1, y1, z); GL.Vertex3(x0, y1, z);
            GL.End();

            GL.Begin(GL.LINE_STRIP);
            GL.Color(GhostBorderColor);
            GL.Vertex3(x0, y0, z); GL.Vertex3(x1, y0, z);
            GL.Vertex3(x1, y1, z); GL.Vertex3(x0, y1, z);
            GL.Vertex3(x0, y0, z);
            GL.End();

            GL.PopMatrix();
        }

        private void EnsureGLMaterial()
        {
            if (_glMat != null) return;
            _glMat = new Material(Shader.Find("Hidden/Internal-Colored"))
                { hideFlags = HideFlags.HideAndDontSave };
            // ZTest Always：忽略深度缓冲，强制绘制在 Tilemap Sprite 顶层
            _glMat.SetInt("_ZTest", (int)UnityEngine.Rendering.CompareFunction.Always);
        }

        #endregion
    }
}
