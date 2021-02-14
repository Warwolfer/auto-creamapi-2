using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using auto_creamapi.Messenger;
using auto_creamapi.Utils;
using HttpProgress;
using MvvmCross.Plugin.Messenger;
using SevenZip;

namespace auto_creamapi.Services
{
    public interface IDownloadCreamApiService
    {
        public Task<string> Download(string username, string password);
        public Task Extract(string filename);
    }

    public class DownloadCreamApiService : IDownloadCreamApiService
    {
        private const string ArchivePassword = "cs.rin.ru";
        private readonly IMvxMessenger _messenger;

        public DownloadCreamApiService(IMvxMessenger messenger)
        {
            _messenger = messenger;
        }

        public async Task<string> Download(string username, string password)
        {
            MyLogger.Log.Debug("Download");
            var container = new CookieContainer();
            var handler = new HttpClientHandler {CookieContainer = container};
            var client = new HttpClient(handler);
            client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:86.0) " +
                                                            "Gecko/20100101 Firefox/86.0");
            var formContent = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("username", username),
                new KeyValuePair<string, string>("password", password),
                new KeyValuePair<string, string>("redirect", "./ucp.php?mode=login"),
                new KeyValuePair<string, string>("login", "login")
            });
            MyLogger.Log.Debug("Download: post login");
            var response1 = await client.PostAsync("https://cs.rin.ru/forum/ucp.php?mode=login", formContent)
                .ConfigureAwait(false);
            MyLogger.Log.Debug("Login Status Code: {StatusCode}", 
                response1.EnsureSuccessStatusCode().StatusCode);
            var cookie = container.GetCookies(new Uri("https://cs.rin.ru/forum/ucp.php?mode=login"))
                .FirstOrDefault(c => c.Name.Contains("_sid"));
            MyLogger.Log.Debug("Login Cookie: {Cookie}", cookie);
            var response2 = await client.GetAsync("https://cs.rin.ru/forum/viewtopic.php?t=70576")
                .ConfigureAwait(false);
            MyLogger.Log.Debug("Download Page Status Code: {StatusCode}", 
                response2.EnsureSuccessStatusCode().StatusCode);
            var content = response2.Content.ReadAsStringAsync();
            var contentResult = await content.ConfigureAwait(false);

            var expression =
                new Regex(".*<a href=\"\\.(?<url>\\/download\\/file\\.php\\?id=.*)\">(?<filename>.*)<\\/a>.*");
            using var reader = new StringReader(contentResult);
            string line;
            var archiveFileList = new Dictionary<string, string>();
            while ((line = await reader.ReadLineAsync().ConfigureAwait(false)) != null)
            {
                var match = expression.Match(line);
                // ReSharper disable once InvertIf
                if (match.Success)
                {
                    archiveFileList.Add(match.Groups["filename"].Value,
                        $"https://cs.rin.ru/forum{match.Groups["url"].Value}");
                    MyLogger.Log.Debug("{X}", archiveFileList.LastOrDefault().Key);
                }
            }

            MyLogger.Log.Debug("Choosing first element from list...");
            var (filename, url) = archiveFileList.FirstOrDefault();
            if (File.Exists(filename))
            {
                MyLogger.Log.Information("{Filename} already exists, skipping download...", filename);
                return filename;
            }

            MyLogger.Log.Information("Start download...");
            var progress = new Progress<ICopyProgress>(
                x => _messenger.Publish(new ProgressMessage(this, "Downloading...", filename, x)));
            await using var fileStream = File.OpenWrite(filename);
            var task = client.GetAsync(url, fileStream, progress);
            await task.ConfigureAwait(false);
            if (task.IsCompletedSuccessfully)
                _messenger.Publish(new ProgressMessage(this, "Downloading...", filename, 1.0));
            MyLogger.Log.Information("Download done.");
            return filename;
        }

        public async Task Extract(string filename)
        {
            MyLogger.Log.Debug("Extract");
            var cwd = Directory.GetCurrentDirectory();
            const string nonlogBuild = "nonlog_build";
            const string steamApi64Dll = "steam_api64.dll";
            const string steamApiDll = "steam_api.dll";
            MyLogger.Log.Information(@"Start extraction of ""{Filename}""...", filename);
            var nonlogBuildPath = Path.Combine(cwd, nonlogBuild);
            if (Directory.Exists(nonlogBuildPath))
                Directory.Delete(nonlogBuildPath, true);
            _messenger.Publish(new ProgressMessage(this, "Extracting...", filename, 1.0));
            SevenZipBase.SetLibraryPath(Path.Combine(cwd, "resources/7z.dll"));
            using (var extractor =
                new SevenZipExtractor(filename, ArchivePassword, InArchiveFormat.Rar)
                    {PreserveDirectoryStructure = false})
            {
                await extractor.ExtractFilesAsync(
                    cwd,
                    $@"{nonlogBuild}\{steamApi64Dll}",
                    $@"{nonlogBuild}\{steamApiDll}"
                ).ConfigureAwait(false);
            }

            if (File.Exists(Path.Combine(nonlogBuildPath, steamApi64Dll)))
                File.Move(
                    Path.Combine(cwd, nonlogBuild, steamApi64Dll),
                    Path.Combine(cwd, steamApi64Dll),
                    true
                );

            if (File.Exists(Path.Combine(nonlogBuildPath, steamApiDll)))
                File.Move(
                    Path.Combine(nonlogBuildPath, steamApiDll),
                    Path.Combine(cwd, steamApiDll),
                    true
                );

            if (Directory.Exists(nonlogBuildPath))
                Directory.Delete(nonlogBuildPath, true);
            MyLogger.Log.Information("Extraction done!");
        }
    }
}