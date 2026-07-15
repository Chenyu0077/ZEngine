//------------------------------
// ZEngine
// 作者: Chenyu
//------------------------------

using UnityEngine;
using UnityEngine.Events;

[System.Serializable]
public class TimelineClip
{
    public string displayName = "Clip";
    public float startTime = 0f;
    public float duration = 1f;
    public Color color = Color.cyan;
    // 可扩展字段：参考资源引用、事件、曲线等
    public UnityEvent onPlay; // 可选
}

