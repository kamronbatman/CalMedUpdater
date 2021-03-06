﻿namespace CalMedUpdater
{
    public interface Install
    {
        SplitPath FilePath { get; }
        string FileArguments { get; }

        void PerformInstall(string installPath);
        void PerformPostInstall(string installPath);
    }
}
