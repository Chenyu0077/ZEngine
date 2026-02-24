//------------------------------
// ZEngine
// 作者: Chenyu
//------------------------------

using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using ZEngine.Core;
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

        private readonly Dictionary<string, AssetAudio> _assets = new Dictionary<string, AssetAudio>(500);
        private readonly Dictionary<EAudioLayer, AudioSourceWrapper> _audioSourceWrappers = new Dictionary<EAudioLayer, AudioSourceWrapper>(200);

        public void OnInit(object param)
        {
            //检测依赖模块
            if (ZEngineMain.Contains(typeof(ResourceManager)) == false)
                throw new Exception($"{nameof(AudioManager)}依赖于{nameof(ResourceManager)}");

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
            foreach(var key in _assets.Keys)
            {
                _assets[key].UnLoad();
            }
            _assets.Clear();
        }

        public void Release(EAudioLayer audioLayer)
        {
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
                    if (clip != null)
                    {
                        if (assetAudio != null)//注意：在加载过程中音频源可能被销毁，所以需要判空
                            audioSource.PlayOneShot(clip);
                    }
                }).Forget(Debug.LogException);
            }
        }

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
