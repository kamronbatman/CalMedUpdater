using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Xml;

namespace CalMedUpdater
{
    public class MainProgram
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

        private static bool Is64Win()
        {
            bool isWow64;
            return IntPtr.Size == 8 || (ModuleContainsFunction("kernel32.dll", "IsWow64Process") && IsWow64Process(GetCurrentProcess(), out isWow64) && isWow64);
        }

        public static void Main(string[] args)
        {
            if (!File.Exists(args[0]))
            {
                Console.WriteLine("Failed to open: {0}", args[0]);
                return;
            }

            bool is64Win = Is64Win();

            XmlDocument doc = new XmlDocument();
            doc.Load(args[0]);

            string installPath = is64Win ? doc["InstallPath"]["x64"].InnerText : doc["InstallPath"]["x86"].InnerText;

            List<ItemizedSt> itemizedSts = new List<ItemizedSt>();

            XmlNode itemizedStsNode = doc["ItemizedSts"];

            if (itemizedStsNode != null)
            {
                foreach (XmlNode itemizedStNode in itemizedStsNode.ChildNodes)
                {
                    itemizedSts.Add(new ItemizedSt() { File = itemizedStNode["File"].InnerText, Source = itemizedStNode["Source"].InnerText });
                }
            }

            XmlNode installNodes = doc["Installs"];
            List<CalMedInstall> installs = new List<CalMedInstall>();

            foreach( XmlNode node in installNodes.ChildNodes)
            {
                CalMedInstall install = CreateCalMedInstall(node);
                if (install.Is64 == is64Win)
                    installs.Add(install);
            }

            bool found = false;
            string sha1 = GetSha1(installPath);

            for (int i = 0; i < installs.Count; i++)
            {
                CalMedInstall install = installs[0];

                if (found)
                {
                    Console.WriteLine("Starting Install {0} {1}", install.FilePath);
                    install.PerformInstall(installPath);
                    install.PerformPostInstall(installPath);
                }
                else if (install.Sha1 == sha1)
                    found = true;
            }

            if (itemizedSts.Count > 0)
            {
                foreach (ItemizedSt item in itemizedSts)
                    CopyItemizedSt(item, installPath);

                Console.WriteLine("Copied ItemizedSts");
            }
        }
        private static void CopyItemizedSt(ItemizedSt item, string installPath)
        {
            string dest = Path.Combine(Path.Combine(installPath, @"rpt\ItemizedSt"), item.File);
            if (File.Exists(dest)) { File.Delete(dest); }

            File.Copy(Path.Combine(item.Source, item.File), dest);
        }

        public static string GetSha1(string installPath)
        {
            string path = Path.Combine(installPath, "maincds.exe");

            if (!File.Exists(path)) { return null; }

            using (var sha1 = SHA1.Create())
            {
                using (var stream = File.OpenRead(path))
                {
                    return BitConverter.ToString(sha1.ComputeHash(stream)).Replace("-", "").ToLowerInvariant();
                }
            }
        }

        public static GenericInstall CreateGenericInstall(XmlNode node)
        {
            return new GenericInstall() {
                FilePath = node["FilePath"].InnerText,
                FileArguments = node["FileArguments"].InnerText
            };
        }

        public static CalMedInstall CreateCalMedInstall(XmlNode node)
        {
            CalMedInstall install = new CalMedInstall()
            {
                FilePath = node["FilePath"].InnerText,
                ConfigPath = node["ConfigPath"].InnerText,
                Sha1 = node["SHA1"].InnerText,
                Is64 = Boolean.Parse(node["Is64"].InnerText),
                XerexRegistry = node["XerexRegistry"].InnerText,
            };

            return install;
        }
    }
}
