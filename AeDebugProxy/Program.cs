using NtApiDotNet;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;

namespace ConsoleApp2
{
    class Program
    {
        static NtProcess _process;

        static void Main(string[] args)
        {
            AppDomain.CurrentDomain.ProcessExit += AppDomain_ProcessExit;
            using (var textWriter = new StreamWriter(@"D:\test.txt"))
            {
                foreach (var arg in args)
                    textWriter.WriteLine(arg);
            }

            IntPtr evnt = new IntPtr();

            if (args[0] == "-p")
            {
                _process = NtProcess.Open(int.Parse(args[1]), ProcessAccessRights.MaximumAllowed);
                evnt = new IntPtr(long.Parse(args[3]));
            }
            else
            {
                var config = new NtProcessCreateConfig();
                config.InitFlags |= ProcessCreateInitFlag.IFEOSkipDebugger;
                config.ThreadFlags |= ThreadCreateFlags.Suspended;
                var path = NtFileUtils.DosFileNameToNt(@"D:\Documents\Visual Studio 2019\Projects\ProxyDebugger\ProxyDebugger\ConsoleApp1\bin\Debug\netcoreapp3.1\ConsoleApp1.exe");
                config.ConfigImagePath = path;
                _process = NtProcess.Create(config).Process;
            }

            int currentRefCount = _process.HandleReferenceCount;
            while (true)
            {
                CheckRemoteDebuggerPresent(_process.Handle, out bool debuggerPresent);
                if (debuggerPresent)
                    break;

                // NOTE: Needed whe using Image File Execution Options
                //if (_process.HandleReferenceCount != currentRefCount)
                // break;

                Thread.Sleep(100);
            }

            bool success = SetEvent(evnt);
        }

        private static void AppDomain_ProcessExit(object sender, EventArgs e)
        {
            if (_process != null)
                _process.Terminate(NtStatus.DBG_CONTROL_C);
        }

        [DllImport("Kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool CheckRemoteDebuggerPresent(
            SafeHandle hProcess,
            [MarshalAs(UnmanagedType.Bool)] out bool pbDebuggerPresent
        );

        [DllImport("Kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool CloseHandle(IntPtr handle);

        [DllImport("Kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetEvent(IntPtr handle);

        [DllImport("Kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool DebugBreakProcess(IntPtr handle);
    }
}
