using System.Collections.Generic;
using System.IO;
using Main.FuncModule.Building;
using Main.FuncModule;
using UnityEditor;
using UnityEngine;

namespace GameScripts.Editor
{
    public class ObjectPalettePanel
    {
        private struct PrefabEntry
        {
            public string guid;
            public string assetPath;
            public string name;
            public string folder;
            public string prefabId;
        }

private class PrefabPreviewCache
        {
            public GameObject prefab;
            public Texture2D  preview;
            public Texture2D  fallback;
            public PlacedObjConfig PlacedObjConfig;
            public Sprite     spriteFromRenderer;
            public bool       previewRequested;
            public bool       previewResolved;
        }

        private readonly List<PrefabEntry>        _prefabs       = new List<PrefabEntry>();
        private readonly Dictionary<string, bool> _folderFoldout = new Dictionary<string, bool>();
        private readonly Dictionary<string, PrefabPreviewCache> _previewCache = new Dictionary<string, PrefabPreviewCache>();
        private string                            _lastPath      = null;
        private const string                      RootFolderName = "根目录";
        private const int                         IconSize       = 64;
        private const int                         IconSpacing    = 4;

        // 当前选中 Prefab 尺寸来源：BuildingConfig / Sprite自动 / 手动
        private enum PlacementSizeMode { BuildingConfig, AutoFromSprite, Manual }
        private PlacementSizeMode _placementSizeMode;
        private string            _placementSizeSource;
        private MapObjectType     _currentObjectType = MapObjectType.Other;

        public void Draw(MapEditorCore core) => Draw(core, 0);

        public void Draw(MapEditorCore core, int availableWidth)
        {
            if (core?.Config == null) return;

            EditorGUILayout.LabelField("对象面板", EditorStyles.boldLabel);

            string scanPath = core.Config.objectPalettePath;
            if (_lastPath != scanPath)
                ScanPrefabs(scanPath);

            // ── 放置尺寸显示 ────────────────────────────────────────────
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            if (_placementSizeMode == PlacementSizeMode.BuildingConfig)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("放置尺寸", EditorStyles.miniLabel, GUILayout.Width(48));
                EditorGUILayout.LabelField(
                    $"{core.PlacementWidth} × {core.PlacementHeight}  （来自 PlacedObjConfig）",
                    EditorStyles.miniLabel);
                EditorGUILayout.EndHorizontal();
            }
            else if (_placementSizeMode == PlacementSizeMode.AutoFromSprite)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("放置尺寸", EditorStyles.miniLabel, GUILayout.Width(48));
                EditorGUILayout.LabelField(
                    $"{core.PlacementWidth} × {core.PlacementHeight}  （自动: {_placementSizeSource}）",
                    EditorStyles.miniLabel);
                EditorGUILayout.EndHorizontal();
            }
            else
            {
                EditorGUILayout.LabelField("放置尺寸（格子数，手动填写）", EditorStyles.miniLabel);
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label("宽", GUILayout.Width(20));
                core.PlacementWidth  = Mathf.Max(1, EditorGUILayout.IntField(core.PlacementWidth,  GUILayout.Width(36)));
                GUILayout.Label("高", GUILayout.Width(20));
                core.PlacementHeight = Mathf.Max(1, EditorGUILayout.IntField(core.PlacementHeight, GUILayout.Width(36)));
                EditorGUILayout.EndHorizontal();
            }

            // 对象类型
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("对象类型", EditorStyles.miniLabel, GUILayout.Width(48));
            _currentObjectType = (MapObjectType)EditorGUILayout.EnumPopup(_currentObjectType, GUILayout.Width(100));
            core.PlacementObjectType = _currentObjectType;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(4);

            if (_prefabs.Count == 0)
            {
                EditorGUILayout.HelpBox(
                    $"在 {scanPath} 中未找到 Prefab。\n请将对象 Prefab 放入该目录，或在 Config 中修改路径。",
                    MessageType.Info);
                return;
            }

            EditorGUILayout.LabelField($"扫描路径: {scanPath}", EditorStyles.miniLabel);
            if (GUILayout.Button("刷新", EditorStyles.miniButton))
                ScanPrefabs(scanPath);

            EditorGUILayout.Space(2);

            int colCount = availableWidth > 0
                ? Mathf.Max(1, availableWidth / (IconSize + IconSpacing))
                : 3;

            DrawPrefabGroups(core, colCount);
        }

        private void DrawPrefabGroups(MapEditorCore core, int colCount)
        {
            string currentFolder = null;
            var folderEntries = new List<PrefabEntry>();

            foreach (var entry in _prefabs)
            {
                if (currentFolder == null)
                    currentFolder = entry.folder;

                if (entry.folder != currentFolder)
                {
                    DrawFolderGroup(core, currentFolder, folderEntries, colCount);
                    folderEntries.Clear();
                    currentFolder = entry.folder;
                }

                folderEntries.Add(entry);
            }

            if (currentFolder != null)
                DrawFolderGroup(core, currentFolder, folderEntries, colCount);
        }

        private void DrawFolderGroup(MapEditorCore core, string folder, List<PrefabEntry> entries, int colCount)
        {
            if (!_folderFoldout.ContainsKey(folder))
                _folderFoldout[folder] = true;

            string label = $"{folder}  [{entries.Count}]";
            _folderFoldout[folder] = EditorGUILayout.Foldout(_folderFoldout[folder], label, true);
            if (!_folderFoldout[folder]) return;

            EditorGUI.indentLevel++;
            DrawPrefabGrid(core, entries, colCount);
            EditorGUI.indentLevel--;
            EditorGUILayout.Space(4);
        }

        private void DrawPrefabGrid(MapEditorCore core, List<PrefabEntry> entries, int colCount)
        {
            int col = 0;
            EditorGUILayout.BeginHorizontal();

            foreach (var entry in entries)
            {
                bool isSelected = core.SelectedPrefabId == entry.prefabId || core.SelectedPrefabId == entry.name;
                var cache = GetPreviewCache(entry);

                Color bg = GUI.backgroundColor;
                GUI.backgroundColor = isSelected ? Color.cyan : new Color(0.3f, 0.3f, 0.3f);

                bool hasAssetPreview = cache.preview != null;
                bool useDirectSprite = !hasAssetPreview && cache.spriteFromRenderer != null;

                string tooltip = entry.prefabId;
                if (cache.PlacedObjConfig != null)
                {
                    var cfg = cache.PlacedObjConfig;
                    tooltip += $"\n{cfg.SizeX}×{cfg.SizeY} 格";
                }

                GUIContent content;
                if (hasAssetPreview)
                {
                    content = new GUIContent(cache.preview, tooltip);
                }
                else if (useDirectSprite)
                {
                    content = new GUIContent("", tooltip);
                }
                else if (cache.fallback != null)
                {
                    content = new GUIContent(cache.fallback, tooltip);
                }
                else
                {
                    string shortName = entry.name.Length <= 6 ? entry.name : entry.name[..6];
                    content = new GUIContent(shortName, tooltip);
                }

                if (GUILayout.Button(content, GUILayout.Width(IconSize), GUILayout.Height(IconSize)))
                {
                    core.SelectedPrefabId = entry.prefabId;
                    core.EditMode         = EditMode.ObjectPlace;

                    if (cache.prefab != null)
                    {
                        var cfg = cache.PlacedObjConfig;
                        if (cfg != null)
                        {
                            core.PlacementWidth      = cfg.SizeX;
                            core.PlacementHeight     = cfg.SizeY;
                            core.PlacementFootprintW = cfg.ActualFootprintX;
                            core.PlacementFootprintH = cfg.ActualFootprintY;
                            _placementSizeMode       = PlacementSizeMode.BuildingConfig;
                            _placementSizeSource     = "PlacedObjConfig";
                            _currentObjectType       = cfg.ObjectType;
                        }
                        else
                        {
                            var autoSize = CalcSizeFromSprite(core, cache.prefab);
                            if (autoSize.HasValue)
                            {
                                core.PlacementWidth      = autoSize.Value.x;
                                core.PlacementHeight     = autoSize.Value.y;
                                core.PlacementFootprintW = 0;
                                core.PlacementFootprintH = 0;
                                _placementSizeMode       = PlacementSizeMode.AutoFromSprite;
                                _placementSizeSource     = cache.spriteFromRenderer != null
                                    ? cache.spriteFromRenderer.name
                                    : entry.name;
                            }
                            else
                            {
                                core.PlacementWidth      = 1;
                                core.PlacementHeight     = 1;
                                core.PlacementFootprintW = 0;
                                core.PlacementFootprintH = 0;
                                _placementSizeMode       = PlacementSizeMode.Manual;
                                _placementSizeSource     = null;
                            }
                            // 没有 PlacedObjConfig 时保持当前已选类型，不自动重置
                        }
                        core.PlacementObjectType = _currentObjectType;
                    }
                }

                if (useDirectSprite && Event.current.type == EventType.Repaint)
                {
                    Rect btnRect = GUILayoutUtility.GetLastRect();
                    DrawSpriteInRect(btnRect, cache.spriteFromRenderer);
                }

                GUI.backgroundColor = bg;

                col++;
                if (col >= colCount)
                {
                    col = 0;
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.BeginHorizontal();
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        private static void DrawSpriteInRect(Rect rect, Sprite sprite)
        {
            if (sprite?.texture == null) return;

            var tex = sprite.texture;
            var texRect = sprite.textureRect;
            var uv = new Rect(
                texRect.x / tex.width,
                texRect.y / tex.height,
                texRect.width / tex.width,
                texRect.height / tex.height);

            float spriteAspect = texRect.width / texRect.height;
            float padding = 0.9f;
            float maxW = rect.width * padding;
            float maxH = rect.height * padding;
            float w, h;
            if (spriteAspect > maxW / maxH) { w = maxW; h = w / spriteAspect; }
            else { h = maxH; w = h * spriteAspect; }

            Rect drawRect = new Rect(
                rect.x + (rect.width - w) * 0.5f,
                rect.y + (rect.height - h) * 0.5f,
                w, h);

            Color prev = GUI.color;
            GUI.color = Color.white;
            GUI.DrawTextureWithTexCoords(drawRect, tex, uv);
            GUI.color = prev;
        }

        private void ScanPrefabs(string path)
        {
            _lastPath = path;
            _prefabs.Clear();
            _folderFoldout.Clear();
            _previewCache.Clear();
_placementSizeMode = PlacementSizeMode.Manual;
            _placementSizeSource = null;
            GridRenderer.ClearObjectSpriteCache();

            if (string.IsNullOrEmpty(path)) return;
            if (!AssetDatabase.IsValidFolder(path)) return;

            string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { path });
            foreach (var guid in guids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                string name      = System.IO.Path.GetFileNameWithoutExtension(assetPath);
                string folder    = GetRelativeFolder(path, assetPath);
                string prefabId  = GetPrefabId(folder, name);
                _prefabs.Add(new PrefabEntry { guid = guid, assetPath = assetPath, name = name, folder = folder, prefabId = prefabId });
            }

            _prefabs.Sort((a, b) =>
            {
                int folderCompare = string.Compare(a.folder, b.folder, System.StringComparison.OrdinalIgnoreCase);
                if (folderCompare != 0) return folderCompare;
                return string.Compare(a.name, b.name, System.StringComparison.OrdinalIgnoreCase);
            });
        }

        private static string GetRelativeFolder(string rootPath, string assetPath)
        {
            string normalizedRoot = NormalizeAssetPath(rootPath).TrimEnd('/');
            string folderPath     = NormalizeAssetPath(Path.GetDirectoryName(assetPath));

            if (string.Equals(folderPath, normalizedRoot, System.StringComparison.OrdinalIgnoreCase))
                return RootFolderName;

            string prefix = normalizedRoot + "/";
            if (folderPath.StartsWith(prefix, System.StringComparison.OrdinalIgnoreCase))
                return folderPath.Substring(prefix.Length);

            return folderPath;
        }

        private static string GetPrefabId(string folder, string name)
        {
            return folder == RootFolderName ? name : $"{folder}/{name}";
        }

        private static Vector2Int? CalcSizeFromSprite(MapEditorCore core, GameObject prefab)
        {
            Sprite sprite = null;
            var sr = prefab.GetComponentInChildren<SpriteRenderer>();
            if (sr != null) sprite = sr.sprite;
            if (sprite == null) return null;

            float cellSize = core.MapData?.cellSize ?? 1f;
            if (cellSize <= 0f) cellSize = 1f;

            int w = Mathf.Max(1, Mathf.CeilToInt(sprite.bounds.size.x / cellSize));
            int h = Mathf.Max(1, Mathf.CeilToInt(sprite.bounds.size.y / cellSize));
            return new Vector2Int(w, h);
        }

        private PrefabPreviewCache GetPreviewCache(PrefabEntry entry)
        {
            if (_previewCache.TryGetValue(entry.assetPath, out var cache))
                return cache;

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(entry.assetPath);
cache = new PrefabPreviewCache
            {
                prefab = prefab,
                fallback = prefab != null ? AssetPreview.GetMiniThumbnail(prefab) : null,
                PlacedObjConfig = prefab != null ? prefab.GetComponent<PlacedObjConfig>() : null,
                spriteFromRenderer = prefab != null ? prefab.GetComponentInChildren<SpriteRenderer>()?.sprite : null,
            };
            _previewCache[entry.assetPath] = cache;
            return cache;
        }

private Texture2D GetStablePreview(PrefabPreviewCache cache)
        {
            if (cache.preview != null) return cache.preview;
            if (cache.prefab == null) return null;
            if (cache.previewResolved) return null;

            int instanceId = cache.prefab.GetInstanceID();
            if (!cache.previewRequested)
            {
                cache.previewRequested = true;
                AssetPreview.GetAssetPreview(cache.prefab);
            }

            if (!AssetPreview.IsLoadingAssetPreview(instanceId))
            {
                cache.preview = AssetPreview.GetAssetPreview(cache.prefab);
                cache.previewResolved = true;
            }
            else
            {
                RepaintPanel();
            }

            return cache.preview;
        }

        private static string NormalizeAssetPath(string path)
        {
            return string.IsNullOrEmpty(path) ? "" : path.Replace('\\', '/');
        }

        private static void RepaintPanel()
        {
            var window = EditorWindow.focusedWindow;
            if (window != null)
                window.Repaint();
        }
    }
}
