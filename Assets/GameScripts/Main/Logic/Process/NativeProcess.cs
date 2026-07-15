using System;
using System.Runtime.InteropServices;

namespace Main.Logic
{
    /// <summary>
    /// 基于 Win32 API 的进程封装。
    ///
    /// 背景：IL2CPP 运行时不支持 System.Diagnostics.Process.Start 启动外部进程
    /// （抛 Win32Exception，NativeErrorCode=0）。Mono 后端可用，IL2CPP 不可用。
    /// 本类直接 P/Invoke kernel32 的 CreateProcess 等原生 API，在 IL2CPP / Mono 下均可工作。
    ///
    /// 仅支持 Windows。仅覆盖 ServerLauncher 需要的能力：启动、查存活、查退出码、终止。
    /// </summary>
    public sealed class NativeProcess : IDisposable
    {
        public int Id { get; private set; }

        private IntPtr _processHandle = IntPtr.Zero;
        private IntPtr _threadHandle  = IntPtr.Zero;

        // 进程仍在运行时 GetExitCodeProcess 返回的值
        private const uint STILL_ACTIVE = 259;

        /// <summary>
        /// 启动一个新进程。失败抛异常（含 Win32 错误码）。
        /// </summary>
        /// <param name="exePath">exe 绝对路径</param>
        /// <param name="arguments">命令行参数（不含 exe 本身）</param>
        /// <param name="workingDir">工作目录</param>
        /// <param name="createNoWindow">true 则不创建控制台窗口</param>
        public static NativeProcess Start(string exePath, string arguments, string workingDir, bool createNoWindow)
        {
            // CreateProcess 的 lpCommandLine 第一个 token 应是程序路径，含空格需加引号
            string commandLine = $"\"{exePath}\" {arguments}";

            var si = new STARTUPINFO();
            si.cb = Marshal.SizeOf(typeof(STARTUPINFO));

            uint creationFlags = CREATE_NEW_PROCESS_GROUP;
            if (createNoWindow)
                creationFlags |= CREATE_NO_WINDOW;

            bool ok = CreateProcess(
                lpApplicationName: exePath,
                lpCommandLine: commandLine,
                lpProcessAttributes: IntPtr.Zero,
                lpThreadAttributes: IntPtr.Zero,
                bInheritHandles: false,
                dwCreationFlags: creationFlags,
                lpEnvironment: IntPtr.Zero,        // null = 继承父进程完整环境变量
                lpCurrentDirectory: workingDir,
                lpStartupInfo: ref si,
                lpProcessInformation: out PROCESS_INFORMATION pi);

            if (!ok)
            {
                int err = Marshal.GetLastWin32Error();
                throw new InvalidOperationException($"CreateProcess 失败, Win32ErrorCode={err}, exe={exePath}");
            }

            return new NativeProcess
            {
                Id = (int)pi.dwProcessId,
                _processHandle = pi.hProcess,
                _threadHandle  = pi.hThread,
            };
        }

        /// <summary>进程是否已退出。</summary>
        public bool HasExited
        {
            get
            {
                if (_processHandle == IntPtr.Zero) return true;
                if (!GetExitCodeProcess(_processHandle, out uint code))
                    return true; // 查询失败，视为已退出
                return code != STILL_ACTIVE;
            }
        }

        /// <summary>退出码（进程未退出时返回 -1）。</summary>
        public int ExitCode
        {
            get
            {
                if (_processHandle == IntPtr.Zero) return -1;
                if (GetExitCodeProcess(_processHandle, out uint code) && code != STILL_ACTIVE)
                    return (int)code;
                return -1;
            }
        }

        /// <summary>强制终止进程。</summary>
        public void Kill()
        {
            if (_processHandle == IntPtr.Zero) return;
            if (!HasExited)
                TerminateProcess(_processHandle, 1);
        }

        /// <summary>等待进程退出，最多 milliseconds 毫秒。返回是否已退出。</summary>
        public bool WaitForExit(int milliseconds)
        {
            if (_processHandle == IntPtr.Zero) return true;
            uint r = WaitForSingleObject(_processHandle, (uint)milliseconds);
            return r == 0; // WAIT_OBJECT_0
        }

        public void Dispose()
        {
            if (_threadHandle != IntPtr.Zero)
            {
                CloseHandle(_threadHandle);
                _threadHandle = IntPtr.Zero;
            }
            if (_processHandle != IntPtr.Zero)
            {
                CloseHandle(_processHandle);
                _processHandle = IntPtr.Zero;
            }
        }

        /// <summary>当前进程 PID（P/Invoke，不依赖 System.Diagnostics.Process）。</summary>
        public static int CurrentProcessId => (int)GetCurrentProcessId();

        /// <summary>
        /// 按 PID 打开并终止一个已存在的进程（用于清理端口占用）。
        /// </summary>
        public static void KillByPid(int pid)
        {
            IntPtr h = OpenProcess(PROCESS_TERMINATE | SYNCHRONIZE, false, (uint)pid);
            if (h == IntPtr.Zero) return;
            try
            {
                TerminateProcess(h, 1);
                WaitForSingleObject(h, 2000);
            }
            finally
            {
                CloseHandle(h);
            }
        }

        // ─────────────────────── Win32 P/Invoke ───────────────────────

        private const uint CREATE_NO_WINDOW        = 0x08000000;
        private const uint CREATE_NEW_PROCESS_GROUP = 0x00000200;
        private const uint PROCESS_TERMINATE       = 0x0001;
        private const uint SYNCHRONIZE             = 0x00100000;

        [StructLayout(LayoutKind.Sequential)]
        private struct PROCESS_INFORMATION
        {
            public IntPtr hProcess;
            public IntPtr hThread;
            public uint dwProcessId;
            public uint dwThreadId;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct STARTUPINFO
        {
            public int cb;
            public string lpReserved;
            public string lpDesktop;
            public string lpTitle;
            public int dwX;
            public int dwY;
            public int dwXSize;
            public int dwYSize;
            public int dwXCountChars;
            public int dwYCountChars;
            public int dwFillAttribute;
            public int dwFlags;
            public short wShowWindow;
            public short cbReserved2;
            public IntPtr lpReserved2;
            public IntPtr hStdInput;
            public IntPtr hStdOutput;
            public IntPtr hStdError;
        }

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern bool CreateProcess(
            string lpApplicationName,
            string lpCommandLine,
            IntPtr lpProcessAttributes,
            IntPtr lpThreadAttributes,
            bool bInheritHandles,
            uint dwCreationFlags,
            IntPtr lpEnvironment,
            string lpCurrentDirectory,
            ref STARTUPINFO lpStartupInfo,
            out PROCESS_INFORMATION lpProcessInformation);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool GetExitCodeProcess(IntPtr hProcess, out uint lpExitCode);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool TerminateProcess(IntPtr hProcess, uint uExitCode);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern uint WaitForSingleObject(IntPtr hHandle, uint dwMilliseconds);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, uint dwProcessId);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr hObject);

        [DllImport("kernel32.dll")]
        private static extern uint GetCurrentProcessId();
    }
}
