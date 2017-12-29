using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace CalMedUpdater
{
    public class GenericInstall : Install
    {
        public virtual string FilePath { get; set; }
        public virtual string FileArguments { get; set; }
        public virtual bool Is64 { get; set; }

        public virtual void PerformInstall(string installPath)
        {
            Console.WriteLine("Starting Installation: {0}", FilePath);
            Process process = Process.Start(FilePath, FileArguments);
            process.WaitForExit();
            Console.WriteLine("Installation Finished");
        }

        public virtual void PerformPostInstall(string installPath)
        {
        }
    }
}
