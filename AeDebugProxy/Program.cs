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
        static NtThread _thread;
        static NtProcess _process;
        static IntPtr _waitHandle;

        static void Main(string[] args)
        {
            AppDomain.CurrentDomain.ProcessExit += AppDomain_ProcessExit;
            using (var textWriter = new StreamWriter(@"D:\test.txt"))
            {
                foreach (var arg in args)
                    textWriter.WriteLine(arg);
            }

            try
            {
                if (args[0] == "-p")
                {
                    _process = NtProcess.Open(int.Parse(args[1]), ProcessAccessRights.MaximumAllowed);
                    _waitHandle = new IntPtr(long.Parse(args[3]));
                }
                else
                {
                    var config = new NtProcessCreateConfig();
                    config.InitFlags |= ProcessCreateInitFlag.IFEOSkipDebugger;
                    config.ThreadFlags |= ThreadCreateFlags.Suspended;
                    var path = NtFileUtils.DosFileNameToNt(args[0]);
                    config.ConfigImagePath = path;
                    var result = NtProcess.Create(config);
                    _process = result.Process;
                    _thread = result.Thread;
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

                if (_thread != null)
                    _thread.Resume();

                if (_waitHandle != IntPtr.Zero)
                    SetEvent(_waitHandle);
            }
            finally
            {
                cleanUp(false);
            }
        }

        private static void AppDomain_ProcessExit(object sender, EventArgs e)
        {
            cleanUp(true);
        }

        static void cleanUp(bool terminate)
        {
            if (_thread != null)
            {
                _thread.Close();
                _thread = null;
            }

            if (_process != null)
            {
                if (terminate)
                    _process.Terminate(NtStatus.DBG_CONTROL_C, false);
                else
                    _process.Close();
                _process = null;
            }

            if (_waitHandle != IntPtr.Zero)
            {
                CloseHandle(_waitHandle);
                _waitHandle = IntPtr.Zero;
            }
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
