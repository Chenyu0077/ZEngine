//using UnityEditor;
//using UnityEngine;
//using System.Linq;
//using System.Collections.Generic;
//using ZEngine.Module.Skill;
//using Sirenix.OdinInspector.Editor;
//using Sirenix.OdinInspector;
//using System;

//public class SkillTimelineEditorWindow : OdinEditorWindow
//{

//    [MenuItem("ZEngineTools/SkillEditor", false, 30)]
//    public static void OpenWindow()
//    {
//        var window = GetWindow<SkillTimelineEditorWindow>("Skill Timeline");
//        window.Show();
//    }

//    [InlineEditor(Expanded = false)]
//    public SkillTimelineAsset timeline;


//    [PropertySpace(15)]
//    [HorizontalGroup("Timeline Preview/Slider")]
//    [LabelText("播放指针 (s)"), Range(0, 10)]
//    [OnValueChanged(nameof(OnPlayheadChanged))]
//    public float playhead;

//    [HorizontalGroup("Timeline Preview/Slider", Width = 100)]
//    [LabelText("时长 (s)")]
//    public float duration = 10f;

//    [ShowInInspector, ReadOnly, PropertyOrder(2)]
//    [LabelText("播放状态")]
//    private bool isPlaying;

//    private float lastUpdateTime;


//    [FoldoutGroup("Timeline Preview"), HideLabel, PropertyOrder(0)]
//    [HorizontalGroup("Timeline Preview/Controls", Width = 60)]
//    [Button(ButtonSizes.Medium), GUIColor(0.6f, 1f, 0.6f)]
//    private void Play()
//    {
//        if (!isPlaying)
//        {
//            isPlaying = true;
//            lastUpdateTime = (float)EditorApplication.timeSinceStartup;
//            EditorApplication.update += UpdatePlayhead;
//        }
//    }

//    [HorizontalGroup("Timeline Preview/Controls", Width = 60)]
//    [Button(ButtonSizes.Medium), GUIColor(1f, 0.6f, 0.6f)]
//    private void Pause()
//    {
//        if (isPlaying)
//        {
//            isPlaying = false;
//            EditorApplication.update -= UpdatePlayhead;
//        }
//    }

//    [HorizontalGroup("Timeline Preview/Controls", Width = 60)]
//    [Button(ButtonSizes.Medium), GUIColor(0.8f, 0.8f, 1f)]
//    private void Stop()
//    {
//        isPlaying = false;
//        playhead = 0;
//        EditorApplication.update -= UpdatePlayhead;
//        OnPlayheadChanged();
//    }

//    [TableList(AlwaysExpanded = true)]
//    [LabelText("轨道列表")]
//    public List<SkillTimelineTrack> Tracks = new();

//    private void UpdatePlayhead()
//    {
//        if (!isPlaying) return;

//        float delta = (float)(EditorApplication.timeSinceStartup - lastUpdateTime);
//        lastUpdateTime = (float)EditorApplication.timeSinceStartup;
//        playhead += delta;

//        if (playhead >= duration)
//        {
//            playhead = duration;
//            Pause();
//        }

//        // 刷新 Inspector
//        Sirenix.Utilities.Editor.GUIHelper.RequestRepaint();

//        // 遍历每个轨道，触发事件
//        foreach (var track in Tracks)
//        {
//            foreach (var clip in track.events)
//            {
//                if (!clipTriggered.Contains(clip) && playhead >= clip.startTime)
//                {
//                    clipTriggered.Add(clip);
//                    OnClipTriggered(track, clip);
//                }
//            }
//        }
//    }

//    private HashSet<SkillEvent> clipTriggered = new();

//    private void ResetEvents()
//    {
//        clipTriggered.Clear();
//    }

//    private void OnPlayheadChanged()
//    {
//        // 拖动滑块时刷新触发状态
//        clipTriggered.Clear();
//        foreach (var track in Tracks)
//        {
//            foreach (var clip in track.events)
//            {
//                if (playhead >= clip.startTime)
//                    clipTriggered.Add(clip);
//            }
//        }
//    }

//    private void OnClipTriggered(SkillTimelineTrack track, SkillEvent clip)
//    {
//        Debug.Log($"[{track.name}] 时间 {clip.startTime:F2}s 触发事件: {clip.name} 类型: {clip.type}");
//    }
//}

