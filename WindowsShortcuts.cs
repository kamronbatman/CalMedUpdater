using IWshRuntimeLibrary;
using System;
using System.IO;
using System.Text;
using File = System.IO.File;

namespace CalMedUpdater
{
    public static class WindowsShortcuts
    {
        private static string GetAllUsersDesktopDirectory()
        {
            var path = new StringBuilder(260);
            Utility.SHGetSpecialFolderPath(IntPtr.Zero, path, 0x19, false);
            return path.ToString();
        }

        public static void DeleteDesktopShortcut(DesktopShortcut desktopShortcut)
        {
            var shortcutPath = Path.Combine(GetAllUsersDesktopDirectory(), $"{desktopShortcut.Name}.lnk");

            File.Delete(shortcutPath);
        }

        public static void CreateDesktopShortcut(string installPath, string fileName, DesktopShortcut desktopShortcut)
        {
            DeleteDesktopShortcut(desktopShortcut);

            var shortcutPath = Path.Combine(GetAllUsersDesktopDirectory(), $"{desktopShortcut.Name}.lnk");
            var mainFilePath = Path.Combine(installPath, fileName);

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
