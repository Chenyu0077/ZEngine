//------------------------------
// ZEngine
// 作者: Chenyu
//------------------------------

using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using YooAsset;
using ZEngine.Manager.Resource;

namespace ZEngine.Manager.Audio
{
    /// <summary>
    /// 音频资源类
    /// </summary>
    public class AssetAudio
    {
        private AssetHandle _handle;
        // 原设计只存一个 _callback，多处并发等待同一资源时后注册的回调被丢弃。
        // 改为列表：所有等待者都能在加载完成时收到通知。
        private readonly List<Action<AudioClip>> _callbacks = new List<Action<AudioClip>>();
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
        /// 资源对象（加载完成前为 null）
        /// </summary>
        public AudioClip Clip { private set; get; }

        public AssetAudio(string location, EAudioLayer audioLayer)
        {
            Location = location;
            AudioLayer = audioLayer;
        }

        /// <summary>
        /// 加载音频资源。若已在加载中，仅追加 callback 到等待列表，不重复发起请求。
        /// </summary>
        public async UniTask Load(Action<AudioClip> callback)
        {
            if (callback != null)
                _callbacks.Add(callback);

            if (_isLoadAsset)
                return; // 已在加载中，callback 已入队，等结果即可

            _isLoadAsset = true;
            _handle = await ResourceManager.Instance.LoadAssetAsync<AudioClip>(Location);
            _handle.Completed += Handle_Completed;
        }

        private void Handle_Completed(AssetHandle obj)
        {
            Clip = _handle.AssetObject as AudioClip;
            foreach (var cb in _callbacks)
                cb?.Invoke(Clip);
            _callbacks.Clear();
        }

        /// <summary>
        /// 卸载音频资源
        /// </summary>
        public void UnLoad()
        {
            if (_isLoadAsset)
            {
                _isLoadAsset = false;
                _callbacks.Clear();
                // _handle 在 await 完成前为 null，提前调用 UnLoad 时需判空
                _handle?.Release();
            }
        }
    }
}
