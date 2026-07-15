//------------------------------
// ZEngine
// 作者: Chenyu
//------------------------------

using Google.Protobuf;
using System;
using System.Collections.Generic;
using UnityEngine;
using ZEngine.Core;
using ZEngine.Manager.Event;

namespace ZEngine.Manager.Network
{
    /// <summary>
    /// 网络管理器
    /// 统一管理TCP、UDP、WebSocket三种网络通道
    /// </summary>
    public class NetworkManager : ManagerSingleton<NetworkManager>, IManager
    {
        /// <summary>
        /// TCP通道
        /// </summary>
        public TcpChannel TcpChannel { get; private set; }

        /// <summary>
        /// UDP通道
        /// </summary>
        public UdpChannel UdpChannel { get; private set; }

        /// <summary>
        /// WebSocket通道
        /// </summary>
        public WebSocketChannel WebSocketChannel { get; private set; }

        /// <summary>
        /// 所有通道列表
        /// </summary>
        private readonly List<NetworkChannelBase> _channels = new List<NetworkChannelBase>();


        #region 生命周期
        public void OnInit(object param)
        {
            // 创建三个通道
            TcpChannel = new TcpChannel("TcpChannel", this);
            UdpChannel = new UdpChannel("UdpChannel", this);
            WebSocketChannel = new WebSocketChannel("WebSocketChannel", this);

            _channels.Add(TcpChannel);
            _channels.Add(UdpChannel);
            _channels.Add(WebSocketChannel);

            _root = new GameObject("[Z][NetworkManager]");
            GameObject.DontDestroyOnLoad(_root);
        }

        public void OnUpdate()
        {
            float elapseSeconds = Time.deltaTime;
            float realElapseSeconds = Time.unscaledDeltaTime;
            // 轮询所有通道
            foreach (var channel in _channels)
            {
                channel.Update(elapseSeconds, realElapseSeconds);
            }
        }

        public void OnDestroy()
        {
            // 关闭所有通道
            foreach (var channel in _channels)
            {
                channel.Shutdown();
            }
            _channels.Clear();

            TcpChannel = null;
            UdpChannel = null;
            WebSocketChannel = null;

            DestroySingleton();
            ZEngineLog.Log("NetworkManager已关闭");
        }

        public void OnGUI()
        {
            
        }
        #endregion


        /// <summary>
        /// 断开所有连接
        /// </summary>
        public void DisconnectAll()
        {
            foreach (var channel in _channels)
            {
                channel.Disconnect();
            }
        }

        #region 连接的回调事件
        /// <summary>
        /// 通道连接成功回调
        /// </summary>
        internal void OnChannelConnected(NetworkChannelBase channel)
        {
            var eventObj = NetworkConnectedEvent.Create(
                channel.ChannelType,
                channel.Name,
                channel.Host,
                channel.Port);
            ZEngineLog.Log($"{channel.Name}频道连接成功，地址:{channel.Host}端口:{channel.Port}");
            EventManager.Instance.DelayMessage(eventObj);
        }

        /// <summary>
        /// 通道断开连接回调
        /// </summary>
        internal void OnChannelDisconnected(NetworkChannelBase channel, DisconnectReason reason)
        {
            var eventObj = NetworkDisconnectedEvent.Create(
                channel.ChannelType,
                channel.Name,
                reason);
            ZEngineLog.Error($"{channel.Name}频道断开连接,断开原因:{reason}");
            EventManager.Instance.DelayMessage(eventObj);
        }

        /// <summary>
        /// 通道连接失败回调
        /// </summary>
        internal void OnChannelConnectFailed(NetworkChannelBase channel, string error)
        {
            var eventObj = NetworkConnectFailedEvent.Create(
                channel.ChannelType,
                channel.Name,
                error);
            ZEngineLog.Error($"{channel.Name}频道连接失败,失败原因:{error}");
            EventManager.Instance.DelayMessage(eventObj);
        }

        /// <summary>
        /// 通道错误回调
        /// </summary>
        internal void OnChannelError(NetworkChannelBase channel, string error)
        {
            var eventObj = NetworkErrorEvent.Create(
                channel.ChannelType,
                channel.Name,
                error);
            ZEngineLog.Error($"{channel.Name}频道错误,错误信息:{error}");
            EventManager.Instance.DelayMessage(eventObj);
        }

        /// <summary>
        /// 通道正在重连回调
        /// </summary>
        internal void OnChannelReconnecting(NetworkChannelBase channel, int currentCount, int maxCount)
        {
            var eventObj = NetworkReconnectingEvent.Create(
                channel.ChannelType,
                channel.Name,
                currentCount,
                maxCount);
            ZEngineLog.Log($"{channel.Name}频道正在重连 ({currentCount}/{maxCount})");
            EventManager.Instance.DelayMessage(eventObj);
        }

        /// <summary>
        /// 通道重连成功回调
        /// </summary>
        internal void OnChannelReconnected(NetworkChannelBase channel)
        {
            var eventObj = NetworkReconnectedEvent.Create(
                channel.ChannelType,
                channel.Name,
                channel.Host,
                channel.Port);
            ZEngineLog.Log($"{channel.Name}频道重连成功");
            EventManager.Instance.DelayMessage(eventObj);
        }

        /// <summary>
        /// 通道重连失败回调
        /// </summary>
        internal void OnChannelReconnectFailed(NetworkChannelBase channel)
        {
            var eventObj = NetworkReconnectFailedEvent.Create(
                channel.ChannelType,
                channel.Name);
            ZEngineLog.Error($"{channel.Name}频道重连失败，已达最大重连次数");
            EventManager.Instance.DelayMessage(eventObj);
        }
        #endregion
    }
}

