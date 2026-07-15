//------------------------------
// ZEngine
// 作者: Chenyu
//------------------------------

using bq;
using UnityEngine;

public static class LogConfig
{
    /// <summary>
    /// 三种Appender的开关，默认为 false，开启后才会根据对应配置项输出日志
    /// </summary>
    public static string appender0_enable = "false";
    public static string appender1_enable = "true";
    public static string appender2_enable = "true";


    #region 三种Appender的配置
    // Appender 名叫 appender_0，类型为 ConsoleAppender
    public static string appender0_type = "console";
    // 使用系统当地时间
    public static string appender0_timeZone= "localtime";
    // appender_0 会输出所有 6 个等级的日志（注意：不同日志等级之间不要有空格，否则解析失败）
    public static string appender0_levels= "[verbose, debug, info, warning, error, fatal]";


    // Appender 名叫 appender_1，类型为 TextFileAppender
    public static string appender1_type = "text_file";
    // 使用系统当地时间
    public static string appender1_timeZone = "localtime";
    // info 及以上四个等级日志，其余等级会被忽略
    public static string appender1_levels = "[info, warning, error, fatal]";
    // base_dir_type 决定相对路径的基准目录，这里为 1：
    // iOS：/var/mobile/Containers/Data/Application/[APP]/Documents
    // Android：[android.content.Context.getExternalFilesDir()]
    // HarmonyOS：/data/storage/el2/base/cache
    // 其他平台：当前工作目录
    public static string appender1_baseDirType = "1";
    // appender_1 保存的路径为相对路径 bqLog/normal，采用滚动文件：
    // 文件名形如 normal_YYYYMMDD_xxx.log，具体见后文「路径与滚动策略」。
    public static string appender1_fileName = "bqLog/normal/normal";
    // 每个文件最大 10,000,000 字节，超过则新开文件
    public static string appender1_maxFileSize = "10000000";
    // 超过 10 天的旧文件会自动清理
    public static string appender1_expireTimeDays = "10";
    // 同一输出目录下，该 Appender 所有文件总大小超过 100,000,000 字节时，
    // 会按日期从最早文件开始清理
    public static string appender1_capacity_limit = "100000000";


    // Appender 名叫 appender_2，类型为 CompressedFileAppender
    public static string appender2_type = "compressed_file";
    // 使用系统当地时间
    public static string appender2_timeZone = "localtime";
    // 输出所有等级日志
    public static string appender2_levels = "[all]";
    // base_dir_type 决定相对路径的基准目录，这里为 1
    public static string appender2_baseDirType = "1";
    // 保存路径为 ~/bqLog/compress_log，文件名形如 compress_log_YYYYMMDD_xxx.logcompr
    public static string appender2_fileName = "bqLog/compress/compress_log";
    // 每个文件最大 10,000,000 字节，超过则新开文件
    public static string appender2_maxFileSize = "10000000";
    // 超过 10 天的旧文件会自动清理
    public static string appender2_expireTimeDays = "10";
    // 同一输出目录下，该 Appender 所有文件总大小超过 100,000,000 字节时，
    // 会按日期从最早文件开始清理
    public static string appender2_capacity_limit = "100000000";
    // appender_2 输出内容将使用下方 RSA2048 公钥进行混合加密
    public static string appender2_pubKey = "ssh-rsa AAAAB3NzaC1yc2EAAAADAQABAAABAQClR9q0A/Lr71hkMdumSi7pPtcoYPVNy9KVr7Mf1iFALKExKdI7/syqzgzLveoPuMRpLBSbGjWUe62Fs6Xaoe0GTVrnw2oJ+Aeh6/JhzAicOuZVMrVeuXlMYw0XX5BeasDZwA16LBee8yU4v7GkOpY0nyXj57jECFR7fSh8dxvd4eod8Q6as3bLpMwnBzDcJk+rlau0uPFELJyOakcWiX5iJPji3/WuaSnMtuhsr/jvfp8wexvjFdZEfYstvwsvDodsc4NgF+vSHzZC+zMUhSIgg7s6zGXftAGhYcp1vt9yP89qGcLZGX71o+8maZtguFef2R4mdN5sLcL03K/tCTfp chenyu@DESKTOP-PHJPLPG";
    #endregion

    /// <summary>
    /// 基础日志功能相关配置项
    /// </summary>
    // 整体异步缓冲区大小为 65535 字节，具体含义见后文
    public static string log_buffer_size = "65535";
    // 开启日志 Crash 复盘功能，详见「程序异常退出的数据保护」
    public static string log_recovery = "true";
    // 仅当日志 Category 匹配以下任一通配符时才处理日志，其余忽略
    public static string log_categories_mask = "";
    // 使用异步日志模式（推荐）
    public static string log_thread_mode = "async";
    // 当日志等级为 error 或 fatal 时，在每条日志后附带调用栈
    public static string log_print_stack_levels = "[error, fatal]";

    /// <summary>
    /// 快照功能相关配置项，快照功能会将满足条件的日志内容保存在内存中，供程序在发生崩溃时提取出来辅助分析问题。
    /// </summary>
    // 启用快照功能，快照缓冲区大小为 64KB
    public static string snapshot_buffer_size = "65536";
    // 仅记录 info 和 error 等级的日志到快照
    public static string snapshot_levels = "[info, error, fatal]";
    // 仅当 Category 为 ModuleA.SystemA.ClassA 或以 ModuleB 开头时，才记录到快照
    public static string snapshot_categories_mask = "";



    public static string GetConfig()
    {
        string cfg = $@"
            appenders_config.appender_0.type={appender0_type}
            appenders_config.appender_0.time_zone={appender0_timeZone}
            appenders_config.appender_0.levels={appender0_levels}
            appenders_config.appender_0.enable={appender0_enable}
            appenders_config.appender_1.type={appender1_type}
            appenders_config.appender_1.time_zone={appender1_timeZone}
            appenders_config.appender_1.levels={appender1_levels}
            appenders_config.appender_1.base_dir_type={appender1_baseDirType}
            appenders_config.appender_1.file_name={appender1_fileName}
            appenders_config.appender_1.max_file_size={appender1_maxFileSize}
            appenders_config.appender_1.expire_time_days={appender1_expireTimeDays}
            appenders_config.appender_1.capacity_limit={appender1_capacity_limit}
            appenders_config.appender_1.enable={appender1_enable}
            appenders_config.appender_2.type={appender2_type}
            appenders_config.appender_2.time_zone={appender2_timeZone}
            appenders_config.appender_2.levels={appender2_levels}
            appenders_config.appender_2.base_dir_type={appender2_baseDirType}
            appenders_config.appender_2.file_name={appender2_fileName}
            appenders_config.appender_2.max_file_size={appender2_maxFileSize}
            appenders_config.appender_2.expire_time_days={appender2_expireTimeDays}
            appenders_config.appender_2.capacity_limit={appender2_capacity_limit}
            appenders_config.appender_2.pub_key={appender2_pubKey}
            appenders_config.appender_2.enable={appender2_enable}
            log.buffer_size={log_buffer_size}
            log.recovery={log_recovery}
            log.categories_mask={log_categories_mask}
            log.thread_mode={log_thread_mode}
            log.print_stack_levels={log_print_stack_levels}
            snapshot.buffer_size={snapshot_buffer_size}
            snapshot.levels={snapshot_levels}
            snapshot.categories_mask={snapshot_categories_mask}
        ";

        return cfg;
    }
}