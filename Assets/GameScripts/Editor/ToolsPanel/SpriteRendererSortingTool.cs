using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace GameScripts.Editor
{
    /// <summary>
    /// 批量设置文件夹下所有 Prefab 中 SpriteRenderer 的渲染层级工具
    /// </summary>
    public class SpriteRendererSortingTool
    {
        private DefaultAsset _targetFolder;
        private string _sortingLayerName = "Default";
        private int _sortingOrder;
        private bool _onlyModifyExistingLayer;

        private Vector2 _scrollPos;
        private List<string> _log = new();

        private string[] _sortingLayerNames;
        private int _sortingLayerIndex;

        // ── 功能二：按子物体名称过滤 ──
        private DefaultAsset _childTargetFolder;
        private string _childSortingLayerName = "Default";
        private int _childSortingOrder;
        private string _childNameFilter = "";
        private string[] _childSortingLayerNames;
        private int _childSortingLayerIndex;
        private Vector2 _childScrollPos;
        private List<string> _childLog = new();

        public SpriteRendererSortingTool()
        {
            RefreshSortingLayers();
            RefreshChildSortingLayers();
        }

        private void RefreshSortingLayers()
        {
            var layers = SortingLayer.layers;
            _sortingLayerNames = new string[layers.Length];
            for (int i = 0; i < layers.Length; i++)
                _sortingLayerNames[i] = layers[i].name;

            _sortingLayerIndex = 0;
            for (int i = 0; i < _sortingLayerNames.Length; i++)
            {
                if (_sortingLayerNames[i] == _sortingLayerName)
                {
                    _sortingLayerIndex = i;
                    break;
                }
            }
        }

        private void RefreshChildSortingLayers()
        {
            var layers = SortingLayer.layers;
            _childSortingLayerNames = new string[layers.Length];
            for (int i = 0; i < layers.Length; i++)
                _childSortingLayerNames[i] = layers[i].name;

            _childSortingLayerIndex = 0;
            for (int i = 0; i < _childSortingLayerNames.Length; i++)
            {
                if (_childSortingLayerNames[i] == _childSortingLayerName)
                {
                    _childSortingLayerIndex = i;
                    break;
                }
            }
        }

        public void OnGUI()
        {
            DrawBulkSection();
            GUILayout.Space(12);
            DrawChildFilterSection();
            GUILayout.Space(8);
            DrawLogSection();
        }

        #region 功能一：批量设置所有 SpriteRenderer

        private void DrawBulkSection()
        {
            EditorGUILayout.LabelField("功能一：批量设置 Prefab SpriteRenderer 渲染层级", EditorStyles.boldLabel);
            GUILayout.Space(4);

            _targetFolder = (DefaultAsset)EditorGUILayout.ObjectField(
                "目标文件夹", _targetFolder, typeof(DefaultAsset), false);

            GUILayout.Space(6);

            if (_sortingLayerNames == null || _sortingLayerNames.Length == 0)
                RefreshSortingLayers();

            int newIndex = EditorGUILayout.Popup("Sorting Layer", _sortingLayerIndex, _sortingLayerNames);
            if (newIndex != _sortingLayerIndex)
            {
                _sortingLayerIndex = newIndex;
                _sortingLayerName = _sortingLayerNames[_sortingLayerIndex];
            }

            _sortingOrder = EditorGUILayout.IntField("Order in Layer", _sortingOrder);

            GUILayout.Space(4);

            _onlyModifyExistingLayer = EditorGUILayout.ToggleLeft(
                "仅修改已选 Sorting Layer 层的组件", _onlyModifyExistingLayer);

            GUILayout.Space(10);

            bool canExecute = _targetFolder != null;
            EditorGUI.BeginDisabledGroup(!canExecute);
            if (GUILayout.Button("执行批量设置", GUILayout.Height(30)))
                ExecuteBulk();
            EditorGUI.EndDisabledGroup();

            if (!canExecute)
                EditorGUILayout.HelpBox("请先选择一个 Project 中的文件夹。", MessageType.Info);
        }

        private void ExecuteBulk()
        {
            _log.Clear();

            string folderPath = AssetDatabase.GetAssetPath(_targetFolder);
            if (string.IsNullOrEmpty(folderPath))
            {
                Debug.LogError("[SpriteRendererSortingTool] 无法获取文件夹路径");
                return;
            }

            string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { folderPath });
            if (guids.Length == 0)
            {
                EditorUtility.DisplayDialog("提示", "未在选定文件夹中找到任何 Prefab。", "确定");
                return;
            }

            int modifiedCount = 0;
            int prefabCount = 0;

            try
            {
                for (int i = 0; i < guids.Length; i++)
                {
                    string assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);
                    EditorUtility.DisplayProgressBar("批量设置中...", assetPath, (float)i / guids.Length);

                    using var editScope = new PrefabEditScope(assetPath);
                    if (editScope.Root == null) continue;

                    prefabCount++;
                    var renderers = editScope.Root.GetComponentsInChildren<SpriteRenderer>(true);
                    bool changed = false;

                    foreach (var sr in renderers)
                    {
                        if (_onlyModifyExistingLayer && sr.sortingLayerName != _sortingLayerName)
                            continue;

                        bool needsUpdate = sr.sortingLayerName != _sortingLayerName || sr.sortingOrder != _sortingOrder;
                        if (!needsUpdate) continue;

                        string before = $"{sr.sortingLayerName}/{sr.sortingOrder}";
                        sr.sortingLayerName = _sortingLayerName;
                        sr.sortingOrder = _sortingOrder;
                        EditorUtility.SetDirty(sr);
                        changed = true;

                        string objPath = GetHierarchyPath(sr.transform, editScope.Root.transform);
                        _log.Add($"[全部] {System.IO.Path.GetFileNameWithoutExtension(assetPath)} → {objPath}  ({before} → {_sortingLayerName}/{_sortingOrder})");
                        modifiedCount++;
                    }

                    if (changed)
                        editScope.MarkSave();
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            string summary = $"完成：扫描 {prefabCount} 个 Prefab，共修改 {modifiedCount} 个 SpriteRenderer。";
            Debug.Log($"[SpriteRendererSortingTool] {summary}");
            EditorUtility.DisplayDialog("完成", summary, "确定");
        }

        #endregion

        #region 功能二：按子物体名称过滤设置

        private void DrawChildFilterSection()
        {
            EditorGUILayout.LabelField("功能二：按子物体名称设置 SpriteRenderer 渲染层级", EditorStyles.boldLabel);
            GUILayout.Space(4);

            _childTargetFolder = (DefaultAsset)EditorGUILayout.ObjectField(
                "目标文件夹", _childTargetFolder, typeof(DefaultAsset), false);

            GUILayout.Space(6);

            _childNameFilter = EditorGUILayout.TextField("子物体名称", _childNameFilter);

            GUILayout.Space(4);

            if (_childSortingLayerNames == null || _childSortingLayerNames.Length == 0)
                RefreshChildSortingLayers();

            int newIndex = EditorGUILayout.Popup("Sorting Layer", _childSortingLayerIndex, _childSortingLayerNames);
            if (newIndex != _childSortingLayerIndex)
            {
                _childSortingLayerIndex = newIndex;
                _childSortingLayerName = _childSortingLayerNames[_childSortingLayerIndex];
            }

            _childSortingOrder = EditorGUILayout.IntField("Order in Layer", _childSortingOrder);

            GUILayout.Space(10);

            bool canExecute = _childTargetFolder != null && !string.IsNullOrEmpty(_childNameFilter);
            EditorGUI.BeginDisabledGroup(!canExecute);
            if (GUILayout.Button("执行按子物体名称设置", GUILayout.Height(30)))
                ExecuteChildFilter();
            EditorGUI.EndDisabledGroup();

            if (_childTargetFolder == null)
                EditorGUILayout.HelpBox("请先选择一个 Project 中的文件夹。", MessageType.Info);
            else if (string.IsNullOrEmpty(_childNameFilter))
                EditorGUILayout.HelpBox("请输入子物体名称。", MessageType.Info);
        }

        private void ExecuteChildFilter()
        {
            _childLog.Clear();

            string folderPath = AssetDatabase.GetAssetPath(_childTargetFolder);
            if (string.IsNullOrEmpty(folderPath))
            {
                Debug.LogError("[SpriteRendererSortingTool] 无法获取文件夹路径");
                return;
            }

            string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { folderPath });
            if (guids.Length == 0)
            {
                EditorUtility.DisplayDialog("提示", "未在选定文件夹中找到任何 Prefab。", "确定");
                return;
            }

            int modifiedCount = 0;
            int prefabCount = 0;
            int skippedCount = 0;

            try
            {
                for (int i = 0; i < guids.Length; i++)
                {
                    string assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);
                    EditorUtility.DisplayProgressBar("按子物体设置中...", assetPath, (float)i / guids.Length);

                    using var editScope = new PrefabEditScope(assetPath);
                    if (editScope.Root == null) continue;

                    prefabCount++;

                    var child = editScope.Root.transform.Find(_childNameFilter);
                    if (child == null)
                    {
                        skippedCount++;
                        continue;
                    }

                    SpriteRenderer[] renderers;
                    var sr = child.GetComponent<SpriteRenderer>();
                    renderers = sr != null ? new[] { sr } : child.GetComponentsInChildren<SpriteRenderer>(true);

                    bool changed = false;

                    foreach (var renderer in renderers)
                    {
                        bool needsUpdate = renderer.sortingLayerName != _childSortingLayerName || renderer.sortingOrder != _childSortingOrder;
                        if (!needsUpdate) continue;

                        string before = $"{renderer.sortingLayerName}/{renderer.sortingOrder}";
                        renderer.sortingLayerName = _childSortingLayerName;
                        renderer.sortingOrder = _childSortingOrder;
                        EditorUtility.SetDirty(renderer);
                        changed = true;

                        string objPath = GetHierarchyPath(renderer.transform, editScope.Root.transform);
                        _childLog.Add($"[子物体] {System.IO.Path.GetFileNameWithoutExtension(assetPath)} → {objPath}  ({before} → {_childSortingLayerName}/{_childSortingOrder})");
                        modifiedCount++;
                    }

                    if (changed)
                        editScope.MarkSave();
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            string summary = $"完成：扫描 {prefabCount} 个 Prefab（{skippedCount} 个未找到子物体），共修改 {modifiedCount} 个 SpriteRenderer。";
            Debug.Log($"[SpriteRendererSortingTool] {summary}");
            EditorUtility.DisplayDialog("完成", summary, "确定");
        }

        #endregion

        #region 日志

        private void DrawLogSection()
        {
            bool hasLog = _log.Count > 0 || _childLog.Count > 0;
            if (!hasLog) return;

            int totalCount = _log.Count + _childLog.Count;
            EditorGUILayout.LabelField($"执行日志（共修改 {totalCount} 个组件）：", EditorStyles.miniBoldLabel);
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos, GUILayout.Height(200));
            foreach (var line in _log)
                EditorGUILayout.LabelField(line, EditorStyles.miniLabel);
            foreach (var line in _childLog)
                EditorGUILayout.LabelField(line, EditorStyles.miniLabel);
            EditorGUILayout.EndScrollView();

            if (GUILayout.Button("清除日志", GUILayout.Width(80)))
            {
                _log.Clear();
                _childLog.Clear();
            }
        }

        #endregion

        private static string GetHierarchyPath(Transform t, Transform root)
        {
            var parts = new List<string>();
            while (t != null && t != root)
            {
                parts.Insert(0, t.name);
                t = t.parent;
            }
            return string.Join("/", parts);
        }

        private class PrefabEditScope : System.IDisposable
        {
            public GameObject Root { get; }
            private readonly string _path;
            private bool _save;

            public PrefabEditScope(string path)
            {
                _path = path;
                Root = PrefabUtility.LoadPrefabContents(path);
            }

            public void MarkSave() => _save = true;

            public void Dispose()
            {
                if (_save)
                    PrefabUtility.SaveAsPrefabAsset(Root, _path);
                PrefabUtility.UnloadPrefabContents(Root);
            }
        }
    }
}