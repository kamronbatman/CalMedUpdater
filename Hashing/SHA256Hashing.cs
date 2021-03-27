using System;
using System.IO;
using System.Security.Cryptography;

namespace CalMedUpdater
{
    public static class SHA256Hashing
    {
        public static string GetFileHash(FileInfo fInfo)
        {
            using var sha256 = SHA256.Create();
            using var fs = fInfo.Open(FileMode.Open);
            fs.Position = 0;

            return BitConverter.ToString(sha256.ComputeHash(fs)).Replace("-", "");
        }
    }
}
