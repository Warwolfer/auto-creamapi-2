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
using auto_creamapi.Utils;
using SharpCompress.Archives;
using SharpCompress.Common;
using SharpCompress.Readers;
using HttpProgress;

namespace auto_creamapi.Model
{
    public class CreamDllModel
    {
        private struct CreamDll
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
                using var stream = File.OpenRead(Filename);
                Hash = BitConverter
                    .ToString(md5.ComputeHash(stream))
                    .Replace("-", string.Empty);
            }
        }

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
            if (!(File.Exists("steam_api.dll") && File.Exists("steam_api64.dll")))
            {
                MyLogger.Log.Information("Missing files, trying to download...");
                new Action(async() => await DownloadDll(Secrets.ForumUsername, Secrets.ForumPassword))();
            }
            else
            {
                Init();
            }
        }

        private void Init()
        {
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

        private async Task DownloadDll(string username, string password)
        {
            var wnd = new DownloadWindow();
            wnd.Show();
            var container = new CookieContainer();
            var handler = new HttpClientHandler {CookieContainer = container};
            var client = new HttpClient(handler);
            var formContent = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("username", username),
                new KeyValuePair<string, string>("password", password),
                new KeyValuePair<string, string>("redirect", "./ucp.php?mode=login"),
                new KeyValuePair<string, string>("login", "login")
            });
            var response1 = await client.PostAsync("https://cs.rin.ru/forum/ucp.php?mode=login", formContent);
            MyLogger.Log.Debug($"Login Status Code: {response1.EnsureSuccessStatusCode().StatusCode.ToString()}");
            var cookie = container.GetCookies(new Uri("https://cs.rin.ru/forum/ucp.php?mode=login"))
                .FirstOrDefault(c => c.Name.Contains("_sid"));
            MyLogger.Log.Debug($"Login Cookie: {cookie}");
            var response2 = await client.GetAsync("https://cs.rin.ru/forum/viewtopic.php?t=70576");
            MyLogger.Log.Debug(
                $"Download Page Status Code: {response2.EnsureSuccessStatusCode().StatusCode.ToString()}");
            var content = response2.Content.ReadAsStringAsync();
            var contentResult = await content;

            var expression =
                new Regex(".*<a href=\"\\.(?<url>\\/download\\/file\\.php\\?id=.*)\">(?<filename>.*)<\\/a>.*");
            using var reader = new StringReader(contentResult);
            string line;
            var archiveFileList = new Dictionary<string, string>();
            while ((line = await reader.ReadLineAsync()) != null)
            {
                var match = expression.Match(line);
                // ReSharper disable once InvertIf
                if (match.Success)
                {
                    archiveFileList.Add(match.Groups["filename"].Value,
                        $"https://cs.rin.ru/forum{match.Groups["url"].Value}");
                    MyLogger.Log.Debug(archiveFileList.LastOrDefault().Key);
                }
            }

            /*foreach (var (filename, url) in archiveFileList)
            {
                MyLogger.Log.Information($"Downloading file: {filename}");
                var fileResponse = await client.GetAsync(url);
                var download = fileResponse.Content.ReadAsByteArrayAsync();
                await File.WriteAllBytesAsync(filename, await download);
            }*/
            MyLogger.Log.Debug("Choosing first element from list...");
            var (filename, url) = archiveFileList.FirstOrDefault();
            MyLogger.Log.Information("Start download...");
            wnd.FilenameLabel.Content = filename;
            /*var fileResponse = await client.GetAsync(url);
            var download = fileResponse.Content.ReadAsByteArrayAsync();
            await File.WriteAllBytesAsync(filename, await download);
            MyLogger.Log.Information($"Download success? {download.IsCompletedSuccessfully}");*/
            var progress = new Progress<ICopyProgress>(
                x =>
                {
                    wnd.PercentLabel.Content = x.PercentComplete.ToString("P");
                    wnd.ProgressBar.Dispatcher.Invoke(() => wnd.ProgressBar.Value = x.PercentComplete,
                        DispatcherPriority.Background);
                });
            await using (var fileStream = File.OpenWrite(filename))
            {
                var task = client.GetAsync(url, fileStream, progress);
                var response = await task;
                /*if (task.IsCompletedSuccessfully)
                {
                    wnd.PercentLabel.Content = "100,00%";
                    wnd.ProgressBar.Value = 1;
                }*/
            }

            MyLogger.Log.Information("Start extraction...");
            var options = new ReaderOptions {Password = "cs.rin.ru"};
            var archive = ArchiveFactory.Open(filename, options);
            var expression1 = new Regex(@"nonlog_build\\steam_api(?:64)?\.dll");
            foreach (var entry in archive.Entries)
            {
                // ReSharper disable once InvertIf
                if (!entry.IsDirectory && expression1.IsMatch(entry.Key))
                {
                    MyLogger.Log.Debug(entry.Key);
                    entry.WriteToDirectory(Directory.GetCurrentDirectory(), new ExtractionOptions
                    {
                        ExtractFullPath = false,
                        Overwrite = true
                    });
                }
            }
            MyLogger.Log.Information("Extraction done!");
            wnd.Close();
            Init();
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
            MyLogger.Log.Information($"Creating CreamAPI DLL @ {targetSteamApiDll}");
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