using System;
using System.Runtime.InteropServices;
using System.Text;

namespace CalMedUpdater
{
    public static class Utility
    {
        [DllImport("shell32.dll")]
        public static extern bool SHGetSpecialFolderPath(IntPtr hwndOwner, [Out] StringBuilder lpszPath, int nFolder, bool fCreate);
        
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool IsWow64Process(IntPtr hProcess, [MarshalAs(UnmanagedType.Bool)] out bool isWow64);
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern IntPtr GetCurrentProcess();
        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        static extern IntPtr GetModuleHandle(string moduleName);
        [DllImport("kernel32.dll", CharSet = CharSet.Ansi, SetLastError = true)]
        static extern IntPtr GetProcAddress(IntPtr hModule, string methodName);

        private static bool ModuleContainsFunction(string moduleName, string methodName)
        {
            var hModule = GetModuleHandle(moduleName);
            return hModule != IntPtr.Zero && GetProcAddress(hModule, methodName) != IntPtr.Zero;
        }
        
        private static bool? _is64Win;

        public static bool Is64Win => _is64Win ??= IntPtr.Size == 8
                                                   || ModuleContainsFunction("kernel32.dll", "IsWow64Process")
                                                   && IsWow64Process(GetCurrentProcess(), out var isWow64)
                                                   && isWow64;
    }
}
