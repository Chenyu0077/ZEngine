//------------------------------
// ZEngine
// 作者: Chenyu
//------------------------------

/// <summary>
/// 日志等级（Verbose, Debug, Info, Warning, Error, Fatal严重程度由低到高）
/// </summary>
public enum LogLevel
{
    Verbose,
    Debug,
    Info,
    Warning,
    Error,
    Fatal
}

/// <summary>
/// 日志上传信息
/// </summary>
public class LogUploadPayLoad
{
    public string Level;        // 日志等级
    public string Message;      // 日志内容
    public string Snapshot;     // 触发时刻前一段时间的全部日志快照
    public string Timestamp;    // 触发时刻的 UTC 时间
    public string DeciedId;     // 设备唯一标识符，用于区分不同用户/设备上传的日志
    public string Platform;     // 运行平台，如 Android、iOS、Windows 等
    public string AppVersion;   // 应用版本号
}
 