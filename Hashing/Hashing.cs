using System.IO;

namespace CalMedUpdater
{
    public static class Hashing
    {
        public static string HashFile(HashType hashType, FileInfo fi)
        {
            return hashType switch
            {
                HashType.SHA256 => SHA256Hashing.GetFileHash(fi),
                _ => null
            };
        }
    }
}
