using System;
using System.IO;
using System.Security.Cryptography;

namespace auto_creamapi.Models
{
    internal class CreamDll
    {
        public readonly string Filename;
        public readonly string Hash;
        public readonly string OrigFilename;

        public CreamDll(string filename, string origFilename)
        {
            Filename = filename;
            OrigFilename = origFilename;
            Hash = "";

            using var md5 = MD5.Create();
            if (File.Exists(Filename))
            {
                using var stream = File.OpenRead(Filename);
                Hash = BitConverter
                    .ToString(md5.ComputeHash(stream))
                    .Replace("-", string.Empty);
            }
        }
    }

    public class CreamDllModel
    {
    }
}