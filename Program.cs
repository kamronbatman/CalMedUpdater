using IWshRuntimeLibrary;
using System;
using System.Collections.Generic;
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
        [DllImport("shell32.dll")]
        static extern bool SHGetSpecialFolderPath(IntPtr hwndOwner, [Out] StringBuilder lpszPath, int nFolder, bool fCreate);

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
            if (!System.IO.File.Exists(args[0]))
            {
                Console.WriteLine("Failed to open: {0}", args[0]);
                return;
            }

            bool is64Win = Is64Win();

            XmlDocument doc = new XmlDocument();
            doc.Load(args[0]);

            XmlNode root = doc["CalMedUpdater"];

            if (root == null) { Console.WriteLine("Cannot find CalMedUpdater root node in XML configuration."); return; }

            string installPath = is64Win ? root["InstallPath"]["x64"].InnerText : root["InstallPath"]["x86"].InnerText;

            List<ItemizedSt> itemizedSts = new List<ItemizedSt>();

            XmlNode itemizedStsNode = root["ItemizedSts"];

            if (itemizedStsNode != null)
            {
                foreach (XmlNode itemizedStNode in itemizedStsNode.ChildNodes)
                {
                    itemizedSts.Add(new ItemizedSt() { File = itemizedStNode["File"].InnerText, Source = itemizedStNode["Source"].InnerText });
                }
            }

            XmlNode installNodes = root["Installs"];
            List<CalMedInstall> installs = new List<CalMedInstall>();

            foreach( XmlNode node in installNodes.ChildNodes)
            {
                CalMedInstall install = CreateCalMedInstall(node);
                if (install.Is64 == is64Win)
                    installs.Add(install);
            }

            string sha1 = GetSha1(installPath);
            Console.WriteLine("SHA1: {0}", sha1 != null ? sha1 : "Not Installed");
            int installIndex = -1;

            if (sha1 != null)
            {
                for (int i = 0; i < installs.Count; i++)
                {
                    CalMedInstall install = installs[i];

                    if (install.Sha1 == sha1)
                    {
                        installIndex = i;
                        break;
                    }
                }
            }

            for (int i = installIndex + 1; i < installs.Count; i++)
            {
                CalMedInstall install = installs[i];

                Console.WriteLine("Starting Install {0} {1}", install.FilePath, install.Is64 ? "64bit" : "32bit");
                install.PerformInstall(installPath);
                Console.WriteLine("Installation Finished");
                Console.WriteLine("Starting Post Install");
                install.PerformPostInstall(installPath);
                Console.WriteLine("Post Install Finished");
            }

            if (itemizedSts.Count > 0)
            {
                foreach (ItemizedSt item in itemizedSts)
                    CopyItemizedSt(item, installPath);

                Console.WriteLine("Copied ItemizedSts");
            }

            XmlNode shortcutNode = root["DesktopShortcut"];
            if (shortcutNode != null)
            {
                CreateDesktopShortcut(installPath, new DesktopShortcut() { Name = shortcutNode["Name"].InnerText, Arguments = shortcutNode["Arguments"].InnerText });
            }
        }
        private static string getAllUsersDesktopDirectory()
        {
            StringBuilder path = new StringBuilder(260);
            SHGetSpecialFolderPath(IntPtr.Zero, path, 0x19, false);
            return path.ToString();
        }
        private static void CreateDesktopShortcut(string installPath, DesktopShortcut desktopShortcut)
        {
            string shortcutPath = Path.Combine(getAllUsersDesktopDirectory(), String.Format("{0}.lnk", desktopShortcut.Name));
            string mainFilePath = Path.Combine(installPath, "maincds.exe");

            if (System.IO.File.Exists(shortcutPath))
                System.IO.File.Delete(shortcutPath);
            
            WshShell shell = new WshShell();
            IWshShortcut shortcut = shell.CreateShortcut(shortcutPath) as IWshShortcut;
            shortcut.Description = desktopShortcut.Name;
            shortcut.WorkingDirectory = installPath;
            shortcut.TargetPath = mainFilePath;
            shortcut.Arguments = desktopShortcut.Arguments;
            shortcut.IconLocation = mainFilePath;
            shortcut.Save();
        }

        private static void CopyItemizedSt(ItemizedSt item, string installPath)
        {
            string dest = Path.Combine(Path.Combine(installPath, @"rpt\ItemizedSt"), item.File);
            if (System.IO.File.Exists(dest)) { System.IO.File.Delete(dest); }

            System.IO.File.Copy(Path.Combine(item.Source, item.File), dest);
        }

        public static string GetSha1(string installPath)
        {
            string path = Path.Combine(installPath, "maincds.exe");

            if (!System.IO.File.Exists(path)) { return null; }

            using (var sha1 = SHA1.Create())
            {
                using (var stream = System.IO.File.OpenRead(path))
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
