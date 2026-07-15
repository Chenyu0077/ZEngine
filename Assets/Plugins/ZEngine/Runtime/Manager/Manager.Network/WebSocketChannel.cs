//------------------------------
// ZEngine
// 作者: Chenyu
//------------------------------

using Google.Protobuf;
using System;
using System.Collections.Generic;
using WebSocketSharp;
using ZEngine.Reference;
using ZEngine.Core;

namespace ZEngine.Manager.Network
{
    /// <summary>
    /// WebSocket网络通道（基于websocket-sharp）
    /// 用于实时推送、匹配、排行榜等场景
    /// 支持自动重连
    /// </summary>
    public class WebSocketChannel : NetworkChannelBase
    {
        #region 常量

        /// <summary>
        /// 消息头长度（2字节消息ID）
        /// </summary>
        private const int HeaderSize = 2;

        #endregion

        #region 字段

        /// <summary>
        /// WebSocket连接
        /// </summary>
        private WebSocket _webSocket;

        /// <summary>
        /// 连接URL
        /// </summary>
        private string _url;

        /// <summary>
        /// 接收队列
        /// </summary>
        private readonly Queue<WsReceiveNetPacket> _receiveQueue = new Queue<WsReceiveNetPacket>();

        /// <summary>
        /// 文本消息接收队列（主线程消费）
        /// </summary>
        private readonly Queue<string> _textReceiveQueue = new Queue<string>();

        /// <summary>
        /// 文本消息回调（主线程触发）
        /// </summary>
        private Action<string> _onTextMessage;

        /// <summary>
        /// 连接状态事件队列——websocket-sharp 回调在后台线程触发，
        /// 统一入队后由 Update()（主线程）出队执行，避免并发修改 EventManager。
        /// </summary>
        private readonly Queue<Action> _pendingEvents = new Queue<Action>();

        // ========== 重连相关 ==========

        /// <summary>
        /// 当前重连次数
        /// </summary>
        private int _reconnectCount;

        /// <summary>
        /// 是否正在重连
        /// </summary>
        private volatile bool _isReconnecting;

        /// <summary>
        /// 重连等待时间累计
        /// </summary>
        private float _reconnectWaitTime;

        /// <summary>
        /// 是否是用户主动断开（主动断开不触发重连）
        /// </summary>
        private bool _isManualDisconnect;

        // ========== 心跳相关 ==========
        private float _heartbeatTimer;
        private float _heartbeatTimeoutTimer;

        #endregion

        #region 属性

        /// <summary>
        /// 是否使用SSL
        /// </summary>
        public bool UseSsl { get; set; } = false;

        /// <summary>
        /// 通道类型
        /// </summary>
        public override NetworkChannelType ChannelType => NetworkChannelType.WebSocket;

        /// <summary>
        /// 是否启用自动重连
        /// </summary>
        public bool AutoReconnectEnabled { get; set; } = true;

        /// <summary>
        /// 最大重连次数
        /// </summary>
        public int MaxReconnectCount { get; set; } = 5;

        /// <summary>
        /// 重连间隔（秒）
        /// </summary>
        public float ReconnectInterval { get; set; } = 3f;

        /// <summary>
        /// 是否正在重连中
        /// </summary>
        public bool IsReconnecting => _isReconnecting;

        // ========== 心跳配置属性 ==========
        public bool HeartbeatEnabled { get; set; } = true;
        public float HeartbeatInterval { get; set; } = 5f;
        public float HeartbeatTimeout { get; set; } = 15f;
        public int HeartbeatMessageId { get; set; } = 1;
        // 二进制心跳
        public Func<IMessage> HeartbeatMessageFactory { get; set; }
        // Json心跳
        public Func<string> HeartbeatTextFactory { get; set; }

        #endregion

        #region 构造函数

        /// <summary>
        /// 构造函数
        /// </summary>
        public WebSocketChannel(string name, NetworkManager networkManager) : base(name, networkManager)
        {
        }

        #endregion

        #region 生命周期方法

        /// <summary>
        /// 连接服务器
        /// </summary>
        public override void Connect(string host, int port)
        {
            if (State == NetworkState.Connecting || State == NetworkState.Connected)
            {
                ZEngineLog.Warning("[WebSocketChannel] 已经在连接或已连接状态，请先断开");
                return;
            }

            Host = host;
            Port = port;
            _isManualDisconnect = false;
            _reconnectCount = 0;
            _isReconnecting = false;

            // 构建WebSocket URL
            string protocol = UseSsl ? "wss" : "ws";
            _url = $"{protocol}://{host}:{port}";

            DoConnect();
        }

        /// <summary>
        /// 使用完整URL连接
        /// </summary>
        public void Connect(string url)
        {
            if (State == NetworkState.Connecting || State == NetworkState.Connected)
            {
                ZEngineLog.Warning("[WebSocketChannel] 已经在连接或已连接状态，请先断开");
                return;
            }

            _url = url;
            _isManualDisconnect = false;
            _reconnectCount = 0;
            _isReconnecting = false;

            try
            {
                // 解析URL获取host和port
                var uri = new Uri(url);
                Host = uri.Host;
                Port = uri.Port;
            }
            catch
            {
                Host = "";
                Port = 0;
            }

            DoConnect();
        }

        /// <summary>
        /// 断开连接
        /// </summary>
        public override void Disconnect()
        {
            if (State == NetworkState.Disconnected)
                return;

            _isManualDisconnect = true;
            _isReconnecting = false;
            State = NetworkState.Disconnecting;

            try
            {
                if (_webSocket != null)
                {
                    _webSocket.OnOpen -= OnWebSocketOpen;
                    _webSocket.OnClose -= OnWebSocketClose;
                    _webSocket.OnError -= OnWebSocketError;
                    _webSocket.OnMessage -= OnWebSocketMessage;

                    if (_webSocket.ReadyState == WebSocketState.Open ||
                        _webSocket.ReadyState == WebSocketState.Connecting)
                    {
                        _webSocket.CloseAsync();
                    }
                    _webSocket = null;
                }
            }
            catch (Exception ex)
            {
                ZEngineLog.Warning($"[WebSocketChannel] 断开连接异常: {ex.Message}");
            }

            OnDisconnected(DisconnectReason.Manual);
            ZEngineLog.Log("[WebSocketChannel] 已断开连接");
        }
        

        /// <summary>
        /// 发送文本消息（JSON协议专用）
        /// </summary>
        public void SendText(string text)
        {
            if (!IsConnected || _webSocket == null)
            {
                ZEngineLog.Warning("[WebSocketChannel] 未连接，无法发送文本消息");
                return;
            }
            try
            {
                _webSocket.SendAsync(text, null);
            }
            catch (Exception ex)
            {
                ZEngineLog.Error($"[WebSocketChannel] 发送文本失败: {ex.Message}");
            }
        }
        

        /// <summary>
        /// 发送消息
        /// </summary>
        public override void Send(int messageId, IMessage message)
        {
            if (!IsConnected || _webSocket == null)
            {
                ZEngineLog.Warning("[WebSocketChannel] 未连接，无法发送消息");
                return;
            }

            byte[] data = Encode(messageId, message);
            try
            {
                _webSocket.Send(data);
            }
            catch (Exception ex)
            {
                ZEngineLog.Error($"[WebSocketChannel] 发送失败: {ex.Message}");
            }
        }


        /// <summary>
        /// 轮询处理接收队列
        /// </summary>
        public override void Update(float elapseSeconds, float realElapseSeconds)
        {
            // 最先处理挂起的连接状态事件（由后台线程入队，此处主线程出队）
            ProcessPendingEvents();

            // 处理重连逻辑
            if (_isReconnecting)
            {
                UpdateReconnect(elapseSeconds);
                return;
            }

            // 已连接状态下更新心跳
            if (IsConnected && HeartbeatEnabled)
            {
                UpdateHeartbeat(elapseSeconds);
            }

            // 处理接收队列
            UpdateReceiveQueue();
        }

        /// <summary>
        /// 关闭并清理通道
        /// </summary>
        public override void Shutdown()
        {
            _isManualDisconnect = true;
            _isReconnecting = false;
            Disconnect();
            ClearReceiveQueue();
            base.Shutdown();
        }

        #endregion

        #region 消息处理器注册

        /// <summary>
        /// 注册文本消息处理器（主线程回调）
        /// </summary>
        public void RegisterTextHandler(Action<string> handler) => _onTextMessage = handler;

        /// <summary>
        /// 注销文本消息处理器
        /// </summary>
        public void UnregisterTextHandler() => _onTextMessage = null;

        #endregion

        #region 心跳相关

        private void ResetHeartbeatTimeout()
        {
            _heartbeatTimeoutTimer = 0f;
        }

        private void ResetHeartbeat()
        {
            _heartbeatTimer = 0f;
            _heartbeatTimeoutTimer = 0f;
        }

        private void UpdateHeartbeat(float elapseSeconds)
        {
            _heartbeatTimer += elapseSeconds;

            // 发送心跳
            if (_heartbeatTimer >= HeartbeatInterval)
            {
                _heartbeatTimer = 0f;
                SendHeartbeat();
            }

            _heartbeatTimeoutTimer += elapseSeconds;

            // 判断心跳超时
            if (_heartbeatTimeoutTimer >= HeartbeatTimeout)
            {
                ZEngineLog.Warning($"[WebSocketChannel] 心跳超时 ({HeartbeatTimeout}秒未收到消息)，断开连接");
                HandleDisconnect(DisconnectReason.Timeout);
            }
        }

        private void SendHeartbeat()
        {
            if (!IsConnected || _webSocket == null)
                return;

            try
            {
                if (HeartbeatMessageFactory != null)
                {
                    ZEngineLog.Log("[WebSocketChannel] 发送二进制心跳消息");
                    var heartbeatMsg = HeartbeatMessageFactory();
                    if (heartbeatMsg != null)
                    {
                        Send(HeartbeatMessageId, heartbeatMsg);
                    }
                }
                else if (HeartbeatTextFactory != null)
                {
                    ZEngineLog.Log("[WebSocketChannel] 发送文本心跳消息");
                    var heartbeatMsg = HeartbeatTextFactory();
                    if (heartbeatMsg != null)
                    {
                        SendText(heartbeatMsg);
                    } 
                }
                else
                {
                    _webSocket.Ping();
                }
            }
            catch (Exception ex)
            {
                ZEngineLog.Warning($"[WebSocketChannel] 发送心跳失败: {ex.Message}");
            }
        }

        #endregion

        #region 重连相关

        /// <summary>
        /// 开始重连
        /// </summary>
        private void StartReconnect()
        {
            if (!AutoReconnectEnabled || _isManualDisconnect)
            {
                return;
            }

            if (_reconnectCount >= MaxReconnectCount)
            {
                ZEngineLog.Error($"[WebSocketChannel] 重连次数已达上限 ({MaxReconnectCount})");
                _isReconnecting = false;
                OnReconnectFailed();
                return;
            }

            _isReconnecting = true;
            _reconnectWaitTime = 0;
            _reconnectCount++;

            ZEngineLog.Log($"[WebSocketChannel] 准备重连 ({_reconnectCount}/{MaxReconnectCount})，等待 {ReconnectInterval} 秒...");

            // 通知上层正在重连
            _networkManager?.OnChannelReconnecting(this, _reconnectCount, MaxReconnectCount);
        }

        /// <summary>
        /// 更新重连逻辑
        /// </summary>
        private void UpdateReconnect(float elapseSeconds)
        {
            _reconnectWaitTime += elapseSeconds;

            if (_reconnectWaitTime >= ReconnectInterval)
            {
                _reconnectWaitTime = 0;
                DoConnect();
            }
        }

        /// <summary>
        /// 重连失败
        /// </summary>
        private void OnReconnectFailed()
        {
            _networkManager?.OnChannelReconnectFailed(this);
        }

        #endregion

        #region 编解码

        /// <summary>
        /// 编码消息为字节数组
        /// 消息格式：[MessageId(2字节)] + [Body(变长)]
        /// </summary>
        public byte[] Encode(int messageId, IMessage message)
        {
            byte[] bodyData = message.ToByteArray();
            int totalLength = HeaderSize + bodyData.Length;
            byte[] data = new byte[totalLength];

            // 写入消息ID（2字节，小端序）
            data[0] = (byte)(messageId & 0xFF);
            data[1] = (byte)((messageId >> 8) & 0xFF);

            // 写入消息体
            Array.Copy(bodyData, 0, data, HeaderSize, bodyData.Length);

            return data;
        }

        /// <summary>
        /// 解码字节数组，返回 WsNetPacket
        /// 消息格式：[MessageId(2字节)] + [Body(变长)]
        /// </summary>
        public WsReceiveNetPacket Decode(byte[] data)
        {
            if (data == null || data.Length < HeaderSize)
                return null;

            // 读取消息ID（2字节）
            int messageId = data[0] | (data[1] << 8);

            // 提取消息体
            int bodyLength = data.Length - HeaderSize;
            byte[] body = new byte[bodyLength];
            Array.Copy(data, HeaderSize, body, 0, bodyLength);

            return WsReceiveNetPacket.Create(messageId, body, bodyLength);
        }

        #endregion

        #region 接收相关

        /// <summary>
        /// 处理接收队列（二进制 + 文本）
        /// </summary>
        private void UpdateReceiveQueue()
        {
            // 处理二进制（Protobuf）队列
            while (true)
            {
                WsReceiveNetPacket packet = null;
                lock (_receiveLock)
                {
                    if (_receiveQueue.Count == 0) break;
                    packet = _receiveQueue.Dequeue();
                }
                if (packet != null)
                {
                    UnpackNetPacket(packet);
                    ReferencePool.Release(packet);
                }
            }

            // 处理文本（JSON）队列
            while (true)
            {
                string text = null;
                lock (_receiveLock)
                {
                    if (_textReceiveQueue.Count == 0) break;
                    text = _textReceiveQueue.Dequeue();
                }
                if (text != null)
                {
                    try { _onTextMessage?.Invoke(text); }
                    catch (Exception ex) { ZEngineLog.Error($"[WebSocketChannel] 文本消息处理异常: {ex.Message}"); }
                }
            }
        }

        /// <summary>
        /// 清空接收队列（二进制 + 文本）
        /// </summary>
        private void ClearReceiveQueue()
        {
            lock (_receiveLock)
            {
                while (_receiveQueue.Count > 0)
                    ReferencePool.Release(_receiveQueue.Dequeue());
                _textReceiveQueue.Clear();
            }
        }

        /// <summary>
        /// 处理二进制消息
        /// 消息格式：[MessageId(2字节)] + [Body(变长)]
        /// </summary>
        private void ProcessBinaryMessage(byte[] data)
        {
            // 收到消息，重置心跳超时
            ResetHeartbeatTimeout();

            // 解码并加入接收队列
            WsReceiveNetPacket packet = Decode(data);
            if (packet != null)
            {
                lock (_receiveLock)
                {
                    _receiveQueue.Enqueue(packet);
                }
            }
            else
            {
                ZEngineLog.Error("[WebSocketChannel] 解码失败");
            }
        }
        
        /// <summary>
        /// 处理Json文本消息
        /// </summary>
        private void ProcessTestMessage(string data)
        {
            // 收到消息，重置心跳超时
            ResetHeartbeatTimeout();

            if (!string.IsNullOrEmpty(data))
            {
                lock (_receiveLock)
                {
                    _textReceiveQueue.Enqueue(data);
                }
            }
            else
            {
                ZEngineLog.Error("[WebSocketChannel] 文本消息为空");
            }
        }

        #endregion

        #region 内部方法

        /// <summary>
        /// 将连接状态事件入队（线程安全，由后台线程调用）
        /// </summary>
        private void EnqueueEvent(Action action)
        {
            lock (_receiveLock)
                _pendingEvents.Enqueue(action);
        }

        /// <summary>
        /// 出队并执行所有挂起的连接状态事件（主线程调用）
        /// </summary>
        private void ProcessPendingEvents()
        {
            while (true)
            {
                Action action;
                lock (_receiveLock)
                {
                    if (_pendingEvents.Count == 0) break;
                    action = _pendingEvents.Dequeue();
                }
                try { action(); }
                catch (Exception ex) { ZEngineLog.Error($"[WebSocketChannel] 事件处理异常: {ex.Message}"); }
            }
        }

        /// <summary>
        /// 执行连接
        /// </summary>
        private void DoConnect()
        {
            State = NetworkState.Connecting;

            try
            {
                // 创建WebSocket
                _webSocket = new WebSocket(_url);

                // 设置事件处理
                _webSocket.OnOpen += OnWebSocketOpen;
                _webSocket.OnClose += OnWebSocketClose;
                _webSocket.OnError += OnWebSocketError;
                _webSocket.OnMessage += OnWebSocketMessage;

                // 开始异步连接
                _webSocket.ConnectAsync();

                ZEngineLog.Log($"[WebSocketChannel] 正在连接: {_url}");
            }
            catch (Exception ex)
            {
                _webSocket = null;

                if (_isReconnecting)
                {
                    ZEngineLog.Warning($"[WebSocketChannel] 重连异常: {ex.Message}，继续尝试...");
                    StartReconnect();
                }
                else
                {
                    OnConnectFailed(ex.Message);
                    ZEngineLog.Error($"[WebSocketChannel] 连接失败: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 处理断开连接
        /// </summary>
        private void HandleDisconnect(DisconnectReason reason)
        {
            if (State == NetworkState.Disconnected || State == NetworkState.Disconnecting)
                return;

            State = NetworkState.Disconnected;
            ResetHeartbeat();

            try
            {
                if (_webSocket != null)
                {
                    _webSocket.OnOpen -= OnWebSocketOpen;
                    _webSocket.OnClose -= OnWebSocketClose;
                    _webSocket.OnError -= OnWebSocketError;
                    _webSocket.OnMessage -= OnWebSocketMessage;
                    _webSocket = null;
                }
            }
            catch { }

            // 通知断开
            OnDisconnected(reason);

            // 非主动断开，尝试重连
            if (!_isManualDisconnect && AutoReconnectEnabled)
            {
                StartReconnect();
            }
        }

        #endregion

        #region WebSocket 事件处理

        private void OnWebSocketOpen(object sender, EventArgs e)
        {
            ResetHeartbeat();
            bool wasReconnecting = _isReconnecting;
            if (wasReconnecting) { _isReconnecting = false; _reconnectCount = 0; }

            EnqueueEvent(() =>
            {
                if (wasReconnecting)
                {
                    ZEngineLog.Log($"[WebSocketChannel] 重连成功: {_url}");
                    OnConnected();
                    _networkManager?.OnChannelReconnected(this);
                }
                else
                {
                    ZEngineLog.Log($"[WebSocketChannel] 连接成功: {_url}");
                    OnConnected();
                }
            });
        }

        private void OnWebSocketClose(object sender, CloseEventArgs e)
        {
            ZEngineLog.Log($"[WebSocketChannel] 连接关闭: Code={e.Code}, Reason={e.Reason}");

            if (State == NetworkState.Disconnecting)
                return;

            DisconnectReason reason = e.Code switch
            {
                1000 => DisconnectReason.Manual,
                1001 => DisconnectReason.ServerClosed,
                1006 => DisconnectReason.NetworkError,
                _    => DisconnectReason.Unknown
            };

            EnqueueEvent(() => HandleDisconnect(reason));
        }

        private void OnWebSocketError(object sender, ErrorEventArgs e)
        {
            ZEngineLog.Error($"[WebSocketChannel] 错误: {e.Message}");

            string errorMsg = e.Message;
            if (State == NetworkState.Connecting)
            {
                if (_isReconnecting)
                {
                    ZEngineLog.Warning("[WebSocketChannel] 重连失败，继续尝试...");
                    State = NetworkState.Disconnected;
                    EnqueueEvent(StartReconnect);
                }
                else
                {
                    EnqueueEvent(() => OnConnectFailed(errorMsg));
                }
            }
            else
            {
                EnqueueEvent(() => OnError(errorMsg));
            }
        }

        private void OnWebSocketMessage(object sender, MessageEventArgs e)
        {
            // 收到任何消息都重置心跳超时
            ResetHeartbeatTimeout();

            try
            {
                if (e.IsBinary)
                {
                    // 二进制消息
                    byte[] data = e.RawData;
                    ProcessBinaryMessage(data);
                }
                else if (e.IsText)
                {
                    // 文本消息
                    ProcessTestMessage(e.Data);
                }
            }
            catch (Exception ex)
            {
                ZEngineLog.Error($"[WebSocketChannel] 处理消息异常: {ex.Message}");
            }
        }

        #endregion
    }
}

