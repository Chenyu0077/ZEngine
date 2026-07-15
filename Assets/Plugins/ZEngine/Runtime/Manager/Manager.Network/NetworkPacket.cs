//------------------------------
// ZEngine
// 作者: Chenyu
//------------------------------

using ZEngine.Reference;

namespace ZEngine.Manager.Network
{
    /// <summary>
    /// 原始数据包基类
    /// </summary>
    public abstract class ReceiveNetPacketBase : IReference
    {
        /// <summary>
        /// 消息ID
        /// </summary>
        public int MessageId { get; protected set; }

        /// <summary>
        /// 消息体数据
        /// </summary>
        public byte[] Body { get; protected set; }

        /// <summary>
        /// 消息体长度
        /// </summary>
        public int BodyLength { get; protected set; }

        /// <summary>
        /// 清理引用（由子类重写）
        /// </summary>
        public virtual void OnRelease()
        {
            MessageId = 0;
            Body = null;
            BodyLength = 0;
        }
    }

    /// <summary>
    /// TCP 原始数据包
    /// 消息格式：[Length(4)] + [ID(2)] + [Body]
    /// </summary>
    public class TcpReceiveNetPacket : ReceiveNetPacketBase
    {
        /// <summary>
        /// 创建数据包
        /// </summary>
        public static TcpReceiveNetPacket Create(int messageId, byte[] body, int bodyLength)
        {
            TcpReceiveNetPacket packet = ReferencePool.Spawn<TcpReceiveNetPacket>();
            packet.MessageId = messageId;
            packet.Body = body;
            packet.BodyLength = bodyLength;
            return packet;
        }
    }

    /// <summary>
    /// UDP 原始数据包
    /// 消息格式：[ID(2)] + [Body]
    /// </summary>
    public class UdpReceiveNetPacket : ReceiveNetPacketBase
    {
        /// <summary>
        /// 创建数据包
        /// </summary>
        public static UdpReceiveNetPacket Create(int messageId, byte[] body, int bodyLength)
        {
            UdpReceiveNetPacket packet = ReferencePool.Spawn<UdpReceiveNetPacket>();
            packet.MessageId = messageId;
            packet.Body = body;
            packet.BodyLength = bodyLength;
            return packet;
        }
    }

    /// <summary>
    /// WebSocket 原始数据包
    /// 消息格式：[ID(2)] + [Body]
    /// </summary>
    public class WsReceiveNetPacket : ReceiveNetPacketBase
    {
        /// <summary>
        /// 创建数据包
        /// </summary>
        public static WsReceiveNetPacket Create(int messageId, byte[] body, int bodyLength)
        {
            WsReceiveNetPacket packet = ReferencePool.Spawn<WsReceiveNetPacket>();
            packet.MessageId = messageId;
            packet.Body = body;
            packet.BodyLength = bodyLength;
            return packet;
        }
    }
}

