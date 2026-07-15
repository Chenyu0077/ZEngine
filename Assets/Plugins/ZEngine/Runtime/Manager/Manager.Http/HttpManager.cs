//------------------------------
// ZEngine
// 作者: Chenyu
//------------------------------

using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using UnityEngine;
using UnityEngine.Networking;
using ZEngine.Core;
using ZEngine.Manager.Log;

namespace ZEngine.Manager.Http
{
    /// <summary>
    /// HTTP 管理器
    /// 基于 UnityWebRequest 的 HTTP 短连接请求管理
    /// </summary>
    public class HttpManager : ManagerSingleton<HttpManager>, IManager
    {
        #region 属性

        /// <summary>
        /// 基础 URL（如：https://api.example.com）
        /// </summary>
        private string BaseUrl { get; set; } = "";

        /// <summary>
        /// 默认超时时间（秒）
        /// </summary>
        private int DefaultTimeout { get; set; } = 30;

        /// <summary>
        /// 默认请求头
        /// </summary>
        private Dictionary<string, string> DefaultHeaders { get; } = new Dictionary<string, string>();
        private readonly object _headersLock = new object();

        #endregion

        #region 生命周期

        public void OnInit(object param)
        {
            //检测依赖模块
            if (ZEngineMain.Contains(typeof(LogManager)) == false)
                throw new Exception($"{nameof(HttpManager)}依赖于{nameof(LogManager)}");

            _root = new GameObject("[Z][HttpManager]");
            GameObject.DontDestroyOnLoad(_root);

            // 设置默认请求头
            lock (_headersLock)
            {
                DefaultHeaders["Content-Type"] = "application/json";
            }
        }

        public void OnUpdate()
        {

        }

        public void OnDestroy()
        {
            lock (_headersLock)
            {
                DefaultHeaders.Clear();
            }
            DestroySingleton();
        }

        public void OnGUI()
        {

        }

        #endregion

        #region GET 请求

        /// <summary>
        /// 发送 GET 请求
        /// </summary>
        /// <param name="url">请求地址（相对路径或完整URL）</param>
        /// <param name="headers">额外请求头</param>
        /// <param name="timeout">超时时间（秒），0表示使用默认值</param>
        public async UniTask<HttpResponse> GetAsync(string url, Dictionary<string, string> headers = null, int timeout = 0, CancellationToken cancellationToken = default)
        {
            return await SendRequestAsync(url, "GET", null, headers, timeout, cancellationToken);
        }

        /// <summary>
        /// 发送 GET 请求（带泛型响应）
        /// </summary>
        public async UniTask<HttpResponse<T>> GetAsync<T>(string url, Dictionary<string, string> headers = null, int timeout = 0, CancellationToken cancellationToken = default)
        {
            var response = await GetAsync(url, headers, timeout, cancellationToken);
            return ParseResponse<T>(response);
        }

        #endregion

        #region POST 请求

        /// <summary>
        /// 发送 POST 请求
        /// </summary>
        /// <param name="url">请求地址</param>
        /// <param name="body">请求体对象（将序列化为JSON）</param>
        /// <param name="headers">额外请求头</param>
        /// <param name="timeout">超时时间（秒）</param>
        public async UniTask<HttpResponse> PostAsync(string url, object body = null, Dictionary<string, string> headers = null, int timeout = 0, CancellationToken cancellationToken = default)
        {
            string jsonBody = body != null ? JsonConvert.SerializeObject(body) : null;
            return await SendRequestAsync(url, "POST", jsonBody, headers, timeout, cancellationToken);
        }

        /// <summary>
        /// 发送 POST 请求（带泛型响应）
        /// </summary>
        public async UniTask<HttpResponse<T>> PostAsync<T>(string url, object body = null, Dictionary<string, string> headers = null, int timeout = 0, CancellationToken cancellationToken = default)
        {
            var response = await PostAsync(url, body, headers, timeout, cancellationToken);
            return ParseResponse<T>(response);
        }

        /// <summary>
        /// 发送 POST 请求（原始JSON字符串）
        /// </summary>
        public async UniTask<HttpResponse> PostJsonAsync(string url, string jsonBody, Dictionary<string, string> headers = null, int timeout = 0, CancellationToken cancellationToken = default)
        {
            return await SendRequestAsync(url, "POST", jsonBody, headers, timeout, cancellationToken);
        }

        #endregion

        #region PUT 请求

        /// <summary>
        /// 发送 PUT 请求
        /// </summary>
        public async UniTask<HttpResponse> PutAsync(string url, object body = null, Dictionary<string, string> headers = null, int timeout = 0, CancellationToken cancellationToken = default)
        {
            string jsonBody = body != null ? JsonConvert.SerializeObject(body) : null;
            return await SendRequestAsync(url, "PUT", jsonBody, headers, timeout, cancellationToken);
        }

        /// <summary>
        /// 发送 PUT 请求（带泛型响应）
        /// </summary>
        public async UniTask<HttpResponse<T>> PutAsync<T>(string url, object body = null, Dictionary<string, string> headers = null, int timeout = 0, CancellationToken cancellationToken = default)
        {
            var response = await PutAsync(url, body, headers, timeout, cancellationToken);
            return ParseResponse<T>(response);
        }

        #endregion

        #region DELETE 请求

        /// <summary>
        /// 发送 DELETE 请求
        /// </summary>
        public async UniTask<HttpResponse> DeleteAsync(string url, Dictionary<string, string> headers = null, int timeout = 0, CancellationToken cancellationToken = default)
        {
            return await SendRequestAsync(url, "DELETE", null, headers, timeout, cancellationToken);
        }

        /// <summary>
        /// 发送 DELETE 请求（带泛型响应）
        /// </summary>
        public async UniTask<HttpResponse<T>> DeleteAsync<T>(string url, Dictionary<string, string> headers = null, int timeout = 0, CancellationToken cancellationToken = default)
        {
            var response = await DeleteAsync(url, headers, timeout, cancellationToken);
            return ParseResponse<T>(response);
        }

        #endregion

        #region 表单请求

        /// <summary>
        /// 发送表单 POST 请求
        /// </summary>
        public async UniTask<HttpResponse> PostFormAsync(string url, Dictionary<string, string> formData, Dictionary<string, string> headers = null, int timeout = 0, CancellationToken cancellationToken = default)
        {
            string fullUrl = GetFullUrl(url);
            int requestTimeout = timeout > 0 ? timeout : DefaultTimeout;

            using (var request = UnityWebRequest.Post(fullUrl, formData))
            {
                SetHeaders(request, headers);
                request.timeout = requestTimeout;

                CancellationTokenRegistration registration = default;
                if (cancellationToken.CanBeCanceled)
                {
                    registration = cancellationToken.Register(request.Abort);
                }
                try
                {
                    await request.SendWebRequest().WithCancellation(cancellationToken);
                    return CreateResponse(request);
                }
                catch (OperationCanceledException)
                {
                    return HttpResponse.Fail(HttpResult.Cancelled, 0, "请求已取消");
                }
                catch (Exception ex)
                {
                    return HttpResponse.Fail(HttpResult.Unknown, 0, ex.Message);
                }
                finally
                {
                    registration.Dispose();
                }
            }
        }

        /// <summary>
        /// 发送表单 POST 请求（带泛型响应）
        /// </summary>
        public async UniTask<HttpResponse<T>> PostFormAsync<T>(string url, Dictionary<string, string> formData, Dictionary<string, string> headers = null, int timeout = 0, CancellationToken cancellationToken = default)
        {
            var response = await PostFormAsync(url, formData, headers, timeout, cancellationToken);
            return ParseResponse<T>(response);
        }

        #endregion

        #region 下载请求

        /// <summary>
        /// 下载文件（返回字节数组）
        /// </summary>
        public async UniTask<HttpResponse> DownloadAsync(string url, Dictionary<string, string> headers = null, int timeout = 0, IProgress<float> progress = null, CancellationToken cancellationToken = default)
        {
            string fullUrl = GetFullUrl(url);
            int requestTimeout = timeout > 0 ? timeout : DefaultTimeout;

            using (var request = UnityWebRequest.Get(fullUrl))
            {
                SetHeaders(request, headers);
                request.timeout = requestTimeout;

                var operation = request.SendWebRequest();

                CancellationTokenRegistration registration = default;
                if (cancellationToken.CanBeCanceled)
                {
                    registration = cancellationToken.Register(request.Abort);
                }

                try
                {
                    // 报告进度
                    while (!operation.isDone)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        progress?.Report(request.downloadProgress);
                        await UniTask.Yield();
                    }

                    progress?.Report(1f);
                    return CreateResponse(request);
                }
                catch (OperationCanceledException)
                {
                    return HttpResponse.Fail(HttpResult.Cancelled, request.responseCode, "请求已取消");
                }
                catch (Exception ex)
                {
                    return HttpResponse.Fail(HttpResult.Unknown, 0, ex.Message);
                }
                finally
                {
                    registration.Dispose();
                }
            }
        }

        /// <summary>
        /// 下载文本文件
        /// </summary>
        public async UniTask<HttpResponse<string>> DownloadTextAsync(string url, Dictionary<string, string> headers = null, int timeout = 0, CancellationToken cancellationToken = default)
        {
            var response = await DownloadAsync(url, headers, timeout, null, cancellationToken);
            if (response.IsSuccess)
            {
                return HttpResponse<string>.FromResponse(response, response.RawText);
            }
            return HttpResponse<string>.FromResponse(response);
        }

        #endregion

        #region 内部方法

        /// <summary>
        /// 发送请求核心方法
        /// </summary>
        private async UniTask<HttpResponse> SendRequestAsync(string url, string method, string jsonBody, Dictionary<string, string> headers, int timeout, CancellationToken cancellationToken = default)
        {
            string fullUrl = GetFullUrl(url);
            int requestTimeout = timeout > 0 ? timeout : DefaultTimeout;

            using (var request = new UnityWebRequest(fullUrl, method))
            {
                // 设置请求体
                if (!string.IsNullOrEmpty(jsonBody))
                {
                    byte[] bodyBytes = Encoding.UTF8.GetBytes(jsonBody);
                    request.uploadHandler = new UploadHandlerRaw(bodyBytes);
                }

                // 设置响应处理器
                request.downloadHandler = new DownloadHandlerBuffer();

                // 设置请求头
                SetHeaders(request, headers);

                // 设置超时
                request.timeout = requestTimeout;

                CancellationTokenRegistration registration = default;
                if (cancellationToken.CanBeCanceled)
                {
                    registration = cancellationToken.Register(request.Abort);
                }

                try
                {
                    await request.SendWebRequest().WithCancellation(cancellationToken);
                    return CreateResponse(request);
                }
                catch (OperationCanceledException)
                {
                    return HttpResponse.Fail(HttpResult.Cancelled, 0, "请求已取消");
                }
                catch (Exception ex)
                {
                    return HttpResponse.Fail(HttpResult.Unknown, 0, ex.Message);
                }
                finally
                {
                    registration.Dispose();
                }
            }
        }

        /// <summary>
        /// 获取完整URL
        /// </summary>
        private string GetFullUrl(string url)
        {
            if (string.IsNullOrEmpty(url))
            {
                if (string.IsNullOrEmpty(BaseUrl))
                    throw new ArgumentException("[HttpManager] URL 和 BaseUrl 均为空，无法构建请求地址");
                return BaseUrl;
            }

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
                foreach (var header in DefaultHeaders)
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

        /// <summary>
        /// 创建响应对象
        /// </summary>
        private HttpResponse CreateResponse(UnityWebRequest request)
        {
            var statusCode = request.responseCode;

            // 检查网络错误（含超时：Unity 超时后 result == ConnectionError 且 error 含 "timeout"）
            if (request.result == UnityWebRequest.Result.ConnectionError)
            {
                if (request.error != null && request.error.IndexOf("timeout", StringComparison.OrdinalIgnoreCase) >= 0)
                    return HttpResponse.Fail(HttpResult.Timeout, statusCode, request.error);
                return HttpResponse.Fail(HttpResult.NetworkError, statusCode, request.error);
            }

            // 检查协议错误
            if (request.result == UnityWebRequest.Result.ProtocolError)
            {
                HttpResult result = statusCode >= 500 ? HttpResult.ServerError : HttpResult.ClientError;
                return HttpResponse.Fail(result, statusCode, request.error);
            }

            // 检查数据处理错误
            if (request.result == UnityWebRequest.Result.DataProcessingError)
            {
                return HttpResponse.Fail(HttpResult.ParseError, statusCode, request.error);
            }

            // 获取响应头
            var headers = new Dictionary<string, string>();
            var responseHeaders = request.GetResponseHeaders();
            if (responseHeaders != null)
            {
                foreach (var header in responseHeaders)
                {
                    headers[header.Key] = header.Value;
                }
            }

            // 成功
            return HttpResponse.Success(
                statusCode,
                request.downloadHandler?.text,
                request.downloadHandler?.data,
                headers
            );
        }

        /// <summary>
        /// 解析响应为泛型对象
        /// </summary>
        private HttpResponse<T> ParseResponse<T>(HttpResponse response)
        {
            if (!response.IsSuccess)
            {
                return HttpResponse<T>.FromResponse(response);
            }

            try
            {
                T data = JsonConvert.DeserializeObject<T>(response.RawText);
                return HttpResponse<T>.FromResponse(response, data);
            }
            catch (Exception ex)
            {
                var errorResponse = HttpResponse.Fail(HttpResult.ParseError, response.StatusCode, $"JSON解析失败: {ex.Message}");
                errorResponse.RawText = response.RawText;
                return HttpResponse<T>.FromResponse(errorResponse);
            }
        }

        #endregion

        #region 便捷方法

        /// <summary>
        /// 设置 Authorization 请求头
        /// </summary>
        public void SetAuthorization(string token)
        {
            lock (_headersLock)
            {
                if (string.IsNullOrEmpty(token))
                {
                    DefaultHeaders.Remove("Authorization");
                }
                else
                {
                    DefaultHeaders["Authorization"] = token;
                }
            }
        }

        /// <summary>
        /// 设置 Bearer Token
        /// </summary>
        public void SetBearerToken(string token)
        {
            lock (_headersLock)
            {
                if (string.IsNullOrEmpty(token))
                {
                    DefaultHeaders.Remove("Authorization");
                }
                else
                {
                    DefaultHeaders["Authorization"] = $"Bearer {token}";
                }
            }
        }

        /// <summary>
        /// 清除 Authorization 请求头
        /// </summary>
        public void ClearAuthorization()
        {
            lock (_headersLock)
            {
                DefaultHeaders.Remove("Authorization");
            }
        }

        /// <summary>
        /// 向默认请求头里添加新请求头
        /// </summary>
        public void SetHeader(string key, string value)
        {
            lock (_headersLock)
            {
                DefaultHeaders[key] = value;
            }
        }

        /// <summary>
        /// 从默认请求头里移除某个请求头
        /// </summary>
        public void RemoveHeader(string key)
        {
            lock (_headersLock)
            {
                if (DefaultHeaders.ContainsKey(key))
                    DefaultHeaders.Remove(key);
            }
        }

        /// <summary>
        /// 清空默认请求头
        /// </summary>
        public void ClearDefaultHeaders()
        {
            lock (_headersLock)
            {
                DefaultHeaders.Clear();
            }
        }

        /// <summary>
        /// 设置默认URL
        /// </summary>
        /// <param name="url"></param>
        public void SetBaseUrl(string url)
        {
            BaseUrl = url;
        }

        /// <summary>
        /// 设置默认超时时间
        /// </summary>
        /// <param name="timeout"></param>
        public void SetDefaultTimeout(int timeout)
        {
            DefaultTimeout = timeout;
        }

        #endregion
    }
}
