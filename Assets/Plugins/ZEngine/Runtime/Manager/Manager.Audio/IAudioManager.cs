//------------------------------
// ZEngine
// 作者: Chenyu
//------------------------------

using UnityEngine;

namespace ZEngine.Manager.Audio
{
    public interface IAudioManager
    {
        /// <summary>
        /// 获取音频源
        /// </summary>
        /// <param name="layer">音频层级</param>
        /// <returns></returns>
        public AudioSource GetAudioSource(EAudioLayer layer);

        /// <summary>
        /// 预加载音频资源
        /// </summary>
        /// <param name="location">资源地址</param>
        /// <param name="audioLayer">音频层级</param>
        public void Preload(string location, EAudioLayer audioLayer);

        /// <summary>
        /// 释放所有音频资源
        /// </summary>
        public void ReleaseAll();

        /// <summary>
        /// 释放指定层级的音频资源
        /// </summary>
        /// <param name="audioLayer">音频层级</param>
        public void Release(EAudioLayer audioLayer);

        /// <summary>
        /// 释放指定层级的特定音频资源
        /// </summary>
        /// <param name="audioLayer">音频层级</param>
        /// <param name="location">资源地址</param>
        public void Release(EAudioLayer audioLayer, string location);

        /// <summary>
        /// 播放背景音乐
        /// </summary>
        /// <param name="location">资源地址</param>
        /// <param name="loop">是否循环播放</param>
        public void PlayMusic(string location, bool loop);

        /// <summary>
        /// 播放环境音效
        /// </summary>
        /// <param name="location">资源地址</param>
        /// <param name="loop">是否循环播放</param>
        public void PlayAmbient(string location, bool loop);

        /// <summary>
        /// 播放语音
        /// </summary>
        /// <param name="location">资源地址</param>
        public void PlayVoice(string location);

        /// <summary>
        /// 播放音效
        /// </summary>
        /// <param name="location">资源地址</param>
        public void PlaySound(string location);

        /// <summary>
        /// 播放外部音频源播放音效
        /// </summary>
        /// <param name="audioSource">外部的音频源</param>
        /// <param name="location">资源地址</param>
        public void PlaySound(AudioSource audioSource, string location);

        /// <summary>
        /// 暂停播放
        /// </summary>
        /// <param name="layer">音频层级</param>
        public void Stop(EAudioLayer layer);

        /// <summary>
        /// 设置所有频道静音
        /// </summary>
        /// <param name="isMute">是否静音</param>
        public void Mute(bool isMute);

        /// <summary>
        /// 设置特定层级的频道静音
        /// </summary>
        /// <param name="layer">音频层级</param>
        /// <param name="isMute">是否静音</param>
        public void Mute(EAudioLayer layer, bool isMute);

        /// <summary>
        /// 查询某一频道是否静音
        /// </summary>
        /// <param name="layer">音频层级</param>
        /// <returns></returns>
        public bool IsMute(EAudioLayer layer);

        /// <summary>
        /// 设置所有频道音量
        /// </summary>
        /// <param name="volume">音量数值</param>
        public void Volume(float volume);

        /// <summary>
        /// 设置特定层级的频道音量
        /// </summary>
        /// <param name="layer">音频层级</param>
        /// <param name="volume">音量数值</param>
        public void Volume(EAudioLayer layer, float volume);

        /// <summary>
        /// 查询频道音量
        /// </summary>
        /// <param name="layer">音频层级</param>
        /// <returns></returns>
        public float GetVolume(EAudioLayer layer);

        /// <summary>
        /// 设置空间音效的监听器（通常为主摄像机或玩家的 Transform）
        /// </summary>
        /// <param name="listener">监听器 Transform，传 null 则退化为全音量播放</param>
        public void SetSoundListener(Transform listener);

        /// <summary>
        /// 在指定世界坐标播放一次性音效，音量根据与监听器的 XY 距离线性衰减
        /// </summary>
        /// <param name="location">资源地址</param>
        /// <param name="worldPosition">音效发出的世界坐标</param>
        /// <param name="minDistance">小于此距离时音量最大</param>
        /// <param name="maxDistance">大于此距离时音量为零</param>
        public void PlaySoundAtPosition(string location, Vector3 worldPosition, float minDistance = 5f, float maxDistance = 30f);
    }
}
