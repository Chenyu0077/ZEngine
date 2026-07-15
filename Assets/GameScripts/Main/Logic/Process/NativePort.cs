using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Main.Logic
{
    /// <summary>
    /// 通过 Win32 iphlpapi.GetExtendedTcpTable 查询端口占用，返回监听指定端口的进程 PID。
    ///
    /// 替代 "netstat -ano" 命令行方案：IL2CPP 不支持 Process.Start 调用 netstat，
    /// 直接读 TCP 表更稳、更快，且无需解析文本。仅支持 Windows / IPv4。
    /// </summary>
    public static class NativePort
    {
        // MIB_TCP_STATE_LISTEN = 2
        private const uint MIB_TCP_STATE_LISTEN = 2;
        // TCP_TABLE_OWNER_PID_LISTENER = 3（只返回 LISTEN 状态的表，含 PID）
        private const int TCP_TABLE_OWNER_PID_ALL = 5;
        private const int AF_INET = 2;

        /// <summary>
        /// 返回所有监听 targetPort 的进程 PID（通常只有一个，异常情况下可能多个）。
        /// </summary>
        public static List<int> FindListeningPids(int targetPort)
        {
            var result = new List<int>();
            int buffSize = 0;

            // 第一次调用取所需缓冲区大小
            GetExtendedTcpTable(IntPtr.Zero, ref buffSize, true, AF_INET, TCP_TABLE_OWNER_PID_ALL, 0);
            IntPtr tcpTable = Marshal.AllocHGlobal(buffSize);
            try
            {
                uint ret = GetExtendedTcpTable(tcpTable, ref buffSize, true, AF_INET, TCP_TABLE_OWNER_PID_ALL, 0);
                if (ret != 0) return result; // 非 0 表示失败

                int numEntries = Marshal.ReadInt32(tcpTable);
                IntPtr rowPtr = (IntPtr)((long)tcpTable + 4); // 跳过 dwNumEntries
                int rowSize = Marshal.SizeOf(typeof(MIB_TCPROW_OWNER_PID));

                for (int i = 0; i < numEntries; i++)
                {
                    var row = (MIB_TCPROW_OWNER_PID)Marshal.PtrToStructure(rowPtr, typeof(MIB_TCPROW_OWNER_PID));

                    if (row.state == MIB_TCP_STATE_LISTEN)
                    {
                        // 本地端口是网络字节序（大端），需转换
                        int localPort = (int)(((row.localPort & 0xFF) << 8) | ((row.localPort >> 8) & 0xFF));
                        if (localPort == targetPort)
                            result.Add((int)row.owningPid);
                    }

                    rowPtr = (IntPtr)((long)rowPtr + rowSize);
                }
            }
            finally
            {
                Marshal.FreeHGlobal(tcpTable);
            }

            return result;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MIB_TCPROW_OWNER_PID
        {
            public uint state;
            public uint localAddr;
            public uint localPort;   // 网络字节序，低 16 位有效
            public uint remoteAddr;
            public uint remotePort;
            public uint owningPid;
        }

        [DllImport("iphlpapi.dll", SetLastError = true)]
        private static extern uint GetExtendedTcpTable(
            IntPtr pTcpTable,
            ref int pdwSize,
            bool bOrder,
            int ulAf,
            int tableClass,
            uint reserved);
    }
}
