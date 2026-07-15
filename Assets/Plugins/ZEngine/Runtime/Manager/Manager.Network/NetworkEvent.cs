//------------------------------
// ZEngine
// 作者: Chenyu
//------------------------------

using Google.Protobuf;
using ZEngine.Manager.Event;
using ZEngine.Reference;

namespace ZEngine.Manager.Network
{
    /// <summary>
    /// 通道连接成功事件
    /// </summary>
    public class NetworkConnectedEvent : IEventMessage
    {
        /// <summary>
        /// 通道类型
        /// </summary>
        public NetworkChannelType ChannelType { get; set; }

        /// <summary>
        /// 通道名称
        /// </summary>
        public string ChannelName { get; set; }

        /// <summary>
        /// 服务器地址
        /// </summary>
        public string Host { get; set; }

        /// <summary>
        /// 服务器端口
        /// </summary>
        public int Port { get; set; }

        public static NetworkConnectedEvent Create(NetworkChannelType channelType, string channelName, string host, int port)
        {
            NetworkConnectedEvent args = ReferencePool.Spawn<NetworkConnectedEvent>();
            args.ChannelType = channelType;
            args.ChannelName = channelName;
            args.Host = host;
            args.Port = port;
            return args;
        }

        public void OnRelease()
        {
            ChannelType = default;
            ChannelName = null;
            Host = null;
            Port = 0;
        }
    }

    /// <summary>
    /// 通道断开连接事件参数
    /// </summary>
    public class NetworkDisconnectedEvent : IEventMessage
    {
        /// <summary>
        /// 通道类型
        /// </summary>
        public NetworkChannelType ChannelType { get; private set; }

        /// <summary>
        /// 通道名称
        /// </summary>
        public string ChannelName { get; private set; }

        /// <summary>
        /// 断开原因
        /// </summary>
        public DisconnectReason Reason { get; private set; }

        public static NetworkDisconnectedEvent Create(NetworkChannelType channelType, string channelName, DisconnectReason reason)
        {
            NetworkDisconnectedEvent args = ReferencePool.Spawn<NetworkDisconnectedEvent>();
            args.ChannelType = channelType;
            args.ChannelName = channelName;
            args.Reason = reason;
            return args;
        }

        public void OnRelease()
        {
            ChannelType = default;
            ChannelName = null;
            Reason = default;
        }
    }

    /// <summary>
    /// 通道连接失败事件参数
    /// </summary>
    public class NetworkConnectFailedEvent : IEventMessage
    {
        /// <summary>
        /// 通道类型
        /// </summary>
        public NetworkChannelType ChannelType { get; private set; }

        /// <summary>
        /// 通道名称
        /// </summary>
        public string ChannelName { get; private set; }

        /// <summary>
        /// 错误信息
        /// </summary>
        public string ErrorMessage { get; private set; }

        public static NetworkConnectFailedEvent Create(NetworkChannelType channelType, string channelName, string errorMessage)
        {
            NetworkConnectFailedEvent args = ReferencePool.Spawn<NetworkConnectFailedEvent>();
            args.ChannelType = channelType;
            args.ChannelName = channelName;
            args.ErrorMessage = errorMessage;
            return args;
        }

        public void OnRelease()
        {
            ChannelType = default;
            ChannelName = null;
            ErrorMessage = null;
        }
    }

    /// <summary>
    /// 通道错误事件参数
    /// </summary>
    public class NetworkErrorEvent : IEventMessage
    {
        /// <summary>
        /// 通道类型
        /// </summary>
        public NetworkChannelType ChannelType { get; private set; }

        /// <summary>
        /// 通道名称
        /// </summary>
        public string ChannelName { get; private set; }

        /// <summary>
        /// 错误信息
        /// </summary>
        public string ErrorMessage { get; private set; }

        public static NetworkErrorEvent Create(NetworkChannelType channelType, string channelName, string errorMessage)
        {
            NetworkErrorEvent args = ReferencePool.Spawn<NetworkErrorEvent>();
            args.ChannelType = channelType;
            args.ChannelName = channelName;
            args.ErrorMessage = errorMessage;
            return args;
        }

        public void OnRelease()
        {
            ChannelType = default;
            ChannelName = null;
            ErrorMessage = null;
        }
    }

    /// <summary>
    /// 通道正在重连事件参数
    /// </summary>
    public class NetworkReconnectingEvent : IEventMessage
    {
        /// <summary>
        /// 通道类型
        /// </summary>
        public NetworkChannelType ChannelType { get; private set; }

        /// <summary>
        /// 通道名称
        /// </summary>
        public string ChannelName { get; private set; }

        /// <summary>
        /// 当前重连次数
        /// </summary>
        public int CurrentCount { get; private set; }

        /// <summary>
        /// 最大重连次数
        /// </summary>
        public int MaxCount { get; private set; }

        public static NetworkReconnectingEvent Create(NetworkChannelType channelType, string channelName, int currentCount, int maxCount)
        {
            NetworkReconnectingEvent args = ReferencePool.Spawn<NetworkReconnectingEvent>();
            args.ChannelType = channelType;
            args.ChannelName = channelName;
            args.CurrentCount = currentCount;
            args.MaxCount = maxCount;
            return args;
        }

        public void OnRelease()
        {
            ChannelType = default;
            ChannelName = null;
            CurrentCount = 0;
            MaxCount = 0;
        }
    }

    /// <summary>
    /// 通道重连成功事件参数
    /// </summary>
    public class NetworkReconnectedEvent : IEventMessage
    {
        /// <summary>
        /// 通道类型
        /// </summary>
        public NetworkChannelType ChannelType { get; private set; }

        /// <summary>
        /// 通道名称
        /// </summary>
        public string ChannelName { get; private set; }

        /// <summary>
        /// 服务器地址
        /// </summary>
        public string Host { get; private set; }

        /// <summary>
        /// 服务器端口
        /// </summary>
        public int Port { get; private set; }

        public static NetworkReconnectedEvent Create(NetworkChannelType channelType, string channelName, string host, int port)
        {
            NetworkReconnectedEvent args = ReferencePool.Spawn<NetworkReconnectedEvent>();
            args.ChannelType = channelType;
            args.ChannelName = channelName;
            args.Host = host;
            args.Port = port;
            return args;
        }

        public void OnRelease()
        {
            ChannelType = default;
            ChannelName = null;
            Host = null;
            Port = 0;
        }
    }

    /// <summary>
    /// 通道重连失败事件参数
    /// </summary>
    public class NetworkReconnectFailedEvent : IEventMessage
    {
        /// <summary>
        /// 通道类型
        /// </summary>
        public NetworkChannelType ChannelType { get; private set; }

        /// <summary>
        /// 通道名称
        /// </summary>
        public string ChannelName { get; private set; }

        public static NetworkReconnectFailedEvent Create(NetworkChannelType channelType, string channelName)
        {
            NetworkReconnectFailedEvent args = ReferencePool.Spawn<NetworkReconnectFailedEvent>();
            args.ChannelType = channelType;
            args.ChannelName = channelName;
            return args;
        }

        public void OnRelease()
        {
            ChannelType = default;
            ChannelName = null;
        }
    }
}

