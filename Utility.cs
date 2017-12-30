using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace CalMedUpdater
{
    public static class Utility
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        extern static bool IsWow64Process(IntPtr hProcess, [MarshalAs(UnmanagedType.Bool)] out bool isWow64);
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        extern static IntPtr GetCurrentProcess();
        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        extern static IntPtr GetModuleHandle(string moduleName);
        [DllImport("kernel32.dll", CharSet = CharSet.Ansi, SetLastError = true)]
        extern static IntPtr GetProcAddress(IntPtr hModule, string methodName);

        private static bool ModuleContainsFunction(string moduleName, string methodName)
        {
            IntPtr hModule = GetModuleHandle(moduleName);
            if (hModule != IntPtr.Zero)
                return GetProcAddress(hModule, methodName) != IntPtr.Zero;
            return false;
        }
        public static bool Is64Win()
        {
            bool isWow64;
            return IntPtr.Size == 8 || (ModuleContainsFunction("kernel32.dll", "IsWow64Process") && IsWow64Process(GetCurrentProcess(), out isWow64) && isWow64);
        }
    }
}
