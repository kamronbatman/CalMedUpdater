using System.IO;

namespace CalMedUpdater
{
    public static class Hashing
    {
        public static string HashFile(HashType hashType, FileInfo fi)
        {
            switch (hashType)
            {
                case HashType.SHA256:
                    return SHA256Hashing.GetFileHash(fi);
            }

            return null;
        }
    }
}
