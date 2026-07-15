using Cysharp.Threading.Tasks;
using Hotfix.Core;
using Main.Core;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using UnityEngine.Networking;
using ZEngine.Core;

/// <summary>
/// 重试配置
/// </summary>
public class RetryOptions
{
    /// <summary>
    /// 最大重试次数
    /// </summary>
    public int MaxRetries { get; set; } = 3;
    /// <summary>
    /// 初始重试延迟（毫秒，默认1秒）
    /// </summary>
    public int InitialDelayMs { get; set; } = 1000;
    /// <summary>
    /// 最大重试延迟（毫秒，默认30秒）
    /// </summary>
    public int MaxDelayMs { get; set; } = 30000;
    /// <summary>
    /// 退避倍数（默认2.0，指数级增长）
    /// </summary>
    public float BackoffMultiplier { get; set; } = 2.0f;
    /// <summary>
    /// 重试时回调（参数：当前重试次数、延迟时间（毫秒）、错误信息）
    /// </summary>
    public Action<int, int, string> OnRetry { get; set; }
    /// <summary>
    /// 默认配置
    /// </summary>
    public static RetryOptions Default => new RetryOptions();
    /// <summary>
    /// 不重试配置
    /// </summary>
    public static RetryOptions None => new RetryOptions() { MaxRetries = 0};
}

/// <summary>
/// AI流式请求管理器，支持流式请求、多个并发请求、请求取消、断线自动重新请求，历史记录等功能
/// </summary>
public class AIStreamMgr : Singleton<AIStreamMgr>
{

    #region 属性

    /// <summary>
    /// 基础 URL（如：https://api.example.com）
    /// </summary>
    public string BaseUrl { get; set; } = "";

    /// <summary>
    /// 默认超时时间（秒）
    /// </summary>
    public int DefaultTimeout { get; set; } = 60;

    /// <summary>
    /// 默认请求头
    /// </summary>
    private Dictionary<string, string> _defaultHeaders { get; } = new Dictionary<string, string>();
    private readonly object _headersLock = new object();

    /// <summary>
    /// 活跃的请求（请求ID -> 取消令牌源）
    /// </summary>
    private readonly Dictionary<string, CancellationTokenSource> _activeRequests = new Dictionary<string, CancellationTokenSource>();
    private readonly object _requestsLock = new object();

    /// <summary>
    /// 当前活跃请求数量
    /// </summary>
    public int ActiveRequestCount
    {
        get
        {
            lock (_requestsLock)
            {
                return _activeRequests.Count;
            }
        }
    }

    /// <summary>
    /// 默认重试配置
    /// </summary>
    public RetryOptions DefaultRetryOptions { get; set; } = RetryOptions.Default;

    #endregion


    #region 构造函数
    public AIStreamMgr() : base()
    {
        // 设置默认请求头
        lock (_headersLock)
        {
            _defaultHeaders["Content-Type"] = "application/json";
        }
    }

    protected override void DestroySingleton()
    {
        lock (_headersLock)
        {
            _defaultHeaders.Clear();
        }

        lock (_requestsLock)
        {
            _activeRequests.Clear();
        }

        base.DestroySingleton();
    }
    #endregion


    #region 内部方法
    /// <summary>
    /// 获取完整URL
    /// </summary>
    private string GetFullUrl(string url)
    {
        if (string.IsNullOrEmpty(url))
            return BaseUrl;

        // 如果已经是完整URL，直接返回
        if (url.StartsWith("http://") || url.StartsWith("https://"))
            return url;

        // 拼接基础URL
        if (string.IsNullOrEmpty(BaseUrl))
            return url;

        // 处理斜杠
        string baseUrl = BaseUrl.TrimEnd('/');
        string path = url.StartsWith("/") ? url : "/" + url;
        return baseUrl + path;
    }

    /// <summary>
    /// 设置请求头
    /// </summary>
    private void SetHeaders(UnityWebRequest request, Dictionary<string, string> headers)
    {
        lock (_headersLock)
        {
            // 设置默认请求头
            foreach (var header in _defaultHeaders)
            {
                request.SetRequestHeader(header.Key, header.Value);
            }
        }

        // 设置额外请求头（覆盖默认）
        if (headers != null)
        {
            foreach (var header in headers)
            {
                request.SetRequestHeader(header.Key, header.Value);
            }
        }
    }


    private void RegisterRequest(string requestId, CancellationTokenSource cts)
    {
        lock (_requestsLock)
        {
            _activeRequests[requestId] = cts;
        }
    }

    private void UnregisterRequest(string requestId)
    {
        lock (_requestsLock)
        {
            if(_activeRequests.TryGetValue(requestId, out var cts))
            {
                cts.Dispose();
                _activeRequests.Remove(requestId);
            }
        }
    }

    /// <summary>
    /// 判断是否可以重试
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    private bool IsRetryableError(UnityWebRequest request)
    {
        // 网络错误、连接错误
        if (request.result == UnityWebRequest.Result.ConnectionError)
            return true;

        // 数据处理错误（解析失败等）
        if (request.result == UnityWebRequest.Result.DataProcessingError)
            return true;

        // 特定HTTP状态码
        long code = request.responseCode;

        // 408 请求超时
        // 429 请求过多
        // 500 服务器错误
        // 502 网关错误
        // 503 服务不可用
        // 504 网关超时
        if (code == 408 || code == 429 || code >= 500)
            return true;

        return false;
    }

    /// <summary>
    /// 计算重试延迟时间（指数退避 + 随机抖动）
    /// </summary>
    /// <param name="options"></param>
    /// <param name="retryCount"></param>
    /// <returns></returns>
    private int CalculateDelay(RetryOptions options, int retryCount)
    {
        // 指数退避计算延迟
        double delay = options.InitialDelayMs * Math.Pow(options.BackoffMultiplier, retryCount - 1);

        // 添加随机抖动（±20%），避免多个请求同时重试导致的雪崩效应
        var random = new System.Random();
        double jitter = delay * 0.2f * (random.NextDouble() * 2 - 1);
        delay += jitter;
        return (int)Math.Min(delay, options.MaxDelayMs);
    }
    #endregion


    #region 公共方法

    /// <summary>
    /// 设置默认请求头
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    public void SetDafaultHeader(string key, string value)
    {
        lock (_headersLock)
        {
            _defaultHeaders[key] = value;
        }
    }

    #endregion


    #region 请求管理相关

    /// <summary>
    /// 取消指定请求
    /// </summary>
    /// <param name="requestId"></param>
    /// <returns></returns>
    public bool CancelRequest(string requestId)
    {
        lock (_requestsLock)
        {
            if (_activeRequests.TryGetValue(requestId, out var cts))
            {
                cts.Cancel();
                cts.Dispose();
                _activeRequests.Remove(requestId);
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// 取消所有活跃请求
    /// </summary>
    public void CancelAllRequests()
    {
        lock (_requestsLock)
        {
            foreach (var cts in _activeRequests.Values)
            {
                cts.Cancel();
                cts.Dispose();
            }
            ZEngineLog.Log($"已取消所有请求，共 {_activeRequests.Count} 个");
            _activeRequests.Clear();
        }
    }

    /// <summary>
    /// 判断某个请求是否活跃
    /// </summary>
    public bool IsRequestActive(string requestId)
    {
        lock (_requestsLock)
        {
            return _activeRequests.ContainsKey(requestId);
        }
    }
    #endregion


    #region UnityWebRequest流式POST请求（SSE）

    /// <summary>
    /// 流式 POST 请求（AI对话，自建requestId）
    /// </summary>
    public async UniTask<bool> StreamPostAsync(
        string url,
        ChatRequest body,
        Action<string> onChunk,
        Action onComplete = null,
        Action<string> onError = null,
        Dictionary<string, string> headers = null,
        int timeout = 0,
        RetryOptions retryOptions = null,
        CancellationToken cancellationToken = default)
    {
        string requestId = Guid.NewGuid().ToString("N");
        return await StreamPostAsync(requestId, url, body, onChunk, onComplete, onError, headers, timeout, retryOptions, cancellationToken);
    }

    /// <summary>
    /// 流式 POST 请求（AI对话）
    /// </summary>
    /// <param name="requestId">请求唯一标识，用于取消特定请求</param>
    /// <param name="url">请求地址</param>
    /// <param name="body">请求体对象</param>
    /// <param name="onChunk">每收到一个数据块的回调</param>
    /// <param name="headers">自定义请求头</param>
    /// <param name="timeout">超时时间（秒）</param>
    /// <param name="retryOptions">重试配置（null使用默认配置）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns></returns>
    public async UniTask<bool> StreamPostAsync(
        string requestId,
        string url,
        ChatRequest body,
        Action<string> onChunk,
        Action onComplete = null,
        Action<string> onError = null,
        Dictionary<string, string> headers = null,
        int timeout = 0,
        RetryOptions retryOptions = null,
        CancellationToken cancellationToken = default)
    {

        var options = retryOptions ?? DefaultRetryOptions;

        // 创建内部取消令牌源，与外部令牌关联，并注册请求
        var internalCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        RegisterRequest(requestId, internalCts);

        string fullUrl = GetFullUrl(url);
        int requestTimeout = timeout > 0 ? timeout : DefaultTimeout;
        string jsonBody = body != null ? JsonConvert.SerializeObject(body) : "";

        string lastError = null;

        try
        {
            for (int attempt = 0; attempt < options.MaxRetries; attempt++)
            {
                internalCts.Token.ThrowIfCancellationRequested();

                // 非首次重试要等待延迟
                if(attempt > 0)
                {
                    int delayMs = CalculateDelay(options, attempt);
                    ZEngineLog.Log($"[{requestId}] 第 {attempt} 次重试，等待 {delayMs}ms，原因: {lastError}");
                    options.OnRetry?.Invoke(attempt, delayMs, lastError);
                    await UniTask.Delay(delayMs, cancellationToken: internalCts.Token);
                }

                // 执行请求
                var result = await ExcuteRequestAsync(requestId, fullUrl, jsonBody, onChunk, headers, requestTimeout, internalCts.Token);

                if (result.Success)
                {
                    ZEngineLog.Log($"[{requestId}] 请求成功");
                    onComplete?.Invoke();
                    return true;
                }

                lastError = result.Error;

                // 不可重试的错误，直接返回
                if (!result.Retryable)
                {
                    ZEngineLog.Error($"[{requestId}] {lastError}（不可重试）");
                    onError?.Invoke(lastError);
                    return false;
                }
            }

            // 超过最大重试次数
            ZEngineLog.Error($"[{requestId}] 已达最大重试次数: {lastError}");
            onError?.Invoke(lastError);
            return false;
        }
        catch (OperationCanceledException)
        {
            ZEngineLog.Log($"[{requestId}] 请求被取消");
            onError?.Invoke(lastError);
            return false;
        }
        catch (Exception ex)
        {
            ZEngineLog.Error($"[{requestId}] 请求异常: {ex.Message}");
            onError?.Invoke(ex.Message);
            return false;
        }
        finally
        {
            UnregisterRequest(requestId);
        }
    }

    #endregion


    #region 核心执行方法
    /// <summary>
    /// 执行单次请求
    /// </summary>
    private async UniTask<(bool Success, bool Retryable, string Error)> ExcuteRequestAsync(
        string requestId,
        string fullUrl,
        string jsonBody,
        Action<string> onChunk,
        Dictionary<string, string> headers,
        int timeout,
        CancellationToken cancellationToken)
    {
        using (var request = new UnityWebRequest(fullUrl, "POST"))
        {
            // 请求体
            byte[] bodyBytes = Encoding.UTF8.GetBytes(jsonBody);
            request.uploadHandler = new UploadHandlerRaw(bodyBytes);

            // 流式响应处理器
            request.downloadHandler = new StreamDownloadHandler(onChunk);

            // 请求头
            SetHeaders(request, headers);
            request.timeout = timeout;

            CancellationTokenRegistration registration = default;
            if (cancellationToken.CanBeCanceled)
            {
                registration = cancellationToken.Register(request.Abort);
            }

            try
            {
                var op = request.SendWebRequest();

                while (!op.isDone)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    await UniTask.Yield();
                }

                // 检查请求结果
                if (request.result == UnityWebRequest.Result.Success)
                {
                    return (true, false, null);
                }
                else
                {
                    string errorMsg = $"请求失败: {request.error}, HTTP状态码: {request.responseCode}";
                    bool retyrable = IsRetryableError(request);
                    return (false, retyrable, errorMsg);
                }
            }
            catch (OperationCanceledException)
            {
                throw; // 由外层捕获处理
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                registration.Dispose();
            }
        }
    }
    #endregion
}
