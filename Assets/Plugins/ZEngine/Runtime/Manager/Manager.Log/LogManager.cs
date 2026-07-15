//------------------------------
// ZEngine
// 作者: Chenyu
//------------------------------

using bq;
using bq.tools;
using Cysharp.Threading.Tasks;
using System;
using UnityEngine;
using ZEngine.Core;
using ZEngine.Manager.Http;

namespace ZEngine.Manager.Log
{
    public class LogManager : ManagerSingleton<LogManager>, IManager
    {
        private bq.log _log;
        private const string _upLoadUrl = "https://your-log-server.com/upload";

        #region 生命周期
        public void OnInit(object param)
        {
            _root = new GameObject("[Z][LogManager]");
            GameObject.DontDestroyOnLoad(_root);

            string cfg = LogConfig.GetConfig();
            _log = bq.log.create_log("ai-game", cfg);

            // 接管 ZEngineLog，统一路由到 LogManager，避免两套系统并存
            ZEngineLog.SetCallback((level, msg) =>
            {
                switch (level)
                {
                    case ELogLevel.Log:       Info(msg);    break;
                    case ELogLevel.Warning:   Warning(msg); break;
                    case ELogLevel.Error:     Error(msg);   break;
                    case ELogLevel.Exception: Fatal(msg);   break;
                }
            });
        }

        public void OnUpdate()
        {
            
        }

        public void OnDestroy()
        {
            _log = null;
            DestroySingleton();
        }

        public void OnGUI()
        {
            
        }

        #endregion


        #region 日志输出
        public void Verbose(string msg)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            UnityEngine.Debug.Log(msg);
#endif
            _log?.verbose(msg);
        }

        public void Debug(string msg)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            UnityEngine.Debug.Log(msg);
#endif
            _log?.debug(msg);
        }

        public void Info(string msg)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            UnityEngine.Debug.Log(msg);
#endif
            _log?.info(msg);
        }

        public void Warning(string msg)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            UnityEngine.Debug.LogWarning(msg);
#endif
            _log?.warning(msg);
        }

        public void Error(string msg)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            UnityEngine.Debug.LogError(msg);
#endif
            _log?.error(msg);
            TriggerEmergencyLog(LogLevel.Error, msg);
        }

        public void Fatal(string msg)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            UnityEngine.Debug.LogError(msg);
#endif
            _log?.fatal(msg);
            TriggerEmergencyLog(LogLevel.Fatal, msg);
        }

        #endregion  


        #region 常用日志调用
        /// <summary>
        /// 直接通过bq.log对象调用底层接口，适用于需要使用bq.log特有功能的场景，如格式化输出、日志快照等
        /// </summary>
        /// <returns></returns>
        public bq.log GetLog()
        {
            return _log;
        }

        /// <summary>
        /// 获得格式化后的最近日志字符串（需要提前配置好快照相关设置）
        /// 某些场景（如异常检测、关键事件上报）需要获取「最近一段时间」的日志快照
        /// </summary>
        /// <param name="snapshotName">使用此来指定日志文本的时间显示，例如：“localtime”、“gmt”、“Z”、“UTC”、“UTC+8”、“UTC-11”、“UTC+11：30”</param>
        public string TakeSnapShot(string snapshotName = "localtime")
        {
            return _log?.take_snapshot(snapshotName);
        }

        /// <summary>
        /// 强制刷新日志缓冲区，将所有待输出日志立即写入目标位置（如文件、控制台等）
        /// </summary>
        public void Flush()
        {
            _log?.force_flush();
        }


        /// <summary>
        /// 解码日志文件，返回解码后的字符串内容
        /// </summary>
        /// <param name="logFileAbsolutePath">日志文件的绝对路径</param>
        /// <param name="privKey">解码私钥</param>
        /// <returns></returns>
        public string DecodeLogFileToString(string logFileAbsolutePath, string privKey = "")
        {
            if (string.IsNullOrEmpty(logFileAbsolutePath))
            {
                UnityEngine.Debug.LogError("DecodeLogFileToString: logFileAbsolutePath 不能为空");
                return "";
            }

            log_decoder decoder = new log_decoder(logFileAbsolutePath, privKey);

            // 检查构造器是否成功
            var initResult = decoder.get_last_decode_result();
            if (initResult != log_decoder.appender_decode_result.success)
            {
                UnityEngine.Debug.LogError($"log_decoder 初始化失败: {initResult}, 路径: {logFileAbsolutePath}");
                return "";
            }

            string decodedLog = "";
            log_decoder.appender_decode_result result;
            while ((result = decoder.decode()) == log_decoder.appender_decode_result.success)
            {
                decodedLog += decoder.get_last_decoded_log_item();
                decodedLog += "\n";
            }

            if (result != log_decoder.appender_decode_result.eof)
            {
                UnityEngine.Debug.LogError($"解码中断: {result}");
            }

            return decodedLog;
        }

        /// <summary>
        /// 解码日志文件，直接将解码后的内容写入指定的输出文件
        /// </summary>
        /// <param name="logFileAbsolutePath">日志文件的绝对路径</param>
        /// <param name="outputFileAbsolutePath">解码后日志文件的保存路径</param>
        /// <param name="privKey">解码私钥</param>
        /// <returns></returns>
        public bool DecodeLogFileToFile(string logFileAbsolutePath, string outputFileAbsolutePath, string privKey = "")
        {
            if(string.IsNullOrEmpty(logFileAbsolutePath) || string.IsNullOrEmpty(outputFileAbsolutePath))
            {
                UnityEngine.Debug.LogError("DecodeLogFileToFile: logFileAbsolutePath 和 outputFileAbsolutePath 不能为空");
                return false;
            }

            return log_decoder.decode_file(logFileAbsolutePath, outputFileAbsolutePath, privKey);
        }
        #endregion


        #region 日志上传

        /// <summary>
        /// 紧急日志上传
        /// </summary>
        /// <param name="level"></param>
        /// <param name="msg"></param>
        private void TriggerEmergencyLog(LogLevel level, string msg)
        {
            Flush();
            UploadAsync(level, msg).Forget();
        }

        // 上传的是一个Json对象，需要服务器端配合解析
        private async UniTaskVoid UploadAsync(LogLevel level, string msg)
        {
            if (string.IsNullOrEmpty(_upLoadUrl))
            {
                UnityEngine.Debug.LogWarning("[LogUpLoad] 上传URL未配置，无法上传日志");
                return;
            }

            string snapshot = TakeSnapShot();
            //UnityEngine.Debug.Log($"[LogUpLoad] 准备上传日志 Snapshot: {snapshot}");

            var payload = new LogUploadPayLoad
            {
                Level = level.ToString(),
                Message = msg,
                Timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
                DeciedId = SystemInfo.deviceUniqueIdentifier,
                Platform = Application.platform.ToString(),
                AppVersion = Application.version,
            };

            HttpManager.Instance.SetHeader("Content-Type", "application/json"); // 防止HttpManager未及时初始化
            var response = await HttpManager.Instance.PostAsync(_upLoadUrl, payload);
            if (response.IsSuccess)
            {
                UnityEngine.Debug.Log($"[LogUpLoad] 上传成功 [{level}]");
            }
            else
            {
                UnityEngine.Debug.LogError($"[LogUpLoad] 上传失败: {response.Error}");
            }
        }
        #endregion
    }
}