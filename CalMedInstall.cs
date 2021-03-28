using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Xml;

namespace CalMedUpdater
{
    public class CalMedInstall : GenericInstall
    {
        public SplitPath ConfigPath { get; set; }
        public SplitPath XerexRegistry { get; set; }
        public override SplitPath FilePath { get; set; }
        public override string FileArguments => $"/LOADINF={ConfigPath} /VERYSILENT";

        public CalMedInstall(XmlNode node)
        {
            ConfigPath = new SplitPath(node["Config"]);
            XerexRegistry = new SplitPath(node["XerexRegistry"]);
            FilePath = new SplitPath(node["FilePath"]);
        }

        private string GetAllUsersStartMenu()
        {
            var path = new StringBuilder(260);
            Utility.SHGetSpecialFolderPath(IntPtr.Zero, path, 0x16, false);
            return path.ToString();
        }

        private string GetSystem32Directory()
        {
            var path = new StringBuilder(260);
            Utility.SHGetSpecialFolderPath(IntPtr.Zero, path, 0x29, false);
            return path.ToString();
        }

        private static void KillXerex()
        {
            try
            {
                var processes = Process.GetProcesses();

                foreach (var process in processes)
                {
                    if (!process.ProcessName.ToLowerInvariant().Contains("xerex")) continue;
                    process.Kill();
                    process.WaitForExit();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        public override void PerformPostInstall(string installPath)
        {
            if (XerexRegistry.Path == null)
            {
                return;
            }

            // Kill Xerex
            KillXerex();

            // Register Midas
            var midasFile = Path.Combine(GetSystem32Directory(), "midas.dll");
            if (File.Exists(midasFile))
            {
                Process.Start(Path.Combine(GetSystem32Directory(), "regsvr32.exe"), "/u /s midas.dll")?.WaitForExit();

                File.Delete(midasFile);
                Console.WriteLine("Midas File Unregistered & Deleted");
            }

            File.Copy(Path.Combine(installPath, @"installation_files\midas.dll"), midasFile);

            Process.Start(Path.Combine(GetSystem32Directory(), "regsvr32.exe"), "/s midas.dll")?.WaitForExit();
            Console.WriteLine("Midas Copied & Registered");

            Process.Start("regedit.exe", String.Format("/s {0}", XerexRegistry))?.WaitForExit();
            Console.WriteLine("Xerex Registry Imported");

            // Copy Xerex to startup
            var xerexFile = Path.Combine(GetAllUsersStartMenu(), @"Programs\Startup\XerexServer.exe");
            if (File.Exists(xerexFile)) { File.Delete(xerexFile); }
            File.Copy(Path.Combine(installPath, @"installation_files\XerexServer.exe"), xerexFile);
            Console.WriteLine("Xerex Copied");

            // Start Xerex
            Process.Start(xerexFile);
            Console.WriteLine("Xerex Started");
        }
    }
}
