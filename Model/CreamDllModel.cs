using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Threading;
using auto_creamapi.Services;
using auto_creamapi.Utils;
using SharpCompress.Archives;
using SharpCompress.Common;
using SharpCompress.Readers;
using HttpProgress;

namespace auto_creamapi.Model
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
        private static readonly Lazy<CreamDllModel> Lazy =
            new Lazy<CreamDllModel>(() => new CreamDllModel());

        public static CreamDllModel Instance => Lazy.Value;
        public string TargetPath { get; set; }

        private readonly Dictionary<string, CreamDll> _creamDlls = new Dictionary<string, CreamDll>();
        private static readonly string HashPath = Path.Combine(Directory.GetCurrentDirectory(), "cream_api.md5");
        private const string X86Arch = "x86";
        private const string X64Arch = "x64";

        private bool _x86Exists;
        private bool _x64Exists;

        private CreamDllModel()
        {
        }

        public async Task Initialize()
        {
            if (!(File.Exists("steam_api.dll") && File.Exists("steam_api64.dll")))
            {
                MyLogger.Log.Information("Missing files, trying to download...");
                var task = DownloadCreamApiService.DownloadAndExtract(Secrets.ForumUsername, Secrets.ForumPassword);
                await task;
                task.Wait();
            }
            
            _creamDlls.Add(X86Arch, new CreamDll("steam_api.dll", "steam_api_o.dll"));
            _creamDlls.Add(X64Arch, new CreamDll("steam_api64.dll", "steam_api64_o.dll"));

            if (!File.Exists(HashPath))
            {
                MyLogger.Log.Information("Writing md5sum file...");
                File.WriteAllLines(HashPath, new[]
                {
                    $"{_creamDlls[X86Arch].Hash}  {_creamDlls[X86Arch].Filename}",
                    $"{_creamDlls[X64Arch].Hash}  {_creamDlls[X64Arch].Filename}"
                });
            }
        }

        public void Save()
        {
            if (_x86Exists) CopyDll(X86Arch);
            if (_x64Exists) CopyDll(X64Arch);
        }

        private void CopyDll(string arch)
        {
            var sourceSteamApiDll = _creamDlls[arch].Filename;
            var targetSteamApiDll = Path.Combine(TargetPath, _creamDlls[arch].Filename);
            var targetSteamApiOrigDll = Path.Combine(TargetPath, _creamDlls[arch].OrigFilename);
            var targetSteamApiDllBackup = Path.Combine(TargetPath, $"{_creamDlls[arch].Filename}.backup");
            MyLogger.Log.Information($"Setting up CreamAPI DLL @ {TargetPath} (arch :{arch})");
            // Create backup of steam_api.dll
            File.Copy(targetSteamApiDll, targetSteamApiDllBackup, true);
            // Check if steam_api_o.dll already exists
            // If missing rename original file 
            if (!File.Exists(targetSteamApiOrigDll))
                File.Move(targetSteamApiDll, targetSteamApiOrigDll, true);
            // Copy creamapi dll
            File.Copy(sourceSteamApiDll, targetSteamApiDll, true);
        }

        public void CheckExistence()
        {
            var x86file = Path.Combine(TargetPath, "steam_api.dll");
            var x64file = Path.Combine(TargetPath, "steam_api64.dll");
            _x86Exists = File.Exists(x86file);
            _x64Exists = File.Exists(x64file);
            if (_x86Exists) MyLogger.Log.Information($"x86 SteamAPI DLL found: {x86file}");
            if (_x64Exists) MyLogger.Log.Information($"x64 SteamAPI DLL found: {x64file}");
        }

        public bool CreamApiApplied(string arch)
        {
            bool a = File.Exists(Path.Combine(TargetPath, _creamDlls[arch].OrigFilename));
            bool b = GetHash(Path.Combine(TargetPath, _creamDlls[arch].Filename)).Equals(_creamDlls[arch].Hash);
            return a & b;
        }

        public bool CreamApiApplied()
        {
            bool a = CreamApiApplied("x86");
            bool b = CreamApiApplied("x64");
            return a | b;
        }

        private string GetHash(string filename)
        {
            if (File.Exists(filename))
            {
                using var md5 = MD5.Create();
                using var stream = File.OpenRead(filename);
                return BitConverter
                    .ToString(md5.ComputeHash(stream))
                    .Replace("-", string.Empty);
            }
            else
            {
                return "";
            }
        }
    }
}