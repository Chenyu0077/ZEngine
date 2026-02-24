//------------------------------
// ZEngine
// 作者: Chenyu
//------------------------------

using UnityEngine;
using System;

namespace ZEngine.Core
{
    /// <summary>
	/// 日志等级
	/// </summary>
	public enum ELogLevel
    {
        /// <summary>
        /// 信息
        /// </summary>
        Log,

        /// <summary>
        /// 警告
        /// </summary>
        Warning,

        /// <summary>
        /// 错误
        /// </summary>
        Error,

        /// <summary>
        /// 异常
        /// </summary>
        Exception,
    }

    public class ZEngineLog
    {
        private static Action<ELogLevel, string> _callback;

        /// <summary>
        /// 监听日志
        /// </summary>
        public static void RegisterCallback(Action<ELogLevel, string> callback)
        {
            _callback += callback;
        }

        /// <summary>
        /// 日志
        /// </summary>
        public static void Log(string info)
        {
            _callback?.Invoke(ELogLevel.Log, $"[ZLog] {info}");
        }

        /// <summary>
        /// 警告
        /// </summary>
        public static void Warning(string info)
        {
            _callback?.Invoke(ELogLevel.Warning, $"[ZLog] {info}");
        }

        /// <summary>
        /// 错误
        /// </summary>
        public static void Error(string info)
        {
            _callback?.Invoke(ELogLevel.Error, $"[ZLog] {info}");
        }

        /// <summary>
        /// 异常
        /// </summary>
        public static void Exception(string info)
        {
            _callback?.Invoke(ELogLevel.Exception, $"[ZLog] {info}");
        }
    }
}
