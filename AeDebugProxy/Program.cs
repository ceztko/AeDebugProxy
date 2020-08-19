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

            while (true)
            {
                bool beingDebugged;
                if (_process.Wow64)
                {
                    PartialPeb32 peb = (PartialPeb32)_process.GetPeb();
                    beingDebugged = peb.BeingDebugged == 1;
                }
                else
                {
                    PartialPeb peb = (PartialPeb)_process.GetPeb();
                    beingDebugged = peb.BeingDebugged == 1;
                }

                if (beingDebugged)
                    break;

                Thread.Sleep(100);
            }
            bool success = SetEvent(evnt);
            CloseHandle(evnt);
            _process = null;
        }

        private static void AppDomain_ProcessExit(object sender, EventArgs e)
        {
            if (_process != null)
                _process.Terminate(NtStatus.DBG_CONTROL_C);
        }

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
