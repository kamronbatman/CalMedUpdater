using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace CalMedUpdater
{
    public class CalMedInstall : GenericInstall
    {
        [DllImport("shell32.dll")]
        static extern bool SHGetSpecialFolderPath(IntPtr hwndOwner, [Out] StringBuilder lpszPath, int nFolder, bool fCreate);

        public string ConfigPath { get; set; }
        public string Sha1 { get; set; }
        public string XerexRegistry { get; set; }
        public override string FilePath { get; set; }
        public override string FileArguments {
            get
            {
                return String.Format("/LOADINF={0} /VERYSILENT", ConfigPath);
            }

            set { }
        }
        public override bool Is64 { get; set; }

        public List<ItemizedSt> ItemizedSts { get; set; } = new List<ItemizedSt>();

        private string getAllUsersStartMenu()
        {
            StringBuilder path = new StringBuilder(260);
            SHGetSpecialFolderPath(IntPtr.Zero, path, 0x16, false);
            return path.ToString();
        }

        private string getSystem32Directory()
        {
            StringBuilder path = new StringBuilder(260);
            SHGetSpecialFolderPath(IntPtr.Zero, path, 0x29, false);
            return path.ToString();
        }

        private void CopyItemizedSt(ItemizedSt item, string installPath)
        {
            string dest = Path.Combine(Path.Combine(installPath, @"rpt\ItemizedSt"), item.File);
            if (File.Exists(dest)) { File.Delete(dest); }

            File.Copy(Path.Combine(item.Source, item.File), dest);
        }

        public override void PerformPostInstall(string installPath)
        {
            // Copy Xerex to startup
            string xerexFile = Path.Combine(getAllUsersStartMenu(), @"Programs\Startup\XerexServer.exe");
            if (File.Exists(xerexFile)) { File.Delete(xerexFile); }
            File.Copy(Path.Combine(installPath, @"installation_files\XerexServer.exe"), xerexFile);
            Console.WriteLine("Xerex Copied");

            // Register Midas
            string midasFile = Path.Combine(getSystem32Directory(), "midas.dll");
            if (File.Exists(midasFile)) {
                Process uregsvr32 = Process.Start(Path.Combine(getSystem32Directory(), "regsvr32.exe"), "/u /s midas.dll");
                uregsvr32.WaitForExit();

                File.Delete(midasFile);
                Console.WriteLine("Midas File Unregistered & Deleted");
            }

            File.Copy(Path.Combine(installPath, @"installation_files\midas.dll"), midasFile);

            Process regsvr32 = Process.Start(Path.Combine(getSystem32Directory(), "regsvr32.exe"), "/s midas.dll");
            regsvr32.WaitForExit();
            Console.WriteLine("Midas Copied & Registered");

            Process regeditProcess = Process.Start("regedit.exe", String.Format("/s {0}", XerexRegistry));
            regeditProcess.WaitForExit();
            Console.WriteLine("Xerex Registry Imported");

            if (ItemizedSts.Count > 0)
            {
                foreach (ItemizedSt item in ItemizedSts)
                    CopyItemizedSt(item, installPath);

                Console.WriteLine("Copied ItemizedSts");
            }

            /*
            Process xerex = new Process();
            xerex.StartInfo = new ProcessStartInfo(Path.Combine(getAllUsersStartMenu(), @"Programs\Startup\XerexServer.exe"));
            xerex.Start();
            Console.WriteLine("Xerex Started");
            */
        }
    }
}
