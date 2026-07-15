//------------------------------
// ZEngine
// 作者: Chenyu
//------------------------------

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class TimelinePlayer : MonoBehaviour
{
    public TimelineAsset timeline;
    public float playhead = 0f;
    public bool isPlaying = false;
    public float duration = 10f;

    private Dictionary<TimelineClip, bool> _clipPlayed = new();

    void Start()
    {
        if (timeline != null)
        {
            foreach (var track in timeline.tracks)
            {
                foreach (var clip in track.clips)
                {
                    _clipPlayed[clip] = false;
                    duration = Mathf.Max(duration, clip.startTime + clip.duration);
                }
            }
        }
    }

    void Update()
    {
        if (!isPlaying) return;

        playhead += Time.deltaTime;

        foreach (var track in timeline.tracks)
        {
            foreach (var clip in track.clips)
            {
                if (!_clipPlayed[clip] && playhead >= clip.startTime)
                {
                    Debug.Log($"播放片段: {clip.displayName}");
                    clip.onPlay?.Invoke();
                    _clipPlayed[clip] = true;
                }
            }
        }

        if (playhead >= duration)
        {
            isPlaying = false;
            Debug.Log("播放结束");
        }
    }

    public void Play()
    {
        playhead = 0;
        isPlaying = true;
        foreach (var clip in _clipPlayed.Keys)
            _clipPlayed[clip] = false;
    }

    public void Stop()
    {
        isPlaying = false;
    }
}

