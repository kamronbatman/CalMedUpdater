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
        [DllImport("shell32.dll")]
        static extern bool SHGetSpecialFolderPath(IntPtr hwndOwner, [Out] StringBuilder lpszPath, int nFolder, bool fCreate);

        public static void Main(string[] args)
        {
            if (!System.IO.File.Exists(args[0]))
            {
                Console.WriteLine("Failed to open: {0}", args[0]);
                return;
            }

            bool is64Win = Utility.Is64Win();

            XmlDocument doc = new XmlDocument();
            doc.Load(args[0]);

            XmlNode root = doc["CalMedUpdater"];

            if (root == null) { Console.WriteLine("Cannot find CalMedUpdater root node in XML configuration."); return; }

            string installPath = is64Win ? root["InstallPath"]["x64"].InnerText : root["InstallPath"]["x86"].InnerText;

            XmlNode shortcutNode = root["DesktopShortcut"];
            DesktopShortcut desktopShortcut = null;

            if (shortcutNode != null)
                desktopShortcut = new DesktopShortcut() { Name = shortcutNode["Name"].InnerText, Arguments = shortcutNode["Arguments"].InnerText };

            if (desktopShortcut != null)
                DeleteDesktopShortcut(installPath, desktopShortcut);

            List<ItemizedSt> itemizedSts = new List<ItemizedSt>();

            XmlNode itemizedStsNode = root["ItemizedSts"];

            if (itemizedStsNode != null)
            {
                foreach (XmlNode itemizedStNode in itemizedStsNode.ChildNodes)
                {
                    itemizedSts.Add(new ItemizedSt() { File = itemizedStNode["File"].InnerText, Source = itemizedStNode["Source"].InnerText });
                }
            }

            string sha1 = GetSha1(installPath);
            Console.WriteLine("SHA1: {0}", sha1 ?? "Not Installed");

            if (sha1 == null)
                Install(new CalMedInstall(root["CalMedInstall"]), installPath);

            CalMedInstall update = new CalMedInstall(root["CalMedUpdate"]);
            if (update.Sha1 != sha1)
                Install(update, installPath);

            if (itemizedSts.Count > 0)
            {
                foreach (ItemizedSt item in itemizedSts)
                    CopyItemizedSt(item, installPath);

                Console.WriteLine("Copied ItemizedSts");
            }

            if (desktopShortcut != null)
            {
                CreateDesktopShortcut(installPath, desktopShortcut);
                Console.WriteLine("Created shortcut");
            }
        }

        private static void Install(CalMedInstall install, string installPath)
        {
            Console.WriteLine("Starting Install {0}", install.FilePath);
            install.PerformInstall(installPath);
            Console.WriteLine("Installation Finished");
            Console.WriteLine("Starting Post Install");
            install.PerformPostInstall(installPath);
            Console.WriteLine("Post Install Finished");
        }

        private static string getAllUsersDesktopDirectory()
        {
            StringBuilder path = new StringBuilder(260);
            SHGetSpecialFolderPath(IntPtr.Zero, path, 0x19, false);
            return path.ToString();
        }

        private static void DeleteDesktopShortcut(string installPath, DesktopShortcut desktopShortcut)
        {
            string shortcutPath = Path.Combine(getAllUsersDesktopDirectory(), String.Format("{0}.lnk", desktopShortcut.Name));

            if (System.IO.File.Exists(shortcutPath)) { System.IO.File.Delete(shortcutPath); }
        }

        private static void CreateDesktopShortcut(string installPath, DesktopShortcut desktopShortcut)
        {
            DeleteDesktopShortcut(installPath, desktopShortcut);

            string shortcutPath = Path.Combine(getAllUsersDesktopDirectory(), String.Format("{0}.lnk", desktopShortcut.Name));
            string mainFilePath = Path.Combine(installPath, "maincds.exe");

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
    }
}
