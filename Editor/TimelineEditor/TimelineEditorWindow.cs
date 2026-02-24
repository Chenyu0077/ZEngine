//------------------------------
// ZEngine
// 作者: Chenyu
//------------------------------

using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class TimelineEditorWindow : EditorWindow
{
    // 数据
    private TimelineAsset timeline;                     // 当前所选timeline
    private Vector2 scrollPos;
    private float zoom = 1f;                            // 时间轴缩放，像素/秒基础值乘子
    private const float basePixelsPerSecond = 100f;
    private float pixelsPerSecond => basePixelsPerSecond * zoom;
    private Vector2 pan = new Vector2(3f, 0);                 // 画布平移（x 用于水平平移）
    private float trackHeight = 40f;                    // 轨道高度
    private float trackSpacing = 6f;                    // 轨道间间距
    private Rect contentRect;
    private float canvasPosX = 0;                       // 编辑区域开始位置X
    private float canvasPosY = 20f;                     // 编辑区域开始位置Y
    private float leftWidth = 200f;                     // 左半部分宽度
    private float splitterWidth = 4f;                   // 分割条宽度
    private float rulerHeight = 20f;                    // 时间轴高度
    private float commonSpace = 3f;                     // 公共间隔
    private float clipOffsetHeight = 4f;                // clip的内缩高度
    private float checkBorder = 6f;                     // 边界触碰检查距离

    private bool isResizing = false;                    // 是否正在调整大小
    private Vector2 leftScroll;
    private Color commonColor = new Color(0.24f, 0.24f, 0.24f, 1f);
    private Color commonColorA = new Color(0.24f, 0.24f, 0.24f, 0.5f);
    private Color backColor = new Color(0.1f, 0.1f, 0.1f, 1f);

    // 交互
    TimelineClip draggedClip;
    TimelineTrack draggedClipTrack;
    Vector2 dragStartMouse;
    float dragStartClipStart;
    bool draggingClipEdgeLeft;
    bool draggingClipEdgeRight;
    TimelineClip selectedClip;
    TimelineTrack selectedTrack;
    float playheadTime = 0f;            // 当前播放时长(用次数表示, 便于转化成像素距离)
    bool isDraggingPlayhead = false;


    // 样式
    private GUIStyle clipStyle;             // 片段样式
    private GUIStyle playheadStyle;         // 指针样式
    private GUIStyle trackHeaderStyle;      // 轨道标题样式

    // 播放控制
    private bool isPlaying = false;         // 是否正在播放
    private bool isLooping = false;         // 是否循环
    private bool isReverse = false;         // 是否反向
    private float playbackSpeed = 1f;       // 播放方向 1 = 正常, -1 = 倒放
    private double lastEditorTime;          // 编辑器最后一次update的时间

    [MenuItem("ZEngineTools/Simple Timeline")]
    public static void OpenWindow()
    {
        var w = GetWindow<TimelineEditorWindow>("Simple Timeline");
        w.minSize = new Vector2(600, 240);
    }

    void OnEnable()
    {
        Selection.selectionChanged += OnSelectionChanged;
        EditorApplication.update += OnEditorUpdate;

        clipStyle = new GUIStyle(EditorStyles.label) { alignment = TextAnchor.MiddleCenter };
        trackHeaderStyle = new GUIStyle(EditorStyles.helpBox)
        {
            normal = { background = Texture2D.grayTexture },
            alignment = TextAnchor.MiddleLeft,
            padding = new RectOffset(10, 4, 2, 2),
            fontStyle = FontStyle.Bold,
        };
    }

    void OnDisable()
    {
        Selection.selectionChanged -= OnSelectionChanged;
        EditorApplication.update -= OnEditorUpdate;
    }

    /// <summary>
    /// 编辑器的Update
    /// </summary>
    private void OnEditorUpdate()
    {
        if (!isPlaying || timeline == null)
            return;

        double deltaTime = EditorApplication.timeSinceStartup - lastEditorTime;
        playheadTime += (float)(deltaTime * playbackSpeed);

        float maxDuration = GetTimelineLength();

        if (isLooping)
        {
            if (playheadTime > maxDuration) playheadTime = 0f;
            if (playheadTime < 0f) playheadTime = maxDuration;
        }
        else
        {
            playheadTime = Mathf.Clamp(playheadTime, 0f, maxDuration);
            if (playheadTime >= maxDuration || playheadTime == 0f)
                isPlaying = false;
        }

        lastEditorTime = EditorApplication.timeSinceStartup;
        Repaint();
    }

    /// <summary>
    /// 资源选择时触发
    /// </summary>
    private void OnSelectionChanged()
    {
        // 自动加载选中的 TimelineAsset（可选）
        var sel = Selection.activeObject as TimelineAsset;
        if (sel != null)
        {
            timeline = sel;
            Repaint();
        }
    }

    void OnGUI()
    {
        DrawToolbar();

        // 手动创建Timeline数据
        if (timeline == null)
        {
            EditorGUILayout.HelpBox("请创建一个Timeline (Assets/Create/SimpleTimeline/Timeline Asset).", MessageType.Info);
            if (GUILayout.Button("Create Timeline Asset"))
            {
                var asset = CreateInstance<TimelineAsset>();
                string path = EditorUtility.SaveFilePanelInProject("Save Timeline", "NewTimeline", "asset", "Save timeline asset");
                if (!string.IsNullOrEmpty(path))
                {
                    AssetDatabase.CreateAsset(asset, path);
                    AssetDatabase.SaveAssets();
                    Selection.activeObject = asset;
                    timeline = asset;
                }
            }
            return;
        }

        // 绘制编辑区
        Rect canvasRect = new Rect(canvasPosX, canvasPosY, position.width, position.height - canvasPosY);
        // 左半部分
        Rect leftRect = new Rect(0, canvasRect.y, leftWidth, canvasRect.height);
        // 分割条
        Rect splitterRect = new Rect(leftWidth, canvasRect.y, splitterWidth, canvasRect.height);
        // 右半部分
        Rect rightRect = new Rect(leftWidth + splitterWidth, canvasRect.y, canvasRect.width - leftWidth - splitterWidth, canvasRect.width);

        DrawLeftPanel(leftRect);
        DrawSplitter(splitterRect);
        DrawRightPanel(rightRect);

    }


    #region 绘制
    /// <summary>
    /// 绘制工具栏
    /// </summary>
    private void DrawToolbar()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        if (GUILayout.Button("Add Track", EditorStyles.toolbarButton))
        {
            if (timeline != null)
            {
                Undo.RecordObject(timeline, "Add Track");
                timeline.tracks.Add(new TimelineTrack() { name = "Track " + (timeline.tracks.Count + 1) });
                EditorUtility.SetDirty(timeline);
            }
        }

        GUILayout.Space(10);
        if (GUILayout.Button(isPlaying ? "⏸ Pause" : "▶ Play", EditorStyles.toolbarButton, GUILayout.Width(60)))
        {
            isPlaying = !isPlaying;
            lastEditorTime = EditorApplication.timeSinceStartup;
        }

        if (GUILayout.Button("⏹ Stop", EditorStyles.toolbarButton, GUILayout.Width(60)))
        {
            isPlaying = false;
            playheadTime = 0f;
            playbackSpeed = 1f;
        }

        if (GUILayout.Button("⏩ Fast", EditorStyles.toolbarButton, GUILayout.Width(60)))
        {
            isPlaying = true;
            playbackSpeed = Mathf.Abs(playbackSpeed) * 2f;
            lastEditorTime = EditorApplication.timeSinceStartup;
        }

        isReverse = GUILayout.Toggle(isReverse, "⏪ Reverse", EditorStyles.toolbarButton, GUILayout.Width(60));
        if (isReverse)
        {
            playbackSpeed = -Mathf.Abs(playbackSpeed);
        }
        else
        {
            playbackSpeed = Mathf.Abs(playbackSpeed);
        }

        isLooping = GUILayout.Toggle(isLooping, "🔁 Loop", EditorStyles.toolbarButton, GUILayout.Width(60));

        if (GUILayout.Button("Fit", EditorStyles.toolbarButton))
        {
            zoom = 1f;
            pan.x = 0f;
        }
        GUILayout.FlexibleSpace();
        // Zoom slider
        zoom = GUILayout.HorizontalSlider(zoom, 0.2f, 3f, GUILayout.Width(150));
        GUILayout.Label($"Zoom {zoom:F2}", GUILayout.Width(70));
        EditorGUILayout.EndHorizontal();
    }

    private void DrawLeftPanel(Rect rect)
    {
        EditorGUI.DrawRect(rect, backColor);
        GUILayout.BeginArea(rect, EditorStyles.helpBox);
        GUILayout.BeginVertical();
        {
            GUILayout.Space(commonSpace);
            EditorGUI.DrawRect(new Rect(0, commonSpace, leftWidth, rulerHeight), commonColor);
            for (int i = 0; i < timeline.tracks.Count; i++)
            {
                DrawTrackBox(i, timeline.tracks[i]);
                GUILayout.Space(trackSpacing);
            }
        }
        GUILayout.EndVertical();
        GUILayout.EndArea();
    }

    private void DrawSplitter(Rect rect)
    {
        EditorGUI.DrawRect(rect, backColor);
        Rect tempRect = new Rect(rect.x, rect.y + commonSpace, rect.width, rect.height - commonSpace);
        EditorGUI.DrawRect(tempRect, commonColor);
        EditorGUIUtility.AddCursorRect(tempRect, MouseCursor.ResizeHorizontal);
        HandleResize(tempRect);
    }

    private void DrawRightPanel(Rect rect)
    {
        EditorGUI.DrawRect(rect, backColor);
        GUILayout.BeginArea(rect);
        GUILayout.BeginVertical();
        {
            // 时间轴
            DrawRuler(new Rect(0, commonSpace, rect.width, rulerHeight));

            contentRect = new Rect(0, rulerHeight + commonSpace, rect.width, rect.height);
            scrollPos = GUI.BeginScrollView(new Rect(0, contentRect.y, rect.width, contentRect.height), scrollPos, new Rect(0, contentRect.y, rect.width, contentRect.height));
            {
                // 间隔线
                DrawTimeGrid(contentRect);

                // 轨道
                float y = rulerHeight + trackSpacing * 2;
                for (int i = 0; i < timeline.tracks.Count; i++)
                {
                    var track = timeline.tracks[i];
                    Rect trackRect = new Rect(commonSpace, y, rect.width, trackHeight);
                    EditorGUI.DrawRect(trackRect, commonColorA);
                    DrawTrack(trackRect, track, i);
                    y += trackHeight + trackSpacing;
                }

                // 时间指针
                DrawPlayhead(contentRect);
            }
            GUI.EndScrollView();

            HandleInput(rect);
        }
        GUILayout.EndVertical();
        GUILayout.EndArea();
    }


    /// <summary>
    /// 调整区域大小
    /// </summary>
    private void HandleResize(Rect splitterRect)
    {
        Event e = Event.current;
        switch (e.type)
        {
            case EventType.MouseDown:
                if (splitterRect.Contains(e.mousePosition))
                {
                    isResizing = true;
                    e.Use();
                }
                break;
            case EventType.MouseUp:
                isResizing = false;
                break;
            case EventType.MouseDrag:
                if (isResizing)
                {
                    leftWidth += e.delta.x;
                    leftWidth = Mathf.Clamp(leftWidth, 100, position.width - 100);
                    Repaint();
                }
                break;
        }
    }

    /// <summary>
    /// 绘制左边轨道头部
    /// </summary>
    private void DrawTrackBox(int index, TimelineTrack track)
    {
        Rect rect = new Rect(10, (index + 2) * trackSpacing + rulerHeight + index * trackHeight, leftWidth - 10 - commonSpace, trackHeight);
        EditorGUI.DrawRect(rect, commonColor);
        Rect contentRect = new Rect(rect.x + 8, rect.y + 8, rect.width - 16, rect.height - 16);
        GUILayout.BeginArea(contentRect);
        GUILayout.EndArea();
    }

    /// <summary>
    /// 绘制时间刻度
    /// </summary>
    private void DrawRuler(Rect rect)
    {
        EditorGUI.DrawRect(rect, commonColor);
        GUI.Box(rect, GUIContent.none);
        int num = Mathf.CeilToInt((rect.width + Mathf.Abs(pan.x)) / pixelsPerSecond);
        for (int i = 0; i < num; i++)
        {
            float time = i;
            float x = TimeToPixel(time) + pan.x;
            if (x >= 0 && x <= rect.width)
            {
                GUI.Label(new Rect(x + 2, rect.y, 40, rect.height - 2), time.ToString("0.00"));
                Vector3 vec0 = new Vector3(x, rect.y + rect.height);
                Vector3 vec1 = new Vector3(x, rect.y + rect.height - 9);
                Handles.DrawLine(vec0, vec1);
                float minPixelsPerSecond = pixelsPerSecond / 5;
                for (int j = 1; j < 5; j++)
                {
                    Vector3 vec2 = new Vector3(x + j * minPixelsPerSecond, rect.y + rect.height);
                    Vector3 vec3 = new Vector3(x + j * minPixelsPerSecond, rect.y + rect.height - 4);
                    Handles.DrawLine(vec2, vec3);
                }
            }
        }
    }

    /// <summary>
    /// 绘制间隔线
    /// </summary>
    private void DrawTimeGrid(Rect rect)
    {
        int steps = Mathf.CeilToInt((rect.width + Mathf.Abs(pan.x)) / (pixelsPerSecond));
        for (int i = 0; i < steps; i++)
        {
            float x = TimeToPixel(i) + pan.x;
            Handles.DrawLine(new Vector3(x, rect.y), new Vector3(x, rect.y + rect.height));
        }
    }

    /// <summary>
    /// 绘制轨道
    /// </summary>
    /// <param name="rect"></param>
    /// <param name="track"></param>
    /// <param name="trackIndex"></param>
    private void DrawTrack(Rect rect, TimelineTrack track, int trackIndex)
    {
        for (int i = 0; i < track.clips.Count; i++)
        {
            var clip = track.clips[i];
            Rect clipRect = new Rect(
                TimeToPixel(clip.startTime) + pan.x,
                rect.y + clipOffsetHeight,
                clip.duration * pixelsPerSecond,
                rect.height - clipOffsetHeight * 2
            );

            //if (clip == selectedClip)
            //{
            //    EditorGUI.DrawRect(clipRect, Color.yellow * 0.4f);
            //}

            EditorGUI.DrawRect(clipRect, clip.color);
            GUI.Label(clipRect, clip.displayName, clipStyle);

            Rect leftEdge = new Rect(clipRect.xMin, clipRect.yMin, checkBorder, clipRect.height);
            Rect rightEdge = new Rect(clipRect.xMax - checkBorder, clipRect.yMin, checkBorder, clipRect.height);
            EditorGUIUtility.AddCursorRect(leftEdge, MouseCursor.ResizeHorizontal);
            EditorGUIUtility.AddCursorRect(rightEdge, MouseCursor.ResizeHorizontal);
        }
    }

    /// <summary>
    /// 绘制时间轴
    /// </summary>
    /// <param name="rect"></param>
    private void DrawPlayhead(Rect rect)
    {
        float x = TimeToPixel(playheadTime) + pan.x;
        Handles.color = Color.red;
        Handles.DrawLine(new Vector3(x, rect.y), new Vector3(x, rect.y + rect.height));
        Vector3[] tri = new Vector3[] {
            new Vector3(x, rect.y + 3),
            new Vector3(x - 6, rect.y + 12),
            new Vector3(x + 6, rect.y + 12)
        };
        Handles.DrawAAConvexPolygon(tri);
    }

    /// <summary>
    /// 次数转像素距离
    /// </summary>
    private float TimeToPixel(float time)
    {
        return time * pixelsPerSecond;
    }

    /// <summary>
    /// 像素距离转次数
    /// </summary>
    private float PixelToTime(float px)
    {
        return px / pixelsPerSecond;
    }

    /// <summary>
    /// 获取时间线有效长度
    /// </summary>
    /// <returns></returns>
    private float GetTimelineLength()
    {
        float max = 0f;
        foreach (var t in timeline.tracks)
        {
            foreach (var c in t.clips)
                max = Mathf.Max(max, c.startTime + c.duration);
        }
        return Mathf.Max(max, 5f);
    }
    #endregion



    #region 交互
    /// <summary>
    /// 交互相关
    /// </summary>
    private void HandleInput(Rect canvasRect)
    {
        Event e = Event.current;
        Vector2 mouse = e.mousePosition;
        bool inside = canvasRect.Contains(mouse);
        float tempSpace = rulerHeight + trackSpacing * 2;

        // 时间轴缩放
        if (inside && e.type == EventType.ScrollWheel)
        {
            if (e.shift)    // 按住shift+滑动滚轮
            {
                pan.x -= e.delta.y * 10f;
            }
            else
            {
                zoom = Mathf.Clamp(zoom * (1f - e.delta.y * 0.1f), 0.2f, 4f);
            }
            e.Use();
            Repaint();
        }

        // 在时间轴上点击移动时间指针
        if (e.type == EventType.MouseDown && e.button == 0 && e.modifiers == EventModifiers.None)
        {
            if (e.mousePosition.y < rulerHeight)
            {
                float px = e.mousePosition.x - pan.x;
                playheadTime = Mathf.Max(0, PixelToTime(px));
                e.Use();
                Repaint();
            }
        }

        // 左键点击
        if (e.type == EventType.MouseDown && e.button == 0 && inside)
        {
            GUI.FocusControl(null); // 清空当前焦点

            // 检查指针是否能被拖拽
            float playheadX = TimeToPixel(playheadTime) + pan.x;
            if (Mathf.Abs(mouse.x - playheadX) < checkBorder)
            {
                isDraggingPlayhead = true;
                e.Use();
                return;
            }

            // 检查是否点击到click
            int trackIdx = GetTrackIndexUnder(mouse);
            if (trackIdx >= 0)
            {
                var track = timeline.tracks[trackIdx];
                TimelineClip hitClip = null;
                foreach (var c in track.clips)
                {
                    Rect clipRect = new Rect(TimeToPixel(c.startTime) + pan.x, tempSpace + trackIdx * (trackHeight + trackSpacing) + clipOffsetHeight, c.duration * pixelsPerSecond, trackHeight - clipOffsetHeight * 2);
                    if (clipRect.Contains(mouse))
                    {
                        hitClip = c;
                        Debug.Log(hitClip.displayName);

                        if (Mathf.Abs(mouse.x - clipRect.xMin) <= checkBorder)
                        {
                            draggingClipEdgeLeft = true;
                        }
                        else if (Mathf.Abs(mouse.x - clipRect.xMax) <= checkBorder)
                        {
                            draggingClipEdgeRight = true;
                        }
                        else
                        {
                            draggingClipEdgeLeft = draggingClipEdgeRight = false;
                        }
                        break;
                    }
                }

                if (hitClip != null)
                {
                    // select and start dragging
                    selectedClip = hitClip;
                    selectedTrack = track;
                    draggedClip = hitClip;
                    draggedClipTrack = track;
                    dragStartMouse = mouse;
                    dragStartClipStart = draggedClip.startTime;
                    e.Use();
                    return;
                }
                else
                {
                    // click empty space in track => clear selection or create clip on double click
                    if (e.clickCount == 2)
                    {
                        // create clip at mouse
                        float px = mouse.x - 120 - pan.x;
                        float t = PixelToTime(px);
                        AddClipAt(trackIdx, t);
                        e.Use();
                        return;
                    }
                    selectedClip = null;
                    Repaint();
                }
            }
        }

        // 拖拽相关
        if (e.type == EventType.MouseDrag && e.button == 0)
        {
            if (isDraggingPlayhead)
            {
                float px = mouse.x - pan.x;
                playheadTime = Mathf.Max(0, PixelToTime(px));
                e.Use();
                Repaint();
                return;
            }

            if (draggedClip != null)
            {
                int trIndex = timeline.tracks.IndexOf(draggedClipTrack);
                float deltaPx = e.delta.x;
                float deltaTime = deltaPx / pixelsPerSecond;
                if (draggingClipEdgeLeft)
                {
                    float newStart = dragStartClipStart + PixelToTime(mouse.x - dragStartMouse.x);
                    // clamp
                    newStart = Mathf.Max(0, Mathf.Min(newStart, draggedClip.startTime + draggedClip.duration - 0.1f));
                    Undo.RecordObject(timeline, "Resize Clip Left");
                    draggedClip.duration += draggedClip.startTime - newStart;
                    draggedClip.startTime = newStart;
                    EditorUtility.SetDirty(timeline);
                }
                else if (draggingClipEdgeRight)
                {
                    float newDuration = draggedClip.duration + deltaTime;
                    newDuration = Mathf.Max(0.05f, newDuration);
                    Undo.RecordObject(timeline, "Resize Clip Right");
                    draggedClip.duration = newDuration;
                    EditorUtility.SetDirty(timeline);
                }
                else
                {
                    // move clip
                    float pxGlobal = mouse.x - dragStartMouse.x;
                    float dt = PixelToTime(pxGlobal);
                    float newStart = Mathf.Max(0f, dragStartClipStart + dt);
                    Undo.RecordObject(timeline, "Move Clip");
                    draggedClip.startTime = newStart;
                    EditorUtility.SetDirty(timeline);
                }
                e.Use();
                Repaint();
            }
        }


        if (e.type == EventType.MouseUp && e.button == 0)
        {
            // release dragging
            isDraggingPlayhead = false;
            draggingClipEdgeLeft = draggingClipEdgeRight = false;
            draggedClip = null;
            draggedClipTrack = null;
            e.Use();
        }

        //right click context menu
        if (e.type == EventType.ContextClick && inside)
        {
            int idx = GetTrackIndexUnder(mouse);
            GenericMenu menu = new GenericMenu();
            if (idx >= 0)
            {
                menu.AddItem(new GUIContent("Add Clip Here"), false, () =>
                {
                    float px = mouse.x - pan.x;
                    float t = PixelToTime(px);
                    AddClipAt(idx, t);
                });
                if (selectedClip != null)
                {
                    menu.AddItem(new GUIContent("Delete Clip"), false, () =>
                    {
                        RemoveClip(selectedTrack, selectedClip);
                    });
                }
            }
            else
            {
                menu.AddItem(new GUIContent("Add Track"), false, () =>
                {
                    Undo.RecordObject(timeline, "Add Track");
                    timeline.tracks.Add(new TimelineTrack() { name = "Track " + (timeline.tracks.Count + 1) });
                    EditorUtility.SetDirty(timeline);
                });
            }
            menu.ShowAsContext();
            e.Use();
        }
    }



    #endregion


    #region 数据操作
    /// <summary>
    /// 获取当前点击的位置的Track索引
    /// </summary>
    private int GetTrackIndexUnder(Vector2 mouse)
    {
        float y = rulerHeight + trackSpacing * 2;
        for (int i = 0; i < timeline.tracks.Count; i++)
        {
            Rect r = new Rect(0, y, position.width, trackHeight);
            if (mouse.y >= r.y && mouse.y <= r.y + r.height)
                return i;
            y += trackHeight + trackSpacing;
        }
        return -1;
    }

    /// <summary>
    /// 添加Clip
    /// </summary>
    private void AddClipAt(int trackIndex, float startTime)
    {
        var t = timeline.tracks[trackIndex];
        Undo.RecordObject(timeline, "Add Clip");
        var c = new TimelineClip()
        {
            displayName = "Clip",
            startTime = Mathf.Max(0, startTime),
            duration = 1f,
            color = new Color(Random.value, Random.value, Random.value)
        };
        t.clips.Add(c);
        EditorUtility.SetDirty(timeline);
    }

    /// <summary>
    /// 移除Clip
    /// </summary>
    private void RemoveClip(TimelineTrack track, TimelineClip clip)
    {
        if (track == null || clip == null) return;
        Undo.RecordObject(timeline, "Remove Clip");
        track.clips.Remove(clip);
        selectedClip = null;
        EditorUtility.SetDirty(timeline);
    }
    #endregion
}



