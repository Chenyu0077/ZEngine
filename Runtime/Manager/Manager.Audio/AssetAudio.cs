//------------------------------
// ZEngine
// 作者: Chenyu
//------------------------------

using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using YooAsset;
using ZEngine.Config;
using ZEngine.Manager.Resource;

namespace ZEngine.Manager.Audio
{
    /// <summary>
    /// 音频资源类
    /// </summary>
    public class AssetAudio
    {
        private AssetHandle _handle;
        private Action<AudioClip> _callback;
        private bool _isLoadAsset = false;

        /// <summary>
        /// 资源地址
        /// </summary>
        public string Location { private set; get; }

        /// <summary>
        /// 音频层级
        /// </summary>
        public EAudioLayer AudioLayer { private set; get; }

        /// <summary>
        /// 资源对象
        /// </summary>
        public AudioClip Clip { private set; get; }

        public AssetAudio(string location, EAudioLayer audioLayer)
        {
            Location = location;
            AudioLayer = audioLayer;
        }

        /// <summary>
        /// 加载音频资源
        /// </summary>
        /// <param name="callback"></param>
        public async UniTask Load(Action<AudioClip> callback)
        {
            if (_isLoadAsset)
                return;

            _isLoadAsset = true;
            _callback = callback;
            _handle = await ResourceManager.Instance.LoadAssetAsync<AudioClip>(Location);
            _handle.Completed += Handle_Completed;
        }

        private void Handle_Completed(AssetHandle obj)
        {
            Clip = _handle.AssetObject as AudioClip;
            _callback?.Invoke(Clip);
        }

        /// <summary>
        /// 卸载音频资源
        /// </summary>
        public void UnLoad()
        {
            if (_isLoadAsset)
            {
                _isLoadAsset = false;
                _callback = null;
                _handle.Release();
            }
        }
    }
}
