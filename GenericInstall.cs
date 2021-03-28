using System.Diagnostics;

namespace CalMedUpdater
{
    public class GenericInstall : Install
    {
        public virtual SplitPath FilePath { get; set; }
        public virtual string FileArguments { get; set; }

        public virtual void PerformInstall(string installPath)
        {
            Process.Start(FilePath, FileArguments)?.WaitForExit();
        }

        public virtual void PerformPostInstall(string installPath)
        {
        }
    }
}
