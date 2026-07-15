//------------------------------
// ZEngine
// зїеп: Chenyu
//------------------------------

using UnityEngine;
using System.Collections.Generic;

//[CreateAssetMenu(menuName = "SimpleTimeline/Timeline Asset")]
public class TimelineAsset : ScriptableObject
{
    public List<TimelineTrack> tracks = new List<TimelineTrack>();
    public float duration = 10f;
}
