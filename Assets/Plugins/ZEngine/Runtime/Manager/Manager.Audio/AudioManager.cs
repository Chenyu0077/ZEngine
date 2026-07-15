//------------------------------
// ZEngine
// 作者: Chenyu
//------------------------------

using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using ZEngine.Core;
using ZEngine.Manager.Log;
using ZEngine.Manager.Resource;

namespace ZEngine.Manager.Audio
{
    public class AudioManager : ManagerSingleton<AudioManager>, IManager, IAudioManager
    {
        /// <summary>
		/// 音频源封装类
		/// </summary>
		private class AudioSourceWrapper
        {
            public GameObject Go { private set; get; }
            public AudioSource Source { private set; get; }
            public AudioSourceWrapper(string name, Transform emitter)
            {
                // Create an empty game object
                Go = new GameObject(name);
                Go.transform.position = emitter.position;
                Go.transform.parent = emitter;

                // Create the source
                Source = Go.AddComponent<AudioSource>();
                Source.volume = 1.0f;
                Source.pitch = 1.0f;
            }
        }

        /// <summary>
        /// 位置音效句柄，管理一个临时 AudioSource 并每帧根据距离更新音量
        /// </summary>
        private class PositionalSoundHandle
        {
            public readonly GameObject Go;
            public readonly AudioSource Source;
            public readonly Vector3 WorldPosition;
            public readonly float MinDistance;
            public readonly float MaxDistance;
            private bool _started;
            private bool _failed;   // clip 为 null 或加载失败时置 true，供 OnUpdate 下一帧清理

            //  _failed 时也视为结束，确保泄漏的句柄被 OnUpdate 销毁
            public bool IsDone => _failed || (_started && !Source.isPlaying);

            public PositionalSoundHandle(Vector3 worldPosition, float minDist, float maxDist, Transform parent)
            {
                WorldPosition = worldPosition;
                MinDistance   = minDist;
                MaxDistance   = maxDist;
                Go = new GameObject("[PositionalSound]");
                Go.transform.position = worldPosition;
                Go.transform.SetParent(parent);
                Source = Go.AddComponent<AudioSource>();
                Source.spatialBlend = 0f;   // 纯 2D，音量由我们手动控制
                Source.playOnAwake  = false;
            }

            public void Play(AudioClip clip)
            {
                if (clip == null)
                {
                    _failed = true;   // Bug 1 fix: clip 无效时标记失败，OnUpdate 下帧回收
                    return;
                }
                Source.clip = clip;
                Source.loop = false;
                Source.Play();
                _started = true;
            }

            /// <summary>
            /// 根据监听器与声源的 XY 距离线性计算音量（忽略 Z，适配 2D 正交相机）
            /// </summary>
            public void UpdateVolume(Transform listener, float baseVolume)
            {
                if (listener == null)
                {
                    Source.volume = baseVolume;
                    return;
                }
                float dist = Vector2.Distance(
                    new Vector2(listener.position.x, listener.position.y),
                    new Vector2(WorldPosition.x,     WorldPosition.y)
                );
                float t = Mathf.Clamp01((dist - MinDistance) / Mathf.Max(0.001f, MaxDistance - MinDistance));
                Source.volume = baseVolume * (1f - t);
            }

            public void Destroy() => UnityEngine.Object.Destroy(Go);
        }

        private readonly Dictionary<string, AssetAudio> _assets = new Dictionary<string, AssetAudio>(500);
        private readonly Dictionary<EAudioLayer, AudioSourceWrapper> _audioSourceWrappers = new Dictionary<EAudioLayer, AudioSourceWrapper>(200);
        private readonly List<PositionalSoundHandle> _positionalSounds = new List<PositionalSoundHandle>();
        private Transform _soundListener;

        public void OnInit(object param)
        {
            //检测依赖模块
            if (ZEngineMain.Contains(typeof(ResourceManager)) == false)
                throw new Exception($"{nameof(AudioManager)}依赖于{nameof(ResourceManager)}");
            if (ZEngineMain.Contains(typeof(LogManager)) == false)
                throw new Exception($"{nameof(AudioManager)}依赖于{nameof(LogManager)}");

            _root = new GameObject("[Z][AudioManager]");
            GameObject.DontDestroyOnLoad(_root);

            foreach(int value in Enum.GetValues(typeof(EAudioLayer)))
            {
                EAudioLayer layer = (EAudioLayer)value;
                _audioSourceWrappers.Add(layer, new AudioSourceWrapper(layer.ToString(), _root.transform));
            }
        }

        public void OnUpdate()
        {
            if (_positionalSounds.Count == 0) return;

            float baseVolume = IsMute(EAudioLayer.Sound) ? 0f : GetVolume(EAudioLayer.Sound);
            for (int i = _positionalSounds.Count - 1; i >= 0; i--)
            {
                var handle = _positionalSounds[i];
                if (handle.IsDone)
                {
                    handle.Destroy();
                    _positionalSounds.RemoveAt(i);
                }
                else
                {
                    handle.UpdateVolume(_soundListener, baseVolume);
                }
            }
        }

        public void OnGUI()
        {
            
        }

        public void OnDestroy()
        {
            //ReleaseAll();
            DestroySingleton();
        }

        public AudioSource GetAudioSource(EAudioLayer layer)
        {
            return _audioSourceWrappers[layer].Source;
        }
        
        public void Preload(string location, EAudioLayer audioLayer)
        {
            if(_assets.ContainsKey(location) == false)
            {
                AssetAudio asset = new AssetAudio(location, audioLayer);
                _assets.Add(location, asset);
                asset.Load(null).Forget(Debug.LogException);
            }
        }

        public void ReleaseAll()
        {
            for (int i = 0; i < _positionalSounds.Count; i++)
                _positionalSounds[i].Destroy();
            _positionalSounds.Clear();

            foreach(var key in _assets.Keys)
                _assets[key].UnLoad();
            _assets.Clear();
        }

        public void Release(EAudioLayer audioLayer)
        {
            if (audioLayer == EAudioLayer.Sound)
            {
                for (int i = 0; i < _positionalSounds.Count; i++)
                    _positionalSounds[i].Destroy();
                _positionalSounds.Clear();
            }

            List<string> removeList = new List<string>();
            foreach (var key in _assets.Keys)
            {
                if (_assets[key].AudioLayer == audioLayer)
                    removeList.Add(key);
            }
            for (int i = 0; i < removeList.Count; i++)
            {
                string key = removeList[i];
                _assets[key].UnLoad();
                _assets.Remove(key);
            }
        }

        public void Release(EAudioLayer audioLayer, string location)
        {
            List<string> removeList = new List<string>();
            foreach (var key in _assets.Keys)
            {
                if (_assets[key].AudioLayer == audioLayer && _assets[key].Location == location)
                    removeList.Add(key);
            }
            for(int i = 0; i < removeList.Count; i++)
            {
                string key = removeList[i];
                _assets[key].UnLoad();
                _assets.Remove(key);
            }
        }

        public void PlayMusic(string location, bool loop)
        {
            if (string.IsNullOrEmpty(location))
                return;

            PlayAudioClip(EAudioLayer.Music, location, loop);
        }

        public void PlayMusic(AudioClip clip, bool loop)
        {
            if (clip == null) return;
            PlayAudioClipInternal(EAudioLayer.Music, clip, loop);
        }

        public void PlayAmbient(string location, bool loop)
        {
            if (string.IsNullOrEmpty(location))
                return;

            PlayAudioClip(EAudioLayer.Ambient, location, loop);
        }

        public void PlayVoice(string location)
        {
            if (string.IsNullOrEmpty(location))
                return;

            //如果是静音状态直接跳过播放
            if (IsMute(EAudioLayer.Voice))
                return;

            PlayAudioClip(EAudioLayer.Voice, location, false);
        }

        public void PlaySound(string location)
        {
            if (string.IsNullOrEmpty(location))
                return;

            //如果是静音状态直接跳过播放
            if (IsMute(EAudioLayer.Sound))
                return;

            PlayAudioClip(EAudioLayer.Sound, location, false);
        }

        public void PlaySound(AudioSource audioSource, string location)
        {
            if (audioSource == null)
                return;

            if (audioSource.isActiveAndEnabled == false)
                return;

            if (string.IsNullOrEmpty(location))
                return;

            //如果是静音状态直接跳过播放
            if (IsMute(EAudioLayer.Sound))
                return;

            if (_assets.ContainsKey(location))
            {
                if (_assets[location].Clip != null)
                    audioSource.PlayOneShot(_assets[location].Clip);
            }
            else
            {
                //新建音频资源
                AssetAudio assetAudio = new AssetAudio(location, EAudioLayer.Sound);
                _assets.Add(location, assetAudio);
                assetAudio.Load((AudioClip clip) =>
                {
                    // Bug 1 fix: 应检查 audioSource（MonoBehaviour）是否已被销毁，而非 assetAudio（C# 对象永不为 null）
                    if (clip != null && audioSource != null)
                        audioSource.PlayOneShot(clip);
                }).Forget(Debug.LogException);
            }
        }

        #region 根据距离播放音效
        public void SetSoundListener(Transform listener)
        {
            _soundListener = listener;
        }

        public void PlaySoundAtPosition(string location, Vector3 worldPosition, float minDistance = 5f, float maxDistance = 30f)
        {
            if (string.IsNullOrEmpty(location)) return;
            if (IsMute(EAudioLayer.Sound)) return;

            var handle = new PositionalSoundHandle(worldPosition, minDistance, maxDistance, _root.transform);
            handle.UpdateVolume(_soundListener, GetVolume(EAudioLayer.Sound));
            _positionalSounds.Add(handle);

            if (_assets.TryGetValue(location, out var asset))
            {
                if (asset.Clip != null)
                {
                    // Clip 已就绪，立即播放
                    handle.Play(asset.Clip);
                }
                else
                {
                    // Clip 正在加载中（由前一次调用发起）——订阅同一 AssetAudio 的回调列表，
                    // 加载完成后所有等待的 handle 同时收到通知并开始播放，不再调用 Play(null) 标坏句柄
                    asset.Load(clip =>
                    {
                        if (handle.Go == null) return;
                        handle.UpdateVolume(_soundListener, GetVolume(EAudioLayer.Sound));
                        handle.Play(clip);
                    }).Forget(Debug.LogException);
                }
            }
            else
            {
                var assetAudio = new AssetAudio(location, EAudioLayer.Sound);
                _assets.Add(location, assetAudio);
                assetAudio.Load(clip =>
                {
                    if (handle.Go == null) return;
                    handle.UpdateVolume(_soundListener, GetVolume(EAudioLayer.Sound));
                    handle.Play(clip);
                }).Forget(Debug.LogException);
            }
        }

        #endregion
        
        public void Stop(EAudioLayer layer)
        {
            _audioSourceWrappers[layer].Source.Stop();
        }

        public void Mute(bool isMute)
        {
            foreach(var key in _audioSourceWrappers.Keys)
            {
                _audioSourceWrappers[key].Source.mute = isMute;
            }
        }

        public void Mute(EAudioLayer layer, bool isMute)
        {
            _audioSourceWrappers[layer].Source.mute = isMute;
        }

        public bool IsMute(EAudioLayer layer)
        {
            return _audioSourceWrappers[layer].Source.mute;
        }

        public void Volume(float volume)
        {
            volume = Mathf.Clamp01(volume);
            foreach(var key in _audioSourceWrappers.Keys)
            {
                _audioSourceWrappers[key].Source.volume = volume;
            }
        }

        public void Volume(EAudioLayer layer, float volume)
        {
            volume = Mathf.Clamp01(volume);
            _audioSourceWrappers[layer].Source.volume = volume;
        }

        public float GetVolume(EAudioLayer layer)
        {
            return _audioSourceWrappers[layer].Source.volume;
        }


        private void PlayAudioClip(EAudioLayer layer, string location, bool isLoop)
        {
            if (_assets.ContainsKey(location))
            {
                if (_assets[location].Clip != null)
                    PlayAudioClipInternal(layer, _assets[location].Clip, isLoop);
            }
            else
            {
                //新建音频资源
                AssetAudio assetAudio = new AssetAudio(location, layer);
                _assets.Add(location, assetAudio);
                assetAudio.Load((AudioClip clip) =>
                {
                    if (clip != null)
                        PlayAudioClipInternal(layer, clip, isLoop);
                }).Forget(Debug.LogException);
            }
        }

        private void PlayAudioClipInternal(EAudioLayer layer, AudioClip clip, bool isLoop)
        {
            if (clip == null)
                return;

            if (layer == EAudioLayer.Music || layer == EAudioLayer.Ambient || layer == EAudioLayer.Voice)
            {
                _audioSourceWrappers[layer].Source.clip = clip;
                _audioSourceWrappers[layer].Source.loop = isLoop;
                _audioSourceWrappers[layer].Source.Play();
            }
            else if(layer == EAudioLayer.Sound)
            {
                _audioSourceWrappers[layer].Source.PlayOneShot(clip);
            }
            else
            {
                throw new NotImplementedException($"{layer}");
            }
        }
    }
}
