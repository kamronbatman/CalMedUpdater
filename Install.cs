using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace CalMedUpdater
{
    public interface Install
    {
        string FilePath { get; }
        string FileArguments { get; }

        void PerformInstall(string installPath);
        void PerformPostInstall(string installPath);
    }
}
