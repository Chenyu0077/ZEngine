//------------------------------
// ZEngine
// 作者: Chenyu
//------------------------------

using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class TimelineTrack
{
    public string name = "Track";
    public List<TimelineClip> clips = new List<TimelineClip>();
    public TrackType trackType;
}

public enum TrackType
{
    Animator,
    Sound,
    Effect,
    Transform
}