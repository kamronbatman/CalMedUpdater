using IWshRuntimeLibrary;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using File = System.IO.File;

namespace CalMedUpdater
{
    public static class WindowsShortcuts
    {
        [DllImport("shell32.dll")]
        static extern bool SHGetSpecialFolderPath(IntPtr hwndOwner, [Out] StringBuilder lpszPath, int nFolder, bool fCreate);

        private static string getAllUsersDesktopDirectory()
        {
            StringBuilder path = new StringBuilder(260);
            SHGetSpecialFolderPath(IntPtr.Zero, path, 0x19, false);
            return path.ToString();
        }

        public static void DeleteDesktopShortcut(DesktopShortcut desktopShortcut)
        {
            string shortcutPath = Path.Combine(getAllUsersDesktopDirectory(), String.Format("{0}.lnk", desktopShortcut.Name));

            File.Delete(shortcutPath);
        }

        public static void CreateDesktopShortcut(string installPath, string fileName, DesktopShortcut desktopShortcut)
        {
            DeleteDesktopShortcut(desktopShortcut);

            string shortcutPath = Path.Combine(getAllUsersDesktopDirectory(), String.Format("{0}.lnk", desktopShortcut.Name));
            string mainFilePath = Path.Combine(installPath, fileName);

            WshShell shell = new WshShell();
            IWshShortcut shortcut = shell.CreateShortcut(shortcutPath) as IWshShortcut;
            shortcut.Description = desktopShortcut.Name;
            shortcut.WorkingDirectory = installPath;
            shortcut.TargetPath = mainFilePath;
            shortcut.Arguments = desktopShortcut.Arguments;
            shortcut.IconLocation = mainFilePath;
            shortcut.Save();
        }
    }
}
