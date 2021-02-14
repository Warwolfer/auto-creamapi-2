using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;
using auto_creamapi.Models;
using auto_creamapi.Utils;

namespace auto_creamapi.Services
{
    public interface ICreamDllService
    {
        public string TargetPath { get; set; }
        public Task Initialize();
        public void Save();
        public void CheckIfDllExistsAtTarget();
        public bool CreamApiApplied();
        public bool CreamApiApplied(string arch);
    }

    public class CreamDllService : ICreamDllService
    {
        private const string X86Arch = "x86";
        private const string X64Arch = "x64";
        private static readonly string HashPath = Path.Combine(Directory.GetCurrentDirectory(), "cream_api.md5");

        private Dictionary<string, CreamDll> _creamDlls;
        private bool _x64Exists;

        private bool _x86Exists;

        public string TargetPath { get; set; }

        public async Task Initialize()
        {
            MyLogger.Log.Debug("CreamDllService: Initialize begin");
            _creamDlls = new Dictionary<string, CreamDll>
            {
                {X86Arch, new CreamDll("steam_api.dll", "steam_api_o.dll")},
                {X64Arch, new CreamDll("steam_api64.dll", "steam_api64_o.dll")}
            };

            if (!File.Exists(HashPath))
            {
                MyLogger.Log.Information("Writing md5sum file...");
                await File.WriteAllLinesAsync(HashPath,
                    new[]
                    {
                        $"{_creamDlls[X86Arch].Hash}  {_creamDlls[X86Arch].Filename}",
                        $"{_creamDlls[X64Arch].Hash}  {_creamDlls[X64Arch].Filename}"
                    }).ConfigureAwait(false);
            }

            MyLogger.Log.Debug("CreamDllService: Initialize end");
        }

        public void Save()
        {
            if (_x86Exists) CopyDll(X86Arch);
            if (_x64Exists) CopyDll(X64Arch);
        }

        public void CheckIfDllExistsAtTarget()
        {
            var x86File = Path.Combine(TargetPath, "steam_api.dll");
            var x64File = Path.Combine(TargetPath, "steam_api64.dll");
            _x86Exists = File.Exists(x86File);
            _x64Exists = File.Exists(x64File);
            if (_x86Exists) MyLogger.Log.Information("x86 SteamAPI DLL found: {X}", x86File);
            if (_x64Exists) MyLogger.Log.Information("x64 SteamAPI DLL found: {X}", x64File);
        }

        public bool CreamApiApplied()
        {
            var a = CreamApiApplied("x86");
            var b = CreamApiApplied("x64");
            return a | b;
        }

        private void CopyDll(string arch)
        {
            var sourceSteamApiDll = _creamDlls[arch].Filename;
            var targetSteamApiDll = Path.Combine(TargetPath, _creamDlls[arch].Filename);
            var targetSteamApiOrigDll = Path.Combine(TargetPath, _creamDlls[arch].OrigFilename);
            var targetSteamApiDllBackup = Path.Combine(TargetPath, $"{_creamDlls[arch].Filename}.backup");
            MyLogger.Log.Information("Setting up CreamAPI DLL @ {TargetPath} (arch :{Arch})", TargetPath, arch);
            // Create backup of steam_api.dll
            File.Copy(targetSteamApiDll, targetSteamApiDllBackup, true);
            // Check if steam_api_o.dll already exists
            // If missing rename original file 
            if (!File.Exists(targetSteamApiOrigDll))
                File.Move(targetSteamApiDll, targetSteamApiOrigDll, true);
            // Copy creamapi dll
            File.Copy(sourceSteamApiDll, targetSteamApiDll, true);
        }

        public bool CreamApiApplied(string arch)
        {
            var a = File.Exists(Path.Combine(TargetPath, _creamDlls[arch].OrigFilename));
            var b = GetHash(Path.Combine(TargetPath, _creamDlls[arch].Filename)).Equals(_creamDlls[arch].Hash);
            return a & b;
        }

        private static string GetHash(string filename)
        {
            if (!File.Exists(filename)) return "";
            using var md5 = MD5.Create();
            using var stream = File.OpenRead(filename);
            return BitConverter
                .ToString(md5.ComputeHash(stream))
                .Replace("-", string.Empty);

        }
    }
}