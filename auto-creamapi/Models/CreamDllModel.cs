using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;
using auto_creamapi.Services;
using auto_creamapi.Utils;

namespace auto_creamapi.Models
{
    internal class CreamDll
    {
        public readonly string Filename;
        public readonly string OrigFilename;
        public readonly string Hash;

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