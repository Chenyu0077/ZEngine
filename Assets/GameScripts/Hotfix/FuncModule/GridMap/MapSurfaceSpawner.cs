using System;
using Hotfix.Core;
using UnityEngine;
using ZEngine.Manager.Resource;

namespace Hotfix.FuncModule
{
    /// <summary>
    /// 运行时地图表面效果生成器。
    /// 如：地图加载后自动生成水面 SpriteRenderer
    /// 原理：扫描所有 terrainType=="water" 的格子，生成 alpha 遮罩贴图注入 waterg Shader，
    ///       单张全图 Quad 渲染，保证水面效果（泡沫/反射/扭曲）全局无缝。
    /// </summary>
    public class MapSurfaceSpawner : MonoBehaviour
    {
        private Material _waterMaterial;
        private Color    _spriteColor      = Color.white;
        private string   _waterTerrainType = "water";
        private string   _sortingLayerName = "Default";
        private int      _sortingOrder     = 5;

        private GameObject _waterGo;
        private Texture2D  _maskTex;
        private Texture2D  _whiteTex;
        private Sprite     _quadSprite;
        private Material   _matInstance;

        private void Awake()
        {
            if (_waterMaterial == null)
            {
                var handle = ResourceManager.Instance.LoadAssetSync<Material>(HotfixAssetPaths.MaterialPath + "WaterMat");
                _waterMaterial = handle?.AssetObject as Material;
            }
        }

        private void OnEnable()
        {
            MapLoader.Instance.OnMapLoaded += OnMapLoaded;
        }

        private void OnDisable()
        {
            MapLoader.Instance.OnMapLoaded -= OnMapLoaded;
            Cleanup();   
        }

        private void OnMapLoaded(MapSaveData mapData)
        {
            Cleanup();
            BuildWater(mapData);
        }

        private void BuildWater(MapSaveData mapData)
        {
            int w = mapData.width;
            int h = mapData.height;

            // 扫描水域格子，填充 alpha 遮罩（Texture2D y=0 在底部，与 Tilemap 对齐）
            var pixels   = new Color32[w * h];
            bool hasWater = false;

            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                var cell = MapLoader.Instance.GetCell(x, y);
                if (cell?.TerrainType == _waterTerrainType)
                {
                    pixels[y * w + x] = new Color32(255, 255, 255, 255);
                    hasWater = true;
                }
            }

            if (!hasWater) return;

            // 生成 alpha 遮罩贴图（Point 过滤保证像素边界清晰）
            _maskTex            = new Texture2D(w, h, TextureFormat.RGBA32, false);
            _maskTex.filterMode = FilterMode.Point;
            _maskTex.wrapMode   = TextureWrapMode.Clamp;
            _maskTex.SetPixels32(pixels);
            _maskTex.Apply();

            // 创建 1×1 白色 Sprite 作为 Quad 载体（Scale 控制实际大小，UV 自然 0→1）
            _whiteTex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            _whiteTex.SetPixel(0, 0, Color.white);
            _whiteTex.Apply();
            _quadSprite = Sprite.Create(_whiteTex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);

            // 实例化材质，注入 alpha 遮罩（不修改原始材质资产）
            _matInstance = GameObject.Instantiate(_waterMaterial);
            _matInstance.SetTexture("_alphaTexture", _maskTex);

            // 计算覆盖整张地图的位置与尺寸
            float   cs     = mapData.cellSize;
            float   totalW = w * cs;
            float   totalH = h * cs;
            Vector3 center = MapLoader.Instance.MapOrigin + new Vector3(totalW * 0.5f, totalH * 0.5f, 0f);

            _waterGo                        = new GameObject("WaterSurface");
            _waterGo.transform.position     = center;
            _waterGo.transform.localScale   = new Vector3(totalW, totalH, 1f);

            var sr                = _waterGo.AddComponent<SpriteRenderer>();
            sr.sprite             = _quadSprite;
            sr.color              = _spriteColor;
            sr.material           = _matInstance;
            sr.sortingLayerName   = _sortingLayerName;
            sr.sortingOrder       = _sortingOrder;
        }

        private void Cleanup()
        {
            if (_waterGo     != null) { GameObject.Destroy(_waterGo);      _waterGo     = null; }
            if (_matInstance != null) { GameObject.Destroy(_matInstance);  _matInstance = null; }
            if (_maskTex     != null) { GameObject.Destroy(_maskTex);      _maskTex     = null; }
            if (_quadSprite  != null) { GameObject.Destroy(_quadSprite);   _quadSprite  = null; }
            if (_whiteTex    != null) { GameObject.Destroy(_whiteTex);     _whiteTex    = null; }
        }
    }
}
